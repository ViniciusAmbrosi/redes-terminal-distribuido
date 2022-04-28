using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Manager;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

public class TCPClientManager : BaseClientManager <KnownPersistentSocketConnection>
{
    public TCPClientManager() :
        base()
    {
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
                Console.WriteLine("TCP: Socket connected to {0}", sender.RemoteEndPoint?.ToString());

                KnownPersistentSocketConnection knownPersistedSocket = new KnownPersistentSocketConnection(
                    sender,
                    remoteEP,
                    RequestManager);

                knownPersistedSocket.CreateMonitoringThread();
                KnownParentEndpoint = knownPersistedSocket;

                ConnectionRequestProtocol connectionRequest
                    = new ConnectionRequestProtocol(ClientIpAddress.ToString(), false, ServerListenedPort);

                knownPersistedSocket.SendMessage(ProtocolConverter<ConnectionRequestProtocol>.ConvertPayloadToByteArray(connectionRequest));
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

            Socket socket;

            while (true)
            {
                socket = listener.Accept();

                IPAddress sourceIp = IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString());
                Console.WriteLine("\nTCP: Received connection request from {0}", sourceIp.ToString());

                KnownPersistentSocketConnection persistentSocket  = new KnownPersistentSocketConnection(
                    socket,
                    (IPEndPoint)socket.RemoteEndPoint,
                    RequestManager);

                persistentSocket.CreateMonitoringThread();
                KnownChildEndpoints.Add(persistentSocket);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    protected override void SendMessage<T>(KnownPersistentSocketConnection endpoint, T request)
    {
        endpoint.SendMessage(ProtocolConverter<T>.ConvertPayloadToByteArray(request));
    }

}