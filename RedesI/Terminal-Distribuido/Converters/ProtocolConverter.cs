using Newtonsoft.Json.Linq;
using System.Text;

namespace Terminal_Distribuido.Converters
{
    public static class ProtocolConverter <T> 
    {
        public static byte[] ConvertPayloadToByteArray(T protocolObject)
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

        public static T? ConvertByteArrayToProtocol(byte[] byteArray, int bytesReceived)
        {
            if (byteArray == null)
            {
                Console.WriteLine("Byte array was null");
                return default(T);
            }

            string payload = Encoding.ASCII.GetString(byteArray, 0, bytesReceived);

            if (string.IsNullOrEmpty(payload))
            {
                Console.WriteLine("Byte array constains empty payload");
                return default(T);
            }

            T? protocolObject = JToken.Parse(payload).ToObject<T>();

            return protocolObject;
        }
    }
}
