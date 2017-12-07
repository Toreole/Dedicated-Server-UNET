using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class ServerClient
{
    public int connectionID;
    public string name;
}

public class Server : MonoBehaviour {

    public enum ServerType
    {
        local, login, game1, chat
    }

    public string versionID = "V.0.1 ALPHA";

    public const int MAX_PLAYERS = 100;

    public ServerType type;
    private int port;

    private int hostID;
    private int webHostID;

    private int reliableChannel;
    private int unreliableChannel;

    private bool started = false;
    private byte error;

    private List<ServerClient> clients = new List<ServerClient>();

    private void Start()
    {
        switch (type)
        {
            case ServerType.local:
                port = ServerLibrary.localPort;
                break;
            case ServerType.login:
                port = ServerLibrary.loginPort;
                break;
            case ServerType.game1:
                port = ServerLibrary.game1Port;
                break;
            case ServerType.chat:
                port = ServerLibrary.chatPort;
                break;
        }

        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_PLAYERS);

        hostID = NetworkTransport.AddHost(topo, port, null);
        //if(type == ServerType.local) webHostID = NetworkTransport.AddWebsocketHost(topo, port, null);
        //Debug.Log(hostID);
        started = true;
        Debug.Log("Server started: " + type.ToString());
    }

    private void Update()
    {
        if (!started)
        {
            //Debug.Log("Server is NOT started");
            return;
        }

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
                Debug.Log( connectionID + " has connected to " + (int) type);
                OnConnection(connectionID);
                break;

            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                OnGetData(msg, connectionID);
                break;

            case NetworkEventType.DisconnectEvent:
                Debug.Log("A Player Has Disconnected!");
                break;
        }
    }

    void OnGetData(string message, int connID)
    {
        string[] sub = message.Split('|');
        //Debug.Log(sub[0]);
        switch (sub[0])
        {
            case "TRNS":
                break;
            case "MSG":
                Send(message, reliableChannel, clients[0].connectionID);
                //Debug.Log("Chat Server recieved message");
                break;
            case "NAME":
                clients.Find(x => x.connectionID == connID).name = sub[1];
                //Debug.Log(sub[1]);
                break;
        }
    }

    void OnConnection(int cn)
    {
        //Add to list
        ServerClient c = new ServerClient();
        c.name = "TEMP";
        c.connectionID = cn;
        clients.Add(c);

        //Tell c ID
        //Get Player Name
        string msg = "ASKNAME|" + cn + "|";
        Send(msg, reliableChannel, cn);
    }

    private void Send(string msg, int channelID, int cnnID)
    {
        //Debug.Log("Send Single");
        List<ServerClient> c = new List<ServerClient>();
        c.Add(clients.Find(x => x.connectionID == cnnID));
        Send(msg, channelID, c);
    }

    private void Send(string msg, int channelID, List<ServerClient> cls)
    {
        //Debug.Log("Send Multiple");
        byte[] message = Encoding.Unicode.GetBytes(msg);
        //Debug.Log(clients[0].connectionID);
        foreach(ServerClient sc in cls) {
            
            NetworkTransport.Send(hostID, sc.connectionID -1, channelID, message, msg.Length * sizeof(char), out error);
        }
    }
}
