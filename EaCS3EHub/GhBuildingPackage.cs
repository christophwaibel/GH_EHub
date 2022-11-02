using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EaCS3EHub
{
    public class GhBuildingPackage : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhBuildingPackage class.
        /// </summary>
        public GhBuildingPackage()
          : base("Building Package", "Building",
                "Building Package for the Energy Hub, including loads, construction cost and -emimssions.",
                "EnergyHubs", "Building")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Name of this building package, e.g. 'Retrofit version 1'", GH_ParamAccess.item);

            pManager.AddNumberParameter("Construction Cost", "Cost", "Construction cost (in money unit, e.g. CHF), e.g. for the whole building, or just for a retrofitting package", GH_ParamAccess.item);
            pManager.AddNumberParameter("Embodied Emissions", "CO2", "Construction related embodied emissions (in kgCO2eq). Could be for the whole construction, or just for a retrofitting package", GH_ParamAccess.item);
            pManager.AddNumberParameter("Space Heating Loads", "Htg", "Annual hourly space heating loads in kWh", GH_ParamAccess.list);
            pManager.AddNumberParameter("Space Cooling Loads", "Clg", "Annual hourly space cooling loads in kWh", GH_ParamAccess.list);
            pManager.AddNumberParameter("Electricity Loads", "Elect", "Annual hourly electricity loads for lighting and equipment, in kWh", GH_ParamAccess.list);
            pManager.AddNumberParameter("Hot Water Demand", "Hw", "Annual hourly hot water demand in kWh", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Building Package", "Bldg", "Building Package as input for the Energy Hub Sovler", GH_ParamAccess.item);
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
            get { return new Guid("05de025c-45b9-4a35-9789-2e2cedd4b791"); }
        }
    }


    public class BuildingPackage
    {
        public BuildingPackage(double[] htg, double[] clg, double[] elec, double[] dhw, double cost, double emissions, string name)
        {

        }
    }

}