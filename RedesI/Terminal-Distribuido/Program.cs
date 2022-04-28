using Terminal_Distribuido.Manager;
using Terminal_Distribuido.Terminal;

public class Program
{
    public static void Main(String[] args)
    {
        //TCPClientManager client = new TCPClientManager();
        UDPClientManager client = new UDPClientManager();

        client.ListenForIncomingSocketConnections();
        client.InitiateOutgoingSocketConnection();

        client.ManageTerminalInput();
    }
}