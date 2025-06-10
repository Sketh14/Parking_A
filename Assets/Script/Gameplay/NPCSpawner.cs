using UnityEngine;

using System.Collections.Generic;
using System;

namespace Parking_A.Gameplay
{
    public class NPCSpawner
    {
        private List<Transform> _npcsSpawned;
        public List<Transform> NPCsSpawned { get => _npcsSpawned; }

        public async void SpawnNpcs(byte[] boundaryData, Action OnNpcsSpawned)
        {
            // Would not need a whole gridmap as we already have the boundary data
            // Find holes in the boundary
            // Select a hole in random
            // Make sure the hole is wide enough
            // Fill the hole with 1/2 npc
        }
    }
}