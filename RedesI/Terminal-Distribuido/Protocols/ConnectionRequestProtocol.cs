
namespace Terminal_Distribuido.Protocols
{
    public class ConnectionRequestProtocol : RequestProtocol
    {
        public string RealIpAddress { get; set; }

        public int ListenedPort { get; set; }

        public ConnectionRequestProtocol(string realIpAddress, bool isResponse, int listenedPort = 0) :
            base(RequestType.AddressSynchronization, isResponse)
        {
            this.RealIpAddress = realIpAddress;
            this.ListenedPort = listenedPort;
        }

        public ConnectionRequestProtocol()
        {
        }
    }
}
