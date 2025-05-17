using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Test_A.Gameplay
{
    [Serializable]
    public class VehicleSpawner
    {
        // private readonly Vector2 _topLeft = new Vector2(-12f, 15f);
        // private readonly Vector2 _bottomRight = new Vector2(12f, -15f);

        //Top-Left: {-6, 11} | Bottom-Right: {6, -11}
        // private readonly float[] _borderCoordinates = new float[] { 6f, 11f };            //Original
        private readonly float[] _borderCoordinates = new float[] { 3f, 3f };           //Test

        //Single float array | Will access dimensions using offset
        private readonly float[] _vehicleDimensions = new float[] { 1f, 1f, 1f };

        [SerializeField] private const int _cVehicleLayerMask = (1 << 6);
        private List<Transform> _vehiclesSpawned;
        public List<Transform> VehiclesSpawned { get => _vehiclesSpawned; }

        public async void SpawnVehicles()
        {
            Debug.Log($"Spawning Vehicles");
            _vehiclesSpawned = new List<Transform>();
            Vector3 spawnPos = Vector3.zero, spawnRot = Vector3.zero, halfExtents = Vector3.zero;
            bool lotFull = false;
            float xPos = 0f, zPos = 0f, yRot = 0f;

            int vehicleCount = 0;
            // RaycastHit boxCasthitInfo;

            Random.InitState(123456);

            #region OverlapCapsuleNonAlloc
            /*
            bool colDetected = false;
            Collider[] overLapHitResults = new Collider[5];
            */
            #endregion OverlapCapsuleNonAlloc

            //Keep filling until the necessary amount of vehicles are filled in the parking lot
            // while(!lotFull)
            for (int i = 0; i < 10; i++)
            {

                //Put vehicles in random spots between lot boundaries
                xPos = Random.Range(-1f, 1f) * (_borderCoordinates[0] - 1f);
                zPos = Random.Range(-1f, 1f) * (_borderCoordinates[1] - 1f);
                yRot = (Random.Range(0f, 1f) > 0.7f) ? 90f : 0f;

                //Check for vehicle collisions
                #region OverlapCapsuleNonAlloc
                //[IMPORTANT] [Implement] OverlapBoxCommand
                // if (Physics.OverlapCapsuleNonAlloc(new Vector3(xPos, 0.7f, zPos + 0.5f), new Vector3(xPos, 0.7f, zPos - 0.5f),
                //      1f, overLapHitResults, _cVehicleLayerMask) == 0)           //There's buffer so it does not work for Non-Alloc

                /*
                Physics.OverlapCapsuleNonAlloc(new Vector3(xPos, 0.7f, zPos + 0.5f), new Vector3(xPos, 0.7f, zPos - 0.5f),
                 1f, overLapHitResults, _cVehicleLayerMask);

                foreach (var col in overLapHitResults)
                {
                    if (col != null)
                    {
                        colDetected = true;
                        Debug.Log($"Col DEtected : {col.transform.position}");
                        break;
                    }
                }
                if (colDetected)
                {
                    colDetected = false;
                    continue;
                }
                */
                #endregion OverlapCapsuleNonAlloc

                spawnPos.Set(xPos, 2f, zPos);         //y should be around 0.58f for now | Test: 0.7f
                halfExtents.Set(0.5f, 0.35f, 0.5f);

                //BoxCast does not take into account the starting collider if it starts within a collider
                //BoxCast Forward
                // if (!Physics.BoxCast(spawnPos, halfExtents, Vector3.forward, Quaternion.identity, 1f, _cVehicleLayerMask))

                // Does not work,as the Physics system queries with no gameobjects present at the instantiated location with overlapping query location
                //BoxCast Down
                if (!Physics.BoxCast(spawnPos, halfExtents, Vector3.down
                    // , out boxCasthitInfo
                    , Quaternion.identity, 1f, _cVehicleLayerMask))
                {
                    spawnRot.Set(0f, yRot, 0f);
                    spawnPos.y = 0.58f;

                    _vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[PoolManager.PoolType.VEHICLE_1].Get().transform);
                    _vehiclesSpawned[vehicleCount].name = $"Vehicle_{vehicleCount}";
                    _vehiclesSpawned[vehicleCount].position = spawnPos;
                    _vehiclesSpawned[vehicleCount].localEulerAngles = spawnRot;
                    // Debug.Log($"vehilce Status | Name : {_vehiclesSpawned[vehicleCount].name} "
                    // + $"| Active : {_vehiclesSpawned[vehicleCount].gameObject.activeInHierarchy}");

                    vehicleCount++;
                }
                // else
                //     Debug.Log($"Hit Info : {boxCasthitInfo.transform.name}");
                await Task.Delay(1000);
            }

            //Tests
            /*{
                // GameObject vehicle;
                vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[PoolManager.PoolType.VEHICLE_1].Get().transform);
                // vehiclesSpawned[0].position.Set(0f, 0.58f, -6f);
                vehiclesSpawned[0].position = new Vector3(0f, 0.58f, -6f);
                vehiclesSpawned[0].localEulerAngles = new Vector3(0f, 90f, 0f);
                // vehiclesSpawned.Add(vehicle.transform);

                vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[PoolManager.PoolType.VEHICLE_1].Get().transform);
                // vehiclesSpawned[1].position.Set(0f, 0.58f, -3f);
                vehiclesSpawned[1].position = new Vector3(0f, 0.58f, -3f);
                vehiclesSpawned[1].localEulerAngles = new Vector3(0f, 0f, 0f);
                // vehiclesSpawned.Add(vehicle.transform);
            }*/
        }

        private void CheckIfValidPlacement()
        {

        }
    }
}