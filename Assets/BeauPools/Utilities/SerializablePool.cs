/*
 * Copyright (C) 2018 - 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    5 April 2019
 * 
 * File:    SerializablePool.cs
 * Purpose: Generic serializable pool.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeauPools
{
    /// <summary>
    /// Serializable pool of objects.
    /// </summary>
    public class SerializablePool<T> : IPrefabPool<T> where T : Component
    {
        #region Inspector

        [SerializeField, Tooltip("Prefab to populate the pool with")]
        private T m_Prefab = null;

        [SerializeField, Tooltip("Optional name to use for labeling instantiated prefabs")]
        private string m_Name = null;

        [Header("Size")]

        [SerializeField, Tooltip("Initial pool capacity")]
        private int m_InitialCapacity = 32;

        [SerializeField, Tooltip("If set, the pool will automatically prewarm on initialization")]
        private bool m_PrewarmOnInitialize = true;

        [SerializeField, Tooltip("The number of prefabs to spawn if PrewarmOnInitialize is set")]
        private int m_InitialPrewarm = 16;

        [Header("Transforms")]

        [SerializeField, Tooltip("Default parent for instantiated prefabs")]
        private Transform m_DefaultPoolRoot = null;

        [SerializeField, Tooltip("Default parent for allocated prefabs")]
        private Transform m_DefaultSpawnRoot = null;

        [SerializeField, Tooltip("If set, prefab transforms will be reset to their initial state on allocation")]
        private bool m_ResetTransformOnAlloc = true;

        #endregion // Inspector

        private PrefabPool<T> m_InnerPool;

        [NonSerialized] private readonly List<T> m_ActiveObjects = new List<T>();
        [NonSerialized] private ReadOnlyCollection<T> m_ReadOnlyActive;

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
            get { return string.IsNullOrEmpty(m_Name) && m_Prefab ? m_Prefab.name : m_Name; }
            set
            {
                if (m_InnerPool != null)
                    throw new InvalidOperationException("Cannot configure name while pool is initialized");
                if (string.IsNullOrEmpty(value))
                    m_Name = m_Prefab ? m_Prefab.name : null;
                else
                    m_Name = value;
            }
        }

        /// <summary>
        /// Pool prefab.
        /// </summary>
        public T Prefab
        {
            get { return m_Prefab; }
            set
            {
                if (m_Prefab != value)
                {
                    if (m_InnerPool != null)
                        throw new InvalidOperationException("Cannot change prefab while pool is initialized");
                    m_Prefab = value;
                }
            }
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
        /// Attempts to initialize the pool. If already initialized, will skip.
        /// </summary>
        public bool TryInitialize(Transform inPoolRoot = null, Transform inSpawnRoot = null, int inPrewarmCapacity = -1)
        {
            if (m_InnerPool != null)
                return false;

            Initialize(inPoolRoot, inSpawnRoot, inPrewarmCapacity);
            return true;
        }

        /// <summary>
        /// Initializes the pool. Will throw an exception if the pool has already been initialized.
        /// </summary>
        public void Initialize(Transform inPoolRoot = null, Transform inSpawnRoot = null, int inPrewarmCapacity = -1)
        {
            if (m_InnerPool != null)
                throw new InvalidOperationException("Cannot load pool twice");

            if (inPoolRoot == null)
                inPoolRoot = m_DefaultPoolRoot;
            if (inSpawnRoot == null)
                inSpawnRoot = m_DefaultSpawnRoot;
            if (inPrewarmCapacity < 0 && m_PrewarmOnInitialize)
                inPrewarmCapacity = m_InitialPrewarm;

            if (!inPoolRoot)
                throw new ArgumentNullException("inPoolRoot", "No pool root, and no default pool root");

            m_InnerPool = new PrefabPool<T>(Name, m_InitialCapacity, m_Prefab, inPoolRoot, inSpawnRoot, m_ResetTransformOnAlloc, !AllowLooseTyping());
            m_InnerPool.Config.RegisterOnAlloc(OnAlloc);
            m_InnerPool.Config.RegisterOnFree(OnFree);

            if (inPrewarmCapacity > 0)
                m_InnerPool.Prewarm(inPrewarmCapacity);
        }

        /// <summary>
        /// Returns if the pool has been initialized.
        /// </summary>
        [MethodImpl(256)]
        public bool IsInitialized()
        {
            return m_InnerPool != null;
        }

        /// <summary>
        /// Prewarms the pool.
        /// </summary>
        public void Prewarm()
        {
            if (m_InnerPool == null)
            {
                Initialize(null, null, m_InitialPrewarm);
            }
            else
            {
                m_InnerPool.Prewarm(m_InitialPrewarm);
            }
        }

        [Obsolete("SerializablePool now implements the IPrefabPool interface")]
        public IPrefabPool<T> InnerPool
        {
            get { return m_InnerPool; }
        }

        /// <summary>
        /// Collection containing all currently spawned objects.
        /// </summary>
        public ReadOnlyCollection<T> ActiveObjects
        {
            [MethodImpl(256)] get { return m_ReadOnlyActive ?? (m_ReadOnlyActive = new ReadOnlyCollection<T>(m_ActiveObjects)); }
        }

        /// <summary>
        /// Copies active objects into the given array buffer.
        /// </summary>
        public int GetActiveObjects(T[] inBuffer)
        {
            int copyCount = Math.Min(inBuffer.Length, m_ActiveObjects.Count);
            m_ActiveObjects.CopyTo(0, inBuffer, 0, copyCount);
            return copyCount;
        }

        /// <summary>
        /// Copies active objects into the given list.
        /// </summary>
        public int GetActiveObjects(List<T> inBuffer)
        {
            inBuffer.AddRange(m_ActiveObjects);
            return m_ActiveObjects.Count;
        }

        /// <summary>
        /// Copies active objects into the given collection.
        /// </summary>
        public int GetActiveObjects(ICollection<T> inBuffer)
        {
            for(int i = 0; i < m_ActiveObjects.Count; i++)
                inBuffer.Add(m_ActiveObjects[i]);
            return m_ActiveObjects.Count;
        }

        /// <summary>
        /// Copies active objects into the given array buffer.
        /// </summary>
        public int GetActiveObjects(T[] inBuffer, int inBufferOffset)
        {
            int copyCount = Math.Min(inBuffer.Length - inBufferOffset, m_ActiveObjects.Count);
            m_ActiveObjects.CopyTo(0, inBuffer, inBufferOffset, copyCount);
            return copyCount;
        }

        [Obsolete("ActiveObjectCount is deprecated in favor of ActiveObjects.Count")]
        public int ActiveObjectCount()
        {
            return m_ActiveObjects.Count;
        }

        /// <summary>
        /// Returns all currently allocated prefabs to the pool.
        /// </summary>
        public void Reset()
        {
            if (m_InnerPool != null && m_ActiveObjects.Count > 0)
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

        /// <summary>
        /// Frees all allocated prefabs currently in the given scene.
        /// </summary>
        public int FreeAllInScene(Scene inScene)
        {
            if (m_InnerPool != null)
            {
                using(PooledList<T> tempList = PooledList<T>.Create())
                {
                    foreach(var activeObj in m_ActiveObjects)
                    {
                        if (activeObj.gameObject.scene == inScene)
                            tempList.Add(activeObj);
                    }

                    int finalCount = tempList.Count;
                    m_InnerPool.Free(tempList);
                    return finalCount;
                }
            }
            
            return 0;
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
            int idx = m_ActiveObjects.IndexOf(inElement);
            int end = m_ActiveObjects.Count - 1;
            if (idx >= 0)
            {
                if (idx != end)
                    m_ActiveObjects[idx] = m_ActiveObjects[end];
                m_ActiveObjects.RemoveAt(end);
            }
        }

        #endregion // Callbacks

        #region IPrefabPool

        public Transform PoolTransform
        {
            get
            {
                if (m_InnerPool != null)
                    return m_InnerPool.PoolTransform;
                
                return m_DefaultPoolRoot;
            }
        }

        public Transform DefaultSpawnTransform
        {
            get
            {
                if (m_InnerPool != null)
                    return m_InnerPool.DefaultSpawnTransform;
                
                return m_DefaultSpawnRoot;
            }
        }

        public PoolConfig<T> Config
        {
            get
            {
                if (m_InnerPool != null)
                    return m_InnerPool.Config;

                return null;
            }
        }

        public int Capacity
        {
            get
            {
                if (m_InnerPool != null)
                    return m_InnerPool.Capacity;
                
                return m_InitialCapacity;
            }
        }

        public int Count
        {
            get
            {
                if (m_InnerPool != null)
                    return m_InnerPool.Count;
                
                return 0;
            }
        }

        public int InUse
        {
            get
            {
                if (m_InnerPool != null)
                    return m_InnerPool.InUse;
                
                return 0;
            }
        }

        [MethodImpl(256)]
        public T Alloc(Transform inParent)
        {
            TryInitialize();
            return m_InnerPool.Alloc(inParent);
        }

        [MethodImpl(256)]
        public T Alloc(Vector3 inPosition, bool inbWorldSpace = false)
        {
            TryInitialize();
            return m_InnerPool.Alloc(inPosition, inbWorldSpace);
        }

        [MethodImpl(256)]
        public T Alloc(Vector3 inPosition, Quaternion inOrientation, bool inbWorldSpace = false)
        {
            TryInitialize();
            return m_InnerPool.Alloc(inPosition, inOrientation, inbWorldSpace);
        }

        [MethodImpl(256)]
        public T Alloc(Vector3 inPosition, Quaternion inOrientation, Transform inParent, bool inbWorldSpace = false)
        {
            TryInitialize();
            return m_InnerPool.Alloc(inPosition, inOrientation, inParent, inbWorldSpace);
        }

        [MethodImpl(256)]
        public T Alloc()
        {
            TryInitialize();
            return m_InnerPool.Alloc();
        }

        [MethodImpl(256)]
        public bool TryAlloc(out T outElement)
        {
            TryInitialize();
            return m_InnerPool.TryAlloc(out outElement);
        }

        [MethodImpl(256)]
        public void Free(T inElement)
        {
            if (m_InnerPool == null)
                throw new InvalidOperationException("Cannot free an element while pool is not initialized");

            m_InnerPool.Free(inElement);
        }

        public void Prewarm(int inCount)
        {
            if (m_InnerPool == null)
            {
                Initialize(null, null, inCount);
            }
            else
            {
                m_InnerPool.Prewarm(inCount);
            }
        }

        public void Shrink(int inCount)
        {
            if (m_InnerPool != null)
            {
                m_InnerPool.Shrink(inCount);
            }
        }

        [MethodImpl(256)]
        public void Clear()
        {
            Reset();
        }

        [MethodImpl(256)]
        public void Dispose()
        {
            Destroy();
        }

        #endregion // IPrefabPool
    }
}