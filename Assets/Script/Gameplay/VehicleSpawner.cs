using System;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

namespace Test_A.Gameplay
{
    [Serializable]
    public class VehicleSpawner
    {
        // private readonly Vector2 _topLeft = new Vector2(-12f, 15f);
        // private readonly Vector2 _bottomRight = new Vector2(12f, -15f);

        //Top-Left: {-6, 11} | Bottom-Right: {6, -11}
        private readonly float[] _borderCoordinates = new float[] { -6f, 11f, 6f, -11f };

        //Single float array | Will access dimensions using offset
        private readonly float[] _vehicleDimensions = new float[] { 1f, 1f, 1f };

        public Transform[] SpawnVehicles()
        {
            Debug.Log($"Spawning Vehicles");
            List<Transform> vehiclesSpawned = new List<Transform>();
            Vector3 spawnPos = Vector3.zero, spawnRot = Vector3.zero;
            bool lotFull = false;
            float xPos = 0f, zPos = 0f, yRot = 0f;

            //Keep filling until the necessary amount of vehicles are filled in the parking lot
            // while(!lotFull)
            for (int i = 0; i < 5; i++)
            {
                vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[PoolManager.PoolType.VEHICLE_1].Get().transform);

                //Put vehicles between lot boundaries
                xPos = Random.Range(-1f, 1f) * (_borderCoordinates[2] - 1f);
                zPos = Random.Range(-1f, 1f) * (_borderCoordinates[1] - 1f);
                yRot = (Random.Range(0f, 1f) > 0.7f) ? 90f : 0f;

                spawnPos.Set(xPos, 0.58f, zPos);
                spawnRot.Set(0f, yRot, 0f);

                vehiclesSpawned[i].position = spawnPos;
                vehiclesSpawned[i].localEulerAngles = spawnRot;
            }

            //Tests
            /*{
                // GameObject vehicle;
                vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[PoolManager.PoolType.VEHICLE_1].Get().transform);
                // vehiclesSpawned[0].position.Set(0f, 0.58f, -6f);
                vehiclesSpawned[0].position = new Vector3(0f, 0.58f, -6f);
                vehiclesSpawned[0].localEulerAngles = new Vector3(0f, 90f, 0f);
                // vehiclesSpawned.Add(vehicle.transform);

                vehiclesSpawned.Add(PoolManager.Instance.PrefabPool[PoolManager.PoolType.VEHICLE_1].Get().transform);
                // vehiclesSpawned[1].position.Set(0f, 0.58f, -3f);
                vehiclesSpawned[1].position = new Vector3(0f, 0.58f, -3f);
                vehiclesSpawned[1].localEulerAngles = new Vector3(0f, 0f, 0f);
                // vehiclesSpawned.Add(vehicle.transform);
            }*/

            return vehiclesSpawned.ToArray();
        }

        // private void
    }
}