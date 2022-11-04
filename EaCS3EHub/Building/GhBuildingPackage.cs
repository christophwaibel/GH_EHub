using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using EaCS3EHub.Demand;
using EaCS3EHub.Building;

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
                "EnergyHubs", "Energy Hubs")
        {
        }
        
        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Name of this building package, e.g. 'Retrofit version 1'", GH_ParamAccess.item);
            pManager.AddGenericParameter("Loads", "loads", "Building energy loads, such as cooling, heating, electricity, hot water", GH_ParamAccess.list);
            pManager.AddNumberParameter("Construction Cost", "Cost", "Construction cost (in money unit, e.g. CHF), e.g. for the whole building, or just for a retrofitting package", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Embodied Emissions", "CO2", "Construction related embodied emissions (in kgCO2eq). Could be for the whole construction, or just for a retrofitting package", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("Lifetime", "lifetime", "Lifetime of the construction, in years", GH_ParamAccess.item, 50);
            for (int i = 2; i < 5; i++)
                pManager[i].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Building Package", "building", "Building Package as input for the Energy Hub Sovler", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = null;
            if(!DA.GetData(0, ref name)) return;

            var inputDemands = new List<GH_ObjectWrapper>();
            if (!DA.GetDataList(1, inputDemands)) return;

            double capex = 0.0;
            DA.GetData(2, ref capex);

            double emissions = 0.0;
            DA.GetData(3, ref emissions);

            int lifeTime = 50;
            DA.GetData(4, ref lifeTime);

            Heating heating = null;
            Cooling cooling = null;
            Electricity electricity = null;
            HotWater hotWater = null;

            foreach(GH_ObjectWrapper demand in inputDemands)
            {
                switch (demand.Value)
                {
                    case Heating valueDemand:
                        heating = valueDemand;
                        break;
                    case Cooling valueDemand:
                        cooling = valueDemand;
                        break;
                    case Electricity valueDemand:
                        electricity = valueDemand;
                        break;
                    case HotWater valueDemand:
                        hotWater = valueDemand;
                        break;
                }
            }

            Building.Building building = new Building.Building (heating.AnnualHourlyDemand, cooling.AnnualHourlyDemand, hotWater.AnnualHourlyDemand, electricity.AnnualHourlyDemand,
                capex, emissions, lifeTime);
            DA.SetData(0, building);
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