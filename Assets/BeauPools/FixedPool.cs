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

using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace BeauPools {
    /// <summary>
    /// Pool with a fixed capacity.
    /// Cannot allocate items when empty.
    /// </summary>
    public class FixedPool<T> : IPool<T> where T : class {
        private PoolConfig<T> m_Config;

        private T[] m_Elements;
        private int m_Count;
        private int m_UseCount;

        public FixedPool(int inSize, Constructor<T> inConstructor, bool inbStrictTyping = true) {
            Pool.VerifySize(inSize);

            m_Config = new PoolConfig<T>(inConstructor, inbStrictTyping);
            m_Elements = new T[inSize];
            m_Count = 0;
            m_UseCount = 0;
        }

        #region IPool

        public int Capacity {
            get { return m_Elements.Length; }
        }

        public int Count {
            get { return m_Count; }
        }

        public int InUse {
            get { return m_UseCount; }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public void Prewarm(int inCount) {
#if VERIFY_POOLS
            if (inCount + m_Count + m_UseCount > m_Elements.Length) {
                throw new ArgumentOutOfRangeException("inCount");
            }
#endif // VERIFY_POOLS
            while (m_Count < inCount) {
                m_Elements[m_Count++] = m_Config.Construct(this);
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public void Shrink(int inCount) {
#if VERIFY_POOLS
            if (inCount < 0) {
                throw new ArgumentOutOfRangeException("inCount");
            }
#endif // VERIFY_POOLS
            for (int i = m_Count - 1; i >= inCount; i--) {
                m_Config.Destroy(this, m_Elements[i]);
                m_Count--;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Shrink(0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            Clear();
            m_Elements = null;
        }

        #endregion // IPool

        #region IPool<T>

        public PoolConfig<T> Config {
            get { return m_Config; }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public virtual T Alloc() {
            Pool.VerifyCount(m_Count);

            m_UseCount++;
            m_Count--;
            T element = m_Elements[m_Count];
            m_Elements[m_Count] = null;
            m_Config.OnAlloc(this, element);
            return element;
        }

        void IPool.Free(object inElement) {
            Free((T) inElement);
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public virtual void Free(T inElement) {
            Pool.VerifyObject(this, inElement);
            Pool.VerifyNotInPool(m_Elements, inElement, m_Count);

            m_UseCount--;
            Pool.VerifyBalance(m_UseCount);

            bool freed = false;

            if (m_Count < m_Elements.Length) {
                m_Elements[m_Count++] = inElement;
                freed = true;
            }

            m_Config.OnFree(this, inElement);

            if (!freed) {
                m_Config.Destroy(this, inElement);
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public bool TryAlloc(out T outElement) {
            if (m_Count <= 0) {
                outElement = null;
                return false;
            }

            m_UseCount++;
            m_Count--;
            outElement = m_Elements[m_Count];
            m_Elements[m_Count] = null;
            m_Config.OnAlloc(this, outElement);
            return true;
        }

        #endregion // IPool<T>
    }
}