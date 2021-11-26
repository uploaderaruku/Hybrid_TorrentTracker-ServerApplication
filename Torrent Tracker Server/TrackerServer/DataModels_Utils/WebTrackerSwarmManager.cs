using System;
using System.Linq;
using System.Collections.Generic;
using Tracker_Server.WebsocketTracker;

namespace Tracker_Server
{
    public class WebTrackerSwarmManager
    {
        #region instance

        static WebTrackerSwarmManager ins;

        public static WebTrackerSwarmManager instance()
        {
            if (ins == null)
                ins = new WebTrackerSwarmManager();

            return ins;
        }

        public WebTrackerSwarmManager()
        {
            this.trackerSwarmDictionary = new Dictionary<string, WebTrackerSwarm>();
        }

        #endregion

        //hash_info 정보에 따라서 TrackerSwarm 검색 진행.
        Dictionary<string, WebTrackerSwarm> trackerSwarmDictionary;

        public void Init()
        {
            //일정주기로 실행되는 이벤트 delegate 등록.
            TorrentTrackerServer.EventTasks += (null_obj, info_dictionary) =>
            {
                var totalPeerInfo = this.getCurrentTotalPeerInfo();

                lock (info_dictionary)
                {
                    if (!info_dictionary.ContainsKey("WebTrackerSwarmManager"))
                    {
                        JsonObject obj = new JsonObject();
                        obj.Add("TotalSeeder", totalPeerInfo.complete);
                        obj.Add("TotalLeecher", totalPeerInfo.incomplete);
                        obj.Add("TotalTorrentFiles", this.trackerSwarmDictionary.Count);

                        info_dictionary.Add("WebTrackerSwarmManager", obj);
                    }
                    else
                    {
                        var tracker = info_dictionary["WebTrackerSwarmManager"];
                        tracker["TotalSeeder"] = totalPeerInfo.complete;
                        tracker["TotalLeecher"] = totalPeerInfo.incomplete;
                        tracker["TotalTorrentFiles"] = this.trackerSwarmDictionary.Count;
                    }
                }

                Console.WriteLine($"WebTrackerSwarmManager TotalTorrentFiles:{this.trackerSwarmDictionary.Count} " +
                                  $"=> TotalSeeder:{totalPeerInfo.complete} / TotalLeecher:{totalPeerInfo.incomplete}");
            };

            Console.WriteLine("WebTrackerSwarmManager initialize.");
        }

        public FileObj_ getCurrentTotalPeerInfo()
        {
            var peerInfo = new FileObj_();

            peerInfo.complete = 0;
            peerInfo.incomplete = 0;

            WebTrackerSwarm[] swarms;

            lock (this.trackerSwarmDictionary)
            {
                swarms = this.trackerSwarmDictionary.Values.ToArray();
            }

            foreach (var item in swarms)
            {
                var data = item.getCurrentSeedersAndLeechers();

                peerInfo.complete += data.complete; //seeder
                peerInfo.incomplete += data.incomplete;//leecher
            }

            swarms = null;

            return peerInfo;
        }

        public WebTrackerSwarm SearchTrackerSwarm(string hash_info)
        {
            getValue:
            //기존에 존재하는 WebTrackerSwarm의 경우, 검색하여 가져온다.
            if (this.trackerSwarmDictionary.TryGetValue(hash_info, out var result))
            {
                return result;
            }
            //새로 추가된 트래커정보라면 TrackerSwarm을 새로 생성후 추가한다.
            else
            {
                lock (this.trackerSwarmDictionary) //trackerSwarmDictionary는 제거되는 경우가 없으므로, 추가하는 부분에만 lock.
                {
                    if (!this.trackerSwarmDictionary.ContainsKey(hash_info))
                    {
                        var newSwarm = new WebTrackerSwarm(hash_info);
                        this.trackerSwarmDictionary.Add(hash_info, newSwarm);
                    }
                }
                goto getValue;
            }
        }
    }

    public class WebTrackerSwarm
    {
        public string hash_info;
        Dictionary<string, WebTorrentPeer> peers;
        Random randSeed;

