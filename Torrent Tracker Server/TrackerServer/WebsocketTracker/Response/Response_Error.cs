using System;
using Utf8Json;
using System.IO;
using DotNetty.Common.Utilities;

namespace Tracker_Server.WebsocketTracker
{
    public class Response_Error
    {
        public static void send(WebTorrentSession session, string message, string action, string info_hash)
        {
            if (string.IsNullOrEmpty(action))
                action = "announce";
            if (string.IsNullOrEmpty(info_hash))
                info_hash = Utils.bytesToStr(new byte[20]);

            JsonObject response = new JsonObject();
            response.Add("action", action);
            response.Add("failure reason", message);
            response.Add("info_hash", info_hash);

            var data = JsonSerializer.Serialize(response);
            var frame = Utils.TextWebSocketFrameFromByteArray(data);
            try
            {
                if (session.socket.Active && session.socket.Open)
                    session.socket.WriteAndFlushAsync(frame);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Response_Error::Exception::{e}");

                if (frame.ReferenceCount == 1)
                    frame.SafeRelease();

                if (session.socket.Active && session.socket.Open)
                    session.socket.CloseAsync();
            }
        }
    }
}
