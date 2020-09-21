/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    18 Sept 2019
 * 
 * File:    TempAlloc.cs
 * Purpose: Temporarily allocated pooled object.
 */

using System;
using System.Collections.Generic;

namespace BeauPools
{
    /// <summary>
    /// Temporary pool allocation
    /// </summary>
    public class TempAlloc<T> : IDisposable where T : class
    {
        private T m_Obj;
        private IPool<T> m_Source;

        internal TempAlloc(IPool<T> inPool, T inAllocatedObject)
        {
            if (inPool == null)
                throw new ArgumentNullException("inPool");
            if (inAllocatedObject == null)
                throw new ArgumentNullException("inAllocatedObject");

            m_Source = inPool;
            m_Obj = inAllocatedObject;
        }

        /// <summary>
        /// Pooled object.
        /// </summary>
        public T Object { get { return m_Obj; } }

        static public implicit operator T(TempAlloc<T> inTempAlloc)
        {
            return inTempAlloc.Object;
        }

        /// <summary>
        /// Recycles the object.
        /// </summary>
        public void Dispose()
        {
            if (m_Source != null && m_Obj != null)
            {
                m_Source.Free(m_Obj);
                m_Obj = null;
                m_Source = null;
            }
        }
    }
}