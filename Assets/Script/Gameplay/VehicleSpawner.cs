using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Random = UnityEngine.Random;
using Math = System.Math;

namespace Test_A.Gameplay
{
    [Serializable]
    public class VehicleSpawner
    {
        // private readonly Vector2 _topLeft = new Vector2(-12f, 15f);
        // private readonly Vector2 _bottomRight = new Vector2(12f, -15f);

        //Top-Left Vehicle Center: {-5.5, 10.5} | Bottom-Right: {5.5, -10.5}

        //Top-Left: {-6, 11} | Bottom-Right: {6, -11}
        private readonly float[] _borderCoordinates;

        //Single float array | Will access dimensions using offset
        private readonly float[] _vehicleDimensions;

        private const int _cVehicleLayerMask = (1 << 6);
        private List<Transform> _vehiclesSpawned;
        public List<Transform> VehiclesSpawned { get => _vehiclesSpawned; }

        private const byte _cGridX = 22, _cGridY = 42;

        public VehicleSpawner()
        {
            _borderCoordinates = new float[] { 6.1f, 11.1f };            //Original
            // _borderCoordinates = new float[] { 3f, 3f };           //Test

            _vehicleDimensions = new float[] { 1f, 1f, 1f, 2f, 1f, 2.5f };

            // SpawnVehicles();
            // SpawnVehicles2();
        }

        public async void SpawnVehicles()
        {
            Debug.Log($"Spawning Vehicles");

            Random.InitState(123456);
            _vehiclesSpawned = new List<Transform>();
            PoolManager.PoolType vehicleType;
            Vector3 spawnPos = Vector3.zero, spawnRot = Vector3.zero, halfExtents = Vector3.zero;
            // bool lotFull = false;
            float xPos = 0f, zPos = 0f, yRot = 0f;

            int noSpaceFound = 0;
            // RaycastHit boxCasthitInfo;

            #region OverlapCapsuleNonAlloc
            /*
            bool colDetected = false;
            Collider[] overLapHitResults = new Collider[5];
            */
            #endregion OverlapCapsuleNonAlloc

            //Keep filling until the necessary amount of vehicles are filled in the parking lot
            // while(!lotFull)
            for (int i = 0; i < 60;)
            {

                //Put vehicles in random spots between lot boundaries
                xPos = Random.Range(-1f, 1f) * (_borderCoordinates[0] - 1f);
                zPos = Random.Range(-1f, 1f) * (_borderCoordinates[1] - 1f);
                yRot = (Random.Range(0f, 1f) > 0.7f) ? 90f : 0f;

                vehicleType = (PoolManager.PoolType)Random.Range(0, (int)PoolManager.PoolType.VEHICLE_L);

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
                spawnRot.Set(0f, yRot, 0f);

                //For Offsetting dimensions array by 2
                halfExtents.Set((_vehicleDimensions[0 + (int)vehicleType * 2] / 2) + 0.2f
                    , 0.5f
                    , (_vehicleDimensions[1 + (int)vehicleType * 2] / 2) + 0.2f);

                //BoxCast does not take into account the starting collider if it starts within a collider
                //BoxCast Forward
                // if (!Physics.BoxCast(spawnPos, halfExtents, Vector3.forward, Quaternion.identity, 1f, _cVehicleLayerMask))

                // Does not work,as the Physics system queries with no gameobjects present at the instantiated location with overlapping query location
                //BoxCast Down
                if (!Physics.BoxCast(spawnPos, halfExtents, Vector3.down
                    // , out boxCasthitInfo
                    , Quaternion.Euler(spawnRot), 1f, _cVehicleLayerMask))
                {
                    spawnPos.y = 0.58f;

                    _vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[vehicleType].Get().transform);
                    _vehiclesSpawned[i].name = $"{vehicleType}_{i}";
                    _vehiclesSpawned[i].position = spawnPos;
                    _vehiclesSpawned[i].localEulerAngles = spawnRot;
                    // Debug.Log($"vehilce Status | Name : {_vehiclesSpawned[i].name} "
                    // + $"| Active : {_vehiclesSpawned[i].gameObject.activeInHierarchy}");

                    i++;
                    noSpaceFound = 0;
                }
                else if (noSpaceFound++ > 30)
                {
                    //     Debug.Log($"Hit Info : {boxCasthitInfo.transform.name}");
                    // noSpaceFound++;
                    // if (noSpaceFound > 30)
                    // {
                    Debug.Log($"Maximum Limit for no space found Hit : {noSpaceFound}");
                    break;
                    // }
                }
                await Task.Delay(10);            //Wait for Physics System to Update
            }

