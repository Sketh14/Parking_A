// #define BOUNDARY_SWAP_DEBUG
// #define NPC_SPAWN_TEST

using UnityEngine;

using System.Collections.Generic;
using System;
using Parking_A.Global;
using System.Threading.Tasks;
using System.Threading;

namespace Parking_A.Gameplay
{
    [Serializable]
    public class NPCSpawner
    {
        internal enum NPCStatus
        {
            SPAWNED_HORIZONTAL_UP = 1 << 0,
            SPAWNED_VERTICAL_RIGHT = 1 << 1,
            SPAWNED_HORIZONTAL_DOWN = 1 << 2,
            SPAWNED_VERTICAL_LEFT = 1 << 3,
            TOTAL_SPAWNED = 1 << 4,
        }

        private List<Transform> _npcsSpawned;
        public List<Transform> NPCsSpawned { get => _npcsSpawned; }

        private NPCStatus _npcSpawnStatus = 0;

        private const int _totalNPCsCountC = 4;
        private const int _minGapC = 4;

        private CancellationTokenSource _cts;
        ~NPCSpawner()
        {
            _cts.Cancel();
        }

        public NPCSpawner()
        {
            _cts = new CancellationTokenSource();
            // TestSwapping();
            // TestSwapping2();
            _npcsSpawned = new List<Transform>();
        }

        public void ClearNPCs()
        {
            _npcsSpawned.Clear();
        }

        public async Task SpawnNpcs(byte[] boundaryData, Action OnNpcsSpawned)
        // public void SpawnNpcs(byte[] boundaryData, Action OnNpcsSpawned)
        {
            // Would not need a whole gridmap as we already have the boundary data
            // Find holes in the boundary
            // Select a hole in random
            // Make sure the hole is wide enough
            // Fill the hole with 1/2 npc

            // Debug.Log($"Spawning NPCs | gridMap[{UniversalConstant._GridXC}x{UniversalConstant._GridYC}] | Size: {UniversalConstant._GridXC * UniversalConstant._GridYC}");

            // Making the boundaryData circular
            // - Original data is in the form [Up -> Down -> Left -> Right]
            // - To make it easy, we will make it [Up -> Right -> Down -> Left], in a clockwise direction
            // - After that, we can search for min-gaps even if 1 side does not contain enough empty cells

            #region Boundary_Swap
#if BOUNDARY_SWAP_DEBUG
            System.Text.StringBuilder debugBoundary = new System.Text.StringBuilder();
#endif

            byte tempBData = 255;
            // First Flip Left
            for (int i = UniversalConstant.GRID_X * 2, j = 1;
                i < ((UniversalConstant.GRID_X * 2) + (UniversalConstant.GRID_Y - 2) / 2); i++, j++)
            {
                tempBData = boundaryData[i];
                boundaryData[i] = boundaryData[(UniversalConstant.GRID_X * 2) + (UniversalConstant.GRID_Y - 2) - j];
                boundaryData[(UniversalConstant.GRID_X * 2) + (UniversalConstant.GRID_Y - 2) - j] = tempBData;
            }

            // Flip Down
            // /*
            for (int i = UniversalConstant.GRID_X, j = 1; i < UniversalConstant.GRID_X + (UniversalConstant.GRID_X / 2); i++, j++)
            {
                tempBData = boundaryData[i];
                boundaryData[i] = boundaryData[(UniversalConstant.GRID_X * 2) - j];
                boundaryData[(UniversalConstant.GRID_X * 2) - j] = tempBData;
            }
            // */

            // First swap Right | Left
            for (int i = UniversalConstant.GRID_X * 2; i < (UniversalConstant.GRID_X * 2) + (UniversalConstant.GRID_Y - 2); i++)
            {
                tempBData = boundaryData[i];
                boundaryData[i] = boundaryData[i + (UniversalConstant.GRID_Y - 2)];
                boundaryData[i + (UniversalConstant.GRID_Y - 2)] = tempBData;
            }

#if BOUNDARY_SWAP_DEBUG
            debugBoundary.Clear();
            for (int i = UniversalConstant._GridXC; i < (UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2); i++)
                debugBoundary.Append($"{i}[{boundaryData[i]}] ,");
            Debug.Log($"Before Swap : {debugBoundary}");
#endif

            // Then swap Right | Down
            // Since Upper Limit is even, then this will work, else need to swap the first items also within themselves
            /*
            //  For odd cases
                1 | 2 | 3           ->  a | b | c           ->  a | b | c           ->  a | b | c           ->  a | b | c           ->  a | b | c
                a | b | c | d | e       1 | 2 | 3 | d | e       d | 2 | 3 | 1 | e       d | e | 3 | 1 | 2       d | e | 2 | 1 | 3       d | e | 1 | 2 | 3
            */
            /*for (int i = UniversalConstant._GridXC * 2; i < (UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2); i++)
            {
                tempBData = boundaryData[i];
                boundaryData[i] = boundaryData[i - UniversalConstant._GridXC];
                boundaryData[i - UniversalConstant._GridXC] = tempBData;
            }*/

            for (int i = UniversalConstant.GRID_X * 2; i < (UniversalConstant.GRID_X * 2) + (UniversalConstant.GRID_Y - 2); i++)
            {
                for (int j = i; j > i - UniversalConstant.GRID_X; j--)
                {
                    tempBData = boundaryData[j];
                    boundaryData[j] = boundaryData[j - 1];
                    boundaryData[j - 1] = tempBData;
                }
            }

#if BOUNDARY_SWAP_DEBUG
            debugBoundary.Clear();
            for (int i = 0; i < boundaryData.Length; i++)
                debugBoundary.Append($"{i}[{boundaryData[i]}] ,");
            Debug.Log($"After Swap : {debugBoundary}");
#endif
            #endregion Boundary_Swap

            int npcCount = 0, emptyCellCount = 0;
            GameObject npc;
            Vector3 spawnPos, spawnRot;
            System.Text.StringBuilder npcName = new System.Text.StringBuilder();

#if NPC_SPAWN_TEST
            int tempIndex = 66 - 62;         //Horizontal
            // int tempIndex = 85 - 84;            //Vertical
            npc = PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.NPC].Get();
            npc.name = $"NPC[{tempIndex}]";
            spawnPos = Vector3.zero;

