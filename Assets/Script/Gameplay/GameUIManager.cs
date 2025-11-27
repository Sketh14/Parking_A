using System.Threading;
using System.Threading.Tasks;
using Parking_A.Global;
using UnityEngine;
using UnityEngine.UI;

namespace Parking_A.Gameplay
{
    public class GameUIManager : MonoBehaviour
    {
        public enum UISelected { POWER_PANEL = 0, LEVEL_RESULT_PANEL = 1, POWER_1 = 2, POWER_2 = 3, POWER_USED, RESET_UI, GO_HOME }

        [SerializeField] private Button _tryAgainBt, _nextLevelBt, _homeBt;
        [SerializeField] private GameObject _levelResultPanel, _powerPanel;
        [SerializeField] private TMPro.TMP_Text _levelResultTxt;
        private const string _levelPassedC = "LEVEL PASSED!", _levelFailedC = "LEVEL FAILED";

        [SerializeField] private Button _openPowerPanelBt, _closePowerPanelBt;
        [SerializeField] private Button[] _usePowerBts;
        [SerializeField] private TMPro.TMP_Text[] _powerPricesTxt;
        [SerializeField] private TMPro.TMP_Text _disclaimerTxt;
        private const string _notEnoughGoldC = "Not Enough Gold!!";

        /// <summary> 0: Clear | 1: Not Enough Gold</summary>
        private int _disclaimerStatus;

        [SerializeField] private TMPro.TMP_Text _playerGoldTxt;
        [SerializeField] private Button _watchAds;

        private CancellationTokenSource _cts;

        private void OnDestroy()
        {
            GameManager.Instance.OnUISelected -= UpdateUIFromResult;
            GameManager.Instance.OnGameStatusChange -= UpdateUIFromGameStatus;


            if (_cts != null) _cts.Cancel();
        }

        private void Start()
        {
            _cts = new CancellationTokenSource();

            // GameManager.Instance.OnNPCHit += (dummyVal) => { UpdateUI(UISelected.LEVEL_RESULT_PANEL, true); };
            GameManager.Instance.OnUISelected += UpdateUIFromResult;
            GameManager.Instance.OnGameStatusChange += UpdateUIFromGameStatus;


            _tryAgainBt.onClick.AddListener(() => UpdateUI(UISelected.RESET_UI, true));
            _openPowerPanelBt.onClick.AddListener(() => UpdateUI(UISelected.POWER_PANEL, true));
            _closePowerPanelBt.onClick.AddListener(() => UpdateUI(UISelected.POWER_PANEL, false));

            _nextLevelBt.onClick.AddListener(() =>
            GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.NEXT_LEVEL_REQUESTED, true));
            // GameManager.Instance.OnGameStatusChange?.Invoke(UniversalConstant.GameStatus.NEXT_LEVEL_REQUESTED, -1));
            _homeBt.onClick.AddListener(() => UpdateUI(UISelected.GO_HOME, false));

            _watchAds.interactable = false;
            _watchAds.onClick.AddListener(() =>
            {
                _watchAds.interactable = false;
                // AdManager.Instance.ShowRewarded();

            });

            for (int i = 0; i < _usePowerBts.Length; i++)
            {
                int tempIndex = i;
                // _usePowerBts[i].onClick.AddListener(() => GameManager.Instance.OnUISelected?.Invoke((UISelected)tempIndex, -1));
                _usePowerBts[i].onClick.AddListener(() => UpdateUI((UISelected)(tempIndex + 2), true));
                _powerPricesTxt[i].text = GameManager.Instance.PowerPrices[i].ToString();
            }

            _playerGoldTxt.text = GameManager.Instance.CurrentPlayerStats.Gold.ToString();
        }



        private void UpdateUIFromResult(UISelected uISelected, int value)
        {
            switch (uISelected)
            {
                case UISelected.POWER_USED:
                    _openPowerPanelBt.interactable = true;

                    break;
            }
        }

        private async void UpdateUIFromGameStatus(UniversalConstant.GameStatus gameStatus, int value)
        {
            switch (gameStatus)
            {
                case UniversalConstant.GameStatus.LEVEL_FAILED:
                    _levelResultPanel.gameObject.SetActive(true);
                    _levelResultTxt.text = _levelFailedC;
                    _tryAgainBt.gameObject.SetActive(true);
                    _nextLevelBt.gameObject.SetActive(false);

                    break;

                case UniversalConstant.GameStatus.LEVEL_PASSED:
                    _levelResultPanel.gameObject.SetActive(true);
                    _levelResultTxt.text = _levelPassedC;
                    _nextLevelBt.gameObject.SetActive(true);
                    _tryAgainBt.gameObject.SetActive(false);

                    break;

                case UniversalConstant.GameStatus.NEXT_LEVEL_REQUESTED:
                    //Adding a delay, as sometimes, InputManager can get fired in between button pressed and reset happening
                    await Task.Delay(100);
                    if (_cts.IsCancellationRequested) return;

                    _levelResultPanel.gameObject.SetActive(false);

                    break;
            }
        }

        private async void UpdateUI(UISelected uISelected, bool value)
        {
            switch (uISelected)
            {
                case UISelected.POWER_PANEL:
                    _powerPanel.SetActive(value);
                    break;

                case UISelected.LEVEL_RESULT_PANEL:
                    _levelResultPanel.gameObject.SetActive(value);
                    break;

                case UISelected.RESET_UI:
                    //Adding a delay, as sometimes, InputManager can get fired in between button pressed and reset happening
                    await Task.Delay(100);
                    if (_cts.IsCancellationRequested) return;

                    GameManager.Instance.OnGameStatusChange?.Invoke(UniversalConstant.GameStatus.RESET_LEVEL, -1);
                    GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.RESET_LEVEL);
                    _levelResultPanel.SetActive(false);
                    break;


                case UISelected.POWER_1:
                case UISelected.POWER_2:
                    // Check if player has enough gold
                    if (GameManager.Instance.CurrentPlayerStats.Gold >= GameManager.Instance.PowerPrices[(int)uISelected - 2])
                    {
                        GameManager.Instance.OnUISelected?.Invoke(uISelected, -1);

                        GameManager.Instance.CurrentPlayerStats.Gold -= GameManager.Instance.PowerPrices[(int)uISelected - 2];
                        _playerGoldTxt.text = GameManager.Instance.CurrentPlayerStats.Gold.ToString();

                        _powerPanel.SetActive(false);
                        _openPowerPanelBt.interactable = false;
                    }
                    else if ((_disclaimerStatus & (1 << 0)) == 0)
                    {
                        _disclaimerTxt.text = _notEnoughGoldC;
                        _disclaimerStatus |= 1 << 0;
                        ClearDisclaimerAsync(0);
                    }

                    break;

                case UISelected.GO_HOME:
                    _ = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(0);
                    break;
            }
        }

        private async void ClearDisclaimerAsync(int flag)
        {
            await Task.Delay(2000);
            if (_cts.IsCancellationRequested) return;

            _disclaimerStatus &= ~(1 << flag);
            if (_disclaimerStatus == 0)
                _disclaimerTxt.text = "";
        }
    }
}