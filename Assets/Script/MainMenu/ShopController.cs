#define MOBILE_CONTROLS

using Parking_A.Global;
using UnityEngine;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Parking_A.MainMenu
{
    public class ShopController : MonoBehaviour
    {
        [System.Serializable]
        internal class VehicleInfos
        {
            // public UniversalConstant.PoolType VehicleType;
            // public int[] SkinPrice;
            // public Material[] SkinsMat;
            public byte EquippedSkinIndex;
            public GameObject VehiclePrefab;
            public VehicleInfoScriptableObject VehicleInfoSO;
        }

        [SerializeField] private Transform _rotatingPlatform;
        [SerializeField] private float _turnSpeed = 1f;

        // [SerializeField] private PoolScriptableObject[] _vehicleSOs;
        [SerializeField] private VehicleInfos[] _vehicleInfos;
        private GameObject[] _vehiclePrefabs;

        [SerializeField] private ShopUIController _shopUIController;
        private byte _currVehicleIndex;

        private void Onestroy()
        {
            _shopUIController.OnVehicleInteraction -= UpdateVehicle;
        }

        private void Start()
        {
            InitializeVehicles();

            _shopUIController.OnVehicleInteraction += UpdateVehicle;
        }

        private void InitializeVehicles()
        {
            _vehiclePrefabs = new GameObject[_vehicleInfos.Length];

            Transform vehicleHolder = _rotatingPlatform.GetChild(0);
            GameObject tempVehicle;
            Vector3 spawnPos = new Vector3(0f, 0.3f, 0f);
            for (int i = 0; i < _vehicleInfos.Length; i++)
            {
                tempVehicle = Instantiate(_vehicleInfos[i].VehiclePrefab, vehicleHolder);
                tempVehicle.transform.localPosition = spawnPos;
                tempVehicle.SetActive(false);

                _vehiclePrefabs[i] = tempVehicle;

                //TODO: Link Equipped Skin to player save stats
                //TODO: Update UI according to Equipped Skin | Price/Buy/Equip

                _vehicleInfos[i].EquippedSkinIndex = 0;
                // _vehicleInfos[i].EquippedSkinIndex = MainMenuManager.Instance.PlayerStats.EquippedVehicleSkinIndexes[i];             //TODO: Link this to player save stats
                _vehiclePrefabs[i].transform.GetChild(4).GetChild(0)
                    .GetComponent<MeshRenderer>().material =
                    _vehicleInfos[i].VehicleInfoSO.SkinsMat[_vehicleInfos[i].EquippedSkinIndex];
            }
            _vehiclePrefabs[0].SetActive(true);             //By default small vehicle will be active
            _currVehicleIndex = 0;

            // Update initial price text to default small vehicle's 1st skin
            _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.UPDATE_VEHICLE_INFO,
                _vehicleInfos[0].VehicleInfoSO.SkinPrice[0]);

            // As the first skin is the default one, so no need to do below
            // Check if player has the dafault skin or not
            // if ((MainMenuManager.Instance._playerStats.BoughtVehicleSkinIndexes[0] & (1 << 0)) != 0)
            // {
            //     // Player has already bought and equipped, so change text to "EQUIPPED"
            //     if ((MainMenuManager.Instance._playerStats.EquippedVehicleSkinIndexes[0] & (1 << 0)) != 0)
            //         _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.VEHICLE_SKIN_BOUGHT, 1);
            //     // Change text to "EQUIP"
            //     else
            //         _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.VEHICLE_SKIN_BOUGHT, 0);
            // }
        }

        void Update()
        {
#if MOBILE_CONTROLS
#endif
            _rotatingPlatform.Rotate(Vector3.up, _turnSpeed * Time.deltaTime);
        }

        public void UpdateVehicle(ShopUIController.InteractionStatus interactionStatus, int value)
        {
            switch (interactionStatus)
            {
                case ShopUIController.InteractionStatus.SELECT_VEHICLE_S:
                    _vehiclePrefabs[0].SetActive(true);
                    _vehiclePrefabs[1].SetActive(false);
                    _vehiclePrefabs[2].SetActive(false);

                    _currVehicleIndex = 0;
                    goto case (ShopUIController.InteractionStatus)101;

                case ShopUIController.InteractionStatus.SELECT_VEHICLE_M:
                    _vehiclePrefabs[0].SetActive(false);
                    _vehiclePrefabs[1].SetActive(true);
                    _vehiclePrefabs[2].SetActive(false);

                    _currVehicleIndex = 1;
                    goto case (ShopUIController.InteractionStatus)101;

                case ShopUIController.InteractionStatus.SELECT_VEHICLE_L:
                    _vehiclePrefabs[0].SetActive(false);
                    _vehiclePrefabs[1].SetActive(false);
                    _vehiclePrefabs[2].SetActive(true);

                    _currVehicleIndex = 2;
                    goto case (ShopUIController.InteractionStatus)101;

                case ShopUIController.InteractionStatus.NEXT_VEHICLE_SKIN:
                    {
                        int skinsCount = _vehicleInfos[_currVehicleIndex].VehicleInfoSO.SkinsMat.Length;
                        _vehicleInfos[_currVehicleIndex].EquippedSkinIndex = (_vehicleInfos[_currVehicleIndex].EquippedSkinIndex + 1) >= skinsCount
                            ? (byte)0 : ++_vehicleInfos[_currVehicleIndex].EquippedSkinIndex;

                        // _vehiclePrefabs[_currVehicleSkinIndex].SetActive(true);
                    }
                    goto case (ShopUIController.InteractionStatus)100;

                case ShopUIController.InteractionStatus.PREV_VEHICLE_SKIN:
                    {
                        int skinsCount = _vehicleInfos[_currVehicleIndex].VehicleInfoSO.SkinsMat.Length;
                        _vehicleInfos[_currVehicleIndex].EquippedSkinIndex = (_vehicleInfos[_currVehicleIndex].EquippedSkinIndex - 1) < 0
                            ? (byte)(skinsCount - 1) : --_vehicleInfos[_currVehicleIndex].EquippedSkinIndex;

                        // _vehiclePrefabs[_currVehicleSkinIndex].SetActive(true);
                    }
                    goto case (ShopUIController.InteractionStatus)100;

                // Set skins
                case (ShopUIController.InteractionStatus)100:
                    _vehiclePrefabs[_currVehicleIndex].transform.GetChild(4).GetChild(0)
                        .GetComponent<MeshRenderer>().material =
                        _vehicleInfos[_currVehicleIndex].VehicleInfoSO.SkinsMat[_vehicleInfos[_currVehicleIndex].EquippedSkinIndex];

                    goto case (ShopUIController.InteractionStatus)101;

                // UI callbacks
                case (ShopUIController.InteractionStatus)101:
                    // Check if the skin currently selected is equipped or not
                    if (MainMenuManager.Instance.PlayerStats.EquippedVehicleSkinIndexes[_currVehicleIndex] ==
                        _vehicleInfos[_currVehicleIndex].EquippedSkinIndex)
                    {
                        _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.VEHICLE_SKIN_BOUGHT, 1);
                    }
                    // Check if the skin currently selected is bought or not
                    else if ((MainMenuManager.Instance.PlayerStats.BoughtVehicleSkinIndexes[_currVehicleIndex]
                        & (1 << _vehicleInfos[_currVehicleIndex].EquippedSkinIndex)) != 0)
                    {
                        _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.VEHICLE_SKIN_BOUGHT, 0);
                    }
                    else
                        _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.VEHICLE_SKIN_BOUGHT, -1);

                    _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.UPDATE_VEHICLE_INFO,
                        _vehicleInfos[_currVehicleIndex].VehicleInfoSO.SkinPrice[_vehicleInfos[_currVehicleIndex].EquippedSkinIndex]);

                    break;

                case ShopUIController.InteractionStatus.BUY_EQUIP_VEHICLE:
                    {
                        //Maybe display something earlier to show that stats are not loaded
                        if (MainMenuManager.Instance.LoadStatsFailCount > MainMenuManager._maxLoadFailCount)
                            return;

                        //Check if the player has bought the skin or not
                        if ((MainMenuManager.Instance.PlayerStats.BoughtVehicleSkinIndexes[_currVehicleIndex]
                            & (1 << _vehicleInfos[_currVehicleIndex].EquippedSkinIndex)) != 0)
                        {
                            // MainMenuManager.Instance.PlayerStats.EquippedVehicleSkinIndexes[_currVehicleIndex] = 0;             //Reset first
                            MainMenuManager.Instance.PlayerStats.EquippedVehicleSkinIndexes[_currVehicleIndex] = _vehicleInfos[_currVehicleIndex].EquippedSkinIndex;

                            _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.VEHICLE_SKIN_BOUGHT, 1);
                            MainMenuManager.Instance.SavePlayerStats();
                            return;
                        }

                        //Check if the player has enough coins
                        int skinPrice = _vehicleInfos[_currVehicleIndex].VehicleInfoSO.SkinPrice[_vehicleInfos[_currVehicleIndex].EquippedSkinIndex];
                        if (MainMenuManager.Instance.PlayerStats.Coins < skinPrice)
                        {
                            //Show message for not enough money
                            return;
                        }

                        MainMenuManager.Instance.PlayerStats.Coins -= skinPrice;
                        MainMenuManager.Instance.PlayerStats.BoughtVehicleSkinIndexes[_currVehicleIndex] |= 1 << _vehicleInfos[_currVehicleIndex].EquippedSkinIndex;
                        _shopUIController.OnVehicleInteraction?.Invoke(ShopUIController.InteractionStatus.VEHICLE_SKIN_BOUGHT, 0);
                        MainMenuManager.Instance.SavePlayerStats();
                    }

                    break;
            }
        }
    }
}