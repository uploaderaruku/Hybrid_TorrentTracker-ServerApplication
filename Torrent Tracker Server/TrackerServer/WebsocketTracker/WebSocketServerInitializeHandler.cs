using System;
using System.Net;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Codecs.Http.WebSockets;
using HttpVersion = DotNetty.Codecs.Http.HttpVersion;

namespace Tracker_Server
{
    class WebSocketServerInitializeHandler : ChannelHandlerAdapter
    {
        public WebSocketServerInitializeHandler(string websocketPath)
        {
            WebsocketPath = websocketPath;
        }

        static string WebsocketPath = "/ws"; //default

        WebSocketServerHandshaker handshaker;
        
        public override void ChannelReadComplete(IChannelHandlerContext ctx) => ctx.Flush();
        
        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            if (msg is IFullHttpRequest request)
            {
                this.HandleHttpRequest(ctx, request);
            }
            else if (msg is WebSocketFrame frame)
            {
                this.HandleWebSocketFrame(ctx, frame);
            }
        }

        async void HandleHttpRequest(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            bool isOk = false;
            try
            {
                if (!request.Result.IsSuccess)
                {
                    SendHttpResponse(ctx, request, new DefaultFullHttpResponse(
                        HttpVersion.Http11, HttpResponseStatus.BadRequest, Utils.allocBuffer(0)));
                }
                else if (!Equals(request.Method, HttpMethod.Get))
                {
                    SendHttpResponse(ctx, request, new DefaultFullHttpResponse(
                        HttpVersion.Http11, HttpResponseStatus.BadRequest, Utils.allocBuffer(0)));
                }
                else if (!request.Headers.Contains(HttpHeaderNames.SecWebsocketVersion))
                {
                    SendHttpResponse(ctx, request, new DefaultFullHttpResponse(
                        HttpVersion.Http11, HttpResponseStatus.BadRequest, Utils.allocBuffer(0)));
                }
                else if (!request.Headers.Contains(HttpHeaderNames.SecWebsocketKey))
                {
                    SendHttpResponse(ctx, request, new DefaultFullHttpResponse(
                        HttpVersion.Http11, HttpResponseStatus.BadRequest, Utils.allocBuffer(0)));
                }
                else //if all condition Ok,
                {
                    isOk = true;
                }

                if (isOk)
                {
                    var wsFactory = new WebSocketServerHandshakerFactory(GetWebSocketLocation(request),
                        null, true, 512 * 1024); //512KB

                    this.handshaker = wsFactory.NewHandshaker(request);
                    
                    if (this.handshaker == null)
                    {
                        await WebSocketServerHandshakerFactory.SendUnsupportedVersionResponse(ctx.Channel);
                    }
                    else
                    {
                        await this.handshaker.HandshakeAsync(ctx.Channel, request);

                        //InitRealIP
                        InitializeRealUserIPAddress(ctx, request);

                        base.HandlerAdded(ctx);
                    }

                    wsFactory = null;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"WebSocketServerInitializeHandler::HandleHttpRequest() Exception: {exception}");
                ctx.CloseAsync();
            }
            finally
            {
                if (request.ReferenceCount > 0)
                    request.SafeRelease(request.ReferenceCount);
            }
        }
        
        async void HandleWebSocketFrame(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            try
            {
                if (frame is CloseWebSocketFrame)
                {
                    await this.handshaker.CloseAsync(ctx.Channel, (CloseWebSocketFrame)frame.Retain());
                    return;
                }
                else if(frame is PingWebSocketFrame)
                {
                    ctx.WriteAsync(new PongWebSocketFrame((IByteBuffer)frame.Content.Retain()));
                    return;
                }
                else if(frame is TextWebSocketFrame)
                {
                    ctx.FireChannelRead(frame.Retain());
                }
                else if(frame is BinaryWebSocketFrame)
                {
                    ctx.FireChannelRead(frame.Retain());
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"WebSocketServerInitializeHandler::HandleWebSocketFrame() Exception: {exception}");
                ctx.CloseAsync();
            }
        }