            //Horizontal
            // spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) + 0.25f 
            //      + (tempIndex % UniversalConstant._GridXC * (UniversalConstant._CellHalfSizeC * 2));
            spawnPos.x = (UniversalConstant._GridXC / 4.0f) - 0.25f
                 - (tempIndex % UniversalConstant._GridXC * (UniversalConstant._CellHalfSizeC * 2));
            spawnPos.z = (UniversalConstant._GridYC / 4.0f * -1.0f) + UniversalConstant._CellHalfSizeC;

            //Vertical
            // spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) + UniversalConstant._CellHalfSizeC;
            // spawnPos.z = (UniversalConstant._GridYC / 4.0f) - (UniversalConstant._CellHalfSizeC * 3)            //Offset as ignoring top/bottom row
            //         - (tempIndex % UniversalConstant._GridYC * (UniversalConstant._CellHalfSizeC * 2));

            npc.transform.position = spawnPos;
            npc.transform.rotation = Quaternion.identity;
#endif

            // return;
            // Debug.Log($"Horiontal Down Start | {UniversalConstant._GridXC + UniversalConstant._GridYC - 2}");

            #region Record_Empty_Cell

            // Record all the empty cells above the gap threshold
            int[] emptyGapArr = new int[10];
            int tempSortNum = 0;
            for (int bIndex = 0, gapIndex = 0; bIndex < boundaryData.Length - 1; bIndex++)        //Last cell is empty and out-of-index
            {
                //Check if the cell is empty
                if (boundaryData[bIndex] != 0)
                {
                    if (emptyCellCount >= _minGapC && gapIndex < emptyGapArr.Length)
                    {
                        emptyGapArr[gapIndex] = (bIndex - 1) * 1000 + emptyCellCount;           //Record count also

                        // Simultaneously sort the emptyGapArr in Descending Order, so that later becomes easier to select
                        for (int i = gapIndex; i > 0; i--)
                        {
                            if ((emptyGapArr[i] % 1000) > (emptyGapArr[i - 1] % 1000))
                            {
                                tempSortNum = emptyGapArr[i];
                                emptyGapArr[i] = emptyGapArr[i - 1];
                                emptyGapArr[i - 1] = tempSortNum;
                            }
                        }

                        gapIndex++;
                    }
                    emptyCellCount = 0;
                    continue;
                }

                emptyCellCount++;
            }

