using System.Net;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;

namespace Terminal_Distribuido
{
    public interface IRequestHandlingStrategy <T>
    {
        bool IsApplicable(RequestProtocol requestProtocol);

        void HandleRequest(byte[] incomingData, int incomingDataByteCount, T protocolObject);
    }
}
