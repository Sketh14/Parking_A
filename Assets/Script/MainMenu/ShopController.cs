using Parking_A.Global;
using UnityEngine;

namespace Parking_A.MainMenu
{
    public class ShopController : MonoBehaviour
    {
        [System.Serializable]
        internal class VehicleInfos
        {
            public UniversalConstant.PoolType VehicleType;
            public byte SkinIndex;
            public Material[] SkinsMat;
        }

        [SerializeField] private Transform _rotatingPlatform;
        [SerializeField] private float _turnSpeed = 1f;

        [SerializeField] private PoolScriptableObject[] _vehicleSOs;
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
            _vehiclePrefabs = new GameObject[_vehicleSOs.Length];

            Transform vehicleHolder = _rotatingPlatform.GetChild(0);
            GameObject tempVehicle;
            Vector3 spawnPos = new Vector3(0f, 0.3f, 0f);
            for (int i = 0; i < _vehicleSOs.Length; i++)
            {
                tempVehicle = Instantiate(_vehicleSOs[i].poolPrefab, vehicleHolder);
                tempVehicle.transform.localPosition = spawnPos;
                tempVehicle.SetActive(false);

                _vehiclePrefabs[i] = tempVehicle;
                _vehicleInfos[i].SkinIndex = 0;             //TODO: Link this to player save stats
                _vehiclePrefabs[i].transform.GetChild(4).GetChild(0)
                    .GetComponent<MeshRenderer>().material =
                    _vehicleInfos[i].SkinsMat[_vehicleInfos[i].SkinIndex];
            }
            _vehiclePrefabs[0].SetActive(true);             //By default small vehicle will be active
            _currVehicleIndex = 0;
        }

        void Update()
        {
            _rotatingPlatform.Rotate(Vector3.up, _turnSpeed * Time.deltaTime);
        }

        public void UpdateVehicle(ShopUIController.InteractionStatus interactionStatus)
        {
            int skinsCount;
            switch (interactionStatus)
            {
                case ShopUIController.InteractionStatus.SELECT_VEHICLE_S:
                    _vehiclePrefabs[0].SetActive(true);
                    _vehiclePrefabs[1].SetActive(false);
                    _vehiclePrefabs[2].SetActive(false);

                    _currVehicleIndex = 0;
                    break;

                case ShopUIController.InteractionStatus.SELECT_VEHICLE_M:
                    _vehiclePrefabs[0].SetActive(false);
                    _vehiclePrefabs[1].SetActive(true);
                    _vehiclePrefabs[2].SetActive(false);

                    _currVehicleIndex = 1;
                    break;

                case ShopUIController.InteractionStatus.SELECT_VEHICLE_L:
                    _vehiclePrefabs[0].SetActive(false);
                    _vehiclePrefabs[1].SetActive(false);
                    _vehiclePrefabs[2].SetActive(true);

                    _currVehicleIndex = 2;
                    break;

                case ShopUIController.InteractionStatus.NEXT_VEHICLE_SKIN:
                    skinsCount = _vehicleInfos[_currVehicleIndex].SkinsMat.Length;
                    _vehicleInfos[_currVehicleIndex].SkinIndex = (_vehicleInfos[_currVehicleIndex].SkinIndex + 1) >= skinsCount
                        ? (byte)0 : ++_vehicleInfos[_currVehicleIndex].SkinIndex;

                    // _vehiclePrefabs[_currVehicleSkinIndex].SetActive(true);
                    _vehiclePrefabs[_currVehicleIndex].transform.GetChild(4).GetChild(0)
                        .GetComponent<MeshRenderer>().material =
                        _vehicleInfos[_currVehicleIndex].SkinsMat[_vehicleInfos[_currVehicleIndex].SkinIndex];
                    break;

                case ShopUIController.InteractionStatus.PREV_VEHICLE_SKIN:
                    skinsCount = _vehicleInfos[_currVehicleIndex].SkinsMat.Length;
                    _vehicleInfos[_currVehicleIndex].SkinIndex = (_vehicleInfos[_currVehicleIndex].SkinIndex - 1) < 0
                        ? (byte)(skinsCount - 1) : --_vehicleInfos[_currVehicleIndex].SkinIndex;

                    // _vehiclePrefabs[_currVehicleSkinIndex].SetActive(true);
                    _vehiclePrefabs[_currVehicleIndex].transform.GetChild(4).GetChild(0)
                        .GetComponent<MeshRenderer>().material =
                        _vehicleInfos[_currVehicleIndex].SkinsMat[_vehicleInfos[_currVehicleIndex].SkinIndex];
                    break;
            }
        }
    }
}