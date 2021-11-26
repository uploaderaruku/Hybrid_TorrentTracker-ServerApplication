using System;
using System.Linq;
using System.Web;
using System.Net;
using System.Text;
using System.Net.Http;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Codecs.Http.WebSockets;

namespace Tracker_Server
{
    public class Utils
    {
        static Random random = new Random();

        //Utils.allocBuffer
        public static IByteBuffer allocBuffer(int size, int maxCapacity = 0)
        {
            int customSize = 0;

            if (size == 0)
            {
                customSize = 0;
            }
            else if (size <= 64)
            {
                customSize = 64;
            }
            else if (size <= 256)
            {
                customSize = 256;
            }
            else if (size <= 512)
            {
                customSize = 512;
            }
            else if (size <= 1024)
            {
                customSize = 1024;
            }
            else if (size <= 2048)
            {
                customSize = 2048;
            }
            else if (size <= 4096)
            {
                customSize = 4096;
            }
            else if (size <= 8192)
            {
                customSize = 8192;
            }
            else if (size <= 16384)
            {
                customSize = 16384;
            }
            else if (size <= 32768)
            {
                customSize = 32768;
            }
            else if (size <= 65536)
            {
                customSize = 65536;
            }
            else
            {
                customSize = size;
            }

            if(maxCapacity > 0)
                return PooledByteBufferAllocator.Default.DirectBuffer(customSize, maxCapacity);
            else if (customSize == 0 && maxCapacity == 0)
                return PooledByteBufferAllocator.Default.DirectBuffer(0, 0); //empty buffer
            else
                return PooledByteBufferAllocator.Default.DirectBuffer(customSize, Int32.MaxValue);
        }

        public static string ByteToHex(byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes);
            return hex.Replace("-", string.Empty);
        }

        public static string ByteToHex(IByteBuffer bytes)
        {
            byte[] data = new byte[bytes.ReadableBytes];
            bytes.GetBytes(0, data);

            return ByteToHex(data);
        }

        public static byte[] HexToByte(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];
            int[] HexValue = new int[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x0C, 0x0D,
                0x0E, 0x0F };

            for (int x = 0, i = 0; i < hexString.Length; i += 2, x += 1)
            {
                bytes[x] = (byte)(HexValue[Char.ToUpper(hexString[i + 0]) - '0'] << 4 |
                                  HexValue[Char.ToUpper(hexString[i + 1]) - '0']);
            }

