using System.Net;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

namespace Terminal_Distribuido.Strategies.Implementations.ConnectionRequest
{
    public class TCPConnectionResponseHandlingStrategy : BaseConnectionResponseHandlingStrategy<SocketMonitor>
    {
        public override void HandleRequest(byte[] incomingData, int incomingDataByteCount, SocketMonitor persistentSocket)
        {
            ConnectionRequestProtocol? connectionRequest =
                        ProtocolConverter<ConnectionRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

            if (connectionRequest == null)
            {
                Console.WriteLine("Can't process network synchronization request protocol");
            }
            else
            {
                persistentSocket.Address = IPAddress.Parse(connectionRequest.RealIpAddress);
            }
        }
    }
}
