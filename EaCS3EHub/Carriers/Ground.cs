using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Carriers
{
    public class Ground:Carrier
    {
        public readonly double[] AnnualHourlyGroundTemperature;
        public Ground(double [] annualHourlyGroundTemp) : base(Carriers.Ground)
        {
            AnnualHourlyGroundTemperature = new double[annualHourlyGroundTemp.Length];
            annualHourlyGroundTemp.CopyTo(AnnualHourlyGroundTemperature, 0);
        }
    }
}
