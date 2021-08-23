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

        public const string SEARCH_TERMS = "searchTerms";

        public void OnGet()
        {
            SearchTerms = Request.Cookies[SEARCH_TERMS];
        }

        [BindProperty]
        public string SearchTerms { get; set; }
        public void OnPost()
        {
            if (null != SearchTerms)
            {
                Console.WriteLine("Someone's search terms: " + SearchTerms);
                Response.Cookies.Delete(SEARCH_TERMS);
                Response.Cookies.Append(SEARCH_TERMS, SearchTerms);
                Console.WriteLine("Append called.");
                //            RedirectToPage(".");
            }
        }

        Dictionary<string, string> JamulusListURLs = new Dictionary<string, string>()
        {
                        {"Any Genre 1", "http://jamulus.softins.co.uk/servers.php?central=anygenre1.jamulus.io:22124" }
                        ,{"Any Genre 2", "http://jamulus.softins.co.uk/servers.php?central=anygenre2.jamulus.io:22224" } 
                        ,{"Any Genre 3", "http://jamulus.softins.co.uk/servers.php?central=anygenre3.jamulus.io:22624" } 
                        ,{"Genre Rock",  "http://jamulus.softins.co.uk/servers.php?central=rock.jamulus.io:22424" }
                        ,{"Genre Jazz",  "http://jamulus.softins.co.uk/servers.php?central=jazz.jamulus.io:22324" }
                        ,{"Genre Classical/Folk",  "http://jamulus.softins.co.uk/servers.php?central=classical.jamulus.io:22524" }
                        ,{"Genre Choral/BBShop",  "http://jamulus.softins.co.uk/servers.php?central=choral.jamulus.io:22724" } 

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

        /*
        class CachedGeolocation
        {
            public CachedGeolocation(int d, double lat, double longi) 
            { 
            //    queriedThisDay = d; 
                latitude = lat; 
                longitude = longi; 
            }
//            public int queriedThisDay;
            public double latitude;
            public double longitude;
        }
        */

//        static Dictionary<string, CachedGeolocation> geocache = new Dictionary<string, CachedGeolocation>();

        protected void SmartGeoLocate(string ip, ref double latitude, ref double longitude)
        {
            // for any IP address, use a cached object if it's not too old.
            if (m_ipAddrToLatLong.ContainsKey(ip))
            {
                var cached = m_ipAddrToLatLong[ip];
//                if (cached.queriedThisDay + 1 < DateTime.Now.DayOfYear)
                {
                    latitude = double.Parse(cached.lat);
                    longitude = double.Parse(cached.lon);
//Console.Write("."); //an actual user refresh so let's see a visual indication
                    return;
                }
            }

            // don't have cached data, or it's too old.
            // NOWEVER, THIS SHIT IF OFFLINE

            IPGeolocationAPI api = new IPGeolocationAPI("79fdbc34bdbd42f8aa3e14896598c40e");
            GeolocationParams geoParams = new GeolocationParams();
            geoParams.SetIPAddress(ip);
            geoParams.SetFields("geo,time_zone,currency");
            Geolocation geolocation = api.GetGeolocation(geoParams);
            latitude = Convert.ToDouble(geolocation.GetLatitude());
            longitude = Convert.ToDouble(geolocation.GetLongitude());
            m_ipAddrToLatLong[ip] = new LatLong(latitude.ToString(), longitude.ToString());
            Console.WriteLine("A client IP has been cached: " + ip + " " + geolocation.GetCity());
        }

//        protected static Dictionary<string, LatLong> m_ipAddrToLatLong = new Dictionary<string, LatLong>();


        protected int DistanceFromClient(string lat, string lon)
        {
            var serverLatitude = float.Parse(lat);
            var serverLongitude = float.Parse(lon);

            string clientIP = HttpContext.Connection.RemoteIpAddress.ToString();
            if (clientIP.Length < 5)
                clientIP = "104.215.148.63"; //microsoft as test 
            double clientLatitude = 0.0;
            double clientLongitude = 0.0;
            SmartGeoLocate(clientIP, ref clientLatitude, ref clientLongitude);

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

        /*
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
        */

    class ServersForMe
        {
            public ServersForMe(string cat, string ip, string na, string ci, int distance, string w, Client[] originallyWho, int count)
            {
                category = cat;
                serverIpAddress = ip;
                name = na;
                city = ci;
                distanceAway = distance;
                who = w;
                whoObjectFromSourceData = originallyWho ;
                usercount = count;
            }
            public string category;
            public string serverIpAddress;
            public string name;
            public string city;
            public int distanceAway;
            public string who;
            public Client[] whoObjectFromSourceData; // just to get the hash to work later. the who string is decorated but this is just data.
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

        protected static Dictionary<string, LatLong> m_PlaceNameToLatLong = new Dictionary<string, LatLong>();
        protected static Dictionary<string, LatLong> m_ipAddrToLatLong = new Dictionary<string, LatLong>();

        protected static bool CallOpenCage(string placeName, ref string lat, ref string lon)
        {
            if (placeName.Length < 3)
                return false;
            string encodedplace = System.Web.HttpUtility.UrlEncode(placeName);
            string endpoint = string.Format("https://api.opencagedata.com/geocode/v1/json?q={0}&key=4fc3b2001d984815a8a691e37a28064c", encodedplace);
            using var client = new HttpClient();
            System.Threading.Tasks.Task<string> task = client.GetStringAsync(endpoint);
            task.Wait();
            string s = task.Result;
            JObject latLongJson = JObject.Parse(s);
            if (latLongJson["results"].HasValues)
            {
                string typeOfMatch = (string)latLongJson["results"][0]["components"]["_type"];
                if (("neighbourhood" == typeOfMatch) ||
                    ("village" == typeOfMatch) ||
                    ("city" == typeOfMatch) ||
                    ("county" == typeOfMatch) ||
                    ("municipality" == typeOfMatch) ||
                    ("administrative" == typeOfMatch) ||
                    ("state" == typeOfMatch) ||
                    ("boundary" == typeOfMatch) ||
                    ("country" == typeOfMatch))
                {
                    lat = (string)latLongJson["results"][0]["geometry"]["lat"];
                    lon = (string)latLongJson["results"][0]["geometry"]["lng"];
                    m_PlaceNameToLatLong[placeName.ToUpper()] = new LatLong(lat, lon);
                    return true;
                }
            }
            return false;
        }

        public void PlaceToLatLon(string serverPlace, string userPlace, string ipAddr, ref string lat, ref string lon)
        {
            ipAddr = ipAddr.Trim();
            serverPlace = serverPlace.Trim();
            userPlace = userPlace.Trim();

            System.Diagnostics.Debug.Assert(serverPlace.ToUpper() == serverPlace);
            System.Diagnostics.Debug.Assert(userPlace.ToUpper() == userPlace);

            if (m_PlaceNameToLatLong.ContainsKey(serverPlace))
            {
                lat = m_PlaceNameToLatLong[serverPlace].lat;
                lon = m_PlaceNameToLatLong[serverPlace].lon;
                return;
            }

            if (m_PlaceNameToLatLong.ContainsKey(userPlace))
            {
                lat = m_PlaceNameToLatLong[userPlace].lat;
                lon = m_PlaceNameToLatLong[userPlace].lon;
                return;
            }

            if (m_ipAddrToLatLong.ContainsKey(ipAddr))
            {
                lat = m_ipAddrToLatLong[ipAddr].lat;
                lon = m_ipAddrToLatLong[ipAddr].lon;
                return;
            }

            if (serverPlace.Length > 1)
                if (serverPlace!= "yourCity")
                {
                    if (CallOpenCage(serverPlace, ref lat, ref lon))
                    {
                        Console.WriteLine("Used server location: " + serverPlace);
                        return;
                    }
                    Console.WriteLine("Server location failed: " + serverPlace);
               }

            // Ok, user country. more general lat long.
            if (m_PlaceNameToLatLong.ContainsKey(userPlace))
            {
                lat = m_PlaceNameToLatLong[userPlace].lat;
                lon = m_PlaceNameToLatLong[userPlace].lon;
                return;
            }

            // didn't find the top user country... ask someone for directions.

            // THE SERVER SELF-REPORT DIDN'T TRANSLATE INTO A LAT-LONG...
            // SO IF THERE ARE USERS THERE, HARVEST THEIR COUNTRY.
            {
//                if( userPlace.Contains(","))
                    if (CallOpenCage(userPlace, ref lat, ref lon))
                    {
                        Console.WriteLine("Used user location: " + userPlace);
                        return;
                    }
                Console.WriteLine("User location failed: " + userPlace);
            }

            if (ipAddr.Length > 5)
            {
                IPGeolocationAPI api = new IPGeolocationAPI("79fdbc34bdbd42f8aa3e14896598c40e");
                GeolocationParams geoParams = new GeolocationParams();
                geoParams.SetIPAddress(ipAddr);
                geoParams.SetFields("geo,time_zone,currency");
                Geolocation geolocation = api.GetGeolocation(geoParams);
                Console.WriteLine(ipAddr + " " + geolocation.GetCity());
                lat = geolocation.GetLatitude();
                lon = geolocation.GetLongitude();
                m_ipAddrToLatLong[ipAddr] = new LatLong(lat, lon);
                Console.WriteLine("AN IP geo has been cached.");
                return;
            }

            // Couldn't find anything. do this instead:
            m_ipAddrToLatLong[ipAddr] = new LatLong("", "");
        }

        static Dictionary<string, DateTime> cfs = new Dictionary<string, DateTime>(); // connection, first sighting
        static Dictionary<string, DateTime> cls = new Dictionary<string, DateTime>(); // connection, latest sighting

        // Here we note who s.who is, because we care how long a person has been on a server. Nothing more than that for now.
        protected void NotateWhoHere(string server, string who)
        {
//            Console.WriteLine(server, who);
            string hash = server + who;

            try
            {
                // maybe we never heard of them.
                if (false == cfs.ContainsKey(hash))
                {
                    cfs[hash] = DateTime.Now;
                    return; // don't forget the finally!
                }

                // ok, we heard of them. Have 10 minutes elapsed since we saw them last? Like, maybe nobody has run my app. So ten mins.
                if (DateTime.Now > cls[hash].AddMinutes(10))
                {
                    // Yeah? Restart their initial sighting clock.
                    cfs[hash] = DateTime.Now;
                }

                // we saw them recently. Just update their last Time Last Seen...
            }
            finally
            {
                cls[hash] = DateTime.Now;
            }
        }


        protected double DurationHereInMins(string server, string who)
        {
            string hash = server + who;
            if(cfs.ContainsKey(hash))
            {
                TimeSpan ts = DateTime.Now.Subtract(cfs[hash]);
                return ts.TotalMinutes;
            }
            return -1; //
        }

        protected string DurationHere(string server, string who)
        {
            string hash = server + who;
            if (false == cfs.ContainsKey(hash))
                return "";

            string show = "";
            while (true)
            {
                TimeSpan ts = DateTime.Now.Subtract(cfs[hash]);

                /*
                if (ts.Days > 0)
                {
                    show = ts.Days.ToString() + "d";
                    break;
                }
                if (ts.Hours > 0)
                {
                    show = ts.Hours.ToString() + "h";
                    break;
                }
                */

                if (ts.Hours > 0) // once an hour is elapsed, don't show nothin.
                    break;

                if (ts.TotalMinutes > 5)
                {
                    show = "(" + ts.Minutes.ToString() + "m)";
                    break;
                }

                // on the very first notice, i don't want this indicator, cuz it's gonna frustrate me with saw-just-onces
                if (ts.TotalMinutes > 1) // so let's see them for 1 minute before we show anything fancy
                    show = "<b>(just&nbsp;" +
                        "arrived)</b>"; // after 1 minute, until 6th minute, they've Just Arrived
                else
                    show = "(" + ts.Minutes.ToString() + "m)";

                break;
            }

            return " <font size='-1'><i>" + show + "</i></font>";
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

                    List<string> userCountries = new List<string>();

                    string who = "";
                    foreach(var guy in server.clients)
                    {
                        // Here we note who s.who is, because we care how long a person has been on a server. Nothing more than that for now.
                        NotateWhoHere(server.name, guy.name);

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

                        userCountries.Add(guy.country.ToUpper());
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

                    // usersCountry is the most common country reported by active users.
                    //var sorted = userCountries.GroupBy(v => v).OrderByDescending(g => g.Count());

                    var nameGroup = userCountries.GroupBy(x => x);
                    var maxCount = nameGroup.Max(g => g.Count());
                    var mostCommons = nameGroup.Where(x => x.Count() == maxCount).Select(x => x.Key).ToArray();
                    string usersCountry = mostCommons[0];


                    List<string> cities = new List<string>();
                    foreach (var guy in server.clients)
                    {
                        if (guy.country.ToUpper() == usersCountry)
                            if(guy.city.Length > 0)
                                cities.Add(guy.city.ToUpper());
                    }

                    string usersCity = "";
                    if (cities.Count > 0)
                    {
                        var citiGroup = cities.GroupBy(x => x);
                        var maxCountr = citiGroup.Max(g => g.Count());
                        var mostCommonCity = citiGroup.Where(x => x.Count() == maxCountr).Select(x => x.Key).ToArray();
                        if (mostCommonCity.GetLength(0) > 0)
                            usersCity = mostCommonCity[0];
                    }

                    string usersPlace = usersCountry;
                    if(usersCity.Length > 1)
                        usersPlace = usersCity + ", " + usersCountry;
                    usersCountry = null;

                        // Ideally, if the users also reveal a city most common among that country, we should add it.

                        //                    IEnumerable<ServersForMe> sortedByDistanceAway = allMyServers.OrderBy(svr => svr.distanceAway);
                        /// xxx 

                        PlaceToLatLon(place.ToUpper(), usersPlace.ToUpper(), server.ip, ref lat, ref lon);

                    //                    allMyServers.Add(new ServersForMe(key, server.ip, server.name, server.city, DistanceFromMe(server.ip), who, people));
                    int dist = 0;
                    if (lat.Length > 1 || lon.Length > 1)
                        dist = DistanceFromClient(lat, lon);

                    allMyServers.Add(new ServersForMe(key, server.ip, server.name, server.city, dist, who, server.clients, people));
                }
            }

            IEnumerable<ServersForMe> sortedByDistanceAway = allMyServers.OrderBy(svr => svr.distanceAway);
            //IEnumerable<ServersForMe> sortedByMusicianCount = allMyServers.OrderByDescending(svr => svr.usercount);

            string output = "<center><table class='table table-light table-hover table-striped'><tr><u><th>Genre<th>Name<th>City<th>Who</u></tr>";

            // First all with more than one musician:
            foreach (var s in sortedByDistanceAway)
            {
                if (s.usercount > 1)
                {
                    // if everyone here got here less than 14 minutes ago, then this is just assembled
                    string newJamFlag = "";
                    foreach(var user in s.whoObjectFromSourceData)
                    {
                        newJamFlag = "<font size='-2'><i>(just&nbsp;assembled)</i></font><br>";
                        if (DurationHereInMins(s.name, user.name) < 14)
                            continue;

                        newJamFlag = ""; // Someone's start time is too old, so nevermind.
                        break;
                    }

                    var newline = "<tr><td>" +
                        s.category.Replace("Genre ", "").Replace(" ", "&nbsp;") +
                        "<td><font size='-1'>" + HighlightUserSearchTerms(s.name) +
                        "</font><td>" + HighlightUserSearchTerms(s.city) + "<td>" + 
                        newJamFlag +
                        HighlightUserSearchTerms(s.who) + "</tr>"; ;
                    output += newline;
                }
            }
            foreach (var s in sortedByDistanceAway)
            {
                if (s.usercount == 1)
                {
                    var newline = "<tr><td>" +
                        s.category.Replace("Genre ", "").Replace(" ", "&nbsp;") +
                        "<td><font size='-1'>" + HighlightUserSearchTerms(s.name) +
                        "</font><td>" + 
                            HighlightUserSearchTerms(s.city) + 
                            "<td>" + 
                            HighlightUserSearchTerms(s.who) + 
                            DurationHere(s.name, s.whoObjectFromSourceData[0].name) +  // we know there's just one! i hope!
                            "</tr>"; ;
                    output += newline;
                }
            }

            output += "</table></center>";
            return output;
        }

        private static Dictionary<string, DateTime> clientIPLastVisit = new Dictionary<string, DateTime>();

        private static System.Threading.Mutex m_serializerMutex = new System.Threading.Mutex();
        public string RightNow
        {
            get
            {
                m_serializerMutex.WaitOne();
                try
                {
                    string ipaddr = HttpContext.Connection.RemoteIpAddress.ToString();

                    if (ipaddr.Length > 5)
                    {
                        IPGeolocationAPI api = new IPGeolocationAPI("79fdbc34bdbd42f8aa3e14896598c40e");
                        GeolocationParams geoParams = new GeolocationParams();
                        geoParams.SetIPAddress(ipaddr);
                        geoParams.SetFields("geo,time_zone,currency");
                        Geolocation geolocation = api.GetGeolocation(geoParams);
                        Console.Write("Refresh request from ");
                        Console.Write(geolocation.GetCity());

                        // Visually indicate if we last heard from this ipaddr
                        // after about 125 seconds has elapsed
                        if(clientIPLastVisit.ContainsKey(ipaddr))
                        {
                            var lastRefresh = clientIPLastVisit[ipaddr];
                            if (DateTime.Now < lastRefresh.AddSeconds(135))
                                if (DateTime.Now > lastRefresh.AddSeconds(115))
                                    Console.Write(" :)");
                        }
                        clientIPLastVisit[ipaddr] = DateTime.Now;

Console.WriteLine();
                    }

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

