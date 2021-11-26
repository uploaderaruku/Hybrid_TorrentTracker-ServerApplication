namespace Tracker_Server
{
    public class WebTorrentPeer
    {
        public WebTorrentSession session;
        
        public string infoHash;
        public long downloaded;
        public long left;
        public long uploaded;
        public string eVent;  // completed = 1 / started = 2 / stopped = 3
        public string key;
        public int numWant;

        //deep copy용 메소드.
        public void updateData(WebTorrentPeer recentlyPeer)
        {
            this.infoHash = recentlyPeer.infoHash;
            this.downloaded = recentlyPeer.downloaded;
            this.left = recentlyPeer.left;
            this.uploaded = recentlyPeer.uploaded;
            this.eVent = recentlyPeer.eVent.ToLower();
            this.key = recentlyPeer.key;
            this.numWant = recentlyPeer.numWant;
        }

        public bool isCompleteDownload
        {
            get
            {
                return this.left == 0;
            }
        }
    }
}