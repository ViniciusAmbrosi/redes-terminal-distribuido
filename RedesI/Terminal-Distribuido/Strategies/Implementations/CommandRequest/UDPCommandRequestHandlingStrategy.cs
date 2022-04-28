using System.Net;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Strategies.Implementations
{
    internal class UDPCommandRequestHandlingStrategy : BaseCommandRequestHandlingStrategy<IPEndPoint>
    {
        public UDPCommandRequestHandlingStrategy(
            HandleResponseDelegate handleResponseDelegate, 
            PropagateRequestDelegate propagateRequestDelegate, 
            TerminalManager terminalManager) : 
            base(handleResponseDelegate, propagateRequestDelegate, terminalManager)
        {
        }

        public override void HandleRequest(byte[] incomingData, int incomingDataByteCount, IPEndPoint endpoint)
        {
            CommandRequestProtocol? commandRequestProtocol = ProcessRequest(incomingData, incomingDataByteCount);

            //continue to propagate request
            PropagateRequestDelegate(commandRequestProtocol, endpoint.Address);
        }
    }
}
