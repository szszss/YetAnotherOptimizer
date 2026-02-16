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
		[BurstCompile]
		public static void ComputeMatrix(ref Matrix4x4 matrix, in Vector3 offset, in Vector3 pivot, in Quaternion rotation, in Vector3 scale, bool canRotate)
		{
			var hasPivot = math.any(pivot);
			matrix *= Matrix4x4.TRS(offset + pivot, canRotate ? rotation : Quaternion.identity, scale);
			if (hasPivot)
			{
				matrix *= Matrix4x4.Translate(-pivot);
			}
		}

		[BurstCompile]
		public static void ApplyAltitude(ref Matrix4x4 matrix, float altitude)
		{
			Matrix4x4 translate = Matrix4x4.identity;
			translate.m13 = altitude;
			matrix *= translate;
		}

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

		[BurstCompile]
		public struct FacialAnimationAccumResultFrameJob : IJob
		{
			public const int RESULT_HEAD = 0;
			public const int RESULT_BROW = 1;
			public const int RESULT_LID = 2;
			public const int RESULT_EYEBALL = 3;
			public const int RESULT_EYEBALL_L = 4;
			public const int RESULT_EYEBALL_R = 5;
			public const int RESULT_MOUTH = 6;
			public const int RESULT_COUNT = 7;

			[ReadOnly]
			public NativeArray<Vector3> HeadOffsets;

			[ReadOnly]
			public NativeArray<Vector3> BrowOffsets;

			[ReadOnly]
			public NativeArray<Vector3> LidOffsets;

			[ReadOnly]
			public NativeArray<Vector3> EyeballOffsets;

			[ReadOnly]
			public NativeArray<Vector3> EyeballOffsetLs;

			[ReadOnly]
			public NativeArray<Vector3> EyeballOffsetRs;

			[ReadOnly]
			public NativeArray<Vector3> MouthOffsets;

			[ReadOnly]
			public int FrameCount;

			[WriteOnly]
			public NativeArray<Vector3> Results;

			[BurstCompile]
			public void Execute()
			{
				var headOffset = float3.zero;
				var browOffset = float3.zero;
				var lidOffset = float3.zero;
				var eyeballOffset = float3.zero;
				var eyeballOffsetL = float3.zero;
				var eyeballOffsetR = float3.zero;
				var mouthOffset = float3.zero;
				for (var i = 0; i < FrameCount; i++)
				{
					headOffset += HeadOffsets.ReinterpretLoad<float3>(i);
					browOffset += BrowOffsets.ReinterpretLoad<float3>(i);
					lidOffset += LidOffsets.ReinterpretLoad<float3>(i);
					eyeballOffset += EyeballOffsets.ReinterpretLoad<float3>(i);
					eyeballOffsetL += EyeballOffsetLs.ReinterpretLoad<float3>(i);
					eyeballOffsetR += EyeballOffsetRs.ReinterpretLoad<float3>(i);
					mouthOffset += MouthOffsets.ReinterpretLoad<float3>(i);
				}
				var iResults = Results.Reinterpret<float3>();

				var frameNum = FrameCount + headOffset.y;
				iResults[RESULT_HEAD] = frameNum != 0
					? new float3(headOffset.x / frameNum, 0f, headOffset.z / frameNum)
					: float3.zero;

				frameNum = FrameCount + browOffset.y;
				iResults[RESULT_BROW] = frameNum != 0
					? new float3(browOffset.x / frameNum, 0f, browOffset.z / frameNum)
					: float3.zero;

				frameNum = FrameCount + lidOffset.y;
				iResults[RESULT_LID] = frameNum != 0
					? new float3(lidOffset.x / frameNum, 0f, lidOffset.z / frameNum)
					: float3.zero;

				frameNum = FrameCount + eyeballOffset.y;
				iResults[RESULT_EYEBALL] = frameNum != 0
					? new float3(eyeballOffset.x / frameNum, 0f, eyeballOffset.z / frameNum)
					: float3.zero;

				frameNum = FrameCount + eyeballOffsetL.y;
				iResults[RESULT_EYEBALL_L] = frameNum != 0
					? new float3(eyeballOffsetL.x / frameNum, 0f, eyeballOffsetL.z / frameNum)
					: float3.zero;

				frameNum = FrameCount + eyeballOffsetR.y;
				iResults[RESULT_EYEBALL_R] = frameNum != 0
					? new float3(eyeballOffsetR.x / frameNum, 0f, eyeballOffsetR.z / frameNum)
					: float3.zero;

				frameNum = FrameCount + mouthOffset.y;
				iResults[RESULT_MOUTH] = frameNum != 0
					? new float3(mouthOffset.x / frameNum, 0f, mouthOffset.z / frameNum)
					: float3.zero;
			}
		}
	}
}