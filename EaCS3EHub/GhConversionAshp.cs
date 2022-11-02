using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EaCS3EHub
{
    public class GhConversionAshp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhConversionAshp class.
        /// </summary>
        public GhConversionAshp()
          : base("Air Source Heat Pump", "ASHP",
                "Air Source Heat Pump conversion technology",
                "EnergyHubs", "Conversion")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Supply Temperature", "SupTemp", "Supply temperature in °C", GH_ParamAccess.item);
            pManager.AddNumberParameter("Fix Cost", "FixCost", "Fix Cost that occurs whenever this technology is installed, no matter its capacity, in any Money Unit (e.g. CHF)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Linear Cost", "LinCost", "Linear Cost in Money-unit (e.g. CHF) per kW capacity installed", GH_ParamAccess.item);
            pManager.AddNumberParameter("Embodied Emissions", "CO2", "Embodied emissions (kgCO2eq) per kW capacity installed", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum Capacity", "MinCap", "Minimum Capacity of technology in kW. I.e., this technology will be installed with at least this capacity, or not at all", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Lifetime", "Lifetime", "Lifetime of technology, in years", GH_ParamAccess.item);

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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("30cfcd83-3ef8-4c60-b6a1-620520f3b3b4"); }
        }
    }

    public class Conversion
    {
        internal static double minCap;
        public Conversion(double minCap)
        {

        }

    }


    public class ASHP : Conversion
    {
        

        public ASHP():base(minCap)
        {

        }
    }
}