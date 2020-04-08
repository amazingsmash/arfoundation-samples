using System;
using System.Net;
using System.Net.Sockets;

public static class NetUtils
{
    public static IPAddress GetMyIP()
    {
        string hostName = Dns.GetHostName(); // Retrive the Name of HOST 
        return Dns.GetHostEntry(hostName).AddressList[0];
    }

    public static Socket OpenServerSocket(int port)
    {
        IPAddress ipAddress = GetMyIP();
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        try
        {
            // Create a Socket that will use Tcp protocol      
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // A Socket must be associated with an endpoint using the Bind method  
            listener.Bind(localEndPoint);
            // Specify how many requests a Socket can listen before it gives Server busy response.  
            // We will listen 10 requests at a time  
            listener.Listen(10);

            Socket socketHandler = listener.Accept();
            return socketHandler;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return null;
        }
    }

    public static Socket OpenClientSocket(IPAddress ipAddress, int port)
    {
        try
        {
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP  socket.    
            Socket sender = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.    
            try
            {
                sender.Connect(remoteEP);
                return sender;
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return null;
        }
    }

    public static void CloseSocket(Socket socket)
    {
        socket.Shutdown(SocketShutdown.Both);
        socket.Close();
    }
}
