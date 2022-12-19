/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    DynamicPool.cs
 * Purpose: Pool with variable size. 
 */

#if !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)
#define VERIFY_POOLS
#endif // !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)

using System.Collections.Generic;

namespace BeauPools
{
    /// <summary>
    /// Pool with an expanding capacity.
    /// </summary>
    public class DynamicPool<T> : IPool<T> where T : class
    {
        protected PoolConfig<T> m_Config;

        private List<T> m_Elements;
        private int m_UseCount;

        public DynamicPool(int inInitialCapacity, Constructor<T> inConstructor, bool inbStrictTyping = true)
        {
            Pool.VerifySize(inInitialCapacity);

            m_Config = new PoolConfig<T>(inConstructor, inbStrictTyping);
            m_Elements = new List<T>(inInitialCapacity);
            m_UseCount = 0;
        }

        #region IPool

        public int Capacity
        {
            get { return m_Elements.Capacity; }
        }

        public int Count
        {
            get { return m_Elements.Count; }
        }

        public int InUse
        {
            get { return m_UseCount; }
        }

        public void Prewarm(int inCount)
        {
            while (m_Elements.Count < inCount)
            {
                m_Elements.Add(m_Config.Construct(this));
            }
        }

        public void Shrink(int inCount)
        {
            for (int i = m_Elements.Count - 1; i >= inCount; --i)
            {
                m_Config.Destroy(this, m_Elements[i]);
                m_Elements.RemoveAt(i);
            }
        }

        public void Clear()
        {
            Shrink(0);
        }

        public void Dispose()
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
            T element = InternalAlloc();
            m_Config.OnAlloc(this, element);
            return element;
        }

        public virtual void Free(T inElement)
        {
            Pool.VerifyObject(this, inElement);
            Pool.VerifyNotInPool(m_Elements, inElement);

            --m_UseCount;
            Pool.VerifyBalance(m_UseCount);

            m_Elements.Add(inElement);

            m_Config.OnFree(this, inElement);
        }

        public bool TryAlloc(out T outElement)
        {
            outElement = Alloc();
            return true;
        }

        protected T InternalAlloc()
        {
            ++m_UseCount;

            T element;
            int elementIndex = m_Elements.Count - 1;
            if (elementIndex >= 0)
            {
                element = m_Elements[elementIndex];
                m_Elements.RemoveAt(elementIndex);
            }
            else
            {
                element = m_Config.Construct(this);
            }

            return element;
        }

        #endregion // IPool<T>
    }
}