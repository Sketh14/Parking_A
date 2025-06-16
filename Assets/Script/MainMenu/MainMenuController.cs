using Parking_A.Global;

using UnityEngine;
using UnityEngine.UI;

using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Parking_A.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playBt;// _quitBt;
        // [SerializeField] private Button _quitYesBt, _quitNoBt;

        [SerializeField] private Toggle _toggleRandomizeLevel;

        // [SerializeField] private GameObject _quitConfirmationPanel;
        [SerializeField] private GameConfigScriptableObject _mainGameConfig;

        void Start()
        {
            //Assigning Buttons
            _playBt.onClick.AddListener(() => SceneManager.LoadScene((int)UniversalConstant.SceneIndex.MAIN_GAMEPLAY));
            //_quitBt.onClick.AddListener(() => _quitConfirmationPanel.SetActive(true));

            //_quitYesBt.onClick.AddListener(Application.Quit);
            // _quitNoBt.onClick.AddListener(() => _quitConfirmationPanel.SetActive(false));

            _toggleRandomizeLevel.onValueChanged.AddListener((toggleValue) =>
            {
                _mainGameConfig.RandomizeLevel = toggleValue;
                // Debug.Log("Random: " + System.DateTime.Now.Day.ToString() + System.DateTime.Now.Hour.ToString()
                //     + System.DateTime.Now.Minute.ToString() + System.DateTime.Now.Second.ToString());
            });

            _mainGameConfig.RandomizeLevel = false;
        }
    }
}