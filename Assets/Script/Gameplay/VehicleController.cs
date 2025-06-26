#define COLLISION_DEBUG_DRAW_2
#define COLLISION_DEBUG_DRAW_1
#define CORNER_COLLISION_DEBUG_DRAW_1
#define DEBUG_SLOW_1

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Parking_A.Global;
using UnityEngine;

namespace Parking_A.Gameplay
{
    public class VehicleController : MonoBehaviour
    {
        [Serializable]
        internal class VehicleInfo
        {
            // public bool hasInteracted;
            /// <summary> Left: [-1, 0] | Right: [1, 0] | Up: [0, 1] | Down: [0, -1] </summary>
            public Vector2 InteractedDir;

            public Vector2 InitialPos;
            public float InitialRot;

            public int MarkerIndex;
            /// <summary> 1: Small | 2: Medium | 3: Long </summary>
            public byte OgVehicleType;
            public byte VehicleType;            // Vehicle type if power is used to change the vehicle

            /// <summary> 0: Interacted or Not | 1: Vertical/Horizontal | 2: Reached Road Or Not | 3: Ferry Around The Road</summary>
            public VehicleStatus VehicleStatus;
        }

        internal enum VehicleStatus
        {
            NOT_INTERACTED = 0, INTERACTED = 1 << 0, ALIGNMENT = 1 << 1,
            REACHED_ROAD = 1 << 2, FERRY_AROUND = 1 << 3, LEFT_PARKING = 1 << 4,
            ONBOARDING_ROAD = 1 << 5, CORNER_COLLIDED = 1 << 6, CORNER_FREE = 1 << 7,
            // COLLIDED_PARKING = 1 << 8,
            COLLIDED_ONBOARDING = 1 << 8, HIT_NPC = 1 << 9,
        }
        internal enum RoadMarkers { TOP_RIGHT, BOTTOM_RIGHT, BOTTOM_LEFT, TOP_LEFT, LEFT_PARKING }

        // private Transform[] _vehicleTransforms;
        private List<VehicleInfo> _vehicleInfos;
        // private int _vehicleSetID;

        private const float _vehicleSpeedMultiplierC = 5f;
        private readonly float[] _roadBoundaries = { 11.1f, 6.1f };                 //Vertical | Horizontal
        private readonly float[] _vehicleSizes = { 0.5f, 0.75f, 1.0f };                 //Vertical | Horizontal

        private VehicleSpawner _vehicleSpawner;
        // private bool _vehiclesSpawned = false;
        private System.Text.StringBuilder _vehicleName;

        [SerializeField] private GameConfigScriptableObject _mainGameConfig;

        private Func<Vector3, Vector3> _roundPosition;
        private int _vehicleExitedCount;

        private const int _collisionCheckLayerMaskC = (1 << 6) | (1 << 7);
        private const int _onBoardingLayerMaskC = 1 << 6;
        // private const int _movingVehicleLayerMaskC = 1 << 8;
        // private const float UniversalConstant._CellHalfSizeC = 0.25f;

        private void OnDestroy()
        {
            GameManager.Instance.OnSelect -= VehicleSelected;
            GameManager.Instance.OnEnvironmentSpawned -= CallVehicleSpawner;
            // GameManager.Instance.OnNPCHit -= DisableVehicle;
            GameManager.Instance.OnGameStatusChange -= UpdateVehicles;
        }

        private void Start()
        {
            GameManager.Instance.OnSelect += VehicleSelected;
            GameManager.Instance.OnEnvironmentSpawned += CallVehicleSpawner;
            // GameManager.Instance.OnNPCHit += DisableVehicle;
            GameManager.Instance.OnGameStatusChange += UpdateVehicles;

            _vehicleName = new System.Text.StringBuilder();
            _vehicleSpawner = new VehicleSpawner();
            _vehicleInfos = new List<VehicleInfo>();
        }

