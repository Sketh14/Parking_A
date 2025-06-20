#define EMERGENCY_LOOP_EXIT
// #define SPAWN_HORIZONTAL_TEST
// #define SPAWN_VERTICAL_TEST

using UnityEngine;

using Parking_A.Global;
using Random = UnityEngine.Random;

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace Parking_A.Gameplay
{
    public class EnvironmentSpawner
    {
        private List<GameObject> _boundariesSpawned;
        public List<GameObject> BoundariesSpawned { get => _boundariesSpawned; }

        // private const string _randomSeed = "SKETH";

        // public async Task SpawnBoundary(Action<byte[]> onBoundarySpawned)
        private CancellationTokenSource _cts;
        ~EnvironmentSpawner()
        {
            _cts.Cancel();
        }

        public EnvironmentSpawner()
        {
            _cts = new CancellationTokenSource();
            _boundariesSpawned = new List<GameObject>();
        }

        public void ClearBoundaries()
        {
            _boundariesSpawned.Clear();
        }

        public async Task SpawnBoundary()
        {
            // Debug.Log($"Spawning Boundary | gridMap[{UniversalConstant._GridXC}x{UniversalConstant._GridYC}] | Size: {UniversalConstant._GridXC * UniversalConstant._GridYC}");
            //Create a grid of 22 x 42 cells
            byte[] gridMap = new byte[(UniversalConstant._GridXC * 2) + ((UniversalConstant._GridYC - 1) * 2)];         //Additional 2 cells just in case

            int gridMapIndex = 0, indexToCheck;
            //Initialize Array to all spots being empty
            for (; gridMapIndex < gridMap.Length; gridMapIndex++)
                gridMap[gridMapIndex] = 0;

            // Random.InitState(GameManager.Instance.MainGameConfig.RandomString.GetHashCode());
            // Random.InitState(123456);

            int boundaryOrientation, neighbourX, neighbourY;
            int boundaryCount = 0;
            Vector3 spawnPos, spawnRot;

            // GameObject boundaryObject;

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

            #region HORIZONTAL_SPAWN
            // 1 cell gap for last-boundary
#if !SPAWN_HORIZONTAL_TEST
            for (gridMapIndex = 0; gridMapIndex < (UniversalConstant._GridXC * 2) - 1; gridMapIndex++)
#else
            for (gridMapIndex = 0; gridMapIndex < 22; gridMapIndex++)
#endif
            {
                //Check if the space is occupied or not | Skip if occupied
                if (gridMap[gridMapIndex] != 0 || (gridMapIndex == UniversalConstant._GridXC - 1))
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
                boundaryOrientation = Random.Range(0, 5);         //Original
                if (boundaryOrientation < 3) continue;
#else
                boundaryOrientation = 3;                             //Test
                // gridMapIndex = 0;              //Test
                // Debug.Log($"[CELL CHECK] boundaryOrientation: {boundaryOrientation}"
                // + $" | gridMapIndex: {gridMapIndex} | gridMap[gridMapIndex]:{gridMap[gridMapIndex]}");
#endif
                // <------------ {(UniversalConstant._cGridX / 4),(UniversalConstant._cGridY / 4)} ------------> Top-Left placement of a car
                // Any combination done with the above co-odrinates will result in a co-ordinate at the top-left of the current cell

                spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) + (gridMapIndex % UniversalConstant._GridXC * 0.5f) + 0.5f;
                // Debug.Log($"spawnPos.x: {spawnPos.x} | mod: {(gridMapIndex % UniversalConstant._cGridX)} | top-left: {(UniversalConstant._cGridX / 4.0f * -1.0f)} ");

                //Both will be down in Y for horizontal pair
                spawnPos.z = (UniversalConstant._GridYC / 4.0f) - 0.25f
                    - (UniversalConstant._GridXC * (UniversalConstant._GridYC - 1) / UniversalConstant._GridXC * 0.5f * (gridMapIndex / UniversalConstant._GridXC));
                // Debug.Log($"spawnPos.x: {spawnPos.x} | mod: {gridMapIndex / (UniversalConstant._cGridX - 1)} | top-left: {UniversalConstant._cGridY / 4.0f} ");

                //Check if boundary can be placed
                for (neighbourX = 0; neighbourX < 2; neighbourX++)
                {
                    indexToCheck = gridMapIndex + neighbourX;
                    // Debug.Log($"[BOUNDS CHECK] (indexToCheck%UniversalConstant._cGridX): {indexToCheck % UniversalConstant._cGridX}"
                    //     + $" | (indexToCheck/UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX}"
                    //     + $" | indexToCheck: {indexToCheck}");

                    //Bounds Check
                    if (indexToCheck < 0 || indexToCheck >= (UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC * 2))      //Out of Range
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
                    gridMap[gridMapIndex + neighbourX] = (byte)UniversalConstant.PoolType.BOUNDARY;

                _boundariesSpawned.Add(PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.BOUNDARY].Get());
                _boundariesSpawned[boundaryCount].name = $"BoundaryH_I[{gridMapIndex}]";
                _boundariesSpawned[boundaryCount].transform.position = spawnPos;
                _boundariesSpawned[boundaryCount].transform.rotation = Quaternion.identity;

                boundaryCount++;

                //Check the validity of the random vehicle | If the vehicle can escape from the parking lot or not

                // If true, then place the vehicle
                // If false, then choose another vehicle | leave the spot empty

                await Task.Yield();
                if (_cts.IsCancellationRequested) return;
            }
            #endregion HORIZONTAL_SPAWN

            #region VERTICAL_SPAWN
            // /*
#if !SPAWN_VERTICAL_TEST
            for (gridMapIndex = UniversalConstant._GridXC * 2;
                gridMapIndex < (UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2) * 2 - 1;       //Avoid top/bottom boudnaries and last cell
                gridMapIndex++)
#else
            for (gridMapIndex = 44; gridMapIndex < 45; gridMapIndex++)
#endif
            {
                //Check if the space is occupied or not | Skip if occupied
                if (gridMap[gridMapIndex] != 0
                    || gridMapIndex == UniversalConstant._GridYC - 3 + (UniversalConstant._GridXC * 2))          //Avoid Last cell for 1st iteration
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

                const int startOffset = UniversalConstant._GridXC * 2;
                spawnPos = Vector3.zero;
                spawnRot = Vector3.zero;
                spawnRot.y = 90f;

#if !SPAWN_VERTICAL_TEST
                //Random Orientation: Up/ Down
                boundaryOrientation = Random.Range(0, 4);         //Original
                if (boundaryOrientation < 3) continue;
#else
                boundaryOrientation = 3;                             //Test
                // gridMapIndex = 83;              //Test
                // Debug.Log($"[CELL CHECK] boundaryOrientation: {boundaryOrientation}"
                // + $" | gridMapIndex: {gridMapIndex} | gridMap[gridMapIndex]:{gridMap[gridMapIndex]}");
#endif
                // <------------ {(UniversalConstant._cGridX / 4),(UniversalConstant._cGridY / 4)} ------------> Top-Left placement of a car
                // Any combination done with the above co-odrinates will result in a co-ordinate at the top-left of the current cell

                spawnRot.y = 90f;
                spawnPos.z = (UniversalConstant._GridYC / 4.0f) - ((gridMapIndex - startOffset)
                    % (UniversalConstant._GridYC - 2) * (UniversalConstant._CellHalfSizeC * 2f)) - 1.0f;       //Extra 1f to offset above boundary
                // - (UniversalConstant._cGridX * (UniversalConstant._cGridY - 1) / UniversalConstant._cGridX * 0.5f * (gridMapIndex / UniversalConstant._cGridX));

                //Both will be right in X for Vertical pair
                // spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + 0.25f + ((UniversalConstant._cGridX - 1) * 0.5f);       // For objects on the left-side
                // spawnPos.x = (UniversalConstant._cGridX / 4.0f * -1.0f) + 0.25f;        // For objects on the left-side
                // If cGridX:22 | cGridY:42, then 84 would be on the right side, as 40/40 is 1
                spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) + 0.25f
                    + ((gridMapIndex - startOffset) / (UniversalConstant._GridYC - 2)
                    * (UniversalConstant._GridXC - 1) * 0.5f);        // For both-sides

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
                    gridMap[gridMapIndex + neighbourY] = (byte)UniversalConstant.PoolType.BOUNDARY;

                _boundariesSpawned.Add(PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.BOUNDARY].Get());
                _boundariesSpawned[boundaryCount].name = $"BoundaryV_I[{gridMapIndex}]";
                _boundariesSpawned[boundaryCount].transform.position = spawnPos;
                _boundariesSpawned[boundaryCount].transform.localEulerAngles = spawnRot;

                boundaryCount++;

                await Task.Yield();
                if (_cts.IsCancellationRequested) return;
            }
            Debug.Log($"Spawning Boundary Finished");
            // */
            #endregion VERTICAL_SPAWN

            // onBoundarySpawned?.Invoke(gridMap);
            GameManager.Instance.OnEnvironmentSpawned?.Invoke(gridMap);
        }

    }
}
