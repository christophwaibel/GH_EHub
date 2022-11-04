using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Carriers
{
    public abstract class Carrier
    {
        public enum Carriers
        {
            Air,
            Electricity,
            NaturalGas,
            Oil,
            Biomass,
            Solar,
            Ground
        }

        public Carriers CarrierType;

        protected Carrier(Carriers carrierType)
        {
            CarrierType = carrierType;
        }
    }
}
