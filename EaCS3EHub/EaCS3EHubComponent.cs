using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;



// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace EaCS3EHub
{
    public class EaCS3EHubComponent : GH_Component
    {
        public EaCS3EHubComponent()
          : base("EnergyHub EaCS3 HS2021", "EHub2021",
              "Energy Hub component for the EaCS3 HS 2021 course at ETHZ",
              "[hive]", "EnergyHubs")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // 0
            pManager.AddTextParameter("directory", "dir", "ehub.exe directory", GH_ParamAccess.item);

            // 1 - 4
            pManager.AddNumberParameter("Heating Loads", "htg", "Annual hourly heating loads (kW/h)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Cooling Loads", "clg", "Annual hourly cooling loads (kW/h)", GH_ParamAccess.list);
            pManager.AddNumberParameter("DHW Loads", "dhw", "Annual hourly domestic hot water loads (kW/h)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Electricity Loads", "elec", "Annual hourly electricity loads (kW/h)", GH_ParamAccess.list);

            // 5, 6
            pManager.AddNumberParameter("Solar Tech Areas", "srfs", "Surface areas for solar energy system technologies (PV, ST, PVT, ...) (m^2)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Solar Potentials", "sol", "Annual hourly solar potentials for each of the available surfaces (Wh)", GH_ParamAccess.tree);

            // technology parameters
            // carrier prices
            // carrier emissions

            // 7
            pManager.AddBooleanParameter("run", "run", "run ehub", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Cost Install", "costInstall", "Total annual installation cost of the energy system (based on NPV and lifetimes of technologies) (CHF/year).", GH_ParamAccess.item);
            pManager.AddNumberParameter("Cost Operation", "costOp", "Total annual operation cost of the energy system (CHF/year).", GH_ParamAccess.item);

            pManager.AddNumberParameter("Emissions Install", "EmInstall", "Total annualized embodied carbon emissions of the energy technologies (CO2eq./year).", GH_ParamAccess.item);
            pManager.AddNumberParameter("Emissions Operation", "EmOp", "Total annual operational carbon emissions of the energy system (CO2eq./year).", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // read inputs from GH component
            //  hourly time series: DHW (list)
            //  hourly time series: clg (list)
            //  hourly time series: htg (list)
            //  hourly time series: elec (list)
            //  surface areas for PV (1 list)
            //  hourly timeseries for each PV surface (GHTree)


            // call CISBAT.exe
            string ehub = null;
            DA.GetData(0, ref ehub);


            bool run = false;
            DA.GetData(7, ref run);

            if (run) 
            {
                //run ehub.exe
                string command = @"0 \n";
                RunEhub(ehub, command);
            }




            // return results to outputs IN GH component


        }

        private static void RunEhub(string FileName, string command)
        {
            string application = FileName;
            System.Diagnostics.Process P = new System.Diagnostics.Process();
            //P.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            P.StartInfo.FileName = application;
            P.StartInfo.Arguments = command;
            P.Start();
            P.WaitForExit();
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("d3733eec-10ec-4f5b-936b-57f3e1a724f3"); }
        }
    }
}
