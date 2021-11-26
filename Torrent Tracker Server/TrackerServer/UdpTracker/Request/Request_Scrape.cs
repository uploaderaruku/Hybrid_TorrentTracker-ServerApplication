using System;
using System.Collections.Generic;

namespace Tracker_Server.UdpTracker
{
    public class Request_Scrape : ClientRequest
    {
        public override void parsePacket()
        {
            List<TrackerSwarm> trackerSwarms = new List<TrackerSwarm>();

            while (this.getByteBuffer().ReadableBytes >= 20)
            {
                var infoHash_data = new byte[20];
                getByteBuffer().ReadBytes(infoHash_data, 0, infoHash_data.Length);
                var infoHash = Utils.bytesToStr(infoHash_data);

                var trackerSwarm = TrackerSwarmManager.instance().SearchTrackerSwarm(infoHash);

                trackerSwarms.Add(trackerSwarm);
                if (trackerSwarms.Count >= 74)
                {
                    break;
                }
            }

            List<TorrentStats> torrentStatsList = new List<TorrentStats>();

            foreach (var trackerSwarm in trackerSwarms)
            {
                var info = trackerSwarm.getCurrentSeedersAndLeechers();

                torrentStatsList.Add(new TorrentStats()
                {
                    downloaded = info.downloaded,
                    complete = info.complete,
                    incomplete = info.incomplete,
                    infoHash = Utils.strToBytes(trackerSwarm.hash_info)
                });
            }


            Response_Scrape.send(this.getDatagram().Sender, this.getContext(), this.getTransactionId(), torrentStatsList);
        }
    }
}
