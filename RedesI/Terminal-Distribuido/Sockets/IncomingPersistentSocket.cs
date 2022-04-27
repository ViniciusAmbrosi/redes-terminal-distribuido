using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Manager;

namespace Terminal_Distribuido.Sockets
{
    public class IncomingPersistentSocket : PersistentSocket
    {
        public IncomingPersistentSocket(
            Socket socketConnection,
            IPAddress address,
            RequestManager requestManager) : 
            base(socketConnection, address, requestManager)
        {
        }
    }
}
