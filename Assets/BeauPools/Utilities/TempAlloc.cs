/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    18 Sept 2019
 * 
 * File:    TempAlloc.cs
 * Purpose: Temporarily allocated pooled object.
 */

using System;
using System.Runtime.CompilerServices;

namespace BeauPools {
    /// <summary>
    /// Temporary pool allocation
    /// </summary>
    public struct TempAlloc<T> : IEquatable<TempAlloc<T>>, IEquatable<T>, IDisposable where T : class {
        private T m_Obj;
        private IPool<T> m_Source;

        internal TempAlloc(IPool<T> inPool, T inAllocatedObject) {
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
        public T Object {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Obj; }
        }

        /// <summary>
        /// If this TempAlloc contains an allocation.
        /// </summary>
        public bool IsAllocated {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return !object.ReferenceEquals(m_Obj, null); }
        }

        static public implicit operator T(TempAlloc<T> inTempAlloc) {
            return inTempAlloc.Object;
        }

        /// <summary>
        /// Frees the object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free() {
            if (!object.ReferenceEquals(m_Obj, null)) {
                m_Source.Free(m_Obj);
                m_Obj = null;
                m_Source = null;
            }
        }

        #region Interfaces and Overrides

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TempAlloc<T> other) {
            return object.ReferenceEquals(m_Obj, other.m_Obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T other) {
            return object.ReferenceEquals(m_Obj, other);
        }

        public override bool Equals(object obj) {
            if (obj is TempAlloc<T>)
                return Equals((TempAlloc<T>) obj);
            if (obj is T)
                return Equals((T) obj);
            return false;
        }

        /// <summary>
        /// Frees the object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
            Free();
        }

        public override int GetHashCode() {
            return object.ReferenceEquals(m_Obj, null) ? 0 : m_Obj.GetHashCode();
        }

        static public bool operator ==(TempAlloc<T> left, TempAlloc<T> right) {
            return left.Equals(right);
        }

        static public bool operator !=(TempAlloc<T> left, TempAlloc<T> right) {
            return !left.Equals(right);
        }

        static public bool operator ==(TempAlloc<T> left, T right) {
            return left.Equals(right);
        }

        static public bool operator !=(TempAlloc<T> left, T right) {
            return !left.Equals(right);
        }

        static public bool operator ==(TempAlloc<T> left, object right) {
            return object.ReferenceEquals(left.m_Obj, right);
        }

        static public bool operator !=(TempAlloc<T> left, object right) {
            return !object.ReferenceEquals(left.m_Obj, right);
        }

        #endregion // Interfaces and Overrides
    }
}