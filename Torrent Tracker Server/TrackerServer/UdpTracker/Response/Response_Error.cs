using System;
using System.IO;
using System.Net;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Tracker_Server.UdpTracker
{
    public class Response_Error
    {
        public static void send(EndPoint remoteAddress, IChannelHandlerContext ctx, int transactionId, string message)
        {
            byte[] messageData = Encoding.UTF8.GetBytes(message);

            IByteBuffer msg = Utils.allocBuffer(4 + 4 + messageData.Length + 2);

            msg.WriteInt((int) Action.error);
            msg.WriteInt(transactionId);
            msg.WriteBytes(messageData);
            msg.WriteByte(0);
            msg.WriteByte(0);

            try
            {
                if (ctx.Channel.Active && ctx.Channel.Open)
                    ctx.WriteAndFlushAsync(new DatagramPacket(msg, remoteAddress));
            }
            catch (IOException e)
            {
                Console.WriteLine($"Response_Error::Exception::{e}");
            }
        }
    }
}
