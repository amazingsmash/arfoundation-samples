using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public interface ITCPEndListener
{
    void OnStatusChanged(TCPEnd.Status status);
    void OnMessageReceived(byte[] msg);
    void OnStatusMessage(string msg);
}

public abstract class TCPEnd
{
    private const int PACKET_MAX_SIZE = 2048;
    public enum Status
    {
        READY, NOT_READY
    }

    protected readonly int port;
    protected readonly string ip;
    protected readonly ITCPEndListener listener = null;

    public abstract TcpClient OtherEnd { get; }
    public bool IsReady { get; protected set; }

    public TCPEnd(string ip, int port, ITCPEndListener listener)
    {
        this.ip = ip;
        this.port = port;
        this.listener = listener;
    }

    /// <summary>   
    /// Send message to client using socket connection.     
    /// </summary>  
    public void SendMessage(string msg)
    {
        byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(msg);
        SendMessage(serverMessageAsByteArray);
    }

    /// <summary>   
    /// Send message to client using socket connection.     
    /// </summary>  
    public void SendMessage(object msg)
    {
        if (!msg.GetType().IsSerializable)
        {
            listener.OnStatusMessage("Object could not be serialized.");
            return;
        }
        try
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            byte[] serverMessageAsByteArray = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, msg);
                serverMessageAsByteArray = memoryStream.ToArray();
            }
            SendMessage(serverMessageAsByteArray);
        }
        catch (Exception e)
        {
            listener.OnStatusMessage("Object could not be sent:" + e.ToString());
        }
    }

    /// <summary>   
    /// Send binary message to client using socket connection.     
    /// </summary>  
    public void SendMessage(byte[] byteArray)
    {
        if (OtherEnd == null)
        {
            return;
        }

        try
        {
            var encapsulatedMsg = EncapsulateMessage(byteArray);
            // Get a stream object for writing.             
            NetworkStream stream = OtherEnd.GetStream();
            if (stream.CanWrite)
            {
                stream.Write(encapsulatedMsg, 0, encapsulatedMsg.Length);
            }
        }
        catch (SocketException socketException)
        {
            listener.OnStatusMessage("Socket exception: " + socketException);
        }
    }

    private byte[] EncapsulateMessage(byte[] messageLoad)
    {
        int length = messageLoad.Length;
        byte[] messageHeader = BitConverter.GetBytes(length);
        var message = Concat(messageHeader, messageLoad);
        if (message.Length != sizeof(int) + messageLoad.Length) { Console.WriteLine("Problem encoding"); }
        return message;
    }

    public static byte[] Concat(byte[] x, byte[] y)
    {
        var z = new byte[x.Length + y.Length];
        x.CopyTo(z, 0);
        y.CopyTo(z, x.Length);
        return z;
    }

    public static byte[] ReadMessageFromNetworkStreamSync(NetworkStream stream)
    {
        byte[] messageLoad = null;
        //Read first packet
        Byte[] packetBuffer = new Byte[PACKET_MAX_SIZE];
        int packetLength = stream.Read(packetBuffer, 0, packetBuffer.Length);
        if (packetLength < sizeof(int))
        {
            Console.WriteLine("Malformed packet received.");
            return null;
        }
        int messageLength = BitConverter.ToInt32(packetBuffer, 0); //Assuming 4 bytes

        messageLoad = new byte[packetLength - sizeof(int)];
        Array.Copy(packetBuffer, 4, messageLoad, 0, messageLoad.Length);
        int remainingBytes = messageLength - messageLoad.Length;

        //Reading the rest
        while (remainingBytes > 0)
        {
            packetLength = stream.Read(packetBuffer, 0, Math.Min(remainingBytes, packetBuffer.Length));
            if (packetLength > 0)
            {
                var nml = new byte[messageLoad.Length + packetLength];
                messageLoad.CopyTo(nml, 0);
                Array.Copy(packetBuffer, 0, nml, messageLoad.Length, packetLength);
                messageLoad = nml;
                remainingBytes = messageLength - messageLoad.Length;
            }
        }
        return messageLoad;
    }

    public static IPAddress GetFirstLocalIPAddressWithOpenTCPPort(int port)
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                string address = ip.Address.ToString();
                try
                {
                    var conn = new TcpClient(address, port);
                    if (conn.Connected)
                    {
                        conn.Close();
                        return ip.Address;
                    }
                }
                catch (Exception) { }
            }
        }
        return null;
    }


}


public class TCPServer : TCPEnd, IDisposable
{
    #region private members     
    /// <summary>   
    /// TCPListener to listen for incomming TCP connection  
    /// requests.   
    /// </summary>  
    private TcpListener tcpListener;
    /// <summary> 
    /// Background thread for TcpServer workload.   
    /// </summary>  
    private Thread tcpListenerThread;
    /// <summary>   
    /// Create handle to connected tcp client.  
    /// </summary>  
    private TcpClient connectedTcpClient;

    NetworkStream stream;

    public override TcpClient OtherEnd => connectedTcpClient;
    #endregion

    // Use this for initialization
    public TCPServer(string ip, int port, ITCPEndListener listener) :
        base(ip, port, listener)
    {
        // Start TcpServer background thread        
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    /// <summary>   
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests    
    /// </summary>  
    private void ListenForIncommingRequests()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse(ip), port);
            tcpListener.Start();
            IsReady = true;
            listener.OnStatusChanged(Status.READY);

            listener.OnStatusMessage($"Server at {ip}:{port}");

            using (connectedTcpClient = tcpListener.AcceptTcpClient())
            {
                using (stream = connectedTcpClient.GetStream())
                {
                    while (stream.CanRead)
                    {
                        var messageLoad = ReadMessageFromNetworkStreamSync(stream);
                        if (listener != null && messageLoad != null)
                        {
                            listener.OnMessageReceived(messageLoad);
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            listener.OnStatusMessage("SocketException " + socketException.ToString());
        }
    }

    public void Dispose()
    {
        tcpListener.Stop();
        connectedTcpClient.Close();
        stream.Close();
    }

}

public class TCPClient : TCPEnd, IDisposable
{
    #region private members 	
    private TcpClient tcpClient;
    private Thread clientReceiveThread;
    public override TcpClient OtherEnd => tcpClient;
    #endregion

    public bool IsListening
    {
        get { return tcpClient != null; }
    }


    public TCPClient(string ip, int port, ITCPEndListener listener) :
        base(ip, port, listener)
    {
    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    public void ConnectToTCPServer()
    {
        try
        {
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            listener.OnStatusMessage("On client connect exception " + e);
        }
    }
    /// <summary> 	
    /// Runs in background clientReceiveThread; Listens for incomming data. 	
    /// </summary>     
    private void ListenForData()
    {
        try
        {
            tcpClient = new TcpClient(ip, port);
            Byte[] bytes = new Byte[1024];
            IsReady = tcpClient != null;
            listener.OnStatusChanged(Status.READY);

            // Get a stream object for reading 				
            using (NetworkStream stream = tcpClient.GetStream())
            {
                while (true)
                {
                    var messageLoad = ReadMessageFromNetworkStreamSync(stream);
                    if (listener != null && messageLoad != null)
                    {
                        listener.OnMessageReceived(messageLoad);
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            listener.OnStatusMessage("Socket exception: " + socketException);
        }
    }

    public void Dispose()
    {
        tcpClient.Close();
    }
}