// #define TESTING
// #define DEBUG_TOAST_POP_PULL

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Parking_A.Utility
{
    public class ToastUtility : MonoBehaviour
    {
        internal enum ToastStatus { FREE = 0, BEING_USED = 1 << 0, FLOATING = 1 << 1, CALLED_AGAIN = 1 << 2 }
        private ToastStatus _currentToastStatus;

        #region Singleton
        private static ToastUtility _instance;
        public static ToastUtility Instance;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else
                Destroy(gameObject);
        }
        #endregion Singleton

        [SerializeField] private RectTransform _toastPrefab;
        [SerializeField] private TMPro.TMP_Text _toastText;
        private const float _END_ANCHOR_Y = 140f;           // Should be 100f, but due to Lerp having 0.8 mult
        private const float _START_ANCHOR_Y = -60f;

        private CancellationTokenSource _cts;

        private void OnDestroy()
        {
            if (_cts != null) _cts.Cancel();
        }

        private void Start()
        {
            _cts = new CancellationTokenSource();

#if TESTING
            TestPopUp();
#endif
        }

        // private Task _popUpTask;
        // public void ShowPopUp(in string toastMsg)
        // {
        //     CancellationTokenSource popUpCts;
        //     _popUpTask = PopUpToast(toastMsg);
        // }

        private CancellationTokenSource _currentPopUpCts;
        // private CancellationTokenSource _currentPopUpCts2;

        public async void ShowPopUp(string toastMsg)
        {
            // _waitCts = new CancellationTokenSource();

            // _prevPopUpCts = _currentPopUpCts;

            CancellationTokenSource popUpCts = new CancellationTokenSource();
            // _currentPopUpCts2 = popUpCts;

            if ((_currentToastStatus & ToastStatus.BEING_USED) != 0)
            {
#if DEBUG_TOAST_POP_PULL
                Debug.Log($"Called Again | prev: {_currentPopUpCts.Token.GetHashCode()} | popUps: {popUpCts.Token.GetHashCode()}");
#endif
                _currentToastStatus |= ToastStatus.CALLED_AGAIN;
                _currentPopUpCts.Cancel();
                // _waitCts.Dispose();

                // popUpCts = new CancellationTokenSource();
                // _currentPopUpCts = popUpCts;
                _currentPopUpCts = popUpCts;

                // _waitCts = new CancellationTokenSource();
                // Debug.Log($"Toast called again");
            }
            else
            {
#if DEBUG_TOAST_POP_PULL
                Debug.Log($"New: {popUpCts.Token.GetHashCode()}");
#endif
                _currentPopUpCts = popUpCts;
                _currentToastStatus &= ~ToastStatus.FREE;
                _currentToastStatus |= ToastStatus.BEING_USED;
                // popUpCts = new CancellationTokenSource();
                // _currentPopUpCts = popUpCts;
            }

            // await Task.Delay(200);              //Wait for the previous async call to stop
            _currentToastStatus &= ~ToastStatus.CALLED_AGAIN;

#if DEBUG_TOAST_POP_PULL
            Debug.Log($"Popping Toast | popUps: {popUpCts.Token.GetHashCode()}");
#endif
            // Transition to Floating from Down
            float timeElapsed = 0f;
            const float speedMult = 2f;
            float interpolateVal;
            Vector2 currentPos = new Vector2(0f, _START_ANCHOR_Y);

            _toastPrefab.anchoredPosition = currentPos;
            _toastText.text = toastMsg;

            try
            {
                // System.Text.StringBuilder debugEase = new System.Text.StringBuilder();
                while (true)
                {
                    timeElapsed += Time.deltaTime * speedMult;

                    // if (_cts.IsCancellationRequested || (_currentToastStatus & ToastStatus.CALLED_AGAIN) != 0) return;
                    if (_cts.IsCancellationRequested || popUpCts.IsCancellationRequested) return;
                    if (timeElapsed > 1) break;

                    // currentPos.y = Mathf.Lerp(_START_ANCHOR_Y, _END_ANCHOR_Y, timeElapsed);

                    interpolateVal = EaseOutBack(timeElapsed) * 0.8f;         // speedMult at 2 is okay
                                                                              // interpolateVal = EaseOutElastic(timeElapsed) * 0.8f;         // speedMult at 1 is okay
                    currentPos.y = Mathf.Lerp(_START_ANCHOR_Y, _END_ANCHOR_Y, interpolateVal);

                    // debugEase.Append($"{interpolateVal} | ");
                    _toastPrefab.anchoredPosition = currentPos;

                    await Task.Yield();
                }
                // Debug.Log($"debugEase : {debugEase}");

                _currentToastStatus |= ToastStatus.FLOATING;
                // Debug.Log($"Toast Floating");

                await Task.Delay(3000, popUpCts.Token);
                // _waitCts = popUpCts;
            }
            catch
            {
#if DEBUG_TOAST_POP_PULL
                Debug.Log($"Cancelled Pop UP | popUps: {_currentPopUpCts.Token.GetHashCode()} "
                    // + $"| current: {_currentPopUpCts2.Token.GetHashCode()} "
                    + $"| popUps: {popUpCts.Token.GetHashCode()}");
#endif

                popUpCts.Dispose();
                return;
            }

            //Return if called another time
            // if (_cts.IsCancellationRequested || (_currentToastStatus & ToastStatus.CALLED_AGAIN) != 0)
            if (_cts.IsCancellationRequested || popUpCts.IsCancellationRequested) return;

            // Debug.Log($"Pulling Down Toast");
            try { PullDownToast(popUpCts.Token); }
            finally
            {
#if DEBUG_TOAST_POP_PULL
                Debug.Log($"Disposing Token | popUps: {_currentPopUpCts.Token.GetHashCode()} "
                    // + $"| current: {_currentPopUpCts2.Token.GetHashCode()} "
                    + $"| popUps: {popUpCts.Token.GetHashCode()}");
#endif

                popUpCts.Dispose();
            }
        }


        private async void PullDownToast(CancellationToken popUpToken)
        {
            // Transition Down from Floating
            float timeElapsed = 0f;
            const float speedMult = 8f;
            // float interpolateVal;
            Vector2 currentPos = new Vector2(0f, _START_ANCHOR_Y);

            try
            {
                // System.Text.StringBuilder debugEase = new System.Text.StringBuilder();
                while (true)
                {
                    timeElapsed += Time.deltaTime * speedMult;

                    // if (_cts.IsCancellationRequested || (_currentToastStatus & ToastStatus.CALLED_AGAIN) != 0) return;
                    if (_cts.IsCancellationRequested || popUpToken.IsCancellationRequested) return;
                    if (timeElapsed > 1) break;

                    currentPos.y = Mathf.Lerp(_END_ANCHOR_Y - 40f, _START_ANCHOR_Y, timeElapsed);           //40f Offset due to speedMult in PopUp

                    // interpolateVal = 1 - EaseOutBack(timeElapsed) * 0.8f;         // speedMult at 2 is okay
                    // interpolateVal = 1 - EaseOutElastic(timeElapsed) * 0.8f;         // speedMult at 1 is okay
                    // currentPos.y = Mathf.Lerp(_END_ANCHOR_Y, _START_ANCHOR_Y, interpolateVal);

                    _toastPrefab.anchoredPosition = currentPos;

                    await Task.Yield();
                }
            }
            catch { throw; }

            _currentToastStatus = ToastStatus.FREE;
        }

        //  https://easings.net/#easeOutBack
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;

            return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
        }

        // https://easings.net/#easeOutElastic
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float EaseOutElastic(float x)
        {
            const float c4 = (2 * Mathf.PI) / 3;
            return (x == 0) ? 0 : (x == 1) ? 1 : Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1;
        }

        private async void TestPopUp()
        {
            await Task.Delay(2000);

            ShowPopUp("Test Message");

            await Task.Delay(1000);

            ShowPopUp("Test Message 2");
        }
    }
}