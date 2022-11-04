using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Carriers
{
    public class NaturalGas : Carrier
    {
        public readonly double Price;               // money/kWh
        public readonly double EmissionsFactor;     // kgCO2/kWh
        public readonly double AvailableYearlyNaturalGas;   // kWh
        public NaturalGas(double price, double emissionsFactor, double availableYearlyNaturalGas) : base(Carriers.NaturalGas) 
        {
            Price = price;
            EmissionsFactor = emissionsFactor;
            AvailableYearlyNaturalGas = availableYearlyNaturalGas;
        }
    }
}
