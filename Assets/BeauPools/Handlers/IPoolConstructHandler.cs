/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    17 July 2020
 * 
 * File:    IPoolConstructHandler.cs
 * Purpose: Interface for a construct/destruct event handler.
 */

namespace BeauPools
{
    /// <summary>
    /// Pool event listener. Receives construct/destruct pool events.
    /// </summary>
    public interface IPoolConstructHandler
    {
        /// <summary>
        /// Called when the object is constructed for the pool.
        /// </summary>
        void OnConstruct();

        /// <summary>
        /// Called when the object is destroyed from the pool.
        /// </summary>
        void OnDestruct();
    }
}