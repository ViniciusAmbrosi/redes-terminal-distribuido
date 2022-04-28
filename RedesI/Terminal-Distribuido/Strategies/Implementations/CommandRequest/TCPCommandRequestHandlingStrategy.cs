using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Strategies.Implementations
{
    public class TCPCommandRequestHandlingStrategy : BaseCommandRequestHandlingStrategy<SocketMonitor>
    {
        public TCPCommandRequestHandlingStrategy(
            HandleResponseDelegate handleResponseDelegate, 
            PropagateRequestDelegate propagateRequestDelegate, 
            TerminalManager terminalManager) : 
            base(handleResponseDelegate, propagateRequestDelegate, terminalManager)
        {
        }

        public override void HandleRequest(byte[] incomingData, int incomingDataByteCount, SocketMonitor persistentSocket)
        {
            CommandRequestProtocol? commandRequestProtocol = ProcessRequest(incomingData, incomingDataByteCount);

            //continue to propagate request
            PropagateRequestDelegate(commandRequestProtocol, persistentSocket.Address);
        }
    }
}
