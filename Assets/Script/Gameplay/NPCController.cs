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
        }

        internal enum NPCStatus
        {
            IDLE = 0,
            MOVING = 1 << 0,
            HORIZONTAL_ALIGNED = 1 << 2,
            TURNING_CORNER = 1 << 3,
            NPC_HIT = 1 << 4,
        }

        private NPCSpawner _npcSpawner;

        private NPCInfo[] _npcInfos;
        private System.Text.StringBuilder _npcName;
        private CancellationTokenSource _cts;

        private const float _speedMultC = 0.5f;
        private const int _collisionLayerMaskC = (1 << 6) | (1 << 7);
        private const int _vehicleLayerC = 6;
        private readonly float[] _walkingBoundaries = { 10.25f, 5.25f };                 //Vertical | Horizontal
        private readonly float[] _rotationMatrixBy90CW = { 0, -1, 1, 0 };            //Og: [1,0] | [0,1] / 90CW : [0,-1] | [1, 0] / 90CCW : [0, 1] | [-1, 0]

#if SLOWTIME_DEBUG
        public bool slowTime = false, slowTime2 = false;
#endif

        private void OnDestroy()
        {
            if (_cts != null) _cts.Cancel();
        }

        private void Start()
        {
            _cts = new CancellationTokenSource();

            InitializeNPCs();
            _npcName = new System.Text.StringBuilder();
        }

        private void InitializeNPCs()
        {
            _npcSpawner = new NPCSpawner();
            GameManager.Instance.OnEnvironmentSpawned += (boundaryData) =>
            {
                _npcSpawner.SpawnNpcs(boundaryData, InitializeNPCData);
            };
        }

        // Right: {(1.00, 0.00, 0.00)} | Down: {(0.00, 0.00, -1.00)} | Left: {(-1.00, 0.00, 0.00)} | Up: {(0.00, 0.00, 1.00)}
        private void InitializeNPCData()
        {
            _npcInfos = new NPCInfo[_npcSpawner.NPCsSpawned.Count];

            for (int i = 0; i < _npcInfos.Length; i++)
            {
                // UP | DOWN
                if (Mathf.Abs(_npcSpawner.NPCsSpawned[i].forward[2]) >= 0.9f)
                    _npcInfos[i].NpcStatus &= ~NPCStatus.HORIZONTAL_ALIGNED;
                //LEFT | RIGHT
                else if (Mathf.Abs(_npcSpawner.NPCsSpawned[i].forward[0]) >= 0.9f)
                    _npcInfos[i].NpcStatus |= NPCStatus.HORIZONTAL_ALIGNED;

                // Debug.Log($"{_npcSpawner.NPCsSpawned[i].name} | i: {i} | forward-Z: {Mathf.Round(_npcSpawner.NPCsSpawned[i].forward[2])}"
                //     + $" | Horizontal Status: {_npcInfos[i].NpcStatus & NPCStatus.HORIZONTAL_ALIGNED}");
            }

            GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.NPC_SPAWNED);
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
            for (int npcIndex = 0; npcIndex < _npcInfos.Length; npcIndex++)
            {
                if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.NPC_HIT) != 0) continue;

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
                    else if (Mathf.Abs(_npcSpawner.NPCsSpawned[npcIndex].position.z) <= _walkingBoundaries[0] - 0.5f)
                    {
                        _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.TURNING_CORNER;
                    }

                    /*
                    // Turn to VERTICAL | RIGHT
                    if (_npcSpawner.NPCsSpawned[npcIndex].position.z >= _walkingBoundaries[0])
                    {
                        _npcInfos[npcIndex].NpcStatus |= NPCStatus.HORIZONTAL_ALIGNED;

                        npcRot = Vector3.zero;
                        npcRot.y = 90f * -1f * (_npcSpawner.NPCsSpawned[npcIndex].position[0] / _walkingBoundaries[1]);
                        _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles = npcRot;

                        npcPos = _npcSpawner.NPCsSpawned[npcIndex].position;
                        npcPos.z = _walkingBoundaries[0];
                        _npcSpawner.NPCsSpawned[npcIndex].position = npcPos;
                    }
                    // Turn to VERTICAL | LEFT
                    else if (_npcSpawner.NPCsSpawned[npcIndex].position.z <= _walkingBoundaries[0] * -1f)
                    {
                        _npcInfos[npcIndex].NpcStatus |= NPCStatus.HORIZONTAL_ALIGNED;

                        npcRot = Vector3.zero;
                        npcRot.y = 90f * -1f * (_npcSpawner.NPCsSpawned[npcIndex].position[0] / _walkingBoundaries[1]);
                        _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles = npcRot;        // new Vector3(0f, 270f, 0f);

                        npcPos = _npcSpawner.NPCsSpawned[npcIndex].position;
                        npcPos.z = _walkingBoundaries[0] * (int)(_npcSpawner.NPCsSpawned[npcIndex].position[2] / _walkingBoundaries[0]);
                        _npcSpawner.NPCsSpawned[npcIndex].position = npcPos;
                    }
                    */
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
                    else if (Mathf.Abs(_npcSpawner.NPCsSpawned[npcIndex].position.x) <= _walkingBoundaries[1] - 0.5f)
                    {
                        _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.TURNING_CORNER;
                    }

                    /*
                    if (_npcSpawner.NPCsSpawned[npcIndex].position.x >= _walkingBoundaries[1])
                    {
                        _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.HORIZONTAL_ALIGNED;
                        _npcInfos[npcIndex].NpcStatus |= NPCStatus.TURNING_CORNER;

                        npcRot = Vector3.zero;
                        npcRot.y = 180f;
                        _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles = npcRot;

                        npcPos = _npcSpawner.NPCsSpawned[npcIndex].position;
                        npcPos.x = _walkingBoundaries[1];
                        _npcSpawner.NPCsSpawned[npcIndex].position = npcPos;
                        // Debug.Log($"Name: {_npcSpawner.NPCsSpawned[npcIndex].name} | Border Reached | npcPos: {npcPos}"
                        //     + $" | npcRot: {npcRot} | status: {_npcInfos[npcIndex].NpcStatus}");
                    }
                    else if (_npcSpawner.NPCsSpawned[npcIndex].position.x <= _walkingBoundaries[1] * -1f)
                    {
                        _npcInfos[npcIndex].NpcStatus &= ~NPCStatus.HORIZONTAL_ALIGNED;
                        _npcInfos[npcIndex].NpcStatus |= NPCStatus.TURNING_CORNER;

                        npcRot = Vector3.zero;
                        _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles = npcRot;

                        npcPos = _npcSpawner.NPCsSpawned[npcIndex].position;
                        npcPos.x = _walkingBoundaries[1] * -1f;
                        _npcSpawner.NPCsSpawned[npcIndex].position = npcPos;
                    }
                    // */
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

            for (int npcIndex = 0; npcIndex < _npcInfos.Length; npcIndex++)
            {
                if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.NPC_HIT) != 0) continue;

                rayStartPos = _npcSpawner.NPCsSpawned[npcIndex].position;
                rayStartPos.y = 0.3f;
                // + _npcSpawner.NPCsSpawned[npcIndex].forward * (UniversalConstant._CellHalfSizeC / 2f);
                rayDir = _npcSpawner.NPCsSpawned[npcIndex].forward;

                for (int rayIndex = 0; rayIndex < 2; rayIndex++)
                {

#if COLLISIONCHECK_DEBUG
                    Debug.DrawRay(rayStartPos, rayDir * UniversalConstant._CellHalfSizeC, Color.cyan);
#endif
                    if (Physics.Raycast(rayStartPos, rayDir, out colliderHitInfo, UniversalConstant._CellHalfSizeC, _collisionLayerMaskC))
                    {
                        // Debug.Log($"RayCast Hit | Name: {colliderHitInfo.transform.name} | colliderHitInfo-layer: {colliderHitInfo.transform.gameObject.layer}");
                        // Check if the NPC has been hit by a vehicle
                        if (colliderHitInfo.transform.gameObject.layer == _vehicleLayerC)
                        {
                            _npcInfos[npcIndex].NpcStatus |= NPCStatus.NPC_HIT;
                            int vehicleID = -1;
                            int.TryParse(colliderHitInfo.transform.name.Substring(12, 3)
                                , out vehicleID);
                            // Debug.Log($"Hit By Vehicle | ID: {vehicleID} | Hit Dir: {colliderHitInfo.transform.forward}");
                            GoFlying(npcIndex, colliderHitInfo.transform.forward);
                            GameManager.Instance.OnNPCHit?.Invoke(vehicleID);
                            continue;
                        }

                        // If collided, turn the NPC around
                        npcRot = Vector3.zero;

                        // VERTICAL
                        if ((_npcInfos[npcIndex].NpcStatus & NPCStatus.HORIZONTAL_ALIGNED) == 0)
                            npcRot.y = 90 * (1 + (int)_npcSpawner.NPCsSpawned[npcIndex].forward[2]);
                        // HORIZONTAL
                        else
                            npcRot.y = 90 * -1 * (int)_npcSpawner.NPCsSpawned[npcIndex].forward[0];

                        _npcSpawner.NPCsSpawned[npcIndex].localEulerAngles = npcRot;
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
                    // rayDir.x = rayDir.x * _rotationMatrixBy90CW[0] + rayDir.z * _rotationMatrixBy90CW[2];
                    // rayDir.z = rayDir.x * _rotationMatrixBy90CW[1] + rayDir.z * _rotationMatrixBy90CW[3];
                }
            }
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