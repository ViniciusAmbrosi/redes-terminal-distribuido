
using System.Net;
using Terminal_Distribuido.Protocols;

namespace Terminal_Distribuido.Manager
{
    public class UDPClientManager : BaseClientManager
    {
        public UDPClientManager() :
            base()
        {
        }

        public override void StartClient(string targetEnvironment, int targetPort)
        {
            throw new NotImplementedException();
        }

        public override void StartServer(int serverPort)
        {
            throw new NotImplementedException();
        }

        public override void HandleResponseDelegate(CommandRequestProtocol response)
        {
            throw new NotImplementedException();
        }

        public override void PropagateCommandToAllPeers(string command)
        {
            throw new NotImplementedException();
        }

        public override void PropagateToKnownPeersWithoutLoop(CommandRequestProtocol request, IPAddress address)
        {
            throw new NotImplementedException();
        }
    }
}
