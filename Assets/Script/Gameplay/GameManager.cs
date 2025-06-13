using Parking_A.Global;
using UnityEngine;

using DateTime = System.DateTime;

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

        [SerializeField] private GameConfigScriptableObject _mainGameConfig;
        private UniversalConstant.GameStatus _gameStatus;


        public string RandomSeed;
        public UniversalConstant.GameStatus GameStatus
        {
            get => _gameStatus;
        }
        public System.Action<int, Vector2> OnSelect;
        public System.Action<int> OnNPCHit;
        public System.Action<UniversalConstant.GameStatus> OnGameStatusChange;
        public System.Action<byte[]> OnEnvironmentSpawned;

        private void Start()
        {
            InitializeLevel();
        }

        private async void InitializeLevel()
        {
            if (_mainGameConfig.RandomizeLevel)
            {
                string tempRandomSeed = DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString()
                    + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString();
                RandomSeed = tempRandomSeed;
                Debug.Log($"Selected Random Seed: {tempRandomSeed}");
            }
            else
                RandomSeed = "SKETH";

            EnvironmentSpawner envSpawner = new EnvironmentSpawner();

            try
            {
                // await envSpawner.SpawnBoundary((values) => boundaryData = values);
                await envSpawner.SpawnBoundary();
            }
            //Cannot initialize boundary | Stop level generation, show some message and restart
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            // GameManager.Instance.GameStatus |= Global.UniversalConstant.GameStatus.BOUNDARY_GENERATION;
            SetGameStatus(UniversalConstant.GameStatus.BOUNDARY_GENERATED);
        }

        public UniversalConstant.GameStatus SetGameStatus(UniversalConstant.GameStatus gameStatus)
        {
            _gameStatus |= gameStatus;

            if ((_gameStatus & UniversalConstant.GameStatus.VEHICLE_SPAWNED) != 0
                && (_gameStatus & UniversalConstant.GameStatus.NPC_SPAWNED) != 0)
                _gameStatus |= UniversalConstant.GameStatus.LEVEL_GENERATED;

            if ((_gameStatus & UniversalConstant.GameStatus.NPC_HIT) != 0)
                _gameStatus |= UniversalConstant.GameStatus.LEVEL_FAILED;

            return GameStatus;
        }
    }
}