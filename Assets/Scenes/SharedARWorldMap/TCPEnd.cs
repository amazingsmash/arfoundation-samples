using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Net;

public interface ITCPEndListener
{
    void OnStatusChanged(TCPEnd.Status status);
    void OnMessageReceived(byte[] msg);
    void OnStatusMessage(string msg);
}

public abstract class TCPEnd
{
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
            // Get a stream object for writing.             
            NetworkStream stream = OtherEnd.GetStream();
            if (stream.CanWrite)
            {
                stream.Write(byteArray, 0, byteArray.Length);
                listener.OnStatusMessage("Server sent his message - should be received by client");
            }
        }
        catch (SocketException socketException)
        {
            listener.OnStatusMessage("Socket exception: " + socketException);
        }
    }
}


public class TCPServer: TCPEnd
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

    public override TcpClient OtherEnd => connectedTcpClient;
    #endregion

    // Use this for initialization
    public TCPServer(string ip, int port, ITCPEndListener listener):
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
            // Create listener on localhost port 8052.          
            tcpListener = new TcpListener(IPAddress.Parse(ip), port);
            tcpListener.Start();
            IsReady = true;
            listener.OnStatusChanged(Status.READY);

            listener.OnStatusMessage("Server is listening");
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    // Get a stream object for reading                  
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        int length;
                        // Read incomming stream into byte arrary.                      
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            // Convert byte array to string message.                            
                            //string clientMessage = Encoding.ASCII.GetString(incommingData);
                            //Debug.Log("client message received as: " + clientMessage); 

                            if (listener != null)
                            {
                                listener.OnMessageReceived(incommingData);
                            }
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
}

public class TCPClient : TCPEnd
{  	
	#region private members 	
	private TcpClient socketConnection; 	
	private Thread clientReceiveThread;
    public override TcpClient OtherEnd => socketConnection;
    #endregion

    public bool IsListening
    {
        get { return socketConnection != null; }
    }


    public TCPClient(string ip, int port, ITCPEndListener listener) :
        base(ip, port, listener)
    {
    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    public void ConnectToTCPServer () { 		
		try {  			
			clientReceiveThread = new Thread (new ThreadStart(ListenForData)); 			
			clientReceiveThread.IsBackground = true; 			
			clientReceiveThread.Start();  		
		} 		
		catch (Exception e) {
            listener.OnStatusMessage("On client connect exception " + e); 		
		} 	
	}  	
	/// <summary> 	
	/// Runs in background clientReceiveThread; Listens for incomming data. 	
	/// </summary>     
	private void ListenForData() { 		
		try { 			
			socketConnection = new TcpClient(ip, port);  			
			Byte[] bytes = new Byte[1024];
            IsReady = socketConnection != null;
            while (true) { 				
				// Get a stream object for reading 				
				using (NetworkStream stream = socketConnection.GetStream()) { 					
					int length; 					
					// Read incomming stream into byte arrary. 					
					while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) { 						
						var incommingData = new byte[length]; 						
						Array.Copy(bytes, 0, incommingData, 0, length); 						
						// Convert byte array to string message. 						
						//string serverMessage = Encoding.ASCII.GetString(incommingData);
                        if (listener != null)
                        {
                            listener.OnMessageReceived(incommingData);
                        }				
					} 				
				} 			
			}
        }         
		catch (SocketException socketException) {
            listener.OnStatusMessage("Socket exception: " + socketException);         
		}     
	}
}