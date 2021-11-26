using System;
using System.Linq;
using System.Collections.Generic;

namespace Tracker_Server
{
    public class TrackerSwarmManager
    {
        #region instance

        static TrackerSwarmManager ins;

        public static TrackerSwarmManager instance()
        {
            if (ins == null)
                ins = new TrackerSwarmManager();

            return ins;
        }

        public TrackerSwarmManager()
        {
            this.trackerSwarmDictionary = new Dictionary<string, TrackerSwarm>();
        }

        #endregion

        //hash_info 정보에 따라서 TrackerSwarm 검색 진행.
        Dictionary<string, TrackerSwarm> trackerSwarmDictionary;

        public void Init()
        {
            //일정주기로 실행되는 이벤트 delegate 등록.
            TorrentTrackerServer.EventTasks += (null_obj, info_dictionary) =>
            {
                var totalPeerInfo = this.getCurrentTotalPeerInfoAndCheckTimeoutTrackerSwarm();

                lock (info_dictionary)
                {
                    if (!info_dictionary.ContainsKey("TrackerSwarmManager"))
                    {
                        JsonObject obj = new JsonObject();
                        obj.Add("TotalSeeder", totalPeerInfo.complete);
                        obj.Add("TotalLeecher", totalPeerInfo.incomplete);
                        obj.Add("TotalTorrentFiles", this.trackerSwarmDictionary.Count);

                        info_dictionary.Add("TrackerSwarmManager", obj);
                    }
                    else
                    {
                        var tracker = info_dictionary["TrackerSwarmManager"];
                        tracker["TotalSeeder"] = totalPeerInfo.complete;
                        tracker["TotalLeecher"] = totalPeerInfo.incomplete;
                        tracker["TotalTorrentFiles"] = this.trackerSwarmDictionary.Count;
                    }
                }

                Console.WriteLine($"TrackerSwarmManager TotalTorrentFiles:{this.trackerSwarmDictionary.Count} " +
                                  $"=> TotalSeeder:{totalPeerInfo.complete} / TotalLeecher:{totalPeerInfo.incomplete}");
            };

            Console.WriteLine("TrackerSwarmManager initialize.");
        }


        public FileObj_ getCurrentTotalPeerInfoAndCheckTimeoutTrackerSwarm()
        {
            var peerInfo = new FileObj_();

            TrackerSwarm[] swarms;

            lock (this.trackerSwarmDictionary)
            {
                swarms = this.trackerSwarmDictionary.Values.ToArray();
            }

            int removedCount = 0;

            foreach (var item in swarms)
            {
                var data = item.getCurrentSeedersAndLeechers();

                peerInfo.complete += data.complete; //seeder
                peerInfo.incomplete += data.incomplete;//leecher

                removedCount += item.CheckTimeoutTrackerSwarm();
            }

            Console.WriteLine($"CheckTimeoutTrackerSwarm::removedCount:{removedCount}");

            swarms = null;

            return peerInfo;
        }

