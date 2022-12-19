/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    DynamicPool.cs
 * Purpose: Pool with fixed size. 
 */

#if !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)
#define VERIFY_POOLS
#endif // !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)

namespace BeauPools
{
    /// <summary>
    /// Pool with a fixed capacity.
    /// Cannot allocate items when empty.
    /// </summary>
    public class FixedPool<T> : IPool<T> where T : class
    {
        protected PoolConfig<T> m_Config;

        private T[] m_Elements;
        private int m_Count;
        private int m_UseCount;

        public FixedPool(int inSize, Constructor<T> inConstructor, bool inbStrictTyping = true)
        {
            Pool.VerifySize(inSize);

            m_Config = new PoolConfig<T>(inConstructor, inbStrictTyping);
            m_Elements = new T[inSize];
            m_Count = 0;
            m_UseCount = 0;
        }

        #region IPool

        public int Capacity
        {
            get { return m_Elements.Length; }
        }

        public int Count
        {
            get { return m_Count; }
        }

        public int InUse
        {
            get { return m_UseCount; }
        }

        public virtual void Prewarm(int inCount)
        {
            while (m_Count < inCount)
            {
                m_Elements[m_Count++] = m_Config.Construct(this);
            }
        }

        public virtual void Shrink(int inCount)
        {
            for (int i = m_Count - 1; i >= inCount; --i)
            {
                m_Config.Destroy(this, m_Elements[i]);
                --m_Count;
            }
        }

        public virtual void Clear()
        {
            Shrink(0);
        }

        public virtual void Dispose()
        {
            Clear();
            m_Elements = null;
        }

        #endregion // IPool

        #region IPool<T>

        public PoolConfig<T> Config
        {
            get { return m_Config; }
        }

        public virtual T Alloc()
        {
            Pool.VerifyCount(m_Count);

            ++m_UseCount;
            --m_Count;
            T element = m_Elements[m_Count];
            m_Elements[m_Count] = null;
            m_Config.OnAlloc(this, element);
            return element;
        }

        public virtual void Free(T inElement)
        {
            Pool.VerifyObject(this, inElement);
            Pool.VerifyNotInPool(m_Elements, inElement);

            --m_UseCount;
            Pool.VerifyBalance(m_UseCount);

            if (m_Count < m_Elements.Length)
                m_Elements[m_Count++] = inElement;

            m_Config.OnFree(this, inElement);
        }

        public virtual bool TryAlloc(out T outElement)
        {
            if (m_Count <= 0)
            {
                outElement = null;
                return false;
            }

            ++m_UseCount;
            --m_Count;
            outElement = m_Elements[m_Count];
            m_Elements[m_Count] = null;
            m_Config.OnAlloc(this, outElement);
            return true;
        }

        #endregion // IPool<T>
    }
}