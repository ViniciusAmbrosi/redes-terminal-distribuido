
namespace Terminal_Distribuido.Protocols
{
    public class RequestProtocol
    {
        public RequestType RequestType { get; set; }

        public RequestProtocol(RequestType requestType)
        {
            RequestType = requestType;
        }

        public RequestProtocol()
        {
        }
    }
}
