using Newtonsoft.Json.Linq;
using System.Text;
using Terminal_Distribuido.Protocols;

namespace Terminal_Distribuido.Converters
{
    public static class ProtocolConverter
    {
        public static byte[] ConvertPayloadToByteArray(CommandRequestProtocol protocolObject)
        {
            if (protocolObject == null)
            {
                Console.WriteLine("Procol object was null");
                return new byte[0];
            }

            string payload = JToken.FromObject(protocolObject).ToString();
            byte[] payloadBytes = Encoding.ASCII.GetBytes(payload);

            return payloadBytes;
        }

        public static CommandRequestProtocol? ConvertByteArrayToProtocol(byte[] byteArray, int bytesReceived)
        {
            if (byteArray == null)
            {
                Console.WriteLine("Byte array was null");
                return null;
            }

            string payload = Encoding.ASCII.GetString(byteArray, 0, bytesReceived);

            if (string.IsNullOrEmpty(payload))
            {
                Console.WriteLine("Byte array constains empty payload");
                return null;
            }

            CommandRequestProtocol? protocolObject = JToken.Parse(payload).ToObject<CommandRequestProtocol>();

            return protocolObject;
        }
    }
}
