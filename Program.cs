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
                    ;
                    /*
                      .UseKestrel(
                        options =>
                        {
                            options.ListenAnyIP(7999);
// options.ListenAnyIP(7998, listenOptions => {   listenOptions.UseHttps("mycert.pfx", "password"); }); 
                        }
                        );
                    */
                });
    }
}
