using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Manager;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Sockets
{
    public class OutgoingPersistentSocket : PersistentSocket
    {
        public OutgoingPersistentSocket(
            Socket socketConnection,
            IPAddress address,
            RequestManager requestManager) : 
            base(socketConnection, address, requestManager)
        {
        }
    }
}
