using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using UnityEngine.XR.ARKit;
using Unity.Collections;
using UnityEngine.UI;

public class SharedARClient : MonoBehaviour, ITCPEndListener
{
    public string serverIP;
    IPAddress ipAddress;
    Socket socketHandler;
    public Text screenMessageText;

    TCPClient client;

    public ARWorldMapController ARWorldMapController = null;

    // Use this for initialization
    void Start()
    {
        ipAddress = IPAddress.Parse(serverIP);
        //socketHandler = NetUtils.OpenClientSocket(ipAddress, SharedARServer.PORT);
        //if (socketHandler != null)
        //{
        //    Debug.Log("Client listening.");
        //}


        client = new TCPClient("127.0.0.1", SharedARServer.PORT, this);

        client.ConnectToTCPServer();

        //client.SendMessage("READY");
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void OnMessageReceived(byte[] msg)
    {
        //string s = "";
        //foreach (var b in msg)
        //{
        //    s = $"{s}, {b}";
        //}
        //Debug.Log(s);
        screenMessageText.text = $"MSG RECEIVED LENGTH: {msg.Length}";
        //ARWorldMap
        if (ARWorldMapController != null)
        {
            NativeArray<byte> nativeArray = new NativeArray<byte>(msg, Allocator.Temp);
            bool success = ARWorldMap.TryDeserialize(nativeArray, out ARWorldMap worldMap);
            if (success)
            {
                if (worldMap.valid)
                {
                    ARWorldMapController.LoadARWorldMap(worldMap);
                    screenMessageText.text = "SUCESS";
                }
            }
        }
    }

    public void OnStatusChanged(TCPEnd.Status status)
    {
        Debug.Log(status);
    }

    public void OnStatusMessage(string msg)
    {
        Debug.Log($"Received Status: {msg}");
    }

}
