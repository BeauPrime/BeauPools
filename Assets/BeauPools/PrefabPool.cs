/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    PrefabPool.cs
 * Purpose: Dynamic pool for Unity prefabs. 
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeauPools
{
    /// <summary>
    /// Prefab pool with an expanding capacity.
    /// </summary>
    public class PrefabPool<T> : DynamicPool<T>, IPrefabPool<T> where T : Component
    {
        private string m_Name;
        private T m_Prefab;
        private Transform m_PoolTransform;

        private bool m_ComponentSkipSelfConstruct;
        private bool m_ComponentSkipSelfAlloc;

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
            : base(inInitialCapacity, GetConstructor(inName, inPrefab, inPoolRoot), inbStrictTyping)
        {
            UnityHelper.Initialize();

            m_Name = inName;
            m_Prefab = inPrefab;
            m_PoolTransform = inPoolRoot;

            m_TargetParent = inSpawnTarget;
            m_ResetTransform = inbResetTransform;

            Transform prefabTransform = inPrefab.transform;
            m_IsRectTransform = prefabTransform is RectTransform;

            m_OriginalOrientation = prefabTransform.localRotation;
            m_OriginalScale = prefabTransform.localScale;

            if (m_IsRectTransform)
            {
                RectTransform prefabRectTransform = (RectTransform) prefabTransform;
                m_OriginalPositionOrAnchor3D = prefabRectTransform.anchoredPosition3D;
                m_OriginalSizeDelta = prefabRectTransform.sizeDelta;
                m_OriginalPivot = prefabRectTransform.pivot;
                
                Vector2 originalMin, originalMax;
                originalMin = prefabRectTransform.anchorMin;
                originalMax = prefabRectTransform.anchorMax;
                m_OriginalAnchors = new Vector4(originalMin.x, originalMin.y, originalMax.x, originalMax.y);
            }
            else
            {
                m_OriginalPositionOrAnchor3D = prefabTransform.localPosition;
            }

            m_ComponentSkipSelfConstruct = inPrefab is IPoolConstructHandler;
            m_ComponentSkipSelfAlloc = inPrefab is IPoolAllocHandler;

            inPrefab.GetComponentsInChildren<IPoolConstructHandler>(true, s_ConstructHandlerList);
            if (s_ConstructHandlerList.Count > (m_ComponentSkipSelfConstruct ? 1 : 0))
            {
                m_Config.RegisterOnConstruct(OnConstructCheckComponents);
                m_Config.RegisterOnDestruct(OnDestructCheckComponents);
            }

            inPrefab.GetComponentsInChildren<IPoolAllocHandler>(true, s_AllocHandlerList);
            if (s_AllocHandlerList.Count > (m_ComponentSkipSelfAlloc ? 1 : 0))
            {
                m_Config.RegisterOnAlloc(OnAllocCheckComponents);
                m_Config.RegisterOnFree(OnFreeCheckComponents);
            }

            m_Config.RegisterOnDestruct(OnDestruct);

            s_AllocHandlerList.Clear();
            s_ConstructHandlerList.Clear();
        }

        #region Specialized Alloc

        public override T Alloc()
        {
            T element = InternalAlloc();

            Transform t = element.transform;
            if (m_ResetTransform)
                ResetTransform(t);
            t.transform.SetParent(m_TargetParent, false);

            if (!m_TargetParent)
                SceneManager.MoveGameObjectToScene(element.gameObject, SceneManager.GetActiveScene());

            m_Config.OnAlloc(this, element);
            return element;
        }

        public T Alloc(Transform inParent)
        {
            T element = InternalAlloc();

            Transform t = element.transform;
            if (m_ResetTransform)
                ResetTransform(t);
            t.transform.SetParent(inParent, false);

            if (!inParent)
                SceneManager.MoveGameObjectToScene(element.gameObject, SceneManager.GetActiveScene());

            m_Config.OnAlloc(this, element);
            return element;
        }

        [MethodImpl(256)]
        public T Alloc(Vector3 inPosition, bool inbWorldSpace = false)
        {
            return Alloc(inPosition, m_OriginalOrientation, m_TargetParent, inbWorldSpace);
        }

        [MethodImpl(256)]
        public T Alloc(Vector3 inPosition, Quaternion inOrientation, bool inbWorldSpace = false)
        {
            return Alloc(inPosition, inOrientation, m_TargetParent, inbWorldSpace);
        }

        public T Alloc(Vector3 inPosition, Quaternion inOrientation, Transform inParent, bool inbWorldSpace = false)
        {
            T element = InternalAlloc();

            Transform t = element.transform;
            if (inbWorldSpace)
            {
                t.position = inPosition;
                t.rotation = inOrientation;
            }
            else
            {
                t.localPosition = inPosition;
                t.localRotation = inOrientation;
            }
            t.SetParent(inParent, inbWorldSpace);

            if (!inParent)
                SceneManager.MoveGameObjectToScene(element.gameObject, SceneManager.GetActiveScene());
            
            m_Config.OnAlloc(this, element);
            return element;
        }

        #endregion // Specialized Alloc

        #region Specialized Free

        public override void Free(T inElement)
        {
            base.Free(inElement);

            inElement.transform.SetParent(m_PoolTransform, false);
        }

        #endregion // Free

        public string Name { get { return m_Name; } }
        public T Prefab { get { return m_Prefab; } }
        public Transform PoolTransform { get { return m_PoolTransform; } }
        public Transform DefaultSpawnTransform { get { return m_TargetParent; } }

        #region Events

        static private Constructor<T> GetConstructor(string inName, T inPrefab, Transform inPoolRoot)
        {
            if (inPoolRoot == null)
                throw new ArgumentNullException("inPoolRoot", "Cannot provide null transform as pool root");

            return (p) =>
            {
                T obj = UnityEngine.Object.Instantiate(inPrefab, inPoolRoot, false);
                obj.name = string.Format("{0} [Pool {1}]", inName, p.Count + p.InUse);
                return obj;
            };
        }

        private void OnConstructCheckComponents(IPool<T> inPool, T inElement)
        {
            using(PooledList<IPoolConstructHandler> children = PooledList<IPoolConstructHandler>.Create())
            {
                inElement.GetComponentsInChildren<IPoolConstructHandler>(true, children);

                for(int i = 0, length = children.Count; i < length; ++i)
                {
                    if (!m_ComponentSkipSelfConstruct || children[i] != inElement)
                        children[i].OnConstruct();
                }
            }
        }

        private void OnDestructCheckComponents(IPool<T> inPool, T inElement)
        {
            using(PooledList<IPoolConstructHandler> children = PooledList<IPoolConstructHandler>.Create())
            {
                inElement.GetComponentsInChildren<IPoolConstructHandler>(true, children);

                for(int i = 0, length = children.Count; i < length; ++i)
                {
                    if (!m_ComponentSkipSelfConstruct || children[i] != inElement)
                        children[i].OnDestruct();
                }
            }
        }

        private void OnAllocCheckComponents(IPool<T> inPool, T inElement)
        {
            using(PooledList<IPoolAllocHandler> children = PooledList<IPoolAllocHandler>.Create())
            {
                inElement.GetComponentsInChildren<IPoolAllocHandler>(true, children);

                for(int i = 0, length = children.Count; i < length; ++i)
                {
                    if (!m_ComponentSkipSelfAlloc || children[i] != inElement)
                        children[i].OnAlloc();
                }
            }
        }

        private void OnFreeCheckComponents(IPool<T> inPool, T inElement)
        {
            using(PooledList<IPoolAllocHandler> children = PooledList<IPoolAllocHandler>.Create())
            {
                inElement.GetComponentsInChildren<IPoolAllocHandler>(true, children);

                for(int i = 0, length = children.Count; i < length; ++i)
                {
                    if (!m_ComponentSkipSelfAlloc || children[i] != inElement)
                        children[i].OnFree();
                }
            }
        }

        static private void OnDestruct(IPool<T> inPool, T inElement)
        {
            // Make sure the object hasn't already been destroyed
            if (inElement && inElement.gameObject)
            {
                // We use DestroyImmediate while quitting to prevent issues with
                // calling Object.Destroy within OnDestroy on the last frame
                // Otherwise we might get "cannot destroy Transform" errors
                if (UnityHelper.IsQuitting())
                    UnityEngine.Object.DestroyImmediate(inElement.gameObject);
                else
                    UnityEngine.Object.Destroy(inElement.gameObject);
            }
        }

        private void ResetTransform(Transform inTransform)
        {
            inTransform.localRotation = m_OriginalOrientation;
            inTransform.localScale = m_OriginalScale;

            if (m_IsRectTransform)
            {
                RectTransform rectTransform = (RectTransform) inTransform;
                rectTransform.anchoredPosition3D = m_OriginalPositionOrAnchor3D;
                rectTransform.sizeDelta = m_OriginalSizeDelta;
                rectTransform.pivot = m_OriginalPivot;
                rectTransform.anchorMin = new Vector2(m_OriginalAnchors.x, m_OriginalAnchors.y);
                rectTransform.anchorMax = new Vector2(m_OriginalAnchors.z, m_OriginalAnchors.w);
            }
            else
            {
                inTransform.localPosition = m_OriginalPositionOrAnchor3D;
            }
        }

        #endregion // Events

        static private readonly List<IPoolConstructHandler> s_ConstructHandlerList = new List<IPoolConstructHandler>(8);
        static private readonly List<IPoolAllocHandler> s_AllocHandlerList = new List<IPoolAllocHandler>(8);
    }

    static internal class UnityHelper
    {
        static private bool s_QuittingApplication;
        static private bool s_Initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static internal void Initialize()
        {
            if (!s_Initialized)
            {
                s_Initialized = true;
                Application.quitting += OnQuitting;
            }
        }

        static private void OnQuitting()
        {
            s_QuittingApplication = true;
        }

        static public bool IsQuitting()
        {
            return s_QuittingApplication;
        }
    }
}