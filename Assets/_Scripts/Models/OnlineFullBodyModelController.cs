using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
//using Kinect = Windows.Kinect;
using JointType = Windows.Kinect.JointType;


public class OnlineFullBodyModelController : ModelController
{
    public ulong ID;
    List<GameObject> JointGOs = new List<GameObject>();
    bool modelParametersInitialized = false;
    public Vector3 modelBasePosition = Vector3.zero;

    public (float x, float y, float z)[] onlineJointPositions = new (float x, float y, float z)[25];
    public void SetModelParameters(ulong modelId)
    {
        ID = modelId;
        gameObject.name = ID.ToString();

        // Create cubes to follow
        for (JointType jt = JointType.SpineBase; jt <= JointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = gameObject.transform;

            JointGOs.Add(jointObj);
        }

        // --------------------------------
        // ----- Assigning Targets -------- 
        // --------------------------------

        // LEFT FOOT
        pole_KneeLeft = JointGOs[13].transform; //KneeLeft
        target_AnkleLeft = JointGOs[14].transform; //AnkleLeft

        target_FootLeft = JointGOs[15].transform; //FootLeft

        // RIGHT FOOT
        pole_KneeRight = JointGOs[17].transform; //KneeRight
        target_AnkleRight = JointGOs[18].transform; //AnkleRight

        target_FootRight = JointGOs[19].transform; //FootRight

        // LEFT HAND
        target_ShoulderLeft = JointGOs[4].transform; //ShoulderLeft

        pole_ElbowLeft = JointGOs[5].transform; //ElbowLeft
        target_WristLeft = JointGOs[6].transform; //WristLeft

        pole_HandLeft = JointGOs[7].transform; //HandLeft
        target_HandTipLeft = JointGOs[21].transform; //HandTipLeft

        // RIGHT HAND
        target_ShoulderRight = JointGOs[8].transform; //ShoulderRight

        pole_ElbowRight = JointGOs[9].transform; //ElbowRight
        target_WristRight = JointGOs[10].transform; //WristRight

        pole_HandRight = JointGOs[11].transform; //HandRight
        target_HandTipRight = JointGOs[23].transform; //HandTipRight

        // SPINE
        pole_SpineMid = JointGOs[1].transform; //SpineMid
        target_SpineShoulder = JointGOs[20].transform; //SpineShoulder

        // HEAD
        pole_Neck = JointGOs[2].transform; //Neck
        target_Head = JointGOs[3].transform; //Head

        // INITIALIZATION
        PareTargetsAndBones();
        modelParametersInitialized = true;
    }

    private void Update()
    {
        if (modelParametersInitialized)
        {
            for (JointType jt = JointType.SpineBase; jt <= JointType.ThumbRight; jt++)
            {
                Transform jointObj = JointGOs[(int)jt].transform;
                jointObj.localPosition = new Vector3(onlineJointPositions[(int)jt].x + modelBasePosition.x, 
                    onlineJointPositions[(int)jt].y + modelBasePosition.y, 
                    onlineJointPositions[(int)jt].z + modelBasePosition.z);
            }

            SpineBase.transform.position = new Vector3(onlineJointPositions[0].x + modelBasePosition.x, 
                onlineJointPositions[0].y + modelBasePosition.y, 
                onlineJointPositions[0].z + modelBasePosition.z);
            HipLeft.transform.position = new Vector3(onlineJointPositions[12].x + modelBasePosition.x, 
                onlineJointPositions[12].y + modelBasePosition.y, 
                onlineJointPositions[12].z + modelBasePosition.z);
            HipRight.transform.position = new Vector3(onlineJointPositions[16].x + modelBasePosition.x, 
                onlineJointPositions[16].y + modelBasePosition.y, 
                onlineJointPositions[16].z + modelBasePosition.z);
        }
    }

}
