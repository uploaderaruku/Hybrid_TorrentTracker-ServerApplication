using System;
using System.Collections.Generic;
using System.Net;
using DotNetty.Buffers;

namespace Tracker_Server.UdpTracker
{
    public class Request_Announce : ClientRequest
    {
        public override void parsePacket()
        {
            IByteBuffer msg = this.getByteBuffer();

            //excluded connectionId(8), actionId(4), transactionId(4) bytes. (16bytes)


            //Transmission request 82byte.
            //Tixati, uTorrent request 84byte,
            //another client request 93byte.
            if (msg.ReadableBytes < 20 + 20 + 8 + 8 + 8 + 4 + 4 + 4 + 4 + 2) //minimum 82bytes Length Required.
            {
                Response_Error.send(this.getDatagram().Sender, this.getContext(), 
                    this.getTransactionId(), "잘못된 Announce packet 입니다.");
                return;
            }

            TorrentPeer newerPeer = new TorrentPeer();

            //get and set CurrentTime
            newerPeer.lastUpdateTime = DateTime.Now;

            byte[] infoHash = new byte[20];
            msg.ReadBytes(infoHash, 0, infoHash.Length);
            newerPeer.infoHash = Utils.bytesToStr(infoHash);
            
            byte[] peerId = new byte[20];
            msg.ReadBytes(peerId, 0, peerId.Length);
            newerPeer.peerId = Utils.bytesToStr(peerId);

            newerPeer.downloaded = msg.ReadLong();
            newerPeer.left = msg.ReadLong();
            newerPeer.uploaded = msg.ReadLong();
            newerPeer.eVent = msg.ReadInt();
            newerPeer.ip = (uint)msg.ReadInt(); //ipv4인경우에만 사용. ipv6이라면 항상 0으로 취급함.
            newerPeer.key = msg.ReadInt();
            newerPeer.numWant = msg.ReadInt();
            newerPeer.port = (ushort)msg.ReadShort();

            //84byte 이상 받아온 경우에만 활용
            if(msg.ReadableBytes >= 2)
                newerPeer.extensions = msg.ReadShort();

            // if (newerPeer.extensions == 1)
            // {
            //    
            // }

            int maxNumWant = TrackerServer_Configure.MaxNumWant;

            //최대 피어수 세팅.
            if (newerPeer.numWant <= 0 || newerPeer.numWant > maxNumWant)
            {
                newerPeer.numWant = maxNumWant;
            }
            
            //ip숫자 세팅
            if (newerPeer.ip == 0) {
                IPEndPoint remoteIpEndPoint = this.getDatagram().Sender as IPEndPoint;
                newerPeer.ip = (uint)IPAddressConverter.ToInt(remoteIpEndPoint.Address);
            }
            
            //TrackerSwarm 불러오기.
            var trackerSwarm = TrackerSwarmManager.instance().SearchTrackerSwarm(newerPeer.infoHash);
            
            trackerSwarm.announceTorrentPeerInfo(newerPeer);
            
            int announceInterval = TrackerServer_Configure.Interval;

            var info = trackerSwarm.getCurrentSeedersAndLeechers();
            int leechers = info.incomplete;
            int seeders = info.complete;

            List<TorrentPeer> peers = trackerSwarm.getCurrentPeers(newerPeer.numWant, newerPeer.peerId, newerPeer.left == 0);

            Response_Announce.send(this.getDatagram().Sender, this.getContext(), 
                this.getTransactionId(), announceInterval, leechers, seeders, peers, newerPeer);
        }
    }
}
