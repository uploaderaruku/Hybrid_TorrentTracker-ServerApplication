using System;
using Utf8Json;
using System.Collections;
using System.Collections.Generic;


namespace Tracker_Server.WebsocketTracker
{
    public class Request_Scrape
    {
        public static void parsePacket(WebTorrentSession session, dynamic request)
        {
            try
            {
                var info_hash = request["info_hash"];

                //if info_hash is List
                if (info_hash is IList)
                {
                    Flag_ flag = new Flag_()
                    {
                        min_request_interval = TrackerServer_Configure.Min_Interval_Web
                    };

                    List<JsonObject> fileArr = new List<JsonObject>();
                    
                    for (int i = 0; i < info_hash.Count; i++)
                    {
                        string raw_info_hash = info_hash[i]["info_hash"];

                        var swarm = WebTrackerSwarmManager.instance().SearchTrackerSwarm(raw_info_hash);

                        var peerCountInfo = swarm.getCurrentSeedersAndLeechers();
                        
                        FileObj_ file_info = peerCountInfo;

                        JsonObject file = new JsonObject();
                        file.Add(raw_info_hash, file_info);

                        fileArr.Add(file);
                    }

                    Response_Scrape_InfoHash data = new Response_Scrape_InfoHash()
                    {
                        action = "scrape",
                        files = fileArr,
                        flags = flag
                    };
                    var response = JsonSerializer.Serialize(data);

                    Response_Scrape.send(session, response);
                }
                //if info_hash is Not List
                else
                {
                    Flag_ flag = new Flag_()
                    {
                        min_request_interval = TrackerServer_Configure.Min_Interval_Web
                    };
                    
                    var swarm = WebTrackerSwarmManager.instance().SearchTrackerSwarm(info_hash);

                    var peerCountInfo = swarm.getCurrentSeedersAndLeechers();
                    
                    FileObj_ file_info = peerCountInfo;

                    JsonObject file = new JsonObject();
                    file.Add(info_hash, file_info);

                    Response_Scrape_InfoHash data = new Response_Scrape_InfoHash()
                    {
                        action = "scrape",
                        files = file,
                        flags = flag
                    };

                    var response = JsonSerializer.Serialize(data);
                    
                    Response_Scrape.send(session, response);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Request_Scrape::parsePacket Exception::{e}");

                session.socket.CloseAsync();
            }
        }
    }
}
