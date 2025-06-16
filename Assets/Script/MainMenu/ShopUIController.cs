using System;
using UnityEngine;
using UnityEngine.UI;

namespace Parking_A.MainMenu
{
    public class ShopUIController : MonoBehaviour
    {
        public enum InteractionStatus { SELECT_VEHICLE_S = 0, SELECT_VEHICLE_M, SELECT_VEHICLE_L, NEXT_VEHICLE_SKIN, PREV_VEHICLE_SKIN, }

        [SerializeField] private Button _nextVehicleSkinBt, _prevVehicleSkinBt;
        [SerializeField] private Button[] _selectVehicleBts;

        public Action<InteractionStatus> OnVehicleInteraction;

        private void Start()
        {
            for (int i = 0; i < _selectVehicleBts.Length; i++)
            {
                int tempIndex = i;
                _selectVehicleBts[i].onClick.AddListener(() => OnVehicleInteraction?.Invoke((InteractionStatus)tempIndex));
            }

            _nextVehicleSkinBt.onClick.AddListener(() => OnVehicleInteraction?.Invoke(InteractionStatus.NEXT_VEHICLE_SKIN));
            _prevVehicleSkinBt.onClick.AddListener(() => OnVehicleInteraction?.Invoke(InteractionStatus.PREV_VEHICLE_SKIN));
        }

    }
}