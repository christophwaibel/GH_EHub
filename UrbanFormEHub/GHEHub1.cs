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
            // 0
            pManager.AddBooleanParameter("RUN", "RUN", "True to run MILP", GH_ParamAccess.item);
            // 1
            pManager.AddNumberParameter("carbcon", "carbcon", "carbon constraint in [kgCO2 / m2]", GH_ParamAccess.item);
            // 2
            pManager.AddNumberParameter("gfa", "gfa", "Gross Floor Area of development, in [m^2].", GH_ParamAccess.item);
            // 3
            pManager.AddNumberParameter("elec demand", "elec", "Electricity demand hourly time series, 8760, in [kWh].", GH_ParamAccess.list);
            // 4
            pManager.AddNumberParameter("cool demand", "cool", "Cooling demand hourly time series, 8760, in [kWh].", GH_ParamAccess.list);
            // 5
            pManager.AddNumberParameter("sh demand", "sh", "Space heating demand hourly time series, 8760, in [kWh].", GH_ParamAccess.list);
            // 6
            pManager.AddNumberParameter("dhw demand", "dhw", "Domestic hot water demand hourly time series, 8760, in [kWh].", GH_ParamAccess.list);
            // 7
            pManager.AddNumberParameter("feedin", "feedin", "Feed-in tariff hourly time series, 8760, in [Rappen/kWh].", GH_ParamAccess.list);
            // 8
            pManager.AddNumberParameter("grid", "grid", "Grid electricity price hourly time series, 8760, in [Rappen/kWh].", GH_ParamAccess.list);
            // 9
            pManager.AddNumberParameter("carbon", "carbon", "Grid carbon emissions factor hourly time series, 8760, in [gCO2/kWh].", GH_ParamAccess.list);
            // 10
            pManager.AddNumberParameter("temp", "temp", "Ambient dry bulb air temperature hourly time series, 8760, in [C°].", GH_ParamAccess.list);
            // 11
            pManager.AddNumberParameter("pvarea", "pvarea", "Facade patches that may be used for PV or solar thermal, in [m^2].", GH_ParamAccess.list);
            // 12
            pManager.AddNumberParameter("solar", "solar", "Solar potentials hourly time series for multiple sensor points, in [W/m^2].", GH_ParamAccess.tree);
            //13
            pManager.AddNumberParameter("maxbat", "maxbat", "Maximal battery capacity. E.g. 100kWh per storey and building.", GH_ParamAccess.item);
            //14
            pManager.AddNumberParameter("maxtes", "maxtes", "Maximal TES capacity. E.g. 1400 kWh per building.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // 0 - 1
            pManager.AddNumberParameter("cost", "cost", "Objective Value. Discounted annual cost for installation and operation of energy system, in [CHF].", GH_ParamAccess.item);
            pManager.AddNumberParameter("co2", "co2", "Annual carbon emissions for operation of energy system in [kgCO2].", GH_ParamAccess.item);

            // 2 - 4
            pManager.AddNumberParameter("capacities", "capacities", "Sized capacities of energy technologies in kW or kWh (storages). [0] AC; [1] hp_s; [2] hp_m; [3] hp_l; [4] chp_s; [5] chp_m; [6] chp_l; [7] boi_s; [8] boi_m; [9] boi_l; [10] bat; [11] tes", GH_ParamAccess.list);
            pManager.AddNumberParameter("pv m2", "pv m2", "installed pv per patch in m2.", GH_ParamAccess.list);
            pManager.AddNumberParameter("st m2", "st m2", "installed st per patch in m2.", GH_ParamAccess.list);

            // 5 -7
            pManager.AddNumberParameter("hp_s_op", "hp_s_op", "Heat pump small operation schedule in kW", GH_ParamAccess.list);
            pManager.AddNumberParameter("hp_m_op", "hp_m_op", "Heat pump medium operation schedule in kW", GH_ParamAccess.list);
            pManager.AddNumberParameter("hp_l_op", "hp_l_op", "Heat pump large operation schedule in kW", GH_ParamAccess.list);

            // 8 - 10
            pManager.AddNumberParameter("chp_s_op", "chp_s_op", "Combined Heat and Power small operation schedule in kW", GH_ParamAccess.list);
            pManager.AddNumberParameter("chp_m_op", "chp_m_op", "Combined Heat and Power medium operation schedule in kW", GH_ParamAccess.list);
            pManager.AddNumberParameter("chp_l_op", "chp_l_op", "Combined Heat and Power large operation schedule in kW", GH_ParamAccess.list);

            // 11 - 13
            pManager.AddNumberParameter("boi_s_op", "boi_s_op", "Boiler small operation schedule in kW", GH_ParamAccess.list);
            pManager.AddNumberParameter("boi_m_op", "boi_m_op", "Boiler medium operation schedule in kW", GH_ParamAccess.list);
            pManager.AddNumberParameter("boi_l_op", "boi_l_op", "Boiler large operation schedule in kW", GH_ParamAccess.list);

            // 14 - 16
            pManager.AddNumberParameter("ch_bat", "ch_bat", "Charging schedule of battery in kW.", GH_ParamAccess.list);
            pManager.AddNumberParameter("dis_bat", "dis_bat", "Discharging schedule of battery in kW.", GH_ParamAccess.list);
            pManager.AddNumberParameter("soc_bat", "soc_bat", "State of charge schedule of battery in kWh.", GH_ParamAccess.list);

            // 17 - 19
            pManager.AddNumberParameter("ch_tes", "ch_tes", "Charging schedule of Thermal Energy Storage in kW.", GH_ParamAccess.list);
            pManager.AddNumberParameter("dis_tes", "dis_tes", "Discharging schedule of Thermal Energy Storage in kW.", GH_ParamAccess.list);
            pManager.AddNumberParameter("soc_tes", "soc_tes", "State of charge schedule of Thermal Energy Storage in kWh.", GH_ParamAccess.list);

            // 20 - 22
            pManager.AddGenericParameter("heat_dump", "heat_dump", "Heat dumped in kWh", GH_ParamAccess.list);
            pManager.AddNumberParameter("ElecPur", "dblEpur", "schedule of electricity purchased", GH_ParamAccess.list);
            pManager.AddNumberParameter("ElecSell", "dblEsell", "schedule of electricity sold", GH_ParamAccess.list);

            // 23 - 26
            pManager.AddNumberParameter("red_elec", "red_elec", "Horizon-reduced electricity demand. 1008 hours, 6 weeks.", GH_ParamAccess.list);
            pManager.AddNumberParameter("red_sh", "red_sh", "Horizon-reduced space heating demand. 1008 hours, 6 weeks.", GH_ParamAccess.list);
            pManager.AddNumberParameter("red_dhw", "red_dhw", "Horizon-reduced domestic hot water demand. 1008 hours, 6 weeks.", GH_ParamAccess.list);
            pManager.AddNumberParameter("red_cool", "red_cool", "Horizon-reduced cooling demand. 1008 hours, 6 weeks.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool start = false;
            if (!DA.GetData(0, ref start)) { };

            double carbcon = double.MaxValue;
            if (!DA.GetData(1, ref carbcon)){};

            double gfa = double.MaxValue;
            if (!DA.GetData(2, ref gfa)) { };

            List<double> elecdem = new List<double>();
            if (!DA.GetDataList(3, elecdem)) { return; }

            List<double> cooldem = new List<double>();
            if (!DA.GetDataList(4, cooldem)) { return; }

            List<double> shdem = new List<double>();
            if (!DA.GetDataList(5, shdem)) { return; }

            List<double> dhwdem = new List<double>();
            if (!DA.GetDataList(6, dhwdem)) { return; }

            List<double> feedin = new List<double>();
            if (!DA.GetDataList(7, feedin)) { return; }

            List<double> grid = new List<double>();
            if (!DA.GetDataList(8, grid)) { return; }

            List<double> carbon = new List<double>();
            if (!DA.GetDataList(9, carbon)) { return; }

            List<double> temp = new List<double>();
            if (!DA.GetDataList(10, temp)) { return; }

            List<double> pvarea = new List<double>();
            if (!DA.GetDataList(11, pvarea)) { return; }


            Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Number> sol;
            if (!DA.GetDataTree(12, out sol)) { return; }
            List<List<double>> solarPotList = new List<List<double>>();

            for (int i = 0; i < sol.Branches.Count; i++)
            {
                solarPotList.Add(new List<double>());
                foreach (Grasshopper.Kernel.Types.GH_Number gooNumber in sol.get_Branch(i))
                {
                    double numb;
                    GH_Convert.ToDouble(gooNumber, out numb, GH_Conversion.Both);
                    solarPotList[i].Add(numb);
                }
            }

            double maxbat = double.MaxValue;
            if (!DA.GetData(13, ref maxbat)) { };
            double maxtes = double.MaxValue;
            if (!DA.GetData(14, ref maxtes)) { };



            if (start)
            {
                Ehub ehub = new Ehub(false, false, gfa, carbcon, elecdem, cooldem, shdem, dhwdem, feedin, grid, carbon, temp, pvarea, solarPotList, maxbat, maxtes);

                DA.SetData(0, ehub.outputs.cost);
                DA.SetData(1, ehub.outputs.carbon);
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