            // System.Text.StringBuilder debugGapString = new System.Text.StringBuilder();
            for (int gapIndex = 0; gapIndex < emptyGapArr.Length; gapIndex++)
            {
                emptyGapArr[gapIndex] /= 1000;
                // debugGapString.Append($"{emptyGapArr[gapIndex]}, ");
            }

            // Debug.Log($"Empty Cells Found: {debugGapString}");
            #endregion Record_Empty_Cell

            // /*
            for (int gapIndex = 0; gapIndex < emptyGapArr.Length && npcCount < _totalNPCsCountC; gapIndex++)
            {
                // int gapIndex = 2;            //Use this to test only 1 vehicle | Comment upper for loop
                spawnPos = spawnRot = Vector3.zero;
                spawnPos.y = 0.1f;
                // Should be such that, the NPCs spawn at the four corners mostly
                // No 2 NPCs at the same corner


                // Vertical | Left [84 - 125]
                if (emptyGapArr[gapIndex] >= (UniversalConstant.GRID_X * 2) + UniversalConstant.GRID_Y - 2)
                {
                    spawnPos.x = (UniversalConstant.GRID_X / 4.0f * -1.0f) + UniversalConstant.HALF_CELL_SIZE;
                    spawnPos.z = (UniversalConstant.GRID_Y / 4.0f * -1.0f) + (UniversalConstant.HALF_CELL_SIZE * 3)            //Offset as ignoring top/bottom row
                        + ((emptyGapArr[gapIndex] - ((UniversalConstant.GRID_X * 2) + UniversalConstant.GRID_Y - 2))
                        % UniversalConstant.GRID_Y * (UniversalConstant.HALF_CELL_SIZE * 2));

                    spawnRot.y = 0f;

                    _npcSpawnStatus |= NPCStatus.SPAWNED_VERTICAL_LEFT;
                    _npcSpawnStatus |= NPCStatus.TOTAL_SPAWNED;
                    npcName.Clear();
                    npcName.Append($"NPC[{emptyGapArr[gapIndex]}]_[{npcCount}]_V");
                    // npc.name = $"NPC[{bIndex}]_V";
                }
                // Horizontal | Down [62 - 83]
                else if (emptyGapArr[gapIndex] >= UniversalConstant.GRID_X + UniversalConstant.GRID_Y - 2)
                {
                    spawnPos.x = (UniversalConstant.GRID_X / 4.0f) - 0.25f
                        - ((emptyGapArr[gapIndex] - (UniversalConstant.GRID_X + UniversalConstant.GRID_Y - 2))
                        % UniversalConstant.GRID_X * (UniversalConstant.HALF_CELL_SIZE * 2));
                    spawnPos.z = (UniversalConstant.GRID_Y / 4.0f * -1.0f) + UniversalConstant.HALF_CELL_SIZE;

                    spawnRot.y = 270f;

                    _npcSpawnStatus |= NPCStatus.SPAWNED_HORIZONTAL_DOWN;
                    npcName.Clear();
                    npcName.Append($"NPC[{emptyGapArr[gapIndex]}]_[{npcCount}]_H");
                    // npc.name = $"NPC[{bIndex}]_H";
                }
                // Vertical | Right [22 - 61]
                else if (emptyGapArr[gapIndex] >= UniversalConstant.GRID_X)
                {
                    spawnPos.x = (UniversalConstant.GRID_X / 4.0f) - UniversalConstant.HALF_CELL_SIZE;
                    spawnPos.z = (UniversalConstant.GRID_Y / 4.0f) - (UniversalConstant.HALF_CELL_SIZE * 3)            //Offset as ignoring top/bottom row
                        - ((emptyGapArr[gapIndex] - UniversalConstant.GRID_X)
                        % UniversalConstant.GRID_Y * (UniversalConstant.HALF_CELL_SIZE * 2));

                    spawnRot.y = 180f;

                    _npcSpawnStatus |= NPCStatus.SPAWNED_VERTICAL_RIGHT;
                    // _npcSpawnStatus |= NPCStatus.TOTAL_SPAWNED;                 //TESTING
                    npcName.Clear();
                    npcName.Append($"NPC[{emptyGapArr[gapIndex]}]_[{npcCount}]_V");
                    // npc.name = $"NPC[{bIndex}]_V";
                }
                // Horizontal | Up [0 - 21]
                else
                {
                    spawnPos.x = (UniversalConstant.GRID_X / 4.0f * -1.0f) + 0.25f
                        + (emptyGapArr[gapIndex] % UniversalConstant.GRID_X * (UniversalConstant.HALF_CELL_SIZE * 2));
                    spawnPos.z = (UniversalConstant.GRID_Y / 4.0f) - UniversalConstant.HALF_CELL_SIZE;

                    spawnRot.y = 90f;

                    _npcSpawnStatus |= NPCStatus.SPAWNED_HORIZONTAL_UP;
                    npcName.Clear();
                    npcName.Append($"NPC[{emptyGapArr[gapIndex]}]_[{npcCount}]_H");
                    // npc.name = $"NPC[{bIndex}]_H";
                }

                npc = PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.NPC].Get();
                _npcsSpawned.Add(npc.transform);
                npc.transform.position = spawnPos;
                npc.transform.localEulerAngles = spawnRot;
                npc.name = npcName.ToString();
                npcCount++;

