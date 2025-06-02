#define EMERGENCY_LOOP_EXIT
// #define SPAWN_HORIZONTAL_TEST
// #define SPAWN_VERTICAL_TEST

using System;

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
            byte[] gridMap = new byte[(UniversalConstant._cGridX * 2) + ((UniversalConstant._cGridY - 1) * 2)];         //Additional 2 cells just in case

            int gridMapIndex = 0, indexToCheck;
            //Initialize Array to all spots being empty
            for (; gridMapIndex < gridMap.Length; gridMapIndex++)
                gridMap[gridMapIndex] = 0;

            Random.InitState(123456);

            int boundaryOrientation, neighbourX, neighbourY;
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

            // 1 cell gap for last-boundary
#if !SPAWN_HORIZONTAL_TEST
            for (gridMapIndex = 0; gridMapIndex < (UniversalConstant._cGridX * 2) - 1; gridMapIndex++)
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
                spawnPos = Vector3.zero;

#if !SPAWN_HORIZONTAL_TEST
                //Random Orientation: Left/Right
                boundaryOrientation = Random.Range(0, 4);         //Original
                if (boundaryOrientation < 3) continue;
#else
                boundaryOrientation = 3;                             //Test
                // gridMapIndex = 0;              //Test
                // Debug.Log($"[CELL CHECK] boundaryOrientation: {boundaryOrientation}"
                // + $" | gridMapIndex: {gridMapIndex} | gridMap[gridMapIndex]:{gridMap[gridMapIndex]}");
#endif
                // <------------ {(UniversalConstant._cGridX / 4),(UniversalConstant._cGridY / 4)} ------------> Top-Left placement of a car
                // Any combination done with the above co-odrinates will result in a co-ordinate at the top-left of the current cell

                spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + (gridMapIndex % UniversalConstant._cGridX * 0.5f) + 0.5f;
                // Debug.Log($"spawnPos.x: {spawnPos.x} | mod: {(gridMapIndex % UniversalConstant._cGridX)} | top-left: {(UniversalConstant._cGridX / 4.0f * -1.0f)} ");

                //Both will be down in Y for horizontal pair
                spawnPos.z = (UniversalConstant._cGridY / 4.0f) - 0.25f
                    - (UniversalConstant._cGridX * (UniversalConstant._cGridY - 1) / UniversalConstant._cGridX * 0.5f * (gridMapIndex / UniversalConstant._cGridX));
                // Debug.Log($"spawnPos.x: {spawnPos.x} | mod: {gridMapIndex / (UniversalConstant._cGridX - 1)} | top-left: {UniversalConstant._cGridY / 4.0f} ");

                //Check if boundary can be placed
                for (neighbourX = 0; neighbourX < 2; neighbourX++)
                {
                    indexToCheck = gridMapIndex + neighbourX;
                    // Debug.Log($"[BOUNDS CHECK] (indexToCheck%UniversalConstant._cGridX): {indexToCheck % UniversalConstant._cGridX}"
                    //     + $" | (indexToCheck/UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX}"
                    //     + $" | indexToCheck: {indexToCheck}");

                    //Bounds Check
                    if (indexToCheck < 0 || indexToCheck >= (UniversalConstant._cGridX * 2) + (UniversalConstant._cGridY * 2))      //Out of Range
                    {
                        // Debug.Log($"(gridMapIndex / UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX} | (gridMapIndex % UniversalConstant._cGridX):{indexToCheck % UniversalConstant._cGridX} "
                        // + $"| Bounds: {indexToCheck}");
                        continue;
                    }
                    //Cell Occupied
                    else if (gridMap[indexToCheck] != 0)
                        continue;
                }

                //Fill the cells
                for (neighbourX = 0; neighbourX < 2; neighbourX++)
                    gridMap[gridMapIndex + neighbourX] = (byte)PoolManager.PoolType.BOUNDARY;

                boundaryObject = PoolManager.Instance.PrefabPool[PoolManager.PoolType.BOUNDARY].Get();
                boundaryObject.name = $"BoundaryH_I[{gridMapIndex}]";
                boundaryObject.transform.position = spawnPos;
                boundaryObject.transform.rotation = Quaternion.identity;

                //Check the validity of the random vehicle | If the vehicle can escape from the parking lot or not

                // If true, then place the vehicle
                // If false, then choose another vehicle | leave the spot empty

                await Task.Yield();
            }

            // /*
