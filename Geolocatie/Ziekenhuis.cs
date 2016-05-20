using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace Geolocatie
{
    public class Ziekenhuis
    {
        public string Naam { get; set; }
        public BasicGeoposition Positie { get; set; }
    }
}
