#define TIMESLOW_DEBUG

using System;
using UnityEngine;
using Parking_A.Global;
using System.Runtime.CompilerServices;

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
        }

        private NPCSpawner _npcSpawner;

        private NPCInfo[] _npcInfos;
        private System.Text.StringBuilder _npcName;

        private const float _speedMult = 3f;
        private readonly float[] _walkingBoundaries = { 10.25f, 5.25f };                 //Vertical | Horizontal

#if TIMESLOW_DEBUG
        public bool slowTime = false, slowTime2 = false;
#endif

        private void Start()
        {
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

                Debug.Log($"{_npcSpawner.NPCsSpawned[i].name} | i: {i} | forward-Z: {Mathf.Round(_npcSpawner.NPCsSpawned[i].forward[2])}"
                    + $" | Status: {_npcInfos[i].NpcStatus}");
            }

            GameManager.Instance.SetGameStatus(UniversalConstant.GameStatus.NPC_SPAWNED);
        }

        private void Update()
        {
            if ((GameManager.Instance.GameStatus & UniversalConstant.GameStatus.LEVEL_GENERATED) == 0) return;

            MoveNPCs();

#if TIMESLOW_DEBUG
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

        private void MoveNPCs()
        {
            Vector3 npcPos, npcRot;
            for (int npcIndex = 0; npcIndex < _npcInfos.Length; npcIndex++)
            {
                _npcSpawner.NPCsSpawned[npcIndex].position += _npcSpawner.NPCsSpawned[npcIndex].forward * Time.deltaTime * _speedMult;

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