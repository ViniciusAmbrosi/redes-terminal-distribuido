﻿using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Sockets
{
    public class IncomingPersistentSocket : PersistentSocket
    {
        public IncomingPersistentSocket(
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
            Thread maintainedSocketThread = new Thread(HandleIncoming);
            maintainedSocketThread.Start();
        }
    }
}
