using Parking_A.Global;
using UnityEngine;
using UnityEngine.UI;
using SceneManager = UnityEngine.SceneManagement.SceneManager;
public class SelctionController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Button _closeSelectionBtn;
    [SerializeField] private Button level1Btn;
    [SerializeField] private Button level2Btn;
    [SerializeField] private Button level3Btn;
    [SerializeField] private GameObject _mainMenuCanvas;
    private void OnEnable()
    {



        level1Btn.onClick.AddListener(() => LoadLevel(1));
        level2Btn.onClick.AddListener(() => LoadLevel(2));
        level3Btn.onClick.AddListener(() => LoadLevel(3));
        _closeSelectionBtn.onClick.AddListener(() =>
        {
            _mainMenuCanvas.SetActive(true);
            gameObject.SetActive(false);
        });
    }

    private void LoadLevel(int v)
    {
        SceneManager.LoadScene((int)UniversalConstant.SceneIndex.MAIN_GAMEPLAY);
    }
    private void OnDisable()
    {
        level1Btn.onClick.RemoveAllListeners();
        level2Btn.onClick.RemoveAllListeners();
        level3Btn.onClick.RemoveAllListeners();
        _closeSelectionBtn.onClick.RemoveAllListeners();

    }
}
