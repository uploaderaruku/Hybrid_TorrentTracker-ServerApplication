using System;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Tracker_Server.UdpTracker;

namespace Tracker_Server
{
    class UDP_TrackerServerHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            if (!(message is DatagramPacket))
            {
                ReferenceCountUtil.SafeRelease(message);
                ctx.CloseAsync();
                return;
            }

            DatagramPacket msg = (DatagramPacket)message;
            IByteBuffer receive_data = msg.Content;

            if (receive_data.ReadableBytes < 16 || receive_data.ReadableBytes > 4000)
            {
                Console.WriteLine($"잘못된 UDP Tracker 패킷을 수신. from:{msg.Sender}");
            }
            else
            {
                long connectionId = receive_data.ReadLong();
                Action actionId = (Action)receive_data.ReadInt();
                int transactionId = receive_data.ReadInt();

                ClientRequest request = null;

                switch (actionId)
                {
                    case Action.connect:
                        request = new Request_Connection();
                        break;
                    case Action.announce:
                        request = new Request_Announce();
                        break;
                    case Action.scrape:
                        request = new Request_Scrape();
                        break;
                    default:
                        Console.WriteLine($"잘못된 Action값이 전달됨. actionId => {actionId}");
                        Response_Error.send(msg.Sender, ctx, transactionId, "잘못된 Action값이 전달됨.");
                        break;
                }

                if (request != null)
                {
                    request.setContext(ctx);
                    request.setDatagram(msg);

                    request.setConnectionId(connectionId);
                    request.setAction(actionId);
                    request.setTransactionId(transactionId);

                    try
                    {
                        request.parsePacket();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"exception:{e}");
                    }
                }
            }

            msg.Release();
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine($"exception:{e}");
        }
    }
}
