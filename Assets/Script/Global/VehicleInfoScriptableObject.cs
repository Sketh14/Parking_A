using UnityEngine;

namespace Parking_A.Global
{
    [CreateAssetMenu(fileName = "VehicleInfoSO_", menuName = "Scriptable Objects/VehicleInfo SO")]
    public class VehicleInfoScriptableObject : ScriptableObject
    {
        public UniversalConstant.PoolType VehicleType;
        public int[] SkinPrice;
        public Material[] SkinsMat;
    }
}