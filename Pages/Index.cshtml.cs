using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using IPGeolocation;
using System.Net.Http;

namespace JamFan21.Pages
{
    public class JamulusServers
    {
        public long numip { get; set; } public long port { get; set; } public string country { get; set; }
        public long maxclients { get; set; } public long perm { get; set; } public string name { get; set; }
        public string ipaddrs { get; set; } public string city { get; set; } public string ip { get; set; }
        public long ping { get; set; } public Os ps { get; set; } public string version { get; set; }
        public string versionsort { get; set; } public long nclients { get; set; } public long index { get; set; }
        public Client[] clients { get; set; } public long? port2 { get; set; }
    }

    public class Client
    {
        public long chanid { get; set; } public string country { get; set; } public string instrument { get; set; }
        public string skill { get; set; } public string name { get; set; } public string city { get; set; }
    }

    public enum Os { Linux, MacOs, Windows };

    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }


        Dictionary<string, string> JamulusListURLs = new Dictionary<string, string>()
        {
//            {"Default", "http://jamulus.softins.co.uk/servers.php?central=jamulus.fischvolk.de:22124" },
 //           {"All Genres", "http://jamulus.softins.co.uk/servers.php?central=jamulusallgenres.fischvolk.de:22224" },
  //          { "Genre Rock", "http://jamulus.softins.co.uk/servers.php?central=jamulusrock.fischvolk.de:22424" },
   //         { "Genre Jazz", "http://jamulus.softins.co.uk/servers.php?central=jamulusjazz.fischvolk.de:22324" },
            { "Genre Classical/Folk/Choir", "http://jamulus.softins.co.uk/servers.php?central=jamulusclassical.fischvolk.de:22524" }
        };

        Dictionary<string, string> LastReportedList = new Dictionary<string, string>();

        protected async Task MineLists()
        {
            foreach (var key in JamulusListURLs.Keys)
            {
                using var client = new HttpClient();
                var serverJson = await client.GetStringAsync(JamulusListURLs[key]);
                LastReportedList[key] = serverJson;
            }
        }

        protected int DistanceFromMe(string ipThem)
        {
            IPGeolocationAPI api = new IPGeolocationAPI("7b09ec85eaa84128b48121ccba8cec2a");

            GeolocationParams geoParams = new GeolocationParams();
            geoParams.SetIPAddress(HttpContext.Connection.RemoteIpAddress.ToString());
            geoParams.SetFields("geo,time_zone,currency");
            Geolocation geolocation = api.GetGeolocation(geoParams);
            var clientLatitude = Convert.ToDouble(geolocation.GetLatitude());
            var clientLongitude = Convert.ToDouble(geolocation.GetLongitude());

            GeolocationParams geoParamssvr = new GeolocationParams();
            geoParamssvr.SetIPAddress(ipThem);
            geoParamssvr.SetFields("geo,time_zone,currency");
            Geolocation geolocationsvr = api.GetGeolocation(geoParamssvr);
            double serverLatitude = Convert.ToDouble(geolocationsvr.GetLatitude());
            double serverLongitude = Convert.ToDouble(geolocationsvr.GetLongitude());

            // https://www.simongilbert.net/parallel-haversine-formula-dotnetcore/
            const double EquatorialRadiusOfEarth = 6371D;
            const double DegreesToRadians = (Math.PI / 180D);
            var deltalat = (serverLatitude - clientLatitude) * DegreesToRadians;
            var deltalong = (serverLongitude - clientLongitude) * DegreesToRadians;
            var a = Math.Pow(
                Math.Sin(deltalat / 2D), 2D) +
                Math.Cos(clientLatitude * DegreesToRadians) *
                Math.Cos(serverLatitude * DegreesToRadians) *
                Math.Pow(Math.Sin(deltalong / 2D), 2D);
            var c = 2D * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1D - a));
            var d = EquatorialRadiusOfEarth * c;
            return Convert.ToInt32(d);
        }

    class ServersForMe
        {
            public ServersForMe(string ip, int distance) { serverIpAddress = ip; distanceAway = distance; }
            public string serverIpAddress;
            public int distanceAway;
        }

        public async Task<string> GetGutsRightNow() //
        {
            await MineLists(); // eventually this will be smart, go ahead and call it.

            // Now for each last reported list, extract all the hmmm servers for now. all them servers by LIST, NAME, CITY, IP ADDRESS, # OF PEOPLE.
            // cuz I wanna add a new var: Every distance to this client!
            // so eager, just get them distances!

            var allMyServers = new List<ServersForMe>();

            foreach (var key in LastReportedList.Keys)
            {
                var serversOnList = System.Text.Json.JsonSerializer.Deserialize<List<JamulusServers>>(LastReportedList[key]);
                foreach (var server in serversOnList)
                {
                    allMyServers.Add(new ServersForMe(server.ip, DistanceFromMe(server.ip)));
                }
            }
            return allMyServers.Count().ToString();
        }

        public string RightNow
        {
            get
            {
                var v = GetGutsRightNow();
                v.Wait();
                return v.Result;
            }
            set
            {
            }
        }
    }
}

