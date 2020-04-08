using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARKit;
using Unity.Collections;

public class SharedARServer : MonoBehaviour, ITCPEndListener
{
    public Text screenMessageText;

    public const int PORT = 4242;
    TCPServer server;

    public ARWorldMapController ARWorldMapController = null;

    // Start is called before the first frame update
    void Start()
    {
        var myIP = NetUtils.GetMyIP();
        screenMessageText.text = myIP.ToString();
        server = new TCPServer("127.0.0.1", PORT, this);
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void SendARWorldMapToClient()
    {
        if (server.IsReady && ARWorldMapController != null)
        {
            //server.SendMessage($"HOLA {Time.time}");

#if UNITY_EDITOR
            byte[] msgBytes = new byte[] { 1, 2, 3, 4 };
            NativeArray<byte> natArray = new NativeArray<byte>(msgBytes, Allocator.Persistent);
            byte[] bs = natArray.ToArray();
            server.SendMessage(msgBytes);
            natArray.Dispose();
#else
            IEnumerator routine = ARWorldMapController.GetARWorldMapAsync(delegate (ARWorldMap? arWorldMap)
            {
                if (arWorldMap is ARWorldMap wm)
                {
                    try
                    {
                        NativeArray<byte> nativeArray = wm.Serialize(Allocator.Temp);
                        byte[] bytes = nativeArray.ToArray();
                        server.SendMessage(bytes);
                        nativeArray.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Problem serializing ARWorldMap. {ex.ToString()}");
                        wm.Dispose();
                    }
                }
                else
                {
                    Debug.LogError("Cannot get ARWorldMap.");
                }
            });
            StartCoroutine(routine);
#endif
        }
    }

    public void OnStatusChanged(TCPEnd.Status status)
    {
        SendARWorldMapToClient();
    }

    public void OnMessageReceived(byte[] msg)
    {
        //Debug.Log($"Received: {msg}");
        //string s = "";
        //foreach (var b in msg)
        //{
        //    s = $"{s}, {b}";
        //}

        var stringMsg = Encoding.ASCII.GetString(msg);
        Debug.Log("Server Received:" + stringMsg);
    }

    public void OnStatusMessage(string msg)
    {
        Debug.Log($"Received Status: {msg}");
    }

}
