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
using Newtonsoft.Json.Linq;

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
            string cookieValueFromReq = Request.Cookies["searchTerms"];
            SearchTerms = cookieValueFromReq;
        }

        [BindProperty]
        public string SearchTerms { get; set; }
        public void OnPost()
        {
            Console.WriteLine(SearchTerms);
            string key = "searchTerms";
            string value = SearchTerms;
            Response.Cookies.Append(key, value);
        }

        Dictionary<string, string> JamulusListURLs = new Dictionary<string, string>()
        {
            {"Default", "http://jamulus.softins.co.uk/servers.php?central=jamulus.fischvolk.de:22124" }
            ,{"All Genres", "http://jamulus.softins.co.uk/servers.php?central=jamulusallgenres.fischvolk.de:22224" }
            ,{ "Genre Rock", "http://jamulus.softins.co.uk/servers.php?central=jamulusrock.fischvolk.de:22424" }
            ,{ "Genre Jazz", "http://jamulus.softins.co.uk/servers.php?central=jamulusjazz.fischvolk.de:22324" }
            ,{ "Genre Classical/Folk/Choir", "http://jamulus.softins.co.uk/servers.php?central=jamulusclassical.fischvolk.de:22524" }
        };

        static Dictionary<string, string> LastReportedList = new Dictionary<string, string>();
        static DateTime? LastReportedListGatheredAt = null;

        protected async Task MineLists()
        {
            if (LastReportedListGatheredAt != null)
            {
                if (DateTime.Now < LastReportedListGatheredAt.Value.AddSeconds(60))
                {
                    //                    Console.WriteLine("Data is less than 60 seconds old, and cached data is adequate.");
                    return; // data we have was gathered within the last minute.
                }
            }

            using var client = new HttpClient();

            var serverStates = new Dictionary<string, Task<string>>();

            foreach (var key in JamulusListURLs.Keys)
            {
                serverStates.Add(key, client.GetStringAsync(JamulusListURLs[key]));
            }

            foreach (var key in JamulusListURLs.Keys)
            {
                LastReportedList[key] = serverStates[key].Result;
            }

            LastReportedListGatheredAt = DateTime.Now;
        }

        class CachedGeolocation
        {
            public CachedGeolocation(int d, double lat, double longi) { queriedThisDay = d; latitude = lat; longitude = longi; }
            public int queriedThisDay;
            public double latitude;
            public double longitude;
        }

        static Dictionary<string, CachedGeolocation> geocache = new Dictionary<string, CachedGeolocation>();

        protected void SmartGeoLocate(string ip, ref double latitude, ref double longitude)
        {
            // for any IP address, use a cached object if it's not too old.
            if (geocache.ContainsKey(ip))
            {
                var cached = geocache[ip];
                if (cached.queriedThisDay + 1 < DateTime.Now.DayOfYear)
                {
                    latitude = cached.latitude;
                    longitude = cached.longitude;
                    return;
                }
            }

            // don't have cached data, or it's too old.
            // NOWEVER, THIS SHIT IF OFFLINE
            /*
            IPGeolocationAPI api = new IPGeolocationAPI("7b09ec85eaa84128b48121ccba8cec2a");
            GeolocationParams geoParams = new GeolocationParams();
            geoParams.SetIPAddress(ip);
            geoParams.SetFields("geo,time_zone,currency");
            Geolocation geolocation = api.GetGeolocation(geoParams);
            latitude = Convert.ToDouble(geolocation.GetLatitude());
            longitude = Convert.ToDouble(geolocation.GetLongitude());
            geocache[ip] = new CachedGeolocation(DateTime.Now.DayOfYear, latitude, longitude);
            */
        }

        protected int DistanceFromMe(string lat, string lon)
        {
            // i'm in seattle
            double clientLatitude = 47.6, clientLongitude = -122.3, serverLatitude = float.Parse(lat), serverLongitude = float.Parse(lon);
//            SmartGeoLocate(clientIP, ref clientLatitude, ref clientLongitude);
//            SmartGeoLocate(ipThem, ref serverLatitude, ref serverLongitude);

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
            public ServersForMe(string cat, string ip, string na, string ci, int distance, string w, int count) 
                { category = cat;  serverIpAddress = ip; name = na; city = ci; distanceAway = distance; who = w; usercount = count; }
            public string category;
            public string serverIpAddress;
            public string name;
            public string city;
            public int distanceAway;
            public string who;
            public int usercount;
        }

        public string HighlightUserSearchTerms(string str)
        {
            if(SearchTerms != null)
                if(SearchTerms.Length > 0)
                {
                    foreach (var term in SearchTerms.Split(' '))
                        if(term.Length > 0) // yeah i guess split cares about um two spaces, gives you a blank word yo
                            str = str.Replace(term, string.Format("<font size='+2' color='green'>{0}</font>", term), true, null); 
                }
            return str;
        }

        public class LatLong
        {
            public LatLong(string la, string lo) { lat = la; lon = lo; }
            public string lat;
            public string lon ;
        }

        protected static Dictionary<string, LatLong> mapToLatLongs = new Dictionary<string, LatLong>();

        public void PlaceToLatLon(string place, ref string lat, ref string lon, string country)
        {
            if (place.Length < 2)
                return; // not exactly bad, but unkonwn!
            if (place == "yourCity")
                return;

            if (mapToLatLongs.ContainsKey(place))
            {
                lat = mapToLatLongs[place].lat;
                lon = mapToLatLongs[place].lon;
                return;
            }

            string encodedplace = System.Web.HttpUtility.UrlEncode(place);
            string endpoint = string.Format("https://api.opencagedata.com/geocode/v1/json?q={0}&key=4fc3b2001d984815a8a691e37a28064c", encodedplace);
            Console.WriteLine(endpoint);
            using var client = new HttpClient();
            System.Threading.Tasks.Task<string> task = client.GetStringAsync(endpoint);
            task.Wait();
            string s = task.Result;
            JObject latLongJson = JObject.Parse(s);
            string typeOfMatch = (string)latLongJson["results"][0]["components"]["_type"] ;
            if (("neighbourhood" == typeOfMatch) ||
                ("village" == typeOfMatch) ||
                ("city" == typeOfMatch) ||
                ("county" == typeOfMatch) ||
                ("municipality" == typeOfMatch) ||
                ("administrative" == typeOfMatch) ||
                ("state" == typeOfMatch) ||
                ("country" == typeOfMatch))
            {
                lat = (string)latLongJson["results"][0]["geometry"]["lat"];
                lon = (string)latLongJson["results"][0]["geometry"]["lng"];
                mapToLatLongs[place] = new LatLong(lat, lon);
            }
            else
            {
                // DUMP THIS LATER AND RELY ON THE IPADDR-TO-LATLONG HERE
                // Try just using the country tehy passed me.
                /* apparently this can crash me cuz i didn't tweak user entry
                encodedplace = System.Web.HttpUtility.UrlEncode(country);
                endpoint = string.Format("https://api.opencagedata.com/geocode/v1/json?q={0}&key=4fc3b2001d984815a8a691e37a28064c", encodedplace);
                Console.WriteLine(endpoint);
                task = client.GetStringAsync(endpoint);
                task.Wait();
                s = task.Result;
                latLongJson = JObject.Parse(s);
                typeOfMatch = (string)latLongJson["results"][0]["components"]["_type"];
                if ("country" == typeOfMatch)
                {
                    lat = (string)latLongJson["results"][0]["geometry"]["lat"];
                    lon = (string)latLongJson["results"][0]["geometry"]["lng"];
                    mapToLatLongs[place] = new LatLong(lat, lon);
                    return;
                }
                */

                // Couldn't find anything. do this instead:
                mapToLatLongs[place] = new LatLong("", ""); 
            }
        }

        public async Task<string> GetGutsRightNow() 
        {
            await MineLists();

            // Now for each last reported list, extract all the hmmm servers for now. all them servers by LIST, NAME, CITY, IP ADDRESS
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
                    if (people < 1)
                        continue; // just fuckin don't care about 0 or even 1. MAYBE I DO WANNA NOTICE MY FRIEND ALL ALONE SOMEWHERE THO!!!!
                    /// EMPTY SERVERS CAN KICK ROCKS
                    /// SERVERS WITH ONE PERSON MIGHT BE THE PERSON I'M SEARCHING FOR

                    string who = "";
                    foreach(var guy in server.clients)
                    {
                        string slimmerInstrument = guy.instrument;
                        if (slimmerInstrument == "-")
                            slimmerInstrument = "";

                        if (slimmerInstrument.Length > 0) // if there's no length to instrument, don't add a space for it.
                            slimmerInstrument = " " + slimmerInstrument;

                        var nam = guy.name.Trim();
                        nam = nam.Replace("  ", " "); // don't want crazy space names
                        nam = nam.Replace("  ", " "); // don't want crazy space names
                        nam = nam.Replace("  ", " "); // don't want crazy space names

                        var newpart = "<b>" + nam + "</b>" + "<i><font size='-1'>" + slimmerInstrument + "</font></i>";
                        newpart = newpart.Replace(" ", "&nbsp;"); // names and instruments have spaces too
                        who = who + newpart + ", ";
                    }
                    who = who.Substring(0, who.Length - 2); // chop that last comma!

                    string lat = "";
                    string lon = "";
                    string place = "";
                    if (server.city.Length > 1)
                        place = server.city;
                    if (server.country.Length > 1)
                    {
                        if (place.Length > 1)
                            place += ", ";
                        place += server.country;
                    }

                    PlaceToLatLon(place, ref lat, ref lon, server.country);

                    //                    allMyServers.Add(new ServersForMe(key, server.ip, server.name, server.city, DistanceFromMe(server.ip), who, people));
                    int dist = 0;
                    if (lat.Length > 1 || lon.Length > 1)
                        dist = DistanceFromMe(lat, lon);

                    allMyServers.Add(new ServersForMe(key, server.ip, server.name, server.city, dist, who, people));
                }
            }

            IEnumerable<ServersForMe> sortedByDistanceAway = allMyServers.OrderBy(svr => svr.distanceAway);
            //IEnumerable<ServersForMe> sortedByMusicianCount = allMyServers.OrderByDescending(svr => svr.usercount);

            string output = "<table border='1'><tr><th><font size='-1'>Server Address</font><th>Category<th>Name<th>City<th>Who</tr>";

            // First all with more than one musician:
            foreach (var s in sortedByDistanceAway)
            {
                if (s.usercount > 1)
                {
                    var newline = "<tr><td><font size='-1'>" + s.serverIpAddress + "</font><td>" +
                        s.category.Replace("Genre ", "") +
                        "<td><font size='-1'>" + HighlightUserSearchTerms(s.name) +
                        "</font><td>" + HighlightUserSearchTerms(s.city) + "<td>" + HighlightUserSearchTerms(s.who) + "</tr>"; ;
                    output += newline;
                }
            }
            foreach (var s in sortedByDistanceAway)
            {
                if (s.usercount == 1)
                {
                    var newline = "<tr><td><font size='-1'>" + s.serverIpAddress + "</font><td>" +
                        s.category.Replace("Genre ", "") +
                        "<td><font size='-1'>" + HighlightUserSearchTerms(s.name) +
                        "</font><td>" + HighlightUserSearchTerms(s.city) + "<td>" + HighlightUserSearchTerms(s.who) + "</tr>"; ;
                    output += newline;
                }
            }

            output += "</table>";
            return output;
        }

        private static System.Threading.Mutex m_serializerMutex = new System.Threading.Mutex();
        public string RightNow
        {
            get
            {
                m_serializerMutex.WaitOne();
                try
                {
                    /* no geolocate for now
                    string ipaddr = HttpContext.Connection.RemoteIpAddress.ToString();
                    if (ipaddr.Length > 5)
                    {
                        Console.Write("Refresh request from ");
                        IPGeolocationAPI api = new IPGeolocationAPI("7b09ec85eaa84128b48121ccba8cec2a");
                        GeolocationParams geoParams = new GeolocationParams();
                        geoParams.SetIPAddress(ipaddr);
                        geoParams.SetFields("geo,time_zone,currency");
                        Geolocation geolocation = api.GetGeolocation(geoParams);
                        Console.WriteLine(geolocation.GetCity());
                    }
                    */

                    var v = GetGutsRightNow();
                    v.Wait();
                    return v.Result;
                }
                finally { m_serializerMutex.ReleaseMutex(); }
            }
            set
            {
            }
        }
    }
}

