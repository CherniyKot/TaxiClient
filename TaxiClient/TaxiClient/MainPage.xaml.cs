using BingMapsRESTToolkit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using System.Globalization;
using System.Windows;
using System.Timers;
using System.Net;

namespace TaxiClient
{
    public partial class MainPage : ContentPage
    {
        Timer RP;
        public MainPage()
        {
            InitializeComponent();
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
            

            RP = new Timer(1000);
            RP.Elapsed += CheckOrderStatus;
            RP.Start();
        }
        public async Task<double> DrawPolyline(Polyline polyline, SimpleWaypoint from, SimpleWaypoint to)
        {
            polyline.Geopath.Clear();
            RouteRequest request = new RouteRequest()
            {
                RouteOptions = new RouteOptions()
                {
                    Avoid = new List<AvoidType>()
                    {
                        AvoidType.MinimizeTolls
                    },
                    TravelMode = TravelModeType.Driving,
                    DistanceUnits = DistanceUnitType.Kilometers,
                    RouteAttributes = new List<RouteAttributeType>()
                    {
                        RouteAttributeType.RoutePath
                    },
                    Optimize = RouteOptimizationType.TimeWithTraffic
                },
                Waypoints = new List<SimpleWaypoint>() { from, to },
                BingMapsKey = "IL4KJLeeFpfhl9mqdvw8~QuoEqNN-cICujvx87S4zcg~AgVzx_pxd7tdbOkilsKFwk5B-2iNInOs1OC1HXhm810TqteSR1TzPN87K6czdFAL"
            };
            var response = await request.Execute();
            if (response.StatusCode == 200)
                foreach (var pos in response.ResourceSets.First().Resources.OfType<Route>().First().RoutePath.Line.Coordinates.Select(e => new Position(e[0], e[1])).ToList())
                {
                    polyline.Geopath.Add(pos);
                }
            else DisplayAlert("Routing error", string.Concat(response.ErrorDetails ?? new[] { "Unknown error occured", "" }), "Ok");

            return response.ResourceSets.First().Resources.OfType<Route>().First().TravelDistance;
        }

        private async void Confirm(object sender, EventArgs e)
        {
            Geocoder geo = new Geocoder();
            var place1 = (await geo.GetPositionsForAddressAsync(PlaceFrom.Text)).FirstOrDefault();
            var place2 = (await geo.GetPositionsForAddressAsync(PlaceTo.Text)).FirstOrDefault();

            var res = await Server.SendOrder(2, place1.Latitude, place1.Longitude, place2.Latitude, place2.Longitude);
            if (res == -1) DisplayAlert("Error", "You can only have one order in a time", "Ok");
            else if (res == -2) return;
            else
            {
                RP.Elapsed -= CheckOrderStatus;
                RP.Elapsed += CheckOrderStatus;
            }
        }

        private void PlaceChanged(object sender, EventArgs e)
        {
            DrawPolyline(polylineOrder, new SimpleWaypoint(PlaceFrom.Text), new SimpleWaypoint(PlaceTo.Text));
        }

        public async void CheckOrderStatus(object sender, ElapsedEventArgs e)
        {
            RP?.Stop();
            var t = await Server.GetOrder();
            if (t == null)
            {
                RP?.Start();
                return;
            }
            if (t.Status != 1)
            {
                if (map.Pins.Where(p => p.Label == "Taxi").Count() == 0)
                    map.Pins.Add(new Pin()
                    {
                        Label = "Taxi",
                        Position = new Position(t.latitudeDriver.Value, t.longitudeDriver.Value),
                        Type = PinType.SavedPin
                    });
                else
                    map.Pins.Where(p => p.Label == "Taxi").FirstOrDefault().Position = new Position(t.latitudeDriver.Value, t.longitudeDriver.Value);
            }
            if (t.Status == 2)
                Dispatcher.BeginInvokeOnMainThread(async () => await DrawPolyline(polylineDriver, new SimpleWaypoint(t.latitudeDriver.Value, t.longitudeDriver.Value), new SimpleWaypoint(t.latitudeFrom, t.longitudeFrom)));
            if (map.Pins.Where(p => p.Label == "Location").Count() == 0)
            {
                map.Pins.Add(new Pin()
                {
                    Label = "Location",
                    Position = new Position(t.latitudeFrom, t.longitudeFrom),
                    Type = PinType.Place
                });
                map.Pins.Add(new Pin()
                {
                    Label = "Destination",
                    Position = new Position(t.latitudeTo, t.longitudeTo),
                    Type = PinType.Place
                });
            }
            if (polylineOrder.Geopath.Count==0) Dispatcher.BeginInvokeOnMainThread (async()=> await DrawPolyline(polylineOrder, new SimpleWaypoint(t.latitudeFrom, t.longitudeFrom), new SimpleWaypoint(t.latitudeTo, t.longitudeTo)));
            RP?.Start();
        }

        
    }


}

