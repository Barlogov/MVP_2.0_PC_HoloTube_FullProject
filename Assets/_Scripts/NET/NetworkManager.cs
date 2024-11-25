using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Data;

using LumiSoft.Net.STUN.Client;
using System.Linq;
using UnityEngine.UI;

/*
#if PLATFORM_STANDALONE_WIN
using WindowsFirewallHelper;
using System.Reflection;
#endif
*/
public class NetworkManager : MonoBehaviour
{
    // Managers
    [SerializeField] UIManager uiManager;
    [SerializeField] OnlineModelsManager onlineModelsManager;
    [SerializeField] AudioManager audioManager;

    //string SERVER_HOST = "192.168.31.70"; // Danil PC
    string SERVER_HOST = "94.140.222.95"; // WAN Ubuntu.22.04-x64 Server                                      

    //string SERVER_HOST = "192.168.31.173"; // Bogdan PC

    int SERVER_PORT = 8888;

    string STUN_HOST = "stun.l.google.com";
    int STUN_PORT = 19302;

    IPEndPoint serverEP_UDP;

    UdpClient udpClient;
    TcpClient tcpClient;
    StreamReader TCP_Reader = null;
    StreamWriter TCP_Writer = null;

    bool client_ID_Received = false;
    string client_ID = string.Empty;
    byte[] client_ID_Length_Byte;
    byte[] client_ID_Byte;

    public List<string> sessionsIDs = new List<string>();
    [SerializeField] List<IPEndPoint> WAN_Clients_UDP_EP = new List<IPEndPoint>();
    [SerializeField] List<IPEndPoint> LAN_Clients_UDP_EP = new List<IPEndPoint>();
    [SerializeField] Dictionary<IPEndPoint, IPEndPoint> WAN_LAN_Correlation_Clients_UDP_EP = new Dictionary<IPEndPoint, IPEndPoint>();

    IPEndPoint WAN_Reciever_UDP_EndPoint;
    byte[] WAN_Reciever_UDP_EndPoint_Bytes = new byte[6];
    byte[] WAN_Reciever_UDP_IP_Bytes = new byte[4];
    byte[] WAN_Reciever_UDP_Port_Bytes = new byte[2];

    bool received_WAN_Reciever_UDP_EndPoint = false;

    IPEndPoint LAN_Reciever_UDP_EndPoint;
    byte[] LAN_Reciever_UDP_EndPoint_Bytes = new byte[6];
    byte[] LAN_Reciever_UDP_IP_Bytes = new byte[4];
    byte[] LAN_Reciever_UDP_Port_Bytes = new byte[2];

    // Traffic testing
    float sentMsgs = 0f;
    float receivedMsgs = 0f;
    int numberOfClientsInSession = 0;

/*    DateTime? firstMsgTime = null;
    DateTime lastMsgTime;*/

    bool programActive = false;
    public bool connectedToServer_TCP = false;
    public bool connectedToSession = false;

    bool connectedToServer_UDP = false;

    CancellationTokenSource cts_onExitFromServer;

    public int AliveMsgPeriod_Ms = 1000;
    string MSGSplitter = "///";
    //NAT_Type currentNAT = NAT_Type.Undefined;

    Session_P2P_NET_Mode currentP2PNETMode = Session_P2P_NET_Mode.None;
    bool receivedResponseChangeP2PNETMode_ToServer = false;
    bool receivedResponseChangeP2PNETMode_ToNAT = false;

    enum MsgType_TCP
    {
        Error, // idk
        Ping, // Alive msg to avoid disconnect
        TextMessage, // Send text msg for debugging
        
        Request_ClientToServer_GetSessionsListIDs, // Client Request to get List from Server
        Response_ServerToClient_GetSessionsListIDs, // Server Response with List<Session>

        Request_ClientToServer_AddPublicUDP_EP, // Client send its "UDP listen port" to Server

        Request_ClientToSession_ConnectToSession, // Client trying to conect to Session
        Response_SessionToClient_ConnectToSession, // Connection to Session state (true or false)

        Request_ClientToSession_AddModel, // Client has Created a local Model and notifies the Session about it
        Request_ClientToSession_DeleteModel, // Client has Deleted a local Model and notifies the Session about it

        Request_ClientToServer_ClientEndPointUDP, // Client asks for its UDP address
        Response_ServerToClient_ClientEndPointUDP, // Server sends the Client's UDP address

        Broadcast_SessionToClients_ClientUDPEndPoints, // Session broadcast msg about Clients UDPEndPoint on the session
        