        public WebTrackerSwarm(string hash_info)
        {
            this.hash_info = hash_info;
            this.peers = new Dictionary<string, WebTorrentPeer>();
            this.randSeed = new Random();
        }

        public void announceTorrentPeerInfo(WebTorrentPeer recentlyPeer)
        {
            switch (recentlyPeer.eVent.ToLower())
            {
                case "started":
                    announceStarted(recentlyPeer);
                    break;
                case "stopped":
                    announceStopped(recentlyPeer);
                    break;
                case "completed":
                case "update":
                    announceUpdate(recentlyPeer);
                    break;
                default:
                    Response_Error.send(recentlyPeer.session, $"잘못된 Event값 event = {recentlyPeer.eVent}", "announce", this.hash_info);
                    break;
            }
        }

        void announceStarted(WebTorrentPeer recentlyPeer)
        {
            bool isAdded = false;
            lock (this.peers)
            {
                if (recentlyPeer.session != null && recentlyPeer.session.socket.Active
                    && this.getPeerListRef().TryAdd(recentlyPeer.session.peer_id, recentlyPeer))
                {
                    isAdded = true;
                }
            }

            if (isAdded)
            {
                var list = recentlyPeer.session.info_hash_List;

                lock (list)
                {
                    if (!list.TryAdd(this.hash_info, this.hash_info))
                    {
                        //Console.WriteLine($"announceStarted Fail Add info_hash_List! session_id:{recentlyPeer.session.session_id}");
                    }
                }
            }
            else
            {
                //Console.WriteLine($"announceStarted Fail Add PeerList! session_id:{recentlyPeer.session.session_id}");
            }
        }

        void announceStopped(WebTorrentPeer recentlyPeer)
        {
            bool removeSuccess = false;

            lock (this.peers)
            {
                if (recentlyPeer.session != null && recentlyPeer.session.socket.Active
                    && this.getPeerListRef().ContainsKey(recentlyPeer.session.peer_id))
                {
                    removeSuccess = this.getPeerListRef().Remove(recentlyPeer.session.peer_id);
                }
            }

            if (removeSuccess)
            {
                var list = recentlyPeer.session.info_hash_List;

                lock (list)
                {
                    if (list.ContainsKey(this.hash_info))
                    {
                        if (!list.Remove(this.hash_info))
                        {
                            //Console.WriteLine($"announceStopped Fail Remove info_hash_List! session_id:{recentlyPeer.session.session_id}");
                        }
                    }
                }
            }
            else
            {
                //Console.WriteLine($"announceStopped Fail Remove PeerList! session_id:{recentlyPeer.session.session_id}");
            }
        }

        void announceUpdate(WebTorrentPeer recentlyPeer)
        {
            bool isContainsKey;
            WebTorrentPeer olderData;

            lock (this.peers)
            {
                isContainsKey = this.getPeerListRef().TryGetValue(recentlyPeer.session.peer_id, out olderData);

                if (isContainsKey)
                {
                    olderData.updateData(recentlyPeer);
                }
            }

            if (!isContainsKey)
            {
                announceStarted(recentlyPeer);
            }
        }

        public bool removeTorrentPeerInfo(string peer_id)
        {
            bool removeSuccess = false;
            lock (this.peers)
            {
                if (this.getPeerListRef().ContainsKey(peer_id))
                {
                    removeSuccess = this.getPeerListRef().Remove(peer_id);
                }
            }

            return removeSuccess;
        }

        public WebTorrentPeer getPeer(string peer_id)
        {
            WebTorrentPeer result = null;
            lock (this.peers)
            {
                if (this.getPeerListRef().TryGetValue(peer_id, out WebTorrentPeer value))
                {
                    result = value;
                }
            }
            return result;
        }

        public Dictionary<string, WebTorrentPeer> getPeerListRef()
        {
            return this.peers;
        }

