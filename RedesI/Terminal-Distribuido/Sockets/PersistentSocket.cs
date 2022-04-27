using System.Net;
using System.Net.Sockets;
using Terminal_Distribuido.Converters;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Sockets
{
    public delegate void PropagateRequestDelegate(byte[] message, IPAddress address);

    public delegate void HandleResponseDelegate(CommandRequestProtocol response);

    public class PersistentSocket
    {
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
            TerminalManager terminalManager)
        {
            this.SocketConnection = socketConnection;
            this.Address = address;
            this.PropagateRequestDelegate = propagateRequestDelegate;
            this.HandleResponseDelegate = handleResponseDelegate;
            this.TerminalManager = terminalManager;
        }

        public void HandleIncoming()
        {
            while (true)
            {
                byte[] incomingDataBytes = new byte[10240];
                int bytesReceived = SocketConnection.Receive(incomingDataBytes);

                RequestProtocol? genericResponse =
                    ProtocolConverter<RequestProtocol>.ConvertByteArrayToProtocol(incomingDataBytes, bytesReceived);

                if (genericResponse.RequestType == RequestType.AddressSynchronization)
                {
                    NetworkSynzhronizationRequestProtocol? networkSynchronizationRequest =
                        ProtocolConverter<NetworkSynzhronizationRequestProtocol>.ConvertByteArrayToProtocol(incomingDataBytes, bytesReceived);

                    Address = IPAddress.Parse(networkSynchronizationRequest.RealIpAddress);

                    continue;
                }

                CommandRequestProtocol? originalRequest = 
                    ProtocolConverter<CommandRequestProtocol>.ConvertByteArrayToProtocol(incomingDataBytes, bytesReceived);

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
                    PropagateRequestDelegate(ProtocolConverter<CommandRequestProtocol>.ConvertPayloadToByteArray(originalRequest), Address);
                }
            }
        }

        public void SendMessage(byte[] message)
        {
            SocketConnection.Send(message);
        }
    }
}
