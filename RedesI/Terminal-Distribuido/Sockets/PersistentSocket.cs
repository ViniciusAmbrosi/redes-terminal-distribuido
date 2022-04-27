using System.Net;
using System.Net.Sockets;
using System.Text;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Sockets
{
    public delegate void PropagateRequestDelegate(byte[] message, IPAddress address);

    public delegate void HandleResponseDelegate(CommandRequestProtocol response);

    public class PersistentSocket
    {
        protected bool PendingIpAddressSynchronization { get; set; }
        protected Socket SocketConnection { get; set; }
        public IPAddress Address { get; protected set; }
        protected PropagateRequestDelegate PropagateRequestDelegate { get; set; }
        protected HandleResponseDelegate HandleResponseDelegate { get; set; }
        protected TerminalManager TerminalManager { get; set; }

        public PersistentSocket(
            Socket socketConnection,
            IPAddress address,
            PropagateRequestDelegate propagateRequestDelegate,
            HandleResponseDelegate handleResponseDelegate,
            TerminalManager terminalManager,
            bool pendingIpAddressSynzhronization)
        {
            this.SocketConnection = socketConnection;
            this.Address = address;
            this.PropagateRequestDelegate = propagateRequestDelegate;
            this.HandleResponseDelegate = handleResponseDelegate;
            this.TerminalManager = terminalManager;
            this.PendingIpAddressSynchronization = pendingIpAddressSynzhronization;
        }

        public void HandleIncoming()
        {
            while (true)
            {
                byte[] incomingDataBytes = new byte[1024];
                int bytesReceived = SocketConnection.Receive(incomingDataBytes);

                if (PendingIpAddressSynchronization)
                {
                    string ipAddress = Encoding.ASCII.GetString(incomingDataBytes, 0, bytesReceived);
                    Address = IPAddress.Parse(ipAddress);

                    this.PendingIpAddressSynchronization = false;
                    continue;
                }

                CommandRequestProtocol? originalRequest = 
                    ProtocolConverter.ConvertByteArrayToProtocol(incomingDataBytes, bytesReceived);

                if (originalRequest == null)
                    continue;

                if (originalRequest.IsResponse)
                {
                    if (originalRequest.AddressStack.Count == 0)
                    {
                        Console.WriteLine("Response received from {0}");
                        Console.WriteLine(originalRequest.Message);
                    }
                    else 
                    {
                        HandleResponseDelegate(originalRequest);
                    }
                }
                else
                {
                    Console.WriteLine("Triggering remote command execution triggered by {0}", Address.ToString());

                    string response = TerminalManager.ExecuteCommand(originalRequest.Message);
                    Console.WriteLine(response);

                    //respond to caller
                    CommandRequestProtocol responseRequest =
                        new CommandRequestProtocol(originalRequest.OriginatorAddress, Address.ToString(), new Stack<string>(originalRequest.AddressStack), response, true);
                    HandleResponseDelegate(responseRequest);
    
                    //continue to propagate request
                    originalRequest.AddressStack.Push(Address.ToString());
                    PropagateRequestDelegate(ProtocolConverter.ConvertPayloadToByteArray(originalRequest), Address);
                }
            }
        }

        public void SendMessage(byte[] message)
        {
            SocketConnection.Send(message);
        }
    }
}
