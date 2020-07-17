/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    10 July 2020
 * 
 * File:    IPrefabPool.cs
 * Purpose: Interface for a prefab pool.
 */

using UnityEngine;

namespace BeauPools
{
    /// <summary>
    /// Pool for a prefab.
    /// </summary>
    public interface IPrefabPool<T> : IPool<T> where T : Component
    {
        string Name { get; }
        T Prefab { get; }
        Transform PoolTransform { get; }
        Transform DefaultSpawnTransform { get; }

        T Alloc(Transform inParent);
        T Alloc(Vector3 inPosition, bool inbWorldSpace = false);
        T Alloc(Vector3 inPosition, Quaternion inOrientation, bool inbWorldSpace = false);
        T Alloc(Vector3 inPosition, Quaternion inOrientation, Transform inParent, bool inbWorldSpace = false);
    }
}