        //exclude_Peer_id 가 null이 아니라면, exclude_Peer_id 을 제외한 peer만 반환한다.
        public List<WebTorrentPeer> getCurrentPeers(ref List<WebTorrentPeer> peers, int numwant = 0, string exclude_Peer_id = null, bool isSeeder = false)
        {
            if (numwant < 1)
                numwant = 1;
            if (numwant > 20)
                numwant = 20;

            if (peers.Count > numwant && numwant > 0)
            {
                if (isSeeder)
                    peers.Sort(WebPeerComparer.seeder);
                else
                    peers.Sort(WebPeerComparer.leecher);

                int FilteringCount = 50;

                if (peers.Count > FilteringCount)
                    peers.RemoveRange(FilteringCount, peers.Count - FilteringCount);

                //remove exclude_Peer_id
                if (!string.IsNullOrEmpty(exclude_Peer_id))
                {
                    peers.RemoveAll(x => x.session.peer_id.Equals(exclude_Peer_id));
                }

                //random
                int maxRandValue = peers.Count - numwant;
                if (maxRandValue < 0 || isSeeder)
                    maxRandValue = 0;
                int initValue = randSeed.Next(0, maxRandValue);
                if (initValue > peers.Count - numwant)
                    initValue = peers.Count - numwant;
                peers.RemoveRange(0, initValue);
                if (peers.Count > numwant)
                    peers.RemoveRange(numwant, peers.Count - numwant);

                return peers;
            }
            else
            {
                //remove exclude_Peer_id
                if (!string.IsNullOrEmpty(exclude_Peer_id))
                {
                    peers.RemoveAll(x => x.session.peer_id.Equals(exclude_Peer_id));
                }

                return peers;
            }
        }

        public FileObj_ getCurrentSeedersAndLeechers()
        {
            List<WebTorrentPeer> list;

            lock (this.peers)
            {
                list = this.getPeerListRef().Values.ToList();
            }

            int totalPeerCount = list.Count;

            List<WebTorrentPeer> seederList = list.FindAll(x => x.left == 0);

            //left == 0byte, peers
            int seeding_peers = seederList.Count;

            //left > 0byte, peers.
            int downloading_peers = totalPeerCount - seeding_peers;

            var peerInfo = new FileObj_();

            peerInfo.complete = seeding_peers;
            peerInfo.incomplete = downloading_peers;
            peerInfo.downloaded = seeding_peers;

            seederList.Clear();
            seederList = null;
            list.Clear();
            list = null;

            return peerInfo;
        }

        public FileObj_ getCurrentSeedersAndLeechers(ref List<WebTorrentPeer> list)
        {
            int totalPeerCount = list.Count;

            List<WebTorrentPeer> seederList = list.FindAll(x => x.left == 0);

            //left == 0byte, peers
            int seeding_peers = seederList.Count;

            //left > 0byte, peers.
            int downloading_peers = totalPeerCount - seeding_peers;

            var peerInfo = new FileObj_();

            peerInfo.complete = seeding_peers;
            peerInfo.incomplete = downloading_peers;
            peerInfo.downloaded = seeding_peers;

            seederList.Clear();
            seederList = null;

            return peerInfo;
        }

        public List<WebTorrentPeer> getPeerList()
        {
            List<WebTorrentPeer> list;

            lock (this.peers)
            {
                list = this.getPeerListRef().Values.ToList();
            }

            return list;
        }
    }

    public class WebPeerComparer : IComparer<WebTorrentPeer>
    {
        public static WebPeerComparer seeder = new WebPeerComparer(true);
        public static WebPeerComparer leecher = new WebPeerComparer(false);

        private WebPeerComparer(bool isSeeder)
        {
            if (isSeeder)
                this.value = -1;
            else
                this.value = 1;
        }

        int value = 1;

        public int Compare(WebTorrentPeer x, WebTorrentPeer y)
        {
            //isSeeder is True, Sort By Descending [ 'left' lower & 'uploaded' higher ]
            //isSeeder is False, Sort By Ascending [ 'left' lower & 'uploaded' higher ]
            if (x.left < y.left)
            {
                return -1 * value;
            }
            else if (x.left == y.left)
            {
                if (x.uploaded > y.uploaded)
                {
                    return -1 * value;
                }
                else if (x.uploaded == y.uploaded)
                {
                    return 0 * value;
                }
                else if (x.uploaded < y.uploaded)
                {
                    return 1 * value;
                }
            }
            return 1 * value;
        }
    }
}