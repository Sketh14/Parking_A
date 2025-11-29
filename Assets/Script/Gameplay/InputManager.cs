using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Parking_A.Gameplay
{
    public class InputManager : MonoBehaviour
    {
        /// <summary> POWER1: Shrink Vehicle | POWER2: Remove Vehicle </summary>
        [System.Flags]
        public enum SelectionStatus
        {
            NOT_SELECTED = 0,
            SELECTED = 1 << 0,
            CHECK_AGAIN = 1 << 1,
            POWER1_ACTIVE = 1 << 2,
            POWER2_ACTIVE = 1 << 3
        }

        private SelectionStatus _selectionStatus = SelectionStatus.NOT_SELECTED;
        private int _hitTransformID = 0;

        // For countering camera rotation (approx. 20° tilt)
        // stored as [m01, m00, m11, m10] = [sin, cos, cos, -sin]
        private readonly float[] _transformMatrix = new float[4]
        {
            0.34f,   // m01 (sin)
            0.94f,   // m00 (cos)
            0.94f,   // m11 (cos)
            -0.34f   // m10 (-sin)
        };

        private const int _vehicleLayerMaskC = (1 << 6);

        private Vector2 _interactPos = Vector2.zero;    // pointer pos when click/touch began
        private Vector2 _lastPointerPos = Vector2.zero; // last known pointer pos
        private Vector2 _slideDir = Vector2.zero;

#if DEBUGGING_TOUCH
        private Vector3 _hitPos = Vector3.zero;
        private bool _drawRay = false;
#endif

        private void Awake()
        {
            // Enable EnhancedTouch globally (safe on Editor + device)
            EnhancedTouchSupport.Enable();
        }

        private void Start()
        {
            GameManager.Instance.OnUISelected += HandleUIAction;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnUISelected -= HandleUIAction;

            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            bool userInteracting;
            Vector2 currentPointerPos = Vector2.zero;

            // 1. Read input using new Input System

            // Touch has priority on mobile / touch devices
            if (Touch.activeTouches.Count > 0)
            {
                userInteracting = true;
                currentPointerPos = Touch.activeTouches[0].screenPosition;
            }
            // Fallback to mouse (Editor / PC)
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                userInteracting = true;
                currentPointerPos = Mouse.current.position.ReadValue();
            }
            else
            {
                userInteracting = false;
            }

#if DEBUGGING_TOUCH
            if (_drawRay)
            {
                Vector3 drawDir = new Vector3(_slideDir.x, 0f, _slideDir.y) * 30f;
                var from = _hitPos;
                from.y = 0.5f;
                Debug.DrawRay(from, drawDir, Color.cyan);
            }
#endif

            // 2. If level not generated, only allow finishing a running selection
            if ((GameManager.Instance.GameStatus & Global.UniversalConstant.GameStatus.LEVEL_GENERATED) == 0)
            {
                if ((_selectionStatus & SelectionStatus.SELECTED) != 0 && !userInteracting)
                {
                    FinishSelection();
                }

                _selectionStatus &= ~SelectionStatus.SELECTED;
                return;
            }

            // 3. While interacting, keep updating last pointer position
            if (userInteracting)
            {
                _lastPointerPos = currentPointerPos;
            }

            // 4. When interaction ends: finish the selection if any
            if (!userInteracting)
            {
                if ((_selectionStatus & SelectionStatus.SELECTED) != 0)
                {
                    FinishSelection();
                }

                _selectionStatus &= ~SelectionStatus.SELECTED;
                return;
            }

            // 5. If user is interacting and nothing is selected yet → try select a vehicle
            if ((_selectionStatus & SelectionStatus.SELECTED) == 0)
            {
                _interactPos = currentPointerPos;

                Ray cameraRay = Camera.main.ScreenPointToRay(_interactPos);
                if (Physics.Raycast(cameraRay, out RaycastHit rayHit, 500f, _vehicleLayerMaskC))
                {
                    _selectionStatus |= SelectionStatus.SELECTED;
                    _hitTransformID = rayHit.transform.GetInstanceID();
#if DEBUGGING_TOUCH
                    _hitPos = rayHit.point;
                    _drawRay = false;
#endif
                    // Debug.Log($"Selected ID: {_hitTransformID}");
                }
            }
        }

        /// <summary>
        /// Called when user stops interacting and a selection was active.
        /// Calculates slideDir, transforms it, and fires events.
        /// </summary>
        private void FinishSelection()
        {
            // Direction from initial press to last pointer pos
            Vector2 dir = _lastPointerPos - _interactPos;
            if (dir.sqrMagnitude > 0.001f)
                _slideDir = dir.normalized;
            else
                _slideDir = Vector2.zero;

            // Apply transform matrix to compensate camera rotation
            float x = _slideDir.x;
            float y = _slideDir.y;

            // using:
            // [ m00 m01 ]
            // [ m10 m11 ]
            // stored as [m01, m00, m11, m10]
            _slideDir.x = x * _transformMatrix[1] + y * _transformMatrix[3]; // x' = x*m00 + y*m10
            _slideDir.y = x * _transformMatrix[0] + y * _transformMatrix[2]; // y' = x*m01 + y*m11

#if DEBUGGING_TOUCH
            _drawRay = true;
#endif

            // If a power is active, keep only power flags, not SELECTED
            if ((_selectionStatus & SelectionStatus.POWER1_ACTIVE) != 0
                || (_selectionStatus & SelectionStatus.POWER2_ACTIVE) != 0)
            {
                _selectionStatus &= ~SelectionStatus.SELECTED;
            }

            GameManager.Instance.OnSelect?.Invoke(_selectionStatus, _hitTransformID, _slideDir);

            // Check if user has not selected another vehicle for Shrink power
            if ((_selectionStatus & SelectionStatus.CHECK_AGAIN) == 0)
            {
                // If nothing remains selected, consider that the power was used
                if ((_selectionStatus & SelectionStatus.SELECTED) == 0)
                {
                    GameManager.Instance.OnUISelected?.Invoke(GameUIManager.UISelected.POWER_USED, -1);
                    GameManager.Instance.SavePlayerStats();
                }

                _selectionStatus = SelectionStatus.NOT_SELECTED;
            }
            else
            {
                _selectionStatus &= ~SelectionStatus.CHECK_AGAIN;
            }
        }

        private void HandleUIAction(GameUIManager.UISelected uISelected, int value)
        {
            switch (uISelected)
            {
                case GameUIManager.UISelected.POWER_1:
                    if ((_selectionStatus & SelectionStatus.POWER1_ACTIVE) != 0)
                        _selectionStatus |= SelectionStatus.CHECK_AGAIN;

                    _selectionStatus |= SelectionStatus.POWER1_ACTIVE;
                    break;

                case GameUIManager.UISelected.POWER_2:
                    _selectionStatus |= SelectionStatus.POWER2_ACTIVE;
                    break;
            }
        }
    }
}
