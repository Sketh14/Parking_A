// #define MOBILE_CONTROLS
// #define DEBUGGING_TOUCH

using UnityEngine;

#if !MOBILE_CONTROLS
using UnityEngine.InputSystem;
#else
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace Parking_A.Gameplay
{
    public class InputManager : MonoBehaviour
    {
        /// <summary> POWER1: Shrink Vehicle | POWER2: Remove Vehicle </summary>
        public enum SelectionStatus
        {
            NOT_SELECTED = 0, SELECTED = 1 << 0, CHECK_AGAIN = 1 << 1, POWER1_ACTIVE = 1 << 2, POWER2_ACTIVE = 1 << 3
        }

        //Keeping track of mouse/touch status
        private SelectionStatus _selectionStatus;

        private int _hitTransformID = 0;

        //For countergin Camera's rotation //In X/Z pair
        private readonly float[] _transformMatrix = new float[4] { 0.34f, 0.94f, 0.94f, -0.34f };      //20fY
        // private float[] transformMatrix = new float[4] { -0.34f, 0.94f, 0.94f, 0.34f };      //-20fY
        // private float[] transformMatrix = new float[4] { 0f, 1f, 1f, 0f };         //0fY

        private const int _vehicleLayerMaskC = (1 << 6);

        private void ODestroy()
        {
            GameManager.Instance.OnUISelected -= HandleUIAction;
        }

        private void Start()
        {
            GameManager.Instance.OnUISelected += HandleUIAction;
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        Vector2 interactPos = Vector2.zero;
#if DEBUGGING_TOUCH
        Vector2 slideDir = Vector2.zero;
        Vector3 hitPos = Vector2.zero;
        bool drawRay = false;
#endif
        private void Update()
        {
            bool userInteracting;

#if DEBUGGING_TOUCH
            if (drawRay)
            {
                Vector3 drawDir = slideDir;
                drawDir.z = slideDir.y;

                // drawDir.x = drawDir.x * transformMatrix[1] + drawDir.z * transformMatrix[3];
                // drawDir.z = drawDir.x * transformMatrix[0] + drawDir.z * transformMatrix[2];

                drawDir.x *= 30f;
                drawDir.y = 0.5f;
                drawDir.z *= 30f;

                Debug.DrawRay(hitPos, drawDir, Color.cyan);
            }
#else
            Vector2 slideDir = Vector2.zero;
#endif


#if !MOBILE_CONTROLS
            userInteracting = Mouse.current.leftButton.isPressed;
            if ((GameManager.Instance.GameStatus & Global.UniversalConstant.GameStatus.LEVEL_GENERATED) == 0
                 || !userInteracting)
            {
                if ((_selectionStatus & SelectionStatus.SELECTED) != 0)
                {
                    slideDir = (Mouse.current.position.value - interactPos).normalized;
                    slideDir.x = slideDir.x * _transformMatrix[1] + slideDir.y * _transformMatrix[3];
                    slideDir.y = slideDir.x * _transformMatrix[0] + slideDir.y * _transformMatrix[2];

                    if ((_selectionStatus & SelectionStatus.POWER1_ACTIVE) != 0
                        || (_selectionStatus & SelectionStatus.POWER2_ACTIVE) != 0)
                        _selectionStatus &= ~SelectionStatus.SELECTED;          //Unset Selected to only keep powers flag on                    

                    GameManager.Instance.OnSelect?.Invoke(_selectionStatus, _hitTransformID, slideDir);

                    // Check if user has not selected the small vehicle for Shrink power
                    if ((_selectionStatus & SelectionStatus.CHECK_AGAIN) == 0)
                        _selectionStatus = SelectionStatus.NOT_SELECTED;
                    else
                        _selectionStatus &= ~SelectionStatus.CHECK_AGAIN;
                    Debug.Log($"Resetting SelectionStatus | {_selectionStatus}");
#if DEBUGGING_TOUCH
                    drawRay = true;
#endif
                }

                _selectionStatus |= SelectionStatus.NOT_SELECTED;
                _selectionStatus &= ~SelectionStatus.SELECTED;
                return;
            }
#else
            userInteracting = (Touch.activeTouches.Count > 0);
            if ((GameManager.Instance.GameStatus & Global.UniversalConstant.GameStatus.LEVEL_GENERATED) == 0
                || !userInteracting)
            {
                if ((_selectionStatus & SelectionStatus.SELECTED) != 0)
                {
                    _selectionStatus &= ~SelectionStatus.POWER_ACTIVE;
                    slideDir = (Touch.activeTouches[0].screenPosition - interactPos).normalized;
                    slideDir.x = slideDir.x * _transformMatrix[1] + slideDir.y * _transformMatrix[3];
                    slideDir.y = slideDir.x * _transformMatrix[0] + slideDir.y * _transformMatrix[2];

                    if ((_selectionStatus & SelectionStatus.SELECTED) != 0)
                    {
                        _selectionStatus &= ~SelectionStatus.POWER_ACTIVE;
                    }
                    else
                        GameManager.Instance.OnSelect?.Invoke(_hitTransformID, slideDir);
                }

                _selectionStatus |= SelectionStatus.NOT_SELECTED;
                _selectionStatus &= ~SelectionStatus.SELECTED;
                return;
            }
#endif

            if ((_selectionStatus & SelectionStatus.SELECTED) == 0)
            {
#if !MOBILE_CONTROLS
                interactPos = Mouse.current.position.value;
#else
                interactPos = Touch.activeTouches[0].screenPosition;
#endif

                Ray cameraRay = Camera.main.ScreenPointToRay(interactPos);
                RaycastHit rayHit;

                if (Physics.Raycast(cameraRay, out rayHit, 500f, _vehicleLayerMaskC))
                {
                    _selectionStatus |= SelectionStatus.SELECTED;
                    _hitTransformID = rayHit.transform.GetInstanceID();
#if DEBUGGING_TOUCH
                    hitPos = rayHit.point;
#endif
                    // Debug.Log($"_hitTransformID: {_hitTransformID}");
                    // GameManager.Instance.OnSelect?.Invoke(rayHit.transform.GetInstanceID(), slideDir);
                }
            }
        }

        private void HandleUIAction(GameUIManager.UISelected uISelected, int value)
        {
            switch (uISelected)
            {
                case GameUIManager.UISelected.POWER_1:
                    // In case the player has selected a small vehicle | Event is fired again to show that small vehicle is selected
                    // Set the CHECK_AGAIN flag, so that the Update does not modify anything
                    if ((_selectionStatus & SelectionStatus.POWER1_ACTIVE) != 0)
                        _selectionStatus |= SelectionStatus.CHECK_AGAIN;

                    _selectionStatus |= SelectionStatus.POWER1_ACTIVE;

                    Debug.Log($"Changing SelectionStatus: {_selectionStatus}");
                    break;

                case GameUIManager.UISelected.POWER_2:
                    _selectionStatus |= SelectionStatus.POWER2_ACTIVE;
                    break;
            }
        }

        private void CheckForTouch()
        {

        }
    }
}