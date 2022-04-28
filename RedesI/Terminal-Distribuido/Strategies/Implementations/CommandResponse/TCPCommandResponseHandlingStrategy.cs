
using System.Net;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

namespace Terminal_Distribuido.Strategies
{
    public class TCPCommandResponseHandlingStrategy : IRequestHandlingStrategy <SocketMonitor>
    {
        private HandleResponseDelegate HandleResponseDelegate { get; set; }

        public TCPCommandResponseHandlingStrategy(HandleResponseDelegate handleResponseDelegate)
        {
            this.HandleResponseDelegate = handleResponseDelegate;
        }

        public bool IsApplicable(RequestProtocol requestProtocol)
        {
            return requestProtocol.RequestType == RequestType.Command && requestProtocol.IsResponse;
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, SocketMonitor persistentSocket)
        {
            CommandRequestProtocol? commandRequestProtocol =
                    ProtocolConverter<CommandRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

            if (commandRequestProtocol == null)
            {
                Console.WriteLine("Can't process command request procol");
            }
            else if (commandRequestProtocol.AddressStack.Count == 0)
            {
                Console.WriteLine("\nResponse received from {0}", commandRequestProtocol.ReplierAddress);
                Console.WriteLine(commandRequestProtocol.Message);
            }
            else
            {
                HandleResponseDelegate(commandRequestProtocol);
            }
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, IPEndPoint endpoint)
        {
            throw new NotImplementedException();
        }
    }
}
