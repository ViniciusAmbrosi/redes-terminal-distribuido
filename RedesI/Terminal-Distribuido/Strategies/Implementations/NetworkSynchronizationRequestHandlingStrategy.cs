
using System.Net;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

namespace Terminal_Distribuido.Strategies
{
    public class NetworkSynchronizationRequestHandlingStrategy : IRequestHandlingStrategy
    {
        public bool IsApplicable(RequestProtocol requestProtocol)
        {
            return requestProtocol.RequestType == RequestType.AddressSynchronization;
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, PersistentSocket persistentSocket)
        {
            NetworkSynzhronizationRequestProtocol? networkSynchronizationRequest =
                        ProtocolConverter<NetworkSynzhronizationRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

            if (networkSynchronizationRequest == null)
            {
                Console.WriteLine("Can't process network synchronization request protocol");
            }
            else 
            {
                persistentSocket.Address = IPAddress.Parse(networkSynchronizationRequest.RealIpAddress);
            }
        }
    }
}