                // Debug.Log($"{npc.name} | Pos: {npc.transform.position} | bIndex: {bIndex}");
                await Task.Yield();
                if (_cts.IsCancellationRequested) return;
            }
            // */

            Debug.Log($"Spawning NPCs Finished");
            OnNpcsSpawned?.Invoke();
        }

        private void TestSwapping()
        {

            System.Text.StringBuilder debugBoundary = new System.Text.StringBuilder();

            char tempBData = ' ';

            char[] testArr = new char[] { '1', '2', '3', '4', '5', '6', '7', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i' };

            int interval = 7;
            debugBoundary.Clear();
            for (int i = 0; i < testArr.Length; i++)
                debugBoundary.Append($"{i}[{testArr[i]}] ,");
            Debug.Log($"Before Swap : {debugBoundary}");

            // Then swap Right | Down
            // Since Upper Limit is even, then this will work, else need to swap the first items also within themselves
            /*
            //  For odd cases
                1 | 2 | 3           ->  a | b | c           ->  a | b | c           ->  a | b | c           ->  a | b | c           ->  a | b | c
                a | b | c | d | e       1 | 2 | 3 | d | e       d | 2 | 3 | 1 | e       d | e | 3 | 1 | 2       d | e | 2 | 1 | 3       d | e | 1 | 2 | 3
            */
            for (int i = interval; i < testArr.Length; i++)
            {
                for (int j = i; j > i - interval; j--)
                {

                    tempBData = testArr[j - 1];
                    testArr[j - 1] = testArr[j];
                    testArr[j] = tempBData;
                }
            }

            debugBoundary.Clear();
            for (int i = 0; i < testArr.Length; i++)
                debugBoundary.Append($"{i}[{testArr[i]}] ,");
            Debug.Log($"After Swap : {debugBoundary}");
        }

        private void TestSwapping2()
        {

            System.Text.StringBuilder debugBoundary = new System.Text.StringBuilder();

            char tempBData = ' ';

            char[] testArr = new char[] { '1', '2', '3', '4', '5', '6', '7', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i' };

            // int interval = 7;
            debugBoundary.Clear();
            for (int i = 0; i < testArr.Length; i++)
                debugBoundary.Append($"{i}[{testArr[i]}] ,");
            Debug.Log($"Before Swap : {debugBoundary}");

            // Then swap Right | Down
            // Since Upper Limit is even, then this will work, else need to swap the first items also within themselves
            /*
            //  For odd cases
                1 | 2 | 3           ->  a | b | c           ->  a | b | c           ->  a | b | c           ->  a | b | c           ->  a | b | c
                a | b | c | d | e       1 | 2 | 3 | d | e       d | 2 | 3 | 1 | e       d | e | 3 | 1 | 2       d | e | 2 | 1 | 3       d | e | 1 | 2 | 3
            */

            for (int i = 0, j = 1; i < testArr.Length / 2; i++, j++)
            {
                tempBData = testArr[i];
                testArr[i] = testArr[testArr.Length - j];
                testArr[testArr.Length - j] = tempBData;
            }

            debugBoundary.Clear();
            for (int i = 0; i < testArr.Length; i++)
                debugBoundary.Append($"{i}[{testArr[i]}] ,");
            Debug.Log($"After Swap : {debugBoundary}");
        }

    }
}