#if !SPAWN_VERTICAL_TEST
            for (gridMapIndex = UniversalConstant._cGridX * 2;
                gridMapIndex < (UniversalConstant._cGridX * 2) + (UniversalConstant._cGridY - 2) * 2;       //Avoid top/bottom boudnaries
                gridMapIndex++)
#else
            for (gridMapIndex = 44; gridMapIndex < 86; gridMapIndex++)
#endif
            {
                //Check if the space is occupied or not | Skip if occupied
                if (gridMap[gridMapIndex] != 0)
                {
                    // Debug.Log($"Cell Occupied | index: {gridMapIndex} | Type: {gridMap[gridMapIndex]}");
                    continue;
                }
#if EMERGENCY_LOOP_EXIT
                else
                {
                    if (emergencyExit >= 250) { Debug.Log($"Emergency Break: {emergencyExit}"); break; }
                    emergencyExit++;
                }
#endif

                spawnPos = Vector3.zero;
                spawnRot = Vector3.zero;
                spawnRot.y = 90f;

#if !SPAWN_VERTICAL_TEST
                //Random Orientation: Up/ Down
                boundaryOrientation = Random.Range(0, 4);         //Original
                if (boundaryOrientation < 3) continue;
#else
                boundaryOrientation = 3;                             //Test
                // gridMapIndex = 0;              //Test
                // Debug.Log($"[CELL CHECK] boundaryOrientation: {boundaryOrientation}"
                // + $" | gridMapIndex: {gridMapIndex} | gridMap[gridMapIndex]:{gridMap[gridMapIndex]}");
#endif
                // <------------ {(UniversalConstant._cGridX / 4),(UniversalConstant._cGridY / 4)} ------------> Top-Left placement of a car
                // Any combination done with the above co-odrinates will result in a co-ordinate at the top-left of the current cell

                spawnRot.y = 90f;
                spawnPos.z = (UniversalConstant._cGridY / 4.0f) - ((gridMapIndex - UniversalConstant._cGridX * 2) % (UniversalConstant._cGridY - 2) * 0.5f) - 1.0f;       //Extra 0.5f to offset above boundary
                // - (UniversalConstant._cGridX * (UniversalConstant._cGridY - 1) / UniversalConstant._cGridX * 0.5f * (gridMapIndex / UniversalConstant._cGridX));

                //Both will be right in X for Vertical pair
                // spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + 0.25f + ((UniversalConstant._cGridX - 1) * 0.5f);       // For objects on the left-side
                // spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + 0.25f;        // For objects on the left-side
                spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + 0.25f
                    + ((gridMapIndex - (UniversalConstant._cGridX * 2)) / (UniversalConstant._cGridY - 2)
                        * (UniversalConstant._cGridX - 1) * 0.5f);        // For both-sides

                //Check if vehicle can be placed
                for (neighbourY = 0; neighbourY < 2; neighbourY++)
                {
                    indexToCheck = gridMapIndex + neighbourY;

                    // Debug.Log($"[BOUNDS CHECK] (indexToCheck%UniversalConstant._cGridX): {indexToCheck % UniversalConstant._cGridX}"
                    //     + $" | (indexToCheck/UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX}"
                    //     + $" | indexToCheck: {indexToCheck}");

                    // - No matter what the value, this (indexToCheck % UniversalConstant._cGridX) will always be between (0 - UniversalConstant._cGridX)
                    if (indexToCheck < 0 || indexToCheck >= gridMap.Length)      //Out of Range
                    {
                        // Debug.Log($"[OUT OF BOUNDS] indexToCheck:{indexToCheck}");
                        continue;
                    }
                    //Cell Occupied
                    else if (gridMap[indexToCheck] != 0)
                        continue;
                }

                //Fill the cells
                for (neighbourY = 0; neighbourY < 2; neighbourY++)
                    gridMap[gridMapIndex + neighbourY] = (byte)PoolManager.PoolType.BOUNDARY;

                boundaryObject = PoolManager.Instance.PrefabPool[PoolManager.PoolType.BOUNDARY].Get();
                boundaryObject.name = $"BoundaryV_I[{gridMapIndex}]";
                boundaryObject.transform.position = spawnPos;
                boundaryObject.transform.localEulerAngles = spawnRot;

                //Check the validity of the random vehicle | If the vehicle can escape from the parking lot or not

                // If true, then place the vehicle
                // If false, then choose another vehicle | leave the spot empty

                await Task.Yield();
            }
            Debug.Log($"Spawning Finished");
            // */
        }

    }
}
