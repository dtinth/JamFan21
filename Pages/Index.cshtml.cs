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
            {"Default", "http://jamulus.softins.co.uk/servers.php?central=jamulus.fischvolk.de:22124" },
            {"All Genres", "http://jamulus.softins.co.uk/servers.php?central=jamulusallgenres.fischvolk.de:22224" }
            ,{ "Genre Rock", "http://jamulus.softins.co.uk/servers.php?central=jamulusrock.fischvolk.de:22424" }
            ,{ "Genre Jazz", "http://jamulus.softins.co.uk/servers.php?central=jamulusjazz.fischvolk.de:22324" }
            ,{ "Genre Classical/Folk/Choir", "http://jamulus.softins.co.uk/servers.php?central=jamulusclassical.fischvolk.de:22524" }
        };

        Dictionary<string, string> LastReportedList = new Dictionary<string, string>();
        DateTime? LastReportedListGatheredAt = null;

        protected async Task MineLists()
        {
            if (LastReportedListGatheredAt != null)
                if (DateTime.Now < LastReportedListGatheredAt.Value.AddSeconds(60))
                    return; // data we have was gathered within the last minute.

            foreach (var key in JamulusListURLs.Keys)
            {
                using var client = new HttpClient();
                var serverJson = await client.GetStringAsync(JamulusListURLs[key]);
                LastReportedList[key] = serverJson;
            }
        }

        class CachedGeolocation
        {
            public CachedGeolocation(int d, double lat, double longi) { queriedThisDay = d;latitude = lat;longitude = longi; }
            public int queriedThisDay;
            public double latitude;
            public double longitude;
        }

        Dictionary<string, CachedGeolocation> geocache = new Dictionary<string, CachedGeolocation>();

        protected void SmartGeoLocate(string ip, ref double latitude, ref double longitude)
        {
            // for any IP address, use a cached object if it's not too old.
            if(geocache.ContainsKey(ip))
            {
                var cached = geocache[ip];
                if(cached.queriedThisDay + 1 < DateTime.Now.DayOfYear)
                {
                    latitude = cached.latitude;
                    longitude = cached.longitude;
                    return;
                }
            }

            // don't have cached data, or it's too old.
            IPGeolocationAPI api = new IPGeolocationAPI("7b09ec85eaa84128b48121ccba8cec2a");
            GeolocationParams geoParams = new GeolocationParams();
            geoParams.SetIPAddress(ip);
            geoParams.SetFields("geo,time_zone,currency");
            Geolocation geolocation = api.GetGeolocation(geoParams);
            latitude = Convert.ToDouble(geolocation.GetLatitude());
            longitude = Convert.ToDouble(geolocation.GetLongitude());
            geocache[ip] = new CachedGeolocation(DateTime.Now.DayOfYear, latitude, longitude);
        }

        protected int DistanceFromMe(string ipThem)
        {
            string clientIP = HttpContext.Connection.RemoteIpAddress.ToString();
            if (clientIP.Length < 5)
                clientIP = "104.215.148.63"; //microsoft as test 

            double clientLatitude = 0.0, clientLongitude = 0.0, serverLatitude = 0.0, serverLongitude = 0.0;
            SmartGeoLocate(clientIP, ref clientLatitude, ref clientLongitude);
            SmartGeoLocate(ipThem, ref serverLatitude, ref serverLongitude);

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
            public ServersForMe(string cat, string ip, string na, string ci, int distance, int peeps, string w) { category = cat;  serverIpAddress = ip; name = na; city = ci; distanceAway = distance; people = peeps; who = w; }
            public string category;
            public string serverIpAddress;
            public string name;
            public string city;
            public int distanceAway;
            public int people;
            public string who;
        }

        public async Task<string> GetGutsRightNow() //
        {
            await MineLists();

            // Now for each last reported list, extract all the hmmm servers for now. all them servers by LIST, NAME, CITY, IP ADDRESS, # OF PEOPLE.
            // cuz I wanna add a new var: Every distance to this client!
            // so eager, just get them distances!

            var allMyServers = new List<ServersForMe>();

            foreach (var key in LastReportedList.Keys)
            {
                var serversOnList = System.Text.Json.JsonSerializer.Deserialize<List<JamulusServers>>(LastReportedList[key]);
                foreach (var server in serversOnList)
                {
                    int people = 0;
                    if (server.clients != null)
                        people = server.clients.GetLength(0);
                    if (people < 2)
                        continue; // just fuckin don't care about 0 or even 1?

                    string who = "";
                    foreach(var guy in server.clients)
                    {
                        string slimmerInstrument = guy.instrument;
                        if (slimmerInstrument == "-")
                            slimmerInstrument = "";

                        var newpart = "<b>" + guy.name + "</b>" + " <i>" + slimmerInstrument + "</i>";
                        newpart = newpart.Replace(" ", "&nbsp;"); // names and instruments have spaces too
                        who = who + newpart + ", ";
                    }
                    who = who.Substring(0, who.Length - 2); // chop that last comma!

                    allMyServers.Add(new ServersForMe(key, server.ip, server.name, server.city, DistanceFromMe(server.ip), people, who));
                }
            }

            IEnumerable<ServersForMe> sortedByDistanceAway = allMyServers.OrderBy(svr => svr.distanceAway);

            string output = "<table border='1'><tr><th>Category<th>Name<th>City<th>IP Address<th>&nbsp;#&nbsp;<th>Who</tr>";
            foreach (var s in sortedByDistanceAway)
            {
                if (s.people > 1) // more than one please
                    output += "<tr><td>" + s.category + "<td>" + s.name + "<td>" + s.city + "<td>" + s.serverIpAddress + "<td>" + s.people + "<td>" + s.who + "</tr>"; ;
            }
            output += "</table>";
            return output;
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

