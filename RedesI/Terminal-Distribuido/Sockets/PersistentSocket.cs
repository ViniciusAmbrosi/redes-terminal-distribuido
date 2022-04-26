using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Terminal_Distribuido.Terminal;
using static P2PClient;

namespace Terminal_Distribuido.Sockets
{
    public delegate void PropagateDelegate(byte[] message, IPAddress address);

    public class PersistentSocket
    {
        protected Socket SocketConnection { get; set; }
        public IPAddress Address { get; protected set; }
        protected PropagateDelegate CallbackDelegate { get; set; }
        protected TerminalManager TerminalManager { get; set; }

        public PersistentSocket(
            Socket socketConnection,
            IPAddress address,
            PropagateDelegate callbackDelegate,
            TerminalManager terminalManager)
        {
            this.SocketConnection = socketConnection;
            this.Address = address;
            this.CallbackDelegate = callbackDelegate;
            this.TerminalManager = terminalManager;
        }

        public void HandleIncoming()
        {
            while (true)
            {
                string incomingDataFromSocket = "";
                byte[] incomingDataBytes = new byte[1024];

                int bytesReceived = SocketConnection.Receive(incomingDataBytes);
                incomingDataFromSocket += Encoding.ASCII.GetString(incomingDataBytes, 0, bytesReceived);

                Hello hello = JToken.Parse(incomingDataFromSocket).ToObject<Hello>();

                string response = TerminalManager.ExecuteCommand(incomingDataFromSocket);
                Console.WriteLine("Triggering remote command execution triggered by {0}", Address.ToString());
                Console.WriteLine(response);

                byte[] msg = Encoding.ASCII.GetBytes(incomingDataFromSocket);

                Console.WriteLine("Command received from {0} -> {1}", Address.ToString(), incomingDataFromSocket);

                //fire event about message received
                CallbackDelegate(msg, Address);
            }
        }

        public void SendMessage(byte[] message)
        {
            SocketConnection.Send(message);
        }
    }
}
