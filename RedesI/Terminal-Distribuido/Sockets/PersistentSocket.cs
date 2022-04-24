using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Terminal_Distribuido.Sockets
{

    public delegate void PropagateDelegate(byte[] message);

    public class PersistentSocket
    {
        protected Socket SocketConnection { get; set; }
        protected IPAddress Address { get; set; }
        protected PropagateDelegate CallbackDelegate { get; set; }

        public PersistentSocket(
            Socket socketConnection,
            IPAddress address,
            PropagateDelegate callbackDelegate)
        {
            this.SocketConnection = socketConnection;
            this.Address = address;
            CallbackDelegate = callbackDelegate;
        }

        public void HandleIncoming()
        {
            while (true)
            {
                string incomingDataFromSocket = "";
                byte[] incomingDataBytes = new byte[1024];

                int bytesReceived = SocketConnection.Receive(incomingDataBytes);
                incomingDataFromSocket += Encoding.ASCII.GetString(incomingDataBytes, 0, bytesReceived);

                byte[] msg;

                Console.WriteLine("Command received from {0} -> {1}", Address.ToString(), incomingDataFromSocket);
                //msg = Encoding.ASCII.GetBytes(incomingDataFromSocket);

                msg = Encoding.ASCII.GetBytes("Message Processed");

                //fire event about message received
                CallbackDelegate(msg);
            }

        }

        public void SendMessage(byte[] message)
        {
            SocketConnection.Send(message);
        }
    }
}
