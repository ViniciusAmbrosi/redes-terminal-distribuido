using System.Net;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

namespace Terminal_Distribuido.Strategies
{
    public class ConnectionResponseHandlingStrategy : IRequestHandlingStrategy <KnownConnection>
    {
        public bool IsApplicable(RequestProtocol requestProtocol)
        {
            return requestProtocol.RequestType == RequestType.AddressSynchronization && requestProtocol.IsResponse;
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, KnownConnection connection)
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
                    connection.Address.ToString(),
                    connection.Endpoint.Port,
                    IPAddress.Parse(connectionRequest.RealIpAddress),
                    connection.Endpoint.Port);

                connection.Address = IPAddress.Parse(connectionRequest.RealIpAddress);
                connection.Endpoint = new IPEndPoint(connection.Address, connection.Endpoint.Port);
            }
        }
    }
}
