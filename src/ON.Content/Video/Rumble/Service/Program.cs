using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace ON.Content.Rumble.Service
{
    public class Program
    {
        public const string SERVICE_NAME = "RUMBLESERVICE";
        public const string API_PORT_NAME = "SERVICE__" + SERVICE_NAME + "__API__PORT";
        public const string GRPC_PORT_NAME = "SERVICE__" + SERVICE_NAME + "__GRPC__PORT";

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(GetPort(API_PORT_NAME), o => o.Protocols = HttpProtocols.Http1);
                        options.ListenAnyIP(GetPort(GRPC_PORT_NAME), o => o.Protocols = HttpProtocols.Http2);
                    });
                    webBuilder.UseStartup<Startup>();
                });
        private static int GetPort(string keyName)
        {
            var str = Environment.GetEnvironmentVariable(keyName, EnvironmentVariableTarget.Process);
            return int.Parse(str);
        }
    }
}