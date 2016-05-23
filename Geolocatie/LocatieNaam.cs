using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Geolocatie
{
    class LocatieNaam : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string _adres;

        public string Adres
        {
            get { return _adres; }
            set
            {
                _adres = value;
                NotifyPropertyChanged("Adres");
            }
        }


        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
