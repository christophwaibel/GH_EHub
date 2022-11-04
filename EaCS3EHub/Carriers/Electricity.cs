using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Carriers
{
    public class Electricity: Carrier
    {
        public readonly double Price;               // money/kWh
        public readonly double EmissionsFactor;     // kgCO2/kWh
        public Electricity(double price, double emissionsFactor) : base(Carriers.Electricity) 
        {
            Price = price;
            EmissionsFactor = emissionsFactor;
        }
    }
}
