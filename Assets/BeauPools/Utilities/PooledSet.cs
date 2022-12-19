/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    PooledSet.cs
 * Purpose: Pooled set of items. Can be reused to avoid garbage generation.
 */

using System;
using System.Collections.Generic;

namespace BeauPools
{
    /// <summary>
    /// Pooled version of a Set.
    /// </summary>
    public class PooledSet<T> : HashSet<T>, IDisposable
    {
        private void Reset()
        {
            Clear();
        }

        /// <summary>
        /// Resets and recycles the PooledList to the pool.
        /// </summary>
        public void Dispose()
        {
            Reset();
            s_ObjectPool.Free(this);
        }

        #region Pool

        // Initial capacity
        private const int POOL_SIZE = 8;

        // Object pool to hold available PooledSet.
        static private IPool<PooledSet<T>> s_ObjectPool;

        static PooledSet()
        {
            s_ObjectPool = new DynamicPool<PooledSet<T>>(POOL_SIZE, (p) => new PooledSet<T>());
        }

        /// <summary>
        /// Retrieves a PooledList for use.
        /// </summary>
        static public PooledSet<T> Create()
        {
            return s_ObjectPool.Alloc();
        }

        /// <summary>
        /// Retrieves a PooledSet for use, copying the contents
        /// of the given IEnumerable.
        /// </summary>
        static public PooledSet<T> Create(IEnumerable<T> inToCopy)
        {
            PooledSet<T> set = s_ObjectPool.Alloc();
            foreach (var obj in inToCopy)
                set.Add(obj);
            return set;
        }

        #endregion
    }
}