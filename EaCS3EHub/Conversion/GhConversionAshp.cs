using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using EaCS3EHub.Carriers;
using EaCS3EHub.Properties;

namespace EaCS3EHub.Conversion
{
    public class GhConversionAshp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhConversionAshp class.
        /// </summary>
        public GhConversionAshp()
          : base("Air Source Heat Pump", "ASHP",
                "Air Source Heat Pump conversion technology",
                "EnergyHubs", "Energy Hubs")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // 0, 1, 2, 3, 4, 5
            pManager.AddNumberParameter("Fix Cost", "FixCost", "Fix Cost that occurs whenever this technology is installed, no matter its capacity, in any Money Unit (e.g. CHF)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Linear Cost", "LinCost", "Linear Cost in Money-unit (e.g. CHF) per kW capacity installed", GH_ParamAccess.item);
            pManager.AddNumberParameter("Embodied Emissions", "CO2", "Embodied emissions (kgCO2eq) per kW capacity installed", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum Capacity", "MinCap", "Minimum Capacity of technology in kW. I.e., this technology will be installed with at least this capacity, or not at all", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Lifetime", "Lifetime", "Lifetime of technology, in years", GH_ParamAccess.item);
            pManager.AddNumberParameter("OM Cost", "OMCost", "Operation & Maintenance Cost, money/kWh", GH_ParamAccess.item);

            // 6, 7
            pManager.AddGenericParameter("Ambient Air", "Air", "Ambient air energy carrier", GH_ParamAccess.item);
            pManager.AddGenericParameter("Electricity", "Electricity", "Electricity energy carrier", GH_ParamAccess.item);

            // 8
            pManager.AddNumberParameter("Supply Temperature", "SupTemp", "Supply temperature in °C. Default is 65 °C", GH_ParamAccess.item, 65.0);
            pManager[8].Optional = true;

            // 9, 10, 11, 12
            pManager.AddNumberParameter("pi1", "pi1", "Technology parameter, Pi1, See Ashouri et al 2013, Optimal design and operation of building services using mixed-integer linear programming techniques, Eqt. 8", GH_ParamAccess.item);
            pManager.AddNumberParameter("pi2", "pi2", "Technology parameter, Pi2, See Ashouri et al 2013, Optimal design and operation of building services using mixed-integer linear programming techniques, Eqt. 8", GH_ParamAccess.item);
            pManager.AddNumberParameter("pi3", "pi3", "Technology parameter, Pi3, See Ashouri et al 2013, Optimal design and operation of building services using mixed-integer linear programming techniques, Eqt. 8", GH_ParamAccess.item);
            pManager.AddNumberParameter("pi4", "pi4", "Technology parameter, Pi4, See Ashouri et al 2013, Optimal design and operation of building services using mixed-integer linear programming techniques, Eqt. 8", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Air Source Heat Pump", "ASHP", "Air Source Heat Pump as input for the Energy Hub solver", GH_ParamAccess.item);
            pManager.AddNumberParameter("COP", "COP", "COP of the ASHP based on Ashouri et al 2013, 'Optimal design and operation of building services using mixed-integer linear programming techniques'.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double fixCost = 0.0;
            double linCost = 0.0;
            double embodiedEm = 0.0;
            double minCap = 0.0;
            int lifetime = 0;
            double omCost = 0.0;

            if (!DA.GetData(0, ref fixCost)) return;
            if (!DA.GetData(1, ref linCost)) return;
            if (!DA.GetData(2, ref embodiedEm)) return;
            if (!DA.GetData(3, ref minCap)) return;
            if (!DA.GetData(4, ref lifetime)) return;
            if (!DA.GetData(5, ref omCost)) return;

            Air airIn = null;
            if (!DA.GetData(6, ref airIn)) return;

            Electricity elecIn = null;
            if (!DA.GetData(7, ref elecIn)) return;

            double supTemp = 0.0;
            DA.GetData(8, ref supTemp);

            double pi1 = 0.0;
            double pi2 = 0.0;
            double pi3 = 0.0;
            double pi4 = 0.0;
            if (!DA.GetData(9, ref pi1)) return;
            if (!DA.GetData(10, ref pi2)) return;
            if (!DA.GetData(11, ref pi3)) return;
            if (!DA.GetData(12, ref pi4)) return;

            AirSourceHeatPump ashp = new AirSourceHeatPump(fixCost, linCost, embodiedEm, minCap, lifetime, omCost, airIn, elecIn, pi1, pi2, pi3, pi4, supTemp);
            DA.SetData(0, ashp);
            DA.SetDataList(1, new List<double>(ashp.COP));

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.energysystems_heatpump_airsource;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("30cfcd83-3ef8-4c60-b6a1-620520f3b3b4"); }
        }
    }
}