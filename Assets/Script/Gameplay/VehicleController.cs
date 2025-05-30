using System;
using UnityEngine;

namespace Test_A.Gameplay
{
    public class VehicleController : MonoBehaviour
    {
        [Serializable]
        internal struct VehicleInfo
        {
            // public bool hasInteracted;
            public Vector2 InteractedDir;
            public int MarkerIndex;

            /// <summary> 0: Interacted or Not | 1: Vertical/Horizontal | 2: Reached Road Or Not | 3: Ferry Around The Road</summary>
            public int VehicleStatus;
        }

        internal enum VehicleStatus { INTERACTED, ALIGNMENT, REACHED_ROAD, FERRY_AROUND }
        internal enum RoadMarkers { TOP_RIGHT, BOTTOM_RIGHT, BOTTOM_LEFT, TOP_LEFT, LEFT_PARKING }

        // private Transform[] _vehicleTransforms;
        private VehicleInfo[] _vehicleInfos;
        private int _vehicleSetID;

        private const float _cVehicleSpeedMultiplier = 5f;
        private readonly float[] _roadBoundaries = { 11f, 6f };                 //Vertical | Horizontal

        public VehicleSpawner vehicleSpawner;
        private bool _vehiclesSpawned = false;

        private void OnDestroy()
        {
            GameManager.Instance.OnSelect -= VehicleSelected;
        }

        private void Start()
        {
            GameManager.Instance.OnSelect += VehicleSelected;

            vehicleSpawner = new VehicleSpawner();

            // vehicleSpawner.SpawnVehicles(InitializeVehicleData);
            vehicleSpawner.SpawnVehicles2(InitializeVehicleData);
            // vehicleSpawner.SpanwVehiclesTest();
            // _vehicleInfos = new VehicleInfo[vehicleSpawner.VehiclesSpawned.Count];
        }

        private void InitializeVehicleData()
        {
            _vehiclesSpawned = true;
            _vehicleInfos = new VehicleInfo[vehicleSpawner.VehiclesSpawned.Count];
        }

        // Update is called once per frame
        void Update()
        {
            if (!_vehiclesSpawned) return;
            MoveVehicle();
            FerryAroundThePark();
        }

        private void VehicleSelected(int vehicleID, Vector2 slideDir)
        {
            // Debug.Log($"vehicleID: {vehicleID} | slideDir : {slideDir} | Diff = {(Mathf.Abs(slideDir.y) - 0.75f)}");
            for (int i = 0; i < vehicleSpawner.VehiclesSpawned.Count; i++)
            {
                if (vehicleID == vehicleSpawner.VehiclesSpawned[i].GetInstanceID()
                    // && !vehicleInfos[i].hasInteracted
                    && (_vehicleInfos[i].VehicleStatus & (1 << (int)VehicleStatus.INTERACTED)) == 0)
                {
                    // Debug.Log($"Found Vehicle! | Vehicle ID : {vehicleID}");
                    _vehicleSetID = i;
                    // vehicleInfos[i].hasInteracted = true;

                    //Validate if the slide direction is matching the vehicle's orientation
                    // bool vehicleOrientationVertical = Mathf.Abs(vehicleSpawner.VehiclesSpawned[i].forward[2]) >= 0.9f ? true : false;
                    // Debug.Log($"Vehicle Orientation: {Mathf.Abs(vehicleSpawner.VehiclesSpawned[i].forward[2])} "
                    // + $" | slideDir: {slideDir.y} | slidedir Rounded: {Mathf.RoundToInt(slideDir.y)}");
                    // + $" | VehicleOrientation: {vehicleOrientationVertical}");

                    // UP | DOWN
                    if (Mathf.Abs(vehicleSpawner.VehiclesSpawned[i].forward[2]) >= 0.9f
                        && (Mathf.Abs(slideDir.y) - 0.75f) > 0f)
                    {
                        _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.INTERACTED);

                        // vehicleInfos[i].interactedDir.y = Mathf.RoundToInt(slideDir.y);     //Dont work well
                        // _vehicleInfos[i].InteractedDir.y = (Mathf.Abs(slideDir.y) - 0.75f) > 0f ? Mathf.RoundToInt(slideDir.y) : 0f;
                        _vehicleInfos[i].InteractedDir.y = Mathf.RoundToInt(slideDir.y);
                        _vehicleInfos[i].InteractedDir.x = 0f;
                        _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.ALIGNMENT);
                    }
                    //LEFT | RIGHT
                    else if (Mathf.Abs(vehicleSpawner.VehiclesSpawned[i].forward[0]) >= 0.9f
                        && (Mathf.Abs(slideDir.x) - 0.75f) > 0f)
                    {
                        _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.INTERACTED);

                        // _vehicleInfos[i].InteractedDir.x = (Mathf.Abs(slideDir.x) - 0.75f) > 0f ? Mathf.RoundToInt(slideDir.x) : 0f;
                        _vehicleInfos[i].InteractedDir.x = Mathf.RoundToInt(slideDir.x);
                        _vehicleInfos[i].InteractedDir.y = 0f;
                        _vehicleInfos[i].VehicleStatus &= ~(1 << (int)VehicleStatus.ALIGNMENT);
                    }

