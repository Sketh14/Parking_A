using Parking_A.Global;
using UnityEngine;
using UnityEngine.UI;

using SceneManager = UnityEngine.SceneManagement.SceneManager;

// tO DO sETTING pANEL AND mUSIC 
namespace Parking_A.MainMenu
{
    public enum MainMenuUIStatus { MAIN_MENU, SHOP }

    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _playBt, _quitBt;
        [SerializeField] private Button _quitYesBt, _quitNoBt;
        [SerializeField] private Button _openShopBt;
        [SerializeField] private Button _rateUsBtn;

        [SerializeField] private Toggle _toggleRandomizeLevel;

        [SerializeField] private GameObject _quitConfirmationPanel;
        [SerializeField] private GameConfigScriptableObject _mainGameConfig;

        [SerializeField] private GameObject _mainMenuCanvas;
        [SerializeField] private GameObject _selectionCanvas;

        private void OnDestroy()
        {
            MainMenuManager.Instance.OnUIInteraction -= HandleUIChange;
        }

        void Start()
        {
            //Assigning Buttons
            _playBt.onClick.AddListener(() => Playlevel());
            _quitBt.onClick.AddListener(() => _quitConfirmationPanel.SetActive(true));

            _quitYesBt.onClick.AddListener(Application.Quit);
            _quitNoBt.onClick.AddListener(() => _quitConfirmationPanel.SetActive(false));

            // Forced Randomization of the level toggleValue 

            _toggleRandomizeLevel.onValueChanged.AddListener((toggleValue) =>
            {
                toggleValue = true;
                _mainGameConfig.RandomizeLevel = toggleValue;
                // Debug.Log("Random: " + System.DateTime.Now.Day.ToString() + System.DateTime.Now.Hour.ToString()
                //     + System.DateTime.Now.Minute.ToString() + System.DateTime.Now.Second.ToString());
            });

            _mainGameConfig.RandomizeLevel = true;

            _openShopBt.onClick.AddListener(() =>
            {
                _mainMenuCanvas.SetActive(false);
                MainMenuManager.Instance.OnUIInteraction?.Invoke(MainMenuUIStatus.SHOP);
            });
            _rateUsBtn.onClick.AddListener(() => Application.OpenURL("market://details?id=" + Application.productName));

            MainMenuManager.Instance.OnUIInteraction += HandleUIChange;
        }
        private void Playlevel()
        {
            _selectionCanvas.SetActive(true);
            _mainMenuCanvas.SetActive(false);
        }

        private void HandleUIChange(MainMenuUIStatus status)
        {
            switch (status)
            {
                case MainMenuUIStatus.MAIN_MENU:
                    _mainMenuCanvas.SetActive(true);

                    break;
            }
        }

    }


}