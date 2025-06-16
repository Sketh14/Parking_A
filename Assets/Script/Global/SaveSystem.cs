using System.IO;
using System.Text;
using UnityEngine;

namespace Parking_A.Global
{
    public class SaveSystem
    {
        public enum SaveStatus { PROCESSING = 0, SAVED_PROGRESS, LOADED_PROGRESS, NEW_SAVE_FILE, NO_SAVE_FILE, SAVE_FAILED, LOAD_FAILED }

        public static void SaveProgress(PlayerStats playerStats, System.Action<SaveStatus, string> OnDataSaved)
        {
            string playerData = JsonUtility.ToJson(playerStats);
            byte[] playerDataInBytes = Encoding.ASCII.GetBytes(playerData);

            string pathToFile = Path.Combine(Application.persistentDataPath, "Parking_PlayerStats.txt");

            FileStream saveFileStream;
            SaveStatus saveStatus = SaveStatus.PROCESSING;

            //Check if the file exists
            if (File.Exists(pathToFile))
                saveFileStream = new FileStream(pathToFile, FileMode.Open);
            //Create file if it does not exists
            else
            {
                saveFileStream = File.Create(pathToFile);
                saveStatus |= SaveStatus.NEW_SAVE_FILE;
            }

            //Save Player Data
            //Try Saving the file
            try
            {
                saveStatus &= ~SaveStatus.PROCESSING;
                saveStatus |= SaveStatus.SAVED_PROGRESS;
                saveFileStream.Write(playerDataInBytes, 0, playerDataInBytes.Length);
                // File.WriteAllBytes(filePath, playerDataInBytes);
                OnDataSaved?.Invoke(saveStatus, "");
                // Debug.Log($"Saved Player Data to : {filePath}");
            }
            catch (System.Exception ex)
            {
                saveStatus &= ~SaveStatus.PROCESSING;
                saveStatus |= SaveStatus.SAVE_FAILED;
                Debug.LogError($"Error ocurred! | Failed to Save Data to path : {pathToFile} | Error : {ex.Message}");
                OnDataSaved?.Invoke(saveStatus, ex.Message);
            }
            finally
            {
                saveFileStream.Close();
            }
        }

        public static void LoadProgress(System.Action<SaveStatus, string, PlayerStats> OnDataLoaded)
        {
            SaveStatus saveStatus = SaveStatus.PROCESSING;
            string pathToFile = Path.Combine(Application.persistentDataPath, "Parking_PlayerStats.txt");
            if (!File.Exists(pathToFile))
            {
                saveStatus |= SaveStatus.NO_SAVE_FILE;
                // Debug.Log($"No Save File Present");
                OnDataLoaded?.Invoke(saveStatus, "", null);
                return;
            }

            byte[] playerDataInBytes;
            try
            {
                saveStatus &= ~SaveStatus.PROCESSING;
                saveStatus |= SaveStatus.LOADED_PROGRESS;
                playerDataInBytes = File.ReadAllBytes(pathToFile);
                string playerData = Encoding.ASCII.GetString(playerDataInBytes);
                OnDataLoaded?.Invoke(saveStatus, "", JsonUtility.FromJson<PlayerStats>(playerData));
            }
            catch (System.Exception ex)
            {
                saveStatus &= ~SaveStatus.PROCESSING;
                saveStatus |= SaveStatus.LOAD_FAILED;
                Debug.LogError($"Error Occured! | Failed to load data from path: {pathToFile} | Error: {ex.Message}");
                OnDataLoaded?.Invoke(saveStatus, ex.Message, null);
            }
        }
    }
}