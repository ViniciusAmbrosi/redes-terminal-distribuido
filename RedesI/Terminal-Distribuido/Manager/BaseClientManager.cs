
using System.Net;
using Terminal_Distribuido.Protocols;
using Terminal_Distribuido.Terminal;

namespace Terminal_Distribuido.Manager
{
    public abstract class BaseClientManager
    {
        protected TerminalManager TerminalManager { get; set; }
        protected IPAddress ClientIpAddress { get; set; }
        protected RequestManager RequestManager { get; set; }

        protected BaseClientManager()
        {
            this.TerminalManager = new TerminalManager();

            this.ClientIpAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];

            this.RequestManager = new RequestManager(HandleResponseDelegate, PropagateToKnownPeersWithoutLoop, TerminalManager);
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
            Console.WriteLine("Provide target environment IP address");
            string? targetEnvironment = Console.ReadLine();

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

            Thread maintainedSocketThread = new Thread(() => StartServer(port));
            maintainedSocketThread.Start();
        }

        public abstract void StartClient(string targetEnvironment, int targetPort);

        public abstract void StartServer(int serverPort);

        public abstract void PropagateCommandToAllPeers(string command);

        public abstract void HandleResponseDelegate(CommandRequestProtocol response);

        public abstract void PropagateToKnownPeersWithoutLoop(CommandRequestProtocol request, IPAddress address);
    }
}
