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
                "EnergyHubs", "Solver")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // 5, 6
            pManager.AddNumberParameter("Solar Tech Areas", "srfs",
                "Surface areas for solar energy system technologies (PV, ST, PVT, ...) (m^2)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Solar Potentials", "sol",
                "Annual hourly solar potentials for each of the available surfaces (Wh)", GH_ParamAccess.tree);


            // 7, 8
            pManager.AddNumberParameter("GHI", "GHI",
                "Global Horizontal Irradiation annual hourly timeseries from an .epw", GH_ParamAccess.list);
            pManager.AddNumberParameter("dryBulb", "dryBulb",
                "Dry bulb temperature annual hourly timeseries from an .epw", GH_ParamAccess.list);


            // 11 - 13
            pManager.AddGenericParameter("Building Package", "Building", "Building Package, containing loads, and construction cost and emissions. " +
                "If multiple building packages are provided, the solver will treat them as decision variable. Useful, e.g. to determine optimal retrofitting package.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Conversion Tech", "ConvTech", "Conversion Technologies considered in the ehub", GH_ParamAccess.list);
            pManager.AddGenericParameter("Storage Tech", "StorageTech", "Storage Technologies considered in the ehub", GH_ParamAccess.list);



            // 9
            // 10
            pManager.AddIntegerParameter("epsilon", "epsilon", "number of epsilon cuts. Min 1, default 3", GH_ParamAccess.item, 3);
            pManager[10].Optional = true;
            pManager.AddBooleanParameter("run", "run", "run ehub", GH_ParamAccess.item);

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
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.IO_Energysytems;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("38fdaff2-2d7a-4149-beec-e75fccdf5a18"); }
        }
    }
}