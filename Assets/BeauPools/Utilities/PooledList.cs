/*
 * Copyright (C) 2018 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    PooledList.cs
 * Purpose: Pooled list of items. Can be reused to avoid garbage generation.
 */

using System;
using System.Collections.Generic;

namespace BeauPools
{
    /// <summary>
    /// Pooled version of a List.
    /// </summary>
    public class PooledList<T> : List<T>, IDisposable
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

        // Object pool to hold available PooledList.
        static private IPool<PooledList<T>> s_ObjectPool;

        static PooledList()
        {
            s_ObjectPool = new DynamicPool<PooledList<T>>(POOL_SIZE, Pool.DefaultConstructor<PooledList<T>>());
        }

        /// <summary>
        /// Retrieves a PooledList for use.
        /// </summary>
        static public PooledList<T> Create()
        {
            return s_ObjectPool.Alloc();
        }

        /// <summary>
        /// Retrieves a PooledList for use, copying the contents
        /// of the given IEnumerable.
        /// </summary>
        static public PooledList<T> Create(IEnumerable<T> inToCopy)
        {
            PooledList<T> list = s_ObjectPool.Alloc();
            list.AddRange(inToCopy);
            return list;
        }

        #endregion
    }
}