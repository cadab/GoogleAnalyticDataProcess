using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Google.Apis.Analytics.v3;
using Google.Apis.Analytics.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GoogleAnalyticDataProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            // Adding JSON file into IConfiguration.
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("config/appsettings.json", true, true).Build();

            string[] scopes = new string[] { AnalyticsService.Scope.Analytics }; // view and manage your Google Analytics data

            var keyFilePath = "config/analytics.p12";  // Downloaded from https://console.developers.google.com
            var serviceAccountEmail = config["ServiceAccountEmail"];  // found https://console.developers.google.com

            //loading the Key file
            var certificate = new X509Certificate2(keyFilePath, "notasecret", X509KeyStorageFlags.Exportable);
            var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(serviceAccountEmail)
            {
                Scopes = scopes
            }.FromCertificate(certificate));

            var service = new AnalyticsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Analytics API Sample",
            });


            while (true)
            {
                DataResource.RealtimeResource.GetRequest realtimeReq = service.Data.Realtime.Get(String.Format("ga:{0}", config["GAID"]), "rt:activeUsers");
                realtimeReq.Dimensions = "rt:country,rt:region,rt:city,rt:deviceCategory,rt:latitude,rt:longitude";
                realtimeReq.Sort = "rt:activeUsers";

                DataResource.RealtimeResource.GetRequest realtimePageViewsReq = service.Data.Realtime.Get(String.Format("ga:{0}", config["GAID"]), "rt:pageviews");
                realtimePageViewsReq.Dimensions = "rt:country,rt:region,rt:city,rt:deviceCategory,rt:latitude,rt:longitude";
                realtimePageViewsReq.Sort = "rt:pageviews";

                RealtimeData realtime = realtimeReq.Execute();
                RealtimeData realtimePageViews = realtimePageViewsReq.Execute();

                var usersRows = realtime.Rows.ToList();
                var pageRows = realtimePageViews.Rows.ToList();

                Console.WriteLine(DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss"));
                Console.WriteLine("Total Active Users: " + realtime.Rows.Sum(t => Convert.ToInt32(t[6])));
                Console.WriteLine("Total Page Views: " + realtimePageViews.Rows.Sum(t => Convert.ToInt32(t[6])));

                Console.WriteLine("-----------------------------------------");

                var userData = CreateData("RealtimeActiveUsers", usersRows);
                var pageData = CreateData("RealtimePageViews", pageRows);

                using (var webClient = new WebClient())
                {
                    webClient.Headers.Add("content-type", "application/json");
                    webClient.UploadString(config["ElasticSearchUrl"], String.Join("\r\n", userData) +"\r\n");

                    webClient.Headers.Add("content-type", "application/json");
                    webClient.UploadString(config["ElasticSearchUrl"], String.Join("\r\n", pageData) + "\r\n");
                }

                Thread.Sleep(Convert.ToInt32(config["IntervalMs"]));
            }
        }

        private static List<string> CreateData(string metricName, List<IList<string>> rows)
        {
            List<string> rts = new List<string>();
            foreach (var item in rows)
            {
                var t = new RealtimeElkData()
                {
                    MetricName = metricName,
                    Country = item[0],
                    Region = item[1],
                    City = item[2],
                    DeviceType = item[3],
                    Loc = new Location()
                    {
                        lat = item[4],
                        lon = item[5]
                    },
                    Value = Convert.ToInt32(item[6]),
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                rts.Add("{ \"index\":{ \"_id\": \"" + t.id + "\"} }");
                rts.Add(JsonConvert.SerializeObject(t));
            }

            return rts;
        }
    }

    public class RealtimeElkData
    {
        public string id => Country + Region + City + DeviceType +Loc.lat + Loc.lon + MetricName + Value + timestamp;
        public string timestamp { get; set; }
        public int Value { get; set; }
        public string MetricName { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string DeviceType { get; set; }
        public Location Loc { get; set; }
    }

    public class Location
    {
        public string lat { get; set; }

        public string lon { get; set; }
    }
}
