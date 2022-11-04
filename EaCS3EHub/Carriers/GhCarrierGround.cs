using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EaCS3EHub.Carriers
{
    public class GhCarrierGround : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhCarrierGround class.
        /// </summary>
        public GhCarrierGround()
          : base("Ground Carrier", "Ground",
              "Ground energy carrier",
              "EnergyHubs", "Energy Hubs")
        {
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Ground Temperature", "GroundTemp", "Ground tempearture in °C, annual hourly time series", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Ground Carrier", "Ground", "Ground energy carrier", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var groundTemp = new List<double>();
            if (!DA.GetDataList(0, groundTemp)) return;

            Ground ground = new Ground(groundTemp.ToArray());
            DA.SetData(0, ground);
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
            get { return new Guid("be97e6b9-e579-492c-85c1-75566b974e61"); }
        }
    }
}