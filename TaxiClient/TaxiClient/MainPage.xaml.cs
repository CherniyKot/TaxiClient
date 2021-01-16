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

namespace TaxiClient
{
    public partial class MainPage : ContentPage
    {
        Timer RP;
        int OrderID;
        int DriverID;
        DriverLocation[] drivers;
        OrderLocation[] orders;
        DriverLocation driver;
        OrderLocation order;
        double UserPathLength;
        double UserPathPrice;
        public MainPage()
        {
            InitializeComponent();

            RefreshPositions(null, null);
            RP = new Timer(1000);
            RP.Elapsed += RefreshPositions;
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

            Server.SendOrder(2, place1.Latitude, place1.Longitude, place2.Latitude, place2.Longitude);
        }

        private void PlaceChanged(object sender, EventArgs e)
        {
            DrawPolyline(polylineOrder, new SimpleWaypoint(PlaceFrom.Text), new SimpleWaypoint(PlaceTo.Text));
        }


        public void RefreshPositions(object sender, ElapsedEventArgs e)
        {
            RP?.Stop();
            drivers = Server.GetDriversLocations();
            Geocoder geo = new Geocoder();
            foreach (var driver in drivers)
            {
                if (map.Pins.Any(d => d != null && d.Type == PinType.SavedPin && d.Label == driver.driverID.ToString()))
                {
                    Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Where(d => d.Type == PinType.SavedPin && d.Label == driver.driverID.ToString()).First().Position = new Position(driver.latitude ?? 0, driver.longitude ?? 0));
                }
                else
                {
                    Dispatcher.BeginInvokeOnMainThread(() => {
                        var pin = new Pin()
                        {
                            Position = new Position(driver.latitude ?? 0, driver.longitude ?? 0),
                            Label = driver.driverID.ToString(),
                            Type = PinType.SavedPin
                        };
                        map.Pins.Add(pin);
                    });
                }
            }
            foreach (var pin in map.Pins.Where(p => p != null && p.Type == PinType.SavedPin))
            {
                if (!drivers.Select(d => d.driverID.ToString()).Contains(pin.Label)) Dispatcher.BeginInvokeOnMainThread(() => map.Pins.Remove(pin));
            }
            RP?.Start();
        }
    }


}

