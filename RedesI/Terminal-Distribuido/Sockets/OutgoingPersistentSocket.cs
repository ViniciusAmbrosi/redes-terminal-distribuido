using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Sockets
{
    public class OutgoingPersistentSocket : PersistentSocket
    {
        public OutgoingPersistentSocket(
            Socket socketConnection,
            IPAddress address,
            PropagateDelegate callbackDelegate,
            TerminalManager terminalManager) : 
            base(socketConnection, address, callbackDelegate, terminalManager)
        {
        }

        public void CreateMonitoringThread()
        {
            Thread incomingSocketThread = new Thread(HandleIncoming);
            incomingSocketThread.Start();
        }
    }
}
