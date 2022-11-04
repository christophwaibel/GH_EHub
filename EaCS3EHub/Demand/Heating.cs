using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Demand
{
    public class Heating : Demand
    {
        public Heating(double [] annualHourlyHeatingDemand) : base(annualHourlyHeatingDemand, Demands.Heating) { }
    }
}
