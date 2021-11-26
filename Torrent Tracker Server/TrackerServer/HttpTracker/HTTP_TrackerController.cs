using System;
using System.Web;
using BencodeNET.Objects;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DotNetty.Buffers;

namespace Tracker_Server.HttpTracker
{
    public class HTTP_TrackerController : ControllerBase
    {
        // /announce
        //
        // ?info_hash=s%AAM%C7k%23%E7%14%C8x%AAA%C0%E88%0As%B3W%FE
        // &peer_id=TIX0284-j3f3f1i3f3a7
        // &port=13273
        // &uploaded=0
        // &downloaded=0
        // &left=0
        // &corrupt=0
        // &key=9M4P9P7F
        // &event=started
        // &numwant=100
        // &compact=1
        // &no_peer_id=1

        static StatusCodeResult dummy = new StatusCodeResult(400);

        [HttpGet("announce")] //=> http://localhost/announce
        public async Task<IActionResult> HttpTrackerAnnounce(string info_hash, string peer_id, ushort port, 
            long uploaded, long downloaded, long left, int numwant, int key, int no_peer_id, int compact, int Event)
        {
            //check validate torrent announce request.
            //if not, ignore request.
            if (string.IsNullOrEmpty(info_hash) || string.IsNullOrEmpty(peer_id))
            {
                return dummy;
            }

            byte[] Info_hash_data = HttpUtility.UrlDecodeToBytes(info_hash);
            byte[] Peer_id_data = HttpUtility.UrlDecodeToBytes(peer_id);
            string Info_hash = Utils.bytesToStr(Info_hash_data);
            string Peer_id = Utils.bytesToStr(Peer_id_data);

            //check validate torrent announce request.
            //if not, ignore request.
            if (Info_hash.Length != 20 || Peer_id.Length != 20)
            {
                return dummy;
            }

            string torrentClient = null;

            //get torrentClient info From http request header, 'User-Agent' parameter.
            var headers = Request.Headers;
            if (headers.ContainsKey("User-Agent"))
            {
                torrentClient = headers["User-Agent"];
            }

            var remote_addr = Request.HttpContext.Connection.RemoteIpAddress;

            var trackerSwarm = TrackerSwarmManager.instance().SearchTrackerSwarm(Info_hash);

            //parse TorrentPeer info from parameter in http GET request data.
            TorrentPeer newerPeer = new TorrentPeer()
            {
                infoHash = Info_hash,
                peerId = Peer_id,
                downloaded = downloaded,
                left = left,
                uploaded = uploaded,
                eVent = Event,
                ip = (uint)IPAddressConverter.ToInt(remote_addr),
                port = port,
                key = key,
                numWant = numwant,
                extensions = 0,
                lastUpdateTime = DateTime.Now
            };

            trackerSwarm.announceTorrentPeerInfo(newerPeer, true);
            
            //create Bencode based announce response.
            BDictionary bencodingDictionary = new BDictionary();

            var info = trackerSwarm.getCurrentSeedersAndLeechers();
            bencodingDictionary.Add("complete", info.complete);
            bencodingDictionary.Add("downloaded", info.downloaded);
            bencodingDictionary.Add("incomplete", info.incomplete);
            bencodingDictionary.Add("interval", TrackerServer_Configure.Interval);
            bencodingDictionary.Add("min interval", TrackerServer_Configure.Interval);

            if (numwant > TrackerServer_Configure.MaxNumWant)
            {
                numwant = TrackerServer_Configure.MaxNumWant;
            }


            var peers = trackerSwarm.getCurrentPeers(numwant, Peer_id, newerPeer.left == 0);

            IByteBuffer buffers = Utils.allocBuffer(peers.Count * 6);

            int peersCount = 0;
            foreach (var peer in peers)
            {
                //자기자신은 제외.
                if(peer.ip == newerPeer.ip && peer.port == newerPeer.port)
                    continue;

                buffers.WriteInt((int)peer.ip);
                buffers.WriteShort((short)peer.port);

                peersCount++;

                //max 피어수를 넘기면 그 이후는 cut.
                if (numwant <= peersCount)
                    break;
            }

            peers.Clear();
            peers = null;


            byte[] peers_data = new byte[buffers.ReadableBytes];
            buffers.ReadBytes(peers_data);
            buffers.Release();

            bencodingDictionary.Add("peers", new BString(peers_data) ); //ipv4 peers Add.
            bencodingDictionary.Add("peers6", string.Empty); //ipv6 not support

            return File(bencodingDictionary.EncodeAsBytes(), "application/octet-stream");
        }



        [HttpGet("scrape")] //=> http://localhost/scrape
        public async Task<IActionResult> HttpTrackerScrape(string info_hash)
        {
            //check validate torrent announce request.
            //if not, ignore request.
            if (string.IsNullOrEmpty(info_hash))
            {
                return dummy;
            }

            byte[] Info_hash_data = HttpUtility.UrlDecodeToBytes(info_hash);
            string Info_hash = Utils.bytesToStr(Info_hash_data);

            //check validate torrent announce request.
            //if not, ignore request.
            if (Info_hash.Length != 20)
            {
                return dummy;
            }
            
            var trackerSwarm = TrackerSwarmManager.instance().SearchTrackerSwarm(Info_hash);

            //create Bencode based announce response.
            BDictionary bencodingDictionary = new BDictionary();

            var info = trackerSwarm.getCurrentSeedersAndLeechers();
            bencodingDictionary.Add("complete", info.complete);
            bencodingDictionary.Add("downloaded", info.downloaded);
            bencodingDictionary.Add("incomplete", info.incomplete);

            return File(bencodingDictionary.EncodeAsBytes(), "application/octet-stream");
        }
    }
}
