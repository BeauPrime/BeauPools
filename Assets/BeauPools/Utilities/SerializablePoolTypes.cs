/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    SerializablePoolTypes.cs
 * Purpose: Common serializable pools.
 */

using System;
using UnityEngine;
using UnityEngine.UI;

namespace BeauPools {
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

    /// <summary>
    /// Serializable pool of Images.
    /// </summary>
    [Serializable]
    public sealed class ImagePool : SerializablePool<Image> { }
}