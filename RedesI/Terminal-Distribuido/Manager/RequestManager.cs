
using System.Net;
using System.Text;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Strategies;
using Terminal_Distribuido.Strategies.Implementations;
using Terminal_Distribuido.Strategies.Implementations.ConnectionRequest;
using Terminal_Distribuido.Strategies.Implementations.ConnectionResponse;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Manager
{
    public class RequestManager
    {
        private SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        private List<IRequestHandlingStrategy<SocketMonitor>> tcpRequestHandlingStrategies { get; set; }
        private List<IRequestHandlingStrategy<IPEndPoint>> udpRequestHandlingStrategies { get; set; }

        public RequestManager(HandleResponseDelegate handleResponseDelegate, PropagateRequestDelegate propagateRequestDelegate, TerminalManager terminalManager)
        {
            this.tcpRequestHandlingStrategies = new List<IRequestHandlingStrategy<SocketMonitor>>();

            this.tcpRequestHandlingStrategies.Add(new TCPConnectionResponseHandlingStrategy());
            this.tcpRequestHandlingStrategies.Add(new TCPCommandRequestHandlingStrategy(handleResponseDelegate, propagateRequestDelegate, terminalManager));
            this.tcpRequestHandlingStrategies.Add(new TCPCommandResponseHandlingStrategy(handleResponseDelegate)); 
            
            this.udpRequestHandlingStrategies = new List<IRequestHandlingStrategy<IPEndPoint>>();

            this.udpRequestHandlingStrategies.Add(new UDPConnectionResponseHandlingStrategy());
            this.udpRequestHandlingStrategies.Add(new UDPCommandRequestHandlingStrategy(handleResponseDelegate, propagateRequestDelegate, terminalManager));
            this.udpRequestHandlingStrategies.Add(new UDPCommandResponseHandlingStrategy(handleResponseDelegate));
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, IPEndPoint protocolObject)
        {
            Semaphore.Wait();

            string payload = Encoding.ASCII.GetString(incomingData, 0, incomingDataByteCount);
            Console.WriteLine("Recived a request with payload as: \n {0}", payload);

            RequestProtocol? genericResponse =
                    ProtocolConverter<RequestProtocol>.ConvertByteArrayToProtocol(incomingData, incomingDataByteCount);

            if (genericResponse == null)
            {
                Console.WriteLine("Request received is invalid, interrupting request handling process.");
                return;
            }

            foreach (IRequestHandlingStrategy<IPEndPoint> strategy in udpRequestHandlingStrategies)
            {
                if (strategy.IsApplicable(genericResponse))
                {
                    strategy.HandleRequest(incomingData, incomingDataByteCount, protocolObject);
                }
            }

            Semaphore.Release();
        }
    }
}
