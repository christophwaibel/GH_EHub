using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Demand
{
    public class Cooling : Demand
    {
        public Cooling(double[] annualHourlyCooling) : base(annualHourlyCooling, Demands.Cooling) { }
    }
}
