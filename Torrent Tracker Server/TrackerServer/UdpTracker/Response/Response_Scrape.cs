using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Tracker_Server.UdpTracker
{
    public class Response_Scrape
    {
        public static void send(EndPoint remoteAddress, IChannelHandlerContext ctx, int transactionId, List<TorrentStats> torrentStatsList)
        {
            IByteBuffer msg = Utils.allocBuffer(4 + 4 + torrentStatsList.Count * 12);

            msg.WriteInt((int)Action.scrape);
            msg.WriteInt(transactionId);

            foreach (var torrentStat in torrentStatsList)
            {
                msg.WriteInt(torrentStat.complete);
                msg.WriteInt(torrentStat.downloaded);
                msg.WriteInt(torrentStat.incomplete);
            }
            torrentStatsList.Clear();
            torrentStatsList = null;
            try
            {
                if (ctx.Channel.Active && ctx.Channel.Open)
                    ctx.WriteAndFlushAsync(new DatagramPacket(msg, remoteAddress));
            }
            catch (IOException e)
            {
                Console.WriteLine($"Response_Scrape::Exception::{e}");
            }
        }
    }
}
