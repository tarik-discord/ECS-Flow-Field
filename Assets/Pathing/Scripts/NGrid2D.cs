using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;


namespace Shooter.Utility
{
    // Needed to mark as a native container.
    //[NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    // Ensure our memory layout is the same as the order of our variables.
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NGrid2D<T> : IDisposable where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        private void* buffer;
        private int length;

        internal Allocator allocator;

        private int2 scaler;

        private int2 dims;


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] internal DisposeSentinel m_DisposeSentinel;
        static int s_staticSafetyId;
        [BurstDiscard]
        static void AssignStaticSafetyId(ref AtomicSafetyHandle safetyHandle)
        {
            // static safety IDs are unique per-type, and should only be initialized the first time an instance of
            // the type is created.
            if (s_staticSafetyId == 0)
            {
                s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NGrid2D<T>>();

                // Each static safety ID can optionally provide custom error messages for each AtomicSafetyErrorType.
                // This is rarely necessary, but can be useful if higher-level code can provide more helpful error guidance
                // than the default error message.
                // See the documentation for AtomicSafetyHandle.SetCustomErrorMessage() for details on the message format.
                //   AtomicSafetyHandle.SetCustomErrorMessage(s_staticSafetyId, AtomicSafetyErrorType.DeallocatedFromJob,
                // "The {5} has been deallocated before being passed into a job. For NativeCustomArrays, this usually means <type-specific guidance here>");
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref safetyHandle, s_staticSafetyId);
        }

#endif

        public NGrid2D(int2 dims, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Native allocation is only valid for Temp, Job and Persistent
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", "allocator");
            if (math.any(dims < 0))
                throw new ArgumentOutOfRangeException("length", "Length must be >= 0");
            if (!UnsafeUtility.IsBlittable<T>())
                throw new ArgumentException(string.Format("{0} used in NativeCustomArray<{0}> must be blittable", typeof(T)));
#endif

            this.dims = dims;
            this.scaler = new int2(1, dims.x);
            this.allocator = allocator;
            this.length = dims.x * dims.y;
            long bufferSize = UnsafeUtility.SizeOf<T>() * length;
            buffer = UnsafeUtility.Malloc(bufferSize, UnsafeUtility.AlignOf<T>(), allocator);
            if (options == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(buffer, bufferSize);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_MinIndex = 0;
            m_MaxIndex = length - 1;
            m_Safety = AtomicSafetyHandle.Create();
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, allocator);
            AssignStaticSafetyId(ref m_Safety);
#endif
        }

        public T this[int x, int y]
        {
            get
            {
                return this[new int2(x, y)];
            }

            set
            {
                this[new int2(x, y)] = value;
            }
        }

        public T this[int2 pos]
        {
            get
            {
                int index = math.dot(pos, scaler);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                if (index < m_MinIndex || index > m_MaxIndex)
                    FailOutOfRangeError(index);
#endif
                return UnsafeUtility.ReadArrayElement<T>(buffer, index);
            }

            set
            {
                int index = math.dot(pos, scaler);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                if (index < m_MinIndex || index > m_MaxIndex)
                    FailOutOfRangeError(index);
#endif
                UnsafeUtility.WriteArrayElement<T>(buffer, index, value);
            }
        }


        public T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                if (index < m_MinIndex || index > m_MaxIndex)
                    FailOutOfRangeError(index);
#endif
                return UnsafeUtility.ReadArrayElement<T>(buffer, index);
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                if (index < m_MinIndex || index > m_MaxIndex)
                    FailOutOfRangeError(index);
#endif
                UnsafeUtility.WriteArrayElement<T>(buffer, index, value);
            }
        }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [BurstDiscard]
        private void FailOutOfRangeError(int index)
        {
            throw new IndexOutOfRangeException("Index " + index + " out of range " + length);
        }
#endif
        public int2 GetPosFromIndex(int index)
        {
            return new int2(index % dims.x, index / dims.x);
        }

        public int GetIndexFromPos(int2 index)
        {
            return math.dot(index, scaler);
        }

        [BurstDiscard]
        public T[] ToArray()
        {
            if (IsCreated)
            {
                T[] data = new T[length];
                for (int i = 0; i < length; i++)
                    data[i] = this[i];
                return data;
            }
            return null;
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsValidAllocator(allocator))
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            UnsafeUtility.Free(buffer, allocator);
        }

        public int2 Dimensions { get { return dims; } }

        public int Width { get { return dims.x; } }
        public int Height { get { return dims.y; } }

        public bool IsCreated { get { return buffer != null; } }
    }

    // Visualizes the custom array in the C# debugger
    internal sealed class NGrid2DDebugView<T> where T : struct
    {
        private NGrid2D<T> m_Array;

        public NGrid2DDebugView(NGrid2D<T> array)
        {
            m_Array = array;
        }

        public T[] Items
        {
            get { return m_Array.ToArray(); }
        }
    }
}