        public TrackerSwarm SearchTrackerSwarm(string hash_info)
        {
            getValue:
            //기존에 존재하는 TrackerSwarm의 경우, 검색하여 가져온다.
            if (this.trackerSwarmDictionary.TryGetValue(hash_info, out TrackerSwarm result))
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
                        var newSwarm = new TrackerSwarm(hash_info);
                        this.trackerSwarmDictionary.Add(hash_info, newSwarm);
                    }
                }
                goto getValue;
            }
        }
    }

    public class TrackerSwarm
    {
        public string hash_info;
        Dictionary<string, TorrentPeer> peers;
        Random randSeed;

        public TrackerSwarm(string hash_info)
        {
            this.hash_info = hash_info;
            this.peers = new Dictionary<string, TorrentPeer>();
            this.randSeed = new Random();
        }

        public void announceTorrentPeerInfo(TorrentPeer recentlyPeer, bool isHTTP = false)
        {
            var keys = $"{recentlyPeer.ip}:{recentlyPeer.port}";

            lock (this.peers)
            {
                if (this.getPeerListRef().TryGetValue(keys, out TorrentPeer result))
                {
                    if (isHTTP)
                    {
                        recentlyPeer.eVent = result.eVent;
                        recentlyPeer.extensions = result.extensions;

                        result.updateData(recentlyPeer);
                    }
                    else
                    {
                        result.updateData(recentlyPeer);
                    }
                }
                else
                {
                    this.getPeerListRef().Add(keys, recentlyPeer);
                }
            }
        }

        //현재 시점의 DateTime과, Peer의 DateTime을 비교하여,
        //특정시간이상 경과한 유저는 접속을 끊은것으로 간주하고 List에서 제거한다.
        public int CheckTimeoutTrackerSwarm()
        {
            DateTime now = DateTime.Now;
            int removedCount = 0;

            lock (this.peers)
            {
                foreach (var item in getPeerListRef())
                {
                    var key = item.Key;
                    var peer = item.Value;

                    if ((now - peer.lastUpdateTime) > TimeSpan.FromMinutes(5.5f))
                    {
                        if (getPeerListRef().Remove(key))
                            removedCount++;
                    }
                }
            }

            return removedCount;
        }


        public Dictionary<string, TorrentPeer> getPeerListRef()
        {
            return this.peers;
        }

        public List<TorrentPeer> getCurrentPeers(int numwant = 0, string exclude_Peer_id = null, bool isSeeder = false)
        {
            if (numwant < 1)
                numwant = 1;
            if (numwant > TrackerServer_Configure.MaxNumWant)
                numwant = TrackerServer_Configure.MaxNumWant;

            List<TorrentPeer> peers;

            lock (this.peers)
            {
                peers = this.getPeerListRef().Values.ToList();
            }

            if (peers.Count > numwant && numwant > 0)
            {
                if (isSeeder)
                    peers.Sort(PeerComparer.seeder);
                else
                    peers.Sort(PeerComparer.leecher);

                int FilteringCount = TrackerServer_Configure.MaxNumWant;

                if (peers.Count > FilteringCount)
                    peers.RemoveRange(FilteringCount, peers.Count - FilteringCount);

                //remove exclude_Peer_id
                if (!string.IsNullOrEmpty(exclude_Peer_id))
                {
                    peers.RemoveAll(x => x.peerId.Equals(exclude_Peer_id));
                }

                //random
                int maxRandValue = peers.Count - numwant - 1;
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
                    peers.RemoveAll(x => x.peerId.Equals(exclude_Peer_id));
                }

                return peers;
            }
        }

        public FileObj_ getCurrentSeedersAndLeechers()
        {
            List<TorrentPeer> list;

            lock (this.peers)
            {
                list = this.getPeerListRef().Values.ToList();
            }

            var seederList = list.FindAll(x => x.left == 0 && x.eVent != (int)Event.stopped);
            var leecherList = list.FindAll(x => x.left > 0 && x.eVent != (int)Event.stopped);
            var completedownloaderList = list.FindAll(x => x.eVent == (int)Event.completed);

            //남은 양이 0byte인, 완전체를 보유하면서, 현재 시딩중인 피어 수.
            //(x.event_value의 값이 3이면 시딩을 중단한것이므로 통계에서 제외)
            int seeding_peers = seederList.Count;

            //남은 양이 0byte를 넘는 현재 시딩중인 피어 수.
            //(x.event_value의 값이 3이면 시딩을 중단한것이므로 통계에서 제외)
            int leecher_peers = leecherList.Count;

            //지정한 범위의 다운로드를 완료한 피어 수.
            //(100%를 받은 시더 or 일부분만 완전히 받은 피어 모두 포함)
            int completedownload_peers = completedownloaderList.Count;

            var peerInfo = new FileObj_();

            peerInfo.complete = seeding_peers;
            peerInfo.incomplete = leecher_peers;
            peerInfo.downloaded = completedownload_peers;

            seederList.Clear();
            seederList = null;
            leecherList.Clear();
            leecherList = null;
            completedownloaderList.Clear();
            completedownloaderList = null;
            list.Clear();
            list = null;

            return peerInfo;
        }
    }

    public class PeerComparer : IComparer<TorrentPeer>
    {
        public static PeerComparer seeder = new PeerComparer(true);
        public static PeerComparer leecher = new PeerComparer(false);

        private PeerComparer(bool isSeeder)
        {
            if (isSeeder)
                this.value = -1;
            else
                this.value = 1;
        }

        int value = 1;

        public int Compare(TorrentPeer x, TorrentPeer y)
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