                    // vehicleInfos[i].interactedDir = slideDir;
                }
            }
        }

        //Responsible for moving the vehicle
        private void MoveVehicle()
        {
            Vector3 vehiclePos;
            for (int i = 0; i < _vehicleInfos.Length; i++)
            {
                // if (vehicleInfos[i].hasInteracted)
                //Check if the vehicle has been interacted with and has not reached the road
                if ((_vehicleInfos[i].VehicleStatus & (1 << (int)VehicleStatus.INTERACTED)) != 0
                    && (_vehicleInfos[i].VehicleStatus & (1 << (int)VehicleStatus.REACHED_ROAD)) == 0)
                {
                    vehiclePos = vehicleSpawner.VehiclesSpawned[i].transform.position;

                    // Testing
                    vehiclePos.Set(_vehicleInfos[i].InteractedDir.x, 0f, _vehicleInfos[i].InteractedDir.y);
                    // vehiclePos.x = _vehicleInfos[i].InteractedDir.x;vehiclePos.y = 0f;vehiclePos.z = _vehicleInfos[i].InteractedDir.y;
                    // vehiclePos += Time.deltaTime ;
                    vehicleSpawner.VehiclesSpawned[i].transform.position += vehiclePos * _cVehicleSpeedMultiplier * Time.deltaTime;
                    // vehicleTransforms[i].transform.position = vehiclePos + vehicleInfos[i].interactedDir * 10f;

                    //Check if the vehicle has reached the "Road" and then disable it
                    //For Vertical Alignment
                    if ((_vehicleInfos[i].VehicleStatus & (1 << (int)VehicleStatus.ALIGNMENT)) != 0)
                    {
                        // vehicleInfos[i].hasInteracted = false;
                        // vehicleInfos[i].vehicleStatus = 0;

                        if (vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0])
                        {
                            _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.TOP_RIGHT;
                            _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.REACHED_ROAD);
                            _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.FERRY_AROUND);
                            vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 90f, 0f);
                        }
                        else if (vehicleSpawner.VehiclesSpawned[i].position.z <= _roadBoundaries[0] * -1f)
                        {
                            _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.BOTTOM_LEFT;
                            _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.REACHED_ROAD);
                            _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.FERRY_AROUND);
                            vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 270f, 0f);
                        }
                    }
                    //For Horizontal Alignment
                    else
                    {
                        if (vehicleSpawner.VehiclesSpawned[i].position.x >= _roadBoundaries[1])
                        {
                            _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.BOTTOM_RIGHT;
                            _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.REACHED_ROAD);
                            _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.FERRY_AROUND);
                            vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 180f, 0f);
                        }
                        else if (vehicleSpawner.VehiclesSpawned[i].position.x <= _roadBoundaries[1] * -1f)
                        {
                            _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.TOP_LEFT;
                            _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.REACHED_ROAD);
                            _vehicleInfos[i].VehicleStatus |= (1 << (int)VehicleStatus.FERRY_AROUND);
                            vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 0f, 0f);
                        }
                    }

                    // Debug.Log($"Vehicle Stats | ID: {i} | pos: {vehicleTransforms[i].transform.position} | "
                    // + $" interactedDir: {vehicleInfos[i].interactedDir}");
                }
            }
        }

        //Responsible for ferrying the vehicle round the park along the road
        private void FerryAroundThePark()
        {
            Vector3 vehiclePos = Vector3.zero;
            for (int i = 0; i < _vehicleInfos.Length; i++)
            {
                //Vehicle has reached the Road
                if ((_vehicleInfos[i].VehicleStatus & (1 << (int)VehicleStatus.REACHED_ROAD)) != 0
                    && (_vehicleInfos[i].VehicleStatus & (1 << (int)VehicleStatus.FERRY_AROUND)) != 0)
                {
                    switch (_vehicleInfos[i].MarkerIndex)
                    {
                        case (int)RoadMarkers.TOP_RIGHT:
                            vehiclePos.Set(1f, 0f, 0f);
                            if (vehicleSpawner.VehiclesSpawned[i].position.x >= _roadBoundaries[1])
                            {
                                _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.BOTTOM_RIGHT;
                                vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 180f, 0f);
                            }
                            break;

                        case (int)RoadMarkers.BOTTOM_RIGHT:
                            vehiclePos.Set(0f, 0f, -1f);
                            if (vehicleSpawner.VehiclesSpawned[i].position.z <= _roadBoundaries[0] * -1f)
                            {
                                _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.BOTTOM_LEFT;
                                vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 270f, 0f);
                            }
                            break;

                        case (int)RoadMarkers.BOTTOM_LEFT:
                            vehiclePos.Set(-1f, 0f, 0f);
                            if (vehicleSpawner.VehiclesSpawned[i].position.x <= _roadBoundaries[1] * -1f)
                            {
                                _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.TOP_LEFT;
                                vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 0f, 0f);
                            }
                            break;

                        case (int)RoadMarkers.TOP_LEFT:
                            vehiclePos.Set(0f, 0f, 1f);
                            if (vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0])
                                _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.LEFT_PARKING;
                            break;

                        case (int)RoadMarkers.LEFT_PARKING:
                            vehiclePos.Set(0f, 0f, 1f);
                            if (vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0] + 4f)
                                _vehicleInfos[i].VehicleStatus &= ~(1 << (int)VehicleStatus.FERRY_AROUND);
                            break;

                        default:
                            Debug.LogError($"Wrong Marker Indexx : {_vehicleInfos[i].MarkerIndex}");
                            break;
                    }
                    vehicleSpawner.VehiclesSpawned[i].transform.position += vehiclePos * _cVehicleSpeedMultiplier * Time.deltaTime;
                }
            }
        }
    }
}