using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public NetworkManager networkManager;
    public AudioManager audioManager;

    [Header("Objects to disable", order = 0)]
    public GameObject UI_ConnectToServer;
    public GameObject UI_ConnectToSession;
    public GameObject UI_ModelCalibration;
    public GameObject KinectGO;
    public GameObject ModelCalibrationRoomGO;
    public GameObject UI_Session;

    List<GameObject> AllObjects;

    [Header("", order = 1)]
    public Camera mainCamera;
    public Text Text_Connection;

    public Dropdown sessionsDropdown;

    public Text debugText;

    public bool needToComeBackToFirstMenu = false;
    public string errorMessage;

    public Toggle microphoneToggle;
    public Toggle speakerToggle;

    public Toggle kinectToggle;
    bool wantEnableKinect = false;
    bool onlineKinectCanBeEnabled = false;
    bool calibrationKinectCanBeEnabled = false;

    bool inCalibration = false;

    [Header("Model Calibration", order = 2)]
    public ModelCalibrator modelCalibrator;
    public Text countDownText;
    public Text colibrationResultText;
    public GameObject StartCalibrationButtonGO;
    public GameObject RecalibrateButtonGO;

    private void Awake()
    {
        /*
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
        */

        AllObjects = new List<GameObject>()
        {
            UI_ConnectToServer,
            UI_ConnectToSession,
            UI_ModelCalibration,
            KinectGO,
            ModelCalibrationRoomGO,
            UI_Session
        };
        ComeBackToFirstMenu();
    }

    //public bool? connectedToServer = null;
    private void Update()
    {
        if (needToComeBackToFirstMenu) ComeBackToFirstMenu();


        if (currentActiveDot == ActiveDot.none) return;
        ServerMode_RedX.SetActive(false);
        ServerMode_RedDot.SetActive(false);
        ServerMode_GreenDot.SetActive(false);
        NATMode_RedX.SetActive(false);
        NATMode_RedDot.SetActive(false);
        NATMode_GreenDot.SetActive(false);
        switch (currentActiveDot)
        {
            case ActiveDot.ServerMode_RedX:
                ServerMode_RedX.SetActive(true);
                break;
            case ActiveDot.ServerMode_RedDot:
                ServerMode_RedDot.SetActive(true);
                break;
            case ActiveDot.ServerMode_GreenDot:
                ServerMode_GreenDot.SetActive(true);
                break;

            case ActiveDot.NATMode_RedX:
                NATMode_RedX.SetActive(true);
                break;
            case ActiveDot.NATMode_RedDot:
                NATMode_RedDot.SetActive(true);
                break;
            case ActiveDot.NATMode_GreenDot:
                NATMode_GreenDot.SetActive(true);
                break;
        }
    }

    public void ToggleKinnect()
    {
        wantEnableKinect = kinectToggle.isOn;
        if (wantEnableKinect && onlineKinectCanBeEnabled) KinectGO.SetActive(true); else KinectGO.SetActive(false);
        if (wantEnableKinect && calibrationKinectCanBeEnabled) ModelCalibrationRoomGO.SetActive(true); else ModelCalibrationRoomGO.SetActive(false);
    }

    public void ToggleMicrophone()
    {
        if (microphoneToggle.isOn)
        {
            audioManager.EnableMicrophone();
        } else
        {
            audioManager.DisableMicrophone();
        }
    }

    public void ToggleSpeaker()
    {
        if (speakerToggle.isOn)
        {
            audioManager.EnableSpeaker();
        }
        else
        {
            audioManager.DisableSpeaker();
        }
    }

    public void DisconnectError(string errorMsg)
    {
        needToComeBackToFirstMenu = true;
        errorMessage = errorMsg;
    }

    public void ComeBackToFirstMenu()
    {
        inCalibration = false;
        onlineKinectCanBeEnabled = false;
        calibrationKinectCanBeEnabled = false;

        if (networkManager.connectedToServer_TCP)
            networkManager.DisconnectFromServer();

        foreach (GameObject go in AllObjects)
        {
            go.SetActive(false);
        }
        UI_ConnectToServer.SetActive(true);
        Text_Connection.text = errorMessage;
        errorMessage = "";
        needToComeBackToFirstMenu = false;
    }
    public void Button_ModelCalibration()
    {
        calibrationKinectCanBeEnabled = true;

        UI_ConnectToServer.SetActive(false);
        if (wantEnableKinect) ModelCalibrationRoomGO.SetActive(true);
        UI_ModelCalibration.SetActive(true);
        countDownText.gameObject.SetActive(false);

        colibrationResultText.gameObject.SetActive(false);

        StartCalibrationButtonGO.SetActive(true);

        RecalibrateButtonGO.SetActive(false);
    }

    Vector3 cameraBasePosition;
    Quaternion cameraBaseRotation;
    public void Button_StartCalibration()
    {
        cameraBasePosition = mainCamera.transform.position;
        cameraBaseRotation = mainCamera.transform.rotation;
        mainCamera.transform.position = new Vector3(0, 0.8f, 0);
        mainCamera.transform.rotation = Quaternion.identity;

        colibrationResultText.gameObject.SetActive(false);
        RecalibrateButtonGO.SetActive(false);

        StartCalibrationButtonGO.SetActive(false);
        countDownText.gameObject.SetActive(true);

        inCalibration = true;
        StartCoroutine(CountDownToCalibration());
    }



    private IEnumerator CountDownToCalibration()
    {
        for (int i = 5; i >= 0; i--)
        {
            countDownText.text = i.ToString();
            yield return new WaitForSeconds(1);
        }
        if (inCalibration)
        {
            countDownText.gameObject.SetActive(false);
            colibrationResultText.text = "Calibration result: " + modelCalibrator.WriteDataToCalibrationFile();
            colibrationResultText.gameObject.SetActive(true);
            RecalibrateButtonGO.SetActive(true);
            mainCamera.transform.position = cameraBasePosition;
            mainCamera.transform.rotation = cameraBaseRotation;
        }
    }

    bool connectingToServer = false;
    public async void Button_ConnectToServer()
    {
        if (!connectingToServer)
        {
            connectingToServer = true;
            Text_Connection.text = "Connection...";

            bool connectedToServer = await networkManager.ConnectToServerAsync();
            if (connectedToServer)
            {
                SessionsRefreshButton();

                UI_ConnectToServer.SetActive(false);
                UI_ConnectToSession.SetActive(true);
            }
            else
            {
                Text_Connection.text = "Can't connect to server. Try again.";
            }
            connectingToServer = false;
        }

    }

    public void SessionsRefreshButton()
    {
        networkManager.RequestListOfSessionsIDs();
        while (networkManager.sessionsIDs.Count == 0) { } //Wait for getting List of SessionsIDs from server

        sessionsDropdown.ClearOptions();
        sessionsDropdown.AddOptions(networkManager.sessionsIDs);
    }

    public void ConnectToSessionButton()
    {
        onlineKinectCanBeEnabled = true;

        networkManager.RequestConnectClientToSession(sessionsDropdown.options[sessionsDropdown.value].text);
        Debug.Log("Connected to session: " + sessionsDropdown.options[sessionsDropdown.value].text);
        UI_ConnectToSession.SetActive(false);
        UI_Session.SetActive(true);
        if (wantEnableKinect) KinectGO.SetActive(true);
    }

    [SerializeField] GameObject ServerMode_RedX;
    [SerializeField] GameObject ServerMode_RedDot;
    [SerializeField] GameObject ServerMode_GreenDot;
    [SerializeField] GameObject NATMode_RedX;
    [SerializeField] GameObject NATMode_RedDot;
    [SerializeField] GameObject NATMode_GreenDot;

    enum ActiveDot
    {
        none,
        ServerMode_RedX,
        ServerMode_RedDot,
        ServerMode_GreenDot,
        NATMode_RedX,
        NATMode_RedDot,
        NATMode_GreenDot
    }
    ActiveDot currentActiveDot = ActiveDot.none; 
    //SetActive can only be called from the main thread.
    private void ChangeActiveDot(ActiveDot newActiveDot)
    {
        currentActiveDot = newActiveDot;
    }

    public void ChangeUI_NETMode_None()
    {
        ChangeActiveDot(ActiveDot.none);
    }

    bool connectingToDifNETMode = false;
    public async void Button_WantChange_P2PNETMode_ToServer()
    {
        if (connectingToDifNETMode) return;
        connectingToDifNETMode = true;
        await networkManager.SendRequestToChangeP2PNETMode_ToServer();
        connectingToDifNETMode = false;
    }

    public void ChangeUI_NETMode_Server_Connection()
    {
        ChangeActiveDot(ActiveDot.ServerMode_RedDot);
    }

    public void ChangeUI_NETMode_Server_Success()
    {
        ChangeActiveDot(ActiveDot.ServerMode_GreenDot);
    }

    public void ChangeUI_NETMode_Server_Fail()
    {
        ChangeActiveDot(ActiveDot.ServerMode_RedX);
    }

    public async void Button_WantChange_P2PNETMode_ToNAT()
    {
        if (connectingToDifNETMode) return;
        connectingToDifNETMode = true;

        await networkManager.SendRequestToChangeP2PNETMode_ToNAT();

        connectingToDifNETMode = false;
    }

    public void ChangeUI_NETMode_NAT_Connection()
    {
        ChangeActiveDot(ActiveDot.NATMode_RedDot);
    }

    public void ChangeUI_NETMode_NAT_Success()
    {
        ChangeActiveDot(ActiveDot.NATMode_GreenDot);
    }

    public void ChangeUI_NETMode_NAT_Fail()
    {
        ChangeActiveDot(ActiveDot.NATMode_RedX);
    }
}
