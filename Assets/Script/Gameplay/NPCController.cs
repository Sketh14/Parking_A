#define SLOWTIME_DEBUG
#define COLLISIONCHECK_DEBUG

using System;
using UnityEngine;
using Parking_A.Global;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Parking_A.Gameplay
{
    public class NPCController : MonoBehaviour
    {
        [Serializable]
        internal struct NPCInfo
        {
            public NPCStatus NpcStatus;

            public Vector2 InitialPos;
            public float InitialRot;
            public byte InitialStatus;
        }

        internal enum NPCStatus
        {
            UNINITIALIZED = 0,
            IDLE = 1 << 0,
            MOVING = 1 << 1,
            HORIZONTAL_ALIGNED = 1 << 2,
            TURNING_CORNER = 1 << 3,
            NPC_HIT = 1 << 4,
        }

        private NPCSpawner _npcSpawner;

        private NPCInfo[] _npcInfos;
        private System.Text.StringBuilder _npcName;
        private CancellationTokenSource _cts;
        private int _hitNPCINdex;

        private const float _speedMultC = 0.5f;
        private const int _collisionLayerMaskC = (1 << 6) | (1 << 7);
        private const int _vehicleLayerMaskC = 1 << 6;
        private const float _idleThreshold = 0.35f;
        private readonly float[] _walkingBoundaries = { 10.25f, 5.25f };                 //Vertical | Horizontal
        private readonly float[] _rotationMatrixBy90CW = { 0, -1, 1, 0 };            //Og: [1,0] | [0,1] / 90CW : [0,-1] | [1, 0] / 90CCW : [0, 1] | [-1, 0]

#if SLOWTIME_DEBUG
        public bool slowTime = false, slowTime2 = false;
#endif

        private void OnDestroy()
        {
            if (_cts != null) _cts.Cancel();
            GameManager.Instance.OnGameStatusChange -= UpdateNPCs;
            GameManager.Instance.OnEnvironmentSpawned -= CallNPCSpawner;
        }

        private void Start()
        {
            _cts = new CancellationTokenSource();

            _npcName = new System.Text.StringBuilder();

            _npcSpawner = new NPCSpawner();
            _npcInfos = new NPCInfo[8];             //Max should never be lesser than set  in Spawner

            GameManager.Instance.OnGameStatusChange += UpdateNPCs;
            GameManager.Instance.OnEnvironmentSpawned += CallNPCSpawner;
        }

        private async void CallNPCSpawner(byte[] boundaryData)
        {
            Debug.Log($"Spawning NPCs");
            try
            {
                if (boundaryData == null)
                {
                    Debug.LogError($"Boundary Data is null");
                    return;
                }

                //Below while block, so as to repeat exact levels
                while (true)
                {
                    if ((GameManager.Instance.GameStatus & UniversalConstant.GameStatus.VEHICLE_SPAWNED) != 0)
                        break;

                    await Task.Delay(100);
                    if (_cts.IsCancellationRequested) return;
                }

                await _npcSpawner.SpawnNpcs(boundaryData, InitializeNPCData);
                if (_cts.IsCancellationRequested) return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // Right: {(1.00, 0.00, 0.00)} | Down: {(0.00, 0.00, -1.00)} | Left: {(-1.00, 0.00, 0.00)} | Up: {(0.00, 0.00, 1.00)}
        private void InitializeNPCData()
        {
            // _npcInfos = new NPCInfo[_npcSpawner.NPCsSpawned.Count];

            for (int i = 0; i < _npcSpawner.NPCsSpawned.Count; i++)
            {
                // UP | DOWN
                if (Mathf.Abs(_npcSpawner.NPCsSpawned[i].forward[2]) >= 0.9f)
                    _npcInfos[i].NpcStatus &= ~NPCStatus.HORIZONTAL_ALIGNED;
                //LEFT | RIGHT
                else if (Mathf.Abs(_npcSpawner.NPCsSpawned[i].forward[0]) >= 0.9f)
                    _npcInfos[i].NpcStatus |= NPCStatus.HORIZONTAL_ALIGNED;

                _npcInfos[i].NpcStatus = 0;         //REset
                _npcInfos[i].NpcStatus |= NPCStatus.MOVING;
                _npcInfos[i].InitialPos.Set(_npcSpawner.NPCsSpawned[i].position.x,
                    _npcSpawner.NPCsSpawned[i].position.z);
                _npcInfos[i].InitialRot = _npcSpawner.NPCsSpawned[i].localEulerAngles.y;
                _npcInfos[i].InitialStatus = (byte)_npcInfos[i].NpcStatus;

                // Debug.Log($"{_npcSpawner.NPCsSpawned[i].name} | i: {i} | forward-Z: {Mathf.Round(_npcSpawner.NPCsSpawned[i].forward[2])}"
                //     + $" | Horizontal Status: {_npcInfos[i].NpcStatus & NPCStatus.HORIZONTAL_ALIGNED}");
            }

            GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.NPC_SPAWNED);
        }

        private void UpdateNPCs(UniversalConstant.GameStatus gameStatus, int value)
        {
            // Debug.Log($"UpdateNPCs | gameStatus: {gameStatus}");
            switch (gameStatus)
            {
                case UniversalConstant.GameStatus.LEVEL_GENERATED:
                    for (int i = 0; i < _npcSpawner.NPCsSpawned.Count; i++)
                        _npcSpawner.NPCsSpawned[i].gameObject.SetActive(true);

                    break;

                case UniversalConstant.GameStatus.RESET_LEVEL:
                    Vector3 npcPos, npcRot;
                    for (int i = 0; i < _npcSpawner.NPCsSpawned.Count; i++)
                    {
                        npcPos = _npcSpawner.NPCsSpawned[i].position;
                        npcPos.x = _npcInfos[i].InitialPos.x;
                        npcPos.z = _npcInfos[i].InitialPos.y;
                        _npcSpawner.NPCsSpawned[i].position = npcPos;
                        // _npcSpawner.NPCsSpawned[i].position.Set(_npcInfos[i].InitialPos.x,
                        //     _npcSpawner.NPCsSpawned[i].position.y, _npcInfos[i].InitialPos.y);

                        npcRot = Vector3.zero;
                        npcRot.y = _npcInfos[i].InitialRot;
                        _npcSpawner.NPCsSpawned[i].localEulerAngles = npcRot;
                        _npcSpawner.NPCsSpawned[i].GetChild(0).localEulerAngles = Vector3.zero;     //.Set(0f, 0f, 0f);
                        // _npcSpawner.NPCsSpawned[i].GetChild(0).localEulerAngles.Set(0f, _npcInfos[i].InitialRot, 0f);

                        // _npcInfos[i].NpcStatus = 0;
                        _npcInfos[i].NpcStatus = (NPCStatus)_npcInfos[i].InitialStatus;
                    }

                    break;

                case UniversalConstant.GameStatus.NEXT_LEVEL_REQUESTED:

                    for (int i = 0; i < _npcSpawner.NPCsSpawned.Count; i++)
                    {
                        PoolManager.Instance.PrefabPool[UniversalConstant.PoolType.NPC]
                            .Release(_npcSpawner.NPCsSpawned[i].gameObject);

                        _npcInfos[i].NpcStatus = NPCStatus.UNINITIALIZED;
                        // _npcInfos[i].NpcStatus |= NPCStatus.IDLE;       //Avoid Update
                        // _npcInfos[i].NpcStatus |= NPCStatus.NPC_HIT;       //Avoid CollisionCheck
                    }

                    _npcSpawner.ClearNPCs();
                    GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.NPC_SPAWNED, false);
                    break;
            }
        }

        private void Update()
        {
            if ((GameManager.Instance.GameStatus & UniversalConstant.GameStatus.LEVEL_GENERATED) == 0) return;

            MoveNPCs();

#if SLOWTIME_DEBUG
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
            if ((GameManager.Instance.GameStatus & UniversalConstant.GameStatus.LEVEL_GENERATED) == 0) return;

            CheckCollisions();
        }

        private void MoveNPCs()
        {
            Vector3 npcPos, npcRot;
            for (int npcIndex = 0; npcIndex < _npcSpawner.NPCsSpawned.Count; npcIndex++)
            {
                if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.NPC_HIT) != 0
                    || (_npcInfos[npcIndex].NpcStatus & NPCStatus.IDLE) != 0
                    || _npcInfos[npcIndex].NpcStatus == NPCStatus.UNINITIALIZED) continue;

                _npcSpawner.NPCsSpawned[npcIndex].position += _npcSpawner.NPCsSpawned[npcIndex].forward * Time.deltaTime * _speedMultC;

                //Do Boundary reach check
                // Vertical Alignment
                if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.HORIZONTAL_ALIGNED) == 0)
                {
                    // Turn the npc and align horizontally once boundary is reached
                    // Need to check if the NPC is up/down | left/right in order to rotate the NPC correctly

                    if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.TURNING_CORNER) == 0
                        && Mathf.Abs(_npcSpawner.NPCsSpawned[npcIndex].position.z) >= _walkingBoundaries[0])
                    {
                        _npcInfos[npcIndex].NpcStatus |= NPCStatus.HORIZONTAL_ALIGNED;
                        _npcInfos[npcIndex].NpcStatus |= NPCStatus.TURNING_CORNER;

                        npcRot = Vector3.zero;
                        npcRot.y = 90f * -1f * (int)(_npcSpawner.NPCsSpawned[npcIndex].position[0] / _walkingBoundaries[1]);        // pos.x/x will give 1 or -1
                        _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles = npcRot;

                        npcPos = _npcSpawner.NPCsSpawned[npcIndex].position;
                        npcPos.z = _walkingBoundaries[0] * (int)(_npcSpawner.NPCsSpawned[npcIndex].position[2] / _walkingBoundaries[0]);        //cast to int as the result would be in float and may not be whole number
                        _npcSpawner.NPCsSpawned[npcIndex].position = npcPos;

                        // Debug.Log($"Name: {_npcSpawner.NPCsSpawned[npcIndex].name} | Border Reached | npcPos: {npcPos}"
                        //     + $" | npcRot: {npcRot} | status: {_npcInfos[npcIndex].NpcStatus}");
                    }
                    else    // if (Mathf.Abs(_npcSpawner.NPCsSpawned[npcIndex].position.z) <= _walkingBoundaries[0])
                    {
                        _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.TURNING_CORNER;
                    }
                }
                // Horizontal Alignment
                else
                {
                    // Turn the npc and align vertically once boundary is reached
                    if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.TURNING_CORNER) == 0
                        && Mathf.Abs(_npcSpawner.NPCsSpawned[npcIndex].position.x) >= _walkingBoundaries[1])
                    {
                        _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.HORIZONTAL_ALIGNED;
                        _npcInfos[npcIndex].NpcStatus |= NPCStatus.TURNING_CORNER;

                        npcRot = Vector3.zero;
                        npcRot.y += 90f * (1 + (int)(_npcSpawner.NPCsSpawned[npcIndex].position[2] / _walkingBoundaries[0]));        // pos.z/z will give 1 or -1
                        _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles = npcRot;

                        npcPos = _npcSpawner.NPCsSpawned[npcIndex].position;
                        npcPos.x = _walkingBoundaries[1] * (int)(_npcSpawner.NPCsSpawned[npcIndex].position[0] / _walkingBoundaries[1]);        //cast to int as the result would be in float and may not be whole number
                        _npcSpawner.NPCsSpawned[npcIndex].position = npcPos;

                        // Debug.Log($"Name: {_npcSpawner.NPCsSpawned[npcIndex].name} | Border Reached | npcPos: {npcPos}"
                        //     + $" | npcRot: {npcRot} | status: {_npcInfos[npcIndex].NpcStatus}");
                    }
                    else    // if (Mathf.Abs(_npcSpawner.NPCsSpawned[npcIndex].position.x) <= _walkingBoundaries[1])
                    {
                        _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.TURNING_CORNER;
                    }

                }
            }
        }

        //Do Collision check to turn back around
        // Right: {(1.00, 0.00, 0.00)} | Down: {(0.00, 0.00, -1.00)} | Left: {(-1.00, 0.00, 0.00)} | Up: {(0.00, 0.00, 1.00)}
        private void CheckCollisions()
        {
            /*
                |   |   |   |       |   |   |   |       |   | - |   |       |   | - |   |
                -------------       -------------       -------------       -------------
                |   | x | - |       | - | x |   |       | - | x |   |       |   | x | - |
                -------------       -------------       -------------       -------------
                |   | - |   |       |   | - |   |       |   |   |   |       |   |   |   |
                HORIZONTAL_UP       VERTICAL_RIGHT      HORIZONTAL_DOWN     VERTICAL_LEFT
            */

            // Need to check for collision in transforms forward direction for colliding with the boundary
            // Check for collisions with the vehicles?

            Vector3 rayStartPos, rayDir, npcRot;
            RaycastHit colliderHitInfo;

            for (int npcIndex = 0; npcIndex < _npcSpawner.NPCsSpawned.Count; npcIndex++)
            {
                if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.NPC_HIT) != 0
                || _npcInfos[npcIndex].NpcStatus == NPCStatus.UNINITIALIZED) continue;

                rayStartPos = _npcSpawner.NPCsSpawned[npcIndex].position;
                rayStartPos.y = 0.3f;
                // + _npcSpawner.NPCsSpawned[npcIndex].forward * (UniversalConstant._CellHalfSizeC / 2f);
                rayDir = _npcSpawner.NPCsSpawned[npcIndex].forward;

                // for (int rayIndex = 0; rayIndex < 2; rayIndex++)
                // {

