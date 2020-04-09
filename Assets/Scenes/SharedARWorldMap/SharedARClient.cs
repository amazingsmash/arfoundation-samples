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

    private string screenString = "";
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
        if (screenString != screenMessageText.text)
        {
            screenMessageText.text = screenString;
        }
    }


    public void OnMessageReceived(byte[] msg)
    {
        //string s = "";
        //foreach (var b in msg)
        //{
        //    s = $"{s}, {b}";
        //}
        //Debug.Log(s);
        screenString = $"MSG RECEIVED LENGTH: {msg.Length}";
        Debug.Log(screenString);
        //ARWorldMap
        if (ARWorldMapController != null)
        {
            Debug.Log("Trying to deserialize ARWM 1");
            NativeArray<byte> nativeArray = new NativeArray<byte>(msg, Allocator.Persistent);
            Debug.Log("Trying to deserialize ARWM 2");
            bool success = ARWorldMap.TryDeserialize(nativeArray, out ARWorldMap worldMap);
            Debug.Log("Trying to deserialize ARWM 3");
            nativeArray.Dispose();
            Debug.Log("Trying to deserialize ARWM 4");
            if (success)
            {
                Debug.Log("ARWM Deserialized");
                Debug.Log("Trying to deserialize ARWM 5");
                if (worldMap.valid)
                {
                    Debug.Log("Trying to deserialize ARWM 6");
                    ARWorldMapController.LoadARWorldMap(worldMap);
                    Debug.Log("Trying to deserialize ARWM 7");
                    screenString = "SUCESS";
                }
            }
        }
    }

    public void OnStatusChanged(TCPEnd.Status status)
    {
        Debug.Log(status);
        if (status == TCPEnd.Status.READY)
        {
            client.SendMessage("SEND ME DATA");
        }
    }

    public void OnStatusMessage(string msg)
    {
        Debug.Log($"Received Status: {msg}");
    }

}
