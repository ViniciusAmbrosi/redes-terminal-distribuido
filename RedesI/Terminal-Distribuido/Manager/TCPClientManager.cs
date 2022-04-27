using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Manager;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

public class TCPClientManager
{
    private TerminalManager TerminalManager { get; set; }
    private OutgoingPersistentSocket? OutgoingPeer { get; set; }
    private ConcurrentDictionary<string, IncomingPersistentSocket> IncomingPeers { get; set; }
    private IPAddress ClientIpAddress { get; set; }
    private RequestManager RequestManager { get; set; }

    public TCPClientManager()
    {
        this.TerminalManager = new TerminalManager();

        this.IncomingPeers = new ConcurrentDictionary<string, IncomingPersistentSocket>();
        
        this.ClientIpAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];

        this.RequestManager = new RequestManager(HandleResponseDelegate, PropagateToKnownPeersWithoutLoop, TerminalManager);
    }

    public void ManageTerminalInput() {
        while (true)
        {
            string? message = Console.ReadLine();

            if (!String.IsNullOrEmpty(message))
            {
                string commandResult = TerminalManager.ExecuteCommand(message);

                Console.WriteLine("\n-- -- -- -- -- -- -- -- -- -- -- -- --");
                Console.WriteLine("\nSource system result");
                Console.WriteLine($"{commandResult}\n");

                // Propagate data to all sockets
                PropagateCommandToAllPeers(message);
            }
        }
    }

    public void InitiateOutgoingSocketConnection()
    {
        Console.WriteLine("Provide target environment IP address");
        string? targetEnvironment = Console.ReadLine();

        Console.WriteLine("Provide target environment port");
        string? targetPort = Console.ReadLine();

        StartClient(targetEnvironment, targetPort);
    }

    public void ListenForIncomingSocketConnections()
    {
        Console.WriteLine("Provide this server port for socket server");
        string? serverPort = Console.ReadLine();

        Thread maintainedSocketThread = new Thread(() => StartServer(serverPort));
        maintainedSocketThread.Start();
    }

    public void StartClient(string? targetEnvironment, string? targetPort)
    {
        int port = 0;

        if (String.IsNullOrEmpty(targetEnvironment) || 
            String.IsNullOrEmpty(targetPort) || 
            !int.TryParse(targetPort, out port)) 
        {
            Console.WriteLine("No target IP or port provided, continuing as client only.");
            return;
        }

        try
        {
            IPAddress ipAddress = IPAddress.Parse(targetEnvironment);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(remoteEP);

            if (sender != null)
            {
                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint?.ToString());

                OutgoingPersistentSocket persistent = new OutgoingPersistentSocket(
                    sender,
                    ipAddress,
                    RequestManager);

                persistent.CreateMonitoringThread();
                OutgoingPeer = persistent;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public void StartServer(string? serverPort)
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
            Socket listener = new Socket(ClientIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(localEndPoint);
            listener.Listen(1);

            Console.WriteLine("Started listening for requests");
            Socket socket;

            while (true)
            {
                socket = listener.Accept();

                IPAddress sourceIp = IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString());
                Console.WriteLine("\nReceived connection request from {0}", sourceIp.ToString());

                IncomingPersistentSocket persistentSocket  = new IncomingPersistentSocket(
                    socket,
                    sourceIp,
                    RequestManager);

                persistentSocket.CreateMonitoringThread();
                IncomingPeers.TryAdd(sourceIp.ToString(), persistentSocket);

                NetworkSynzhronizationRequestProtocol networkSynzhronizationRequest 
                    = new NetworkSynzhronizationRequestProtocol(ClientIpAddress.ToString());

                socket.Send(ProtocolConverter<NetworkSynzhronizationRequestProtocol>.ConvertPayloadToByteArray(networkSynzhronizationRequest));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public void PropagateCommandToAllPeers(string command)
    {
        foreach (var clientEntry in IncomingPeers)
        {
            CommandRequestProtocol commandRequest = 
                new CommandRequestProtocol(ClientIpAddress.ToString(), ClientIpAddress.ToString(), command, false);

            clientEntry.Value.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(commandRequest));
        }

        if (OutgoingPeer != null)
        {
            CommandRequestProtocol commandRequest = 
                new CommandRequestProtocol(ClientIpAddress.ToString(), ClientIpAddress.ToString(), command, false);

            OutgoingPeer.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(commandRequest));
        }
    }

    public void HandleResponseDelegate(CommandRequestProtocol response)
    {
        string targetAddress = response.AddressStack.Count == 0 ? response.OriginatorAddress : response.AddressStack.Pop();
        string? outgoingPeerAddress = OutgoingPeer?.Address?.ToString();

        Console.WriteLine("trying to respond to {0}", targetAddress);
        Console.WriteLine("checking against parent {0}", outgoingPeerAddress);

        if (OutgoingPeer != null &&
            outgoingPeerAddress != null &&
            outgoingPeerAddress.Equals(targetAddress.ToString()))
        {
            Console.WriteLine("Sending response request to {0}", outgoingPeerAddress);
            OutgoingPeer.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(response));
            return;
        }

        foreach (var incomingPeer in IncomingPeers)
        {
            string? incomingPeerAddress = incomingPeer.Value.Address.ToString();

            Console.WriteLine("checking against child {0}", incomingPeerAddress);

            if (incomingPeerAddress.Equals(targetAddress.ToString()))
            {
                Console.WriteLine("Sending response request to {0}", incomingPeerAddress);
                incomingPeer.Value.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(response));
                return;
            }
        }
    }

    public void PropagateToKnownPeersWithoutLoop(CommandRequestProtocol request, IPAddress address)
    {
        string? outgoingPeerAddress = OutgoingPeer?.Address?.ToString();

        if (OutgoingPeer != null &&
            outgoingPeerAddress != null &&
            !outgoingPeerAddress.Equals(address.ToString()))
        {
            CommandRequestProtocol commandRequest =
                new CommandRequestProtocol(
                    request.OriginatorAddress, 
                    null,
                    new Stack<string>(request.AddressStack), 
                    request.Message, 
                    false);

            commandRequest.AddressStack.Push(ClientIpAddress.ToString());

            Console.WriteLine("Sending propagate request to {0}", outgoingPeerAddress);
            OutgoingPeer.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(commandRequest));
        }

        foreach (var incomingPeer in IncomingPeers)
        {
            string? incomingPeerAddress = incomingPeer.Value.Address.ToString();

            if (!incomingPeerAddress.Equals(address.ToString()))
            {
                CommandRequestProtocol commandRequest =
                    new CommandRequestProtocol(
                        request.OriginatorAddress,
                        null,
                        new Stack<string>(request.AddressStack),
                        request.Message,
                        false);

                commandRequest.AddressStack.Push(ClientIpAddress.ToString());

                Console.WriteLine("Sending propagate request to {0}", incomingPeerAddress);
                incomingPeer.Value.SendMessage(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(commandRequest));
            }
        }
    }
}