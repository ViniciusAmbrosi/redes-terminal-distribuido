using System.Net;
using System.Net.Sockets;

namespace Terminal_Distribuido.Sockets
{
    public class IncomingPersistentSocket : PersistentSocket
    {
        public IncomingPersistentSocket(
            Socket socketConnection,
            IPAddress address,
            PropagateDelegate callbackDelegate) : 
            base(socketConnection, address, callbackDelegate)
        {
        }

        public void CreateMonitoringThread()
        {
            Thread maintainedSocketThread = new Thread(HandleIncoming);
            maintainedSocketThread.Start();
        }
    }
}
