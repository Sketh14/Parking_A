using UnityEngine;

namespace Parking_A.Global
{
    [CreateAssetMenu(fileName = "PoolSO_", menuName = "Scriptable Objects/Pool SO")]
    public class PoolScriptableObject : ScriptableObject
    {
        public UniversalConstant.PoolType poolType;
        public GameObject poolPrefab;
    }
}