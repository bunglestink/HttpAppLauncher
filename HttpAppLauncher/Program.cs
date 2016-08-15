using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace HttpAppLauncher {
    public class Program {
        [STAThread]
        public static void Main(string[] args)
        {
            var portFlag = args.Where(s => s.StartsWith("--port=")).FirstOrDefault();
            var port = portFlag == null ? 5077 : int.Parse(portFlag.Substring(7));
            var url = string.Format("http://localhost:{0}", port);

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls(url)
                .Build();

            host.Run();
        }
    }

    public class Startup {
        readonly IDictionary<string, string> commandMap;

        public Startup()
        {
            commandMap = new Dictionary<string, string>() {
                {"code", @"C:\Program Files (x86)\Microsoft VS Code\Code.exe"},
                {"explorer", "explorer"},
            };
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async context => {
                Console.WriteLine("[{0}] {1} {2}",
                    DateTime.Now,
                    context.Request.Method,
                    context.Request.Path);
                var ip = context.Connection.RemoteIpAddress;
                if (ip.ToString() != "127.0.0.1") {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden.");
                    return;
                }
                if (context.Request.Method != "POST") {
                    context.Response.StatusCode = 405;
                    await context.Response.WriteAsync("Only POST supported.");
                    return;
                }
                using (var reader = new StreamReader(context.Request.Body)) {
                    var path = await reader.ReadToEndAsync();
                    var commands = context.Request.Path.ToString().Split('/').Where(s => s != "");
                    if (commands.Count() != 1) {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Not Found.");
                        return;
                    }
                    if (path.Contains("\"")) {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Bad request: double quotes not allowed.");
                        return;
                    }
                    var command = commands.First();
                    if (!commandMap.ContainsKey(command)) {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Not Found.");
                        return;
                    }
                    var windowsCommand = commandMap[commands.First()];
                    Process.Start(windowsCommand, string.Format("\"{0}\"", path));
                    context.Response.StatusCode = 204;
                    await context.Response.WriteAsync("");
                }
            });
        }
    }
}
