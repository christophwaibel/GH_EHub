using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Carriers
{
    public class Biomass:Carrier
    {
        public readonly double AvailableYearlyBiomass;  // kWh
        public readonly double Price;                   // money/kWh
        public readonly double EmissionsFactor;         // kgCO2/kWh
        public Biomass(double price, double emissionsFactor, double availableYearlyBiomass) : base(Carriers.Biomass) 
        {
            Price = price;
            EmissionsFactor = emissionsFactor;
            AvailableYearlyBiomass = availableYearlyBiomass;
        }
    }
}
