﻿using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Terminal_Distribuido.Sockets;
using Terminal_Distribuido.Terminal;

public class P2PClient
{
    public static OutgoingPersistentSocket? outgoingPeer;
    public static ConcurrentDictionary<string, IncomingPersistentSocket> incomingPeers = new ConcurrentDictionary<string, IncomingPersistentSocket>();

    private static SemaphoreSlim Semaphore = new SemaphoreSlim(1);

    private static TerminalManager terminalManager = new TerminalManager();

    public class Hello {
        public Hello(string command, bool isRequest, bool isResponse)
        {
            this.command = command;
            this.isRequest = isRequest;
            this.isResponse = isResponse;
        }

        public string command { get; set; } 
        public bool isRequest { get; set; }
        public bool isResponse { get; set; }
    }

    public static int Main(String[] args)
    {
        InitiateOutgoingSocketConnection();
        ListenForIncomingSocketConnections();

        while (true)
        {
            string? message = Console.ReadLine();
            Hello hello = new Hello(message, true, false);
            
            byte[] msg = Encoding.ASCII.GetBytes(JToken.FromObject(hello).ToString());

            string commandResult = terminalManager.ExecuteCommand(message);
            Console.WriteLine("Source system result");
            Console.WriteLine($"{commandResult}\n");

            // Propagate data to all sockets
            PropagateOutgoingMessage(msg);
        }
    }

    public static void InitiateOutgoingSocketConnection()
    {
        Console.WriteLine("Provide target environment IP address");
        string? targetEnvironment = Console.ReadLine();

        Console.WriteLine("Provide target environment port");
        string? targetPort = Console.ReadLine();

        StartClient(targetEnvironment, targetPort);
    }

    public static void ListenForIncomingSocketConnections()
    {
        Console.WriteLine("Provide this server port for socket server");
        string? serverPort = Console.ReadLine();

        Thread maintainedSocketThread = new Thread(() => StartServer(serverPort));
        maintainedSocketThread.Start();
    }

    public static void StartClient(string? targetEnvironment, string? targetPort)
    {
        int port = 0;
        if (String.IsNullOrEmpty(targetEnvironment) || String.IsNullOrEmpty(targetPort) || !int.TryParse(targetPort, out port)) 
        {
            Console.WriteLine("No target IP or port provided, continuing as client only.");
            return;
        }

        try
        {
            IPAddress ipAddress = IPAddress.Parse(targetEnvironment);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                sender.Connect(remoteEP);

                Console.WriteLine("Socket connected to {0}", sender?.RemoteEndPoint?.ToString());

                OutgoingPersistentSocket persistent = new OutgoingPersistentSocket(sender, ipAddress, PropagateIncomingSocketMessageDelegate, terminalManager);

                persistent.CreateMonitoringThread();
                outgoingPeer = persistent;
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public static void StartServer(string? serverPort)
    {
        int port = 0;
        if (String.IsNullOrEmpty(serverPort) || !int.TryParse(serverPort, out port))
        {
            Console.WriteLine("Server port not provided. Interrupting flow.");
            return;
        }

        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
        IPAddress ipAddress = host.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

        try
        {
            // Create a Socket that will use Tcp protocol
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // A Socket must be associated with an endpoint using the Bind method

            listener.Bind(localEndPoint);
            listener.Listen(10); // We will listen 10 requests at a time

            Console.WriteLine("Started listening for requests");
            Socket socket;

            while (true)
            {
                socket = listener.Accept();
                IPAddress sourceIp = IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString());
                
                Console.WriteLine("\nReceived connection request from {0}", sourceIp.ToString());

                IncomingPersistentSocket persistentSocket  = new IncomingPersistentSocket(
                    socket,
                    sourceIp,
                    PropagateIncomingSocketMessageDelegate,
                    terminalManager);

                persistentSocket.CreateMonitoringThread();
                incomingPeers.TryAdd(sourceIp.ToString(), persistentSocket);
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\n Press any key to continue...");
        Console.ReadKey();
    }

    // Create a method for a delegate.
    public static void PropagateOutgoingMessage(byte[] message)
    {
        try
        {
            Semaphore.Wait();

            foreach (var clientEntry in incomingPeers)
            {
                PersistentSocket targetSocket = clientEntry.Value;
                targetSocket.SendMessage(message);
            }

            if (outgoingPeer != null)
            {
                outgoingPeer.SendMessage(message);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public static void PropagateIncomingSocketMessageDelegate(byte[] message, IPAddress address)
    {
        try
        {
            Semaphore.Wait();

            foreach (var incomingPeer in incomingPeers)
            {
                string? incomingPeerAddress = incomingPeer.Value.Address.ToString();
                if (!incomingPeerAddress.Equals(address.ToString()))
                {
                    incomingPeer.Value.SendMessage(message);
                }
                else
                {
                    Console.WriteLine("Skipping Loop. From {0} | To {1}", address.ToString(), incomingPeerAddress);
                }
            }

            string? outgoingPeerAddress = outgoingPeer?.Address?.ToString();
            if (outgoingPeer != null && outgoingPeerAddress != null && !outgoingPeerAddress.Equals(address.ToString()))
            {
                outgoingPeer.SendMessage(message);
            }
            else 
            {
                Console.WriteLine("Skipping Loop. From {0} | To {1}", address.ToString(), outgoingPeerAddress);
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}