using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Building
{
    public class Building
    {

        public double[] HeatingDemand;
        public double[] CoolingDemand;
        public double[] HotWaterDemand;
        public double[] ElectricityDemand;

        public double Capex;        // in money
        public double Emissions;    // in kgCO2eq
        public int Lifetime;        // in years


        public Building(double[] heatingDemand, double[] coolingDemand, double [] hotWaterDemand, double [] electricityDemand,
            double capex, double emissions, int lifeTime)
        {
            HeatingDemand = new double[heatingDemand.Length];
            heatingDemand.CopyTo(HeatingDemand, 0);

            CoolingDemand = new double[coolingDemand.Length];
            coolingDemand.CopyTo(CoolingDemand, 0);

            HotWaterDemand = new double[hotWaterDemand.Length];
            hotWaterDemand.CopyTo(HotWaterDemand, 0);

            ElectricityDemand = new double[electricityDemand.Length];
            electricityDemand.CopyTo(ElectricityDemand, 0);

            Capex = capex;
            Emissions = emissions;
            Lifetime = lifeTime;
        }
    }
}
