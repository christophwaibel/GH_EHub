using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Carriers
{
    public class Oil:Carrier
    {
        public readonly double Price;               // money/kWh
        public readonly double EmissionsFactor;     // kgCO2/kWh
        public readonly double AvailableYearlyOil;  // kWh
        public Oil(double price, double emissionsFactor, double availableYearlyOil) : base(Carriers.Oil) 
        {
            Price = price;
            EmissionsFactor = emissionsFactor;
            AvailableYearlyOil = availableYearlyOil;
        }
    }
}
