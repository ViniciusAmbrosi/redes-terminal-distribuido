
using System.Net;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;

namespace Terminal_Distribuido.Strategies.Implementations.ConnectionResponse
{
    public class UDPConnectionResponseHandlingStrategy : BaseConnectionResponseHandlingStrategy<IPEndPoint>
    {
        public override void HandleRequest(byte[] incomingData, int incomingDataByteCount, IPEndPoint endpoint)
        {
            ConnectionRequestProtocol? connectionRequest
                = ProtocolConverter<ConnectionRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

            if (connectionRequest == null)
            {
                Console.WriteLine("Could not process connection response");
            }
            else
            {
                Console.WriteLine("\nNetwork Synzhronized. [Being {0}:{1} | Established {2}:{3}]",
                    endpoint.Address.ToString(),
                    endpoint.Port,
                    IPAddress.Parse(connectionRequest.RealIpAddress),
                    endpoint.Port);

                endpoint.Address = IPAddress.Parse(connectionRequest.RealIpAddress);
            }
        }
    }
}
