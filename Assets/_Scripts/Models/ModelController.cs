using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelController : MonoBehaviour
{
    // -------------------------
    // ----- BASE BONES --------
    // -------------------------

    [Header("--- Base Bones  ---")]
    public GameObject SpineBase;
    public GameObject HipLeft;
    public GameObject HipRight;

    // ----------------------------
    // ----- BONES WITH IK --------
    // ----------------------------

    [Header("Left Foot: ")]
    public GameObject AnkleLeft;
    public GameObject FootLeft;

    [Header("Right Foot: ")]
    public GameObject AnkleRight;
    public GameObject FootRight;

    [Header("Left Hand: ")]
    public GameObject ShoulderLeft;
    public GameObject WristLeft;
    public GameObject HandTipLeft;

    [Header("Right Hand: ")]
    public GameObject ShoulderRight;
    public GameObject WristRight;
    public GameObject HandTipRight;

    [Header("Spine: ")]
    public GameObject SpineShoulder;

    [Header("Head: ")]
    public GameObject Head;

    // ----------------------
    // ----- Targets -------- 
    // ----------------------

    [Header("Targets | Dont Assign")]
    // Left Foot
    public Transform pole_KneeLeft;
    public Transform target_AnkleLeft;

    public Transform target_FootLeft;

    // Right Foot
    public Transform pole_KneeRight;
    public Transform target_AnkleRight;

    public Transform target_FootRight;

    // Left Hand
    public Transform target_ShoulderLeft;

    public Transform pole_ElbowLeft;
    public Transform target_WristLeft;

    public Transform pole_HandLeft;
    public Transform target_HandTipLeft;

    // Right Hand
    public Transform target_ShoulderRight;

    public Transform pole_ElbowRight;
    public Transform target_WristRight;

    public Transform pole_HandRight;
    public Transform target_HandTipRight;

    // Spine
    public Transform pole_SpineMid;
    public Transform target_SpineShoulder;

    // Head
    public Transform pole_Neck;
    public Transform target_Head;

    private void Awake()
    {
        SpineBase = transform.Find("Root/SpineBase").gameObject;
        HipLeft = transform.Find("Root/HipLeft").gameObject;
        HipRight = transform.Find("Root/HipRight").gameObject;

        AnkleLeft = transform.Find("Root/HipLeft/KneeLeft/AnkleLeft").gameObject;
        FootLeft = transform.Find("Root/HipLeft/KneeLeft/AnkleLeft/FootLeft").gameObject;

        AnkleRight = transform.Find("Root/HipRight/KneeRight/AnkleRight").gameObject;
        FootRight = transform.Find("Root/HipRight/KneeRight/AnkleRight/FootRight").gameObject;

        SpineShoulder = transform.Find("Root/SpineBase/SpineMid/SpineShoulder").gameObject;
        Head = transform.Find("Root/SpineBase/SpineMid/SpineShoulder/Neck/Head").gameObject;

        ShoulderLeft = transform.Find("Root/SpineBase/SpineMid/LH/ShoulderLeft").gameObject;
        WristLeft = transform.Find("Root/SpineBase/SpineMid/LH/ShoulderLeft/ElbowLeft/WristLeft").gameObject;
        HandTipLeft = transform.Find("Root/SpineBase/SpineMid/LH/ShoulderLeft/ElbowLeft/WristLeft/HandLeft/HandTipLeft").gameObject;

        ShoulderRight = transform.Find("Root/SpineBase/SpineMid/RH/ShoulderRight").gameObject;
        WristRight = transform.Find("Root/SpineBase/SpineMid/RH/ShoulderRight/ElbowRight/WristRight").gameObject;
        HandTipRight = transform.Find("Root/SpineBase/SpineMid/RH/ShoulderRight/ElbowRight/WristRight/HandRight/HandTipRight").gameObject;
    }

    public void PareTargetsAndBones()
    {
        // LEFT FOOT
        AnkleLeft.GetComponent<FastIKFabric>().Pole = pole_KneeLeft;
        AnkleLeft.GetComponent<FastIKFabric>().Target = target_AnkleLeft;
        AnkleLeft.GetComponent<FastIKFabric>().Init();

        FootLeft.GetComponent<FastIKFabric>().Target = target_FootLeft;
        FootLeft.GetComponent<FastIKFabric>().Init();

        // RIGHT FOOT
        AnkleRight.GetComponent<FastIKFabric>().Pole = pole_KneeRight;
        AnkleRight.GetComponent<FastIKFabric>().Target = target_AnkleRight;
        AnkleRight.GetComponent<FastIKFabric>().Init();

        FootRight.GetComponent<FastIKFabric>().Target = target_FootRight;
        FootRight.GetComponent<FastIKFabric>().Init();

        // LEFT HAND
        ShoulderLeft.GetComponent<FastIKFabric>().Target = target_ShoulderLeft;
        ShoulderLeft.GetComponent<FastIKFabric>().Init();

        WristLeft.GetComponent<FastIKFabric>().Pole = pole_ElbowLeft;
        WristLeft.GetComponent<FastIKFabric>().Target = target_WristLeft;
        WristLeft.GetComponent<FastIKFabric>().Init();

        HandTipLeft.GetComponent<FastIKFabric>().Pole = pole_HandLeft;
        HandTipLeft.GetComponent<FastIKFabric>().Target = target_HandTipLeft;
        HandTipLeft.GetComponent<FastIKFabric>().Init();

        // RIGHT HAND
        ShoulderRight.GetComponent<FastIKFabric>().Target = target_ShoulderRight;
        ShoulderRight.GetComponent<FastIKFabric>().Init();

        WristRight.GetComponent<FastIKFabric>().Pole = pole_ElbowRight;
        WristRight.GetComponent<FastIKFabric>().Target = target_WristRight;
        WristRight.GetComponent<FastIKFabric>().Init();

        HandTipRight.GetComponent<FastIKFabric>().Pole = pole_HandRight;
        HandTipRight.GetComponent<FastIKFabric>().Target = target_HandTipRight;
        HandTipRight.GetComponent<FastIKFabric>().Init();

        // SPINE
        SpineShoulder.GetComponent<FastIKFabric>().Pole = pole_SpineMid;
        SpineShoulder.GetComponent<FastIKFabric>().Target = target_SpineShoulder;
        SpineShoulder.GetComponent<FastIKFabric>().Init();

        // HEAD
        Head.GetComponent<FastIKFabric>().Pole = pole_Neck;
        Head.GetComponent<FastIKFabric>().Target = target_Head;
        Head.GetComponent<FastIKFabric>().Init();
    }




}


/* Dynamic model lenght
 * 
public Vector3 modelLength = new Vector3(0f, 0f, 0f); // length of model in 3 dimension
float modelMax_X = 0f; // max value between handtips of model
float modelMax_Y = 0f; // max value between hand and foot of model

public Vector3 SyncModelLength()
{
    float localModelDif_X = HandTipRight.transform.position.x - HandTipLeft.transform.position.x; // local value between handtips of model
    float localModelDif_Y = Head.transform.position.y - FootRight.transform.position.y; // local value between hand and foot of model

    if (localModelDif_X > modelMax_X)
    {
        modelMax_X = localModelDif_X;
        modelLength.x = modelMax_X;
        modelLength.z = (modelMax_X + modelMax_Y)/2;
    }
    if(localModelDif_Y > modelMax_Y)
    {
        modelMax_Y = localModelDif_Y;
        modelLength.y = modelMax_Y;
        modelLength.z = (modelMax_X + modelMax_Y) / 2;
    }

    return modelLength;
}
*/