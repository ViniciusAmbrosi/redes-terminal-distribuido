using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

namespace Terminal_Distribuido.Manager
{
    public class UDPClientManager : BaseClientManager <KnownConnection>
    {
        public UDPClientManager() :
            base()
        {
        }

        public override void StartClient(string targetEnvironment, int targetPort)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(targetEnvironment);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, targetPort);

                Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                ConnectionRequestProtocol connectionRequest
                    = new ConnectionRequestProtocol(ClientIpAddress.ToString(), false, ServerListenedPort);

                byte[] sendbuf = ProtocolConverter<ConnectionRequestProtocol>.ConvertPayloadToByteArray(connectionRequest);
                sender.SendTo(sendbuf, remoteEP);

                Console.WriteLine("UDP: Socket connected to {0}", remoteEP.ToString());

                this.KnownParentEndpoint = new KnownConnection(remoteEP);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public override void StartServer(int serverPort)
        {
            EndPoint localEndPoint = new IPEndPoint(ClientIpAddress, serverPort);

            try
            {
                Socket listener = new Socket(ClientIpAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                listener.Bind(localEndPoint);

                while (true)
                {
                    EndPoint tempTargetEndpoint = new IPEndPoint(ClientIpAddress, serverPort);

                    byte[] incomingData = new byte[10240];
                    int incomingDataByteCount = listener.ReceiveFrom(incomingData, ref tempTargetEndpoint);

                    IPAddress sourceIp = IPAddress.Parse(((IPEndPoint)tempTargetEndpoint).Address.ToString());
                    KnownConnection? endpoint = GetKnownEndpoint(sourceIp);

                    if (endpoint != null)
                    {
                        RequestManager.HandleRequest(incomingData, incomingDataByteCount, endpoint);
                    }
                    else
                    {
                        ConnectionRequestProtocol? connectionRequest
                            = ProtocolConverter<ConnectionRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

                        if (connectionRequest != null)
                        {
                            Console.WriteLine("\nUDP: Received new connection request from {0} at port {1} [Remote Ip: {2}, Remote Port: {3}]",
                                sourceIp.ToString(),
                                ((IPEndPoint)tempTargetEndpoint).Port,
                                connectionRequest.RealIpAddress,
                                connectionRequest.ListenedPort);

                            EndPoint realRemoteEndpoint = new IPEndPoint(
                                IPAddress.Parse(connectionRequest.RealIpAddress),
                                connectionRequest.ListenedPort);

                            KnownChildEndpoints.Add(new KnownConnection((IPEndPoint)realRemoteEndpoint));

                            ConnectionRequestProtocol connectionResponse
                                = new ConnectionRequestProtocol(ClientIpAddress.ToString(), true);

                            listener.SendTo(
                                ProtocolConverter<ConnectionRequestProtocol>.ConvertPayloadToByteArray(connectionResponse),
                                realRemoteEndpoint);
                        }
                        else
                        {
                            Console.WriteLine("Connection Request is Invalid. Denying service.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        protected override void SendMessage<T>(KnownConnection connection, T request)
        {
            Socket sender = new Socket(connection.Endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            sender.SendTo(ProtocolConverter<T>.ConvertPayloadToByteArray(request), connection.Endpoint);
        }

        private KnownConnection? GetKnownEndpoint(IPAddress sourceIp)
        {
            if (KnownParentEndpoint != null &&
                KnownParentEndpoint.Address.ToString().Equals(sourceIp.ToString()))
            {
                return KnownParentEndpoint;
            }

            return KnownChildEndpoints.Where(endpoint => endpoint.Address.ToString().Equals(sourceIp.ToString())).FirstOrDefault();
        }
    }
}
