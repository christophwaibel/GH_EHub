using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EaCS3EHub.Carriers
{
    public class GhCarrierAir : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhCarrierAir class.
        /// </summary>
        public GhCarrierAir()
          : base("Air Carrier", "Air",
              "Air Energy Carrier",
              "EnergyHubs", "Energy Hubs")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Ambient Air Temperature", "airTemp", "Ambient Air Temperature annual hourly time series, in °C", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Air Carrier", "Air", "Air energy carrier", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var airTemp = new List<double>();
            if (!DA.GetDataList(0, airTemp)) return;

            Air air = new Air(airTemp.ToArray());
            DA.SetData(0, air);
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
            get { return new Guid("51f3c187-4619-488c-b51c-226a43338048"); }
        }
    }
}