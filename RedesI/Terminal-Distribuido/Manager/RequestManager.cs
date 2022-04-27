
using System.Text;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Strategies;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Manager
{
    public class RequestManager
    {
        private SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        private List<IRequestHandlingStrategy> requestHandlingStrategies { get; set; }

        public RequestManager(HandleResponseDelegate handleResponseDelegate, PropagateRequestDelegate propagateRequestDelegate, TerminalManager terminalManager)
        {
            this.requestHandlingStrategies = new List<IRequestHandlingStrategy>();

            this.requestHandlingStrategies.Add(new NetworkSynchronizationRequestHandlingStrategy());
            this.requestHandlingStrategies.Add(new CommandRequestHandlingStrategy(handleResponseDelegate, propagateRequestDelegate, terminalManager));
            this.requestHandlingStrategies.Add(new CommandResponseHandlingStrategy(handleResponseDelegate));
        }

        public void HandleRequest(byte[] incomingData, int incomingDataByteCount, PersistentSocket persistentSocket)
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

            foreach (IRequestHandlingStrategy strategy in requestHandlingStrategies)
            {
                if (strategy.IsApplicable(genericResponse))
                {
                    strategy.HandleRequest(incomingData, incomingDataByteCount, persistentSocket);
                }
            }

            Semaphore.Release();
        }
    }
}