#if COLLISIONCHECK_DEBUG
                Debug.DrawRay(rayStartPos, rayDir * UniversalConstant._CellHalfSizeC, Color.cyan);
#endif
                // If collided with anything, turn the NPC around
                if (Physics.Raycast(rayStartPos, rayDir, out colliderHitInfo, UniversalConstant._CellHalfSizeC, _collisionLayerMaskC))
                {
                    // Debug.Log($"RayCast Hit | Name: {colliderHitInfo.transform.name} | colliderHitInfo-layer: {colliderHitInfo.transform.gameObject.layer}");

                    npcRot = Vector3.zero;

                    // VERTICAL
                    if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.HORIZONTAL_ALIGNED) == 0)
                        npcRot.y = 90 * (1 + (int)_npcSpawner.NPCsSpawned[npcIndex].forward[2]);
                    // HORIZONTAL
                    else
                        npcRot.y = 90 * -1 * (int)_npcSpawner.NPCsSpawned[npcIndex].forward[0];

                    _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles = npcRot;

                    if (UnityEngine.Random.Range(0f, 1f) < _idleThreshold)
                    {
                        _npcInfos[npcIndex].NpcStatus |= NPCStatus.IDLE;
                        _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.MOVING;
                        _ = StartMoving(npcIndex);
                        // Debug.Log($"Idling | npcIndex: {npcIndex} | Status: {_npcInfos[npcIndex].NpcStatus}");
                    }
                }

                //Rotate the rayDir according to NPC Orientation
                // Debug.Log($"rayDir: {rayDir} | rayIndex: {rayIndex} | x: {rayDir.x} | z: {rayDir.z}");

                // Remove [if-else] if possible
                //Rotate Clockwise
                if ((_npcSpawner.NPCsSpawned[npcIndex].forward.x > 0.5 && _npcSpawner.NPCsSpawned[npcIndex].position.z > 0.5)           // HORIZONTAL_UP
                    || (_npcSpawner.NPCsSpawned[npcIndex].forward.z < -0.5 && _npcSpawner.NPCsSpawned[npcIndex].position.x > 0.5)       // VERTICAL_RIGHT
                    || (_npcSpawner.NPCsSpawned[npcIndex].forward.x < -0.5 && _npcSpawner.NPCsSpawned[npcIndex].position.z < -0.5)      // HORIZONTAL_DOWN
                    || (_npcSpawner.NPCsSpawned[npcIndex].forward.z > 0.5 && _npcSpawner.NPCsSpawned[npcIndex].position.x < -0.5))      // VERTICAL_LEFT
                {
                    rayDir.Set(
                        rayDir.x * _rotationMatrixBy90CW[0] + rayDir.z * _rotationMatrixBy90CW[2],
                        rayDir.y,
                        rayDir.x * _rotationMatrixBy90CW[1] + rayDir.z * _rotationMatrixBy90CW[3]
                    );
                }
                //Rotate Counter-Clockwise
                else
                {
                    rayDir.Set(
                        rayDir.x * _rotationMatrixBy90CW[3] + rayDir.z * _rotationMatrixBy90CW[1],
                        rayDir.y,
                        rayDir.x * _rotationMatrixBy90CW[2] + rayDir.z * _rotationMatrixBy90CW[0]
                    );
                }

