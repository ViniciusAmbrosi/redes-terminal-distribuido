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

            //Thread outgoingSocketThread = new Thread(HanldeOutgoing);
            //outgoingSocketThread.Start();
        }

        //public void HanldeOutgoing()
        //{
        //    while (true)
        //    {
        //        string? message = Console.ReadLine();
        //        byte[] msg = Encoding.ASCII.GetBytes(message);

        //        // Send the data through the socket.
        //        int bytesSent = SocketConnection.Send(msg);
        //    }
        //}
    }
}
