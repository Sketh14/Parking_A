#define EMERGENCY_LOOP_EXIT
// #define SPAWN_LOOP_TEST

using System;
using System.Collections.Generic;

using UnityEngine;

using Parking_A.Global;
using Random = UnityEngine.Random;
using System.Threading.Tasks;

namespace Parking_A.Gameplay
{
    public class EnvironmentSpawner
    {
        public async void SpawnEnvironment(Action<int[]> onVehiclesSpawned)
        {
            Debug.Log($"Spawning Vehicles | gridMap[{UniversalConstant._cGridX}x{UniversalConstant._cGridY}] | Size: {UniversalConstant._cGridX * UniversalConstant._cGridY}");
            //Create a grid of 22 x 42 cells
            byte[] gridMap = new byte[(UniversalConstant._cGridX * 2) + (UniversalConstant._cGridY * 2)];

            int gridMapIndex = 0, indexToCheck;
            //Initialize Array to all spots being empty
            for (; gridMapIndex < gridMap.Length; gridMapIndex++)
                gridMap[gridMapIndex] = 0;

            Random.InitState(123456);

            int boundaryOrientation, vehicleCount = 0, neighbourX, neighbourY;
            int xDir, yDir;
            Vector3 spawnPos, spawnRot;

            GameObject boundaryObject;

#if EMERGENCY_LOOP_EXIT
            int emergencyExit = 0;
#endif

            /* Boundary Orientations
                  Top / Down        Right / Left
                |   | x |   |      |   |   |   |  
                -------------      -------------  
                |   | x |   |      | x | x | x |  
                -------------      -------------  
                |   | x |   |      |   |   |   |  
            */

            // 1 cell gap for boundary
#if !SPAWN_HORIZONTAL_TEST
            for (gridMapIndex = 0; gridMapIndex < UniversalConstant._cGridX * 2; gridMapIndex++)
#else
            for (gridMapIndex = 0; gridMapIndex < 22; gridMapIndex++)
#endif
            {
                //Check if the space is occupied or not | Skip if occupied
                if (gridMap[gridMapIndex] != 0)
                    continue;
#if EMERGENCY_LOOP_EXIT
                else
                {
                    if (emergencyExit >= 50) { Debug.Log($"Emergency Break: {emergencyExit}"); break; }
                    emergencyExit++;
                }
#endif

                xDir = 1;
                spawnPos = Vector3.zero;
                spawnPos.y = 0.28f;
                spawnRot = Vector3.zero;

#if !SPAWN_HORIZONTAL_TEST
                //Random Orientation
                //0: Left | 1: Right
                boundaryOrientation = Random.Range(0, 3);         //Original

                if (boundaryOrientation == 2) continue;
#else
                // boundaryOrientation = Random.Range(0, 2);         //Original
                boundaryOrientation = 1;                             //Test
                // gridMapIndex = 0;              //Test
                // Debug.Log($"[CELL CHECK] boundaryOrientation: {boundaryOrientation}"
                // + $" | gridMapIndex: {gridMapIndex} | gridMap[gridMapIndex]:{gridMap[gridMapIndex]}");
#endif
                // <------------ {(UniversalConstant._cGridX / 4),(UniversalConstant._cGridY / 4)} ------------> Top-Left placement of a car
                // Any combination done with the above co-odrinates will result in a co-ordinate at the top-left of the current cell


                switch (boundaryOrientation)
                {
                    //Check Left
                    case 0:
                        xDir = -1;

                        //By default, the car will be placed in the left-orientation
                        spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + (gridMapIndex % UniversalConstant._cGridX * 0.5f);
                        goto case 4;

                    //Check Right
                    case 1:
                        xDir = 1;

                        spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + (gridMapIndex % UniversalConstant._cGridX * 0.5f) + 0.5f;
                        // Debug.Log($"spawnPos.x: {spawnPos.x} | mod: {(gridMapIndex % UniversalConstant._cGridX)} | top-left: {(UniversalConstant._cGridX / 4.0f * -1.0f)} ");
                        goto case 4;

                    //Check Horizontal Pairs
                    case 4:
                        //Both will be down in Y for horizontal pair
                        spawnPos.z = (UniversalConstant._cGridY / 4.0f) - 0.25f
                            - (UniversalConstant._cGridX * (UniversalConstant._cGridY - 1) / UniversalConstant._cGridX * 0.5f * (gridMapIndex / UniversalConstant._cGridX));
                        // Debug.Log($"spawnPos.x: {spawnPos.x} | mod: {gridMapIndex / (UniversalConstant._cGridX - 1)} | top-left: {UniversalConstant._cGridY / 4.0f} ");

                        //Check if vehicle can be placed
                        for (neighbourX = 0; neighbourX < 2; neighbourX++)
                        {
                            indexToCheck = gridMapIndex + (neighbourX * xDir);
                            // Debug.Log($"[BOUNDS CHECK] (indexToCheck%UniversalConstant._cGridX): {indexToCheck % UniversalConstant._cGridX}"
                            //     + $" | (indexToCheck/UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX}"
                            //     + $" | indexToCheck: {indexToCheck}");

                            //Bounds Check
                            if (indexToCheck < 0 || indexToCheck >= (UniversalConstant._cGridX * 2) + (UniversalConstant._cGridY * 2))      //Out of Range
                            {
                                // Debug.Log($"(gridMapIndex / UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX} | (gridMapIndex % UniversalConstant._cGridX):{indexToCheck % UniversalConstant._cGridX} "
                                // + $"| Bounds: {indexToCheck}");
                                goto case 6;
                            }
                            //Cell Filled Check
                            else if (gridMap[indexToCheck] != 0)
                                goto case 6;
                        }

                        //Fill the cells
                        for (neighbourX = 0; neighbourX < 2; neighbourX++)
                            gridMap[gridMapIndex + (neighbourX * xDir)] = (byte)PoolManager.PoolType.BOUNDARY;
                        // gridMap[gridMapIndex + (neighbourX * xDir) +
                        //     (UniversalConstant._cGridX * (UniversalConstant._cGridY - 1) * (gridMapIndex / UniversalConstant._cGridX))]
                        //     = (byte)PoolManager.PoolType.BOUNDARY;

                        boundaryObject = PoolManager.Instance.PrefabPool[PoolManager.PoolType.BOUNDARY].Get();
                        boundaryObject.name = $"BoundaryI{gridMapIndex}";
                        boundaryObject.transform.position = spawnPos;
                        boundaryObject.transform.localEulerAngles = spawnRot;

                        vehicleCount++;
                        break;

                    //Cell Occupied | Out Of Bounds
                    case 6:
                        continue;

                    default:
                        Debug.LogError($"Vehicle Spawner | Wrong Vehicle Orientation: {boundaryOrientation}");
                        continue;
                }
                //Check the validity of the random vehicle | If the vehicle can escape from the parking lot or not

                // If true, then place the vehicle
                // If false, then choose another vehicle | leave the spot empty

                await Task.Yield();
            }

            /*
            #if !SPAWN_VERTICAL_TEST
                        for (gridMapIndex = 0; gridMapIndex < UniversalConstant._cGridY * 2; gridMapIndex++)
            #else
                        for (gridMapIndex = 0; gridMapIndex < 22; gridMapIndex++)
            #endif
                        {
                            //Check if the space is occupied or not | Skip if occupied
                            if (gridMap[gridMapIndex] != 0)
                                continue;
            #if EMERGENCY_LOOP_EXIT
                            else
                            {
                                if (emergencyExit >= 50) { Debug.Log($"Emergency Break: {emergencyExit}"); break; }
                                emergencyExit++;
                            }
            #endif

                            xDir = 1; yDir = 1;
                            spawnPos = Vector3.zero;
                            spawnPos.y = 0.28f;
                            spawnRot = Vector3.zero;
                            spawnRot.y = 90f;

            #if !SPAWN_VERTICAL_TEST
                            //Random Orientation
                            //2: Up | 3: Down
                            boundaryOrientation = Random.Range(2, 5);         //Original

                            if (boundaryOrientation == 4) continue;
            #else
                            boundaryOrientation = 1;                             //Test
                            // gridMapIndex = 0;              //Test
                            // Debug.Log($"[CELL CHECK] boundaryOrientation: {boundaryOrientation}"
                            // + $" | gridMapIndex: {gridMapIndex} | gridMap[gridMapIndex]:{gridMap[gridMapIndex]}");
            #endif
                            // <------------ {(UniversalConstant._cGridX / 4),(UniversalConstant._cGridY / 4)} ------------> Top-Left placement of a car
                            // Any combination done with the above co-odrinates will result in a co-ordinate at the top-left of the current cell

                            switch (boundaryOrientation)
                            {
                                //Check up
                                case 2:
                                    yDir = -1;
                                    spawnRot.y = 90f;

                                    spawnPos.z = (UniversalConstant._cGridY / 4.0f) - (gridMapIndex / UniversalConstant._cGridX * 0.5f);
                                    goto case 5;

                                // Check down
                                case 3:
                                    yDir = 1;
                                    spawnRot.y = 90f;

                                    spawnPos.z = (UniversalConstant._cGridY / 4.0f) - (gridMapIndex / UniversalConstant._cGridX * 0.5f) - 0.5f;
                                    goto case 5;

                                //Check vertical pairs
                                case 5:
                                    //Both will be right in X for Vertical pair
                                    spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + (gridMapIndex % UniversalConstant._cGridX * 0.5f) - 0.25f;

                                    //Check if vehicle can be placed
                                    for (neighbourY = 0; neighbourY < 2; neighbourY++)
                                    {
                                        indexToCheck = gridMapIndex + (neighbourY * yDir * UniversalConstant._cGridX);

                                        // Debug.Log($"[BOUNDS CHECK] (indexToCheck%UniversalConstant._cGridX): {indexToCheck % UniversalConstant._cGridX}"
                                        //     + $" | (indexToCheck/UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX}"
                                        //     + $" | indexToCheck: {indexToCheck}");

                                        //Bounds Check
                                        // - No matter what the value, this (indexToCheck % UniversalConstant._cGridX) will always be between (0 - UniversalConstant._cGridX)
                                        if (indexToCheck < 0 || indexToCheck >= (UniversalConstant._cGridX * 2) + (UniversalConstant._cGridY * 2))      //Out of Range
                                        {
                                            // Debug.Log($"[OUT OF BOUNDS] indexToCheck:{indexToCheck}");
                                            goto case 6;
                                        }
                                        //Cell Filled Check
                                        else if (gridMap[indexToCheck] != 0)
                                            goto case 6;
                                    }

                                    //Fill the cells
                                    for (neighbourY = 0; neighbourY < 2; neighbourY++)
                                        gridMap[gridMapIndex + (neighbourY * yDir * UniversalConstant._cGridX)] = (byte)PoolManager.PoolType.BOUNDARY;

                                    boundaryObject = PoolManager.Instance.PrefabPool[PoolManager.PoolType.BOUNDARY].Get();
                                    boundaryObject.name = $"BoundaryI{gridMapIndex}";
                                    boundaryObject.transform.position = spawnPos;
                                    boundaryObject.transform.localEulerAngles = spawnRot;

                                    vehicleCount++;
                                    break;

                                //Cell Occupied | Out Of Bounds
                                case 6:
                                    continue;

                                default:
                                    Debug.LogError($"Vehicle Spawner | Wrong Vehicle Orientation: {boundaryOrientation}");
                                    continue;
                            }

                            //Check the validity of the random vehicle | If the vehicle can escape from the parking lot or not

                            // If true, then place the vehicle
                            // If false, then choose another vehicle | leave the spot empty

                            await Task.Yield();
                        }
            */

            Debug.Log($"Spawning Finished");
        }
    }
}