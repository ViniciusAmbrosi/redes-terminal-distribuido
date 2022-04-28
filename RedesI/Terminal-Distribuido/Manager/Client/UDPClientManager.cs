
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;

namespace Terminal_Distribuido.Manager
{
    public class UDPClientManager : BaseClientManager
    {
        protected IPEndPoint? KnownParentEndpoint { get; set; }
        protected ConcurrentBag<IPEndPoint> KnownChildEndpoints { get; set; }

        public UDPClientManager() :
            base()
        {
            this.KnownChildEndpoints = new ConcurrentBag<IPEndPoint>();
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

                Console.WriteLine("Socket connected to {0}", remoteEP.ToString());

                this.KnownParentEndpoint = remoteEP;
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

                Console.WriteLine("Started listening for requests");

                while (true)
                {
                    EndPoint tempTargetEndpoint = new IPEndPoint(ClientIpAddress, serverPort);

                    byte[] incomingData = new byte[10240];
                    int incomingDataByteCount = listener.ReceiveFrom(incomingData, ref tempTargetEndpoint);

                    IPAddress sourceIp = IPAddress.Parse(((IPEndPoint)tempTargetEndpoint).Address.ToString());
                    IPEndPoint? endpoint = GetKnownEndpoint(sourceIp);

                    if (endpoint != null)
                    {
                        Console.WriteLine("Processing Request");
                        RequestManager.HandleRequest(incomingData, incomingDataByteCount, endpoint);
                    }
                    else
                    {
                        ConnectionRequestProtocol? connectionRequest
                            = ProtocolConverter<ConnectionRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

                        if (connectionRequest != null)
                        {
                            Console.WriteLine("\nReceived new connection request from {0} at port {1} [Remote Ip: {2}, Remote Port: {3}]",
                                sourceIp.ToString(),
                                ((IPEndPoint)tempTargetEndpoint).Port,
                                connectionRequest.RealIpAddress,
                                connectionRequest.ListenedPort);

                            EndPoint realRemoteEndpoint = new IPEndPoint(
                                IPAddress.Parse(connectionRequest.RealIpAddress),
                                connectionRequest.ListenedPort);

                            KnownChildEndpoints.Add((IPEndPoint)realRemoteEndpoint);

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

        public override void PropagateCommandToAllPeers(string command)
        {
            foreach (var childEndpoint in KnownChildEndpoints)
            {
                CommandRequestProtocol commandRequest =
                    new CommandRequestProtocol(ClientIpAddress.ToString(), ClientIpAddress.ToString(), command, false);

                SendMessage(childEndpoint, commandRequest);
            }

            if (KnownParentEndpoint != null)
            {
                CommandRequestProtocol commandRequest =
                    new CommandRequestProtocol(ClientIpAddress.ToString(), ClientIpAddress.ToString(), command, false);

                SendMessage(KnownParentEndpoint, commandRequest);
            }
        }

        public override void HandleResponseDelegate(CommandRequestProtocol response)
        {
            string targetAddress = response.AddressStack.Pop();
            string? outgoingPeerAddress = KnownParentEndpoint?.Address?.ToString();

            Console.WriteLine("Trying to respond to {0}", targetAddress);
            Console.WriteLine("checking against parent {0}", outgoingPeerAddress);

            if (KnownParentEndpoint != null &&
                outgoingPeerAddress != null &&
                outgoingPeerAddress.Equals(targetAddress.ToString()))
            {
                Console.WriteLine("Sending response request to {0}", outgoingPeerAddress);
                SendMessage(KnownParentEndpoint, response);

                return;
            }

            foreach (var childEndpoint in KnownChildEndpoints)
            {
                string? incomingPeerAddress = childEndpoint.Address.ToString();

                Console.WriteLine("checking against child {0}", incomingPeerAddress);

                if (incomingPeerAddress.Equals(targetAddress.ToString()))
                {
                    Console.WriteLine("Sending response request to {0}", incomingPeerAddress);
                    SendMessage(childEndpoint, response);

                    return;
                }
            }
        }

        public override void PropagateToKnownPeersWithoutLoop(CommandRequestProtocol request, IPAddress address)
        {
            string? outgoingPeerAddress = KnownParentEndpoint?.Address?.ToString();

            if (KnownParentEndpoint != null &&
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
                SendMessage(KnownParentEndpoint, commandRequest);
            }

            foreach (var childEndpoint in KnownChildEndpoints)
            {
                string? incomingPeerAddress = childEndpoint.Address.ToString();

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
                    SendMessage(childEndpoint, commandRequest);
                }
            }
        }

        private IPEndPoint? GetKnownEndpoint(IPAddress sourceIp)
        {
            if (KnownParentEndpoint != null &&
                KnownParentEndpoint.Address.ToString().Equals(sourceIp.ToString()))
            {
                return KnownParentEndpoint;
            }

            return KnownChildEndpoints.Where(endpoint => endpoint.Address.ToString().Equals(sourceIp.ToString())).FirstOrDefault();
        }

        private void SendMessage<T>(IPEndPoint endpoint, T request)
        {
            Socket sender = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            sender.SendTo(ProtocolConverter<T>.ConvertPayloadToByteArray(request), endpoint);
        }
    }
}
