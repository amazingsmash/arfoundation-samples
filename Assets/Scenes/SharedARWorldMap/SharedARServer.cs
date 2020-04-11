using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.XR.ARKit;
using System;


[Serializable]
public struct SVector3
{
    public float x, y, z;

    public SVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static implicit operator Vector3(SVector3 v) => new Vector3(v.x, v.y, v.z);
    public static implicit operator SVector3(Vector3 v) => new SVector3(v.x, v.y, v.z);
};

[Serializable]
public class AppState
{
    public List<SVector3> positions = new List<SVector3>();

    public static AppState Main = new AppState();
}

public class SharedARServer : MonoBehaviour, ITCPEndListener
{
    public const int PORT = 4242;
    TCPServer server = null;
    public bool useLocalhostLoop = false;

    public ARWorldMapController ARWorldMapController = null;

    // Start is called before the first frame update
    void Start()
    {
        var myIP = NetUtils.GetMyIP();
        string myIPString = useLocalhostLoop? "127.0.0.1" : myIP.ToString();
        server = new TCPServer(myIPString, PORT, this);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % 100 == 0)
        {
            SendARWorldMapToClient();
        }

        if (Time.frameCount % 50 == 0)
        {
            SendAppStateToClient();
        }
    }

    private void SendAppStateToClient()
    {
        if (server.IsReady && ARWorldMapController != null)
        {
            AppState.Main.positions.Add(new Vector3(1, 1, 1));
            AppState.Main.positions.Add(new Vector3(2, 2, 2));

            server.SendMessage(AppState.Main);
            AppState.Main.positions.Add(new Vector3(3, 3, 3));

            server.SendMessage(AppState.Main);
            SharedARUIManager.sharedARStatusMessage = "AppState sent";
        }
    }


    private void SendARWorldMapToClient()
    {
        if (server.IsReady && ARWorldMapController != null)
        {
#if UNITY_EDITOR
            byte[] msgBytes = new byte[211094];
            for (int i = 0; i < msgBytes.Length; i++) { msgBytes[i] = (byte)i; }

            NativeArray<byte> natArray = new NativeArray<byte>(msgBytes, Allocator.Persistent);
            byte[] bs = natArray.ToArray();
            server.SendMessage(msgBytes);
            natArray.Dispose();
            SharedARUIManager.sharedARStatusMessage = "ARWorldMap sent";
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
                        SharedARUIManager.sharedARStatusMessage = "ARWorldMap sent";
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
