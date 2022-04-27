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
            PropagateRequestDelegate callbackDelegate,
            HandleResponseDelegate handleResponseDelegate,
            TerminalManager terminalManager,
            bool pendingIpAddressSynzhronization) : 
            base(socketConnection, address, callbackDelegate, handleResponseDelegate, terminalManager, pendingIpAddressSynzhronization)
        {
        }

        public void CreateMonitoringThread()
        {
            Thread incomingSocketThread = new Thread(HandleIncoming);
            incomingSocketThread.Start();
        }
    }
}
