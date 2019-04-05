/*
 * Copyright (C) 2018-2019. Filament Games, LLC. All rights reserved.
 * Author:  Alex Beauchesne
 * Date:    5 April 2019
 * 
 * File:    IPooledObject.cs
 * Purpose: Interface for a pooled object with lifecycle callbacks. 
 */

namespace BeauPools
{
    /// <summary>
    /// Pooled object. Receives pool events.
    /// </summary>
    public interface IPooledObject<T> where T : class
    {
        /// <summary>
        /// Called when the object is constructed for a pool.
        /// </summary>
        void OnConstruct(IPool<T> inPool);

        /// <summary>
        /// Called when the object is destroyed while in the pool.
        /// </summary>
        void OnDestruct();
        
        /// <summary>
        /// Called when the object is allocated from the pool.
        /// </summary>
        void OnAlloc();

        /// <summary>
        /// Called when the object is returned to the pool.
        /// </summary>
        void OnFree();
    }
}