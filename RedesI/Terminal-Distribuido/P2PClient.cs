using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

public class P2PClient
{
    private static SemaphoreSlim Semaphore { get; set; }
    private static TerminalManager TerminalManager { get; set; }
    public static OutgoingPersistentSocket? OutgoingPeer { get; set; }
    public static ConcurrentDictionary<string, IncomingPersistentSocket> IncomingPeers { get; set; }
    public static IPAddress ClientIpAddress { get; private set; }

    public static int Main(String[] args)
    {
        InitializeP2PClient();

        InitiateOutgoingSocketConnection();
        ListenForIncomingSocketConnections();

        while (true)
        {
            string? message = Console.ReadLine();

            if (String.IsNullOrEmpty(message)) { 
                continue;
            }
            string commandResult = TerminalManager.ExecuteCommand(message);


            Console.WriteLine("Source system result");
            Console.WriteLine($"{commandResult}\n");

            // Propagate data to all sockets
            PropagateOutgoingMessage(message);
        }
    }

    private static void InitializeP2PClient() 
    {
        ClientIpAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
        TerminalManager = new TerminalManager();
        Semaphore = new SemaphoreSlim(1);
        IncomingPeers = new ConcurrentDictionary<string, IncomingPersistentSocket>();
    }

    public static void InitiateOutgoingSocketConnection()
    {
        Console.WriteLine("Provide target environment IP address");
        string? targetEnvironment = Console.ReadLine();

        Console.WriteLine("Provide target environment port");
        string? targetPort = Console.ReadLine();

        StartClient(targetEnvironment, targetPort);
    }

    public static void ListenForIncomingSocketConnections()
    {
        Console.WriteLine("Provide this server port for socket server");
        string? serverPort = Console.ReadLine();

        Thread maintainedSocketThread = new Thread(() => StartServer(serverPort));
        maintainedSocketThread.Start();
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

                OutgoingPersistentSocket persistent = new OutgoingPersistentSocket(
                    sender, 
                    ipAddress, 
                    PropagateIncomingSocketMessageDelegate, 
                    HandleResponseDelegate, 
                    TerminalManager);

                persistent.CreateMonitoringThread();
                OutgoingPeer = persistent;
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
        if (String.IsNullOrEmpty(serverPort) || !int.TryParse(serverPort, out port))
        {
            Console.WriteLine("Server port not provided. Interrupting flow.");
            return;
        }

        IPEndPoint localEndPoint = new IPEndPoint(ClientIpAddress, port);

        try
        {
            // Create a Socket that will use Tcp protocol
            Socket listener = new Socket(ClientIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // A Socket must be associated with an endpoint using the Bind method

            listener.Bind(localEndPoint);
            listener.Listen(10); // We will listen 10 requests at a time

            Console.WriteLine("Started listening for requests");
            Socket socket;

            while (true)
            {
                socket = listener.Accept();

                IPAddress sourceIp = IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString());
                Console.WriteLine("\nReceived connection request from {0}", sourceIp.ToString());

                IncomingPersistentSocket persistentSocket  = new IncomingPersistentSocket(
                    socket,
                    ClientIpAddress,
                    PropagateIncomingSocketMessageDelegate,
                    HandleResponseDelegate,
                    TerminalManager);

                persistentSocket.CreateMonitoringThread();
                IncomingPeers.TryAdd(sourceIp.ToString(), persistentSocket);

                NetworkSynzhronizationRequestProtocol networkSynzhronizationRequest 
                    = new NetworkSynzhronizationRequestProtocol(ClientIpAddress.ToString());

                socket.Send(ProtocolConverter<NetworkSynzhronizationRequestProtocol>.ConvertPayloadToByteArray(networkSynzhronizationRequest));
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
    public static void PropagateOutgoingMessage(string command)
    {
        try
        {
            Semaphore.Wait();

            foreach (var clientEntry in IncomingPeers)
            {
                CommandRequestProtocol commandRequest = 
                    new CommandRequestProtocol(ClientIpAddress.ToString(), clientEntry.Value.Address.ToString(), command, false);

                clientEntry.Value.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(commandRequest));
            }

            if (OutgoingPeer != null)
            {
                CommandRequestProtocol commandRequest = 
                    new CommandRequestProtocol(ClientIpAddress.ToString(), OutgoingPeer.Address.ToString(), command, false);

                OutgoingPeer.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(commandRequest));
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public static void HandleResponseDelegate(CommandRequestProtocol response)
    {
        try
        {
            Semaphore.Wait();

            string targetAddress = response.AddressStack.Pop();
            string? outgoingPeerAddress = OutgoingPeer?.Address?.ToString();

            if (OutgoingPeer != null &&
                outgoingPeerAddress != null &&
                outgoingPeerAddress.Equals(targetAddress.ToString()))
            {
                OutgoingPeer.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(response));
                return;
            }

            foreach (var incomingPeer in IncomingPeers)
            {
                string? incomingPeerAddress = incomingPeer.Value.Address.ToString();

                if (incomingPeerAddress.Equals(targetAddress.ToString()))
                {
                    incomingPeer.Value.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(response));
                    return;
                }
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public static void PropagateIncomingSocketMessageDelegate(byte[] message, IPAddress address)
    {
        try
        {
            Semaphore.Wait();

            foreach (var incomingPeer in IncomingPeers)
            {
                string? incomingPeerAddress = incomingPeer.Value.Address.ToString();

                if (!incomingPeerAddress.Equals(address.ToString()))
                {
                    incomingPeer.Value.SendMessage(message);
                }
            }

            string? outgoingPeerAddress = OutgoingPeer?.Address?.ToString();

            if (OutgoingPeer != null &&
                outgoingPeerAddress != null &&
                !outgoingPeerAddress.Equals(address.ToString()))
            {
                OutgoingPeer.SendMessage(message);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}