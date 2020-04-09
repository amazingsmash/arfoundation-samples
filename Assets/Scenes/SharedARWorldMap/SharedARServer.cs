using System.Text;
using UnityEngine;
using UnityEngine.UI;
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
            byte[] msgBytes = new byte[211094];
            for(int i = 0; i < msgBytes.Length; i++) { msgBytes[i] = (byte)i; }

            NativeArray<byte> natArray = new NativeArray<byte>(msgBytes, Allocator.Persistent);
            byte[] bs = natArray.ToArray();
            server.SendMessage(msgBytes);
            natArray.Dispose();
            Debug.Log("Message sent");
#else
            IEnumerator routine = ARWorldMapController.GetARWorldMapAsync(delegate (ARWorldMap? arWorldMap)
            {
                if (arWorldMap is ARWorldMap wm)
                {
                    try
                    {
                        NativeArray<byte> nativeArray = wm.Serialize(Allocator.Temp);
                        byte[] bytes = nativeArray.ToArray();
                        Debug.Log($"Trying to send ARWM Size: {bytes.Length}");
                        server.SendMessage(bytes);
                        nativeArray.Dispose();
                        Debug.Log("ARWM SENT");
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
        Debug.Log("Server Status:" + status);
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

        SendARWorldMapToClient();
    }

    public void OnStatusMessage(string msg)
    {
        Debug.Log($"Received Status: {msg}");
    }

}
