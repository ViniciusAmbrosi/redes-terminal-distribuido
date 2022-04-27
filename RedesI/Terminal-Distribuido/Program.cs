
public class Program
{
    public static void Main(String[] args)
    {
        ClientManager p2pClient = new ClientManager();

        p2pClient.InitiateOutgoingSocketConnection();
        p2pClient.ListenForIncomingSocketConnections();

        p2pClient.ManageTerminalInput();
    }
}