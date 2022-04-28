using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Manager;
using Terminal_Distribuido.Protocols;

namespace Terminal_Distribuido.Sockets
{
    public delegate void PropagateRequestDelegate(CommandRequestProtocol request, IPAddress address);

    public delegate void HandleResponseDelegate(CommandRequestProtocol response);

    public class SocketMonitor
    {
        protected Socket SocketConnection { get; set; }
        protected RequestManager RequestManager { get; set; }
        public IPAddress Address { get; set; }

        public SocketMonitor(
            Socket socketConnection,
            IPAddress address,
            RequestManager requestManager)
        {
            this.SocketConnection = socketConnection;
            this.Address = address;
            this.RequestManager = requestManager;
        }

        public void HandleIncoming()
        {
            //pending split of merged TCP packets received
            while (true)
            {
                byte[] incomingData = new byte[10240];
                int incomingDataByteCount = SocketConnection.Receive(incomingData);

                //RequestManager.HandleRequest(incomingData, incomingDataByteCount, this);
            }
        }

        public void CreateMonitoringThread()
        {
            Thread maintainedSocketThread = new Thread(HandleIncoming);
            maintainedSocketThread.Start();
        }

        public void SendMessage(byte[] message)
        {
            SocketConnection.Send(message);
        }
    }
}