            return bytes;
        }

        public static IByteBuffer HexToByteBuf(string hexString)
        {
            byte[] data = HexToByte(hexString);
            IByteBuffer buffer = Utils.allocBuffer(data.Length);
            buffer.WriteBytes(data);
            return buffer;
        }


        public static IByteBuffer ByteToByteBuf(byte[] data)
        {
            IByteBuffer buffer = Utils.allocBuffer(data.Length);
            buffer.WriteBytes(data);
            return buffer;
        }

        public static byte[] ByteBufToByte(IByteBuffer data)
        {
            byte[] buffer = new byte[data.ReadableBytes];
            data.ReadBytes(buffer);
            return buffer;
        }
        
        //For Performance Code
        public static byte[] strToBytes(string str_data)
        {
            char[] charArr = str_data.ToCharArray();
            byte[] resultArr = new byte[charArr.Length];
            Buffer.BlockCopy(charArr, 0, resultArr, 0, resultArr.Length);
            return resultArr;
        }

        public static string bytesToStr(byte[] bytes)
        {
            char[] charArr = new char[bytes.Length];
            Buffer.BlockCopy(bytes, 0, charArr, 0, charArr.Length);
            return new string(charArr);
        }

        //BitTorrent Confirmed.
        public static byte[] strToBytes_original(string str_data)
        {
            char[] charArr = str_data.ToCharArray();
            byte[] resultArr = charArr.Select(c => (byte)c).ToArray();
            return resultArr;
        }

        public static string bytesToStr_original(byte[] bytes)
        {
            char[] charArr = bytes.Select(b => (char)b).ToArray();
            return new string(charArr);
        }

        public static string TextWebSocketFrameToString(WebSocketFrame frame)
        {
            byte[] data = new byte[frame.Content.ReadableBytes];
            frame.Content.ReadBytes(data);
            string Str = Encoding.UTF8.GetString(data);
            data = null;
            return Str;
        }

        public static TextWebSocketFrame TextWebSocketFrameFromString(string stringData)
        {
            byte[] data = Encoding.UTF8.GetBytes(stringData);
            IByteBuffer buffer = Utils.allocBuffer(data.Length);
            buffer.WriteBytes(data);
            data = null;
            return new TextWebSocketFrame(buffer);
        }

        public static TextWebSocketFrame TextWebSocketFrameFromByteArray(byte[] data)
        {
            IByteBuffer buffer = Utils.allocBuffer(data.Length);
            buffer.WriteBytes(data);
            return new TextWebSocketFrame(buffer);
        }
        
        public static string getSessionIDFromChannel(IChannel channel)
        {
            var remoteEndPoint = (IPEndPoint) channel.RemoteAddress;

            var ip = (uint)IPAddressConverter.ToInt(remoteEndPoint.Address);
            var port = (ushort)remoteEndPoint.Port;
            
            return $"{ip}:{port}";
        }

        public static string getCombinePeerID(string sessionID, string peerID)
        {
            var info = sessionID.Split(':');
            int ip = (int)uint.Parse(info[0]);
            ushort port = ushort.Parse(info[1]);
            
            IByteBuffer buf = Utils.allocBuffer(9);

            buf.WriteByte(random.Next(0, 255));
            buf.WriteInt(ip);
            buf.WriteShort(random.Next(0, 65535));
            buf.WriteUnsignedShortLE(port);

            Span<byte> spanData = buf.Array.AsSpan().Slice(buf.ArrayOffset, buf.ReadableBytes);

            var base64 = Convert.ToBase64String(spanData).AsSpan();
            //var base64 = Convert.ToBase64String(Utils.ByteBufToByte(buf));

            var clientInfo = peerID.AsSpan(0, 8);
            //var clientInfo = peerID.Substring(0, 8);

            buf.Release();

            Span<char> resultSpan = stackalloc char[20];

            clientInfo.CopyTo(resultSpan.Slice(0, 8));
            base64.CopyTo(resultSpan.Slice(8, 12));
            string result = resultSpan.ToString();
            
            if (result.Length != 20)
                throw new Exception("invalid combine peer id.");

            return result;
        }


        public static T getJsonValue<T>(dynamic jobject, string key)
        {
            try
            {
                if (jobject.ContainsKey(key))
                    return (T)jobject[key];
                else
                    return default(T);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception::getJsonValue<{typeof(T).Name}>( key:{key} / value:{jobject[key]} )");
                return default(T);
            }
        }

        public static async void sendTelegram(string message)
        {
            if (!TrackerServer_Configure.Telegram_Enable)
                return;

            if (string.IsNullOrEmpty(TrackerServer_Configure.Telegram_API_Key))
                return;

            if (string.IsNullOrEmpty(TrackerServer_Configure.Telegram_TargetChatID))
                return;

            string TelegramRequestURL = 
             $"https://api.telegram.org/" +
             $"{TrackerServer_Configure.Telegram_API_Key}/sendmessage" +
             $"?chat_id={TrackerServer_Configure.Telegram_TargetChatID}" +
             $"&parse_mode={TrackerServer_Configure.Telegram_parse_mode}" +
             $"&disable_web_page_preview={TrackerServer_Configure.Telegram_disable_web_page_preview}" +
             $"&text=";

            using (HttpClient client = new HttpClient())
            {
                var replaceMessage = message.Replace("_", " ");
                replaceMessage = replaceMessage.Replace("*", " ");
                replaceMessage = replaceMessage.Replace("@", " ");
                replaceMessage = replaceMessage.Replace("&", " ");
                replaceMessage = replaceMessage.Replace("`", " ");

                try
                {
                    var data = $"{TelegramRequestURL}{HttpUtility.UrlEncode(replaceMessage)}";

                    var result = await client.GetAsync(data);

                    var response = await result.Content.ReadAsStringAsync();

                    if (response.Contains("\"ok\":false"))
                        Console.WriteLine($"Send Telegram Failed. \nresponse: {response}\n\nmessage: {replaceMessage}");

                    result.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"sendTelegramException: {e}");
                }
            }
        }
    }
}
