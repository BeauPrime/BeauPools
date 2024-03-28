/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    PrefabPool.cs
 * Purpose: Dynamic pool for Unity prefabs. 
 */

#if !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)
#define VERIFY_POOLS
#endif // !SKIP_POOL_VERIFY && (DEVELOPMENT || DEVELOPMENT_BUILD || DEBUG)

#if UNITY_2021_3_OR_NEWER
#define FAST_TRANSFORM_RESET
#endif // UNITY_2021_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeauPools {
    /// <summary>
    /// Prefab pool with an expanding capacity.
    /// </summary>
    public sealed class PrefabPool<T> : DynamicPool<T>, IPrefabPool<T> where T : Component {
        private string m_Name;
        private T m_Prefab;
        private Transform m_PoolTransform;

        private Transform m_TargetParent;
        private bool m_IsRectTransform;
        private bool m_ResetTransform;

        private Quaternion m_OriginalOrientation;
        private Vector3 m_OriginalScale;
        private Vector3 m_OriginalPositionOrAnchor3D;
        private Vector2 m_OriginalSizeDelta;
        private Vector4 m_OriginalAnchors;
        private Vector2 m_OriginalPivot;

        public PrefabPool(int inInitialCapacity, T inPrefab, Transform inPoolRoot, Transform inSpawnTarget = null, bool inbResetTransform = true, bool inbStrictTyping = true)
            : this(inPrefab.name, inInitialCapacity, inPrefab, inPoolRoot, inSpawnTarget, inbResetTransform, inbStrictTyping) { }

        public PrefabPool(string inName, int inInitialCapacity, T inPrefab, Transform inPoolRoot, Transform inSpawnTarget = null, bool inbResetTransform = true, bool inbStrictTyping = true)
            : base(inInitialCapacity, GetConstructor(inName, inPrefab, inPoolRoot), inbStrictTyping) {
            UnityHelper.Initialize();

            m_Name = inName;
            m_Prefab = inPrefab;
            m_PoolTransform = inPoolRoot;

            if (!inPoolRoot) {
                throw new ArgumentNullException("inPoolRoot");
            }
            if (!inPrefab) {
                throw new ArgumentNullException("inPoolRoot");
            }

            if (inPoolRoot.gameObject.activeInHierarchy) {
                UnityEngine.Debug.LogWarningFormat("[PrefabPool] Prefab Pool '{0}' of type '{1}' has an active pool root '{2}'", inName, typeof(T).Name, inPoolRoot.name);
            }

            m_TargetParent = inSpawnTarget;
            m_ResetTransform = inbResetTransform;

            Transform prefabTransform = inPrefab.transform;
            m_IsRectTransform = prefabTransform is RectTransform;

            m_OriginalScale = prefabTransform.localScale;

#if FAST_TRANSFORM_RESET
            prefabTransform.GetLocalPositionAndRotation(out m_OriginalPositionOrAnchor3D, out m_OriginalOrientation);
#else
            m_OriginalOrientation = prefabTransform.localRotation;
#endif // FAST_TRANSFORM_RESET

            if (m_IsRectTransform) {
                RectTransform prefabRectTransform = (RectTransform) prefabTransform;
#if !FAST_TRANSFORM_RESET
                m_OriginalPositionOrAnchor3D = prefabRectTransform.anchoredPosition3D;
#endif // !FAST_TRANSFORM_RESET
                m_OriginalSizeDelta = prefabRectTransform.sizeDelta;
                m_OriginalPivot = prefabRectTransform.pivot;

                Vector2 originalMin, originalMax;
                originalMin = prefabRectTransform.anchorMin;
                originalMax = prefabRectTransform.anchorMax;
                m_OriginalAnchors = new Vector4(originalMin.x, originalMin.y, originalMax.x, originalMax.y);
            }
#if !FAST_TRANSFORM_RESET
            else {
                m_OriginalPositionOrAnchor3D = prefabTransform.localPosition;
            }
#endif // !FAST_TRANSFORM_RESET

            PooledPrefabInstance poolTracker = inPrefab.GetComponent<PooledPrefabInstance>();
            if (poolTracker == null) {
                poolTracker = inPrefab.gameObject.AddComponent<PooledPrefabInstance>();
            }
            poolTracker.SourcePool = null;
            poolTracker.PoolToken = null;
            poolTracker.hideFlags |= HideFlags.NotEditable | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            if (!poolTracker.HandlersInitialized) {
                poolTracker.ConstructHandlers = inPrefab.GetComponentsInChildren(typeof(IPoolConstructHandler), true);
                poolTracker.AllocHandlers = inPrefab.GetComponentsInChildren(typeof(IPoolAllocHandler), true);
                poolTracker.HandlersInitialized = true;
            }

            bool prefabConstructHandler = inPrefab is IPoolConstructHandler;
            bool prefabAllocHandler = inPrefab is IPoolAllocHandler;

            if (poolTracker.ConstructHandlers.Length > (prefabConstructHandler ? 1 : 0)) {
                m_Config.RegisterOnConstruct(OnConstructCheckComponents);
                m_Config.RegisterOnDestruct(OnDestructCheckComponents);
            }

            if (poolTracker.AllocHandlers.Length > (prefabAllocHandler ? 1 : 0)) {
                m_Config.RegisterOnAlloc(OnAllocCheckComponents);
                m_Config.RegisterOnFree(OnFreeCheckComponents);
            }

            m_Config.RegisterOnDestruct(OnDestruct);
        }

        #region Specialized Alloc

        public override T Alloc() {
            T element = InternalAlloc();

            Transform t = element.transform;
            if (m_ResetTransform)
                ResetTransform(t);
            t.SetParent(m_TargetParent, false);

            if (!m_TargetParent)
                SceneManager.MoveGameObjectToScene(element.gameObject, SceneManager.GetActiveScene());

            m_Config.OnAlloc(this, element);
            return element;
        }

        public T Alloc(Transform inParent) {
            T element = InternalAlloc();

            Transform t = element.transform;
            if (m_ResetTransform)
                ResetTransform(t);
            t.SetParent(inParent, false);

            if (!inParent)
                SceneManager.MoveGameObjectToScene(element.gameObject, SceneManager.GetActiveScene());

            m_Config.OnAlloc(this, element);
            return element;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Alloc(Vector3 inPosition, bool inbWorldSpace = false) {
            return Alloc(inPosition, m_OriginalOrientation, m_TargetParent, inbWorldSpace);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Alloc(Vector3 inPosition, Quaternion inOrientation, bool inbWorldSpace = false) {
            return Alloc(inPosition, inOrientation, m_TargetParent, inbWorldSpace);
        }

        public T Alloc(Vector3 inPosition, Quaternion inOrientation, Transform inParent, bool inbWorldSpace = false) {
            T element = InternalAlloc();

            Transform t = element.transform;
            if (inbWorldSpace) {
                t.SetPositionAndRotation(inPosition, inOrientation);
            } else {
#if FAST_TRANSFORM_RESET
                t.SetLocalPositionAndRotation(inPosition, inOrientation);
#else
                t.localPosition = inPosition;
                t.localRotation = inOrientation;
#endif // FAST_TRANSFORM_RESET
            }
            t.SetParent(inParent, inbWorldSpace);

            if (!inParent)
                SceneManager.MoveGameObjectToScene(element.gameObject, SceneManager.GetActiveScene());

            m_Config.OnAlloc(this, element);
            return element;
        }

        #endregion // Specialized Alloc

        #region Specialized Free

        public override void Free(T inElement) {
            base.Free(inElement);

            inElement.transform.SetParent(m_PoolTransform, false);
        }

        #endregion // Free

        public string Name { get { return m_Name; } }
        public T Prefab { get { return m_Prefab; } }
        public Transform PoolTransform { get { return m_PoolTransform; } }
        public Transform DefaultSpawnTransform { get { return m_TargetParent; } }

        #region Events

        static private Constructor<T> GetConstructor(string inName, T inPrefab, Transform inPoolRoot) {
            if (inPoolRoot == null)
                throw new ArgumentNullException("inPoolRoot", "Cannot provide null transform as pool root");

            return (p) => {
                T obj = UnityEngine.Object.Instantiate(inPrefab, inPoolRoot, false);
#if UNITY_EDITOR
                obj.name = string.Format("{0} [Pool {1}]", inName, p.Count + p.InUse);
#endif // UNITY_EDITOR
                obj.GetComponent<PooledPrefabInstance>().Initialize(inPrefab, obj, p);
                return obj;
            };
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        private void OnConstructCheckComponents(IPool<T> inPool, T inElement) {
            Component[] children = inElement.GetComponent<PooledPrefabInstance>().ConstructHandlers;
            for (int i = 0, length = children.Length; i < length; ++i) {
                if (!ReferenceEquals(children[i], inElement)) {
                    ((IPoolConstructHandler) children[i]).OnConstruct();
                }
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        private void OnDestructCheckComponents(IPool<T> inPool, T inElement) {
            Component[] children = inElement.GetComponent<PooledPrefabInstance>().ConstructHandlers;
            for (int i = 0, length = children.Length; i < length; ++i) {
                if (!ReferenceEquals(children[i], inElement)) {
                    ((IPoolConstructHandler) children[i]).OnDestruct();
                }
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        private void OnAllocCheckComponents(IPool<T> inPool, T inElement) {
            Component[] children = inElement.GetComponent<PooledPrefabInstance>().AllocHandlers;
            for (int i = 0, length = children.Length; i < length; ++i) {
                if (!ReferenceEquals(children[i], inElement)) {
                    ((IPoolAllocHandler) children[i]).OnAlloc();
                }
            }
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        private void OnFreeCheckComponents(IPool<T> inPool, T inElement) {
            Component[] children = inElement.GetComponent<PooledPrefabInstance>().AllocHandlers;
            for (int i = 0, length = children.Length; i < length; ++i) {
                if (!ReferenceEquals(children[i], inElement)) {
                    ((IPoolAllocHandler) children[i]).OnFree();
                }
            }
        }

        static private void OnDestruct(IPool<T> inPool, T inElement) {
            // Make sure the object hasn't already been destroyed
            if (inElement && inElement.gameObject) {
                // We use DestroyImmediate while quitting to prevent issues with
                // calling Object.Destroy within OnDestroy on the last frame
                // Otherwise we might get "cannot destroy Transform" errors
                if (UnityHelper.IsQuitting())
                    UnityEngine.Object.DestroyImmediate(inElement.gameObject);
                else
                    UnityEngine.Object.Destroy(inElement.gameObject);
            }
        }

        private void ResetTransform(Transform inTransform) {
#if FAST_TRANSFORM_RESET
            inTransform.SetLocalPositionAndRotation(m_OriginalPositionOrAnchor3D, m_OriginalOrientation);
#else
            inTransform.localRotation = m_OriginalOrientation;
#endif // FAST_TRANSFORM_RESET
            inTransform.localScale = m_OriginalScale;

            if (m_IsRectTransform) {
                RectTransform rectTransform = (RectTransform) inTransform;
#if !FAST_TRANSFORM_RESET
                rectTransform.anchoredPosition3D = m_OriginalPositionOrAnchor3D;
#endif // !FAST_TRANSFORM_RESET
                rectTransform.sizeDelta = m_OriginalSizeDelta;
                rectTransform.pivot = m_OriginalPivot;
                rectTransform.anchorMin = new Vector2(m_OriginalAnchors.x, m_OriginalAnchors.y);
                rectTransform.anchorMax = new Vector2(m_OriginalAnchors.z, m_OriginalAnchors.w);
            }
#if !FAST_TRANSFORM_RESET
            else {
                inTransform.localPosition = m_OriginalPositionOrAnchor3D;
            }
#endif // FAST_TRANSFORM_RESET
        }

        #endregion // Events
    }

    /// <summary>
    /// Internal unity helpers.
    /// </summary>
    static internal class UnityHelper {
        static private bool s_QuittingApplication;
        static private bool s_Initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static internal void Initialize() {
            if (!s_Initialized) {
                s_Initialized = true;
                Application.quitting += OnQuitting;
            }
        }

        static private void OnQuitting() {
            s_QuittingApplication = true;
        }

        static public bool IsQuitting() {
            return s_QuittingApplication;
        }
    }
}