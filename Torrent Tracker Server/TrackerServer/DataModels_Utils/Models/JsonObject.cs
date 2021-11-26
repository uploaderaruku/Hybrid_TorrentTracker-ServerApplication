using System.Collections.Generic;

namespace Tracker_Server
{
    public class JsonObject : Dictionary<string, dynamic>
    {
        
    }

    #region Scrape
    
    public class Response_Scrape_InfoHash
    {
        public string action { get; set; }
        public dynamic files { get; set; } //JsonObject or List<JsonObject>
        public Flag_ flags { get; set; }
    }


    public class FileObj_
    {
        public int complete { get; set; }
        public int incomplete { get; set; }
        public int downloaded { get; set; }
    }

    public class Flag_
    {
        public int min_request_interval { get; set; }
    }

    #endregion


    #region Announce

    public class Response_PeersInfo
    {
        public string action { get; set; }
        public int complete { get; set; }
        public int incomplete { get; set; }
        public int interval { get; set; }
        public string info_hash { get; set; }
    }

    public class Response_Offer
    {
        public string action { get; set; }
        public dynamic offer { get; set; }
        public string offer_id { get; set; }
        public string peer_id { get; set; }
        public string info_hash { get; set; }
    }

    public class Response_Answer
    {
        public string action { get; set; }
        public dynamic answer { get; set; }
        public string offer_id { get; set; }
        public string peer_id { get; set; }
        public string info_hash { get; set; }
    }

    public class SDPItem
    {
        public string type { get; set; } //offer, answer
        public string sdp { get; set; }
    }

    #endregion
}