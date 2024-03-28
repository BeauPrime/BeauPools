/*
 * Copyright (C) 2024. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    26 March 2024
 * 
 * File:    PooledPrefabInstance.cs
 * Purpose: Prefab instance data.
 */

using System;
using UnityEngine;

namespace BeauPools {
    [AddComponentMenu(""), DisallowMultipleComponent]
    internal sealed class PooledPrefabInstance : MonoBehaviour {
        [SerializeField] internal Component[] ConstructHandlers;
        [SerializeField] internal Component[] AllocHandlers;
        [NonSerialized] internal bool HandlersInitialized;

        [NonSerialized] internal Component OriginalPrefab;
        [NonSerialized] internal IPool SourcePool;
        [NonSerialized] internal Component PoolToken;

        internal void Initialize(Component inOriginal, Component inComponent, IPool inPool) {
            OriginalPrefab = inOriginal;
            PoolToken = inComponent;
            SourcePool = inPool;
            hideFlags |= HideFlags.NotEditable;
        }
    }
}