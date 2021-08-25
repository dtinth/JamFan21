using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JamFan21
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
//                    ;
  //                  /*
                      .UseKestrel(
                        options =>
                        {
                            var httpPort = 80;

                            // Allow overriding port using the PORT environment variable.
                            if (Environment.GetEnvironmentVariable("PORT") != null) {
                                httpPort = int.Parse(Environment.GetEnvironmentVariable("PORT"));
                            }

                            options.ListenAnyIP(httpPort);

                            // Listen on port 443 if "jamfan.pfx" exists.
                            if (System.IO.File.Exists("jamfan.pfx")) {
                                options.ListenAnyIP(443, listenOptions => {
                                    listenOptions.UseHttps("jamfan.pfx", "jamfan");
                                });
                            }
                        }
                        );
    //                */
                });
    }
}
