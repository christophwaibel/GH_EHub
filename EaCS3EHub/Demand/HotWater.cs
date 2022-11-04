using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Demand
{
    public class HotWater : Demand
    {
        public HotWater(double[] annualHourlyHotWater) : base(annualHourlyHotWater, Demands.HotWater) { }
    }
}
