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

        public async Task<string> GetGutsRightNow()
        {
            using var client = new HttpClient();
            var serverJson = await client.GetStringAsync("http://jamulus.softins.co.uk/servers.php?central=jamulus.fischvolk.de:22124");
            var servers =  System.Text.Json.JsonSerializer.Deserialize<List<JamulusServers>>(serverJson);

                    IPGeolocationAPI api = new IPGeolocationAPI("7b09ec85eaa84128b48121ccba8cec2a");

                    GeolocationParams geoParams = new GeolocationParams();
                    geoParams.SetIPAddress(HttpContext.Connection.RemoteIpAddress.ToString()) ;
                    geoParams.SetFields("geo,time_zone,currency");
                    Geolocation geolocation = api.GetGeolocation(geoParams);
                    var clientLatitude = Convert.ToDouble( geolocation.GetLatitude() ) ;
                    var clientLongitude = Convert.ToDouble( geolocation.GetLongitude() );

var output = "" ;
            foreach(var server in servers)
            {
                // server has people?
                if(server.clients != null)
                {
output = output + " another " ;
                    GeolocationParams geoParamssvr = new GeolocationParams();
                    geoParamssvr.SetIPAddress(server.ipaddrs);
output = output +  "xxx " + server.ipaddrs.ToString();
                    geoParamssvr.SetFields("geo,time_zone,currency");
                    Geolocation geolocationsvr = api.GetGeolocation(geoParamssvr);
                    double serverLatitude = Convert.ToDouble( geolocationsvr.GetLatitude() );
                    double serverLongitude = Convert.ToDouble( geolocationsvr.GetLongitude() ) ;

                    // this is the client's lat-long... ok on server?
//                    return clientLatitude.ToString() + " " + clientLongitude.ToString() + " " + serverLatitude.ToString() + " " + serverLongitude.ToString()  ;

                    const double EquatorialRadiusOfEarth = 6371D;
                    const double DegreesToRadians = (Math.PI / 180D);
//                    https://www.simongilbert.net/parallel-haversine-formula-dotnetcore/
var deltalat = (serverLatitude - clientLatitude) * DegreesToRadians;
var deltalong = (serverLatitude - clientLatitude) * DegreesToRadians;
var a = Math.Pow(
Math.Sin(deltalat /  2D), 2D) +
Math.Cos(clientLatitude * DegreesToRadians) *
Math.Cos(serverLatitude * DegreesToRadians) *
Math.Pow(Math.Sin(deltalong / 2D), 2D);
var c = 2D * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1D - a));
var d = EquatorialRadiusOfEarth * c;
output = output + " " + d.ToString() ;
                }
            }
            return output ;
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

