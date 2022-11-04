using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Carriers
{
    public class Solar:Carrier
    {
        /// <summary>
        /// In W/m2
        /// </summary>
        public readonly double[] AnnualHourlyIrradiation;
        public Solar(double [] annualHourlyIrradiation) : base(Carriers.Solar)
        {
            AnnualHourlyIrradiation = new double[annualHourlyIrradiation.Length];
            annualHourlyIrradiation.CopyTo(AnnualHourlyIrradiation, 0);
        }
    }
}
