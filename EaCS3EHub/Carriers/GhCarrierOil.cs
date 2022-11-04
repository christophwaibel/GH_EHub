using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EaCS3EHub.Carriers
{
    public class GhCarrierOil : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhCarrierOil class.
        /// </summary>
        public GhCarrierOil()
          : base("Oil Carrier", "Oil",
              "Oil energy carrier",
              "EnergyHubs", "Energy Hubs")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Price", "Price", "Oil price in money/kWh", GH_ParamAccess.item);
            pManager.AddNumberParameter("Emissions", "Emissions", "Emissions factor of oil in kgCO2/kWh", GH_ParamAccess.item);
            pManager.AddNumberParameter("Yearly Oil", "YearlyOil", "Yearly oil budget, which cannot be exceeded. If no value provided, Double.MaxValue used.", GH_ParamAccess.item, System.Double.MaxValue);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Oil Carrier", "Oil", "Oil energy carrier", GH_ParamAccess.item);
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

            double oilBudget = 0.0;
            DA.GetData(2, ref oilBudget);

            Oil oil = new Oil(price, emissions, oilBudget);
            DA.SetData(0, oil);
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
            get { return new Guid("771f1719-a016-4ed8-9dce-7c55d164bcb7"); }
        }
    }
}