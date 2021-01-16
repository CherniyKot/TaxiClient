using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TaxiClient
{
    public static class Server
    {
        private static readonly HttpClient client = new HttpClient();
        public static DriverLocation[] GetDriversLocations()
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
            var request = WebRequest.Create("https://localhost:44368/Api/FreeDriversLocations");
            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch
            {
                return new DriverLocation[] { };
            }

            DriverLocation[] result;
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string r = reader.ReadLine();
                try
                {
                    result = JsonConvert.DeserializeObject<DriverLocation[]>(r);
                }
                catch
                {
                    return null;
                }
            }
            return result;
        }

        public static async Task<bool> SendOrder(int userId, double longitudeFrom, double latitudeFrom, double longitudeTo, double latitudeTo)
        {
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

            var values = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "longitudeFrom", longitudeFrom.ToString().Replace('.',',') },
                { "latitudeFrom", latitudeFrom.ToString().Replace('.',',') },
                { "longitudeTo", longitudeTo.ToString().Replace('.',',') },
                { "latitudeTo", latitudeTo.ToString().Replace('.',',') }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://localhost:44368/Api/CreateOrder", content);
            return response.IsSuccessStatusCode;
        }

    }

    public class DriverLocation
    {
        public int driverID { get; set; }
        public string driverName { get; set; }
        public string driverCar { get; set; }
        public double? longitude { get; set; }
        public double? latitude { get; set; }
    }


    public class OrderLocation
    {
        public int orderID { get; set; }
        public string userName { get; set; }
        public double longitudeFrom { get; set; }
        public double latitudeFrom { get; set; }
        public double longitudeTo { get; set; }
        public double latitudeTo { get; set; }
    }
}