        Broadcast_SessionToClients_AddModel, // Session broadcast msg about new model was added on another Client
        Broadcast_SessionToClients_DeleteModel, // Session broadcast msg about deleted model from Client

        Request_ClientToServer_Disconect, // Сlient asks to be disconnected from the server
        Response_ServerToClient_Disconect, // Nothing atm  -- mb add error on Server close

        Request_ClientToSession_CurrentSessionP2PNETMode, // Client Request current Session mode 

        Request_ClientToSession_ChangeP2PNETMode_ToServer, // Client Request change Session mode to be "Server"
        Response_SessionToClient_ChangeP2PNETMode_ToServer, // Session ask client to chenge it Session mode to "Server"

        Request_ClientToSession_ChangeP2PNETMode_ToNAT, // Client Request change Session mode to be "NAT"
        Response_SessionToClient_ChangeP2PNETMode_ToNAT, // Session ask client to chenge it Session mode to "NAT"

        Request_ClientToServer_GetID,
        Response_ServerToClient_GetID,

        Response_ServerToClient_UDP_ConnectionState
    }

    enum MsgType_UDP
    {
        Error,
        Ping,
        TextMessage, // Send text msg for debugging

        ConnectionRequest,

        OnlineModelPoints,
        AudioSample,

        SearchClients_LAN_Request,
        SearchClients_LAN_Response,
        
        TURNServer
    }

    enum Session_P2P_NET_Mode
    {
        None,
        Server,
        NAT
    }

    private void Awake()
    {
        programActive = true;
    }

    float sentAudioSamples = 0;
    float receivedAudioSamples = 0;
    private void FixedUpdate()
    {
        // UDP Test
        if (connectedToSession)
        {
            if(numberOfClientsInSession < 2)
            {
                uiManager.debugText.text = $"You are all alone D:\n";
            } else
            {
                if (currentP2PNETMode == Session_P2P_NET_Mode.NAT)
                {
                    uiManager.debugText.text =
                        $"Nember of connected users: {numberOfClientsInSession}\n" +
                        $"- NAT Mode -\n" +
                        $"UDP Ping Packets Sent: {sentMsgs}\n" +
                        $"UDP Ping Packets Received: {receivedMsgs / (numberOfClientsInSession - 1)}\n" +
                        $"Audio Samples Sent: {sentAudioSamples}\n" +
                        $"Audio Samples Received: {receivedAudioSamples / (numberOfClientsInSession - 1)}\n";

                    uiManager.debugText.text += $"\nWAN {WAN_Reciever_UDP_EndPoint}, other users:\n";
                    lock (WAN_Clients_UDP_EP)
                    {
                        foreach (IPEndPoint ep in WAN_Clients_UDP_EP)
                        {
                            uiManager.debugText.text += ep.ToString();
                            uiManager.debugText.text += "\n";
                        }
                    }
                    uiManager.debugText.text += $"\nLAN {LAN_Reciever_UDP_EndPoint}, other users:\n";
                    lock (LAN_Clients_UDP_EP)
                    {
                        foreach (IPEndPoint ep in LAN_Clients_UDP_EP)
                        {
                            uiManager.debugText.text += ep.ToString();
                            uiManager.debugText.text += "\n";
                        }
                    }
                }

                if (currentP2PNETMode == Session_P2P_NET_Mode.Server)
                {
                    receivedMsgs = sentMsgs = 0;
                    uiManager.debugText.text =
                        $"Nember of connected users: {numberOfClientsInSession}\n" +
                        $"- Server Mode -\n" +
                        $"Audio Samples Sent: {sentAudioSamples}\n" +
                        $"Audio Samples Received: {receivedAudioSamples / (numberOfClientsInSession - 1)}\n";
                }
            }
            
        } else
        {
            receivedMsgs = sentMsgs = receivedAudioSamples = sentAudioSamples = 0;
            uiManager.debugText.text = "";
        }
        // --UDP Test
    }

