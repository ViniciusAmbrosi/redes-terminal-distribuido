using System.Net;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Strategies
{
    public class CommandRequestHandlingStrategy : IRequestHandlingStrategy <KnownConnection>
    {
        protected HandleResponseDelegate HandleResponseDelegate { get; set; }

        protected PropagateRequestDelegate PropagateRequestDelegate { get; set; }

        protected TerminalManager TerminalManager { get; set; }

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

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, KnownConnection connection)
        {
            CommandRequestProtocol? commandRequestProtocol = ProcessRequest(incomingData, incomingDataByteCount);

            //continue to propagate request
            PropagateRequestDelegate(commandRequestProtocol, connection.Address);
        }

        protected CommandRequestProtocol? ProcessRequest(byte[] incomingData, int incomingDataByteCount)
        {
            CommandRequestProtocol? commandRequestProtocol =
                ProtocolConverter<CommandRequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

            if (commandRequestProtocol == null)
            {
                Console.WriteLine("Can't process command request procol");
                return null;
            }
            else
            {
                Console.WriteLine("\n-- -- -- -- -- -- -- -- -- -- -- -- --");
                Console.WriteLine("\nTriggering remote command [{0}] execution triggered by [{1}]",
                    commandRequestProtocol.Message,
                    commandRequestProtocol.OriginatorAddress);

                string response = TerminalManager.ExecuteCommand(commandRequestProtocol.Message);
                Console.WriteLine(response);

                //respond to caller
                CommandRequestProtocol responseRequest =
                    new CommandRequestProtocol(commandRequestProtocol.OriginatorAddress,
                        Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString(),
                        new Stack<string>(commandRequestProtocol.AddressStack),
                        response,
                        true);

                HandleResponseDelegate(responseRequest);
                
                //continue to propagate request
                return commandRequestProtocol;
            }
        }
    }
}
