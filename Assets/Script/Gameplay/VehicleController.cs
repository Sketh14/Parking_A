using System;
using UnityEngine;

namespace Test_A.Gameplay
{
    public class VehicleController : MonoBehaviour
    {
        [Serializable]
        internal struct VehicleInfo
        {
            public bool hasInteracted;

            // 0: Interacted or Not | 1: Forwards/Backwards | 2: Left/Right
            // public int vehicleStatus;
            public Vector2 interactedDir;
        }


        [SerializeField] private Transform[] vehicleTransforms;
        private VehicleInfo[] vehicleInfos;
        private int vehicleSetID;

        private void OnDestroy()
        {
            GameManager.Instance.OnSelect -= VehicleSelected;
        }

        private void Start()
        {
            GameManager.Instance.OnSelect += VehicleSelected;

            vehicleInfos = new VehicleInfo[vehicleTransforms.Length];
        }

        // Update is called once per frame
        void Update()
        {
            MoveVehicle();
        }

        private void VehicleSelected(int vehicleID, Vector2 slideDir)
        {
            for (int i = 0; i < vehicleTransforms.Length; i++)
            {
                if (vehicleID == vehicleTransforms[i].GetInstanceID() && !vehicleInfos[i].hasInteracted)
                {
                    // Debug.Log($"Found Vehicle! | Vehicle ID : {vehicleID}");
                    vehicleSetID = i;
                    vehicleInfos[i].hasInteracted = true;

                    //Validate if the slide direction is matching the vehicle's orientation
                    // bool vehicleOrientationVertical = Mathf.Abs(vehicleTransforms[i].forward[2]) >= 0.9f ? true : false;
                    // Debug.Log($"Vehicle Orientation: {Mathf.Abs(vehicleTransforms[i].forward[2])} "
                    // + $" | slideDir: {slideDir.y} | slidedir Rounded: {Mathf.RoundToInt(slideDir.y)}"
                    // + $" | VehicleOrientation: {vehicleOrientationVertical}");

                    if (Mathf.Abs(vehicleTransforms[i].forward[2]) >= 0.9f)
                    {
                        vehicleInfos[i].interactedDir.x = 0f;
                        // vehicleInfos[i].interactedDir.y = Mathf.RoundToInt(slideDir.y);     //Dont work well
                        vehicleInfos[i].interactedDir.y = (Mathf.Abs(slideDir.y) - 0.75f) > 0f ? Mathf.RoundToInt(slideDir.y) : 0f;
                    }
                    else
                    {
                        vehicleInfos[i].interactedDir.x = (Mathf.Abs(slideDir.x) - 0.75f) > 0f ? Mathf.RoundToInt(slideDir.x) : 0f;
                        vehicleInfos[i].interactedDir.y = 0f;
                    }

                    // vehicleInfos[i].interactedDir = slideDir;
                }
            }
        }

        //Responsible for moving the vehicle
        private void MoveVehicle()
        {
            Vector3 vehiclePos;
            for (int i = 0; i < vehicleInfos.Length; i++)
            {
                if (vehicleInfos[i].hasInteracted)
                {
                    vehiclePos = vehicleTransforms[i].transform.position;

                    // Testing
                    // /*
                    {
                        vehiclePos.x += vehicleInfos[i].interactedDir.x * 2f;
                        vehiclePos.z += vehicleInfos[i].interactedDir.y * 2f;
                        vehicleTransforms[i].transform.position = vehiclePos;
                        // vehicleTransforms[i].transform.position = vehiclePos + vehicleInfos[i].interactedDir * 10f;
                    }
                    // */


                    //Check if the vehicle has reached the "Road" and then disable it
                    vehicleInfos[i].hasInteracted = false;

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