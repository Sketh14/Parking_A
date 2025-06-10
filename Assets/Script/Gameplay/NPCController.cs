using System;
using UnityEngine;

namespace Parking_A.Gameplay
{
    public class NPCController : MonoBehaviour
    {
        [Serializable]
        internal struct NPCInfo
        {

        }

        [SerializeField] private Transform[] _npcTransforms;
        [SerializeField] private NPCInfo[] _npcInfos;

        private NPCSpawner _npcSpawner;

        private void Start()
        {
            InitializeNPCs();

        }

        private void InitializeNPCs()
        {
            byte[] boundaryData = null;
            GameManager.Instance.OnEnvironmentSpawned += (values) => { boundaryData = values; };

            _npcSpawner = new NPCSpawner();
            _npcSpawner.SpawnNpcs(boundaryData, () => { });
        }

        private void InitializeNPCData(in byte[] npcData)
        {
            _npcInfos = new NPCInfo[npcData.Length];
        }

        private void Update()
        {
            if ((GameManager.Instance.GameStatus & Global.UniversalConstant.GameStatus.LEVEL_GENERATED) == 0) return;

            MoveNPCs();
        }

        private void MoveNPCs()
        {

        }
    }
}