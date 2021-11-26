using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Tracker_Server.UdpTracker
{
    public class Response_Announce
    {
        public static void send(EndPoint remoteAddress, IChannelHandlerContext ctx, int transactionId
            , int interval, int leechers, int seeders, List<TorrentPeer> peers, TorrentPeer newerPeer)
        {
            //udp프로토콜 ipv6관련 내용.   https://www.bittorrent.org/beps/bep_0015.html


            //ipv4인 경우.
            if (remoteAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                //Console.WriteLine($"Response_Announce::From {remoteAddress}, ipv4");
            }
            //ipv6인 경우.
            else if (remoteAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                //Console.WriteLine($"Response_Announce::From {remoteAddress}, ipv6");
            }
            
            IByteBuffer msg = Utils.allocBuffer(4 + 4 + 4 + 4 + 4 + peers.Count * 6);

            msg.WriteInt((int)Action.announce);
            msg.WriteInt(transactionId);
            msg.WriteInt(interval);
            msg.WriteInt(leechers);
            msg.WriteInt(seeders);

            //ipv4의 경우, 4byte(ip) + 2byte(port) 합쳐서 총 6byte를 사용하나
            //ipv6의 경우, 16byte(ip) + 2byte(port) 합쳐서 총 18byte를 사용한다.
            foreach (var peer in peers)
            {
                //자기자신은 제외.
                if (peer.ip == newerPeer.ip && peer.port == newerPeer.port)
                    continue;

                msg.WriteInt((int)peer.ip);
                msg.WriteShort((short)peer.port);
            }
            peers.Clear();
            peers = null;

            try
            {
                if (ctx.Channel.Active && ctx.Channel.Open)
                    ctx.WriteAndFlushAsync(new DatagramPacket(msg, remoteAddress));
            }
            catch (IOException e)
            {
                Console.WriteLine($"Response_Announce::Exception::{e}");
            }
        }
    }
}
