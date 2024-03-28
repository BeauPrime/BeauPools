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

namespace BeauPools {
    /// <summary>
    /// Pooled version of a StringBuilder.
    /// </summary>
    public sealed class PooledStringBuilder : IDisposable {
        /// <summary>
        /// The internal StringBuilder object.
        /// </summary>
        public readonly StringBuilder Builder;

        private readonly IPool<PooledStringBuilder> m_Pool;
        private readonly int m_MinCapacity;

        internal PooledStringBuilder(IPool<PooledStringBuilder> inPool, int inCapacity) {
            Builder = new StringBuilder(inCapacity);
            m_Pool = inPool;
            m_MinCapacity = inCapacity;
        }

        private void Reset() {
            Builder.Length = 0;
            Builder.Capacity = m_MinCapacity;
        }

        /// <summary>
        /// Resets and recycles the builder to the pool.
        /// </summary>
        public void Dispose() {
            Reset();
            if (m_Pool != null) {
                m_Pool.Free(this);
            }
        }

        public override string ToString() {
            return Builder.ToString();
        }

        static public implicit operator StringBuilder(PooledStringBuilder inPooled) {
            return inPooled.Builder;
        }

        #region Pool

        // Initial capacity
        private const int PoolSize = 4;
        private const int SmallSize = 256;
        private const int LargeSize = 4096;
        private const int SkipPoolSize = LargeSize * 2;

        // Object pools to hold available StringBuilders.
        static private IPool<PooledStringBuilder> s_SmallObjectPool;
        static private IPool<PooledStringBuilder> s_LargeObjectPool;

        static PooledStringBuilder() {
            s_SmallObjectPool = new DynamicPool<PooledStringBuilder>(PoolSize, (p) => new PooledStringBuilder(p, SmallSize));
            s_LargeObjectPool = new DynamicPool<PooledStringBuilder>(2, (p) => new PooledStringBuilder(p, LargeSize));
        }

        /// <summary>
        /// Retrieves a PooledStringBuilder for use.
        /// This will have an initial capacity of 256 characters.
        /// </summary>
        static public PooledStringBuilder Create() {
            return s_SmallObjectPool.Alloc();
        }

        /// <summary>
        /// Retrieves a large PooledStringBuilder for use.
        /// This will have an initial capacity of 4096 characters
        /// </summary>
        static public PooledStringBuilder CreateLarge() {
            return s_LargeObjectPool.Alloc();
        }

        /// <summary>
        /// Retrieves a PooledStringBuilder for use.
        /// </summary>
        static public PooledStringBuilder Create(int inDesiredCapacity) {
            if (inDesiredCapacity > SkipPoolSize) {
                return new PooledStringBuilder(null, inDesiredCapacity);
            } else if (inDesiredCapacity > SmallSize) {
                return s_LargeObjectPool.Alloc();
            } else {
                return s_SmallObjectPool.Alloc();
            }
        }

        /// <summary>
        /// Retrieves a PooledStringBuilder to use.
        /// This will have a capacity guaranteed to be greater than the given string's length.
        /// </summary>
        static public PooledStringBuilder Create(string inSource) {
            PooledStringBuilder psb = inSource == null ? s_SmallObjectPool.Alloc() : Create(inSource.Length * 6 / 5);
            psb.Builder.Append(inSource);
            return psb;
        }

        /// <summary>
        /// Prewarms the internal pools.
        /// </summary>
        static public void Prewarm() {
            s_SmallObjectPool.Prewarm();
            s_LargeObjectPool.Prewarm();
        }

        #endregion
    }
}