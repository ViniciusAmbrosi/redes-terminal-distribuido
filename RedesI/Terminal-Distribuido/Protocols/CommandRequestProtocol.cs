
namespace Terminal_Distribuido.Protocols
{
    public class CommandRequestProtocol
    {
        public string OriginatorAddress { get; set; }

        public string ReplierAddress { get; set; }

        public Stack<string> AddressStack { get; set; }

        public string Message { get; set; }

        public bool IsResponse { get; set; }


        public CommandRequestProtocol(string originatorAddress, string targetAddress, string message, bool isResponse)
        {
            OriginatorAddress = originatorAddress;
            AddressStack = new Stack<string>();
            Message = message;
            IsResponse = isResponse;

            AddressStack.Push(targetAddress);
        }

        
        public CommandRequestProtocol(string originatorAddress, string replierAddress, Stack<string> targets, string message, bool isResponse)
        {
            OriginatorAddress = originatorAddress;
            ReplierAddress = replierAddress;
            AddressStack = targets;
            Message = message;
            IsResponse = isResponse;
        }

        public CommandRequestProtocol()
        {
        }
    }
}
