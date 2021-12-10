using System;
using Utf8Json;
using System.Collections;

namespace Tracker_Server.WebsocketTracker
{
    public class Request_Announce
    {
        public static void parsePacket(WebTorrentSession session, dynamic request)
        {
            try
            {
                string info_hash = request["info_hash"];
                string peer_id = request["peer_id"];

                if (!request.ContainsKey("event"))
                    request.Add("event", "update");

                string key = null;

                if (request.ContainsKey("key"))
                    key = Utils.getJsonValue<string>(request, "key");

                WebTorrentPeer announcePeer = new WebTorrentPeer()
                {
                    session = session,
                    infoHash = info_hash,
                    eVent = Utils.getJsonValue<string>(request, "event"),
                    numWant = Utils.getJsonValue<int>(request, "numwant"),
                    downloaded = Utils.getJsonValue<long>(request, "downloaded"),
                    uploaded = Utils.getJsonValue<long>(request, "uploaded"),
                    left = Utils.getJsonValue<long>(request, "left"),
                    key = key
                };

                bool isStarted = announcePeer.eVent.Equals("started");
                bool isCompleted = announcePeer.eVent.Equals("completed");
                bool isStopped = announcePeer.eVent.Equals("stopped");
                bool isUpdate = !(announcePeer.uploaded <= 0 && announcePeer.downloaded <= 0 && announcePeer.left <= 0)
                                && announcePeer.eVent.Equals("update");


                //check session peer_id
                if (string.IsNullOrEmpty(session.peer_id))
                {
                    //make unique_key
                    session.peer_id = Utils.getCombinePeerID(session.session_id, peer_id);
                }

                //overwrite peer_id
                peer_id = session.peer_id;

                //send packet (isStarted || isCompleted || isUpdate)
                if (isStopped || isStarted || isCompleted || isUpdate || request.ContainsKey("answer"))
                {
                    //search torrent peer info from trackerswarm, with info_hash.
                    var swarm = WebTrackerSwarmManager.instance().SearchTrackerSwarm(info_hash);

                    //announce peerinfo
                    swarm.announceTorrentPeerInfo(announcePeer);

                    //get current peerList snapshot
                    var peerList = swarm.getPeerList();

                    //get seeders, leechers info.
                    var peerCountInfo = swarm.getCurrentSeedersAndLeechers(ref peerList);
                    int complete = peerCountInfo.complete;
                    int incomplete = peerCountInfo.incomplete;

                    //no have answer.
                    if (!request.ContainsKey("answer"))
                    {
                        Response_PeersInfo data = new Response_PeersInfo()
                        {
                            action = "announce",
                            complete = complete,
                            incomplete = incomplete,
                            interval = TrackerServer_Configure.Interval_Web,
                            info_hash = info_hash
                        };

                        var response = JsonSerializer.Serialize(data);

                        Response_Announce.send(session, response);
                    }
                    //have answer.
                    else
                    {
                        var to_peer_id = request["to_peer_id"];

                        var targetPeer = swarm.getPeer(to_peer_id);
                        if (targetPeer != null)
                        {
                            Response_Answer data = new Response_Answer()
                            {
                                action = "announce",
                                answer = request["answer"],
                                offer_id = request["offer_id"],
                                peer_id = peer_id,
                                info_hash = info_hash
                            };

                            var response = JsonSerializer.Serialize(data);

                            Response_Announce.send(targetPeer.session, response);
                        }
                    }

                    if (isStopped)
                    {
                        return;
                    }

                    //if have 'offers' parameters & 'offers' Type is List.
                    if (request.ContainsKey("offers") && request["offers"] is IList)
                    {
                        var peers = swarm.getCurrentPeers(ref peerList, announcePeer.numWant, peer_id, announcePeer.isCompleteDownload);

                        dynamic offers = request["offers"];

                        for (int i = 0; i < peers.Count && i < offers.Count; i++)
                        {
                            var peer = peers[i];
                            dynamic offer_item = offers[i];

                            dynamic offer = offer_item["offer"];
                            string offer_id = offer_item["offer_id"];

                            if (offer == null || string.IsNullOrEmpty(offer_id))
                            {
                                continue;
                            }

                            Response_Offer data = new Response_Offer()
                            {
                                action = "announce",
                                offer = offer,
                                offer_id = offer_id,
                                peer_id = peer_id,
                                info_hash = info_hash
                            };

                            var response = JsonSerializer.Serialize(data);
                           
                            Response_Announce.send(peer.session, response);
                        }
                        peers.Clear();
                    }
                    peerList.Clear();
                    peerList = null;
                }
                //invalid event.
                else
                {
                    Response_Error.send(session, "invalid event", "announce", info_hash);
                    session.socket.CloseAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Request_Announce::parsePacket Exception::{e}");

                session.socket.CloseAsync();
            }
        }
    }
}