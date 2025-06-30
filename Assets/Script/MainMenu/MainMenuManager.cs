using Parking_A.Global;
using UnityEngine;

namespace Parking_A.MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {
        #region Singleton
        private static MainMenuManager _instance;
        public static MainMenuManager Instance { get => _instance; }
        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(this.gameObject);
        }
        #endregion Singleton

        public PlayerStats PlayerStats;
        public int LoadStatsFailCount { get; set; }
        public const int _maxLoadFailCount = 3;

        public System.Action<MainMenuUIStatus> OnUIInteraction;

        void Start()
        {
            SaveSystem.LoadProgress(LoadPlayerStats);
        }

        public void SavePlayerStats()
        {
            SaveSystem.SaveProgress(PlayerStats, SavePlayerStats);
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

        private void LoadPlayerStats(SaveSystem.SaveStatus saveStatus, string resultStr, PlayerStats loadedPlayerStats)
        {
            if ((saveStatus & SaveSystem.SaveStatus.LOADED_PROGRESS) != 0)
            {
                PlayerStats = loadedPlayerStats;
                Debug.Log($"Loaded player Stats");
            }
            else if ((saveStatus & SaveSystem.SaveStatus.NO_SAVE_FILE) != 0)
            {
                PlayerStats = new PlayerStats();
                Debug.Log($"No Save file found");
            }
            else
            {
                LoadStatsFailCount++;
                SaveSystem.LoadProgress(LoadPlayerStats);

                //Show something for loading failed
                Debug.LogError($"Loading progress failed! | Error: {resultStr}");
            }
        }
    }
}