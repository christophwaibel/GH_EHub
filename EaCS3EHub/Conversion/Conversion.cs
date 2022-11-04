using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EaCS3EHub.Conversion
{
    public abstract class Conversion
    {
        public double FixCost;
        public double LinearCost;
        public double EmbodiedEmissions;
        public double MinCapacity;
        public int Lifetime;
        public double OperationMaintenanceCost;

        protected Conversion(double fixCost, double linCost, double embodiedEmissions, double minCap, int lifetime, double omCost) 
        {
            FixCost = fixCost;
            LinearCost = linCost;
            EmbodiedEmissions = embodiedEmissions;
            MinCapacity = minCap;
            Lifetime = lifetime;
            OperationMaintenanceCost = omCost;
        }
    }
}