    // TCP and UDP Connection
    public async Task<bool> ConnectToServerAsync()
    {
        Debug.Log("Connecting to the server...");
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(5000);
        try
        {
            // -------------------------------------
            //              TCP + UDP
            // -------------------------------------
            // TCP
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(SERVER_HOST, SERVER_PORT);
            Debug.Log("Connected to server: " + tcpClient.Connected);
            TCP_Reader = new StreamReader(tcpClient.GetStream());
            TCP_Writer = new StreamWriter(tcpClient.GetStream());
            TCP_Writer.AutoFlush = true;
            if (TCP_Writer is null || TCP_Reader is null)
            {
                throw new Exception("TCP_Writer is null || TCP_Reader is null");
            }
            connectedToServer_TCP = true;

            // UDP
            udpClient = new UdpClient(0, AddressFamily.InterNetwork);
            serverEP_UDP = new IPEndPoint(IPAddress.Parse(SERVER_HOST), SERVER_PORT);

            // -------------------------------------
            //              NAT + LAN
            // -------------------------------------
            STUN_Result result = null;

            await Task.Run(() => {
                result = STUN_Client.Query(STUN_HOST, STUN_PORT, udpClient.Client);

                string strHostName = Dns.GetHostName();

                IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
                IPAddress[] addr = ipEntry.AddressList;

                foreach (IPAddress ip in addr)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        LAN_Reciever_UDP_EndPoint = new IPEndPoint(ip, ((IPEndPoint)udpClient.Client.LocalEndPoint).Port);
                        LAN_Reciever_UDP_IP_Bytes = LAN_Reciever_UDP_EndPoint.Address.GetAddressBytes();
                        LAN_Reciever_UDP_Port_Bytes = BitConverter.GetBytes((UInt16)LAN_Reciever_UDP_EndPoint.Port);
                        LAN_Reciever_UDP_EndPoint_Bytes = LAN_Reciever_UDP_IP_Bytes.Concat(LAN_Reciever_UDP_Port_Bytes).ToArray();
                    }
                }
            });

            Debug.Log("LAN EP: " + LAN_Reciever_UDP_EndPoint);

            if (result.NetType == STUN_NetType.UdpBlocked)
            {
                Debug.Log($"Blocked or bad STUN server!");
                Debug.Log("Connection failed !!!");
                return false;
            }
            else
            {
                WAN_Reciever_UDP_EndPoint = result.PublicEndPoint;

                WAN_Reciever_UDP_IP_Bytes = WAN_Reciever_UDP_EndPoint.Address.GetAddressBytes(); ;
                WAN_Reciever_UDP_Port_Bytes = BitConverter.GetBytes((UInt16)WAN_Reciever_UDP_EndPoint.Port);
                WAN_Reciever_UDP_EndPoint_Bytes = WAN_Reciever_UDP_IP_Bytes.Concat(WAN_Reciever_UDP_Port_Bytes).ToArray();

                Debug.Log($"Public UDP Reciever EndPoint: {WAN_Reciever_UDP_EndPoint.Address}:{WAN_Reciever_UDP_EndPoint.Port}");
                Debug.Log("NAT type: " + result.NetType);
                Send_Public_UDP_EP_ToServer(WAN_Reciever_UDP_EndPoint); // MayBe: Add async/await here to make sure the Server knows the Client's port
            }

            // -------------------------------------
            //               Tasks
            // -------------------------------------

            cts_onExitFromServer = new CancellationTokenSource();

            _ = Task.Run(ReceiveMessageAsync_TCP, cts_onExitFromServer.Token);
            _ = Task.Run(ReceiveMessageAsync_UDP, cts_onExitFromServer.Token);
            _ = Task.Run(SendAliveMsgAsync_TCP, cts_onExitFromServer.Token);
            _ = Task.Run(SendAliveMsgAsync_UDP, cts_onExitFromServer.Token);

            // -------------------------------------
            //       TCP Connection to Server
            // -------------------------------------

            SendMessageToServer_TCP(MsgType_TCP.Request_ClientToServer_GetID);
            while (!client_ID_Received){ } // Wait for client_ID

            // -------------------------------------
            //       UDP Connection to Server
            // -------------------------------------

            byte numberOfUDPConnectionRequests = 0;
            byte[] data = Encoding.UTF8.GetBytes(client_ID);
            while (!connectedToServer_UDP)
            {
                if (numberOfUDPConnectionRequests < 5)
                {
                    SendMessageToServer_UDP(MsgType_UDP.ConnectionRequest, data);
                    Debug.Log("UDP ConnectionRequest Sent");
                    numberOfUDPConnectionRequests++;
                    await Task.Delay(1000);
                }
                else
                {
                    Debug.Log($"connectedToServer_TCP: {connectedToServer_TCP}, connectedToServer_UDP: {connectedToServer_UDP}");
                    DisconnectFromServer();
                    return false;
                }
            }

