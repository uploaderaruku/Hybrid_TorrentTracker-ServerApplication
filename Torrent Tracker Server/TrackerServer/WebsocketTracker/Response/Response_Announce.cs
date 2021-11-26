using System;
using System.IO;
using DotNetty.Common.Utilities;

namespace Tracker_Server.WebsocketTracker
{
    public class Response_Announce
    {
        public static void send(WebTorrentSession session, byte[] response)
        {
            var frame = Utils.TextWebSocketFrameFromByteArray(response);
            try
            {
                if (session.socket.Active && session.socket.Open)
                    session.socket.WriteAndFlushAsync(frame);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Response_Announce::Exception::{e}");
                
                if (frame.ReferenceCount == 1)
                    frame.SafeRelease();

                if (session.socket.Active && session.socket.Open)
                    session.socket.CloseAsync();
            }
        }
    }
}
