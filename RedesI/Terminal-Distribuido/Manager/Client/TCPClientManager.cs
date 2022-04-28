using System.Collections.Concurrent;
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
                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint?.ToString());

                KnownPersistentSocketConnection socketMonitor = new KnownPersistentSocketConnection(
                    sender,
                    remoteEP,
                    RequestManager);

                socketMonitor.CreateMonitoringThread();
                KnownParentEndpoint = socketMonitor;
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
                Console.WriteLine("\nReceived connection request from {0}", sourceIp.ToString());

                KnownPersistentSocketConnection persistentSocket  = new KnownPersistentSocketConnection(
                    socket,
                    (IPEndPoint)socket.RemoteEndPoint,
                    RequestManager);

                persistentSocket.CreateMonitoringThread();
                KnownChildEndpoints.Add(persistentSocket);

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

    protected override void SendMessage<T>(KnownPersistentSocketConnection endpoint, T request)
    {
        endpoint.SendMessage(ProtocolConverter<T>.ConvertPayloadToByteArray(request));
    }

}