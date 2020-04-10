using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.XR.ARKit;
using System;

public class SharedARServer : MonoBehaviour, ITCPEndListener
{
    public const int PORT = 4242;
    TCPServer server = null;

    public ARWorldMapController ARWorldMapController = null;

    // Start is called before the first frame update
    void Start()
    {
        var myIP = NetUtils.GetMyIP();
        string myIPString = myIP.ToString();
        server = new TCPServer(myIPString, PORT, this);
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
            byte[] msgBytes = new byte[211094];
            for(int i = 0; i < msgBytes.Length; i++) { msgBytes[i] = (byte)i; }

            NativeArray<byte> natArray = new NativeArray<byte>(msgBytes, Allocator.Persistent);
            byte[] bs = natArray.ToArray();
            server.SendMessage(msgBytes);
            natArray.Dispose();
            SharedARUIManager.sharedARStatusMessage = "Message sent";
#else
            IEnumerator routine = ARWorldMapController.GetARWorldMapAsync(delegate (ARWorldMap? arWorldMap)
            {
                if (arWorldMap is ARWorldMap wm)
                {
                    try
                    {
                        NativeArray<byte> nativeArray = wm.Serialize(Allocator.Temp);
                        byte[] bytes = nativeArray.ToArray();
                        SharedARUIManager.sharedARStatusMessage = $"Trying to send ARWM Size: {bytes.Length}";
                        server.SendMessage(bytes);
                        nativeArray.Dispose();
                        SharedARUIManager.sharedARStatusMessage = "ARWM SENT";
                    }
                    catch (Exception ex)
                    {
                        SharedARUIManager.sharedARStatusMessage = $"Problem serializing ARWorldMap. {ex.ToString()}";
                        wm.Dispose();
                    }
                }
                else
                {
                    SharedARUIManager.sharedARStatusMessage = "Cannot get ARWorldMap.";
                }
            });
            StartCoroutine(routine);
#endif
        }
    }

    public void OnStatusChanged(TCPEnd.Status status)
    {
        SharedARUIManager.sharedARStatusMessage = "Server Status:" + status;
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
        SharedARUIManager.sharedARStatusMessage = "Server Received:" + stringMsg;

        SendARWorldMapToClient();
    }

    public void OnStatusMessage(string msg)
    {
        SharedARUIManager.sharedARStatusMessage = msg;
    }

}
