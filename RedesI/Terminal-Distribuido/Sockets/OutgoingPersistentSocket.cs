using System.Net;
using System.Net.Sockets;

namespace Terminal_Distribuido.Sockets
{
    public class OutgoingPersistentSocket : PersistentSocket
    {
        public OutgoingPersistentSocket(
            Socket socketConnection,
            IPAddress address,
            PropagateDelegate callbackDelegate) : 
            base(socketConnection, address, callbackDelegate)
        {
        }

        public void CreateMonitoringThread()
        {
            Thread incomingSocketThread = new Thread(HandleIncoming);
            incomingSocketThread.Start();
        }
    }
}
