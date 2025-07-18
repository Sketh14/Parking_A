#define TESTING

using System.Threading;
using System.Threading.Tasks;
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

            SaveSystem.LoadProgress((saveStatus, saveStr, playerStats) =>
            {
                if ((saveStatus & SaveSystem.SaveStatus.LOADED_PROGRESS) != 0)
                    CurrentPlayerStats = playerStats;
                else
                {
                    CurrentPlayerStats = new PlayerStats();
                }
#if TESTING
                CurrentPlayerStats.Gold = 10000;
                CurrentPlayerStats.Coins = 10000;
#endif
            });
        }
        #endregion Singleton

        public GameConfigScriptableObject MainGameConfig;
        public PlayerStats CurrentPlayerStats;
        private UniversalConstant.GameStatus _gameStatus;
        [SerializeField] private int[] _powerPrices;
        public int[] PowerPrices { get => _powerPrices; }

        public UniversalConstant.GameStatus GameStatus
        {
            get => _gameStatus;
        }

        public VehicleInfoScriptableObject[] VehicleInfoSOs;
        private EnvironmentSpawner _envSpawner;

        public System.Action<InputManager.SelectionStatus, int, Vector2> OnSelect;
        // public System.Action<int> OnNPCHit;
        public System.Action<GameUIManager.UISelected, int> OnUISelected;
        public System.Action<UniversalConstant.GameStatus, int> OnGameStatusChange;
        public System.Action<byte[]> OnEnvironmentSpawned;

        private CancellationTokenSource _cts;

        private void OnDestroy()
        {
            if (_cts != null) _cts.Cancel();
            UnityAdsManager.Instance.OnAdStatusChange -= RewardPlayer;
        }

        private void Start()
        {
            UnityAdsManager.Instance.OnAdStatusChange += RewardPlayer;
            _cts = new CancellationTokenSource();

            PoolManager.Instance.InitializePool();
            _envSpawner = new EnvironmentSpawner();
            InitializeLevel();
            MainGameConfig.RandomString = "1316642";            //Template level
        }

        private void RewardPlayer(UnityAdsManager.AdStatus adStatus)
        {
            switch (adStatus)
            {
                case UnityAdsManager.AdStatus.AD_COMPLETED:
                    CurrentPlayerStats.Gold += UniversalConstant.COINS_RECEIVED;
                    SavePlayerStats();

                    break;
            }
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
            }
            Random.InitState(MainGameConfig.RandomString.GetHashCode());
            Debug.Log($"Selected Random Seed: {MainGameConfig.RandomString}");          // OG: 1316642

            try
            {
                // await envSpawner.SpawnBoundary((values) => boundaryData = values);
                await _envSpawner.SpawnBoundary();
                if (_cts.IsCancellationRequested) return;
            }
            //Cannot initialize boundary | Stop level generation, show some message and restart
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            // GameManager.Instance.GameStatus |= Global.UniversalConstant.GameStatus.BOUNDARY_GENERATION;
            SetGameStatus(UniversalConstant.GameStatus.BOUNDARY_GENERATED);
        }

        public UniversalConstant.GameStatus SetGameStatus(UniversalConstant.GameStatus gameStatus, bool status = true)
        {
            // if ((gameStatus & UniversalConstant.GameStatus.RESET_LEVEL) != 0)
            // {
            //     _gameStatus &= ~UniversalConstant.GameStatus.NPC_HIT;
            //     _gameStatus &= ~UniversalConstant.GameStatus.LEVEL_FAILED;
            //     return GameStatus;
            // }

            switch (gameStatus)
            {
                case UniversalConstant.GameStatus.RESET_LEVEL:
                    _gameStatus &= ~UniversalConstant.GameStatus.NPC_HIT;
                    _gameStatus &= ~UniversalConstant.GameStatus.LEVEL_FAILED;

                    return GameStatus;

                case UniversalConstant.GameStatus.NPC_HIT:
                    _gameStatus |= UniversalConstant.GameStatus.NPC_HIT;
                    _gameStatus |= UniversalConstant.GameStatus.LEVEL_FAILED;
                    OnGameStatusChange?.Invoke(UniversalConstant.GameStatus.LEVEL_FAILED, -1);

                    return GameStatus;

                case UniversalConstant.GameStatus.NEXT_LEVEL_REQUESTED:
                    _gameStatus &= ~UniversalConstant.GameStatus.LEVEL_GENERATED;
                    DeSpawnBoundary();
                    OnGameStatusChange?.Invoke(UniversalConstant.GameStatus.NEXT_LEVEL_REQUESTED, -1);
                    RequestLevelGeneration();

                    return GameStatus;

                case UniversalConstant.GameStatus.LEVEL_PASSED:
                    _gameStatus |= UniversalConstant.GameStatus.LEVEL_PASSED;

                    return GameStatus;
            }

            if (status)
                _gameStatus |= gameStatus;
            else
                _gameStatus &= ~gameStatus;

            if ((_gameStatus & UniversalConstant.GameStatus.VEHICLE_SPAWNED) != 0
                && (_gameStatus & UniversalConstant.GameStatus.NPC_SPAWNED) != 0)
            {
                _gameStatus |= UniversalConstant.GameStatus.LEVEL_GENERATED;
                OnGameStatusChange?.Invoke(UniversalConstant.GameStatus.LEVEL_GENERATED, -1);
            }

            // if ((_gameStatus & UniversalConstant.GameStatus.NPC_HIT) != 0)
            //     _gameStatus |= UniversalConstant.GameStatus.LEVEL_FAILED;

            return GameStatus;
        }

        private async void RequestLevelGeneration()
        {
            await Task.Delay(100);
            if (_cts.IsCancellationRequested) return;

            if ((_gameStatus & UniversalConstant.GameStatus.VEHICLE_SPAWNED) == 0
                && (_gameStatus & UniversalConstant.GameStatus.NPC_SPAWNED) == 0
                && (_gameStatus & UniversalConstant.GameStatus.BOUNDARY_GENERATED) == 0)
            {
                _gameStatus = UniversalConstant.GameStatus.UNINITIALIZED;
                Debug.Log($"All Systems uninitialized | Calling for initialization again");
                InitializeLevel();
            }
            else
                RequestLevelGeneration();
        }

        private void DeSpawnBoundary()
        {
            for (int i = 0; i < _envSpawner.BoundariesSpawned.Count; i++)
            {
                PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.BOUNDARY]
                    .Release(_envSpawner.BoundariesSpawned[i]);
            }
            _envSpawner.ClearBoundaries();
            SetGameStatus(UniversalConstant.GameStatus.BOUNDARY_GENERATED, false);
            // _gameStatus &= ~UniversalConstant.GameStatus.BOUNDARY_GENERATED;
        }
    }
}