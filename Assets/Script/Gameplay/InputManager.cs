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
        //Keeping track of mouse/touch status
        private byte _selectionStatus;

        private int _hitTransformID = 0;

        //For countergin Camera's rotation //In X/Z pair
        private readonly float[] _transformMatrix = new float[4] { 0.34f, 0.94f, 0.94f, -0.34f };      //20fY
        // private float[] transformMatrix = new float[4] { -0.34f, 0.94f, 0.94f, 0.34f };      //-20fY
        // private float[] transformMatrix = new float[4] { 0f, 1f, 1f, 0f };         //0fY

        private const int _cVehicleLayerMask = (1 << 6);
        private void Start()
        {
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

#if DEBUGGING_TOUCH
        Vector2 interactPos = Vector2.zero, slideDir = Vector2.zero;
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
            Vector2 interactPos = Vector2.zero, slideDir = Vector2.zero;
#endif

#if !MOBILE_CONTROLS
            userInteracting = Mouse.current.leftButton.isPressed;
            if ((GameManager.Instance.GameStatus & Global.UniversalConstant.GameStatus.LEVEL_GENERATED) == 0
                 && !userInteracting)
            {
                if (_selectionStatus == 1)
                {
                    slideDir = (Mouse.current.position.value - interactPos).normalized;
                    slideDir.x = slideDir.x * _transformMatrix[1] + slideDir.y * _transformMatrix[3];
                    slideDir.y = slideDir.x * _transformMatrix[0] + slideDir.y * _transformMatrix[2];

                    GameManager.Instance.OnSelect?.Invoke(_hitTransformID, slideDir);

#if DEBUGGING_TOUCH
                    drawRay = true;
#endif
                }
                _selectionStatus = 0;
                return;
            }
#else
            userInteracting = (Touch.activeTouches.Count > 0);
            if ((GameManager.Instance.GameStatus & Global.UniversalConstant.GameStatus.LEVEL_GENERATED) == 0
                && !userInteracting)
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

                if (Physics.Raycast(cameraRay, out rayHit, 500f, _cVehicleLayerMask))
                {
                    _selectionStatus = 1;
                    _hitTransformID = rayHit.transform.GetInstanceID();
#if DEBUGGING_TOUCH
                    hitPos = rayHit.point;
#endif
                    // Debug.Log($"_hitTransformID: {_hitTransformID}");
                    // GameManager.Instance.OnSelect?.Invoke(rayHit.transform.GetInstanceID(), slideDir);
                }
            }
        }


        private void CheckForTouch()
        {

        }
    }
}