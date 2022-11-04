using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EaCS3EHub.Carriers
{
    public class GhCarrierElectricity : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhCarrierElectricity class.
        /// </summary>
        public GhCarrierElectricity()
          : base("Electricity Carrier", "Electricity",
              "Electricity energy carrier",
              "EnergyHubs", "Energy Hubs")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Price", "Price", "Electricity price in money/kWh", GH_ParamAccess.item);
            pManager.AddNumberParameter("Emissions", "Emissions", "Emissions factor of electricity in kgCO2/kWh", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Electricity Carrier", "Electricity", "Electricity energy carrier", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double price = 0.0;
            if (!DA.GetData(0, ref price)) return;

            double emissions = 0.0;
            if (!DA.GetData(1, ref emissions)) return;

            DA.SetData(0, new Electricity(price, emissions));
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
            get { return new Guid("c46e9ecc-bdb7-4b0a-9bff-13c2afdb7467"); }
        }
    }
}