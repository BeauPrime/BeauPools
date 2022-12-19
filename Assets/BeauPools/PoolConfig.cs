/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    PoolConfig.cs
 * Purpose: Callbacks and configurations for a pool. 
 */

#if !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)
#define VERIFY_POOLS
#endif // !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)

using System;

namespace BeauPools
{
    /// <summary>
    /// Generic object construction function.
    /// </summary>
    public delegate T Constructor<T>(IPool<T> inPool) where T : class;

    /// <summary>
    /// Delegate for alloc/free/dispose events.
    /// </summary>
    public delegate void LifecycleEvent<T>(IPool<T> inPool, T inElement) where T : class;

    /// <summary>
    /// Pool event configuration.
    /// </summary>
    public sealed class PoolConfig<T> where T : class
    {
        private Constructor<T> m_Constructor;

        private LifecycleEvent<T> m_OnConstruct;
        private LifecycleEvent<T> m_OnDestruct;

        private LifecycleEvent<T> m_OnAlloc;
        private LifecycleEvent<T> m_OnFree;

        internal readonly bool StrictTyping;

        internal PoolConfig(Constructor<T> inConstructor, bool inbStrictTyping)
        {
            StrictTyping = inbStrictTyping;

            if (StrictTyping)
                Pool.VerifyType<T>();
            Pool.VerifyConstructor(inConstructor);

            m_Constructor = inConstructor;

            Type type = typeof(T);
            if (typeof(IPooledObject<T>).IsAssignableFrom(type))
            {
                m_OnConstruct += PooledObjectOnConstruct;
                m_OnDestruct += PooledObjectOnDestruct;
                m_OnAlloc += PooledObjectOnAlloc;
                m_OnFree += PooledObjectOnFree;
            }
            if (typeof(IPoolConstructHandler).IsAssignableFrom(type))
            {
                m_OnConstruct += PooledConstructHandlerOnConstruct;
                m_OnDestruct += PooledConstructHandlerOnDestruct;
            }
            if (typeof(IPoolAllocHandler).IsAssignableFrom(type))
            {
                m_OnAlloc += PooledAllocHandlerOnAlloc;
                m_OnFree += PooledAllocHandlerOnFree;
            }
        }

        #region Callback Registration

        public void RegisterOnConstruct(LifecycleEvent<T> inEvent)
        {
            m_OnConstruct += inEvent;
        }

        public void RegisterOnDestruct(LifecycleEvent<T> inEvent)
        {
            m_OnDestruct += inEvent;
        }

        public void RegisterOnAlloc(LifecycleEvent<T> inEvent)
        {
            m_OnAlloc += inEvent;
        }

        public void RegisterOnFree(LifecycleEvent<T> inEvent)
        {
            m_OnFree += inEvent;
        }

        #endregion // Callback Registration

        #region Callback Execution

        internal T Construct(IPool<T> inPool)
        {
            T obj = m_Constructor(inPool);

            if (StrictTyping)
                Pool.VerifyObject(inPool, obj);

            if (m_OnConstruct != null)
                m_OnConstruct(inPool, obj);

            return obj;
        }

        internal void Destroy(IPool<T> inPool, T inElement)
        {
            if (m_OnDestruct != null)
                m_OnDestruct(inPool, inElement);
        }

        internal void OnAlloc(IPool<T> inPool, T inElement)
        {
            if (m_OnAlloc != null)
                m_OnAlloc(inPool, inElement);
        }

        internal void OnFree(IPool<T> inPool, T inElement)
        {
            if (m_OnFree != null)
                m_OnFree(inPool, inElement);
        }

        #endregion // Callback Execution

        #region Casted callbacks

        static private void PooledObjectOnConstruct(IPool<T> inPool, T inElement)
        {
            ((IPooledObject<T>) inElement).OnConstruct(inPool);
        }

        static private void PooledObjectOnDestruct(IPool<T> inPool, T inElement)
        {
            ((IPooledObject<T>) inElement).OnDestruct();
        }

        static private void PooledObjectOnAlloc(IPool<T> inPool, T inElement)
        {
            ((IPooledObject<T>) inElement).OnAlloc();
        }

        static private void PooledObjectOnFree(IPool<T> inPool, T inElement)
        {
            ((IPooledObject<T>) inElement).OnFree();
        }

        static private void PooledConstructHandlerOnConstruct(IPool<T> inPool, T inElement)
        {
            ((IPoolConstructHandler) inElement).OnConstruct();
        }

        static private void PooledConstructHandlerOnDestruct(IPool<T> inPool, T inElement)
        {
            ((IPoolConstructHandler) inElement).OnDestruct();
        }

        static private void PooledAllocHandlerOnAlloc(IPool<T> inPool, T inElement)
        {
            ((IPoolAllocHandler) inElement).OnAlloc();
        }

        static private void PooledAllocHandlerOnFree(IPool<T> inPool, T inElement)
        {
            ((IPoolAllocHandler) inElement).OnFree();
        }

        #endregion // Casted callbacks
    }
}