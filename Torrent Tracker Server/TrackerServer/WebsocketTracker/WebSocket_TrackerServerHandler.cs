using System;
using Utf8Json;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using DotNetty.Transport.Channels;
using DotNetty.Codecs.Http.WebSockets;
using Tracker_Server.WebsocketTracker;

namespace Tracker_Server
{
    public class WebSocket_TrackerServerHandler : SimpleChannelInboundHandler<WebSocketFrame>
    {
        static Dictionary<string, WebTorrentSession> SessionList = new Dictionary<string, WebTorrentSession>();

        //getSession
        public static WebTorrentSession getSession(string sessionID)
        {
            WebTorrentSession session = null;
            lock (SessionList)
            {
                if (SessionList.TryGetValue(sessionID, out var value))
                {
                    session = value;
                }
            }
            return session;
        }

        //connected websocket client
        public override void ChannelActive(IChannelHandlerContext ctx)
        {
            var remoteAddress = (IPEndPoint)ctx.Channel.RemoteAddress;
            var ip = (uint)IPAddressConverter.ToInt(remoteAddress.Address);
            var port = (ushort)remoteAddress.Port;
            
            var sessionID = Utils.getSessionIDFromChannel(ctx.Channel);

            //Create New WebTorrentSession
            var session = new WebTorrentSession(ctx.Channel)
            {
                ip = ip,
                port = port,
                session_id = sessionID
            };

            //Add WebTorrentSession To SessionList
            lock (SessionList)
            {
                if (!SessionList.TryAdd(sessionID, session))
                {
                    Console.WriteLine($"ChannelActive Fail Add SessionList! session_id:{session.session_id}");
                }
            }
        }

        //disconnected websocket client
        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            var sessionID = Utils.getSessionIDFromChannel(ctx.Channel);

            WebTorrentSession session = null;

            //Remove WebTorrentSession To SessionList
            lock (SessionList)
            {
                if (SessionList.TryGetValue(sessionID, out var value))
                {
                    session = value;

                    if (!SessionList.Remove(sessionID))
                    {
                        Console.WriteLine($"ChannelInactive Fail Remove SessionList! session_id:{session.session_id}");
                    }

                    if (!sessionID.Equals(session.session_id))
                    {
                        Console.WriteLine($"ChannelInactive invalid session_id! {sessionID} / {session.session_id}");
                    }
                }
            }
            
            try
            {
                string[] list;

                lock (session.info_hash_List)
                {
                    list = session.info_hash_List.Values.ToArray();
                }
                
                foreach (var item in list)
                {
                    var info_hash = item;

                    WebTrackerSwarm swarm = WebTrackerSwarmManager.instance().SearchTrackerSwarm(info_hash);

                    if (!swarm.removeTorrentPeerInfo(session.peer_id))
                    {
                        Console.WriteLine($"ChannelInactive removeTorrentPeerInfo false! peer_id:{session.peer_id}");
                    }
                }
                
                list = null;
                session.info_hash_List.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine($"ChannelInactive Exception: {e}");
            }
        }

