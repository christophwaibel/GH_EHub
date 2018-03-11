using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace UrbanFormEHub
{
    public class GHEHub1 : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public GHEHub1()
            : base("EhubForm", "EhubForm",
                "Multi hub for Thesis",
                "EnergyHubs", "ThesisHub")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("path", "path", "path", GH_ParamAccess.item);
            pManager.AddBooleanParameter("mincarb", "mincarb", "minimize carbon only, no matter the cost?", GH_ParamAccess.item);
            pManager.AddNumberParameter("carbcon", "carbcon", "carbon constraint in kgCO2 absolute", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = "";
            if (!DA.GetData(0, ref path)) { };

            bool carbmin = false;
            if (!DA.GetData(1, ref carbmin)) { };

            double? carbcon = null;
            try
            {
                DA.GetData(2, ref carbcon);
            }
            catch { }

            if (!carbcon.IsNullOrDefault())
            {
                Ehub ehub = new Ehub(path, carbmin, carbcon);
            }
            else
            {
                Ehub ehub = new Ehub(path, carbmin);
            }

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("30593d00-398a-4c61-9844-7f5ffececb85"); }
        }
    }
}
