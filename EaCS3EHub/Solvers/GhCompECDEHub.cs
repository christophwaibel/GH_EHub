using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using EaCS3EHub.Properties;

namespace EaCS3EHub
{
    public class GhCompECDEHub : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhCompECDEHub class.
        /// </summary>
        public GhCompECDEHub()
          : base("EnergyHub CompECD HS2022", "EHub2022",
                "Energy Hub component for the CompECD HS 2022 course at ETHZ",
                "EnergyHubs", "Energy Hubs")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.senary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Building", "building", "Building inputs to the energy hub. Could be retrofitting packages. " +
                "If more than one input is provided here, it will treat the different building inputs as decision variable (i.e., determining which building package is optimal", GH_ParamAccess.list);
            pManager.AddGenericParameter("Conversion", "conversion", "Conversion technologies, such as PV, ASHP, CHP, ...", GH_ParamAccess.list);
            pManager.AddGenericParameter("Storage", "storage", "Storage technologies, such as batteries, thermal energy storage, cold storage, ...", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Epsilon", "ε", "Number of epsilon-cuts, i.e. number of solutions between cost and carbon minimal. E.g., ε=3 will give a total of 5 solutions. Min 1, default 3.", GH_ParamAccess.item, 3);
            pManager[3].Optional = true;
            pManager.AddTextParameter("Folder", "folder", "Output folder path for writing results to", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "run", "Run the energy hub solver.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // 0, 1
            pManager.AddNumberParameter("CAPEX", "capex",
                "Total annual installation cost of the energy system (based on NPV and lifetimes of technologies) (CHF/year).",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("OPEX", "opex",
                "Total annual operation cost of the energy system (CHF/year).", GH_ParamAccess.list);

            // 2
            pManager.AddNumberParameter("Emissions", "emissions",
                "Total annualized operational and embodied carbon emissions of the energy hub (CO2eq./year).",
                GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.IO_Energysytems;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("38fdaff2-2d7a-4149-beec-e75fccdf5a18"); }
        }
    }
}