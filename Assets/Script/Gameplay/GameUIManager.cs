using UnityEngine;
using UnityEngine.UI;

namespace Parking_A.Gameplay
{
    public class GameUIManager : MonoBehaviour
    {
        [SerializeField] private Button _resetLevelBt;

        [SerializeField] private GameObject _levelFailedPanel;

        private void OnDestroy()
        {

        }

        private void Start()
        {
            GameManager.Instance.OnNPCHit += (dummyVal) => { _levelFailedPanel.gameObject.SetActive(true); };
            _resetLevelBt.onClick.AddListener(ResetUI);
        }

        private void ResetUI()
        {
            GameManager.Instance.OnGameStatusChange?.Invoke(Global.UniversalConstant.GameStatus.RESET_LEVEL);

            _levelFailedPanel.SetActive(false);
        }
    }
}