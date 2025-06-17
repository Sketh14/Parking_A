using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Parking_A.Gameplay
{
    public class GameUIManager : MonoBehaviour
    {
        public enum UISelected { POWER_PANEL = 0, LEVEL_FAILED_PANEL = 1, POWER_1 = 2, POWER_2 = 3, POWER_USED, RESET_UI }

        [SerializeField] private Button _resetLevelBt;
        [SerializeField] private GameObject _levelFailedPanel, _powerPanel;

        [SerializeField] private Button _openPowerPanelBt, _closePowerPanelBt;
        [SerializeField] private Button[] _usePowerBts;
        [SerializeField] private TMPro.TMP_Text[] _powerPricesTxt;
        [SerializeField] private TMPro.TMP_Text _disclaimerTxt;
        private const string _notEnoughGoldC = "Not Enough Gold!!";

        /// <summary> 0: Clear | 1: Not Enough Gold</summary>
        private int _disclaimerStatus;

        [SerializeField] private TMPro.TMP_Text _playerGoldTxt;
        [SerializeField] private Button _watchAds;

        private void OnDestroy()
        {
            GameManager.Instance.OnUISelected -= UpdateUIFromResult;
        }

        private void Start()
        {
            GameManager.Instance.OnNPCHit += (dummyVal) => { UpdateUI(UISelected.LEVEL_FAILED_PANEL, true); };
            GameManager.Instance.OnUISelected += UpdateUIFromResult;

            _resetLevelBt.onClick.AddListener(() => UpdateUI(UISelected.RESET_UI, true));
            _openPowerPanelBt.onClick.AddListener(() => UpdateUI(UISelected.POWER_PANEL, true));
            _closePowerPanelBt.onClick.AddListener(() => UpdateUI(UISelected.POWER_PANEL, false));

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

        private void UpdateUI(UISelected uISelected, bool value)
        {
            switch (uISelected)
            {
                case UISelected.POWER_PANEL:
                    _powerPanel.SetActive(value);
                    break;

                case UISelected.LEVEL_FAILED_PANEL:
                    _levelFailedPanel.gameObject.SetActive(value);
                    break;

                case UISelected.RESET_UI:
                    GameManager.Instance.OnGameStatusChange?.Invoke(Global.UniversalConstant.GameStatus.RESET_LEVEL);

                    _levelFailedPanel.SetActive(false);
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
            }
        }

        private async void ClearDisclaimerAsync(int flag)
        {
            await Task.Delay(2000);
            _disclaimerStatus &= ~(1 << flag);
            if (_disclaimerStatus == 0)
                _disclaimerTxt.text = "";
        }
    }
}