        private async void CallVehicleSpawner(byte[] boundaryData)
        {
            Debug.Log($"Spawning Vehicles");
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
                await _vehicleSpawner.SpawnVehicles2(boundaryData, InitializeVehicleData);
            }
            //Cannot initialize vehicles | Stop level generation, show some message and restart
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            // GameManager.Instance.GameStatus |= Global.UniversalConstant.GameStatus.VEHICLE_SPAWNED;
            GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.VEHICLE_SPAWNED);
            // GameManager.Instance.GameStatus |= Global.UniversalConstant.GameStatus.LEVEL_GENERATED;
        }

        private void InitializeVehicleData(int[] vehicleTypes)
        {
            // _vehiclesSpawned = true;
            // _vehicleInfos = new VehicleInfo[_vehicleSpawner.VehiclesSpawned.Count];
            for (int i = 0; i < vehicleTypes.Length; i++)
            {
                if (_vehicleInfos.Count <= i)
                    _vehicleInfos.Add(new VehicleInfo());

                _vehicleInfos[i].OgVehicleType = (byte)vehicleTypes[i];
                _vehicleInfos[i].VehicleType = 255;
                _vehicleInfos[i].InitialPos.Set(_vehicleSpawner.VehiclesSpawned[i].position.x,
                    _vehicleSpawner.VehiclesSpawned[i].position.z);
                _vehicleInfos[i].InitialRot = _vehicleSpawner.VehiclesSpawned[i].localEulerAngles.y;
            }
        }

        private void DisableVehicle(int vehicleID)
        {
            _vehicleInfos[vehicleID].VehicleStatus |= VehicleStatus.HIT_NPC;
        }

        private void UpdateVehicles(UniversalConstant.GameStatus gameStatus, int value)
        {
            // Debug.Log($"UpdateVehicles | gameStatus: {gameStatus}");
            switch (gameStatus)
            {
                case UniversalConstant.GameStatus.LEVEL_GENERATED:
                    for (int i = 0; i < _vehicleSpawner.VehiclesSpawned.Count; i++)
                        _vehicleSpawner.VehiclesSpawned[i].gameObject.SetActive(true);

                    break;

                case UniversalConstant.GameStatus.NPC_HIT:
                    _vehicleInfos[value].VehicleStatus |= VehicleStatus.HIT_NPC;
                    break;

                case UniversalConstant.GameStatus.RESET_LEVEL:
                    Vector3 vehiclePos, vehicleRot;
                    for (int i = 0; i < _vehicleSpawner.VehiclesSpawned.Count; i++)
                    {
                        // Vehicle has been changed to Small using power | Revert it back
                        if (_vehicleInfos[i].VehicleType != 255)
                        {
                            PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.VEHICLE_S]
                                .Release(_vehicleSpawner.VehiclesSpawned[i].gameObject);

                            _vehicleSpawner.VehiclesSpawned[i] =
                                PoolManager.Instance.PrefabPool[(UniversalConstant.PoolType)_vehicleInfos[i].OgVehicleType]
                                .Get().transform;
                        }

                        vehiclePos = _vehicleSpawner.VehiclesSpawned[i].position;
                        vehiclePos.x = _vehicleInfos[i].InitialPos.x;
                        vehiclePos.z = _vehicleInfos[i].InitialPos.y;
                        _vehicleSpawner.VehiclesSpawned[i].position = vehiclePos;
                        // _vehicleSpawner.VehiclesSpawned[i].position.Set(_vehicleInfos[i].InitialPos.x,
                        //     _vehicleSpawner.VehiclesSpawned[i].position.y, _vehicleInfos[i].InitialPos.y);

                        vehicleRot = Vector3.zero;
                        vehicleRot.y = _vehicleInfos[i].InitialRot;
                        _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = vehicleRot;
                        // _vehicleSpawner.VehiclesSpawned[i].localEulerAngles.Set(0f, _vehicleInfos[i].InitialRot, 0f);

                        _vehicleInfos[i].VehicleStatus = 0;
                        _vehicleInfos[i].VehicleType = 255;
                        _vehicleInfos[i].InteractedDir = Vector2.zero;
                        _vehicleSpawner.VehiclesSpawned[i].gameObject.SetActive(true);
                    }
                    break;

                case UniversalConstant.GameStatus.NEXT_LEVEL_REQUESTED:
                    for (int i = 0; i < _vehicleSpawner.VehiclesSpawned.Count; i++)
                    {
                        //Moved the logic down as the vehicles will get despawn as they exit the parking lot
                        // Vehicle has been changed to Small using power | Revert it back
                        /*if (_vehicleInfos[i].VehicleType != 255)
                        {
                            PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.VEHICLE_S]
                                .Release(_vehicleSpawner.VehiclesSpawned[i].gameObject);
                        }
                        else
                        {
                            PoolManager.Instance.PrefabPool[(UniversalConstant.PoolType)_vehicleInfos[i].OgVehicleType]
                                .Release(_vehicleSpawner.VehiclesSpawned[i].gameObject);
                        }*/

                        //These do not need explicit reset
                        _vehicleInfos[i].OgVehicleType = 0;
                        _vehicleInfos[i].MarkerIndex = 0;
                        _vehicleInfos[i].InitialRot = 0;

                        _vehicleInfos[i].VehicleStatus = 0;
                        _vehicleInfos[i].VehicleType = 255;
                        _vehicleInfos[i].InteractedDir = Vector2.zero;
                    }

                    _vehicleExitedCount = 0;
                    _vehicleSpawner.ClearVehicles();
                    GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.VEHICLE_SPAWNED, false);
                    break;
            }
        }

