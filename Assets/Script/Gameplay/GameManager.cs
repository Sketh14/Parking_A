using UnityEngine;

namespace Test_A.Gameplay
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

        public System.Action<int, Vector2> OnSelect;
    }
}