using Geolocatie.Common;
using Geolocatie.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json;
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
using System.Net.Http;
using Windows.Storage.Streams;


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

        MapRouteFinderResult routeResult;

        public GeoPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            locatie();
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
        private async Task<Geoposition> getLocation()
        {
            Geolocator gl = new Geolocator
            {
                DesiredAccuracy = PositionAccuracy.High,
                MovementThreshold = 10

            };

            try
            {
                Geoposition gp = await gl.GetGeopositionAsync();
                
                return gp;
            }
            catch (Exception e)
            {
                message(e.Message, "ERROR!");
                return null;
            }            
        }

        public async void locatie()
        {
            Geoposition gp = await getLocation();

            MyMap.MapElements.Clear();
            MyMap.Center = gp.Coordinate.Point;
            AddPushpin(gp.Coordinate.Point.Position.Latitude, gp.Coordinate.Point.Position.Longitude, "eigen");
            slider.Value = 13;
            
            var result = await MapLocationFinder.FindLocationsAtAsync(MyMap.Center);

            eigenLocatie.Text = locatieTekst(result, MyMap);
        }

        private string locatieTekst(MapLocationFinderResult result, MapControl MyMap)//locatie in string formaat
        {
            if (result.Status == MapLocationFinderStatus.Success)
            {
                return "locatie : " + result.Locations[0].Address.Street + ", " + result.Locations[0].Address.StreetNumber + "\n "
                    + result.Locations[0].Address.PostCode + " " + result.Locations[0].Address.Town;
            }
            else
            {
                return "plaats niet gevonden!";
            }
        }

        private async void getZiekenhuizen()//route van huidige locatie naar dichtste ziekenhuis
        {
            MapService.ServiceToken = "qZO7GwUqKeWcjJiEOva1qA";

            Geoposition gp = await getLocation();
            BasicGeoposition gpBasis = new BasicGeoposition();
            gpBasis.Longitude = gp.Coordinate.Point.Position.Longitude;
            gpBasis.Latitude = gp.Coordinate.Point.Position.Latitude;
            Geopoint gpPoint = new Geopoint(gpBasis);

            string httpheader = "https://maps.googleapis.com/maps/api/place/nearbysearch/json?location=" + gpBasis.Latitude + "," + gpBasis.Longitude + "&radius=30000&type=hospital&key=AIzaSyBVbK0OS14hDYy10rOAhX94BEOuV02HBXQ";

            var client = new HttpClient();
            var result = await client.GetStringAsync(httpheader);
            GooglePlacesResponse gpr = (GooglePlacesResponse)JsonConvert.DeserializeObject<GooglePlacesResponse>(result);
            
            int count = gpr.results.Length;

            BasicGeoposition plaats = new BasicGeoposition();

            BasicGeoposition nearestHospital = new BasicGeoposition();
            Geopoint nearestHospitalPoint;

            double dichtste = 30000;

            if (gpr.status == "OK")
            {
                for (int j = 0; j < count; j++)
                {
                    string name = gpr.results[j].name;
                    plaats.Latitude = gpr.results[j].geometry.location.lat;
                    plaats.Longitude = gpr.results[j].geometry.location.lng;

                    double afstand = Distance(gpBasis.Latitude, gpBasis.Longitude, plaats.Latitude, plaats.Longitude, 1);

                    if (afstand < dichtste)
                    {
                        dichtste = afstand;

                        nearestHospital.Longitude = gpr.results[j].geometry.location.lng;
                        nearestHospital.Latitude = gpr.results[j].geometry.location.lat;
                    }
                }
                nearestHospitalPoint = new Geopoint(nearestHospital);
                AddPushpin(nearestHospital.Latitude, nearestHospital.Longitude, "zh");
                AddPushpin(gpBasis.Latitude, gpBasis.Longitude, "eigen");

                routeResult = await MapRouteFinder.GetDrivingRouteAsync(nearestHospitalPoint, gpPoint);

                if (routeResult.Status == MapRouteFinderStatus.Success)
                {
                    // Use the route to initialize a MapRouteView.
                    MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                    viewOfRoute.RouteColor = Colors.Blue;
                    viewOfRoute.OutlineColor = Colors.Blue;
                    // Add the new MapRouteView to the Routes collection
                    // of the MapControl.
                    MyMap.Routes.Clear();
                    MyMap.Routes.Add(viewOfRoute);
                    // Fit the MapControl to the route.
                    await MyMap.TrySetViewBoundsAsync(
                    routeResult.Route.BoundingBox,
                    null,
                    Windows.UI.Xaml.Controls.Maps.MapAnimationKind.Bow);

                }
                else
                {
                    message("Je route kon niet berekend worden", "FOUTMELDING");
                }

            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)//Routeknop
        {
            MyMap.Children.Clear();
            
            getZiekenhuizen();
        }

        private void slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (slider != null)
                MyMap.ZoomLevel = (int)slider.Value;
        }

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

        public void AddPushpin(double lat, double lon, string type)//marker objecten
        {
            BasicGeoposition location = new BasicGeoposition();
            location.Latitude = lat;
            location.Longitude = lon;

            var pin = new Ellipse()
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                Width = 20,
                Height = 20,
            };

            if (type == "zh")
            {
                pin.Fill = new SolidColorBrush(Colors.Blue);
                pin.Tapped += pin_Tapped_Zh;
            }
            if (type == "eigen")
            {
                pin.Fill = new SolidColorBrush(Colors.Red);
                pin.Tapped += pin_Tapped_Eigen;
            }
            MapControl.SetLocation(pin, new Geopoint(location));
            MyMap.Children.Add(pin);
        }
        
        private async void myMap_MapTapped(MapControl sender,MapInputEventArgs args)//adres verkrijgen van getapte locatie
        {
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(args.Location);

            eigenLocatie.Text = locatieTekst(result, MyMap);
        }
        //message tekst
        void pin_Tapped_Eigen(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
           message("Dit is uw huidige locatie.", "info");
        }
        void pin_Tapped_Zh(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            message("Dichtste ziekenhuis.", "info");
        }
        //afstand tussen 2 plaatsen berekenen
        public double Distance(double pos1Latitude, double pos1Longitude, double pos2Latitude, double pos2Longitude, int type)
        {
            double R = (type == 0) ? 3960 : 6371; //if type == 0 ? miles : kilometers;
            double dLat = this.toRadian(pos2Latitude - pos1Latitude);
            double dLon = this.toRadian(pos2Longitude - pos1Longitude);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(this.toRadian(pos1Latitude)) * Math.Cos(this.toRadian(pos2Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = R * c;
            return d;
        }
        private double toRadian(double val)
        {
            return (Math.PI / 180) * val;
        }
    }
}
