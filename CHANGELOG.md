### Version 0.2.0
**17 July 2020**

Improvements to prefab pools and serializable pools. Updated copyright info, package name.

### Features
* Added ``IPoolConstructHandler`` and ``IPoolAllocHandler`` for receiving construct/destruct and alloc/free lifecycle events
* Child components of prefabs that implement the above two handlers will receive their appropriate events when using a ``PrefabPool`` or ``SerializablePool``

### Fixes
* Fixed ``RectTransform`` prefab transforms not resetting properly in certain cases

### Improvements
* ``SerializablePool`` now implements a standard ``IPrefabPool`` interface, shared with ``PrefabPool``. You will no longer have to access ``InnerPool`` to operate on the pool.