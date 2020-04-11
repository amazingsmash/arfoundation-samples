using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using UnityEngine.XR.ARKit;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class SharedARClient : MonoBehaviour, ITCPEndListener
{
    //public string serverIP;
    IPAddress ipAddress;
    TCPClient client;

    public ARWorldMapController ARWorldMapController = null;
    public InputField ipInputField = null;

    // Use this for initialization
    void Start()
    {
        if (ipInputField != null && ipInputField.text != "") {
            ipAddress = IPAddress.Parse(ipInputField.text);
        }
        else
        {
            ipAddress = IPAddress.Parse("127.0.0.1");
        }

        //ipAddress = TCPEnd.GetFirstLocalIPAddressWithOpenTCPPort(SharedARServer.PORT);

        client = new TCPClient(ipAddress.ToString(), SharedARServer.PORT, this);

        client.ConnectToTCPServer();
    }

    public void OnMessageReceived(byte[] msg)
    {
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream serializationStream = new MemoryStream(msg, 0, msg.Length, false, false);
            //serializationStream.Write(msg, 0, msg.Length);
            object obj = bf.Deserialize(serializationStream);
            if (obj != null)
            {
                if (obj is AppState)
                {
                    AppState appState = obj as AppState;
                    Debug.Log("AppState received.");
                }
            }
            return;
        }
        catch (System.Exception)
        {
        }

        //string s = "";
        //foreach (var b in msg)
        //{
        //    s = $"{s}, {b}";
        //}
        //Debug.Log(s);
        SharedARUIManager.sharedARStatusMessage = $"MSG RECEIVED LENGTH: {msg.Length}";
        //ARWorldMap
        if (ARWorldMapController != null)
        {
            NativeArray<byte> nativeArray = new NativeArray<byte>(msg, Allocator.Persistent);
            Debug.Log("Trying to deserialize ARWM");
            bool success = ARWorldMap.TryDeserialize(nativeArray, out ARWorldMap worldMap);
            nativeArray.Dispose();
            if (success)
            {
                Debug.Log("ARWM Deserialized");
                if (worldMap.valid)
                {
                    Debug.Log("ARWM Valid");
                    ARWorldMapController.LoadARWorldMap(worldMap);
                    SharedARUIManager.sharedARStatusMessage = "SUCESS";
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
