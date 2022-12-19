/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    PooledStringBuilder.cs
 * Purpose: Pooled StringBuilder. Can be reused to avoid garbage generation.
 */

using System;
using System.Text;

namespace BeauPools
{
    /// <summary>
    /// Pooled version of a StringBuilder.
    /// </summary>
    public class PooledStringBuilder : IDisposable
    {
        /// <summary>
        /// The internal StringBuilder object.
        /// </summary>
        public readonly StringBuilder Builder = new StringBuilder(256);

        private void Reset()
        {
            Builder.Length = 0;
            Builder.EnsureCapacity(256);
        }

        /// <summary>
        /// Resets and recycles the builder to the pool.
        /// </summary>
        public void Dispose()
        {
            Reset();
            s_ObjectPool.Free(this);
        }

        public override string ToString()
        {
            return Builder.ToString();
        }

        static public implicit operator StringBuilder(PooledStringBuilder inPooled)
        {
            return inPooled.Builder;
        }

        #region Pool

        // Initial capacity
        private const int POOL_SIZE = 8;

        // Object pool to hold available StringBuilders.
        static private IPool<PooledStringBuilder> s_ObjectPool;

        static PooledStringBuilder()
        {
            s_ObjectPool = new DynamicPool<PooledStringBuilder>(POOL_SIZE, (p) => new PooledStringBuilder());
        }

        /// <summary>
        /// Retrieves a PooledStringBuilder for use.
        /// </summary>
        static public PooledStringBuilder Create()
        {
            return s_ObjectPool.Alloc();
        }

        /// <summary>
        /// Retrieves a PooledStringBuilder to use.
        /// </summary>
        static public PooledStringBuilder Create(string inSource)
        {
            PooledStringBuilder builder = s_ObjectPool.Alloc();
            builder.Builder.Append(inSource);
            return builder;
        }

        #endregion
    }
}