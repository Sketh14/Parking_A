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
            public Vector2 interactedDir;

            /// <summary> 0: Interacted or Not | 1: Vertical/Horizontal | 2: Positive/Negative | 3: Reached Road Or Not</summary>
            public int vehicleStatus;
        }


        [SerializeField] private Transform[] _vehicleTransforms;
        private VehicleInfo[] _vehicleInfos;
        private int _vehicleSetID;

        private const float _cVehicleSpeedMultiplier = 5f;
        private readonly float[] _roadBoundaries = { 15f, 12f };                 //Vertical | Horizontal

        private void OnDestroy()
        {
            GameManager.Instance.OnSelect -= VehicleSelected;
        }

        private void Start()
        {
            GameManager.Instance.OnSelect += VehicleSelected;

            _vehicleInfos = new VehicleInfo[_vehicleTransforms.Length];
        }

        // Update is called once per frame
        void Update()
        {
            MoveVehicle();
        }

        private void VehicleSelected(int vehicleID, Vector2 slideDir)
        {
            for (int i = 0; i < _vehicleTransforms.Length; i++)
            {
                if (vehicleID == _vehicleTransforms[i].GetInstanceID()
                    // && !vehicleInfos[i].hasInteracted
                    && (_vehicleInfos[i].vehicleStatus & (1 << 0)) == 0)
                {
                    // Debug.Log($"Found Vehicle! | Vehicle ID : {vehicleID}");
                    _vehicleSetID = i;
                    // vehicleInfos[i].hasInteracted = true;
                    _vehicleInfos[i].vehicleStatus |= (1 << 0);

                    //Validate if the slide direction is matching the vehicle's orientation
                    // bool vehicleOrientationVertical = Mathf.Abs(vehicleTransforms[i].forward[2]) >= 0.9f ? true : false;
                    // Debug.Log($"Vehicle Orientation: {Mathf.Abs(vehicleTransforms[i].forward[2])} "
                    // + $" | slideDir: {slideDir.y} | slidedir Rounded: {Mathf.RoundToInt(slideDir.y)}"
                    // + $" | VehicleOrientation: {vehicleOrientationVertical}");

                    if (Mathf.Abs(_vehicleTransforms[i].forward[2]) >= 0.9f)
                    {
                        _vehicleInfos[i].interactedDir.x = 0f;
                        // vehicleInfos[i].interactedDir.y = Mathf.RoundToInt(slideDir.y);     //Dont work well
                        _vehicleInfos[i].interactedDir.y = (Mathf.Abs(slideDir.y) - 0.75f) > 0f ? Mathf.RoundToInt(slideDir.y) : 0f;
                        _vehicleInfos[i].vehicleStatus |= (1 << 1);
                    }
                    else
                    {
                        _vehicleInfos[i].interactedDir.x = (Mathf.Abs(slideDir.x) - 0.75f) > 0f ? Mathf.RoundToInt(slideDir.x) : 0f;
                        _vehicleInfos[i].interactedDir.y = 0f;
                        _vehicleInfos[i].vehicleStatus &= ~(1 << 1);
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
                if ((_vehicleInfos[i].vehicleStatus & (1 << 0)) != 0
                    && (_vehicleInfos[i].vehicleStatus & (1 << 3)) == 0)
                {
                    vehiclePos = _vehicleTransforms[i].transform.position;

                    // Testing
                    vehiclePos.x = _vehicleInfos[i].interactedDir.x;
                    vehiclePos.y = 0f;
                    vehiclePos.z = _vehicleInfos[i].interactedDir.y;
                    // vehiclePos += Time.deltaTime ;
                    _vehicleTransforms[i].transform.position += vehiclePos * _cVehicleSpeedMultiplier * Time.deltaTime;
                    // vehicleTransforms[i].transform.position = vehiclePos + vehicleInfos[i].interactedDir * 10f;

                    //Check if the vehicle has reached the "Road" and then disable it
                    //For Vertical Alignment
                    if ((_vehicleInfos[i].vehicleStatus & (1 << 1)) != 0
                        && (_vehicleTransforms[i].position.z >= _roadBoundaries[0]
                            || _vehicleTransforms[i].position.z <= _roadBoundaries[0] * -1f))
                    {
                        // vehicleInfos[i].hasInteracted = false;
                        // vehicleInfos[i].vehicleStatus = 0;
                        _vehicleInfos[i].vehicleStatus |= (1 << 3);
                    }
                    //For Horizontal Alignment
                    else if (_vehicleTransforms[i].position.x >= _roadBoundaries[1]
                            || _vehicleTransforms[i].position.x <= _roadBoundaries[1] * -1f)
                    {
                        _vehicleInfos[i].vehicleStatus |= (1 << 3);
                    }

                    // Debug.Log($"Vehicle Stats | ID: {i} | pos: {vehicleTransforms[i].transform.position} | "
                    // + $" interactedDir: {vehicleInfos[i].interactedDir}");
                }
            }
        }

        //Responsible for ferrying the vehicle round the park along the road
        private void FerryAroundThePark()
        {

        }
    }
}