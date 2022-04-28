
using System.Net;

namespace Terminal_Distribuido.Sockets
{
    public class KnownConnection
    {
        public IPAddress Address { get; set; }

        public IPEndPoint Endpoint { get; set; }

        public KnownConnection(IPEndPoint endpoint)
        {
            this.Endpoint = endpoint;
            this.Address = Endpoint.Address;
        }
    }
}
