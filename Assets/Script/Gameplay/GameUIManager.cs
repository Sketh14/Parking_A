using UnityEngine;
using UnityEngine.UI;

namespace Parking_A.Gameplay
{
    public class GameUIManager : MonoBehaviour
    {
        public enum UISelected { POWER_1, POWER_2 }

        [SerializeField] private Button _resetLevelBt;

        [SerializeField] private GameObject _levelFailedPanel;

        [SerializeField] private Button[] _powerBts;

        private void OnDestroy()
        {

        }

        private void Start()
        {
            GameManager.Instance.OnNPCHit += (dummyVal) => { _levelFailedPanel.gameObject.SetActive(true); };
            _resetLevelBt.onClick.AddListener(ResetUI);

            for (int i = 0; i < _powerBts.Length; i++)
            {
                int tempIndex = i;
                _powerBts[i].onClick.AddListener(() => GameManager.Instance.OnUISelected?.Invoke((UISelected)tempIndex, -1));
            }
        }

        private void ResetUI()
        {
            GameManager.Instance.OnGameStatusChange?.Invoke(Global.UniversalConstant.GameStatus.RESET_LEVEL);

            _levelFailedPanel.SetActive(false);
        }
    }
}