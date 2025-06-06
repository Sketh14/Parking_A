#define COLLISION_DEBUG_DRAW_2
#define COLLISION_DEBUG_DRAW_1

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Parking_A.Gameplay
{
    public class VehicleController : MonoBehaviour
    {
        [Serializable]
        internal struct VehicleInfo
        {
            // public bool hasInteracted;
            /// <summary> Left: [-1, 0] | Right: [1, 0] | Up: [0, 1] | Down: [0, -1] </summary>
            public Vector2 InteractedDir;
            public int MarkerIndex;
            public int VehicleType;
            public int ActivityCount;

            /// <summary> 0: Interacted or Not | 1: Vertical/Horizontal | 2: Reached Road Or Not | 3: Ferry Around The Road</summary>
            public VehicleStatus VehicleStatus;
        }

        internal enum VehicleStatus
        {
            NOT_INTERACTED = 0,
            INTERACTED = 1 << 0,
            ALIGNMENT = 1 << 1,
            REACHED_ROAD = 1 << 2,
            FERRY_AROUND = 1 << 3,
            LEFT_PARKING = 1 << 4,
            ONBOARDING_ROAD = 1 << 5,
            COLLIDED_ONBOARDING = 1 << 6,
        }
        internal enum RoadMarkers { TOP_RIGHT, BOTTOM_RIGHT, BOTTOM_LEFT, TOP_LEFT, LEFT_PARKING }

        // private Transform[] _vehicleTransforms;
        private VehicleInfo[] _vehicleInfos;
        private int _vehicleSetID;

        private const float _cVehicleSpeedMultiplier = 5f;
        private readonly float[] _roadBoundaries = { 11.1f, 6.1f };                 //Vertical | Horizontal
        private readonly float[] _vehicleSizes = { 0.5f, 0.75f, 1.0f };                 //Vertical | Horizontal

        private VehicleSpawner _vehicleSpawner;
        private bool _vehiclesSpawned = false;
        private System.Text.StringBuilder _vehicleName;

        [SerializeField] private GameConfigScriptableObject _mainGameConfig;

        private Func<Vector3, Vector3> _roundPosition;

        private const int _cCollisionCheckLayerMask = (1 << 6) | (1 << 7);
        private const float _cGridCellSize = 0.25f;

        private void OnDestroy()
        {
            GameManager.Instance.OnSelect -= VehicleSelected;
        }

        private void Start()
        {
            GameManager.Instance.OnSelect += VehicleSelected;

            InitializeLevel();
            _vehicleName = new System.Text.StringBuilder();
        }

        private async void InitializeLevel()
        {
            if (_mainGameConfig.RandomizeLevel)
            {
                string tempRandomSeed = DateTime.Now.Day.ToString() + DateTime.Now.Hour.ToString()
                    + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString();
                GameManager.Instance.RandomSeed = tempRandomSeed;
                Debug.Log($"Selected Random Seed: {tempRandomSeed}");
            }
            else
                GameManager.Instance.RandomSeed = "SKETH";

            EnvironmentSpawner envSpawner = new EnvironmentSpawner();
            _vehicleSpawner = new VehicleSpawner();

            byte[] boundaryData = null;

            GameManager.Instance.GameStatus |= Global.UniversalConstant.GameStatus.BOUNDARY_GENERATION;
            try
            {
                await envSpawner.SpawnBoundary((values) => boundaryData = values);
            }
            //Cannot initialize boundary | Stop level generation, show some message and restart
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            GameManager.Instance.GameStatus |= Global.UniversalConstant.GameStatus.VEHICLE_SPAWNING;
            try
            {
                // vehicleSpawner.SpawnVehicles(InitializeVehicleData);
                // vehicleSpawner.SpanwVehiclesTest();
                // _vehicleInfos = new VehicleInfo[vehicleSpawner.VehiclesSpawned.Count];
                if (boundaryData == null)
                {
                    Debug.LogError($"Boundary Data is null");
                    return;
                }
                _vehicleSpawner.SpawnVehicles2(boundaryData, (vehicleTypes) => InitializeVehicleData(vehicleTypes));
            }
            //Cannot initialize vehicles | Stop level generation, show some message and restart
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            GameManager.Instance.GameStatus |= Global.UniversalConstant.GameStatus.LEVEL_GENERATED;
        }

        private void InitializeVehicleData(in int[] vehicleTypes)
        {
            _vehiclesSpawned = true;
            _vehicleInfos = new VehicleInfo[_vehicleSpawner.VehiclesSpawned.Count];
            for (int i = 0; i < _vehicleInfos.Length; i++)
                _vehicleInfos[i].VehicleType = vehicleTypes[i];
        }


        public bool slowTime = false;
        void Update()
        {
            if (!_vehiclesSpawned) return;
            MoveVehicle();
            FerryAroundThePark();

            if (slowTime)
            {
                Time.timeScale = 0.15f;
                slowTime = false;
            }
        }

        private void FixedUpdate()
        {
            if (!_vehiclesSpawned) return;
            CheckCollisions();
            WaitForVehicleToPass();
        }

        private void VehicleSelected(int vehicleID, Vector2 slideDir)
        {
            // Debug.Log($"vehicleID: {vehicleID} | slideDir : {slideDir} | Diff = {(Mathf.Abs(slideDir.y) - 0.75f)}");
            for (int i = 0; i < _vehicleSpawner.VehiclesSpawned.Count; i++)
            {
                if (vehicleID == _vehicleSpawner.VehiclesSpawned[i].GetInstanceID()
                    // && !vehicleInfos[i].hasInteracted
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.INTERACTED) == 0)
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
                    if (Mathf.Abs(_vehicleSpawner.VehiclesSpawned[i].forward[2]) >= 0.9f
                        && (Mathf.Abs(slideDir.y) - 0.75f) > 0f)
                    {
                        _vehicleInfos[i].VehicleStatus |= VehicleStatus.INTERACTED;

                        // vehicleInfos[i].interactedDir.y = Mathf.RoundToInt(slideDir.y);     //Dont work well
                        // _vehicleInfos[i].InteractedDir.y = (Mathf.Abs(slideDir.y) - 0.75f) > 0f ? Mathf.RoundToInt(slideDir.y) : 0f;
                        _vehicleInfos[i].InteractedDir.y = Mathf.RoundToInt(slideDir.y);
                        _vehicleInfos[i].InteractedDir.x = 0f;
                        _vehicleInfos[i].VehicleStatus |= VehicleStatus.ALIGNMENT;
                    }
                    //LEFT | RIGHT
                    else if (Mathf.Abs(_vehicleSpawner.VehiclesSpawned[i].forward[0]) >= 0.9f
                        && (Mathf.Abs(slideDir.x) - 0.75f) > 0f)
                    {
                        _vehicleInfos[i].VehicleStatus |= VehicleStatus.INTERACTED;

                        // _vehicleInfos[i].InteractedDir.x = (Mathf.Abs(slideDir.x) - 0.75f) > 0f ? Mathf.RoundToInt(slideDir.x) : 0f;
                        _vehicleInfos[i].InteractedDir.x = Mathf.RoundToInt(slideDir.x);
                        _vehicleInfos[i].InteractedDir.y = 0f;
                        _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ALIGNMENT;
                    }

                    _vehicleName.Clear();
                    _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[i].name, 0,
                        _vehicleSpawner.VehiclesSpawned[i].name.Length - 2);
                    _vehicleName.Append(((int)VehicleStatus.INTERACTED).ToString("00"));
                    _vehicleSpawner.VehiclesSpawned[i].name = _vehicleName.ToString();
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
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.INTERACTED) != 0
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.REACHED_ROAD) == 0
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.COLLIDED_ONBOARDING) == 0)
                {
                    vehiclePos = _vehicleSpawner.VehiclesSpawned[i].transform.position;

                    // Testing
                    vehiclePos.Set(_vehicleInfos[i].InteractedDir.x, 0f, _vehicleInfos[i].InteractedDir.y);
                    // vehiclePos.x = _vehicleInfos[i].InteractedDir.x;vehiclePos.y = 0f;vehiclePos.z = _vehicleInfos[i].InteractedDir.y;
                    // vehiclePos += Time.deltaTime ;
                    _vehicleSpawner.VehiclesSpawned[i].transform.position += vehiclePos * _cVehicleSpeedMultiplier * Time.deltaTime;
                    // vehicleTransforms[i].transform.position = vehiclePos + vehicleInfos[i].interactedDir * 10f;

                    //Check if the vehicle has reached the "Road" and then disable it
                    //For Vertical Alignment
                    if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.ALIGNMENT) != 0)
                    {
                        // vehicleInfos[i].hasInteracted = false;
                        // vehicleInfos[i].vehicleStatus = 0;

                        if (_vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0])
                        {
                            _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.TOP_RIGHT;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.REACHED_ROAD;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.FERRY_AROUND;
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                            _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 90f, 0f);

                            vehiclePos = _vehicleSpawner.VehiclesSpawned[i].position;
                            vehiclePos.z = _roadBoundaries[0];
                            _vehicleSpawner.VehiclesSpawned[i].position = vehiclePos;

                            RenameVehicle(i);
                            // _vehicleName.Clear();
                            // _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[i].name, 0
                            //     , _vehicleSpawner.VehiclesSpawned[i].name.Length - 2);
                            // _vehicleName.Append(((int)_vehicleInfos[i].VehicleStatus).ToString("d2"));
                            // _vehicleSpawner.VehiclesSpawned[i].name = _vehicleName.ToString();
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0] - 1.0f)
                        {
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                            // Debug.Log($"Reaching Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.z <= _roadBoundaries[0] * -1f)
                        {
                            _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.BOTTOM_LEFT;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.REACHED_ROAD;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.FERRY_AROUND;
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                            _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 270f, 0f);

                            vehiclePos = _vehicleSpawner.VehiclesSpawned[i].position;
                            vehiclePos.z = _roadBoundaries[0] * -1f;
                            _vehicleSpawner.VehiclesSpawned[i].position = vehiclePos;

                            RenameVehicle(i);
                            // _vehicleName.Clear();
                            // _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[i].name, 0
                            //     , _vehicleSpawner.VehiclesSpawned[i].name.Length - 2);
                            // _vehicleName.Append(((int)_vehicleInfos[i].VehicleStatus).ToString("d2"));
                            // _vehicleSpawner.VehiclesSpawned[i].name = _vehicleName.ToString();
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.z <= (_roadBoundaries[0] * -1f) + 1.0f)
                        {
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                            // Debug.Log($"Reaching Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                        }
                    }
                    //For Horizontal Alignment
                    else
                    {
                        if (_vehicleSpawner.VehiclesSpawned[i].position.x >= _roadBoundaries[1])
                        {
                            // Debug.Log($"Reached Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                            _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.BOTTOM_RIGHT;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.REACHED_ROAD;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.FERRY_AROUND;
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                            _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 180f, 0f);

                            vehiclePos = _vehicleSpawner.VehiclesSpawned[i].position;
                            vehiclePos.x = _roadBoundaries[1];
                            _vehicleSpawner.VehiclesSpawned[i].position = vehiclePos;

                            RenameVehicle(i);
                            // _vehicleName.Clear();
                            // _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[i].name, 0
                            //     , _vehicleSpawner.VehiclesSpawned[i].name.Length - 2);
                            // _vehicleName.Append(((int)_vehicleInfos[i].VehicleStatus).ToString("d2"));
                            // _vehicleSpawner.VehiclesSpawned[i].name = _vehicleName.ToString();
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.x >= _roadBoundaries[1] - 1.0f)
                        {
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                            // Debug.Log($"Reaching Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.x <= _roadBoundaries[1] * -1f)
                        {
                            _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.TOP_LEFT;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.REACHED_ROAD;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.FERRY_AROUND;
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                            _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 0f, 0f);

                            vehiclePos = _vehicleSpawner.VehiclesSpawned[i].position;
                            vehiclePos.x = _roadBoundaries[1] * -1f;
                            _vehicleSpawner.VehiclesSpawned[i].position = vehiclePos;

                            RenameVehicle(i);
                            // _vehicleName.Clear();
                            // _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[i].name, 0
                            //     , _vehicleSpawner.VehiclesSpawned[i].name.Length - 2);
                            // _vehicleName.Append(((int)_vehicleInfos[i].VehicleStatus).ToString("d2"));
                            // _vehicleSpawner.VehiclesSpawned[i].name = _vehicleName.ToString();
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.x <= (_roadBoundaries[1] * -1f) + 1.0f)
                        {
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                        }
                    }

                    // Debug.Log($"Vehicle Stats | ID: {i} | pos: {_vehicleSpawner.VehiclesSpawned[i].transform.position} | "
                    // + $" interactedDir: {_vehicleInfos[i].InteractedDir}");
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
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.REACHED_ROAD) != 0
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.FERRY_AROUND) != 0
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.COLLIDED_ONBOARDING) == 0)
                {
                    switch (_vehicleInfos[i].MarkerIndex)
                    {
                        case (int)RoadMarkers.TOP_RIGHT:
                            vehiclePos.Set(1f, 0f, 0f);
                            if (_vehicleSpawner.VehiclesSpawned[i].position.x >= _roadBoundaries[1])
                            {
                                _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.BOTTOM_RIGHT;
                                _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 180f, 0f);
                            }
                            break;

                        case (int)RoadMarkers.BOTTOM_RIGHT:
                            vehiclePos.Set(0f, 0f, -1f);
                            if (_vehicleSpawner.VehiclesSpawned[i].position.z <= _roadBoundaries[0] * -1f)
                            {
                                _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.BOTTOM_LEFT;
                                _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 270f, 0f);
                            }
                            break;

                        case (int)RoadMarkers.BOTTOM_LEFT:
                            vehiclePos.Set(-1f, 0f, 0f);
                            if (_vehicleSpawner.VehiclesSpawned[i].position.x <= _roadBoundaries[1] * -1f)
                            {
                                _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.TOP_LEFT;
                                _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = new Vector3(0f, 0f, 0f);
                            }
                            break;

                        case (int)RoadMarkers.TOP_LEFT:
                            vehiclePos.Set(0f, 0f, 1f);
                            if (_vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0])
                                _vehicleInfos[i].MarkerIndex = (int)RoadMarkers.LEFT_PARKING;
                            break;

                        case (int)RoadMarkers.LEFT_PARKING:
                            vehiclePos.Set(0f, 0f, 1f);
                            if (_vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0] + 4f)
                            {
                                _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.FERRY_AROUND;

                                RenameVehicle(i);
                                // _vehicleName.Clear();
                                // _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[i].name, 0,
                                //      _vehicleSpawner.VehiclesSpawned[i].name.Length - 2);
                                // _vehicleName.Append(((int)_vehicleInfos[i].VehicleStatus).ToString("d2"));
                                // _vehicleSpawner.VehiclesSpawned[i].name = _vehicleName.ToString();
                            }
                            break;

                        default:
                            Debug.LogError($"Wrong Marker Indexx : {_vehicleInfos[i].MarkerIndex}");
                            break;
                    }
                    _vehicleSpawner.VehiclesSpawned[i].transform.position += vehiclePos * _cVehicleSpeedMultiplier * Time.deltaTime;
                }
            }
        }

        private void CheckCollisions()
        {
            /*             
                |   | i | i |   |       |   | i | i | i |   |
                -----------------       ---------------------
                | - | x | x | - |       | - | x | x | x | - |
                -----------------       ---------------------
                | - | x | x | - |       | - | x | x | x | - |
                -----------------       ---------------------
                | - | x | x | - |       |   | i | i | i |   |
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

            Vector3 rayStartPos, rayDir, vehiclePos;
            RaycastHit colliderHitInfo;
            int vehicleStatus;
            for (int i = 0; i < _vehicleInfos.Length; i++)
            {
                //Check if the vehicle has been interacted with or have reached the road
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.INTERACTED) == 0
                    || (_vehicleInfos[i].VehicleStatus & VehicleStatus.REACHED_ROAD) != 0
                    || (_vehicleInfos[i].VehicleStatus & VehicleStatus.COLLIDED_ONBOARDING) != 0)
                    continue;

                rayStartPos = _vehicleSpawner.VehiclesSpawned[i].position;
                rayDir = Vector3.zero;

                //Check Orientation and set Raycast-Points / Raycast-Directions
                //Vertical Alignment
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.ALIGNMENT) != 0)
                {
                    rayStartPos.z += _cGridCellSize * _vehicleInfos[i].InteractedDir.y * (_vehicleInfos[i].VehicleType + 1);
                    rayDir.z = _vehicleInfos[i].InteractedDir.y;

                    _roundPosition = (posToChange) =>
                    {
                        // Debug.Log($"Vertical | posToChange: {posToChange} | Dir: {_vehicleInfos[i].InteractedDir} "
                        //     + $"| Vehicle-Type: {_vehicleInfos[i].VehicleType}");
                        posToChange.z = posToChange.z + (0.05f * -1f * _vehicleInfos[i].InteractedDir.y)
                                                    + (_vehicleSizes[_vehicleInfos[i].VehicleType - 1] * -1f * _vehicleInfos[i].InteractedDir.y);
                        posToChange.x = _vehicleSpawner.VehiclesSpawned[i].position.x;
                        posToChange.y = _vehicleSpawner.VehiclesSpawned[i].position.y;
                        return posToChange;
                    };
                }
                //Horizontal Alignment
                else
                {
                    rayStartPos.x += _cGridCellSize * _vehicleInfos[i].InteractedDir.x * (_vehicleInfos[i].VehicleType + 1);
                    rayDir.x = _vehicleInfos[i].InteractedDir.x;

                    _roundPosition = (posToChange) =>
                    {
                        // Debug.Log($"Horizontal | posToChange: {posToChange} | Dir: {_vehicleInfos[i].InteractedDir} "
                        //     + $"| Vehicle-Type: {_vehicleInfos[i].VehicleType}");
                        posToChange.x = posToChange.x + (0.05f * -1f * _vehicleInfos[i].InteractedDir.x)
                                                    + (_vehicleSizes[_vehicleInfos[i].VehicleType - 1] * -1f * _vehicleInfos[i].InteractedDir.x);
                        posToChange.z = _vehicleSpawner.VehiclesSpawned[i].position.z;
                        posToChange.y = _vehicleSpawner.VehiclesSpawned[i].position.y;
                        return posToChange;
                    };
                }

                // Debug.Log($"Checking Vehicle | index: {i} | name: {vehicleSpawner.VehiclesSpawned[i].name}"
                // + $" | interactedDir: {_vehicleInfos[i].InteractedDir}"
                // + $" | rayPos: {rayStartPos} | Pos: {vehicleSpawner.VehiclesSpawned[i].position}");
                for (int j = 1; j >= -2; j -= 3)
                {
                    //Using Opposite, as the 2nd component is to be shifted up/down
                    rayStartPos.x += _cGridCellSize * Mathf.Abs(_vehicleInfos[i].InteractedDir.y) * j;       //Vertical
                    rayStartPos.z += _cGridCellSize * Mathf.Abs(_vehicleInfos[i].InteractedDir.x) * j;       //Horizontal
#if COLLISION_DEBUG_DRAW_1
                    Debug.DrawRay(rayStartPos, rayDir * 0.1f, Color.cyan);
#endif
                    if (Physics.Raycast(rayStartPos, rayDir, out colliderHitInfo, 0.1f, _cCollisionCheckLayerMask))
                    {
                        {
                            //- Instead of fully stopping when the vehicle encounters another vehicle oncoming on the road
                            //  [=] Add it to some kind of queue?
                            //  [=] Keep checking if the vehicle has passed and there is ample space behind it to fit the current vehicle
                            //  [=] The oncoming raycast-hit vehicle, use it to raycast in the backwards direction to check if 
                            //      there is space to fit the current vehicle
                            //  [=] Just need to raycast or check for collision on the down side or whichever direction the car is 
                            //      coming from, to check if there is enough space to fit the current vehicle
                            //      {+} [Wrong] Only 1 point is needed, as the passing vehicle which raycast hit, would have already passed? 
                            //      {+} 2 points needed, 1 to the upper side, forward-side and 1 point with offset to the down-side, back-side
                            //          . So as to check if there is any incoming vehicle or not and if the passing vehicle is gone
                        }

                        int.TryParse(colliderHitInfo.transform.name[(colliderHitInfo.transform.name.Length - 2)..]
                            , out vehicleStatus);
                        // [colliderHitInfo.transform.name.Length - 1] - '0';

                        // Debug.Log($"Hit | Point: {colliderHitInfo.point} | name: {colliderHitInfo.transform.name}"
                        //     + $" | zPos: {_vehicleSpawner.VehiclesSpawned[i].position.z} | vehicleStatus: {vehicleStatus}");

                        //Check if the vehicle is still inside parking
                        if (vehicleStatus == 0)
                            _vehicleInfos[i].VehicleStatus = 0;
                        else
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.COLLIDED_ONBOARDING;

                        RenameVehicle(i);
                        // _vehicleName.Clear();
                        // _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[i].name, 0,
                        //      _vehicleSpawner.VehiclesSpawned[i].name.Length - 2);
                        // _vehicleName.Append(((int)_vehicleInfos[i].VehicleStatus).ToString("d2"));
                        // _vehicleSpawner.VehiclesSpawned[i].name = _vehicleName.ToString();

                        //Round down position to multiples of _cGridCellSize when stopping the vehicle
                        // vehiclePos = vehicleSpawner.VehiclesSpawned[i].position;
                        // vehiclePos.z = _roundPosition(vehiclePos.z) * _cGridCellSize;
                        vehiclePos = _roundPosition(colliderHitInfo.point);
                        _vehicleSpawner.VehiclesSpawned[i].position = vehiclePos;
                        break;
                    }
                }
            }
        }

        private void WaitForVehicleToPass()
        {
            Vector3 rayStartPos, rayDir, tempRayPos;
            RaycastHit colliderHitInfo;
            for (int i = 0; i < _vehicleInfos.Length; i++)
            {
                //Check if the vehicle has been interacted with or have reached the road
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.COLLIDED_ONBOARDING) == 0 &&
                    (_vehicleInfos[i].VehicleStatus & VehicleStatus.ONBOARDING_ROAD) == 0)
                    continue;

                rayStartPos = _vehicleSpawner.VehiclesSpawned[i].position;
                rayDir = Vector3.zero;

                //Check Orientation and set Raycast-Points / Raycast-Directions
                //Vertical Alignment
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.ALIGNMENT) != 0)
                {
                    rayStartPos.z += (_cGridCellSize + 0.1f) * _vehicleInfos[i].InteractedDir.y * (_vehicleInfos[i].VehicleType + 1);
                    rayDir.z = _vehicleInfos[i].InteractedDir.y;

                    // rayStartPos.x += (_cGridCellSize + 0.1f) * Mathf.Abs(_vehicleInfos[i].InteractedDir.y) * (_vehicleInfos[i].VehicleType + 1);
                    // rayDir.x = -1.0f;
                }
                //Horizontal Alignment
                else
                {
                    rayStartPos.x += (_cGridCellSize - 0.05f) * _vehicleInfos[i].InteractedDir.x * (_vehicleInfos[i].VehicleType + 1);
                    rayDir.x = _vehicleInfos[i].InteractedDir.x;

                    // rayStartPos.z += (_cGridCellSize + 0.1f) * Mathf.Abs(_vehicleInfos[i].InteractedDir.x) * (_vehicleInfos[i].VehicleType + 1);
                    // rayDir.z = -1.0f;
                }

                // Debug.Log($"Checking Vehicle | index: {i} | name: {_vehicleSpawner.VehiclesSpawned[i].name}"
                // + $" | interactedDir: {_vehicleInfos[i].InteractedDir}"
                // + $" | rayPos: {rayStartPos} | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                for (int j = 1; j > -2; j--)
                {
                    tempRayPos = rayStartPos;
                    tempRayPos.z += (_cGridCellSize + 0.1f) * Mathf.Abs(_vehicleInfos[i].InteractedDir.x) * (_vehicleInfos[i].VehicleType + 1) * j;
                    tempRayPos.x += (_cGridCellSize + 0.1f) * Mathf.Abs(_vehicleInfos[i].InteractedDir.y) * (_vehicleInfos[i].VehicleType + 1) * j;

#if COLLISION_DEBUG_DRAW_2
                    Debug.DrawRay(tempRayPos, rayDir * _cGridCellSize * 2f * _vehicleInfos[i].VehicleType, Color.cyan);
#endif
                    // Debug.DrawRay(tempRayPos, rayDir * _cGridCellSize * 2.75f * (_vehicleInfos[i].VehicleType + 1), Color.cyan);

                    // Raycast straight in front of the vehicle perpendicular to the vehicle with the length of the vehicle plus offset
                    // if (!Physics.Raycast(tempRayPos, rayDir, out colliderHitInfo, _cGridCellSize * 2.75f * (_vehicleInfos[i].VehicleType + 1), _cCollisionCheckLayerMask))
                    if (!Physics.Raycast(tempRayPos, rayDir, out colliderHitInfo, _cGridCellSize * 2f * _vehicleInfos[i].VehicleType, _cCollisionCheckLayerMask))
                    {
                        // {+} 2 points needed, 1 to the upper side, forward-side and 1 point with offset to the down-side, back-side
                        //     . So as to check if there is any incoming vehicle or not and if the passing vehicle is gone
                        // _vehicleInfos[i].ActivityCount++;
                        _vehicleInfos[i].ActivityCount |= 1 << (j + 1);
                        // Debug.Log($"Not Hit[{j}]");

                        // if (_vehicleInfos[i].ActivityCount > 5)           //Arbitrary value | Wait enough for vehicle to pass
                        if ((_vehicleInfos[i].ActivityCount & (1 << 0)) != 0 &&
                            (_vehicleInfos[i].ActivityCount & (1 << 1)) != 0 &&
                            (_vehicleInfos[i].ActivityCount & (1 << 2)) != 0)
                        {
                            // Debug.Log($"All Clear");
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.COLLIDED_ONBOARDING;
                            _vehicleInfos[i].ActivityCount = 0;

                            RenameVehicle(i);
                            // _vehicleName.Clear();
                            // _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[i].name, 0,
                            //      _vehicleSpawner.VehiclesSpawned[i].name.Length - 2);
                            // _vehicleName.Append(((int)_vehicleInfos[i].VehicleStatus).ToString("d2"));
                            // _vehicleSpawner.VehiclesSpawned[i].name = _vehicleName.ToString();
                        }
                    }
                    else
                    {
                        // _vehicleInfos[i].ActivityCount = 0;
                        _vehicleInfos[i].ActivityCount &= ~(1 << (j + 1));
                        _vehicleInfos[i].VehicleStatus |= VehicleStatus.COLLIDED_ONBOARDING;
                        _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                        // Debug.Log($"Hit[{j}] | hitPoint: {colliderHitInfo.point}");
                    }
                }
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RenameVehicle(int vehicleIndex)
        {
            _vehicleName.Clear();
            _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[vehicleIndex].name, 0
                , _vehicleSpawner.VehiclesSpawned[vehicleIndex].name.Length - 2);
            _vehicleName.Append(((int)_vehicleInfos[vehicleIndex].VehicleStatus).ToString("d2"));
            _vehicleSpawner.VehiclesSpawned[vehicleIndex].name = _vehicleName.ToString();
        }
    }
}