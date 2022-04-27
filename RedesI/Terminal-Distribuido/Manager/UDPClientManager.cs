
using System.Net;
using System.Net.Sockets;
using System.Text;
using Terminal_Distribuido.Protocols;

namespace Terminal_Distribuido.Manager
{
    public class UDPClientManager : BaseClientManager
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

                byte[] sendbuf = Encoding.ASCII.GetBytes("Connect me!");
                sender.SendTo(sendbuf, remoteEP);

                Console.WriteLine("Socket connected to {0}", remoteEP.ToString());
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
                    byte[] incomingData = new byte[10240];
                    int incomingDataByteCount = listener.ReceiveFrom(incomingData, ref localEndPoint);

                    Console.WriteLine("\nReceived connection request");

                    IPAddress sourceIp = IPAddress.Parse(((IPEndPoint)localEndPoint).Address.ToString());
                    Console.WriteLine("\nReceived connection request from {0}", sourceIp.ToString());
                }
            }
            catch (Exception ex)
            { 
            }
        }

        public override void HandleResponseDelegate(CommandRequestProtocol response)
        {
            throw new NotImplementedException();
        }

        public override void PropagateCommandToAllPeers(string command)
        {
            throw new NotImplementedException();
        }

        public override void PropagateToKnownPeersWithoutLoop(CommandRequestProtocol request, IPAddress address)
        {
            throw new NotImplementedException();
        }
    }
}
