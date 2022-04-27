
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Strategies
{
    public class CommandRequestHandlingStrategy : IRequestHandlingStrategy
    {
        private HandleResponseDelegate HandleResponseDelegate { get; set; }

        private PropagateRequestDelegate PropagateRequestDelegate { get; set; }

        private TerminalManager TerminalManager { get; set; }

        public CommandRequestHandlingStrategy(HandleResponseDelegate handleResponseDelegate, PropagateRequestDelegate propagateRequestDelegate, TerminalManager terminalManager)
        {
            this.HandleResponseDelegate = handleResponseDelegate;
            this.PropagateRequestDelegate = propagateRequestDelegate;
            this.TerminalManager = terminalManager;
        }

        public bool IsApplicable(RequestProtocol requestProtocol)
        {
            return requestProtocol.RequestType == RequestType.Command && !requestProtocol.IsResponse;
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, PersistentSocket persistentSocket)
        {
            CommandRequestProtocol? commandRequeustProtocol =
                    ProtocolConverter<CommandRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

            if (commandRequeustProtocol == null)
            {
                Console.WriteLine("Can't process command request procol");
            }
            else
            {
                Console.WriteLine("\n-- -- -- -- -- -- -- -- -- -- -- -- --");
                Console.WriteLine("\nTriggering remote command [{0}] execution triggered by [{1}]", 
                    commandRequeustProtocol.Message,
                    commandRequeustProtocol.OriginatorAddress);

                string response = TerminalManager.ExecuteCommand(commandRequeustProtocol.Message);
                Console.WriteLine(response);

                //respond to caller
                CommandRequestProtocol responseRequest =
                    new CommandRequestProtocol(commandRequeustProtocol.OriginatorAddress, 
                        persistentSocket.Address.ToString(), 
                        new Stack<string>(commandRequeustProtocol.AddressStack), 
                        response, 
                        true);

                HandleResponseDelegate(responseRequest);

                //continue to propagate request
                PropagateRequestDelegate(commandRequeustProtocol, persistentSocket.Address);
            }
        }
    }
}
