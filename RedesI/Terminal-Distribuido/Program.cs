
using Terminal_Distribuido.Manager;

public class Program
{
    public static void Main(String[] args)
    {
        //TCPClientManager tcpClient = new TCPClientManager();

        //tcpClient.InitiateOutgoingSocketConnection();
        //tcpClient.ListenForIncomingSocketConnections();

        //tcpClient.ManageTerminalInput();

        UDPClientManager udpClient = new UDPClientManager();

        udpClient.InitiateOutgoingSocketConnection();
        udpClient.ListenForIncomingSocketConnections();

        udpClient.ManageTerminalInput();
    }
}