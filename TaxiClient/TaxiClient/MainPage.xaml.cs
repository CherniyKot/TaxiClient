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
        OrderStatus order;
        public MainPage()
        {
            InitializeComponent();
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;

            CheckOrderStatus(null, null);

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

        private async void PlaceChanged(object sender, EventArgs e)
        {
            Geocoder geo = new Geocoder();
            var place1 = (await geo.GetPositionsForAddressAsync(PlaceFrom.Text)).FirstOrDefault();
            var place2 = (await geo.GetPositionsForAddressAsync(PlaceTo.Text)).FirstOrDefault();
            DrawPolyline(polylineOrder, new SimpleWaypoint(place1.Latitude, place1.Longitude), new SimpleWaypoint(place2.Latitude, place2.Longitude));
        }

        public async void CheckOrderStatus(object sender, ElapsedEventArgs e)
        {
            RP?.Stop();
            order = await Server.GetOrder();
            if (order == null || order.Status == 0)
            {
                Dispatcher.BeginInvokeOnMainThread(() =>
                {
                    CostLayout.IsVisible = false;
                    OrderLayout.IsVisible = true;
                    PlaceFrom.IsVisible = true;
                    PlaceTo.IsVisible = true;
                });
                if (polylineDriver.Geopath.Any())
                    Dispatcher.BeginInvokeOnMainThread(() =>
                    {
                        polylineDriver.Geopath.Clear();
                        polylineOrder.Geopath.Clear();
                    });
                map.Pins.Clear();
                RP?.Start();
                return;
            }
            Dispatcher.BeginInvokeOnMainThread(() =>
                {
                    CostLayout.IsVisible = true;
                    OrderLayout.IsVisible = false;
                    PlaceFrom.IsVisible = false;
                    PlaceTo.IsVisible = false;
                    Cost.Text = order.Cost.ToString();
                });
            if (order.Status != 1)
            {
                if (map.Pins.Where(p => p.Label == "Taxi").Count() == 0)
                    map.Pins.Add(new Pin()
                    {
                        Label = "Taxi",
                        Position = new Position(order.latitudeDriver.Value, order.longitudeDriver.Value),
                        Type = PinType.SavedPin
                    });
                else
                    map.Pins.Where(p => p.Label == "Taxi").FirstOrDefault().Position = new Position(order.latitudeDriver.Value, order.longitudeDriver.Value);
            }


            if (order.Status == 2)
                Dispatcher.BeginInvokeOnMainThread(async () =>
                {
                    await DrawPolyline(polylineDriver, new SimpleWaypoint(order.latitudeDriver.Value, order.longitudeDriver.Value), new SimpleWaypoint(order.latitudeFrom, order.longitudeFrom));
                    polylineOrder.Geopath.Clear();
                });
            if (map.Pins.Where(p => p.Label == "Location").Count() == 0)
            {
                map.Pins.Add(new Pin()
                {
                    Label = "Location",
                    Position = new Position(order.latitudeFrom, order.longitudeFrom),
                    Type = PinType.Place
                });
                map.Pins.Add(new Pin()
                {
                    Label = "Destination",
                    Position = new Position(order.latitudeTo, order.longitudeTo),
                    Type = PinType.Place
                });
            }
            if (polylineOrder.Geopath.Count==0) Dispatcher.BeginInvokeOnMainThread (async()=> await DrawPolyline(polylineOrder, new SimpleWaypoint(order.latitudeFrom, order.longitudeFrom), new SimpleWaypoint(order.latitudeTo, order.longitudeTo)));
            RP?.Start();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
#warning TODO
            int id = 2;
            Server.CancelOrder(id);
        }
    }


}