#if COLLISIONCHECK_DEBUG
                Debug.DrawRay(rayStartPos, rayDir * UniversalConstant._CellHalfSizeC, Color.cyan);
#endif
                // Only register hit from the sides
                // Check if the NPC has been hit by a vehicle
                if (Physics.Raycast(rayStartPos, rayDir, out colliderHitInfo, UniversalConstant._CellHalfSizeC, _vehicleLayerMaskC))
                {
                    // Debug.Log($"RayCast Hit | Name: {colliderHitInfo.transform.name} | colliderHitInfo-layer: {colliderHitInfo.transform.gameObject.layer}");
                    // _hitNPCINdex = npcIndex;
                    _npcInfos[npcIndex].NpcStatus |= NPCStatus.NPC_HIT;
                    int vehicleID = -1;
                    int.TryParse(colliderHitInfo.transform.name.Substring(12, 3), out vehicleID);
                    // Debug.Log($"Hit By Vehicle | ID: {vehicleID}");


                    // int vehicleDir = 0;
                    // int.TryParse(colliderHitInfo.transform.name.Substring(22, 2), out vehicleDir);
                    // Debug.Log($"Hit By Vehicle | ID: {vehicleID} | Hit Dir: {vehicleDir}");

                    // Determining Hit-Direction
                    //  - Since forward will be on the same orienatation as the vehicle, would just need to determine 
                    //    if its going forward or backward
                    //  - Just need to check the quadrant in which the vehicle is present

                    int dirMult2 = 1 * (int)colliderHitInfo.transform.forward.x
                        * (int)(colliderHitInfo.transform.position.x / Math.Abs(colliderHitInfo.transform.position.x))  //Check if the vehicle is "Right" or "Left" from the center
                        + 1 * (int)colliderHitInfo.transform.forward.z
                        * (int)(colliderHitInfo.transform.position.z / Math.Abs(colliderHitInfo.transform.position.z));  //Check if the vehicle is "Up" or "Down" from the center
                    // Debug.Log($"dirMult |z: {1 * (int)colliderHitInfo.transform.forward.z} "
                    // + $"|zPos: {colliderHitInfo.transform.position.z / Math.Abs(colliderHitInfo.transform.position.z)} "
                    // + $"|x: {1 * (int)colliderHitInfo.transform.forward.x} "
                    // + $"|xPos: {colliderHitInfo.transform.position.x / Math.Abs(colliderHitInfo.transform.position.x)} ");

                    GoFlying(npcIndex, colliderHitInfo.transform.forward * dirMult2);
                    // GameManager.Instance.OnNPCHit?.Invoke(vehicleID);
                    GameManager.Instance.OnGameStatusChange?.Invoke(UniversalConstant.GameStatus.LEVEL_FAILED, vehicleID);
                    GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.NPC_HIT);
                }

                // rayDir.x = rayDir.x * _rotationMatrixBy90CW[0] + rayDir.z * _rotationMatrixBy90CW[2];
                // rayDir.z = rayDir.x * _rotationMatrixBy90CW[1] + rayDir.z * _rotationMatrixBy90CW[3];
                // }
            }
        }

        /// <summary>
        /// Start Moving after a delay
        /// </summary>
        /// <param name="npcIndex"> Index of NPC</param>
        private async Task StartMoving(int npcIndex)
        {
            const int delayInSec = 2;
            await Task.Delay(delayInSec * 1000);
            if (_cts.IsCancellationRequested) return;
            _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.IDLE;
            _npcInfos[npcIndex].NpcStatus |= NPCStatus.MOVING;
        }

        // Throw NPC in the direction of being hit
        private async void GoFlying(int npcIndex, Vector3 hitDir)
        {
            float timeElapsed = 0f;
            const float timeMultC = 1.5f, flySpeedMultC = 7f, rotSpeedMultC = 6f;
            Vector3 npcPos = _npcSpawner.NPCsSpawned[npcIndex].position;
            // Vector3 npcRot = _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles;

            // int dirMult = 1;
            Vector3 rotDir = Vector3.zero;
            if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.HORIZONTAL_ALIGNED) == 0)
            {
                rotDir.z = _npcSpawner.NPCsSpawned[npcIndex].forward.z * -1f          // Check which direction to roll
                    * (int)(_npcSpawner.NPCsSpawned[npcIndex].position.x / _walkingBoundaries[1]);      // Check which side the NPC is
            }
            else
            {
                rotDir.x = _npcSpawner.NPCsSpawned[npcIndex].forward.x                // Check which direction to roll
                    * (int)(_npcSpawner.NPCsSpawned[npcIndex].position.z / _walkingBoundaries[0]);      // Check which side the NPC is
            }
            Quaternion rotQuat = Quaternion.Euler(rotDir);
            // Debug.Log($"rotDir: {rotDir} | rotQuat: {rotQuat} | Alignment-Status: {_npcInfos[npcIndex].NpcStatus & NPCStatus.HORIZONTAL_ALIGNED}");

            // if (_npcSpawner.NPCsSpawned[npcIndex].position.)
            while (timeElapsed < 1.0f)
            {
                timeElapsed += Time.deltaTime * timeMultC;

                npcPos += hitDir * Time.deltaTime * flySpeedMultC;
                // npcRot += rotDir * Time.deltaTime * rotSpeedMultC;

                _npcSpawner.NPCsSpawned[npcIndex].position = npcPos;
                // _npcSpawner.NPCsSpawned[npcIndex].GetChild(0).localEulerAngles = npcRot;
                _npcSpawner.NPCsSpawned[npcIndex].GetChild(0).Rotate(rotDir * rotSpeedMultC);

                await Task.Yield();
                if (_cts.Token.IsCancellationRequested) return;
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RenameNPC(in int npcIndex)
        {
            _npcName.Clear();
            _npcName.Append(_npcSpawner.NPCsSpawned[npcIndex].name, 0
                , _npcSpawner.NPCsSpawned[npcIndex].name.Length - 2);
            _npcName.Append(((int)_npcInfos[npcIndex].NpcStatus).ToString("d2"));
            _npcSpawner.NPCsSpawned[npcIndex].name = _npcName.ToString();
        }
    }
}