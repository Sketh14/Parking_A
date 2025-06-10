#define BOUNDARY_SWAP_DEBUG

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

        public void SpawnNpcs(byte[] boundaryData, Action OnNpcsSpawned)
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

#if BOUNDARY_SWAP_DEBUG
            debugBoundary.Clear();
            for (int i = boundaryData.Length - 1; i >= 0; i--)
                debugBoundary.Append($"{i}[{boundaryData[i]}] ,");
            Debug.Log($"Before Swap : {debugBoundary}");
#endif

            byte tempBData = 255;
            // First swap Right | Left, then swap Right | Down
            for (int i = UniversalConstant._GridXC * 2; i < UniversalConstant._GridYC; i--)
            {
                boundaryData[i] = boundaryData[i - UniversalConstant._GridYC];
            }

#if BOUNDARY_SWAP_DEBUG
            debugBoundary.Clear();
            for (int i = boundaryData.Length - 1; i >= 0; i--)
                debugBoundary.Append($"{i}[{boundaryData[i]}] ,");
            Debug.Log($"After Swap : {debugBoundary}");
#endif
            return;

            int npcCount = 0, emptyCell = 0;
            // Top / Bottom grid cells
            for (int bIndex = 0; bIndex < UniversalConstant._GridXC * 2; bIndex++)
            {
                //Check if the cell is empty
                if (boundaryData[bIndex] != 0)
                    continue;

                emptyCell++;
                // Should be such that, the NPCs spawn at the four corners mostly
                // No 2 NPCs at the same corner
                if (emptyCell >= _minGapC)
                {

                }

                // await Task.Yield();
            }

            // Left / Right grid cells
            for (int bDataIndex = UniversalConstant._GridXC * 2;
                bDataIndex < (UniversalConstant._GridXC * 2) + (UniversalConstant._GridYC - 2) * 2 - 1;       //Avoid top/bottom boudnaries and last cell
                bDataIndex++)
            {
                //Check if the cell is empty
                if (boundaryData[bDataIndex] != 0)
                    continue;

                emptyCell++;
                if (emptyCell >= _minGapC)
                {

                }

                // await Task.Yield();
            }
        }
    }
}