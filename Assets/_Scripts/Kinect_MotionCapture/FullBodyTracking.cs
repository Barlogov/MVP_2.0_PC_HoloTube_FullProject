using UnityEngine;
using System.Collections.Generic;
using Kinect = Windows.Kinect;
using Joint = Windows.Kinect.Joint;
using Newtonsoft.Json;
using UnityEngine.Networking.Types;
using System.IO;
using System;

public class FullBodyTracking : MonoBehaviour
{
    public GameObject BodySourceManager;
    private BodySourceManager _BodyManager;

    public NetworkManager netManager;

    public GameObject modelPref;

    public Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>(); // Points in space
    public Dictionary<ulong, GameObject> _Models = new Dictionary<ulong, GameObject>(); // Mesh in space 

    (float x, float y, float z)[] jointPositions = new (float x, float y, float z)[25];

    public Dictionary<Kinect.JointType, Kinect.JointType> boneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },

        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },

        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

    (float x, float y, float z) SpineBasePosition = (0, 0, 0);
    private void OnEnable()
    {
        string path = "ModelCalibrationValues.json";
        try
        {
            StreamReader calibrationReader = new StreamReader(path);
            SpineBasePosition = JsonConvert.DeserializeObject<(float x, float y, float z)>(calibrationReader.ReadLine());
            calibrationReader.Close();

            Debug.Log(SpineBasePosition.x + " " + SpineBasePosition.y + " " + SpineBasePosition.z);
        } 
        catch(Exception e)
        {
            Debug.Log(e);
        }
    }

    void Update()
    {
        if (BodySourceManager == null) return;
        
        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null) return;
        
        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null) return;
        
        List<ulong> trackedIds = new List<ulong>();
        
        foreach (var body in data)
        {
            if (body == null) continue;
            if (body.IsTracked) trackedIds.Add(body.TrackingId);
        }
        
        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownIds)
        {
            if (!trackedIds.Contains(trackingId)) DeleteBodyObject(trackingId);
        }
        
        foreach (var body in data)
        {
            if (body == null) continue;

            if (body.IsTracked)
            {
                if (!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }

                RefreshBodyObject(body, _Bodies[body.TrackingId], body.TrackingId);
            }
        }
    }

    private void DeleteBodyObject(ulong id)
    {
        Destroy(_Bodies[id]);
        _Bodies.Remove(id);
        Destroy(_Models[id]);
        _Models.Remove(id);

        // NET
        netManager.RequestDeleteModelFromSession(id);
        //--NET
    }
   
    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform;

            // Text Coordinates
            if (jt == Kinect.JointType.Head || jt == Kinect.JointType.Neck)
            {
                GameObject worldText = new GameObject("WorldText", typeof(TextMesh));
                worldText.GetComponent<TextMesh>().fontSize = 40;

                worldText.transform.localScale *= 0.15f;
                //worldText.transform.localPosition = new Vector3(0, Random.Range(-4f, 4f), 0);
                worldText.transform.parent = jointObj.transform;
            }
            //
        }

        GameObject personModel = Instantiate(modelPref);
        personModel.name = ("Model:" + id);
        _Models.Add(id, personModel);

        ModelController modelController = personModel.GetComponent<ModelController>();

        // --------------------------------
        // ----- Assigning Targets -------- 
        // --------------------------------

        // LEFT FOOT
        modelController.pole_KneeLeft = body.transform.Find("KneeLeft");
        modelController.target_AnkleLeft = body.transform.Find("AnkleLeft");

        modelController.target_FootLeft = body.transform.Find("FootLeft");

        // RIGHT FOOT
        modelController.pole_KneeRight = body.transform.Find("KneeRight");
        modelController.target_AnkleRight = body.transform.Find("AnkleRight");

        modelController.target_FootRight = body.transform.Find("FootRight");

        // LEFT HAND
        modelController.target_ShoulderLeft = body.transform.Find("ShoulderLeft");

        modelController.pole_ElbowLeft = body.transform.Find("ElbowLeft");
        modelController.target_WristLeft = body.transform.Find("WristLeft");

        modelController.pole_HandLeft = body.transform.Find("HandLeft");
        modelController.target_HandTipLeft = body.transform.Find("HandTipLeft");

        // RIGHT HAND
        modelController.target_ShoulderRight = body.transform.Find("ShoulderRight");

        modelController.pole_ElbowRight = body.transform.Find("ElbowRight");
        modelController.target_WristRight = body.transform.Find("WristRight");

        modelController.pole_HandRight = body.transform.Find("HandRight");
        modelController.target_HandTipRight = body.transform.Find("HandTipRight");

        // SPINE
        modelController.pole_SpineMid = body.transform.Find("SpineMid");
        modelController.target_SpineShoulder = body.transform.Find("SpineShoulder");

        // HEAD
        modelController.pole_Neck = body.transform.Find("Neck");
        modelController.target_Head = body.transform.Find("Head");

        // INITIALIZATION
        modelController.PareTargetsAndBones();

        // NET add model
        netManager.RequestAddModelToSession(id);
        // --NET

        return body;
    }

    private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject, ulong id)
    {
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];

            Transform jointObj = bodyObject.transform.Find(jt.ToString());

            jointObj.localPosition = GetVector3FromJoint(sourceJoint);

            jointPositions[(int)jt].x = jointObj.position.x;
            jointPositions[(int)jt].y = jointObj.position.y;
            jointPositions[(int)jt].z = jointObj.position.z;

            // Text Coordinates
            if (jt == Kinect.JointType.Head || jt == Kinect.JointType.Neck)
            {
                TextMesh WorldText = bodyObject.transform.Find($"{jt.ToString()}/WorldText").GetComponent<TextMesh>(); ;
                WorldText.text = $"x: {body.Joints[jt].Position.X}, y: {body.Joints[jt].Position.Y}, z: {body.Joints[jt].Position.Z}";
            }
            //
        }
        
        GameObject model = _Models[id];
        ModelController modelController = model.GetComponent<ModelController>();
        modelController.SpineBase.transform.position = GetVector3FromJoint(body.Joints[Kinect.JointType.SpineBase]);
        modelController.HipLeft.transform.position = GetVector3FromJoint(body.Joints[Kinect.JointType.HipLeft]);
        modelController.HipRight.transform.position = GetVector3FromJoint(body.Joints[Kinect.JointType.HipRight]);

        // NET
        netManager.SendModelPoints(jointPositions, id);
        // --NET

    }

    private Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X * 10 - SpineBasePosition.x, joint.Position.Y * 10 - SpineBasePosition.y, joint.Position.Z * 10 - SpineBasePosition.z);
    }

}