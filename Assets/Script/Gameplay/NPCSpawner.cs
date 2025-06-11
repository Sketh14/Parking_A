// #define BOUNDARY_SWAP_DEBUG
// #define NPC_SPAWN_TEST

using UnityEngine;

using System.Collections.Generic;
using System;
using Parking_A.Global;
using System.Threading.Tasks;

namespace Parking_A.Gameplay
{
    public class NPCSpawner
    {
        private List<Transform> _npcsSpawned;
        public List<Transform> NPCsSpawned { get => _npcsSpawned; }

        private const int _totalNPCsCountC = 4;
        private const int _minGapC = 3;

        /*
        public NPCSpawner()
        {
            // TestSwapping();
            TestSwapping2();
        }
        // */

        public async Task SpawnNpcs(byte[] boundaryData, Action OnNpcsSpawned)
        // public void SpawnNpcs(byte[] boundaryData, Action OnNpcsSpawned)
        {
            // Would not need a whole gridmap as we already have the boundary data
            // Find holes in the boundary
            // Select a hole in random
            // Make sure the hole is wide enough
            // Fill the hole with 1/2 npc

            Debug.Log($"Spawning NPCs | gridMap[{UniversalConstant._GridXC}x{UniversalConstant._GridYC}] | Size: {UniversalConstant._GridXC * UniversalConstant._GridYC}");

            // Making the boundaryData circular
            // - Original data is in the form [Up -> Down -> Left -> Right]
            // - To make it easy, we will make it [Up -> Right -> Down -> Left], in a clockwise direction
            // - After that, we can search for min-gaps even if 1 side does not contain enough empty cells

#if BOUNDARY_SWAP_DEBUG
            System.Text.StringBuilder debugBoundary = new System.Text.StringBuilder();
#endif

            byte tempBData = 255;
            // First Flip Left
            for (int i = UniversalConstant._GridXC * 2, j = 1;
                i < ((UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2) / 2); i++, j++)
            {
                tempBData = boundaryData[i];
                boundaryData[i] = boundaryData[(UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2) - j];
                boundaryData[(UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2) - j] = tempBData;
            }

            // Flip Down
            // /*
            for (int i = UniversalConstant._GridXC, j = 1; i < UniversalConstant._GridXC + (UniversalConstant._GridXC / 2); i++, j++)
            {
                tempBData = boundaryData[i];
                boundaryData[i] = boundaryData[(UniversalConstant._GridXC * 2) - j];
                boundaryData[(UniversalConstant._GridXC * 2) - j] = tempBData;
            }
            // */

            // First swap Right | Left
            for (int i = UniversalConstant._GridXC * 2; i < (UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2); i++)
            {
                tempBData = boundaryData[i];
                boundaryData[i] = boundaryData[i + (UniversalConstant._GridYC - 2)];
                boundaryData[i + (UniversalConstant._GridYC - 2)] = tempBData;
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

            for (int i = UniversalConstant._GridXC * 2; i < (UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2); i++)
            {
                for (int j = i; j > i - UniversalConstant._GridXC; j--)
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

            int npcCount = 0, emptyCell = 0;
            GameObject npc;
            Vector3 spawnPos;

#if NPC_SPAWN_TEST
            int tempIndex = 66 - 62;         //Horizontal
            // int tempIndex = 85 - 84;            //Vertical
            npc = PoolManager.Instance.PrefabPool[PoolManager.PoolType.NPC].Get();
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

            // Top / Bottom grid cells
            for (int bIndex = 0; bIndex < boundaryData.Length - 1; bIndex++)
            {
                //Check if the cell is empty
                if (boundaryData[bIndex] != 0)
                {
                    emptyCell = 0;
                    continue;
                }

                emptyCell++;
                spawnPos = Vector3.zero;
                // Should be such that, the NPCs spawn at the four corners mostly
                // No 2 NPCs at the same corner
                if (emptyCell >= _minGapC)
                {
                    //Record current index somewhere to be used later?
                    emptyCell = 0;

                    npc = PoolManager.Instance.PrefabPool[PoolManager.PoolType.NPC].Get();
                    npc.name = $"NPC[{bIndex}]";

                    // Vertical | Left [84 - 125]
                    if (bIndex >= (UniversalConstant._GridXC * 2) + UniversalConstant._GridYC - 2)
                    {
                        spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) + UniversalConstant._CellHalfSizeC;
                        spawnPos.z = (UniversalConstant._GridYC / 4.0f * -1.0f) + (UniversalConstant._CellHalfSizeC * 3)            //Offset as ignoring top/bottom row
                            + ((bIndex - ((UniversalConstant._GridXC * 2) + UniversalConstant._GridYC - 2))
                            % UniversalConstant._GridYC * (UniversalConstant._CellHalfSizeC * 2));
                    }
                    // Horizontal | Down [62 - 83]
                    else if (bIndex >= UniversalConstant._GridXC + UniversalConstant._GridYC - 2)
                    {
                        spawnPos.x = (UniversalConstant._GridXC / 4.0f) - 0.25f
                            - ((bIndex - (UniversalConstant._GridXC + UniversalConstant._GridYC - 2))
                            % UniversalConstant._GridXC * (UniversalConstant._CellHalfSizeC * 2));
                        spawnPos.z = (UniversalConstant._GridYC / 4.0f * -1.0f) + UniversalConstant._CellHalfSizeC;
                    }
                    // Vertical | Right [22 - 61]
                    else if (bIndex >= UniversalConstant._GridXC)
                    {
                        spawnPos.x = (UniversalConstant._GridXC / 4.0f) - UniversalConstant._CellHalfSizeC;
                        spawnPos.z = (UniversalConstant._GridYC / 4.0f) - (UniversalConstant._CellHalfSizeC * 3)            //Offset as ignoring top/bottom row
                            - ((bIndex - UniversalConstant._GridXC)
                            % UniversalConstant._GridYC * (UniversalConstant._CellHalfSizeC * 2));
                    }
                    // Horizontal | Up [0 - 21]
                    else
                    {
                        spawnPos.x = (UniversalConstant._GridXC / 4.0f * -1.0f) + 0.25f
                            + (bIndex % UniversalConstant._GridXC * (UniversalConstant._CellHalfSizeC * 2));
                        spawnPos.z = (UniversalConstant._GridYC / 4.0f) - UniversalConstant._CellHalfSizeC;
                    }

                    npc.transform.position = spawnPos;
                    npc.transform.rotation = Quaternion.identity;
                }

                // await Task.Yield();
            }
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