            GameManager.Instance.OnVehiclesSpawned?.Invoke();
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

        public void SpawnVehicles2()
        {
            //Create a grid of 22 x 42 cells
            byte[] gridMap = new byte[_cGridX * _cGridY];

            int gridMapIndex = 0;
            //Initialize Array to all spots being empty
            for (; gridMapIndex < gridMap.Length; gridMapIndex++)
                gridMap[gridMapIndex] = 0;

            int vehicleType, neighbourX, neighbourY;
            bool cellOccupied = false;
            Random.InitState(123456);

            for (gridMapIndex = 0; gridMapIndex < 1; gridMapIndex++)
            {
                //Choose a random vehicle 
                // 0: Blank | 1-3: Vehicle Index
                // vehicleType = Random.Range(0, 4);           //Original
                vehicleType = 1;              //Test | Only include small vehicles

                //Fill the associated cells: Left,Right,Up,Down accordingly
                //- As we are going left to right from the top
                //  [=] Checking from the top-left of a cell and including other cells should do
                //      as we will never be going the opposite way
                //  [=] Maybe when back-tracking is implemented
                switch (vehicleType)
                {
                    //Blank Space
                    case 0: continue;

                    //Small Vehicle
                    case 1:
                        //Check if the selected cell is already occupied
                        //If not, then check if all the neighbour cells to fill are occupied or not

                        //Check horizontal pairs
                        for (neighbourX = 1; neighbourX >= -1; neighbourX *= -1)
                        {
                            //Bounds Check
                            if (gridMapIndex + neighbourX < 0 || gridMapIndex + neighbourX > _cGridX)
                                continue;
                            else if (gridMap[gridMapIndex + neighbourX] != 0)
                            {
                                cellOccupied = true;
                                break;
                            }
                            // gridMap[gridMapIndex + neighbourX] = (byte)vehicleType;
                        }

                        if (cellOccupied)
                            continue;
                        else
                        {
                            //Check vertical pairs
                            for (neighbourY = 1; neighbourY >= -1; neighbourY *= -1)
                            {
                                // Debug.Log($"gridMapIndex: {gridMapIndex} | neighbourX: {neighbourX} | neighbourY: {neighbourY}"
                                //     + $"| [gridMapIndex + nX + nY * 22]: {gridMapIndex + neighbourX + neighbourY * 22}");

                                //Bounds Check
                                if (gridMapIndex + neighbourY < 0 || gridMapIndex + (neighbourY * _cGridX) > _cGridX * _cGridY)
                                    continue;
                                // 1 down/up will be on the next line, so multiply by gridX
                                else if (gridMap[gridMapIndex + (neighbourY * _cGridX)] != 0)                                
                                    break;
                                
                                // gridMap[gridMapIndex + neighbourY * _cGridX] = (byte)vehicleType;                             
                            }
                        }

                        //Skip Over and reset the cellOccupied counter
                        break;

                    //Medium Vehicle
                    case 2:
                        break;

                    //Long Vehicle
                    case 3:
                        break;

                    default:
                        Debug.LogError($"Wrong Vehicle Type: {vehicleType}");
                        continue;
                }
                cellOccupied = false;

                //Check the validity of the random vehicle | If the vehicle can escape from the parking lot or not

                // If true, then place the vehicle
                // If false, then choose another vehicle | leave the spot empty
            }
        }

        private void CheckIfValidPlacement(int topLeftIndex, PoolManager.PoolType vehicleType)
        {
        }
    }
}