            Debug.Log("Connection successful!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            return false;
        }
    }

    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //                                                                                                      UDP
    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    // UDP Reader           
    // Hex                 [00 00 00 00]   [00 00]        [00]
    // UDP msg struckture:      IP           Port       MsgType_UDP
    async Task ReceiveMessageAsync_UDP()
    {
        while (programActive)
        {
            if (cts_onExitFromServer.Token.IsCancellationRequested) return;
            try
            {
                var receivedDatagram = await udpClient.ReceiveAsync();
                //Debug.Log($"Received {BitConverter.ToString(receivedDatagram.Buffer)}");
                if (receivedDatagram.Buffer.Length == 0) continue;
                byte[] senderIP = receivedDatagram.Buffer.Take(4).ToArray();
                byte[] senderPort = receivedDatagram.Buffer.Skip(4).Take(2).ToArray(); // BitConverter.GetBytes((UInt16)65500) / BitConverter.ToUInt16(port)
                byte receivedMsgType = receivedDatagram.Buffer.Skip(6).Take(1).ToArray()[0];
                byte[] receivedMessage = receivedDatagram.Buffer.Skip(7).ToArray();
                
                // Errors
                if (receivedMsgType == (byte)MsgType_UDP.Error)
                {
                    Debug.Log($"Error: {Encoding.UTF8.GetString(receivedMessage)}");
                    continue;
                }

                // Ping
                if (receivedMsgType == (byte)MsgType_UDP.Ping)
                {
                    receivedMsgs++;
                    Debug.Log($"Ping from: {receivedDatagram.RemoteEndPoint}");
                    continue;
                }

                // Text
                if (receivedMsgType == (byte)MsgType_UDP.TextMessage)
                {
                    continue;
                }

                // OnlineModelPoints
                if (receivedMsgType == (byte)MsgType_UDP.OnlineModelPoints)
                {
                    ulong modelID = BitConverter.ToUInt64(receivedMessage.Take(8).ToArray(), 0); // Ulong = UInt64 = 8 bytes 
                    (float x, float y, float z)[] jointPositions = JsonConvert.DeserializeObject<(float x, float y, float z)[]>(Encoding.UTF8.GetString(receivedMessage.Skip(8).ToArray()));
                    onlineModelsManager.UpdateModelsJointPositions(modelID, jointPositions);
                    continue;
                }

                // AudioSample
                if (receivedMsgType == (byte)MsgType_UDP.AudioSample)
                {
                    Debug.Log("Received Audio Sample");
                    receivedAudioSamples++;
                    audioManager.ProvideAudioSample(receivedMessage);
                    continue;
                }

                // LAN connection Request
                if (receivedMsgType == (byte)MsgType_UDP.SearchClients_LAN_Request)
                {
                    //            EP                                     msgType                                                             msg: 4 byte IP + 2 byte Port                                    
                    byte[] data = LAN_Reciever_UDP_EndPoint_Bytes.Concat(new byte[] { (byte)MsgType_UDP.SearchClients_LAN_Response }).Concat(WAN_Reciever_UDP_EndPoint_Bytes).ToArray();

                    lock (udpClient)
                    {
                        udpClient.Send(data, data.Length, receivedDatagram.RemoteEndPoint); // new IPEndPoint(new IPAddress(senderIP), BitConverter.ToUInt16(senderPort, 0)
                    }

                    continue;
                }

                // LAN connection Response
                if (receivedMsgType == (byte)MsgType_UDP.SearchClients_LAN_Response)
                {
                    IPEndPoint sender_LAN_EP = receivedDatagram.RemoteEndPoint; // new IPEndPoint(new IPAddress(senderIP), BitConverter.ToUInt16(senderPort, 0))
                    IPEndPoint sender_WAN_EP = new IPEndPoint(new IPAddress(receivedMessage.Take(4).ToArray()), BitConverter.ToUInt16(receivedMessage.Skip(4).Take(2).ToArray(), 0));

                    if (sender_LAN_EP.Port == LAN_Reciever_UDP_EndPoint.Port && sender_LAN_EP.Address.Equals(LAN_Reciever_UDP_EndPoint.Address)) continue; // Dont add yourself
                    
                    if (LAN_Clients_UDP_EP.Exists(x => x.Equals(sender_LAN_EP)))
                    {
                        Debug.Log($"----------- Already have this EP: {sender_LAN_EP}");
                        continue;
                    } else
                    {
                        Debug.Log($"----------- Received new correlation: sender_LAN_EP: {sender_LAN_EP}, sender_WAN_EP: {sender_WAN_EP}");
                    }

                    lock (LAN_Clients_UDP_EP)
                    {
                        try
                        {
                            LAN_Clients_UDP_EP.Add(sender_LAN_EP);
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                        }
                    }
                    lock (WAN_Clients_UDP_EP)
                    {
                        try
                        {
                            WAN_Clients_UDP_EP.Remove(sender_WAN_EP);
                        }
                        catch (Exception e)
                        {
                            Debug.Log(e);
                        }
                    }
                    continue;
                }

                Debug.Log($"Unknown message: {receivedDatagram.Buffer} from {receivedDatagram.RemoteEndPoint}");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }
    }

    // Tuple 
    // (float x, float y, float z)[] jointPositions = new (float x, float y, float z)[25];

    
    // byte[4]     byte[2] byte[1]  byte[+]
    // {ip         port}   msgType  Msg
    // C0-A8-1F-E6 DC-FF   04       00-00-00-00-00..  
    private void SendMessageToUsers_UDP(MsgType_UDP msgType, byte[] msg)
    {

            if (numberOfClientsInSession > 1) // We already get other Clients IPs from the Server (>0) and we are not alone in the Session (>1)
            {
                //            EP                                     msgType                              Msg
                byte[] data = WAN_Reciever_UDP_EndPoint_Bytes.Concat(new byte[] { (byte)msgType }).Concat(msg).ToArray();

                if (currentP2PNETMode == Session_P2P_NET_Mode.NAT)
                {
                    lock (WAN_Clients_UDP_EP)
                    {
                        foreach (IPEndPoint endPoint in WAN_Clients_UDP_EP)
                        {
                            lock (udpClient)
                            {
                                udpClient.Send(data, data.Length, endPoint);
                            }
                            Debug.Log("WAN UDP sent to: " + endPoint);
                        }
                    }
                    //     EP                                     msgType                              Msg
                    data = LAN_Reciever_UDP_EndPoint_Bytes.Concat(new byte[] { (byte)msgType }).Concat(msg).ToArray();

                    lock (LAN_Clients_UDP_EP)
                    {
                        foreach (IPEndPoint endPoint in LAN_Clients_UDP_EP)
                        {
                            lock (udpClient)
                            {
                                udpClient.Send(data, data.Length, endPoint);
                            }
                            Debug.Log("LAN UDP sent to: " + endPoint);
                        }
                    }
                }

                if(currentP2PNETMode == Session_P2P_NET_Mode.Server)
                {
                    SendMessageToServer_UDP(MsgType_UDP.TURNServer, data);
                }

                if (msgType == MsgType_UDP.AudioSample) sentAudioSamples++;
            }
    }

    // byte[4]     byte[2] byte[1]  byte[+]
    // {ip         port}   msgType  Msg
    // C0-A8-1F-E6 DC-FF   04       00-00-00-00-00..  
    private void SendMessageToServer_UDP(MsgType_UDP msgType, byte[] msg)
    {
        //            EP                                     msgType                            Msg
        byte[] data = WAN_Reciever_UDP_EndPoint_Bytes.Concat(new byte[] { (byte)msgType }).Concat(msg).ToArray();

        lock (udpClient)
        {
            udpClient.Send(data, data.Length, serverEP_UDP);
        }
    }

    // UDP Ping
    private async Task SendAliveMsgAsync_UDP()
    {
        while (programActive)
        {
            if (cts_onExitFromServer.Token.IsCancellationRequested) return;

            SendMessageToServer_UDP(MsgType_UDP.Ping, new byte[0]);
            Debug.Log("UDP Ping to Server sent");

            if (currentP2PNETMode == Session_P2P_NET_Mode.NAT)
            {
                try
                {
                    if (connectedToSession)
                    {
                        SendMessageToUsers_UDP(MsgType_UDP.Ping, new byte[0]);
                        Debug.Log("UDP Ping to WAN Users sent");
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
            sentMsgs++;
            await Task.Delay(AliveMsgPeriod_Ms);
        }
    }

    // UDP Send Model Points (Ulong id + jPs)
    public void SendModelPoints((float x, float y, float z)[] jPs, ulong modelID)
    {
        byte[] modelID_Byte = BitConverter.GetBytes(modelID);
        //Debug.Log($"midelID HEX: {BitConverter.ToString(modelID_Byte)}");

        byte[] jPs_Byte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jPs));
        //Debug.Log($"jPs HEX: {BitConverter.ToString(jPs_Byte)}");

        byte[] data = modelID_Byte.Concat(jPs_Byte).ToArray();

        SendMessageToUsers_UDP(MsgType_UDP.OnlineModelPoints, data);
    }

    // UDP Send Mic Audio
    public void SendMicAudio(byte[] audioSample)
    {
        byte[] msg = client_ID_Length_Byte.Concat(client_ID_Byte).Concat(audioSample).ToArray();
        if (connectedToSession) SendMessageToUsers_UDP(MsgType_UDP.AudioSample, msg);
    }

    int numberOfLANConnectionRequestsSent = 0;
    private async Task SearchClientsLAN()
    {
        try
        {
            while (numberOfLANConnectionRequestsSent <= 1)
            {
                //            EP                                     msgType                                                                                        
                byte[] data = LAN_Reciever_UDP_EndPoint_Bytes.Concat(new byte[] { (byte)MsgType_UDP.SearchClients_LAN_Request }).ToArray();

                lock (udpClient)
                {
                    foreach (IPEndPoint ep in WAN_Clients_UDP_EP)
                    {
                        udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, ep.Port));
                    }
                    foreach (IPEndPoint ep in LAN_Clients_UDP_EP)
                    {
                        udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, ep.Port));
                    }
                }
                Debug.Log($"SearchClientsLAN(): {numberOfLANConnectionRequestsSent++}");
                await Task.Delay(500);
            }
            numberOfLANConnectionRequestsSent = 0;
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    [SerializeField] InputField inputField;

    public void DebugSend()
    {
        string[] ep = inputField.text.Split(':');
        IPAddress serverIP = IPAddress.Parse(ep[0]);
        int serverPort = Int32.Parse(ep[1]);
        IPEndPoint DebugClient = new IPEndPoint(serverIP, serverPort);

        // _ = DebugSendAsync(DebugClient);

        byte[] data = Encoding.UTF8.GetBytes($"Hi Sync, thread {System.Environment.CurrentManagedThreadId}");
        lock (udpClient)
        {
            udpClient.Send(data, data.Length, DebugClient);
        }

        Debug.Log("Debug msg sent to: " + DebugClient);
    }

    async Task DebugSendAsync(IPEndPoint DebugClient)
    {
        byte[] data = Encoding.UTF8.GetBytes($"Hi Async, thread {System.Environment.CurrentManagedThreadId}");
        await udpClient.SendAsync(data, data.Length, DebugClient);

        data = Encoding.UTF8.GetBytes($"Hi Sync, thread {System.Environment.CurrentManagedThreadId}");
        lock (udpClient)
        {
            udpClient.Send(data, data.Length, DebugClient);
        }
    }


    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //                                                                                                      TCP
    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    // TCP Reader
    async Task ReceiveMessageAsync_TCP()
    {
        while (connectedToServer_TCP && programActive)
        {
            if (cts_onExitFromServer.Token.IsCancellationRequested) return;
            try
            {
                
                string? receivedMessage = await TCP_Reader.ReadLineAsync();
                if (string.IsNullOrEmpty(receivedMessage)) continue;
                string[] messageCollection = receivedMessage.Split(new string[] { MSGSplitter }, StringSplitOptions.None);


                // Received Broadcast from Session to Clients => AddModel
                if (messageCollection[0] == MsgType_TCP.Broadcast_SessionToClients_AddModel.ToString())
                {
                    lock (onlineModelsManager.locker_onlineModelIDsToSpawn)
                    {
                        onlineModelsManager.onlineModelIDsToSpawn.Add(ulong.Parse(messageCollection[1]));
                    }
                    continue;
                }

                // Received Broadcast from Session to Clients => DeleteModel
                if (messageCollection[0] == MsgType_TCP.Broadcast_SessionToClients_DeleteModel.ToString())
                {
                    onlineModelsManager.onlineModelIDsToDelete.Add(ulong.Parse(messageCollection[1]));
                    continue;
                }

                // Received Broadcast from Session to Clients => all ClientUDPEndPoints
                if (messageCollection[0] == MsgType_TCP.Broadcast_SessionToClients_ClientUDPEndPoints.ToString())
                {
                    Debug.Log("Client UDP EndPoints received!"); //вывод сообщения
                    try
                    {
                        List<string> All_WAN_Clients_UDP_EP_String = JsonConvert.DeserializeObject<List<string>>(messageCollection[1]);
                        numberOfClientsInSession = 0;

                        lock (LAN_Clients_UDP_EP)
                        {
                            LAN_Clients_UDP_EP.Clear();
                        }
                        lock (WAN_Clients_UDP_EP)
                        {
                            WAN_Clients_UDP_EP.Clear();
                            foreach (string str in All_WAN_Clients_UDP_EP_String)
                            {
                                numberOfClientsInSession++;
                                // str: 192.168.31.70:5555
                                // ep:  [0]           [1]
                                string[] epString = str.Split(':');
                                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(epString[0]), Int32.Parse(epString[1]));
                                if (ep.Port == WAN_Reciever_UDP_EndPoint.Port && ep.Address.Equals(WAN_Reciever_UDP_EndPoint.Address)) continue; // Dont Add yourself

                                WAN_Clients_UDP_EP.Add(ep);
                            }
                        }
                        receivedMsgs = sentMsgs = receivedAudioSamples = sentAudioSamples = 0;
                        _ = Task.Run(SearchClientsLAN);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message);
                    }
                    continue;
                    
                }

                if (messageCollection[0] == MsgType_TCP.Response_ServerToClient_ClientEndPointUDP.ToString())
                {
                    string[] ep_string  = messageCollection[1].Split(':');
                    IPEndPoint WAN_Reciever_UDP_EndPoint_FromServer = new IPEndPoint(IPAddress.Parse(ep_string[0]), Int32.Parse(ep_string[1]));

                    if (WAN_Reciever_UDP_EndPoint.Port == WAN_Reciever_UDP_EndPoint_FromServer.Port 
                        && WAN_Reciever_UDP_EndPoint.Address.Equals(WAN_Reciever_UDP_EndPoint_FromServer.Address))
                    {
                        Debug.Log($"Received correct WAN_Reciever_UDP_EndPoint ");
                    } else
                    {
                        Debug.Log("ERROR!!! Received incorrect WAN_Reciever_UDP_EndPoint");
                    }
                    
                    continue;
                }

                if(messageCollection[0] == MsgType_TCP.Response_ServerToClient_GetSessionsListIDs.ToString())
                {
                    Debug.Log($"Sessions received: {messageCollection[1]}");
                    try
                    {
                        sessionsIDs = JsonConvert.DeserializeObject<List<string>>(messageCollection[1]);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message);
                    }
                    continue;
                }

                if(messageCollection[0] == MsgType_TCP.Response_SessionToClient_ConnectToSession.ToString())
                {
                    connectedToSession = bool.Parse(messageCollection[1]);
                    Debug.Log($"Connected to session: {connectedToSession}");
                    continue;
                }

                if (messageCollection[0] == MsgType_TCP.Response_SessionToClient_ChangeP2PNETMode_ToServer.ToString())
                {
                    receivedResponseChangeP2PNETMode_ToServer = true;
                    currentP2PNETMode = Session_P2P_NET_Mode.Server;
                    uiManager.ChangeUI_NETMode_Server_Success();
                    continue;
                }

                if (messageCollection[0] == MsgType_TCP.Response_SessionToClient_ChangeP2PNETMode_ToNAT.ToString())
                {
                    receivedResponseChangeP2PNETMode_ToNAT = true;
                    currentP2PNETMode = Session_P2P_NET_Mode.NAT;
                    uiManager.ChangeUI_NETMode_NAT_Success();
                    continue;
                }

                if (messageCollection[0] == MsgType_TCP.Response_ServerToClient_GetID.ToString())
                {
                    client_ID = messageCollection[1];
                    client_ID_Received = true;
                    Debug.Log($"Received client ID: {client_ID}");
                    client_ID_Length_Byte = BitConverter.GetBytes(client_ID.Length);
                    client_ID_Byte = Encoding.UTF8.GetBytes(client_ID);

                    continue;
                }

                

                if (messageCollection[0] == MsgType_TCP.Response_ServerToClient_UDP_ConnectionState.ToString())
                {
                    connectedToServer_UDP = bool.TryParse(messageCollection[1], out bool result);
                    continue;
                }

                // If its unknown message
                Debug.Log($"Unknown message: {receivedMessage}");
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                uiManager.DisconnectError(ex.Message);
                DisconnectFromServer();
                break;
            }
        }
    }

    // TCP Writer
    private void SendMessageToServer_TCP(MsgType_TCP type, string msg = "")
    {
        lock (TCP_Writer)
        {
            TCP_Writer.WriteLine(type + MSGSplitter + msg);
        }
    }

    // TCP Ping
    private async Task SendAliveMsgAsync_TCP()
    {
        while (connectedToServer_TCP && programActive)
        {
            if (cts_onExitFromServer.Token.IsCancellationRequested) return;

            SendMessageToServer_TCP(MsgType_TCP.Ping, "ping");
            Debug.Log("TCP Ping to Server sent");
            await Task.Delay(AliveMsgPeriod_Ms);
        }
    }

    // TCP Send EP to Server
    public void Send_Public_UDP_EP_ToServer(IPEndPoint publicEP)
    {
        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToServer_AddPublicUDP_EP, publicEP.ToString());
    }

    // TCP Get List<SessionsIDs>
    public void RequestListOfSessionsIDs()
    {
        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToServer_GetSessionsListIDs);
    }

    // TCP Request to Connect to the Session
    public void RequestConnectClientToSession(string sessionID)
    {
        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToSession_ConnectToSession, sessionID);

        while (!connectedToSession){ } //Wait for Connection to Session

        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToSession_CurrentSessionP2PNETMode);

        while (!(receivedResponseChangeP2PNETMode_ToServer || receivedResponseChangeP2PNETMode_ToNAT)) { } //Wait for geting Session P2PNETMode
        receivedResponseChangeP2PNETMode_ToServer = false;
        receivedResponseChangeP2PNETMode_ToNAT = false;
    }

    // TCP Send ModelID to Session To Create It
    public void RequestAddModelToSession(ulong modelId)
    {
        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToSession_AddModel, modelId.ToString());
    }

    // TCP Send ModelID to Session To Delete It
    public void RequestDeleteModelFromSession(ulong modelId)
    {
        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToSession_DeleteModel, modelId.ToString());
    }

    // TODO Add Server part of this  
    public async Task SendRequestToChangeP2PNETMode_ToServer()
    {
        if (currentP2PNETMode == Session_P2P_NET_Mode.Server) return;

        receivedMsgs = sentMsgs = receivedAudioSamples = sentAudioSamples = 0;

        uiManager.ChangeUI_NETMode_Server_Connection();
        receivedResponseChangeP2PNETMode_ToServer = false;
        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToSession_ChangeP2PNETMode_ToServer);
        bool changedToServer = false;

        await Task.Run(() => {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            while (!receivedResponseChangeP2PNETMode_ToServer) { if (cts.IsCancellationRequested) return; }

            changedToServer = true;
        });

        if (changedToServer)
        {
            currentP2PNETMode = Session_P2P_NET_Mode.Server;
            uiManager.ChangeUI_NETMode_Server_Success();
        }
        else
        {
            uiManager.ChangeUI_NETMode_Server_Fail();
        }
        receivedResponseChangeP2PNETMode_ToServer = false;
    }

    public async Task SendRequestToChangeP2PNETMode_ToNAT()
    {
        if (currentP2PNETMode == Session_P2P_NET_Mode.NAT) return;

        receivedMsgs = sentMsgs = receivedAudioSamples = sentAudioSamples = 0;

        uiManager.ChangeUI_NETMode_NAT_Connection();
        receivedResponseChangeP2PNETMode_ToNAT = false;
        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToSession_ChangeP2PNETMode_ToNAT);
        bool changedToNAT = false;

        await Task.Run(() => {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(5000);
            while (!receivedResponseChangeP2PNETMode_ToNAT) { if (cts.IsCancellationRequested) return; }

            changedToNAT = true;
        });

        if (changedToNAT)
        {
            currentP2PNETMode = Session_P2P_NET_Mode.NAT;
            uiManager.ChangeUI_NETMode_NAT_Success();
        }
        else
        {
            uiManager.ChangeUI_NETMode_NAT_Fail();
        }
        receivedResponseChangeP2PNETMode_ToNAT = false;
    }


    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //                                                                                                  Non-Specific
    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


    public void DisconnectFromServer()
    {
        SendMessageToServer_TCP(MsgType_TCP.Request_ClientToServer_Disconect);
        connectedToServer_TCP = false;
        connectedToServer_UDP = false;
        connectedToSession = false;
        client_ID_Received = false;

        cts_onExitFromServer.Cancel();
        uiManager.ChangeUI_NETMode_None();

        TCP_Reader.Close();
        TCP_Writer.Close();
        tcpClient.Close();
        udpClient.Close();
    }

    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    //                                                                                                      App
    // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

    void OnDisable()
    {
        programActive = false;
        if(connectedToServer_TCP) DisconnectFromServer();
    }
}


