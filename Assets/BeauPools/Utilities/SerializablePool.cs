/*
 * Copyright (C) 2018-2019. Filament Games, LLC. All rights reserved.
 * Author:  Alex Beauchesne
 * Date:    5 April 2019
 * 
 * File:    SerializablePool.cs
 * Purpose: Generic serializable pool.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeauPools
{
    /// <summary>
    /// Serializable pool of objects.
    /// </summary>
    public abstract class SerializablePool<T> where T : Component
    {
        #region Inspector

        [SerializeField]
        private string m_Name = null;

        [SerializeField]
        private T m_Prefab = null;

        [Header("Size")]

        [SerializeField]
        private int m_InitialCapacity = 32;

        [SerializeField]
        private int m_InitialPrewarm = 16;

        [SerializeField]
        private bool m_PrewarmOnInitialize = true;

        [Header("Transforms")]

        [SerializeField]
        private Transform m_DefaultPoolRoot = null;

        [SerializeField]
        private Transform m_DefaultSpawnRoot = null;

        [SerializeField]
        private bool m_ResetTransformOnAlloc = true;

        #endregion // Inspector

        private PrefabPool<T> m_InnerPool;
        private List<T> m_ActiveObjects = new List<T>();

        // default constructor
        public SerializablePool() { }

        public SerializablePool(T inPrefab) : this(inPrefab.name, inPrefab) { }

        public SerializablePool(string inName, T inPrefab)
        {
            m_Name = inName;
            m_Prefab = inPrefab;
        }

        /// <summary>
        /// Pool name.
        /// </summary>
        public string Name
        {
            get { return string.IsNullOrEmpty(m_Name) ? m_Prefab.name : m_Name; }
        }

        /// <summary>
        /// Configures capacity and prewarm settings.
        /// </summary>
        public void ConfigureCapacity(int inCapacity, int inPrewarm, bool inbPrewarmOnInit = true)
        {
            if (m_InnerPool != null)
                throw new InvalidOperationException("Cannot configure settings while pool is initialized");

            m_InitialCapacity = inCapacity;
            m_InitialPrewarm = inPrewarm;
            m_PrewarmOnInitialize = inbPrewarmOnInit;
        }

        /// <summary>
        /// Configures transform and reset settings.
        /// </summary>
        public void ConfigureTransforms(Transform inPoolRoot, Transform inSpawnRoot = null, bool inbResetTransformOnAlloc = true)
        {
            if (m_InnerPool != null)
                throw new InvalidOperationException("Cannot configure settings while pool is initialized");

            m_DefaultPoolRoot = inPoolRoot;
            m_DefaultSpawnRoot = inSpawnRoot;
            m_ResetTransformOnAlloc = inbResetTransformOnAlloc;
        }

        /// <summary>
        /// Initializes the pool.
        /// </summary>
        public void Initialize(Transform inPoolRoot = null, Transform inSpawnRoot = null)
        {
            if (m_InnerPool != null)
                throw new InvalidOperationException("Cannot load pool twice");

            if (inPoolRoot == null)
                inPoolRoot = m_DefaultPoolRoot;
            if (inSpawnRoot == null)
                inSpawnRoot = m_DefaultSpawnRoot;

            if (!inPoolRoot)
                throw new ArgumentNullException("inPoolRoot", "No pool root, and no default pool root");

            m_InnerPool = new PrefabPool<T>(Name, m_InitialCapacity, m_Prefab, inPoolRoot, inSpawnRoot, m_ResetTransformOnAlloc, !AllowLooseTyping());
            m_InnerPool.Config.RegisterOnAlloc(OnAlloc);
            m_InnerPool.Config.RegisterOnFree(OnFree);

            if (m_PrewarmOnInitialize)
                m_InnerPool.Prewarm(m_InitialPrewarm);
        }

        /// <summary>
        /// Returns if the pool has been initialized.
        /// </summary>
        public bool IsInitialized()
        {
            return m_InnerPool != null;
        }

        /// <summary>
        /// Prewarms the pool.
        /// </summary>
        public void Prewarm()
        {
            m_InnerPool.Prewarm(m_InitialPrewarm);
        }

        /// <summary>
        /// The working pool.
        /// </summary>
        public PrefabPool<T> InnerPool
        {
            get { return m_InnerPool; }
        }

        /// <summary>
        /// Enumerable containing all currently spawned objects.
        /// This will be invalidated if you free any objects while iterating.
        /// </summary>
        public IEnumerable<T> ActiveObjects()
        {
            foreach (var element in m_ActiveObjects)
                yield return element;
        }

        /// <summary>
        /// Writes all currently spawned objects to the given collection.
        /// Returns the number of currently spawned objects.
        /// </summary>
        public int ActiveObjects(ICollection<T> ioCollection)
        {
            foreach (var element in m_ActiveObjects)
                ioCollection.Add(element);
            return m_ActiveObjects.Count;
        }

        /// <summary>
        /// Returns the number of current spawned objects.
        /// </summary>
        public int ActiveObjectCount()
        {
            return m_ActiveObjects.Count;
        }

        /// <summary>
        /// Returns all currently allocated prefabs to the pool.
        /// </summary>
        public void Reset()
        {
            if (m_InnerPool != null)
            {
                using(PooledList<T> tempList = PooledList<T>.Create(m_ActiveObjects))
                {
                    m_InnerPool.Free(tempList);
                }
            }
        }

        /// <summary>
        /// Unloads the pool.
        /// </summary>
        public void Destroy()
        {
            if (m_InnerPool != null)
            {
                Reset();

                m_InnerPool.Dispose();
                m_InnerPool = null;
            }
        }

        #region Virtuals

        /// <summary>
        /// Override to allow subclasses of T to be used for the prefab.
        /// By default, this will return true if T is an abstract type.
        /// </summary>
        protected virtual bool AllowLooseTyping()
        {
            return typeof(T).IsAbstract;
        }

        #endregion // Virtuals

        #region Callbacks

        private void OnAlloc(IPool<T> inPool, T inElement)
        {
            m_ActiveObjects.Add(inElement);
        }

        private void OnFree(IPool<T> inPool, T inElement)
        {
            m_ActiveObjects.Remove(inElement);
        }

        #endregion // Callbacks

        #region Conversions

        static public implicit operator PrefabPool<T>(SerializablePool<T> inSerialized)
        {
            return inSerialized.InnerPool;
        }

        #endregion // Conversions
    }
}