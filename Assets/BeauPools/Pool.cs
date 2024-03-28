/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    Pool.cs
 * Purpose: Extension methods for dealing with pools. 
 */

#if !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)
#define VERIFY_POOLS
#endif // !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace BeauPools {
    /// <summary>
    /// Contains pool extension methods.
    /// </summary>
    static public class Pool {
        /// <summary>
        /// Prefills the pool to capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Prewarm(this IPool inThis) {
            inThis.Prewarm(inThis.Capacity);
        }

        /// <summary>
        /// Returns the empty constructor for the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Constructor<T> DefaultConstructor<T>() where T : class, new() {
            return (p) => { return new T(); };
        }

        #region TempAlloc

        /// <summary>
        /// Returns a temporary allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public TempAlloc<T> TempAlloc<T>(this IPool<T> inThis) where T : class {
            T obj = inThis.Alloc();
            return new TempAlloc<T>(inThis, obj);
        }

        #endregion // TempAlloc

        #region Collections

        #region Alloc

        /// <summary>
        /// Allocates enough elements to fill the given array.
        /// Returns the total number of allocated elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int Alloc<T>(this IPool<T> inThis, T[] inDest) where T : class {
            return Alloc<T>(inThis, inDest, 0, inDest.Length);
        }

        /// <summary>
        /// Allocates enough elements to fill the given array, starting from the given index.
        /// Returns the total number of allocated elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int Alloc<T>(this IPool<T> inThis, T[] inDest, int inStartIndex) where T : class {
            return Alloc<T>(inThis, inDest, inStartIndex, inDest.Length - inStartIndex);
        }

        /// <summary>
        /// Allocates the given number of elements and writes to the array.
        /// Returns the total number of allocated elements.
        /// </summary>
        static public int Alloc<T>(this IPool<T> inThis, T[] inDest, int inStartIndex, int inCount) where T : class {
            int allocCount = 0;
            for (int i = 0; i < inCount; ++i) {
                int idx = inStartIndex + i;
                if (inDest[idx] == null) {
                    inDest[idx] = inThis.Alloc();
                    ++allocCount;
                }
            }
            return allocCount;
        }

        /// <summary>
        /// Allocates the given number of elements and adds to the collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Alloc<T>(this IPool<T> inThis, ICollection<T> inDest, int inCount) where T : class {
            for (int i = 0; i < inCount; ++i)
                inDest.Add(inThis.Alloc());
        }

        #endregion // Alloc

        #region TryAlloc

        /// <summary>
        /// Attempts to allocate enough elements to fill the given array.
        /// Returns the total number of allocated elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int TryAlloc<T>(this IPool<T> inThis, T[] inDest) where T : class {
            return TryAlloc<T>(inThis, inDest, 0, inDest.Length);
        }

        /// <summary>
        /// Attempts to allocate enough elements to fill the given array, starting from the given index.
        /// Returns the total number of allocated elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int TryAlloc<T>(this IPool<T> inThis, T[] inDest, int inStartIndex) where T : class {
            return TryAlloc<T>(inThis, inDest, inStartIndex, inDest.Length - inStartIndex);
        }

        /// <summary>
        /// Attempts to allocate the given number of elements and writes to the array.
        /// Returns the total number of allocated elements.
        /// </summary>
        static public int TryAlloc<T>(this IPool<T> inThis, T[] inDest, int inStartIndex, int inCount) where T : class {
            int allocCount = 0;
            T element;
            for (int i = 0; i < inCount; ++i) {
                int idx = inStartIndex + i;
                if (inDest[idx] != null)
                    continue;
                if (!inThis.TryAlloc(out element))
                    break;
                inDest[idx] = element;
                ++allocCount;
            }

            return allocCount;
        }

        /// <summary>
        /// Attempts to allocate the given number of elements and adds to the collection.
        /// Returns the total number of allocated elements.
        /// </summary>
        static public int TryAlloc<T>(this IPool<T> inThis, ICollection<T> inDest, int inCount) where T : class {
            T element;
            for (int i = 0; i < inCount; ++i) {
                if (!inThis.TryAlloc(out element))
                    return i;
                inDest.Add(element);
            }

            return inCount;
        }

        #endregion // TryAlloc

        #region Free

        /// <summary>
        /// Frees all elements from the given array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Free<T>(this IPool<T> inThis, T[] inSrc) where T : class {
            Free<T>(inThis, inSrc, 0, inSrc.Length);
        }

        /// <summary>
        /// Frees elements from the given array, starting at the given index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Free<T>(this IPool<T> inThis, T[] inSrc, int inStartIndex) where T : class {
            Free<T>(inThis, inSrc, inStartIndex, inSrc.Length - inStartIndex);
        }

        /// <summary>
        /// Frees a subsection of the given array.
        /// </summary>
        static public void Free<T>(this IPool<T> inThis, T[] inSrc, int inStartIndex, int inCount) where T : class {
            for (int i = 0; i < inCount; ++i) {
                int idx = inStartIndex + i;
                T element = inSrc[idx];
                if (element != null) {
                    inThis.Free(element);
                    inSrc[idx] = default(T);
                }
            }
        }

        /// <summary>
        /// Frees all elements of the given collection.
        /// </summary>
        static public void Free<T>(this IPool<T> inThis, ICollection<T> inSrc) where T : class {
            foreach (var element in inSrc)
                inThis.Free(element);

            inSrc.Clear();
        }

        /// <summary>
        /// Frees all elements of the given list.
        /// </summary>
        static public void Free<T>(this IPool<T> inThis, List<T> inSrc) where T : class {
            foreach (var element in inSrc)
                inThis.Free(element);

            inSrc.Clear();
        }

        #endregion // Free

        #endregion // Collections

        #region Asserts

        [Conditional("VERIFY_POOLS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void VerifyType<T>() {
#if VERIFY_POOLS
            Type type = typeof(T);
            if (type.IsAbstract || type.IsInterface)
                throw new ArgumentException("Cannot create a strictly-typed generic pool with an abstract class or interface");
#endif // VERIFY_POOLS
        }

        [Conditional("VERIFY_POOLS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void VerifyObject<T>(IPool<T> inPool, T inElement) where T : class {
#if VERIFY_POOLS
            if (inElement == null)
                throw new ArgumentNullException("inElement", "Provided object was null");

            if (inPool.Config.StrictTyping && inElement.GetType() != typeof(T))
                throw new ArgumentException("Expected type " + typeof(T).FullName + ", got " + inElement.GetType().FullName, "inElement");
#endif // VERIFY_POOLS
        }

        [Conditional("VERIFY_POOLS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void VerifySize(int inSize) {
#if VERIFY_POOLS
            if (inSize <= 0)
                throw new ArgumentOutOfRangeException("inSize", "Pool capacity must not be less than 1");
#endif // VERIFY_POOLS
        }

        [Conditional("VERIFY_POOLS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void VerifyCount(int inCount) {
#if VERIFY_POOLS
            if (inCount <= 0)
                throw new InvalidOperationException("Pool is empty");
#endif // VERIFY_POOLS
        }

        [Conditional("VERIFY_POOLS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void VerifyBalance(int inBalance) {
#if VERIFY_POOLS
            if (inBalance < 0)
                throw new InvalidOperationException("Mismatched alloc/free calls");
#endif // VERIFY_POOLS
        }

        [Conditional("VERIFY_POOLS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal void VerifyConstructor<T>(Constructor<T> inConstructor) where T : class {
#if VERIFY_POOLS
            if (inConstructor == null)
                throw new ArgumentNullException("inConstructor", "Cannot provide a null constructor");
#endif // VERIFY_POOLS
        }

        [Conditional("VERIFY_POOLS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static internal void VerifyNotInPool<T>(T[] inPool, T inElement, int inCount) {
#if VERIFY_POOLS
            for(int i = 0; i < inCount; i++) {
                if (ReferenceEquals(inPool[i], inElement)) {
                    throw new ArgumentException("Element has already been freed", "inElement");
                }
            }
#endif // VERIFY_POOLS
        }

        [Conditional("VERIFY_POOLS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static internal void VerifyNotInPool<T>(List<T> inPool, T inElement) {
#if VERIFY_POOLS
            for(int i = 0, count = inPool.Count; i < count; i++) {
                if (ReferenceEquals(inPool[i], inElement)) {
                    throw new ArgumentException("Element has already been freed", "inElement");
                }
            }
#endif // VERIFY_POOLS
        }

        #endregion // Asserts

        #region Configuration

        /// <summary>
        /// Enables using IDisposable.Dispose when an element is disposed/destroyed.
        /// </summary>
        static public void UseIDisposable<T>(this IPool<T> inPool) where T : class, IDisposable {
            inPool.Config.RegisterOnDestruct(IDisposableOnDispose<T>);
        }

        static private void IDisposableOnDispose<T>(IPool<T> inPool, T inElement) where T : class, IDisposable {
            inElement.Dispose();
        }

        #endregion // Configuration

        #region Prefab

        /// <summary>
        /// Attempts to free the given prefab instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryFree(Component inPrefab) {
            PooledPrefabInstance prefabInstance = inPrefab.GetComponent<PooledPrefabInstance>();
            if (prefabInstance != null) {
                prefabInstance.SourcePool.Free(prefabInstance.PoolToken);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to free the given prefab instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool TryFree(GameObject inPrefab) {
            PooledPrefabInstance prefabInstance = inPrefab.GetComponent<PooledPrefabInstance>();
            if (prefabInstance != null) {
                prefabInstance.SourcePool.Free(prefabInstance.PoolToken);
                return true;
            }

            return false;
        }

        #endregion // Prefab
    }
}