using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Sockets;

public class P2PClient
{
    public static OutgoingPersistentSocket? outgoingPeer;
    public static ConcurrentDictionary<string, IncomingPersistentSocket> incomingPeers = new ConcurrentDictionary<string, IncomingPersistentSocket>();

    private static SemaphoreSlim Semaphore = new SemaphoreSlim(1);

    public static int Main(String[] args)
    {
        Console.WriteLine("Provide target environment IP address");
        string? targetEnvironment = Console.ReadLine();

        Console.WriteLine("Provide target environment port");
        string? targetPort = Console.ReadLine();

        StartClient(targetEnvironment, targetPort);

        Console.WriteLine("Provide this server port for socket server");
        string? serverPort = Console.ReadLine();

        StartServer(serverPort);
        return 0;
    }

    public static void StartClient(string? targetEnvironment, string? targetPort)
    {
        int port = 0;
        if (String.IsNullOrEmpty(targetEnvironment) || String.IsNullOrEmpty(targetPort) || !int.TryParse(targetPort, out port)) 
        {
            Console.WriteLine("No target IP or port provided, continuing as client only.");
            return;
        }

        try
        {
            IPAddress ipAddress = IPAddress.Parse(targetEnvironment);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                sender.Connect(remoteEP);

                Console.WriteLine("Socket connected to {0}", sender?.RemoteEndPoint?.ToString());

                OutgoingPersistentSocket persistent = new OutgoingPersistentSocket(sender, ipAddress, HandlePropagation);

                persistent.CreateMonitoringThread();
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
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static void StartServer(string? serverPort)
    {
        int port = 0;
        if (String.IsNullOrEmpty(serverPort) && !int.TryParse(serverPort, out port))
        {
            Console.WriteLine("Server port not provided. Interrupting flow.");
            return;
        }

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = host.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        try
        {
            // Create a Socket that will use Tcp protocol
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // A Socket must be associated with an endpoint using the Bind method

            listener.Bind(localEndPoint);
            listener.Listen(10); // We will listen 10 requests at a time

            Console.WriteLine("Started listening for requests");
            Socket socket;

            while (true)
            {
                socket = listener.Accept();

                Console.WriteLine("\nReceived connection request...");

                IncomingPersistentSocket persistentSocket  = new IncomingPersistentSocket(
                    socket, 
                    IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()),
                    HandlePropagation);

                persistentSocket.CreateMonitoringThread();
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\n Press any key to continue...");
        Console.ReadKey();
    }

    // Create a method for a delegate.
    public static void HandlePropagation(byte[] message)
    {
        try
        {
            Semaphore.Wait();

            Console.WriteLine(message);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}