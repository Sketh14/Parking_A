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

        public string RandomSeed;
        public UniversalConstant.GameStatus GameStatus;
        public System.Action<int, Vector2> OnSelect;
        public System.Action OnVehiclesSpawned;
        public System.Action<byte[]> OnEnvironmentSpawned;
    }
}