using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace TaxiClient
{
    public static class Server
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<int> SendOrder(int userId, double longitudeFrom, double latitudeFrom, double longitudeTo, double latitudeTo)
        {

            var values = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "longitudeFrom", longitudeFrom.ToString("F9").Replace('.',',') },
                { "latitudeFrom", latitudeFrom.ToString("F9").Replace('.',',') },
                { "longitudeTo", longitudeTo.ToString("F9").Replace('.',',') },
                { "latitudeTo", latitudeTo.ToString("F9").Replace('.',',') }
            };

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://localhost:44368/Api/CreateOrder", content);
            int r;
            if(int.TryParse(await response.Content.ReadAsStringAsync(),out r))
            {
                return r;
            }
            return -2;   
        }
        public static async Task<OrderStatus> GetOrder()
        {
            WebResponse response;
            try
            {
                //#TODO int id = int.Parse(await SecureStorage.GetAsync("UserId"));
                int id = 2;
                var request = WebRequest.Create($"https://localhost:44368/Api/UserOrder?userId={id}");
                response = request.GetResponse();
            }
            catch
            {
                return null;
            }
            OrderStatus result;
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string r = reader.ReadLine();
                try
                {
                    result = JsonConvert.DeserializeObject<OrderStatus>(r);
                }
                catch
                {
                    return null;
                }
            }
            return result;
        }

        public static void CancelOrder(int userId)
        {
            var values = new Dictionary<string, string>
            {
                { "userId", userId.ToString() }
            };

            var content = new FormUrlEncodedContent(values);
            client.PostAsync("https://localhost:44368/Api/CancelOrder", content);
        }

    }

    public class OrderStatus
    {
        public int Status { get; set; }
        public double longitudeFrom { get; set; }
        public double latitudeFrom { get; set; }
        public double longitudeTo { get; set; }
        public double latitudeTo { get; set; }
        public double? longitudeDriver { get; set; }
        public double? latitudeDriver { get; set; }
        public double Cost { get; set; }
    }

}

