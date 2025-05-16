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
                    vehicleInfos[i].interactedDir = slideDir;
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
                    /*{
                        vehiclePos.x = vehicleInfos[i].interactedDir.x * 2f;
                        vehiclePos.z = vehicleInfos[i].interactedDir.y * 2f;
                        vehicleTransforms[i].transform.position = vehiclePos;
                        // vehicleTransforms[i].transform.position = vehiclePos + vehicleInfos[i].interactedDir * 10f;
                    }*/


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