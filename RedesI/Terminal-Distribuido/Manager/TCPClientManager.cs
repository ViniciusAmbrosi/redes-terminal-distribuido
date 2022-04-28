using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Manager;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

public class TCPClientManager : BaseClientManager
{
    protected SocketMonitor? OutgoingPeer { get; set; }
    protected ConcurrentDictionary<string, SocketMonitor> IncomingPeers { get; set; }

    public TCPClientManager() :
        base()
    {
        this.IncomingPeers = new ConcurrentDictionary<string, SocketMonitor>();
    }

    public override void StartClient(string targetEnvironment, int port)
    {
        try
        {
            IPAddress ipAddress = IPAddress.Parse(targetEnvironment);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(remoteEP);

            if (sender != null)
            {
                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint?.ToString());

                SocketMonitor socketMonitor = new SocketMonitor(
                    sender,
                    ipAddress,
                    RequestManager);

                socketMonitor.CreateMonitoringThread();
                OutgoingPeer = socketMonitor;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public override void StartServer(int port)
    {
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

                SocketMonitor persistentSocket  = new SocketMonitor(
                    socket,
                    sourceIp,
                    RequestManager);

                persistentSocket.CreateMonitoringThread();
                IncomingPeers.TryAdd(sourceIp.ToString(), persistentSocket);

                ConnectionRequestProtocol networkSynzhronizationRequest 
                    = new ConnectionRequestProtocol(ClientIpAddress.ToString(), true);

                socket.Send(ProtocolConverter<ConnectionRequestProtocol>.ConvertPayloadToByteArray(networkSynzhronizationRequest));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public override void PropagateCommandToAllPeers(string command)
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

    public override void HandleResponseDelegate(CommandRequestProtocol response)
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

    public override void PropagateToKnownPeersWithoutLoop(CommandRequestProtocol request, IPAddress address)
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