        //onMessage From websocket client
        protected override void ChannelRead0(IChannelHandlerContext ctx, WebSocketFrame msg)
        {
            var sessionID = Utils.getSessionIDFromChannel(ctx.Channel);

            WebTorrentSession session = null;

            if (SessionList.TryGetValue(sessionID, out var value))
            {
                session = value;
            }

            MemoryStream ms = null;

            string action = null, info_hash = null, peer_id = null;
            try
            {
                if (session == null)
                {
                    throw new Exception($"Invalid session.");
                }

                //get MemoryStream From ByteBuffer.
                var buffer = msg.Content.GetIoBuffer();
                ms = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);
                buffer = null;

                //Parse Json with Utf8Json
                dynamic request = JsonSerializer.Deserialize<dynamic>(ms);

                //Data parsing & validation check
                if (request.ContainsKey("action"))
                    action = request["action"].ToLower().Trim();

                if (string.IsNullOrEmpty(action))
                {
                    throw new Exception($"Invalid Data! No Have 'Action' Parameter");
                }

                if (action.Equals("announce"))
                {
                    //announce
                    if (request.ContainsKey("info_hash"))
                        info_hash = request["info_hash"];

                    if (request.ContainsKey("peer_id"))
                        peer_id = request["peer_id"];
                    
                    if (!string.IsNullOrEmpty(info_hash))
                    {
                        if (info_hash.Length != 20)
                        {
                            throw new Exception($"Invalid info_hash. info_hash => {info_hash}, {info_hash.Length} length");
                        }
                    }

                    if (!string.IsNullOrEmpty(peer_id))
                    {
                        if (peer_id.Length != 20)
                        {
                            throw new Exception($"Invalid peer_id. peer_id => {peer_id}, {peer_id.Length} length");
                        }
                    }

                    if (request.ContainsKey("answer"))
                    {
                        if (!request.ContainsKey("to_peer_id"))
                        {
                            throw new Exception($"Invalid answer, No Have 'to_peer_id' Parameter");
                        }

                        if (!request.ContainsKey("offer_id"))
                        {
                            throw new Exception($"Invalid answer, No Have 'offer_id' Parameter");
                        }

                        if (string.IsNullOrEmpty(request["offer_id"]))
                        {
                            throw new Exception($"Invalid answer, Invalid offer_id");
                        }

                        var to_peer_id = request["to_peer_id"];

                        if (to_peer_id.Length != 20)
                        {
                            throw new Exception($"Invalid to_peer_id. to_peer_id => {to_peer_id}, {to_peer_id.Length} length");
                        }
                    }

                    if (request.ContainsKey("numwant") && request.ContainsKey("offers") && request["offers"] is IList)
                    {
                        var offers = request["offers"];

                        request["numwant"] = offers.Count;
                    }

                    if (!request.ContainsKey("compact"))
                    {
                        request.Add("compact", -1);
                    }
                    else
                    {
                        request["compact"] = -1;
                    }

                    Request_Announce.parsePacket(session, request);
                    //announce
                }
                else if (action.Equals("scrape"))
                {
                    //scrape
                    if (request.ContainsKey("info_hash"))
                    {
                        var info_hash_value = request["info_hash"];

                        //info_hash is Arrays
                        if (info_hash_value is IList)
                        {
                            foreach (var item in info_hash)
                            {
                                string itemStr = item.ToString();
                                if (itemStr.Length != 20)
                                {
                                    throw new Exception($"Invalid info_hash. info_hash => {itemStr}, {itemStr.Length} length");
                                }
                            }
                        }
                        //info_hash is Single Object
                        else
                        {
                            info_hash = info_hash_value;
                            
                            if (info_hash.Length != 20)
                            {
                                throw new Exception($"Invalid info_hash. info_hash => {info_hash}, {info_hash.Length} length");
                            }
                        }

                        Request_Scrape.parsePacket(session, request);
                    }
                    else
                    {
                        throw new Exception($"Invalid Scrape request.");
                    }
                    //scrape
                }
                else
                {
                    throw new Exception($"Invalid Action. action => {action}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"TrackerServerHandlerException::{e}");

                if (session != null)
                    Response_Error.send(session, $"TrackerServerHandlerException::{e.Message}", action, info_hash);
                
                ctx.CloseAsync();
            }
            finally
            {
                if(ms != null)
                    ms.Dispose();

                msg.Release();
            }
        }

        public static void addSessionTotalCountTask()
        {
            TorrentTrackerServer.EventTasks += (sender, info_dictionary) =>
            {
                lock (info_dictionary)
                {
                    if (!info_dictionary.ContainsKey("WebSocket_TrackerServerHandler"))
                    {
                        JsonObject obj = new JsonObject();
                        obj.Add("sessionCount", SessionList.Count);
                        info_dictionary.Add("WebSocket_TrackerServerHandler", obj);
                    }
                    else
                    {
                        info_dictionary["WebSocket_TrackerServerHandler"]["sessionCount"] = SessionList.Count;
                    }
                }
            
                Console.WriteLine($"WebSocket_Tracker SessionTotalCount:{SessionList.Count}");
            };
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            if (e is SocketException)
            {
                e = null;
                return;
            }

            var msg = $"WebSocket_TrackerServer::ExceptionCaught: {e}";

            //Console.WriteLine(msg);
            Utils.sendTelegram(msg);
            ctx.CloseAsync();
            e = null;
        }
    }
}