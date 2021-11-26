using System;

namespace Tracker_Server
{
    public class TorrentPeer
    {
        public DateTime lastUpdateTime;

        public string infoHash;
        public string peerId;
        public long downloaded;
        public long left;
        public long uploaded;
        public int eVent;  // completed = 1 / started = 2 / stopped = 3
        public uint ip;
        public ushort port;
        public int key;
        public int numWant;
        public short extensions; // 0 = none / 1 = authentication / 2 = request string

        //deep copy용 메소드.
        public void updateData(TorrentPeer recentlyPeer)
        {
            this.lastUpdateTime = recentlyPeer.lastUpdateTime;

            this.infoHash = recentlyPeer.infoHash;
            this.peerId = recentlyPeer.peerId;
            this.downloaded = recentlyPeer.downloaded;
            this.left = recentlyPeer.left;
            this.uploaded = recentlyPeer.uploaded;
            this.eVent = recentlyPeer.eVent;
            this.ip = recentlyPeer.ip;
            this.port = recentlyPeer.port;
            this.key = recentlyPeer.key;
            this.numWant = recentlyPeer.numWant;
            this.extensions = recentlyPeer.extensions;
        }
    }
}
