// #define MOBILE_CONTROLS
#define DEBUGGING_TOUCH

using UnityEngine;

#if !MOBILE_CONTROLS
using UnityEngine.InputSystem;
#else
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace Test_A.Gameplay
{
    public class InputManager : MonoBehaviour
    {
        //Keeping track of mouse/touch status
        private byte _selectionStatus;

        private int _hitTransformID = 0;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }


#if DEBUGGING_TOUCH
        Vector2 interactPos = Vector2.zero, slideDir = Vector2.zero;
#endif
        private void Update()
        {
            bool userInteracting;

            // Vector2 interactPos = Vector2.zero, slideDir = Vector2.zero;
#if !MOBILE_CONTROLS
            userInteracting = Mouse.current.leftButton.isPressed;
            if (!userInteracting)
            {
                if (_selectionStatus == 1)
                {
                    slideDir = (Mouse.current.position.value - interactPos).normalized;
                    GameManager.Instance.OnSelect?.Invoke(_hitTransformID, slideDir);
                }
                _selectionStatus = 0;
                return;
            }
#else
            userInteracting = (Touch.activeTouches.Count > 0);
            if (!userInteracting)
            {
                if(_selectionStatus == 1){
                slideDir = (Touch.activeTouches[0].screenPosition - interactPos).normalized;
                GameManager.Instance.OnSelect?.Invoke(_hitTransformID, slideDir);
                }
                _selectionStatus = 0;
                return;
            }
#endif

            if (_selectionStatus == 0)
            {
#if !MOBILE_CONTROLS
                interactPos = Mouse.current.position.value;
#else
                interactPos = Touch.activeTouches[0].screenPosition;
#endif

                Ray cameraRay = Camera.main.ScreenPointToRay(interactPos);
                RaycastHit rayHit;

                if (Physics.Raycast(cameraRay, out rayHit, 100f))
                {
                    _selectionStatus = 1;
                    _hitTransformID = rayHit.transform.GetInstanceID();
                    // GameManager.Instance.OnSelect?.Invoke(rayHit.transform.GetInstanceID(), slideDir);
                }
            }
        }


        private void CheckForTouch()
        {

        }
    }
}