using System;
using UnityEngine;
using UnityEngine.UI;

namespace Parking_A.MainMenu
{
    public class ShopUIController : MonoBehaviour
    {
        public enum InteractionStatus
        {
            SELECT_VEHICLE_S = 0, SELECT_VEHICLE_M, SELECT_VEHICLE_L,
            NEXT_VEHICLE_SKIN, PREV_VEHICLE_SKIN, BUY_EQUIP_VEHICLE,
            VEHICLE_SKIN_BOUGHT, UPDATE_VEHICLE_INFO
        }

        [SerializeField] private Button _nextVehicleSkinBt, _prevVehicleSkinBt;
        // [SerializeField] private Button[] _buyOrEquipBts;
        [SerializeField] private Button _buyOrEquipSkinBt;
        [SerializeField] private TMPro.TMP_Text _skinPriceTxt;
        [SerializeField] private Button[] _selectVehicleBts;

        public Action<InteractionStatus, int> OnVehicleInteraction;             //Pass additional value | index of Bts

        private void Oestroy()
        {
            OnVehicleInteraction -= UpdateUI;
        }

        private void Start()
        {
            OnVehicleInteraction += UpdateUI;

            #region Buttons
            for (int i = 0; i < _selectVehicleBts.Length; i++)
            {
                int tempIndex = i;
                _selectVehicleBts[i].onClick.AddListener(() => OnVehicleInteraction?.Invoke((InteractionStatus)tempIndex, -1));
            }

            _nextVehicleSkinBt.onClick.AddListener(() => OnVehicleInteraction?.Invoke(InteractionStatus.NEXT_VEHICLE_SKIN, -1));
            _prevVehicleSkinBt.onClick.AddListener(() => OnVehicleInteraction?.Invoke(InteractionStatus.PREV_VEHICLE_SKIN, -1));

            // for (int i = 0; i < _selectVehicleBts.Length; i++)
            // {
            //     int tempIndex = i;
            //     _buyOrEquipBts[i].onClick.AddListener(() => OnVehicleInteraction?.Invoke(InteractionStatus.BUY_EQUIP_VEHICLE, tempIndex));
            // }

            _buyOrEquipSkinBt.onClick.AddListener(() => OnVehicleInteraction?.Invoke(InteractionStatus.BUY_EQUIP_VEHICLE, -1));
            //NOTE: Only checking the 1st skin for small vehicle as it is default. The rest can be updated 
            //      when the player goes to the next skin.
            if ((MainMenuManager.Instance.PlayerStats.BoughtVehicleSkinIndexes[0] & (1 << 0)) != 0)
                _buyOrEquipSkinBt.GetComponentInChildren<TMPro.TMP_Text>().text = "EQUIPPED";
            #endregion Buttons
        }

        private void UpdateUI(InteractionStatus interactionStatus, int value)
        {
            switch (interactionStatus)
            {
                case InteractionStatus.VEHICLE_SKIN_BOUGHT:
                    // _buyOrEquipBts[value].GetComponentInChildren<TMPro.TMP_Text>().text = "EQUIP";
                    switch (value)
                    {
                        case -1:
                            _buyOrEquipSkinBt.GetComponentInChildren<TMPro.TMP_Text>().text = "BUY";
                            break;

                        case 0:
                            _buyOrEquipSkinBt.GetComponentInChildren<TMPro.TMP_Text>().text = "EQUIP";
                            break;

                        case 1:
                            _buyOrEquipSkinBt.GetComponentInChildren<TMPro.TMP_Text>().text = "EQUIPPED";
                            break;
                    }
                    break;

                case InteractionStatus.UPDATE_VEHICLE_INFO:
                    _skinPriceTxt.text = value.ToString();
                    break;
            }
        }
    }
}