using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

[NativeContainer]
public unsafe struct NGrid2D<T> : IDisposable where T : struct
{

    private void* buffer;
    private int length;

    internal Allocator allocator;

    private int2 scaler;

    private int2 dims;


#if ENABLE_UNITY_COLLECTIONS_CHECKS
    internal int m_MinIndex;
    internal int m_MaxIndex;
    internal AtomicSafetyHandle m_Safety;
    internal DisposeSentinel m_DisposeSentinel;
#endif

    public NGrid2D(int2 dims, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
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
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);
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

    private void FailOutOfRangeError(int index)
    {
        throw new IndexOutOfRangeException("Index " + index + " out of range " + length);
    }

    public int2 GetPosFromIndex(int index)
    {
        return new int2(index % dims.x, index / dims.x);
    }



    public void Dispose()
    {
        UnsafeUtility.Free(buffer, allocator);
    }

    public int2 Dimensions { get { return dims; } }

    public int Width { get { return dims.x; } }
    public int Height { get { return dims.y; } }

    public bool IsCreated { get { return buffer != null; } }
}
