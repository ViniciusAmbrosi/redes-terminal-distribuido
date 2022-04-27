
public class Program
{
    public static void Main(String[] args)
    {
        TCPClientManager p2pClient = new TCPClientManager();

        p2pClient.InitiateOutgoingSocketConnection();
        p2pClient.ListenForIncomingSocketConnections();

        p2pClient.ManageTerminalInput();
    }
}