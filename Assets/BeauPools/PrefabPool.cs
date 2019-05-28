/*
 * Copyright (C) 2018-2019. Filament Games, LLC. All rights reserved.
 * Author:  Alex Beauchesne
 * Date:    5 April 2019
 * 
 * File:    PrefabPool.cs
 * Purpose: Dynamic pool for Unity prefabs. 
 */

using System;
using UnityEngine;

namespace BeauPools
{
    /// <summary>
    /// Prefab pool with an expanding capacity.
    /// </summary>
    public class PrefabPool<T> : DynamicPool<T> where T : Component
    {
        private string m_Name;
        private T m_Prefab;
        private Transform m_PoolTransform;

        private Transform m_TargetParent;
        private bool m_ResetTransform;
        private Vector3 m_OriginalPosition;
        private Quaternion m_OriginalOrientation;

        public PrefabPool(int inInitialCapacity, T inPrefab, Transform inInactiveRoot, Transform inSpawnTarget = null, bool inbResetTransform = true, bool inbStrictTyping = true) : this(inPrefab.name, inInitialCapacity, inPrefab, inInactiveRoot, inSpawnTarget, inbResetTransform, inbStrictTyping) { }

        public PrefabPool(string inName, int inInitialCapacity, T inPrefab, Transform inInactiveRoot, Transform inSpawnTarget = null, bool inbResetTransform = true, bool inbStrictTyping = true) : base(inInitialCapacity, GetConstructor(inName, inPrefab, inInactiveRoot), inbStrictTyping)
        {
            m_Name = inName;
            m_Prefab = inPrefab;
            m_PoolTransform = inInactiveRoot;

            m_TargetParent = inSpawnTarget;
            m_ResetTransform = inbResetTransform;

            m_OriginalPosition = inPrefab.transform.localPosition;
            m_OriginalOrientation = inPrefab.transform.localRotation;

            m_Config.RegisterOnDestruct(OnDestruct);
        }

        #region Specialized Alloc

        public override T Alloc()
        {
            T element = InternalAlloc();

            Transform t = element.transform;
            if (m_ResetTransform)
                ResetTransform(t);
            t.transform.SetParent(m_TargetParent, false);

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

            m_Config.OnAlloc(this, element);
            return element;
        }

        public T Alloc(Vector3 inPosition, bool inbWorldSpace = false)
        {
            return Alloc(inPosition, m_OriginalOrientation, m_TargetParent, inbWorldSpace);
        }

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

        static private Constructor<T> GetConstructor(string inName, T inPrefab, Transform inInactiveRoot)
        {
            if (inInactiveRoot == null)
                throw new ArgumentNullException("inInactiveRoot", "Cannot provide null transform as pool root");

            return (p) =>
            {
                T obj = UnityEngine.Object.Instantiate(inPrefab, inInactiveRoot, false);
                obj.name = inName + string.Format(" [Pool {0}]", p.Count + p.InUse);
                return obj;
            };
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
            inTransform.localPosition = m_OriginalPosition;
            inTransform.localRotation = m_OriginalOrientation;
        }

        #endregion // Events
    }

    static internal class UnityHelper
    {
        static private bool s_QuittingApplication;
        static private bool s_Initialized;

        [RuntimeInitializeOnLoadMethod]
        static private void Initialize()
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