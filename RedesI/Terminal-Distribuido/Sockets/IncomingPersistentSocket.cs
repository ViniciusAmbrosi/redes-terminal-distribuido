using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Sockets
{
    public class IncomingPersistentSocket : PersistentSocket
    {
        public IncomingPersistentSocket(
            Socket socketConnection,
            IPAddress address,
            PropagateDelegate callbackDelegate,
            TerminalManager terminalManager) : 
            base(socketConnection, address, callbackDelegate, terminalManager)
        {
        }

        public void CreateMonitoringThread()
        {
            Thread maintainedSocketThread = new Thread(HandleIncoming);
            maintainedSocketThread.Start();
        }
    }
}
