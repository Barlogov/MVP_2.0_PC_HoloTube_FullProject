using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OnlineModelsManager : MonoBehaviour
{
    public List<ulong> onlineModelIDsToSpawn = new List<ulong>();
    public List<ulong> onlineModelIDsToDelete = new List<ulong>();

    public GameObject onlineFullBodyModelPref;

    Dictionary<ulong, GameObject> onlineModelGOs = new Dictionary<ulong, GameObject>();
    Dictionary<ulong, OnlineFullBodyModelController> onlineModelControllers = new Dictionary<ulong, OnlineFullBodyModelController>();
    Dictionary<ulong, (float x, float y, float z)[]> ModelsJointPositions = new Dictionary<ulong, (float x, float y, float z)[]>();

    public object locker_onlineModelIDsToSpawn = new object();
    public object locker_ModelsJointPositions = new object();

    // Update is called once per frame
    void Update()
    {
        // Delete Online Model
        foreach (ulong modelId in onlineModelIDsToDelete.ToList())
        {
            Destroy(onlineModelGOs[modelId]);

            onlineModelGOs.Remove(modelId);
            onlineModelControllers.Remove(modelId);
            lock (locker_ModelsJointPositions)
            {
                ModelsJointPositions.Remove(modelId);
            }
            

            SetModelsBasePosision();

            // Empty List check
            onlineModelIDsToDelete.Remove(modelId);
            if (onlineModelIDsToDelete.Count == 0) break;
        }

        // Spawn Online Model
        lock (locker_onlineModelIDsToSpawn)
        {
            foreach (ulong modelId in onlineModelIDsToSpawn.ToList())
            {
                // Create model
                GameObject onlineModelGO = Instantiate(onlineFullBodyModelPref);
                onlineModelGOs.Add(modelId, onlineModelGO);
                OnlineFullBodyModelController onlineModelController = onlineModelGO.GetComponent<OnlineFullBodyModelController>();
                onlineModelControllers.Add(modelId, onlineModelController);
                onlineModelController.SetModelParameters(modelId);
                SetModelsBasePosision();

                // Empty List check
                onlineModelIDsToSpawn.Remove(modelId);
                if (onlineModelIDsToSpawn.Count == 0) break;
            }
        }

        lock (locker_ModelsJointPositions)
        {
            // Update model position if UpdateModelsJointPositions was called at least ones
            foreach (ulong modelId in ModelsJointPositions.Keys)
            {
                if (!onlineModelControllers.ContainsKey(modelId)) continue;
                onlineModelControllers[modelId].onlineJointPositions = ModelsJointPositions[modelId];
            }
        }
    }
    public void UpdateModelsJointPositions(ulong modelID, (float x, float y, float z)[] jointPositions)
    {
        lock (locker_ModelsJointPositions)
        {
            if (ModelsJointPositions.ContainsKey(modelID))
            {
                ModelsJointPositions[modelID] = jointPositions;
            }
            else
            {
                ModelsJointPositions.Add(modelID, jointPositions);
            }
        }
    }

    public float maxDistanse;
    public Vector3 modelsSpawnBasePosition;
    void SetModelsBasePosision()
    {
        float gap = 10;

        if (onlineModelControllers.Count == 1)
        {
            onlineModelControllers.ElementAt(0).Value.modelBasePosition = modelsSpawnBasePosition;
            return;
        }

        if (onlineModelControllers.Count == 2)
        {
            onlineModelControllers.ElementAt(0).Value.modelBasePosition = modelsSpawnBasePosition - new Vector3(gap/2, 0, 0);
            onlineModelControllers.ElementAt(1).Value.modelBasePosition = modelsSpawnBasePosition + new Vector3(gap/2, 0, 0);
            return;
        }

        if (onlineModelControllers.Count == 3)
        {
            onlineModelControllers.ElementAt(0).Value.modelBasePosition = modelsSpawnBasePosition - new Vector3(gap, 0, 0);
            onlineModelControllers.ElementAt(1).Value.modelBasePosition = modelsSpawnBasePosition;
            onlineModelControllers.ElementAt(2).Value.modelBasePosition = modelsSpawnBasePosition + new Vector3(gap, 0, 0);
            return;
        }

        if (onlineModelControllers.Count > 3)
        {
            gap = maxDistanse / (onlineModelControllers.Count - 1);

            onlineModelControllers.First().Value.modelBasePosition = modelsSpawnBasePosition - new Vector3(maxDistanse, 0, 0);
            onlineModelControllers.Last().Value.modelBasePosition = modelsSpawnBasePosition + new Vector3(maxDistanse, 0, 0);
            
            for (int i = 1; i < onlineModelControllers.Count; i++)
            {
                onlineModelControllers.ElementAt(i).Value.modelBasePosition = modelsSpawnBasePosition + new Vector3(-maxDistanse + gap*i, 0, 0);
            }
        }
    }
}
