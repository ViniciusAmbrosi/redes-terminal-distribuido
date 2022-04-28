
using System.Net;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

namespace Terminal_Distribuido.Strategies
{
    public class ConnectionRequestHandlingStrategy : IRequestHandlingStrategy<KnownConnection>
    {
        public bool IsApplicable(RequestProtocol requestProtocol)
        {
            return requestProtocol.RequestType == RequestType.AddressSynchronization && !requestProtocol.IsResponse;
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, KnownConnection protocolObject)
        {
            ConnectionRequestProtocol? connectionRequest
               = ProtocolConverter<ConnectionRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

            if (connectionRequest == null)
            {
                Console.WriteLine("Could not process connection request");
            }
            else
            {
                Console.WriteLine("\nReceived network synchronization request from {0} at port {1} [Remote Ip: {2}, Remote Port: {3}]",
                    protocolObject.Address,
                    protocolObject.Endpoint.Port,
                    connectionRequest.RealIpAddress,
                    connectionRequest.ListenedPort);

                protocolObject.Address = IPAddress.Parse(connectionRequest.RealIpAddress);
                protocolObject.Endpoint = new IPEndPoint(protocolObject.Address, protocolObject.Endpoint.Port);

                ConnectionRequestProtocol networkSynzhronizationRequest
                    = new ConnectionRequestProtocol(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString(), true);

                if (protocolObject is KnownPersistentSocketConnection)
                {
                    ((KnownPersistentSocketConnection)protocolObject)
                        .SendMessage(ProtocolConverter<ConnectionRequestProtocol>.ConvertPayloadToByteArray(networkSynzhronizationRequest));
                }
            }
        }
    }
}
