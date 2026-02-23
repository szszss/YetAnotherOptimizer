using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace YaOpt.Unity
{
	[BurstCompile]
	public class YaOptBurst
	{
		// Core

		[BurstCompile]
		public static void ComputeMatrix(ref Matrix4x4 matrix, in Vector3 offset, in Vector3 pivot, in Quaternion rotation, in Vector3 scale, bool canRotate)
		{
			// Original code:
			// var hasPivot = math.any(pivot);
			// matrix *= Matrix4x4.TRS(offset + pivot, canRotate ? rotation : Quaternion.identity, scale);
			// if (hasPivot)
			// {
			//   matrix *= Matrix4x4.Translate(-pivot);
			// }
			
			// After optimized by AI:
			// Optimization: Construct the transform matrix directly using Unity.Mathematics
			// Logic: Combined = Translate(offset + pivot) * Rotate * Scale * Translate(-pivot)
			// This avoids multiple Matrix4x4.TRS/Translate calls and matrix multiplications.

			float3 p = pivot;
			float3 s = scale;
			
			// 1. Rotation & Scale (RS part)
			// If canRotate is false, we just use Scale (Rotation is Identity)
			float3x3 rs;
			if (canRotate)
			{
				quaternion r = rotation;
				rs = math.mul(new float3x3(r), float3x3.Scale(s));
			}
			else
			{
				rs = float3x3.Scale(s);
			}

			// 2. Translation (T part)
			// Final Position = (offset + pivot) - (RS * pivot)
			// We subtract (RS * pivot) because Translate(-pivot) happens *before* RS in the TRS chain logic,
			// effectively moving the pivot point to origin, transforming, then moving back.
			float3 t = ((float3)offset + p) - math.mul(rs, p);

			// 3. Compose final local matrix
			float4x4 localTransform = new float4x4(rs, t);

			// 4. Apply to original matrix
			// Convert ref Matrix4x4 to float4x4, multiply, and convert back
			// Since Matrix4x4 and float4x4 have same memory layout, this is efficient.
			float4x4 m = (float4x4)matrix;
			m = math.mul(m, localTransform);
			matrix = (Matrix4x4)m;
		}
		
		[BurstCompile]
		public static void ApplyAltitude(ref Matrix4x4 matrix, float altitude)
		{
			// Original code:
			// Matrix4x4 translate = Matrix4x4.identity;
			// translate.m13 = altitude;
			// matrix *= translate;
			
			// After optimized by AI:
			// Optimization: Avoid full Matrix4x4 multiplication.
			// ApplyAltitude effectively adds (UpVector * altitude) to the Position.
			// In Unity Matrix (Column Major), m13 is the Y translation component of a pre-translation.
			// But the original code was: matrix *= Matrix4x4.Translate(0, 0, altitude) (m13?? Wait, m13 is 'y' in col 3? No)
			
			// Let's re-read original code carefully:
			// Matrix4x4 translate = Matrix4x4.identity;
			// translate.m13 = altitude; // m13 is Row 1, Col 3. Indices are [row, col]. 
			// In Unity, Column 3 is the position (x, y, z, 1). 
			// So m03=x, m13=y, m23=z.
			// So original code set m13=altitude. This is a translation of (0, altitude, 0).
			// Wait, previous code said "translate.m13 = altitude".
			// Matrix4x4.Translate(new Vector3(0,0,altitude)) would set m23 (z).
			// Matrix4x4 indices are [row, column]. m13 is 2nd row (y), 4th column (pos).
			// So the original code translates by (0, altitude, 0) in LOCAL space?
			// No, matrix *= translate means: New = Old * Translate.
			// This transforms the coordinate system.
			// Resulting Position += Old.Rotation * (0, altitude, 0).
			// This is equivalent to adding (Old.Up * altitude) to the position.
			
			float4x4 m = (float4x4)matrix;
			
			// m.c1 is the "Up" vector (2nd column).
			// We add (Up * altitude) to the Position (m.c3).
			m.c3 += m.c1 * altitude;
			
			matrix = (Matrix4x4)m;
		}

		// Facial Animation

		[BurstCompile]
		[StructLayout(LayoutKind.Explicit)]
		public struct FacialAnimationFrameStruct
		{
			[FieldOffset(0*16+0 )] public float4  HeadOffsetAsFloat4;
			[FieldOffset(0*16+0 )] public Vector3 HeadOffset;
			[FieldOffset(0*16+12)] private float _padding0;
			[FieldOffset(1*16+0 )] public float4  BrowOffsetAsFloat4;
			[FieldOffset(1*16+0 )] public Vector3 BrowOffset;
			[FieldOffset(1*16+12)] private float _padding1;
			[FieldOffset(2*16+0 )] public float4  LidOffsetAsFloat4;
			[FieldOffset(2*16+0 )] public Vector3 LidOffset;
			[FieldOffset(2*16+12)] private float _padding2;
			[FieldOffset(3*16+0 )] public float4  EyeballOffsetAsFloat4;
			[FieldOffset(3*16+0 )] public Vector3 EyeballOffset;
			[FieldOffset(3*16+12)] private float _padding3;
			[FieldOffset(4*16+0 )] public float4  EyeballOffsetLAsFloat4;
			[FieldOffset(4*16+0 )] public Vector3 EyeballOffsetL;
			[FieldOffset(4*16+12)] private float _padding4;
			[FieldOffset(5*16+0 )] public float4  EyeballOffsetRAsFloat4;
			[FieldOffset(5*16+0 )] public Vector3 EyeballOffsetR;
			[FieldOffset(5*16+12)] private float _padding5;
			[FieldOffset(6*16+0 )] public float4  MouthOffsetAsFloat4;
			[FieldOffset(6*16+0 )] public Vector3 MouthOffset;
			[FieldOffset(6*16+12)] private float _padding6;

			[BurstCompile]
			public void Add(in FacialAnimationFrameStruct other)
			{
				HeadOffsetAsFloat4 += other.HeadOffsetAsFloat4;
				BrowOffsetAsFloat4 += other.BrowOffsetAsFloat4;
				LidOffsetAsFloat4 += other.LidOffsetAsFloat4;
				EyeballOffsetAsFloat4 += other.EyeballOffsetAsFloat4;
				EyeballOffsetLAsFloat4 += other.EyeballOffsetLAsFloat4;
				EyeballOffsetRAsFloat4 += other.EyeballOffsetRAsFloat4;
				MouthOffsetAsFloat4 += other.MouthOffsetAsFloat4;
			}

			[BurstCompile]
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static void ProcessField(ref float4 field, float frameCountFloat)
			{
				field.y += frameCountFloat;
				field /= field.y;
				if (float.IsNaN(field.y))
					field = float4.zero;
				else
					field.y = 0;
			}

			[BurstCompile]
			public void Process(float frameCountFloat)
			{
				ProcessField(ref HeadOffsetAsFloat4, frameCountFloat);
				ProcessField(ref BrowOffsetAsFloat4, frameCountFloat);
				ProcessField(ref LidOffsetAsFloat4, frameCountFloat);
				ProcessField(ref EyeballOffsetAsFloat4, frameCountFloat);
				ProcessField(ref EyeballOffsetLAsFloat4, frameCountFloat);
				ProcessField(ref EyeballOffsetRAsFloat4, frameCountFloat);
				ProcessField(ref MouthOffsetAsFloat4, frameCountFloat);
			}
		}

		[BurstCompile]
		public static void AccumResultFrames(
			in NativeArray<FacialAnimationFrameStruct> frames,
			int FrameCount, ref FacialAnimationFrameStruct result)
		{
			var tmp = new FacialAnimationFrameStruct();
			for (var i = 0; i < FrameCount; i++)
			{
				tmp.Add(frames[i]);
			}
			tmp.Process(FrameCount);
			result = tmp;
		}

		// CombatExtended

		[BurstCompile]
		public static void PointsOnLineOfSight(
			in float3 start,
			in float3 end,
			ref NativeArray<int3> result,
			out int count,
			out bool outOfBound)
		{
			int cx = (int)start.x;
			var cy = (int)start.y;
			int cz = (int)start.z;
			int endX = (int)end.x;
			int endZ = (int)end.z;

			if (cx == endX || cz == endZ)
			{
				PointsOnStraightLineOfSight(start, end, ref result, out count, out outOfBound);
				return;
			}

			outOfBound = false;
			var capacity = result.Length;
			var index = 0;

			var dir = end - start;
			int stepX = dir.x >= 0 ? 1 : -1;
			int stepZ = dir.z >= 0 ? 1 : -1;

			float tMaxX, tMaxZ;

			if (stepX > 0)
			{
				tMaxX = ((cx + 1) - start.x) / dir.x;
			}
			else
			{
				tMaxX = (cx - start.x) / dir.x;
			}

			if (stepZ > 0)
			{
				tMaxZ = ((cz + 1) - start.z) / dir.z;
			}
			else
			{
				tMaxZ = (cz - start.z) / dir.z;
			}

			var tDeltaX = stepX / dir.x;
			var tDeltaZ = stepZ / dir.z;

			result[index++] = new int3(cx, cy, cz);

			while (cx != endX || cz != endZ)
			{
				bool corner = math.abs(tMaxX - tMaxZ) < 0.0001f;

				if (corner)
				{
					if (index + 3 > capacity)
					{
						count = index;
						outOfBound = true;
						return;
					}
					result[index++] = new int3(cx + stepX, cy, cz);
					result[index++] = new int3(cx, cy, cz + stepZ);
					cx += stepX;
					cz += stepZ;
					result[index++] = new int3(cx, cy, cz);
					tMaxX += tDeltaX;
					tMaxZ += tDeltaZ;
				}
				else
				{
					if (index + 1 > capacity)
					{
						count = index;
						outOfBound = true;
						return;
					}
					if (tMaxX < tMaxZ)
					{
						cx += stepX;
						tMaxX += tDeltaX;
						result[index++] = new int3(cx, cy, cz);
					}
					else
					{
						cz += stepZ;
						tMaxZ += tDeltaZ;
						result[index++] = new int3(cx, cy, cz);
					}
				}
			}

			count = index;
		}

		[BurstCompile]
		private static void PointsOnStraightLineOfSight(
			in float3 start,
			in float3 end,
			ref NativeArray<int3> result,
			out int count,
			out bool outOfBound)
		{
			var capacity = result.Length;
			outOfBound = false;

			var sx = (int)start.x;
			var sy = (int)start.y;
			var sz = (int)start.z;
			var ex = (int)end.x;
			var ez = (int)end.z;

			if (sx == ex)
			{
				// Vertical line
				var step = sz <= ez ? 1 : -1;
				count = math.abs(ez - sz) + 1;
				if (count > capacity)
				{
					count = capacity;
					outOfBound = true;
				}
				for (var i = 0; i < count; i++)
				{
					result[i] = new int3(sx, sy, sz + i * step);
				}
			}
			else
			{
				// Horizontal line
				var step = sx <= ex ? 1 : -1;
				count = math.abs(ex - sx) + 1;
				if (count > capacity)
				{
					count = capacity;
					outOfBound = true;
				}
				for (var i = 0; i < count; i++)
				{
					result[i] = new int3(sx + i * step, sy, sz);
				}
			}
		}
	}
}