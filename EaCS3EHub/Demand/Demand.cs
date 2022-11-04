using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Demand
{
    public abstract class Demand
    {
        /// <summary>
        /// Demand type
        /// </summary>
        public enum Demands
        {
            Heating,
            Cooling,
            Electricity,
            HotWater,
            Food
        }

        public Demands DemandType;

        public double[] AnnualHourlyDemand;

        protected Demand(double [] annualHourlyTimeSeries, Demands demandType)
        {
            if (annualHourlyTimeSeries.Length == EhubMisc.Misc.HoursPerYear)
            {
                AnnualHourlyDemand = new double[EhubMisc.Misc.HoursPerYear];
                annualHourlyTimeSeries.CopyTo(AnnualHourlyDemand, 0);
            }

            DemandType = demandType;
        }

    }
}
