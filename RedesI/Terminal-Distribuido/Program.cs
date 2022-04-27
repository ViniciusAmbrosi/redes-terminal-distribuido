
public class Program
{
    public static void Main(String[] args)
    {
        ClientManager p2PClient = new ClientManager();

        p2PClient.InitiateOutgoingSocketConnection();
        p2PClient.ListenForIncomingSocketConnections();

        p2PClient.ManageTerminalInput();
    }
}