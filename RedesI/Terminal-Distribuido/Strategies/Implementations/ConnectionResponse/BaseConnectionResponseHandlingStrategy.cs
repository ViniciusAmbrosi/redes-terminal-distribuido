
using Terminal_Distribuido.Protocols;

namespace Terminal_Distribuido.Strategies
{
    public abstract class BaseConnectionResponseHandlingStrategy <T> : IRequestHandlingStrategy <T>
    {
        public bool IsApplicable(RequestProtocol requestProtocol)
        {
            return requestProtocol.RequestType == RequestType.AddressSynchronization && requestProtocol.IsResponse;
        }

        public abstract void HandleRequest(byte[] incomingData, int incomingDataByteCount, T endpoint);
    }
}
