using Fleck;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace TPSConnectionManager
{
    public static class Extension
    {

        public static string ClientAddressAndPort(this IWebSocketConnection socket)
        {
            return socket.ConnectionInfo.ClientIpAddress.ToString() + ":" + socket.ConnectionInfo.ClientPort.ToString();
        }

        public static bool IsSameSocket(this IWebSocketConnection distionarySocket, IWebSocketConnection actualSocket)
        {
            return ClientAddressAndPort(distionarySocket) == ClientAddressAndPort(actualSocket);
        }

        public static bool IsSameSocket(this IWebSocketConnection distionarySocket, string ClientAdressAndPort)
        {
            return ClientAddressAndPort(distionarySocket) == ClientAdressAndPort;
        }

        public static string ConvertNullToEmptyString(this string str)
        {
            return (String.IsNullOrEmpty(str)) ? "" : str;
        }

        public static T DeepClone<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
        public static T Default<T>(this T obj) where T : class, new()
        {
            return obj ?? new T();
        }
    }
}
