using System.Collections.Generic;
using DotNetty.Transport.Channels;

namespace Tracker_Server
{
    public class WebTorrentSession
    {
        public WebTorrentSession(IChannel socket)
        {
            this.socket = socket;
            this.info_hash_List = new Dictionary<string, string>();
        }

        public IChannel socket;
        public string realIP;
        public uint ip;
        public ushort port;
        public string session_id;
        public string peer_id;
        public Dictionary<string, string> info_hash_List;
    }
}
