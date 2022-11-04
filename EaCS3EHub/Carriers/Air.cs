using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Carriers
{
    public class Air:Carrier
    {
        /// <summary>
        /// 8760 ambient air temperature in °C
        /// </summary>
        public double[] AnnualHourlyAmbientTemperature;
        public Air(double [] annualHourlyAirTemp) : base(Carriers.Air) 
        {
            AnnualHourlyAmbientTemperature = new double[annualHourlyAirTemp.Length];
            annualHourlyAirTemp.CopyTo(AnnualHourlyAmbientTemperature, 0);
        }
    }
}