        static void SendHttpResponse(IChannelHandlerContext ctx, IFullHttpRequest req, IFullHttpResponse res)
        {
            try
            {
                HttpUtil.SetContentLength(res, res.Content.ReadableBytes);

                if (ctx.Channel.Active && ctx.Channel.Open)
                    ctx.Channel.WriteAndFlushAsync(res);

                if (!HttpUtil.IsKeepAlive(req) || res.Status.Code != 200)
                {
                    ctx.CloseAsync();
                }
            }
            catch (Exception exception)
            {
                if (res.ReferenceCount > 0)
                    res.SafeRelease(res.ReferenceCount);

                Console.WriteLine($"WebSocketServerInitializeHandler::SendHttpResponse() Exception: {exception}");
                ctx.CloseAsync();
            }
        }

        static string GetWebSocketLocation(IFullHttpRequest req)
        {
            bool result = req.Headers.TryGet(HttpHeaderNames.Host, out ICharSequence value);

            if (result)
            {
                if (false) //ssl, tls
                {
                    return $"wss://{value.ToString()}{WebsocketPath}";
                }
                else
                {
                    return $"ws://{value.ToString()}{WebsocketPath}";
                }
            }
            else
            {
                return $"ws://localhost/{WebsocketPath}";
            }
        }

        static readonly AsciiString Forwarded = new AsciiString("Forwarded");
        static readonly AsciiString X_Forwarded_For = new AsciiString("X-Forwarded-For");
        static readonly AsciiString Proxy_Client_IP = new AsciiString("Proxy-Client-IP");
        static readonly AsciiString WL_Proxy_Client_IP = new AsciiString("WL-Proxy-Client-IP");
        static readonly AsciiString HTTP_CLIENT_IP = new AsciiString("HTTP_CLIENT_IP");
        static readonly AsciiString HTTP_X_FORWARDED_FOR = new AsciiString("HTTP_X_FORWARDED_FOR");

        static void InitializeRealUserIPAddress(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            string result = null;

            try
            {
                if (request.Headers.TryGet(Forwarded, out ICharSequence value1))
                {
                    var elementArr = value1.ToString().Split(';');
                    foreach (var item in elementArr)
                    {
                        if (item.Contains("for="))
                        {
                            var ipArr = item.Split("=");
                            result = ipArr[1].Trim();
                            break;
                        }
                    }
                }
                else if (request.Headers.TryGet(X_Forwarded_For, out ICharSequence value2))
                {
                    var ipArr = value2.ToString().Split(',');
                    result = ipArr[0];
                }
                else if (request.Headers.TryGet(Proxy_Client_IP, out ICharSequence value3))
                {
                    var ipArr = value2.ToString().Split(',');
                    result = ipArr[0];
                }
                else if (request.Headers.TryGet(WL_Proxy_Client_IP, out ICharSequence value4))
                {
                    var ipArr = value2.ToString().Split(',');
                    result = ipArr[0];
                }
                else if (request.Headers.TryGet(HTTP_CLIENT_IP, out ICharSequence value5))
                {
                    var ipArr = value2.ToString().Split(',');
                    result = ipArr[0];
                }
                else if (request.Headers.TryGet(HTTP_X_FORWARDED_FOR, out ICharSequence value6))
                {
                    var ipArr = value2.ToString().Split(',');
                    result = ipArr[0];
                }

                if (IPAddress.TryParse(result, out var parseTest))
                {

                }
                else
                {
                    IPEndPoint endPoint = ctx.Channel.RemoteAddress as IPEndPoint;
                    result = endPoint.Address.ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"RealUserIPAddressException: {e}");
                IPEndPoint endPoint = ctx.Channel.RemoteAddress as IPEndPoint;
                result = endPoint.Address.ToString();
            }

            var sessionID = Utils.getSessionIDFromChannel(ctx.Channel);
            if (sessionID != null)
            {
                var session = WebSocket_TrackerServerHandler.getSession(sessionID);

                if (session != null)
                {
                    session.realIP = result;
                }
            }
        }
    }
}
