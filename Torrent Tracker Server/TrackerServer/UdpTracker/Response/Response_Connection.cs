using System;
using System.IO;
using System.Net;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Tracker_Server.UdpTracker
{
    public class Response_Connection
    {
        public static void send(EndPoint remoteAddress, IChannelHandlerContext ctx, int transactionId, long connectionId)
        {
            IByteBuffer msg = Utils.allocBuffer(4 + 4 + 8);

            msg.WriteInt((int)Action.connect);
            msg.WriteInt(transactionId);
            msg.WriteLong(connectionId);
            
            try
            {
                if (ctx.Channel.Active && ctx.Channel.Open)
                    ctx.WriteAndFlushAsync(new DatagramPacket(msg, remoteAddress));
            }
            catch (IOException e)
            {
                Console.WriteLine($"Response_Connection::Exception::{e}");
            }
        }
    }
}
