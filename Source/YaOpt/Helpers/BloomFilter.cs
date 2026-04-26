using System.Runtime.CompilerServices;

namespace YaOpt.Helpers
{
	public struct BloomFilter
	{
		private ulong _bits;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(int hash)
		{
			_bits |= 1ul << hash;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(object objToHashed)
		{
			Set(objToHashed.GetHashCode());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Get(int hash)
		{
			return (_bits & (1ul << hash)) != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Get(object objToHashed)
		{
			return Get(objToHashed.GetHashCode());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset()
		{
			_bits = 0;
		}
	}
}