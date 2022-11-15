using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using EaCS3EHub.Properties;

namespace EaCS3EHub.Demand
{
    public class GhDemand : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhHeating class.
        /// </summary>
        public GhDemand()
          : base("Demand", "demand",
              "Building energy demand 8760 time series",
              "EnergyHubs", "Energy Hubs")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("TimeSeries", "timeSeries", "8760 time series of heating loads", GH_ParamAccess.list);
            pManager.AddTextParameter("Demand Type", "type", "Demand type. Possible intputs: 'Heating', 'Cooling', 'Electricity', 'HotWater'. Default is 'Heating'", GH_ParamAccess.item, "Heating");
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Demand", "demand", "Demand", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var timeSeries = new List<double>();
            if (!DA.GetDataList(0, timeSeries)) return;
            string demandType = null;
            DA.GetData(1, ref demandType);

            Demand demandOut = null;
            switch (demandType)
            {
                case "Heating":
                    demandOut = new Heating(timeSeries.ToArray());
                    break;
                case "Cooling":
                    demandOut = new Cooling(timeSeries.ToArray());
                    break;
                case "Electricity":
                    demandOut = new Electricity(timeSeries.ToArray());
                    break;
                case "HotWater":
                    demandOut = new HotWater(timeSeries.ToArray());
                    break;
            }


            DA.SetData(0, demandOut);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.demand_energydemand;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("e68a7a39-e67f-4f94-ac17-cb16d3f82f49"); }
        }
    }
}