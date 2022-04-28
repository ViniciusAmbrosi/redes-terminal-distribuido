using Terminal_Distribuido.Manager;
using Terminal_Distribuido.Terminal;

public class Program
{
    public static void Main(String[] args)
    {
        TerminalManager terminalManager = new TerminalManager();

        TCPClientManager tcpClient = new TCPClientManager();

        tcpClient.ListenForIncomingSocketConnections();
        tcpClient.InitiateOutgoingSocketConnection();

        //Console.WriteLine("Started listening for requests");

        //tcpClient.ManageTerminalInput();

        UDPClientManager udpClient = new UDPClientManager();

        udpClient.ListenForIncomingSocketConnections();
        udpClient.InitiateOutgoingSocketConnection();

        //Console.WriteLine("Started listening for requests");

        //udpClient.ManageTerminalInput();

        while (true)
        {
            string? message = Console.ReadLine();

            if (!String.IsNullOrEmpty(message))
            {
                string commandResult = terminalManager.ExecuteCommand(message);

                Console.WriteLine("\n-- -- -- -- -- -- -- -- -- -- -- -- --");
                Console.WriteLine("\nSource system result");
                Console.WriteLine($"{commandResult}\n");

                tcpClient.PropagateCommandToAllPeers(message);
                udpClient.PropagateCommandToAllPeers(message);
            }
        }
    }
}