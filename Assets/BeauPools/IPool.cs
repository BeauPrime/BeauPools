/*
 * Copyright (C) 2018 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    IPool.cs
 * Purpose: Base interfaces for pools.
 */

using System;

namespace BeauPools
{
    /// <summary>
    /// Non-generic pool.
    /// </summary>
    public interface IPool : IDisposable
    {
        /// <summary>
        /// Current pool capacity.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Current pool size.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Number of items in use.
        /// </summary>
        int InUse { get; }

        /// <summary>
        /// Ensures pool size is not less than the given count.
        /// </summary>
        void Prewarm(int inCount);

        /// <summary>
        /// Ensures pool size is not greater than the given count.
        /// </summary>
        void Shrink(int inCount);

        /// <summary>
        /// Clears the pool.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Generic pool.
    /// </summary>
    public interface IPool<T> : IPool where T : class
    {
        /// <summary>
        /// Pool event configuration.
        /// </summary>
        PoolConfig<T> Config { get; }

        /// <summary>
        /// Allocates an element from the pool.
        /// </summary>
        T Alloc();

        /// <summary>
        /// Attempts to allocate an element from the pool.
        /// </summary>
        bool TryAlloc(out T outElement);

        /// <summary>
        /// Frees an element back into the pool.
        /// </summary>
        void Free(T inElement);
    }
}