using System;
using System.Collections.Generic;
using Parking_A.Global;
using UnityEngine;

namespace Parking_A.Gameplay
{
    public class PoolManager : MonoBehaviour
    {
        // public enum PoolType { BLANK = 0, VEHICLE_S = 1, VEHICLE_M, VEHICLE_L, BOUNDARY }

        #region Singleton
        private static PoolManager _instance;
        public static PoolManager Instance { get => _instance; }

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(this.gameObject);

            PrefabPool = new Dictionary<UniversalConstant.PoolType, UnityEngine.Pool.ObjectPool<GameObject>>();
        }
        #endregion Singleton

        [SerializeField] private PoolScriptableObject[] _poolSObjArr;

        public Dictionary<UniversalConstant.PoolType, UnityEngine.Pool.ObjectPool<GameObject>> PrefabPool;

        private void Start()
        {
            PrefabPool = new Dictionary<UniversalConstant.PoolType, UnityEngine.Pool.ObjectPool<GameObject>>();
            //Initialize pool to contain 5 of every item
            for (int i = 0; i < _poolSObjArr.Length; i++)
            {
                GameObject poolHolder = new GameObject(_poolSObjArr[i].name.ToString());
                poolHolder.transform.SetParent(transform);

                int temp = i;
                UnityEngine.Pool.ObjectPool<GameObject> objectPool = new UnityEngine.Pool.ObjectPool<GameObject>(
                    createFunc: () => { return CreatePool(temp, poolHolder.transform); },
                    actionOnGet: GetPooledObject,
                    actionOnRelease: ReleasePooledObject,
                    defaultCapacity: 5,
                    maxSize: 30
                );

                PrefabPool.Add((UniversalConstant.PoolType)(i + 1), objectPool);
                // Debug.Log($"Creating Prefab Pool | Name : {poolHolder.name} | i: {0} | PoolType: {(PoolType)i} | Count: {PrefabPool[PoolType.VEHICLE_1].CountAll}");
            }
        }

        private GameObject CreatePool(int poolIndex, in Transform poolParent)
        {
            // Debug.Log($"CreatePool called for{poolIndex}");
            GameObject poolObject = Instantiate(_poolSObjArr[poolIndex].poolPrefab, poolParent);
            return poolObject;
        }

        private void GetPooledObject(GameObject pooledObject)
        {
            // Debug.Log($"Activating Pooled Object | Name: {pooledObject.name} | Active : {pooledObject.activeInHierarchy}");
            pooledObject.SetActive(true);
        }

        private void ReleasePooledObject(GameObject pooledObject)
        {
            pooledObject.SetActive(false);
        }
    }
}