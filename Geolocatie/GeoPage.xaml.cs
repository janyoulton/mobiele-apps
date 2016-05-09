using Geolocatie.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.UI;
using Windows.UI.Xaml.Shapes;
using Windows.Services.Maps;
using Windows.UI.Xaml.Controls.Maps;
using System.Threading.Tasks;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Geolocatie
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GeoPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();


        public GeoPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            MapService.ServiceToken = "qZO7GwUqKeWcjJiEOva1qA​";
            slider.Value = 8;
            this.MyMap.Center = new Geopoint(new BasicGeoposition() { Latitude = 50.913498, Longitude = 5.344768 }); //bij starten centreren op Hasselt
            MyMap.MapTapped += myMap_MapTapped;
            
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
        //METHODES zelf geschreven
        private async Task<Geoposition> getLocation()
        {
            Geolocator gl = new Geolocator
            {
                DesiredAccuracy = PositionAccuracy.High
            };

            try
            {
                Geoposition gp = await gl.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromMinutes(1),
                    timeout: TimeSpan.FromSeconds(20));

                MyMap.Center = gp.Coordinate.Point;
                AddPushpin(gp.Coordinate.Point.Position.Latitude, gp.Coordinate.Point.Position.Longitude, Colors.Red, "eigen");
                MyMap.ZoomLevel = (int)slider.Value;


                var result = await MapLocationFinder.FindLocationsAtAsync(MyMap.Center);

                // Get the address
                if (result.Status == MapLocationFinderStatus.Success)
                {
                    string address = result.Locations[0].Address.StreetNumber + result.Locations[0].Address.Street;
                    eigenLocatie.Text = address;
                }
                else
                {
                    message("plaatsnaam niet gevonden!","error");
                }

                return gp;

            }
            catch (Exception e)
            {
                message(e.Message, "ERROR!");
                return null;
            }

            
        }

        private async void ziekenhuisLocaties()
        {
            BasicGeoposition jessazh = new BasicGeoposition();
            jessazh.Latitude = 50.9323;//51.1971; //50.913498
            jessazh.Longitude = 5.3024;//4.5854; // 5.344768
            Geopoint jessazhLocatie = new Geopoint(jessazh);
            AddPushpin(jessazh.Latitude, jessazh.Longitude, Colors.Blue, "zh");

            BasicGeoposition AZVesalius = new BasicGeoposition();
            AZVesalius.Latitude = 50.871752;
            AZVesalius.Longitude = 5.512873;
            Geopoint AZVLocatie = new Geopoint(AZVesalius);
            AddPushpin(AZVesalius.Latitude, AZVesalius.Longitude, Colors.Blue, "zh");



            Geoposition mijnLocatie = await getLocation();
            BasicGeoposition mijnLocatieBasic = new BasicGeoposition();
            mijnLocatieBasic.Longitude = mijnLocatie.Coordinate.Point.Position.Longitude;
            mijnLocatieBasic.Latitude = mijnLocatie.Coordinate.Point.Position.Latitude;
            Geopoint mijnLocatiePoint = new Geopoint(mijnLocatieBasic);
            
            MapRouteFinderResult routeResult =
                await MapRouteFinder.GetDrivingRouteAsync(
                    jessazhLocatie,
                    mijnLocatiePoint,
                    MapRouteOptimization.Time,
                    MapRouteRestrictions.None);

            try {
                 if (routeResult.Status == MapRouteFinderStatus.Success)
                {
                    // Use the route to initialize a MapRouteView.
                    MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                    viewOfRoute.RouteColor = Colors.Blue;
                    viewOfRoute.OutlineColor = Colors.Blue;
                    // Add the new MapRouteView to the Routes collection
                    // of the MapControl.
                    MyMap.Routes.Add(viewOfRoute);
                    // Fit the MapControl to the route.
                    await MyMap.TrySetViewBoundsAsync(
                    routeResult.Route.BoundingBox,
                    null,
                    Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);
                }
            }
            catch (Exception e)
            {
                message(e.Message, "ERROR!");
            }

            //DIT GEDEELTE OP APARTE PAGINA!!!

            //tbTurnByTurn.Inlines.Add(new Run()
            //{
            //    Text = "Total estimated time (minutes) = "
            //    + routeResult.Route.EstimatedDuration.TotalMinutes.ToString("F1")
            //});
            //tbTurnByTurn.Inlines.Add(new LineBreak());
            //tbTurnByTurn.Inlines.Add(new Run()
            //{
            //    Text = "Total length (kilometers) = "
            //    + (routeResult.Route.LengthInMeters / 1000).ToString("F1")
            //});
            //tbTurnByTurn.Inlines.Add(new LineBreak());
            //// Display the directions.
            //tbTurnByTurn.Inlines.Add(new Run()
            //{
            //    Text = "DIRECTIONS"
            //});
            //tbTurnByTurn.Inlines.Add(new LineBreak());
            //// Loop through the legs and maneuvers.
            //int legCount = 0;
            //foreach (MapRouteLeg leg in routeResult.Route.Legs)
            //{
            //    foreach (MapRouteManeuver maneuver in leg.Maneuvers)
            //    {
            //        tbTurnByTurn.Inlines.Add(new Run()
            //        {
            //            Text = maneuver.InstructionText
            //        });
            //        tbTurnByTurn.Inlines.Add(new LineBreak());
            //    }
            //}



            //Distance(jessazh, AZVesalius, DistanceType.Kilometers);
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)//Locatie
        {
            //alle vorige pinnen verwijderen
            MyMap.Children.Clear();

            //ziekenhuizen limburg
            ziekenhuisLocaties();
        }

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)//Beschrijving
        {

        }

        private void AppBarButton_Click_2(object sender, RoutedEventArgs e)//Route
        {

        }

        private void slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (slider != null)
                MyMap.ZoomLevel = (int)slider.Value;
        }



        //METHODES gevonden op internet
        private async void message(string body, string title)
        {
            var dlg = new MessageDialog(
                    string.Format(body), title);

            try
            {
                await dlg.ShowAsync();
            }
            catch (Exception) { }
        }

        public void AddPushpin(double lat, double lon, Color c, string type)
        {
            BasicGeoposition location = new BasicGeoposition();
            location.Latitude = lat;
            location.Longitude = lon;

            var pin = new Ellipse()
            {
                Fill = new SolidColorBrush(c),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                Width = 20,
                Height = 20,
            };

            if (type == "zh")
            {
                pin.Tapped += pin_Tapped_Zh;
            }
            if (type == "eigen")
            {
                pin.Tapped += pin_Tapped_Eigen;
            }
           
            Windows.UI.Xaml.Controls.Maps.MapControl.SetLocation(pin, new Geopoint(location));
            MyMap.Children.Add(pin);
        }

        private async void myMap_MapTapped(MapControl sender,MapInputEventArgs args)
        {
            //adres verkrijgen van getapte locatie
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(args.Location);
            if (result.Status == MapLocationFinderStatus.Success)
            {
                if (result.Locations.Count > 0)
                {
                    string address = result.Locations[0].Address.StreetNumber + " " + result.Locations[0].Address.Street;
                    eigenLocatie.Text = address;
                }
            }
        }

        void pin_Tapped_Eigen(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
           message("Dit is uw huidige locatie.", "info");
        }
        void pin_Tapped_Zh(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            message("Dit is een ziekenhuis.", "info");
        }

        //public double Distance(Position pos1, Position pos2, DistanceType type)
        //{
        //    double R = (type == DistanceType.Miles) ? 3960 : 6371;
        //    double dLat = this.toRadian(pos2.Latitude - pos1.Latitude);
        //    double dLon = this.toRadian(pos2.Longitude - pos1.Longitude);
        //    double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(this.toRadian(pos1.Latitude)) * Math.Cos(this.toRadian(pos2.Latitude)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        //    double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        //    double d = R * c;

        //    return d;
        //}
        //private double toRadian(double val)
        //{
        //    return (Math.PI / 180) * val;
        //}
        //public enum DistanceType { Miles, Kilometers };
        //public struct Position
        //{
        //    public double Latitude;
        //    public double Longitude;
        //}
    }
}
