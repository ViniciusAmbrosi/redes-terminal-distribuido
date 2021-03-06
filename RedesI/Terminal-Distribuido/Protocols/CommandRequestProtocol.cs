
using System.Text.Json.Serialization;
using Terminal_Distribuido.Converters;

namespace Terminal_Distribuido.Protocols
{
    public class CommandRequestProtocol : RequestProtocol
    {
        public string OriginatorAddress { get; set; }

        public string? ReplierAddress { get; set; }

        public Stack<string> AddressStack { get; set; }

        public string Message { get; set; }

        public CommandRequestProtocol(string originatorAddress, string targetAddress, string message, bool isResponse) :
            base(RequestType.Command, isResponse)
        {
            OriginatorAddress = originatorAddress;
            AddressStack = new Stack<string>();
            Message = message;

            AddressStack.Push(targetAddress);
        }
        
        public CommandRequestProtocol(string originatorAddress, string replierAddress, Stack<string> targets, string message, bool isResponse) :
            base(RequestType.Command, isResponse)
        {
            OriginatorAddress = originatorAddress;
            ReplierAddress = replierAddress;
            AddressStack = targets;
            Message = message;
        }

        public void ReorderStack()
        { 
            Stack<string> newStack = new Stack<string>();

            while (AddressStack.Count > 0)
            {
                newStack.Push(AddressStack.Pop());
            }

            AddressStack = newStack;
        }

        public CommandRequestProtocol() :
            base(RequestType.Command, false)
        {
        }
    }
}
