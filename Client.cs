using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Client : MonoBehaviour {

    public enum TargetServer
    {
        local, login, game1, chat
    }

    public string versionID = "V.0.1 ALPHA";

    public const int MAX_PLAYERS = 100;

    int port = 6666;
    public TargetServer targetConnect = TargetServer.local;
    string targetIP = "127.0.0.1";

    private int hostID;

    private int reliableChannel;
    private int unreliableChannel;

    private int connectionID;
    private int clientID;

    private float connectionTime;
    private bool connected = false;
    private byte error;

    private string playerName;

    public void Connect()
    {
        if (connected)
            return;

        string pName = GameObject.Find("InputField").GetComponent<InputField>().text;
        if (pName == "")
        {
            Debug.Log("Enter a name!");
            return;
        }

        switch (targetConnect)
        {
            case TargetServer.local:
                port = ServerLibrary.localPort;
                targetIP = ServerLibrary.local;
                break;
            case TargetServer.game1:
                port = ServerLibrary.game1Port;
                targetIP = ServerLibrary.Game1;
                break;
            case TargetServer.chat:
                port = ServerLibrary.chatPort;
                targetIP = ServerLibrary.Chat;
                break;
            case TargetServer.login:
                port = ServerLibrary.loginPort;
                targetIP = ServerLibrary.Login;
                break;
        }

        playerName = pName;

        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_PLAYERS);

        hostID = NetworkTransport.AddHost(topo);

        connectionID = NetworkTransport.Connect(hostID, targetIP, port, 0, out error);
        
        connectionTime = Time.time;
        connected = true;

        if (!connected) Debug.Log("Couldnt connect");
    }

    private void Update()
    {
        if (!connected)
        {
            //Debug.Log("Server is NOT started");
            return;
        }
        Debug.Log(clientID + " - " + this.connectionID);

        int recHostID;
        int connectionID;
        int channelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostID, out connectionID, out channelID, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connected");
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Received: " + msg);
                OnGetData(msg);
                break;
        }
    }

    void Send(string message, int channelID)
    {
        byte[] msg = Encoding.Unicode.GetBytes(message);
        NetworkTransport.Send(hostID, connectionID, channelID, msg, message.Length * sizeof(char), out error);
    }

    void OnGetData(string msg)
    {
        string[] sub = msg.Split('|');
        switch (sub[0])
        {
            case "ASKNAME":
                clientID = int.Parse(sub[1]);
                Send("NAME|" + playerName, reliableChannel);
                Debug.Log("ASKNAME");


                break;
        }
    }

}
