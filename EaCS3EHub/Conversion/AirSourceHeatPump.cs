using EaCS3EHub.Carriers;
using EhubMisc;

namespace EaCS3EHub.Conversion
{
    public class AirSourceHeatPump:Conversion
    {

        public Air AirInput;
        public Electricity ElectricityInput;
        
        public double Pi1;
        public double Pi2;
        public double Pi3;
        public double Pi4;

        public double SupplyTemp;

        public double[] COP;

        public AirSourceHeatPump(double fixCost, double linCost, double embodiedEmissions, double minCap, int lifetime, double omCost,
            Air airInput, Electricity electricityInput, double pi1, double pi2, double pi3, double pi4, double supplyTemp)
            :base(fixCost, lifetime, embodiedEmissions, minCap, lifetime, omCost)
        {
            AirInput = airInput;
            ElectricityInput = electricityInput;
            Pi1 = pi1;
            Pi2 = pi2;
            Pi3 = pi3;
            Pi4 = pi4;
            SupplyTemp = supplyTemp;

            COP = TechnologyEfficiencies.CalculateCOPHeatPump(airInput.AnnualHourlyAmbientTemperature, SupplyTemp, Pi1, Pi2, Pi3, Pi4);
        }
    }
}
