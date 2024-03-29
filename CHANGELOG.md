## Version 0.3.0
**28 March 2024**

New `PrefabPool` instance data, optimizations

### Features
* Added `Pool.TryFree(Component)` and `Pool.TryFree(GameObject)` to attempt to return an instance back to its original pool

### Improvements
* `PrefabPool` caches `IPoolAllocHandler`/`IPoolConstructHandler` instances per-instance
* `PoolConfig` ignores strict type testing for abstract, sealed, and interface types
* `DynamicPool.Prewarm` attempts to reserve additional storage (if required) before constructing new instances
* `PrefabPool` uses `SetLocalPositionAndRotation` instead of individual transform position/rotation changes [2021.3+]
* `PooledStringBuilder` maintains small and large pools, used depending on the requested capacity
* Added `PooledStringBuilder.Prewarm()` for prewarming small/large pools

## Version 0.2.3
**18 Dec 2022**

Memory allocation optimizations

### Improvements
* Various `SerializablePool` memory allocation optimizations
* Added `Pool.Free` overload for List
* Safety checks can be bypassed by specifying `SKIP_POOL_VERIFY` compiler flag

## Version 0.2.2
**11 Oct 2022**

Improvements to SerializablePool interface, tweaks to TempAlloc.

### Features
* Added `SerializablePool.GetActiveObjects` method for copying list of active objects to an existing array, list, or collection.

### Improvements
* **Breaking Change**: `TempAlloc` has been converted to a struct (was erroneously a class despite prior changelog).

## Version 0.2.1
**15 May 2021**

Improvements to SerializablePool interface, bug fixes, and optimizations.

### Features
* Added `TempAlloc` disposable struct, to help manage temporary allocations.
* Added ``SerializablePool.FreeAllInScene`` to assist with unloading scenes with allocated objects from a persistent pool

### Fixes
* Fixed bug with Application quit detection order, which should reduce errors when stopping the game in the editor under certain conditions
* Fixed case where PrefabPool would unnecessarily attempt to call child alloc/construct callbacks if the parent implemented a pool callback interface

### Improvements
* ``SerializablePool.ActiveObjects`` now returns a ``ReadOnlyCollection`` instead of a plain `IEnumerable`

## Version 0.2.0
**17 July 2020**

Improvements to prefab pools and serializable pools. Updated copyright info, package name.

### Features
* Added ``IPoolConstructHandler`` and ``IPoolAllocHandler`` for receiving construct/destruct and alloc/free lifecycle events
* Child components of prefabs that implement the above two handlers will receive their appropriate events when using a ``PrefabPool`` or ``SerializablePool``

### Fixes
* Fixed ``RectTransform`` prefab transforms not resetting properly in certain cases

### Improvements
* ``SerializablePool`` now implements a standard ``IPrefabPool`` interface, shared with ``PrefabPool``. You will no longer have to access ``InnerPool`` to operate on the pool.