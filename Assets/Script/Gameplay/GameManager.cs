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
        }
        #endregion Singleton

        public GameConfigScriptableObject MainGameConfig;
        public PlayerStats CurrentPlayerStats;
        private UniversalConstant.GameStatus _gameStatus;

        public UniversalConstant.GameStatus GameStatus
        {
            get => _gameStatus;
        }
        public System.Action<InputManager.SelectionStatus, int, Vector2> OnSelect;
        public System.Action<int> OnNPCHit;
        public System.Action<GameUIManager.UISelected, int> OnUISelected;
        public System.Action<UniversalConstant.GameStatus> OnGameStatusChange;
        public System.Action<byte[]> OnEnvironmentSpawned;

        private void Start()
        {
            InitializeLevel();

            SaveSystem.LoadProgress((saveStatus, saveStr, playerStats) =>
            {
                if ((saveStatus & SaveSystem.SaveStatus.LOADED_PROGRESS) != 0)
                    CurrentPlayerStats = playerStats;
                else
                    CurrentPlayerStats = new PlayerStats();
            });
        }

        public void SavePlayerStats()
        {
            SaveSystem.SaveProgress(CurrentPlayerStats, SavePlayerStats);
        }

        private void SavePlayerStats(SaveSystem.SaveStatus saveStatus, string resultStr)
        {
            if ((saveStatus & SaveSystem.SaveStatus.SAVED_PROGRESS) != 0)
            {
                Debug.Log($"Progress Saved");
            }
            else
            {
                //Show something for save failed
                Debug.LogError($"Saving progress failed | Error: {resultStr}");
            }
        }

        private async void InitializeLevel()
        {
            if (MainGameConfig.RandomizeLevel)
            {
                string tempRandomSeed = DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString()
                    + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString();
                MainGameConfig.RandomString = tempRandomSeed;
                Debug.Log($"Selected Random Seed: {tempRandomSeed}");
            }

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