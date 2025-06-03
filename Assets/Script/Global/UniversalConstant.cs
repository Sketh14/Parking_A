using UnityEngine;

namespace Parking_A.Global
{
    public class UniversalConstant
    {
        public enum SceneIndex { MAIN_MENU = 0, MAIN_GAMEPLAY = 1 }
        public enum GameStatus { BOUNDARY_GENERATION = 0, VEHICLE_SPAWNING = 1 << 0, LEVEL_GENERATED = 1 << 1 }

        public const byte _GridXC = 22, _GridYC = 42;
    }
}