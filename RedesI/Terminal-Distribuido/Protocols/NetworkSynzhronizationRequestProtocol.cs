﻿
namespace Terminal_Distribuido.Protocols
{
    public class NetworkSynzhronizationRequestProtocol : RequestProtocol
    {
        public string RealIpAddress { get; set; }

        public NetworkSynzhronizationRequestProtocol(string realIpAddress) :
            base(RequestType.AddressSynchronization)
        {
            RealIpAddress = realIpAddress;
        }

        public NetworkSynzhronizationRequestProtocol()
        {
        }
    }
}
