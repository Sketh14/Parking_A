#define RESET_SKINS_AT_END

using System;
using System.Collections.Generic;
using Parking_A.Global;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace Parking_A.Gameplay
{
    public class PoolManager : MonoBehaviour
    {
        // public enum PoolType { BLANK = 0, VEHICLE_S = 1, VEHICLE_M, VEHICLE_L, BOUNDARY }
        internal enum PoolStatus { NOT_INTIALIZED = 0, INITIALIZED = 1 << 0 }

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
        private PoolStatus _poolStatus;

        public Dictionary<UniversalConstant.PoolType, UnityEngine.Pool.ObjectPool<GameObject>> PrefabPool;

        private void OnDestroy()
        {
#if RESET_SKINS_AT_END
            //Reset Skins
            for (int i = 0; i < 3; i++)
            {
                _poolSObjArr[i].poolPrefab.transform.GetChild(4).GetChild(0).GetComponent<MeshRenderer>().material =
                    GameManager.Instance.VehicleInfoSOs[i].SkinsMat[0];
            }
#endif
        }

        private void Start()
        {
            PrefabPool = new Dictionary<UniversalConstant.PoolType, UnityEngine.Pool.ObjectPool<GameObject>>();
        }

        public void InitializePool()
        {
            if ((_poolStatus & PoolStatus.INITIALIZED) != 0)
                return;

            //Updating skins of Vehicles | maybe this should be in GameManager
            for (int i = 0; i < 3; i++)
            {
                _poolSObjArr[i].poolPrefab.transform.GetChild(4).GetChild(0).GetComponent<MeshRenderer>().material =
                    GameManager.Instance.VehicleInfoSOs[i]
                        .SkinsMat[GameManager.Instance.CurrentPlayerStats.EquippedVehicleSkinIndexes[i]];
            }

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

            _poolStatus |= PoolStatus.INITIALIZED;
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
            pooledObject.SetActive(false);
        }

        private void ReleasePooledObject(GameObject pooledObject)
        {
            pooledObject.SetActive(false);
        }
    }
}