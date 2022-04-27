
namespace Terminal_Distribuido.Protocols
{
    public class RequestProtocol
    {
        public RequestType RequestType { get; set; }
        public bool IsResponse { get; set; }

        public RequestProtocol(RequestType requestType, bool isResponse)
        {
            this.RequestType = requestType;
            this.IsResponse = isResponse;
        }

        public RequestProtocol()
        {
        }
    }
}
