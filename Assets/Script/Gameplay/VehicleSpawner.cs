// #define EMERGENCY_LOOP_EXIT
// #define SPAWN_LOOP_TEST

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Random = UnityEngine.Random;
using Math = System.Math;
using Parking_A.Global;
using System.Linq.Expressions;

namespace Parking_A.Gameplay
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

        // private const string _randomSeed = "AVIN";
        // private const byte _cGridX = 22, _cGridY = 42;

        public VehicleSpawner()
        {
            _vehiclesSpawned = new List<Transform>();
            _borderCoordinates = new float[] { 6.1f, 11.1f };            //Original
            // _borderCoordinates = new float[] { 3f, 3f };           //Test

            _vehicleDimensions = new float[] { 1f, 1f, 1f, 2f, 1f, 2.5f };

            // SpawnVehicles();
            // SpawnVehicles2();
        }

        public async void SpawnVehicles(Action onVehiclesSpawned)
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

            // GameManager.Instance.OnVehiclesSpawned?.Invoke();
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

        public async void SpawnVehicles2(byte[] boundaryData, Action<int[]> onVehiclesSpawned)
        {
            Debug.Log($"Spawning Vehicles | gridMap[{UniversalConstant._GridXC}x{UniversalConstant._GridYC}] | Size: {UniversalConstant._GridXC * UniversalConstant._GridYC}");
            //Create a grid of 22 x 42 cells
            byte[] gridMap = new byte[UniversalConstant._GridXC * UniversalConstant._GridYC];

            int gridMapIndex = 0, indexToCheck;
            //Initialize Array to all spots being empty
            for (; gridMapIndex < gridMap.Length; gridMapIndex++)
                gridMap[gridMapIndex] = 0;

            //Initialize Boundary Data
#if !DEBUG_GRID_BOUNDARY
            System.Text.StringBuilder debugGrid = new System.Text.StringBuilder();
#endif
            //Top / Bottom
            for (int bIndex = 0; bIndex < UniversalConstant._GridXC * 2; bIndex++)
            {
                gridMap[bIndex + (bIndex / UniversalConstant._GridXC
                    * UniversalConstant._GridXC * (UniversalConstant._GridYC - 2))] = boundaryData[bIndex];
            }
            //Left / Right
            for (int bIndex = UniversalConstant._GridXC, bDataIndex = UniversalConstant._GridXC * 2;
                bDataIndex < (UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2) * 2 - 1;       //Avoid top/bottom boudnaries and last cell
                bDataIndex++)
            {
                gridMap[bIndex] = boundaryData[bDataIndex];

                // bIndex += UniversalConstant._cGridX
                //     - (bDataIndex - (UniversalConstant._cGridX * 2)) / (UniversalConstant._cGridY - 2)
                //         * (bDataIndex - UniversalConstant._cGridX) + (UniversalConstant._cGridX - 1);
                // Debug.Log($"bIndex: {bIndex} | bDataIndex: {bDataIndex} | gridMap[{bIndex}]: {boundaryData[bDataIndex]}");
                bIndex += UniversalConstant._GridXC;

                if (bIndex == UniversalConstant._GridXC * (UniversalConstant._GridYC - 1))
                    bIndex = (UniversalConstant._GridXC * 2) - 1;
            }

#if DEBUG_GRID_BOUNDARY_TOP_BOTTOM

#if DEBUG_GB_1
            for (int i = 0; i < UniversalConstant._cGridX * 2; i++)
            {
                if (i == UniversalConstant._cGridX)
                    debugGrid.Append($"\n");
                debugGrid.Append($" {gridMap[i]} |");
            }
#endif

#if !DEBUG_GB_2
            int startDebugIndex = UniversalConstant._cGridX * (UniversalConstant._cGridY - 1);
            for (int i = startDebugIndex; i < startDebugIndex + UniversalConstant._cGridX; i++)
                debugGrid.Append($" [{i}]: {gridMap[i]} |");
#endif

            Debug.Log("gridMap: \n" + debugGrid.ToString());
#endif


#if DEBUG_GRID_BOUNDARY_LEFT_RIGHT
            for (int bIndex = UniversalConstant._cGridX, bDataIndex = UniversalConstant._cGridX * 2;
                bDataIndex < (UniversalConstant._cGridX * 2) + (UniversalConstant._cGridY - 2) * 2 - 1;       //Avoid top/bottom boudnaries and last cell
                bDataIndex++)
            {
                debugGrid.Append($" [{bIndex}]: {gridMap[bIndex]} |");

                bIndex += UniversalConstant._cGridX;

                if (bIndex == UniversalConstant._cGridX * (UniversalConstant._cGridY - 1))
                    bIndex = UniversalConstant._cGridX - 1;
            }

            Debug.Log("gridMap: \n" + debugGrid.ToString());
#endif

            List<int> addedVehicleTypes = new List<int>();
            Random.InitState(GameManager.Instance.RandomSeed.GetHashCode());
            // Random.InitState(123456);

            int vehicleType, vehicleOrientation, vehicleCount = 0, neighbourX, neighbourY;
            int xDir, yDir;
            Vector3 spawnPos, spawnRot;

#if EMERGENCY_LOOP_EXIT
            int emergencyExit = 0;
#endif

            // 1 cell gap for boundary
#if !SPAWN_LOOP_TEST
            for (gridMapIndex = 0; gridMapIndex < UniversalConstant._GridXC * UniversalConstant._GridYC; gridMapIndex++)
#else
            for (gridMapIndex = 100; gridMapIndex < 122; gridMapIndex++)
#endif
            {
                //Check if the space is occupied or not | Skip if occupied
                if (gridMap[gridMapIndex] != 0
                    || (gridMapIndex % UniversalConstant._GridXC) == 0 || (gridMapIndex % UniversalConstant._GridXC) == (UniversalConstant._GridXC - 1)    //Vertical Gaps
                    || (gridMapIndex / UniversalConstant._GridXC) == 0 || (gridMapIndex / UniversalConstant._GridXC) == (UniversalConstant._GridYC - 1))
                    continue;
#if EMERGENCY_LOOP_EXIT
                else
                {
                    if (emergencyExit >= 250) { Debug.Log($"Emergency Break: {emergencyExit}"); break; }
                    emergencyExit++;
                }
#endif

                xDir = 1; yDir = 1;
                // cellOccupied = false;
                spawnPos = Vector3.zero;
                spawnPos.y = 0.28f;
                spawnRot = Vector3.zero;

#if !SPAWN_LOOP_TEST
                //Choose a random vehicle 
                // 0: Blank | 1-3: Vehicle Index
                vehicleType = Random.Range(0, 4);           //Original

                if (vehicleType == 0) continue;

                //Random Orientation
                //0: Left | 1: Right | 2: Up | 3: Down
                vehicleOrientation = Random.Range(0, 4);         //Original
#else
                // vehicleOrientation = Random.Range(0, 2);         //Original
                vehicleOrientation = 1;                             //Test
                vehicleType = 2;              //Test | Only include small vehicles
                // gridMapIndex = 0;              //Test
#endif
                // Debug.Log($"[CELL CHECK] vehicleType: {vehicleType} | vehicleOrientation: {vehicleOrientation}"
                // + $" | gridMapIndex: {gridMapIndex} | gridMap[gridMapIndex]:{gridMap[gridMapIndex]}");

                //Fill the associated cells: Left,Right,Up,Down accordingly
                //- As we are going left to right from the top
                //  [=] Checking from the top-left of a cell and including other cells should do
                //      as we will never be going the opposite way
                //  [=] Maybe when back-tracking is implemented

                // Subtracting "0.5" as then the car will be sitting inside the border, if not then it will be on the border
                // Also "0.5" is taken as 1 cell is divided into 4 parts, so intervals of "0.5"

                // <------------ {(UniversalConstant._cGridX / 4),(UniversalConstant._cGridY / 4)} ------------> Top-Left placement of a car
                // Any combination done with the above co-odrinates will result in a co-ordinate at the top-left of the current cell

                //Small Vehicle
                /*  Orientations
                        Main                 Top               Bottom             Right              Left
                    |   |   |   |       |   | x | x |      |   |   |   |      |   |   |   |      |   |   |   |  
                    -------------       -------------      -------------      -------------      -------------
                    |   | x |   |       |   | x | x |      |   | x | x |      |   | x | x |      | x | x |   |  
                    -------------       -------------      -------------      -------------      -------------
                    |   |   |   |       |   |   |   |      |   | x | x |      |   | x | x |      | x | x |   |  

                */

                //Medium Vehicle
                /*  Orientations
                            Main                        Top                         Bottom                      Right                       Left
                    |   |   |   |   |   |       |   |   | x | x |   |      |   |   |   |   |   |      |   |   |   |   |   |      |   |   |   |   |   |  
                    ---------------------       ---------------------      ---------------------      ---------------------      ---------------------
                    |   |   |   |   |   |       |   |   | x | x |   |      |   |   |   |   |   |      |   |   |   |   |   |      |   |   |   |   |   |  
                    ---------------------       ---------------------      ---------------------      ---------------------      ---------------------
                    |   |   | x |   |   |       |   |   | x | x |   |      |   |   | x | x |   |      |   |   | x | x | x |      | x | x | x |   |   |  
                    ---------------------       ---------------------      ---------------------      ---------------------      ---------------------
                    |   |   |   |   |   |       |   |   |   |   |   |      |   |   | x | x |   |      |   |   | x | x | x |      | x | x | x |   |   |
                    ---------------------       ---------------------      ---------------------      ---------------------      ---------------------
                    |   |   |   |   |   |       |   |   |   |   |   |      |   |   | x | x |   |      |   |   |   |   |   |      |   |   |   |   |   |  

                */

                //Long Vehicle
                /*  Orientations
                                Main                                Top                                 Bottom                              Right                               Left
                    |   |   |   |   |   |   |   |       |   |   |   | x | x |   |   |      |   |   |   |   |   |   |   |      |   |   |   |   |   |   |   |      |   |   |   |   |   |   |   |  
                    -----------------------------       -----------------------------      -----------------------------      -----------------------------      -----------------------------
                    |   |   |   |   |   |   |   |       |   |   |   | x | x |   |   |      |   |   |   |   |   |   |   |      |   |   |   |   |   |   |   |      |   |   |   |   |   |   |   |  
                    -----------------------------       -----------------------------      -----------------------------      -----------------------------      -----------------------------
                    |   |   |   |   |   |   |   |       |   |   |   | x | x |   |   |      |   |   |   |   |   |   |   |      |   |   |   |   |   |   |   |      |   |   |   |   |   |   |   |  
                    -----------------------------       -----------------------------      -----------------------------      -----------------------------      -----------------------------
                    |   |   |   | x |   |   |   |       |   |   |   | x | x |   |   |      |   |   |   | x | x |   |   |      |   |   |   | x | x | x | x |      | x | x | x | x |   |   |   |  
                    -----------------------------       -----------------------------      -----------------------------      -----------------------------      -----------------------------
                    |   |   |   |   |   |   |   |       |   |   |   |   |   |   |   |      |   |   |   | x | x |   |   |      |   |   |   | x | x | x | x |      | x | x | x | x |   |   |   |
                    -----------------------------       -----------------------------      -----------------------------      -----------------------------      -----------------------------
                    |   |   |   |   |   |   |   |       |   |   |   |   |   |   |   |      |   |   |   | x | x |   |   |      |   |   |   |   |   |   |   |      |   |   |   |   |   |   |   |  
                    -----------------------------       -----------------------------      -----------------------------      -----------------------------      -----------------------------
                    |   |   |   |   |   |   |   |       |   |   |   |   |   |   |   |      |   |   |   | x | x |   |   |      |   |   |   |   |   |   |   |      |   |   |   |   |   |   |   |  
                */

                switch (vehicleOrientation)
                {
                    //Check Left
                    case 0:
                        xDir = -1;
                        spawnRot.y = -90f;

                        //By default, the car will be placed in the left-orientation
                        spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) - (0.25f * (vehicleType - 1)) + (gridMapIndex % UniversalConstant._GridXC * 0.5f);
                        goto case 4;

                    //Check Right
                    case 1:
                        xDir = 1;
                        spawnRot.y = 90f;

                        spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) - (0.25f * (vehicleType - 1)) + (gridMapIndex % UniversalConstant._GridXC * 0.5f) + (0.5f * vehicleType);
                        // Debug.Log($"spawnPos.x: {spawnPos.x} | mod: {(gridMapIndex % UniversalConstant._cGridX)} | top-left: {(UniversalConstant._cGridX / 4.0f * -1.0f)} ");
                        goto case 4;

                    //Check Horizontal Pairs
                    case 4:
                        //Both will be down in Y for horizontal pair
                        spawnPos.z = (UniversalConstant._GridYC / 4.0f) - (gridMapIndex / UniversalConstant._GridXC * 0.5f) - 0.5f;
                        // Debug.Log($"spawnPos.x: {spawnPos.x} | mod: {gridMapIndex / (UniversalConstant._cGridX - 1)} | top-left: {UniversalConstant._cGridY / 4.0f} ");

                        // /*
                        //Check if vehicle can exit the parking lot
                        for (neighbourX = 0, neighbourY = 0; neighbourX < 2; neighbourX++)
                        {
                            // Debug.Log($"neighbourX: {neighbourX} | 0:{(gridMapIndex / UniversalConstant._cGridX) * UniversalConstant._cGridX + ((UniversalConstant._cGridX - 1) * neighbourX)}"
                            //     + $" | 1: {((gridMapIndex / UniversalConstant._cGridX) * UniversalConstant._cGridX) + UniversalConstant._cGridX + ((UniversalConstant._cGridX - 1) * neighbourX)}"
                            //     + $" | grid[0]: {gridMap[((gridMapIndex / UniversalConstant._cGridX) * UniversalConstant._cGridX) + ((UniversalConstant._cGridX - 1) * neighbourX)]}"
                            //     + $" | grid[1]: {gridMap[((gridMapIndex / UniversalConstant._cGridX) * UniversalConstant._cGridX) + UniversalConstant._cGridX + ((UniversalConstant._cGridX - 1) * neighbourX)]}");
                            //Check if gridMapIndex | (gridMapIndex + gridX) has boundary on the same row or not
                            if (gridMap[((gridMapIndex / UniversalConstant._GridXC) * UniversalConstant._GridXC)
                                + ((UniversalConstant._GridXC - 1) * neighbourX)] != 0
                                || gridMap[((gridMapIndex / UniversalConstant._GridXC) * UniversalConstant._GridXC) + UniversalConstant._GridXC
                                + ((UniversalConstant._GridXC - 1) * neighbourX)] != 0)
                                neighbourY++;
                        }

                        // If both sides are blocked then dont continue
                        if (neighbourY > 1)
                        {
                            // Debug.Log($"Skipping Spawn | neighbourY: {neighbourY}");
                            goto case 6;
                        }
                        // */

                        //Check if vehicle can be placed
                        for (neighbourX = 0; neighbourX < (2 + vehicleType); neighbourX++)
                        {
                            //Check the cells down below also
                            for (neighbourY = 0; neighbourY < 2; neighbourY++)
                            {
                                indexToCheck = gridMapIndex + (neighbourX * xDir) + (neighbourY * UniversalConstant._GridXC);
                                // Debug.Log($"[BOUNDS CHECK] (indexToCheck%UniversalConstant._cGridX): {indexToCheck % UniversalConstant._cGridX}"
                                //     + $" | (indexToCheck/UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX}"
                                //     + $" | indexToCheck: {indexToCheck}");

                                //Bounds Check
                                if (indexToCheck < 0 || indexToCheck >= (UniversalConstant._GridXC * UniversalConstant._GridYC)      //Out of Range
                                    || (indexToCheck / UniversalConstant._GridXC) != ((gridMapIndex + (neighbourY * yDir * UniversalConstant._GridXC)) / UniversalConstant._GridXC)      //On the same line check
                                    || (indexToCheck % UniversalConstant._GridXC) == 0 || (indexToCheck % UniversalConstant._GridXC) == (UniversalConstant._GridXC - 1)    //Vertical Gaps
                                    || (indexToCheck / UniversalConstant._GridXC) == 0 || (indexToCheck / UniversalConstant._GridXC) == (UniversalConstant._GridYC - 1))      //Horizontal Gaps
                                {
                                    // Debug.Log($"([OUT OF BOUNDS] gridMapIndex / UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX} | (gridMapIndex % UniversalConstant._cGridX):{indexToCheck % UniversalConstant._cGridX} "
                                    // + $"| Bounds: {indexToCheck}");
                                    goto case 6;
                                }
                                //Cell Filled Check
                                else if (gridMap[indexToCheck] != 0)
                                    goto case 6;
                                // else
                                //     gridMap[indexToCheck] = (byte)vehicleType;
                            }
                        }

                        //Fill the cells
                        for (neighbourX = 0; neighbourX < (2 + vehicleType); neighbourX++)
                        {
                            for (neighbourY = 0; neighbourY < 2; neighbourY++)
                                gridMap[gridMapIndex + (neighbourX * xDir) + (neighbourY * UniversalConstant._GridXC)] = (byte)vehicleType;
                        }

                        _vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[(PoolManager.PoolType)vehicleType].Get().transform);
                        _vehiclesSpawned[vehicleCount].name = $"Vehicle[{vehicleType}]I[{gridMapIndex}]O[{vehicleOrientation}]";
                        _vehiclesSpawned[vehicleCount].position = spawnPos;
                        _vehiclesSpawned[vehicleCount].localEulerAngles = spawnRot;
                        addedVehicleTypes.Add(vehicleType);

                        vehicleCount++;
                        break;

                    //Check up
                    case 2:
                        yDir = -1;
                        spawnRot.y = 0f;

                        spawnPos.z = (UniversalConstant._GridYC / 4.0f) + (0.25f * (vehicleType - 1)) - (gridMapIndex / UniversalConstant._GridXC * 0.5f);
                        goto case 5;

                    // Check down
                    case 3:
                        yDir = 1;
                        spawnRot.y = 180f;

                        spawnPos.z = (UniversalConstant._GridYC / 4.0f) + (0.25f * (vehicleType - 1)) - (gridMapIndex / UniversalConstant._GridXC * 0.5f) - (0.5f * vehicleType);
                        goto case 5;

                    //Check vertical pairs
                    case 5:
                        //Both will be right in X for Vertical pair
                        spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) + (gridMapIndex % UniversalConstant._GridXC * 0.5f) + 0.5f;
                        xDir = 1;

                        //Check if vehicle can exit the parking lot
                        for (neighbourX = 0, neighbourY = 0; neighbourX < 2; neighbourX++)
                        {
                            // Debug.Log($"neighbourX: {neighbourX} | 0:{gridMapIndex % UniversalConstant._cGridX}"
                            //     + $" | 1: {(gridMapIndex + 1) % UniversalConstant._cGridX}");
                            //Check if gridMapIndex | (gridMapIndex + gridX) has boundary on the same row or not
                            if (gridMap[(gridMapIndex % UniversalConstant._GridXC)
                                + (UniversalConstant._GridXC * (UniversalConstant._GridYC - 1) * neighbourX)] != 0
                                || gridMap[((gridMapIndex + 1) % UniversalConstant._GridXC)
                                + (UniversalConstant._GridXC * (UniversalConstant._GridYC - 1) * neighbourX)] != 0)
                                neighbourY++;
                        }

                        // If both sides are blocked then dont continue
                        if (neighbourY > 1)
                            goto case 6;

                        //Check if vehicle can be placed
                        for (neighbourX = 0; neighbourX < 2; neighbourX++)
                        {
                            //Check the cells down below also
                            for (neighbourY = 0; neighbourY < (2 + vehicleType); neighbourY++)
                            {
                                indexToCheck = gridMapIndex + (neighbourX * xDir) + (neighbourY * yDir * UniversalConstant._GridXC);

                                // Debug.Log($"[BOUNDS CHECK] (indexToCheck%UniversalConstant._cGridX): {indexToCheck % UniversalConstant._cGridX}"
                                //     + $" | (indexToCheck/UniversalConstant._cGridX): {indexToCheck / UniversalConstant._cGridX}"
                                //     + $" | indexToCheck: {indexToCheck}");

                                //Bounds Check
                                // - No matter what the value, this (indexToCheck % UniversalConstant._cGridX) will always be between (0 - UniversalConstant._cGridX)
                                if (indexToCheck < 0 || indexToCheck >= (UniversalConstant._GridXC * UniversalConstant._GridYC)      //Out of Range
                                    || (indexToCheck / UniversalConstant._GridXC) != ((gridMapIndex + (neighbourY * yDir * UniversalConstant._GridXC)) / UniversalConstant._GridXC)      //On the same line check
                                    || (indexToCheck % UniversalConstant._GridXC) == 0 || (indexToCheck % UniversalConstant._GridXC) == (UniversalConstant._GridXC - 1)    //Vertical Gaps
                                    || (indexToCheck / UniversalConstant._GridXC) == 0 || (indexToCheck / UniversalConstant._GridXC) == (UniversalConstant._GridYC - 1))      //Horizontal Gaps
                                {
                                    // Debug.Log($"[OUT OF BOUNDS] indexToCheck:{indexToCheck}");
                                    goto case 6;
                                }
                                //Cell Filled Check
                                else if (gridMap[indexToCheck] != 0)
                                    goto case 6;
                            }
                        }

                        //Fill the cells
                        for (neighbourX = 0; neighbourX < 2; neighbourX++)
                        {
                            for (neighbourY = 0; neighbourY < (2 + vehicleType); neighbourY++)
                                gridMap[gridMapIndex + (neighbourX * xDir) + (neighbourY * yDir * UniversalConstant._GridXC)] = (byte)vehicleType;
                        }

                        _vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[(PoolManager.PoolType)vehicleType].Get().transform);
                        _vehiclesSpawned[vehicleCount].name = $"Vehicle[{vehicleType}]I[{gridMapIndex}]O[{vehicleOrientation}]";
                        _vehiclesSpawned[vehicleCount].position = spawnPos;
                        _vehiclesSpawned[vehicleCount].localEulerAngles = spawnRot;
                        addedVehicleTypes.Add(vehicleType);

                        vehicleCount++;
                        break;

                    //Cell Occupied | Out Of Bounds
                    case 6:
                        continue;

                    default:
                        Debug.LogError($"Vehicle Spawner | Wrong Vehicle Orientation: {vehicleOrientation} | VehicleType: {vehicleType}");
                        continue;
                }
                //Check the validity of the random vehicle | If the vehicle can escape from the parking lot or not

                // If true, then place the vehicle
                // If false, then choose another vehicle | leave the spot empty

                await Task.Yield();
            }

            Debug.Log($"Spawning Finished");
            onVehiclesSpawned?.Invoke(addedVehicleTypes.ToArray());
        }

        //Test if vehicles spawn within bounds
        public void SpanwVehiclesTest()
        {
            Debug.Log($"Spawning Test Vehicles | gridMap[{UniversalConstant._GridXC}x{UniversalConstant._GridYC}] | Size: {UniversalConstant._GridXC * UniversalConstant._GridYC}");
            //Create a grid of 22 x 42 cells
            byte[] gridMap = new byte[UniversalConstant._GridXC * UniversalConstant._GridYC];

            int gridMapIndex = 0, vehicleCount = 0;
            //Initialize Array to all spots being empty
            for (; gridMapIndex < gridMap.Length; gridMapIndex++)
                gridMap[gridMapIndex] = 0;

            Vector3 spawnPos, spawnRot;
            int vehicleType;

            for (gridMapIndex = 0; gridMapIndex < UniversalConstant._GridXC * UniversalConstant._GridYC; gridMapIndex++)
            {
                if (gridMapIndex % UniversalConstant._GridXC != 0) continue;

                spawnPos = Vector3.zero;
                spawnRot = Vector3.zero;

                vehicleType = 1;              //Test | Only include small vehicles

                spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) + (gridMapIndex % UniversalConstant._GridXC * 0.5f) + 0.25f;
                spawnPos.z = (UniversalConstant._GridYC / 4.0f) - (gridMapIndex / UniversalConstant._GridXC * 0.5f) - 0.25f;   // + (UniversalConstant._cGridY / 4) - 0.5f;


                _vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[(PoolManager.PoolType)vehicleType].Get().transform);
                _vehiclesSpawned[vehicleCount].name = $"Vehicle[{vehicleType}]_{vehicleCount}";
                _vehiclesSpawned[vehicleCount].position = spawnPos;
                _vehiclesSpawned[vehicleCount].localEulerAngles = spawnRot;
                _vehiclesSpawned[vehicleCount].localScale *= 0.5f;

                vehicleCount++;
            }
        }

        private void CheckIfValidPlacement(int topLeftIndex, PoolManager.PoolType vehicleType)
        {
        }
    }
}