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
            public int VehicleType;

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

        private const int _cVehicleLayerMask = (1 << 6);

        private void OnDestroy()
        {
            GameManager.Instance.OnSelect -= VehicleSelected;
        }

        private void Start()
        {
            GameManager.Instance.OnSelect += VehicleSelected;

            vehicleSpawner = new VehicleSpawner();

            // vehicleSpawner.SpawnVehicles(InitializeVehicleData);
            vehicleSpawner.SpawnVehicles2((vehicleTypes) => InitializeVehicleData(vehicleTypes));
            // vehicleSpawner.SpanwVehiclesTest();
            // _vehicleInfos = new VehicleInfo[vehicleSpawner.VehiclesSpawned.Count];
        }

        private void InitializeVehicleData(in int[] vehicleTypes)
        {
            _vehiclesSpawned = true;
            _vehicleInfos = new VehicleInfo[vehicleSpawner.VehiclesSpawned.Count];
            for (int i = 0; i < _vehicleInfos.Length; i++)
                _vehicleInfos[i].VehicleType = vehicleTypes[i];
        }

        void Update()
        {
            if (!_vehiclesSpawned) return;
            MoveVehicle();
            FerryAroundThePark();
        }

        private void FixedUpdate()
        {
            if (!_vehiclesSpawned) return;
            CheckCollisions();
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

        private void CheckCollisions()
        {
            /*             
                |   | i | i |   |
                -----------------
                | - | x | x | - |
                -----------------
                | - | x | x | - |
                -----------------
                | - | x | x | - |
                -----------------
                |   | i | i |   |   

                -> We would only need to check in the direction the vehicle is travelling, as the vehicle will not 
                    move sideways anyway. Removing the side checks will be okay
                -> If we keep the side checks, if the vehicle moves parallel to another vehicle on the next cell
                    , then the check would return true
                -> No need to check the opposite travelling direction also, as the vehicle moving in one direction
                    cant hit anything in its opposite direction
                -> In case of collisions, the vehicle colliding can check for the collided vehicle, no need for extra
                    checks from the collided vehicle, about which vehicle hit. Can manipulate hit-vehicle from hitInfo.
            */

            Vector3 rayStartPos;
            RaycastHit colliderHitInfo;
            for (int i = 0; i < _vehicleInfos.Length; i++)
            {
                //Check if the vehicle has been interacted with 
                if ((_vehicleInfos[i].VehicleStatus & (1 << (int)VehicleStatus.INTERACTED)) != 0)
                {
                    rayStartPos = vehicleSpawner.VehiclesSpawned[i].position;

                    //Check Orientation
                    //Vertical Alignment
                    if ((_vehicleInfos[i].VehicleStatus & (1 << (int)VehicleStatus.ALIGNMENT)) != 0)
                    {
                        /*switch ((PoolManager.PoolType)_vehicleInfos[i].VehicleType)
                        {
                            case PoolManager.PoolType.VEHICLE_S:
                                rayStartPos.z += 0.25f * (_vehicleInfos[i].VehicleType + 1);          //Vehicle_3
                                break;
                            case PoolManager.PoolType.VEHICLE_M:
                                rayStartPos.z += 0.25f * (_vehicleInfos[i].VehicleType + 1);          //Vehicle_3
                                break;
                            case PoolManager.PoolType.VEHICLE_L:
                                rayStartPos.z += 0.25f * (_vehicleInfos[i].VehicleType + 1);          //Vehicle_3
                                break;
                        }*/
                        rayStartPos.z += 0.25f * (_vehicleInfos[i].VehicleType + 1);          //Vehicle_3
                    }
                    //Horizontal Alignment
                    else
                    {
                        /*switch ((PoolManager.PoolType)_vehicleInfos[i].VehicleType)
                        {
                            case PoolManager.PoolType.VEHICLE_S:
                                rayStartPos.x += 0.75f;          //Vehicle_3
                                break;
                            case PoolManager.PoolType.VEHICLE_M:
                                rayStartPos.x += 0.75f;          //Vehicle_3
                                break;
                            case PoolManager.PoolType.VEHICLE_L:
                                rayStartPos.x += 0.75f;          //Vehicle_3
                                break;
                        }*/
                        rayStartPos.x += 0.25f * (_vehicleInfos[i].VehicleType + 1);          //Vehicle_3
                    }

                    // Debug.Log($"Checking Vehicle | index: {i} | name: {vehicleSpawner.VehiclesSpawned[i].name}"
                    // + $" | rayPos: {rayStartPos} | Pos: {vehicleSpawner.VehiclesSpawned[i].position}");
                    for (int j = 1; j >= -2; j -= 3)
                    {
                        rayStartPos.x += 0.25f * j;
                        if (Physics.Raycast(rayStartPos, Vector3.forward, out colliderHitInfo, 0.25f))
                        {
                            // Debug.Log($"Hit | Point: {colliderHitInfo.point} | name: {colliderHitInfo.transform.name}");
                        }
                    }
                }
            }
        }
    }
}