using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tracker_Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TrackerServer_Configure.LoadServerConfigFromFile();
            
            TorrentTrackerServer.Run().GetAwaiter().GetResult();

            CreateHostBuilder(args).Build().Run();

            TorrentTrackerServer.Stop();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Console.WriteLine($"\nHTTP Tracker Server ListenURI: \nhttp://localhost:{TrackerServer_Configure.Web_And_Http_Listen_PORT}/announce");
            
            Console.WriteLine($"\nWeb Server Statistics URI: \nhttp://localhost:{TrackerServer_Configure.Web_And_Http_Listen_PORT}/stats\n\n");
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logBuilder => {
                    //log clear
                    logBuilder.ClearProviders();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    //listening webserver urls
                    webBuilder.UseUrls($"http://*:{TrackerServer_Configure.Web_And_Http_Listen_PORT}");
                    //configure webserver options
                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.AllowSynchronousIO = true;
                        serverOptions.Limits.MaxConcurrentConnections = null;
                        serverOptions.Limits.MaxConcurrentUpgradedConnections = null;
                        serverOptions.Limits.MaxRequestBodySize = 1048576 * 1024; //1MB * 1024
                    });
                });
        }
    }
}
