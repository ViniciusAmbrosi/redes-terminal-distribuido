using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

namespace Terminal_Distribuido.Strategies
{
    public class CommandResponseHandlingStrategy : IRequestHandlingStrategy <KnownConnection>
    {
        private HandleResponseDelegate HandleResponseDelegate { get; set; }

        public CommandResponseHandlingStrategy(HandleResponseDelegate handleResponseDelegate)
        {
            this.HandleResponseDelegate = handleResponseDelegate;
        }

        public bool IsApplicable(RequestProtocol requestProtocol)
        {
            return requestProtocol.RequestType == RequestType.Command && requestProtocol.IsResponse;
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, KnownConnection persistentSocket)
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
    }
}
