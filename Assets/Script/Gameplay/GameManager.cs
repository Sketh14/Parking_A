using Parking_A.Global;
using UnityEngine;

namespace Parking_A.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager _instance;
        public static GameManager Instance { get => _instance; }

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(this.gameObject);

            RandomSeed = "";

        }
        #endregion Singleton

        public UniversalConstant.GameStatus SetGameStatus(UniversalConstant.GameStatus gameStatus)
        {
            _gameStatus |= gameStatus;

            if ((_gameStatus & UniversalConstant.GameStatus.VEHICLE_SPAWNED) != 0
                && (_gameStatus & UniversalConstant.GameStatus.NPC_SPAWNED) != 0)
                _gameStatus |= UniversalConstant.GameStatus.LEVEL_GENERATED;

            return GameStatus;
        }
        private UniversalConstant.GameStatus _gameStatus;


        public string RandomSeed;
        public UniversalConstant.GameStatus GameStatus
        {
            get => _gameStatus;
        }
        public System.Action<int, Vector2> OnSelect;
        public System.Action OnVehiclesSpawned;
        public System.Action<int> OnNPCHit;
        public System.Action<byte[]> OnEnvironmentSpawned;
    }
}