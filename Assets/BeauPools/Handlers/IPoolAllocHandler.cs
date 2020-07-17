/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    17 July 2020
 * 
 * File:    IPoolAllocHandler.cs
 * Purpose: Interface for an alloc/free event handler.
 */

namespace BeauPools
{
    /// <summary>
    /// Pool event listener. Receives alloc/free pool events.
    /// </summary>
    public interface IPoolAllocHandler
    {
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