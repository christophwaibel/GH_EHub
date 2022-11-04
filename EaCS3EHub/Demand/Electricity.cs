using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Demand
{
    public class Electricity:Demand
    {
        public Electricity(double[] annualHourlyElectricity) : base(annualHourlyElectricity, Demands.Electricity) { }
    }
}
