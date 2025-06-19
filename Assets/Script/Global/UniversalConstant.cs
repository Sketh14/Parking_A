using UnityEngine;

namespace Parking_A.Global
{
    public class UniversalConstant
    {
        public enum SceneIndex { MAIN_MENU = 0, MAIN_GAMEPLAY = 1 }
        public enum GameStatus
        {
            BOUNDARY_GENERATED = 0,
            VEHICLE_SPAWNED = 1 << 0,
            NPC_SPAWNED = 1 << 1,
            LEVEL_GENERATED = 1 << 2,
            NPC_HIT = 1 << 3,
            LEVEL_FAILED = 1 << 4,
            RESET_LEVEL = 1 << 5,
            LEVEL_PASSED = 1 << 6,
            NEXT_LEVEL_REQUESTED = 1 << 7,
            VEHICLE_UNLOADED = 1 << 8,
            LOAD_NEXT_LEVEL = 1 << 9,
        }
        public enum PoolType { BLANK = 0, VEHICLE_S = 1, VEHICLE_M, VEHICLE_L, BOUNDARY, NPC }

        public const byte _GridXC = 22, _GridYC = 42;
        public const float _CellHalfSizeC = 0.25f;
    }
}