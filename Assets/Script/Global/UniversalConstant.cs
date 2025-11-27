using UnityEngine;

namespace Parking_A.Global
{
    public class UniversalConstant
    {
        public enum SceneIndex { MAIN_MENU = 0, MAIN_GAMEPLAY = 1 }
        public enum GameStatus
        {
            UNINITIALIZED = 0,
            BOUNDARY_GENERATED = 1 << 0,
            VEHICLE_SPAWNED = 1 << 1,
            NPC_SPAWNED = 1 << 2,
            LEVEL_GENERATED = 1 << 3,
            NPC_HIT = 1 << 4,
            LEVEL_FAILED = 1 << 5,
            RESET_LEVEL = 1 << 6,
            LEVEL_PASSED = 1 << 7,
            NEXT_LEVEL_REQUESTED = 1 << 8,
            VEHICLE_UNLOADED = 1 << 9,
            LOAD_NEXT_LEVEL = 1 << 10,
        }
        public enum PoolType { BLANK = 0, VEHICLE_S = 1, VEHICLE_M, VEHICLE_L, BOUNDARY, NPC }

        public const byte GRID_X = 22, GRID_Y = 42;
        public const float HALF_CELL_SIZE = 0.25f;
        public const int COINS_RECEIVED = 5;
    }
}