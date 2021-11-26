using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels.Sockets;

namespace Tracker_Server
{
    public class TorrentTrackerServer
    {
        static IEventLoopGroup bossGroup;
        static IEventLoopGroup workerGroup;
        static IChannel boundTCP_Channel;
        static IChannel boundUDP_Channel;
        
        public static async Task Run()
        {
            Console.WriteLine("Start TorrentTrackerServer");

            //Manager Init
            TrackerSwarmManager.instance().Init();
            WebTrackerSwarmManager.instance().Init();
            WebSocket_TrackerServerHandler.addSessionTotalCountTask();
            
            //Get Cpu Core
            int cpu_Processor_count = Environment.ProcessorCount;

            //Default WorkerThreads = CPU Core * 2
            int thread_Count = cpu_Processor_count * 2;

            //Load Configure.
            if (TrackerServer_Configure.WorkerThreadCount > 0)
                thread_Count = TrackerServer_Configure.WorkerThreadCount;
            
            //Set Boss EventLoop (Single Thread)
            bossGroup = new SingleThreadEventLoop();
            
            //Set Worker EventLoop (Maybe Multi Thread)
            workerGroup = new MultithreadEventLoopGroup(thread_Count);

            Console.WriteLine($"\nCPU Core => {cpu_Processor_count} / Worker Thread => {thread_Count}\n");
            
            ServerBootstrap serverBootstrap = new ServerBootstrap();
            Bootstrap udpBootstrap = new Bootstrap();
            
            try
            {
                udpBootstrap
                    .Group(workerGroup)
                    .Channel<SocketDatagramChannel>()
                    .Option(ChannelOption.SoRcvbuf, 32 * 1048576) //32MB
                    .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                    .Option(ChannelOption.RcvbufAllocator, new FixedRecvByteBufAllocator(4096))
                    .Handler(new ActionChannelInitializer<IDatagramChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(new UDP_TrackerServerHandler());
                    }));

                Console.WriteLine("TorrentTrackerServer::UDP Server initialize.");

                serverBootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                    .ChildOption(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
                    .ChildOption(ChannelOption.TcpNodelay, true)
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(new HttpServerCodec());
                        pipeline.AddLast(new HttpObjectAggregator(65535));
                        pipeline.AddLast(new WebSocketServerInitializeHandler("/ws/announce"));
                        pipeline.AddLast(new WebSocket_TrackerServerHandler());
                    }));

                Console.WriteLine("TorrentTrackerServer::Websocket Server initialize.");

                boundTCP_Channel = await serverBootstrap.BindAsync(TrackerServer_Configure.Websocket_Listen_PORT);
                Console.WriteLine($"\nWebSocket Tracker Server ListenURI: \nws://localhost:{TrackerServer_Configure.Websocket_Listen_PORT}/ws/announce");

                boundUDP_Channel = await udpBootstrap.BindAsync(TrackerServer_Configure.UDP_Listen_PORT);
                Console.WriteLine($"\nUDP Tracker Server ListenURI: \nudp://localhost:{TrackerServer_Configure.UDP_Listen_PORT}/announce");

                EventHandlerTask();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                Console.WriteLine("TorrentTrackerServer initialize Failure.");
            }
        }

        public static async void Stop()
        {
            //CloseAsync
            await boundUDP_Channel.CloseAsync();
            Console.WriteLine("TorrentTrackerServer UDP_CloseAsync");
            await boundTCP_Channel.CloseAsync();
            Console.WriteLine("TorrentTrackerServer Websocket_CloseAsync");

            await bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2));
            await workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2));

            Console.WriteLine("TorrentTrackerServer ShutdownGracefully Complete.");
        }


        static Dictionary<string, JsonObject> trackerServerStatisticsInfo = new Dictionary<string, JsonObject>();

        public static EventHandler<Dictionary<string, JsonObject>> EventTasks;

        public static void RefreshStatisticsInfo()
        {
            EventTasks.Invoke(null, trackerServerStatisticsInfo);
        }

        public static string GetStatisticsInfo()
        {
            var info_dictionary = trackerServerStatisticsInfo;

            JsonObject tracker;
            string tracker_seeders;
            string tracker_leechers;
            string tracker_TorrentFiles;

            JsonObject web_tracker;
            string web_tracker_seeders;
            string web_tracker_leechers;
            string web_tracker_TorrentFiles;
            string sessionCounts;

            lock (info_dictionary)
            {
                tracker = info_dictionary["TrackerSwarmManager"];
                tracker_seeders = tracker["TotalSeeder"].ToString();
                tracker_leechers = tracker["TotalLeecher"].ToString();
                tracker_TorrentFiles = tracker["TotalTorrentFiles"].ToString();

                web_tracker = info_dictionary["WebTrackerSwarmManager"];
                web_tracker_seeders = web_tracker["TotalSeeder"].ToString();
                web_tracker_leechers = web_tracker["TotalLeecher"].ToString();
                web_tracker_TorrentFiles = web_tracker["TotalTorrentFiles"].ToString();

                sessionCounts = info_dictionary["WebSocket_TrackerServerHandler"]["sessionCount"].ToString();
            }

            //  2022/11/11 12:34:56
            var currentTime = DateTime.UtcNow.AddHours( +9.0 ).ToString("yyyy/MM/dd HH:mm:ss");

            string stats =
                $"\n===============================\nTorrentTrackerServer StatisticsInfo\n{currentTime}\n===============================\n\n" +
                $"tracker_TorrentFiles => {tracker_TorrentFiles} \n\n" +
                $"tracker_seeders : {tracker_seeders} \ntracker_leechers : {tracker_leechers} \n\n\n" +
                $"web_tracker_TorrentFiles => {web_tracker_TorrentFiles} \n\n" +
                $"web_tracker_seeders : {web_tracker_seeders} \nweb_tracker_leechers : {web_tracker_leechers} \n\n" +
                $"web_tracker_sessionCounts : {sessionCounts} \n\n";


            return stats;
        }

        static async void EventHandlerTask()
        {
            await Task.Delay(1000);

            Console.WriteLine($"Start EventHandlerTask");

            Stopwatch sw = new Stopwatch();
            long elapsedMilliseconds = 0;
            
            while (true)
            {
                sw.Restart();

                //  2021/11/01 12:34:56
                int timeHour = 9; //(UTC + 9Hour) = KST
                var currentTime = DateTime.UtcNow.AddHours( timeHour ).ToString("yyyy/MM/dd HH:mm:ss");

                try
                {
                    Console.WriteLine($"\n\nEventHandlerTask==={currentTime}=========================\n");

                    //Run EventHandler Tasks.
                    TorrentTrackerServer.RefreshStatisticsInfo();

                    Console.WriteLine($"\n===============================================EventHandlerTask\n\n");
                    
                    Utils.sendTelegram(TorrentTrackerServer.GetStatisticsInfo());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"EventHandlerTask:{e}");
                }
                finally
                {
                    elapsedMilliseconds = sw.ElapsedMilliseconds;
                    
                    if (elapsedMilliseconds < 60000)
                        await Task.Delay(TimeSpan.FromSeconds(60) - TimeSpan.FromMilliseconds(elapsedMilliseconds));
                    else
                        Console.WriteLine($"EventHandlerTask isOverload?! {elapsedMilliseconds}ms");
                }
            }
        }
    }

    public enum Event : int
    {
        none = 0,
        completed = 1,
        started = 2,
        stopped = 3
    }

    public enum Action : int
    {
        connect = 0,
        announce = 1,
        scrape = 2,
        error = 3
    }
}
