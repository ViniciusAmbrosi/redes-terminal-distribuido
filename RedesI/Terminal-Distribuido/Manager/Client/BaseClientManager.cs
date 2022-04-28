using System.Collections.Concurrent;
using System.Net;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Manager
{
    public abstract class BaseClientManager <T>
        where T : KnownConnection
    {
        protected T? KnownParentEndpoint { get; set; }
        protected ConcurrentBag<T> KnownChildEndpoints { get; set; }
        protected TerminalManager TerminalManager { get; set; }
        protected IPAddress ClientIpAddress { get; set; }
        protected RequestManager RequestManager { get; set; }
        protected int ServerListenedPort { get; set; }

        protected BaseClientManager()
        {
            this.TerminalManager = new TerminalManager();

            this.ClientIpAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];

            this.RequestManager = new RequestManager(HandleResponseDelegate, PropagateToKnownPeersWithoutLoop, TerminalManager);
            
            this.KnownChildEndpoints = new ConcurrentBag<T>();
        }

        public virtual void ManageTerminalInput()
        {
            while (true)
            {
                string? message = Console.ReadLine();

                if (!String.IsNullOrEmpty(message))
                {
                    string commandResult = TerminalManager.ExecuteCommand(message);

                    Console.WriteLine("\n-- -- -- -- -- -- -- -- -- -- -- -- --");
                    Console.WriteLine("\nSource system result");
                    Console.WriteLine($"{commandResult}\n");

                    // Propagate data to all sockets
                    PropagateCommandToAllPeers(message);
                }
            }
        }

        public void InitiateOutgoingSocketConnection()
        {
            Console.WriteLine("Should connect to another client (N/Y)?");
            string? shouldConnect = Console.ReadLine();

            if ("Y".Equals(shouldConnect, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Provide target environment IP address");
                string? targetEnvironment = Console.ReadLine();

                if (targetEnvironment.Equals("loopback", StringComparison.OrdinalIgnoreCase))
                {
                    targetEnvironment = ClientIpAddress.ToString();
                }

                Console.WriteLine("Provide target environment port");
                string? targetPort = Console.ReadLine();

                int port = 0;

                if (String.IsNullOrEmpty(targetEnvironment) ||
                    String.IsNullOrEmpty(targetPort) ||
                    !int.TryParse(targetPort, out port))
                {
                    Console.WriteLine("No target IP or port provided, continuing as client only.");
                    return;
                }

                StartClient(targetEnvironment, port);
            }
        }

        public void ListenForIncomingSocketConnections()
        {
            Console.WriteLine("Provide this server port for socket server");
            string? serverPort = Console.ReadLine();

            int port = 0;
            if (String.IsNullOrEmpty(serverPort) || !int.TryParse(serverPort, out port))
            {
                Console.WriteLine("Server port not provided. Interrupting flow.");
                return;
            }

            this.ServerListenedPort = port;

            Thread maintainedSocketThread = new Thread(() => StartServer(port));
            maintainedSocketThread.Start();
        }

        public abstract void StartClient(string targetEnvironment, int targetPort);

        public abstract void StartServer(int serverPort);

        public void PropagateCommandToAllPeers(string command)
        {
            foreach (var childEndpoint in KnownChildEndpoints)
            {
                CommandRequestProtocol commandRequest =
                    new CommandRequestProtocol(ClientIpAddress.ToString(), ClientIpAddress.ToString(), command, false);

                SendMessage(childEndpoint, commandRequest);
            }

            if (KnownParentEndpoint != null)
            {
                CommandRequestProtocol commandRequest =
                    new CommandRequestProtocol(ClientIpAddress.ToString(), ClientIpAddress.ToString(), command, false);

                SendMessage(KnownParentEndpoint, commandRequest);
            }
        }

        public void HandleResponseDelegate(CommandRequestProtocol response)
        {
            string targetAddress = response.AddressStack.Pop();
            string? outgoingPeerAddress = KnownParentEndpoint?.Address?.ToString();

            Console.WriteLine("Trying to respond to {0}", targetAddress);
            Console.WriteLine("checking against parent {0}", outgoingPeerAddress);

            if (KnownParentEndpoint != null &&
                outgoingPeerAddress != null &&
                outgoingPeerAddress.Equals(targetAddress.ToString()))
            {
                Console.WriteLine("Sending response request to {0}", outgoingPeerAddress);
                SendMessage(KnownParentEndpoint, response);

                return;
            }

            foreach (var childEndpoint in KnownChildEndpoints)
            {
                string? incomingPeerAddress = childEndpoint.Address.ToString();

                Console.WriteLine("checking against child {0}", incomingPeerAddress);

                if (incomingPeerAddress.Equals(targetAddress.ToString()))
                {
                    Console.WriteLine("Sending response request to {0}", incomingPeerAddress);
                    SendMessage(childEndpoint, response);

                    return;
                }
            }
        }

        public void PropagateToKnownPeersWithoutLoop(CommandRequestProtocol request, IPAddress address)
        {
            string? outgoingPeerAddress = KnownParentEndpoint?.Address?.ToString();

            if (KnownParentEndpoint != null &&
                outgoingPeerAddress != null &&
                !outgoingPeerAddress.Equals(address.ToString()))
            {
                CommandRequestProtocol commandRequest =
                    new CommandRequestProtocol(
                        request.OriginatorAddress,
                        null,
                        new Stack<string>(request.AddressStack),
                        request.Message,
                        false);

                commandRequest.AddressStack.Push(ClientIpAddress.ToString());

                Console.WriteLine("Sending propagate request to {0}", outgoingPeerAddress);
                SendMessage(KnownParentEndpoint, commandRequest);
            }

            foreach (var childEndpoint in KnownChildEndpoints)
            {
                string? incomingPeerAddress = childEndpoint.Address.ToString();

                if (!incomingPeerAddress.Equals(address.ToString()))
                {
                    CommandRequestProtocol commandRequest =
                        new CommandRequestProtocol(
                            request.OriginatorAddress,
                            null,
                            new Stack<string>(request.AddressStack),
                            request.Message,
                            false);

                    commandRequest.AddressStack.Push(ClientIpAddress.ToString());

                    Console.WriteLine("Sending propagate request to {0}", incomingPeerAddress);
                    SendMessage(childEndpoint, commandRequest);
                }
            }
        }

        protected abstract void SendMessage<R> (T endpoint, R request);
    }
}
