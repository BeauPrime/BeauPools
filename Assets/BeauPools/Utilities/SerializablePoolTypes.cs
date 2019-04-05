/*
 * Copyright (C) 2018-2019. Filament Games, LLC. All rights reserved.
 * Author:  Alex Beauchesne
 * Date:    5 April 2019
 * 
 * File:    SerializablePoolTypes.cs
 * Purpose: Common serializable pools.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeauPools
{
    /// <summary>
    /// Serializable pool of Transforms.
    /// </summary>
    [Serializable]
    public sealed class TransformPool : SerializablePool<Transform> { }

    /// <summary>
    /// Serializable pool of SpriteRenderers.
    /// </summary>
    [Serializable]
    public sealed class SpriteRendererPool : SerializablePool<SpriteRenderer> { }

    /// <summary>
    /// Serializable pool of RectTransforms.
    /// </summary>
    [Serializable]
    public sealed class RectTransformPool : SerializablePool<RectTransform> { }
}