using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatClient : MonoBehaviour {

    public const int MAX_PLAYERS = 100;

    int port;
    //public Client.TargetServer targetConnect = Client.TargetServer.local;
    string targetIP;

    private int hostID;

    private int reliableChannel;
    private int unreliableChannel;

    private int connectionID;

    private float connectionTime;
    private bool connected = false;
    private byte error;

    private string playerName;

    public void Connect()
    {
        if (connected)
            return;
        
        port = ServerLibrary.chatPort;
        targetIP = ServerLibrary.Chat;

        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_PLAYERS);

        hostID = NetworkTransport.AddHost(topo, 0);

        connectionID = NetworkTransport.Connect(hostID, targetIP, port, 0, out error);

        connectionTime = Time.time;
        connected = true;
    }

    public void SendMessage()
    {
        if (!connected) { Connect(); return; }

        byte[] message = Encoding.Unicode.GetBytes("MSG|" +  GameObject.Find("ChatInput").GetComponent<InputField>().text);

        NetworkTransport.Send(hostID, connectionID, reliableChannel, message, message.Length, out error);
    }

    private void Update()
    {
        if (!connected)
            return;
        

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

            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Chat Client Receive: " +  msg);
                string[] incoming = msg.Split('|');

                if (incoming[0] != "MSG")
                    return;
                
                GameObject.Find("ChatBox").GetComponent<Text>().text += "\n" + incoming[1];
                

                break;
        }
    }
}