#if DEBUG_SLOW_1
        public bool slowTime = false, slowTime2 = false;
#endif
        void Update()
        {
            if ((GameManager.Instance.GameStatus & Global.UniversalConstant.GameStatus.LEVEL_GENERATED) == 0) return;

            MoveVehicle();
            FerryAroundThePark();

#if DEBUG_SLOW_1
            if (slowTime || slowTime2)
            {
                if (slowTime)
                    Time.timeScale = 0.45f;
                else
                    Time.timeScale = 0.15f;
            }
            else
                Time.timeScale = 1f;
#endif
        }

        private void FixedUpdate()
        {
            if ((GameManager.Instance.GameStatus & Global.UniversalConstant.GameStatus.LEVEL_GENERATED) == 0) return;

            CheckCollisions();
            OnBoardingRoadCollisionCheck();
            CornerVehicleCollisionCheck();
        }

        private void VehicleSelected(InputManager.SelectionStatus selectionStatus, int vehicleID, Vector2 slideDir)
        {
            // Debug.Log($"selectionStatus: {selectionStatus} | vehicleID: {vehicleID}"
            // + $" | slideDir : {slideDir} | Diff = {(Mathf.Abs(slideDir.y) - 0.75f)}");
            for (int i = 0; i < _vehicleSpawner.VehiclesSpawned.Count; i++)
            {
                if (vehicleID == _vehicleSpawner.VehiclesSpawned[i].GetInstanceID()
                    // && !vehicleInfos[i].hasInteracted
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.INTERACTED) == 0)
                {
                    // Debug.Log($"Found Vehicle! | Vehicle ID : {vehicleID}");
                    // _vehicleSetID = i;
                    // vehicleInfos[i].hasInteracted = true;

                    //Validate if the slide direction is matching the vehicle's orientation
                    // bool vehicleOrientationVertical = Mathf.Abs(vehicleSpawner.VehiclesSpawned[i].forward[2]) >= 0.9f ? true : false;
                    // Debug.Log($"Vehicle Orientation: {Mathf.Abs(vehicleSpawner.VehiclesSpawned[i].forward[2])} "
                    // + $" | slideDir: {slideDir.y} | slidedir Rounded: {Mathf.RoundToInt(slideDir.y)}");
                    // + $" | VehicleOrientation: {vehicleOrientationVertical}");

                    switch (selectionStatus)
                    {
                        //Do Nothing | Normal Selection
                        case InputManager.SelectionStatus.SELECTED:
                            break;

                        //Replace vehicle with a smaller vehicle
                        case InputManager.SelectionStatus.POWER1_ACTIVE:

                            // Check if the player has selected a Small Vehicle
                            //  - If small Vehicle is selected, then do not continue | Set the power again
                            if (_vehicleInfos[i].OgVehicleType == 1)
                            {
                                GameManager.Instance.OnUISelected?.Invoke(GameUIManager.UISelected.POWER_1, -1);
                                Debug.Log($"Power 1 | Small Vehicle is selected");
                                return;
                            }

                            System.Text.StringBuilder vehicleName =
                                new System.Text.StringBuilder(_vehicleSpawner.VehiclesSpawned[i].name);
                            PoolManager.Instance.PrefabPool[(UniversalConstant.PoolType)_vehicleInfos[i].OgVehicleType]
                                .Release(_vehicleSpawner.VehiclesSpawned[i].gameObject);

                            _vehicleSpawner.VehiclesSpawned[i] =
                                PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.VEHICLE_S].Get().transform;
                            vehicleName[8] = '1';
                            _vehicleSpawner.VehiclesSpawned[i].name = vehicleName.ToString();

                            _vehicleInfos[i].VehicleType = 1;
                            _vehicleSpawner.VehiclesSpawned[i].position =
                                new Vector3(_vehicleInfos[i].InitialPos.x, 0.28f, _vehicleInfos[i].InitialPos.y);

                            Vector3 vehicleRot = Vector3.zero;
                            vehicleRot.y = _vehicleInfos[i].InitialRot;
                            _vehicleSpawner.VehiclesSpawned[i].localEulerAngles = vehicleRot;
                            return;

                        //Remove vehicle
                        case InputManager.SelectionStatus.POWER2_ACTIVE:
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.INTERACTED;
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.LEFT_PARKING;
                            _vehicleSpawner.VehiclesSpawned[i].gameObject.SetActive(false);
                            // PoolManager.Instance.PrefabPool[(UniversalConstant.PoolType)_vehicleInfos[i].VehicleType]
                            //     .Release(_vehicleSpawner.VehiclesSpawned[i].gameObject);
                            return;
                    }

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
            for (int i = 0; i < _vehicleSpawner.VehiclesSpawned.Count; i++)
            {
                // if (vehicleInfos[i].hasInteracted)
                //Check if the vehicle has been interacted with and has not reached the road
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.HIT_NPC) == 0
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.INTERACTED) != 0
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.REACHED_ROAD) == 0
                    && (_vehicleInfos[i].VehicleStatus & VehicleStatus.COLLIDED_ONBOARDING) == 0)
                {
                    vehiclePos = _vehicleSpawner.VehiclesSpawned[i].transform.position;

                    // Testing
                    vehiclePos.Set(_vehicleInfos[i].InteractedDir.x, 0f, _vehicleInfos[i].InteractedDir.y);
                    // vehiclePos.x = _vehicleInfos[i].InteractedDir.x;vehiclePos.y = 0f;vehiclePos.z = _vehicleInfos[i].InteractedDir.y;
                    // vehiclePos += Time.deltaTime ;
                    _vehicleSpawner.VehiclesSpawned[i].transform.position += vehiclePos * _vehicleSpeedMultiplierC * Time.deltaTime;
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
                        }
                        // Tip of the vehicle has touched the center of the road | Basically, vehicle has ONBOARDED the road
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0] - (UniversalConstant.HALF_CELL_SIZE * 2.2f)
                            - (UniversalConstant.HALF_CELL_SIZE * (_vehicleInfos[i].OgVehicleType - 1)))
                        {
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                            // Debug.Log($"Reaching Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0] - (UniversalConstant.HALF_CELL_SIZE * 4.5f)
                            - (UniversalConstant.HALF_CELL_SIZE * _vehicleInfos[i].OgVehicleType))
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
                        }
                        // Tip of the vehicle has touched the center of the road | Basically, vehicle has ONBOARDED the road
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.z <= (_roadBoundaries[0] * -1f) + (UniversalConstant.HALF_CELL_SIZE * 2.2f)
                            + (UniversalConstant.HALF_CELL_SIZE * (_vehicleInfos[i].OgVehicleType - 1)))
                        {
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                            // Debug.Log($"Reaching Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.z <= (_roadBoundaries[0] * -1f) + (UniversalConstant.HALF_CELL_SIZE * 4.5f)
                            + (UniversalConstant.HALF_CELL_SIZE * _vehicleInfos[i].OgVehicleType))
                        {
                            _vehicleInfos[i].VehicleStatus |= VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                            // Debug.Log($"Reaching Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                        }

                        // S - 10.55 | M - 10.3 | L - 10.05
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
                        }
                        // Tip of the vehicle has touched the center of the road | Basically, vehicle has ONBOARDED the road
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.x >= _roadBoundaries[1] - (UniversalConstant.HALF_CELL_SIZE * 2.2f)
                            - (UniversalConstant.HALF_CELL_SIZE * (_vehicleInfos[i].OgVehicleType - 1)))
                        {
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                            // Debug.Log($"Reaching Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                        }
                        // Taking 0.1 extra in consideration of the boundary
                        // Road Marker is 2 cells away | Last cell is occupied by boundary | Vehicle size offset
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.x >= _roadBoundaries[1]
                            - (UniversalConstant.HALF_CELL_SIZE * 4.5f) - (UniversalConstant.HALF_CELL_SIZE * _vehicleInfos[i].OgVehicleType))
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
                        }
                        // Tip of the vehicle has touched the center of the road | Basically, vehicle has ONBOARDED the road
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.x <= (_roadBoundaries[1] * -1f) + (UniversalConstant.HALF_CELL_SIZE * 2.2f)
                            + (UniversalConstant.HALF_CELL_SIZE * (_vehicleInfos[i].OgVehicleType - 1)))
                        {
                            _vehicleInfos[i].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                            RenameVehicle(i);
                            // Debug.Log($"Reaching Road | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                        }
                        else if (_vehicleSpawner.VehiclesSpawned[i].position.x <= (_roadBoundaries[1] * -1f)
                            + (UniversalConstant.HALF_CELL_SIZE * 4.5f) + (UniversalConstant.HALF_CELL_SIZE * _vehicleInfos[i].OgVehicleType))
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
            for (int i = 0; i < _vehicleSpawner.VehiclesSpawned.Count; i++)
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
                            if (_vehicleSpawner.VehiclesSpawned[i].position.z >= _roadBoundaries[0] + 8f)
                            {
                                _vehicleInfos[i].VehicleStatus = VehicleStatus.NOT_INTERACTED;
                                RenameVehicle(i);

                                _vehicleExitedCount++;
                                // if (_vehicleExitedCount >= 2               //TESTING
                                if (_vehicleExitedCount >= _vehicleSpawner.VehiclesSpawned.Count
                                    && (GameManager.Instance.GameStatus & UniversalConstant.GameStatus.LEVEL_FAILED) == 0)
                                    GameManager.Instance.OnGameStatusChange?.Invoke(UniversalConstant.GameStatus.LEVEL_PASSED, -1);

                                // Vehicle has been changed to Small using power | Revert it back
                                if (_vehicleInfos[i].VehicleType != 255)
                                {
                                    PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.VEHICLE_S]
                                        .Release(_vehicleSpawner.VehiclesSpawned[i].gameObject);
                                }
                                else
                                {
                                    PoolManager.Instance.PrefabPool[(UniversalConstant.PoolType)_vehicleInfos[i].OgVehicleType]
                                        .Release(_vehicleSpawner.VehiclesSpawned[i].gameObject);
                                }
                            }
                            break;

                        default:
                            Debug.LogError($"Wrong Marker Indexx : {_vehicleInfos[i].MarkerIndex}");
                            break;
                    }
                    _vehicleSpawner.VehiclesSpawned[i].transform.position += vehiclePos * _vehicleSpeedMultiplierC * Time.deltaTime;
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
            // int vehicleStatus;
            for (int i = 0; i < _vehicleSpawner.VehiclesSpawned.Count; i++)
            {
                //Check if the vehicle has been interacted with or have reached the road
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.HIT_NPC) != 0
                    || (_vehicleInfos[i].VehicleStatus & VehicleStatus.INTERACTED) == 0
                    || (_vehicleInfos[i].VehicleStatus & VehicleStatus.REACHED_ROAD) != 0)
                    // || (_vehicleInfos[i].VehicleStatus & VehicleStatus.COLLIDED_PARKING) != 0)
                    continue;

                rayStartPos = _vehicleSpawner.VehiclesSpawned[i].position;
                rayDir = Vector3.zero;

                //Check Orientation and set Raycast-Points / Raycast-Directions
                //Vertical Alignment
                if ((_vehicleInfos[i].VehicleStatus & VehicleStatus.ALIGNMENT) != 0)
                {
                    rayStartPos.z += UniversalConstant.HALF_CELL_SIZE * _vehicleInfos[i].InteractedDir.y * (_vehicleInfos[i].OgVehicleType + 1)
                        - (0.1f * _vehicleInfos[i].InteractedDir.y);
                    rayDir.z = _vehicleInfos[i].InteractedDir.y;

                    _roundPosition = (posToChange) =>
                    {
                        // Debug.Log($"Vertical | posToChange: {posToChange} | Dir: {_vehicleInfos[i].InteractedDir} "
                        //     + $"| Vehicle-Type: {_vehicleInfos[i].VehicleType}");
                        posToChange.z = posToChange.z + (0.05f * -1f * _vehicleInfos[i].InteractedDir.y)
                                                    + (_vehicleSizes[_vehicleInfos[i].OgVehicleType - 1] * -1f * _vehicleInfos[i].InteractedDir.y);
                        posToChange.x = _vehicleSpawner.VehiclesSpawned[i].position.x;
                        posToChange.y = _vehicleSpawner.VehiclesSpawned[i].position.y;
                        return posToChange;
                    };
                }
                //Horizontal Alignment
                else
                {
                    rayStartPos.x += UniversalConstant.HALF_CELL_SIZE * _vehicleInfos[i].InteractedDir.x * (_vehicleInfos[i].OgVehicleType + 1)
                        - (0.1f * _vehicleInfos[i].InteractedDir.x);
                    rayDir.x = _vehicleInfos[i].InteractedDir.x;

                    _roundPosition = (posToChange) =>
                    {
                        // Debug.Log($"Horizontal | posToChange: {posToChange} | Dir: {_vehicleInfos[i].InteractedDir} "
                        //     + $"| Vehicle-Type: {_vehicleInfos[i].VehicleType}");
                        posToChange.x = posToChange.x + (0.05f * -1f * _vehicleInfos[i].InteractedDir.x)
                                                    + (_vehicleSizes[_vehicleInfos[i].OgVehicleType - 1] * -1f * _vehicleInfos[i].InteractedDir.x);
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
                    rayStartPos.x += UniversalConstant.HALF_CELL_SIZE * Mathf.Abs(_vehicleInfos[i].InteractedDir.y) * j;       //Vertical
                    rayStartPos.z += UniversalConstant.HALF_CELL_SIZE * Mathf.Abs(_vehicleInfos[i].InteractedDir.x) * j;       //Horizontal
#if COLLISION_DEBUG_DRAW_1
                    Debug.DrawRay(rayStartPos, rayDir * 0.25f, Color.cyan);
#endif
                    if (Physics.Raycast(rayStartPos, rayDir, out colliderHitInfo, 0.25f, _collisionCheckLayerMaskC))
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

                        // int.TryParse(colliderHitInfo.transform.name[(colliderHitInfo.transform.name.Length - 2)..]
                        //     , out vehicleStatus);
                        // [colliderHitInfo.transform.name.Length - 1] - '0';

                        // Debug.Log($"Hit | Point: {colliderHitInfo.point} | name: {colliderHitInfo.transform.name}"
                        //     + $" | zPos: {_vehicleSpawner.VehiclesSpawned[i].position.z} | vehicleStatus: {vehicleStatus}");

                        //Check if the vehicle is still inside parking
                        // if (vehicleStatus == 0) 
                        _vehicleInfos[i].VehicleStatus = 0;
                        // else
                        // _vehicleInfos[i].VehicleStatus |= VehicleStatus.COLLIDED_PARKING;

                        RenameVehicle(i);

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

        private void OnBoardingRoadCollisionCheck()
        {
            Vector3 rayStartPos, rayDir, tempRayPos;
            RaycastHit colliderHitInfo;

            // System.Text.StringBuilder debugOnBoarding = new System.Text.StringBuilder();
            const int rayCountC = 4;
            const int rayLengthMultC = 9;
            int vIndex, rayIndex, hitCount;
            for (vIndex = 0; vIndex < _vehicleSpawner.VehiclesSpawned.Count; vIndex++)
            {
                //Check if the vehicle has been interacted with or have reached the road
                if ((_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.HIT_NPC) != 0
                    || ((_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.COLLIDED_ONBOARDING) == 0
                    && (_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.ONBOARDING_ROAD) == 0)
                    || (_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.CORNER_COLLIDED) != 0)
                    continue;

                // Only need to do this once, so no need for OverlapBox
                /*if ((_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.CORNER_COLLIDED) == 0)
                // Dont need a overlapbox as dont need the colliders, just need to check if there is a vehicle or not, so just raycast to that side with proper layer
                // && Physics.OverlapBox(_vehicleSpawner.VehiclesSpawned[vIndex].position, Vector3.one, Quaternion.identity, _cMovingVehicleLayerMask))
                {
                    CornerVehicleCollisionCheck(vIndex);
                    continue;
                }*/

                rayStartPos = _vehicleSpawner.VehiclesSpawned[vIndex].position;
                rayDir = Vector3.zero;

                //Check Orientation and set Raycast-Points / Raycast-Directions
                //Vertical Alignment
                if ((_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.ALIGNMENT) != 0)
                {
                    rayStartPos.z += ((UniversalConstant.HALF_CELL_SIZE * _vehicleInfos[vIndex].OgVehicleType) + 0.1f       //+ (0.1f * _vehicleInfos[vIndex].OgVehicleType)       //Vehicle Size + Offset
                                + (UniversalConstant.HALF_CELL_SIZE * rayLengthMultC)) * _vehicleInfos[vIndex].InteractedDir.y;     //Cells offset
                    rayDir.z = _vehicleInfos[vIndex].InteractedDir.y * -1f;
                }
                //Horizontal Alignment
                else
                {
                    rayStartPos.x += ((UniversalConstant.HALF_CELL_SIZE * _vehicleInfos[vIndex].OgVehicleType) + 0.1f       //+ (0.1f * _vehicleInfos[vIndex].OgVehicleType)      //Vehicle Size + Offset
                                + (UniversalConstant.HALF_CELL_SIZE * rayLengthMultC)) * _vehicleInfos[vIndex].InteractedDir.x;     //Cells Offset
                    rayDir.x = _vehicleInfos[vIndex].InteractedDir.x * -1f;
                }

                hitCount = 0;
                // Debug.Log($"Checking Vehicle | index: {i} | name: {_vehicleSpawner.VehiclesSpawned[i].name}"
                // + $" | interactedDir: {_vehicleInfos[i].InteractedDir}"
                // + $" | rayPos: {rayStartPos} | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                for (rayIndex = rayCountC + (_vehicleInfos[vIndex].OgVehicleType - 1);
                    rayIndex > -(rayCountC - 1); rayIndex--)
                {
                    tempRayPos = rayStartPos;
                    tempRayPos.z += (UniversalConstant.HALF_CELL_SIZE * 2f) * _vehicleInfos[vIndex].InteractedDir.x * rayIndex;
                    tempRayPos.x += (UniversalConstant.HALF_CELL_SIZE * 2f) * Mathf.Abs(_vehicleInfos[vIndex].InteractedDir.y) * rayIndex;

#if COLLISION_DEBUG_DRAW_2
                    Debug.DrawRay(tempRayPos, rayDir * UniversalConstant.HALF_CELL_SIZE * (rayLengthMultC - 1), Color.cyan);
#endif
                    // Debug.DrawRay(tempRayPos, rayDir * _cGridCellSize * 2.75f * (_vehicleInfos[i].VehicleType + 1), Color.cyan);

                    // Raycast straight in front of the vehicle parallel to the vehicle with the length of the vehicle plus offset
                    // if (!Physics.Raycast(tempRayPos, rayDir, out colliderHitInfo, _cGridCellSize * 2.75f * (_vehicleInfos[i].VehicleType + 1), _cCollisionCheckLayerMask))
                    if (Physics.Raycast(tempRayPos, rayDir, out colliderHitInfo,
                        UniversalConstant.HALF_CELL_SIZE * (rayLengthMultC - 1), _onBoardingLayerMaskC))
                    {
                        // {+} 2 points needed, 1 to the upper side, forward-side and 1 point with offset to the down-side, back-side
                        //     . So as to check if there is any incoming vehicle or not and if the passing vehicle is gone
                        // Debug.Log($"Not Hit[{j}]");

                        _vehicleInfos[vIndex].VehicleStatus |= VehicleStatus.COLLIDED_ONBOARDING;
                        _vehicleInfos[vIndex].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                        hitCount++;
                        // Debug.Log($"Hit[{vIndex}] | hitPoint: {colliderHitInfo.point}");
                        break;
                    }
                }
                if (hitCount == 0)
                {
                    // Debug.Log($"All Clear | vIndex: {rayIndex} | hitCount: {hitCount}");
                    _vehicleInfos[vIndex].VehicleStatus &= ~VehicleStatus.COLLIDED_ONBOARDING;
                    _vehicleInfos[vIndex].VehicleStatus |= VehicleStatus.ONBOARDING_ROAD;

                    RenameVehicle(vIndex);
                }
            }

        }

        //Horizontal: 2 | Vertical: 3 -> Vertical
        private void CornerVehicleCollisionCheck()
        {
            Vector3 rayStartPos, rayDir, tempRayPos;
            RaycastHit colliderHitInfo;

            // System.Text.StringBuilder debugOnBoarding = new System.Text.StringBuilder();
            const int rayCountC = 3;
            const int rayLengthMultC = 6;
            const float cellMult1C = 10f, cellMult2C = 4.5f;
            int vIndex, rayIndex, hitCount;

            for (vIndex = 0; vIndex < _vehicleSpawner.VehiclesSpawned.Count; vIndex++)
            {
                // If the vehicle has collided with oncoming vehicle before escaping the corner, then unset the flag
                // and check if the corner is free again or not
                if ((_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.COLLIDED_ONBOARDING) != 0
                    && (_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.CORNER_FREE) != 0)
                {
                    _vehicleInfos[vIndex].VehicleStatus &= ~VehicleStatus.CORNER_FREE;
                }

                //Check if the vehicle has been interacted with or have reached the road
                if (((_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.COLLIDED_ONBOARDING) == 0
                    && (_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.ONBOARDING_ROAD) == 0)
                    || (_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.CORNER_FREE) != 0)
                    continue;
                // System.Text.StringBuilder debugOnBoarding = new System.Text.StringBuilder();

                rayStartPos = _vehicleSpawner.VehiclesSpawned[vIndex].position;
                rayDir = Vector3.zero;

                //Check Orientation and set Raycast-Points / Raycast-Directions
                //Vertical Alignment | This will only be possible in bottom-right corner
                if ((_vehicleInfos[vIndex].VehicleStatus & VehicleStatus.ALIGNMENT) != 0)
                {
                    //Check if the vehicle is at the corner | Only proceed then, else not needed
                    if (Mathf.Abs(_vehicleSpawner.VehiclesSpawned[vIndex].position.x) < _roadBoundaries[1] - (UniversalConstant.HALF_CELL_SIZE * cellMult1C) &&        //2 cell gap
                        Mathf.Abs(_vehicleSpawner.VehiclesSpawned[vIndex].position.z) < _roadBoundaries[0] - (UniversalConstant.HALF_CELL_SIZE * cellMult2C))
                    {
                        // Debug.Log($"Outside the corner");
                        _vehicleInfos[vIndex].VehicleStatus |= VehicleStatus.CORNER_FREE;
                        return;
                    }

                    // rayStartPos.z += ((_cGridHalfCellSize * _vehicleInfos[vIndex].VehicleType) + 0.1f      //Vehicle Size + Offset
                    //             + (_cGridHalfCellSize * 7f)) * _vehicleInfos[vIndex].InteractedDir.y;     //Cells offset
                    rayStartPos.x = _roadBoundaries[1] + (UniversalConstant.HALF_CELL_SIZE * 4f);
                    rayStartPos.z = (_roadBoundaries[0] - (UniversalConstant.HALF_CELL_SIZE * 4f)) * -1f;
                    rayDir.x = _vehicleInfos[vIndex].InteractedDir.y;
                }
                //Horizontal Alignment
                else
                {
                    if (Mathf.Abs(_vehicleSpawner.VehiclesSpawned[vIndex].position.z) < _roadBoundaries[0] - (UniversalConstant.HALF_CELL_SIZE * cellMult1C) &&                //2 cell gap
                        Mathf.Abs(_vehicleSpawner.VehiclesSpawned[vIndex].position.x) < _roadBoundaries[1] - (UniversalConstant.HALF_CELL_SIZE * cellMult2C))
                    {
                        // Debug.Log($"Outside the corner");
                        _vehicleInfos[vIndex].VehicleStatus |= VehicleStatus.CORNER_FREE;
                        return;
                    }

                    // rayStartPos.x += ((_cGridHalfCellSize * _vehicleInfos[vIndex].VehicleType) + 0.1f      //Vehicle Size + Offset
                    //             + (_cGridHalfCellSize * 7f)) * _vehicleInfos[vIndex].InteractedDir.x;     //Cells Offset
                    rayStartPos.z = (_roadBoundaries[0] + (UniversalConstant.HALF_CELL_SIZE * 4f)) * _vehicleInfos[vIndex].InteractedDir.x;
                    rayStartPos.x = (_roadBoundaries[1] - (UniversalConstant.HALF_CELL_SIZE * 4f)) * _vehicleInfos[vIndex].InteractedDir.x;
                    rayDir.z = _vehicleInfos[vIndex].InteractedDir.x * -1f;
                }

                hitCount = 0;

                // Debug.Log($"Checking Vehicle | index: {i} | name: {_vehicleSpawner.VehiclesSpawned[i].name}"
                // + $" | interactedDir: {_vehicleInfos[i].InteractedDir}"
                // + $" | rayPos: {rayStartPos} | Pos: {_vehicleSpawner.VehiclesSpawned[i].position}");
                for (rayIndex = -1; rayIndex < rayCountC; rayIndex++)
                {
                    tempRayPos = rayStartPos;
                    tempRayPos.x += UniversalConstant.HALF_CELL_SIZE * 2.5f * _vehicleInfos[vIndex].InteractedDir.x * rayIndex;
                    tempRayPos.z += UniversalConstant.HALF_CELL_SIZE * 2.5f * _vehicleInfos[vIndex].InteractedDir.y * rayIndex;

#if CORNER_COLLISION_DEBUG_DRAW_1
                    Debug.DrawRay(tempRayPos, rayDir * UniversalConstant.HALF_CELL_SIZE * rayLengthMultC, Color.cyan);
#endif
                    // Debug.DrawRay(tempRayPos, rayDir * _cGridCellSize * 2.75f * (_vehicleInfos[i].VehicleType + 1), Color.cyan);

                    // Raycast to the right of  the vehicle perpendicular to the vehicle with some offset
                    if (Physics.Raycast(tempRayPos, rayDir, out colliderHitInfo,
                        UniversalConstant.HALF_CELL_SIZE * rayLengthMultC, _onBoardingLayerMaskC))
                    {
                        // Debug.Log($"Hit Vehicle | vIndex: {rayIndex} | hitCount: {hitCount}");

                        _vehicleInfos[vIndex].VehicleStatus |= VehicleStatus.COLLIDED_ONBOARDING;
                        _vehicleInfos[vIndex].VehicleStatus |= VehicleStatus.CORNER_COLLIDED;

                        _vehicleInfos[vIndex].VehicleStatus &= ~VehicleStatus.CORNER_FREE;
                        _vehicleInfos[vIndex].VehicleStatus &= ~VehicleStatus.ONBOARDING_ROAD;
                        hitCount++;
                        break;
                    }
                }
                if (hitCount == 0)
                {
                    // Debug.Log($"Not Hit | rayIndex: {rayIndex}");

                    _vehicleInfos[vIndex].VehicleStatus |= VehicleStatus.CORNER_FREE;
                    _vehicleInfos[vIndex].VehicleStatus &= ~VehicleStatus.CORNER_COLLIDED;
                    RenameVehicle(vIndex);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RenameVehicle(in int vehicleIndex)
        {
            _vehicleName.Clear();

            _vehicleName.Append(_vehicleSpawner.VehiclesSpawned[vehicleIndex].name, 0
                , _vehicleSpawner.VehiclesSpawned[vehicleIndex].name.Length - 4);
            _vehicleName.Append(((int)_vehicleInfos[vehicleIndex].VehicleStatus).ToString("D4"));           //Max status can go to ((1 << 10) - 1) 

            _vehicleSpawner.VehiclesSpawned[vehicleIndex].name = _vehicleName.ToString();
        }
    }
}