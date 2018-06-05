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
            pManager.AddNumberParameter("solar", "solar", "Solar potentials hourly time series for multiple sensor points, in [W/m^2]. Should be patches * 4, beacuse 4 potential profiles per patch. will be averaged in this script.", GH_ParamAccess.tree);
            //13
            pManager.AddNumberParameter("maxbat", "maxbat", "Maximal battery capacity. E.g. 100kWh per storey and building.", GH_ParamAccess.item);
            //14
            pManager.AddNumberParameter("maxtes", "maxtes", "Maximal TES capacity. E.g. 1400 kWh per building.", GH_ParamAccess.item);
            //15
            pManager.AddBooleanParameter("redhor", "redhor", "Reduce horizon to 6 weeks?", GH_ParamAccess.item); 
            //16
            pManager.AddBooleanParameter("minCarbon", "minCarbon", "Objective: minimize carbon, instead cost? True, if min carbon.", GH_ParamAccess.item);
            //17
            pManager.AddBooleanParameter("multithread", "multithread", "Activate multi threading? If yes, maximum available threads are used. If false, it's limited to 1 thread.", GH_ParamAccess.item);
            //18
            pManager.AddNumberParameter("rent", "rent", "Rent per m2", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // 0 - 1
            pManager.AddNumberParameter("cost", "cost", "Objective Value. Discounted annual cost for installation and operation of energy system, in [CHF].", GH_ParamAccess.item);
            pManager.AddNumberParameter("co2", "co2", "Specific annual carbon emissions for operation of energy system in [kgCO2/m2].", GH_ParamAccess.item);

            // 2 - 4
            pManager.AddNumberParameter("capacities", "capacities", "Sized capacities of energy technologies in kW or kWh (storages). [0] AC; [1] hp_s; [2] hp_m; [3] hp_l; [4] chp_s; [5] chp_m; [6] chp_l; [7] boi_s; [8] boi_m; [9] boi_l; [10] bat; [11] tes", GH_ParamAccess.list);
            pManager.AddNumberParameter("pv m2", "pv m2", "installed pv per patch in m2.", GH_ParamAccess.list);
            pManager.AddNumberParameter("st m2", "st m2", "installed st per patch in m2.", GH_ParamAccess.list);

            // 5
            pManager.AddNumberParameter("hp_op", "hp_op", "Heat pump operation schedule in kW", GH_ParamAccess.list);

            // 6-7
            pManager.AddNumberParameter("chp_op_h", "chp_op_h", "Combined Heat and Power operation schedule (heating) in kW", GH_ParamAccess.list);
            pManager.AddNumberParameter("chp_op_e", "chp_op_e", "Combined Heat and Power operation schedule (electricity) in kW", GH_ParamAccess.list);

            // 8
            pManager.AddNumberParameter("boi_op", "boi_op", "Boiler operation schedule in kW", GH_ParamAccess.list);

            // 9-11
            pManager.AddNumberParameter("ch_bat", "ch_bat", "Charging schedule of battery in kW.", GH_ParamAccess.list);
            pManager.AddNumberParameter("dis_bat", "dis_bat", "Discharging schedule of battery in kW.", GH_ParamAccess.list);
            pManager.AddNumberParameter("soc_bat", "soc_bat", "State of charge schedule of battery in kWh.", GH_ParamAccess.list);

            // 12-14
            pManager.AddNumberParameter("ch_tes", "ch_tes", "Charging schedule of Thermal Energy Storage in kW.", GH_ParamAccess.list);
            pManager.AddNumberParameter("dis_tes", "dis_tes", "Discharging schedule of Thermal Energy Storage in kW.", GH_ParamAccess.list);
            pManager.AddNumberParameter("soc_tes", "soc_tes", "State of charge schedule of Thermal Energy Storage in kWh.", GH_ParamAccess.list);

            // 15-18
            pManager.AddGenericParameter("heat_dump", "heat_dump", "Heat dumped in kWh", GH_ParamAccess.list);
            pManager.AddGenericParameter("pv_prod", "pv_prod", "PV electricity generation in kWh", GH_ParamAccess.list);
            pManager.AddNumberParameter("ElecPur", "dblEpur", "schedule of electricity purchased", GH_ParamAccess.list);
            pManager.AddNumberParameter("ElecSell", "dblEsell", "schedule of electricity sold", GH_ParamAccess.list);

            // 19-22
            pManager.AddNumberParameter("red_elec", "red_elec", "Horizon-reduced electricity demand. 1008 hours, 6 weeks.", GH_ParamAccess.list);
            pManager.AddNumberParameter("red_sh", "red_sh", "Horizon-reduced space heating demand. 1008 hours, 6 weeks.", GH_ParamAccess.list);
            pManager.AddNumberParameter("red_dhw", "red_dhw", "Horizon-reduced domestic hot water demand. 1008 hours, 6 weeks.", GH_ParamAccess.list);
            pManager.AddNumberParameter("red_cool", "red_cool", "Horizon-reduced cooling demand. 1008 hours, 6 weeks.", GH_ParamAccess.list);
            //23
            pManager.AddNumberParameter("totCost", "totCost", "Total Cost, i.e. cost of energy system + -Rent*area", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! CAREFUL HARDCODED STUFF LINES 160 following


            bool start = false;
            if (!DA.GetData(0, ref start)) { };

            double carbcon = double.MaxValue;
            if (!DA.GetData(1, ref carbcon)) { };

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

            //assuming 30% of the facade can be covered with PV
            for(int i=0; i<pvarea.Count; i++)
            {
                if(i>8 && i<45) pvarea[i] *= 0.3;
                if (i > 53 && i < 90) pvarea[i] *= 0.3;
                if (i > 98 && i < 135) pvarea[i] *= 0.3;
                if (i> 143) pvarea[i] *= 0.3;
            }

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

            bool reducedhorizon = false;
            if (!DA.GetData(15, ref reducedhorizon)) { };

            bool mincarbon = false;
            if (!DA.GetData(16, ref mincarbon)) { };

            bool multithreading = true;
            if (!DA.GetData(17, ref multithreading)) { };

            double rent = 65;
            if(!DA.GetData(18, ref rent)){};

            if (start)
            {
                Ehub ehub = new Ehub(multithreading, mincarbon , false, gfa, carbcon, elecdem, cooldem, shdem, dhwdem, feedin, grid, carbon, temp, pvarea, solarPotList, maxbat, maxtes,reducedhorizon);

                if (!ehub.outputs.infeasible)
                {
                    DA.SetData(0, ehub.outputs.cost);
                    DA.SetData(1, ehub.outputs.carbon);

                    List<double> x_cap = new List<double>();
                    x_cap.Add(ehub.outputs.x_ac);
                    x_cap.Add(ehub.outputs.x_hp_s);
                    x_cap.Add(ehub.outputs.x_hp_m);
                    x_cap.Add(ehub.outputs.x_hp_l);
                    x_cap.Add(ehub.outputs.x_chp_s);
                    x_cap.Add(ehub.outputs.x_chp_m);
                    x_cap.Add(ehub.outputs.x_chp_l);
                    x_cap.Add(ehub.outputs.x_boi_s);
                    x_cap.Add(ehub.outputs.x_boi_m);
                    x_cap.Add(ehub.outputs.x_boi_l);
                    x_cap.Add(ehub.outputs.x_bat);
                    x_cap.Add(ehub.outputs.x_tes);
                    DA.SetDataList(2, x_cap);

                    List<double> x_pv = new List<double>();
                    List<double> x_st = new List<double>();
                    for (int p = 0; p < ehub.outputs.x_pv.Length; p++)
                    {
                        x_pv.Add(ehub.outputs.x_pv[p]);
                        x_st.Add(ehub.outputs.x_st[p]);
                    }
                    DA.SetDataList(3, x_pv);
                    DA.SetDataList(4, x_st);

                    List<double> x_hp_op = new List<double>();
                    List<double> x_chp_op_h = new List<double>();
                    List<double> x_chp_op_e = new List<double>();
                    List<double> x_boi_op = new List<double>();
                    List<double> x_bat_ch_op = new List<double>();
                    List<double> x_bat_dis_op = new List<double>();
                    List<double> x_bat_soc_op = new List<double>();
                    List<double> x_tes_ch_op = new List<double>();
                    List<double> x_tes_dis_op = new List<double>();
                    List<double> x_tes_soc_op = new List<double>();
                    List<double> x_dump_op = new List<double>();
                    List<double> x_pv_prod = new List<double>();
                    List<double> x_elec_pur = new List<double>();
                    List<double> x_elec_sell = new List<double>();
                    List<double> d_elec_red = new List<double>();
                    List<double> d_sh_red = new List<double>();
                    List<double> d_dhw_red = new List<double>();
                    List<double> d_cool_red = new List<double>();

                    for (int t = 0; t < ehub.horizon; t++)
                    {
                        x_hp_op.Add(ehub.outputs.x_hp_s_op_sh[t] + ehub.outputs.x_hp_s_op_dhw[t] + ehub.outputs.x_hp_m_op_sh[t] + ehub.outputs.x_hp_m_op_dhw[t] + ehub.outputs.x_hp_l_op_sh[t] + ehub.outputs.x_hp_l_op_dhw[t]);
                        x_chp_op_h.Add(ehub.outputs.x_chp_s_op_sh[t] + ehub.outputs.x_chp_s_op_dhw[t] + ehub.outputs.x_chp_m_op_sh[t] + ehub.outputs.x_chp_m_op_dhw[t] + ehub.outputs.x_chp_l_op_sh[t] + ehub.outputs.x_chp_l_op_dhw[t]);
                        x_chp_op_e.Add(ehub.outputs.x_chp_s_op_e[t] + ehub.outputs.x_chp_m_op_e[t] + ehub.outputs.x_chp_l_op_e[t]);
                        x_boi_op.Add(ehub.outputs.x_boi_s_op_sh[t] + ehub.outputs.x_boi_s_op_dhw[t] + ehub.outputs.x_boi_m_op_sh[t] + ehub.outputs.x_boi_m_op_dhw[t] + ehub.outputs.x_boi_l_op_sh[t] + ehub.outputs.x_boi_l_op_dhw[t]);
                        x_bat_ch_op.Add(ehub.outputs.x_batcharge[t]);
                        x_bat_dis_op.Add(ehub.outputs.x_batdischarge[t]);
                        x_bat_soc_op.Add(ehub.outputs.x_batsoc[t]);
                        x_tes_ch_op.Add(ehub.outputs.x_tescharge_sh[t]+ ehub.outputs.x_tescharge_dhw[t]);
                        x_tes_dis_op.Add(ehub.outputs.x_tesdischarge_sh[t]+ ehub.outputs.x_tesdischarge_dhw[t]);
                        x_tes_soc_op.Add(ehub.outputs.x_tessoc[t]);
                        x_dump_op.Add(ehub.outputs.x_chp_s_dump_sh[t]+ehub.outputs.x_chp_m_dump_sh[t]+ehub.outputs.x_chp_l_dump_sh[t]+ehub.outputs.x_chp_s_dump_sh[t]+ehub.outputs.x_chp_m_dump_sh[t]+ehub.outputs.x_chp_l_dump_sh[t]+
                        ehub.outputs.x_st_dump_dhw[t]+ehub.outputs.x_st_dump_sh[t]);
                        x_pv_prod.Add(ehub.outputs.b_pvprod[t]);
                        x_elec_pur.Add(ehub.outputs.x_elecpur[t]);
                        x_elec_sell.Add(ehub.outputs.x_feedin[t]); 
                        d_elec_red.Add(ehub.d_elec[t]);
                        d_sh_red.Add(ehub.d_sh[t]);
                        d_dhw_red.Add(ehub.d_dhw[t]);
                        d_cool_red.Add(ehub.d_cool[t]);
                    }

                    DA.SetDataList(5, x_hp_op);
                    DA.SetDataList(6, x_chp_op_h);
                    DA.SetDataList(7, x_chp_op_e);
                    DA.SetDataList(8, x_boi_op);
                    DA.SetDataList(9, x_bat_ch_op);
                    DA.SetDataList(10, x_bat_dis_op);
                    DA.SetDataList(11, x_bat_soc_op);
                    DA.SetDataList(12, x_tes_ch_op);
                    DA.SetDataList(13, x_tes_dis_op);
                    DA.SetDataList(14, x_tes_soc_op);
                    DA.SetDataList(15, x_dump_op);
                    DA.SetDataList(16, x_pv_prod);
                    DA.SetDataList(17, x_elec_pur);
                    DA.SetDataList(18, x_elec_sell);
                    DA.SetDataList(19, d_elec_red);
                    DA.SetDataList(20, d_sh_red);
                    DA.SetDataList(21, d_dhw_red);
                    DA.SetDataList(22, d_cool_red);

                    DA.SetData(23, ehub.outputs.cost + (rent * -1 * gfa));
                }
                else
                {
                    DA.SetData(0, 10000000);    // penalty value. should never get so high otherwise
                    DA.SetData(1, 200);         // same
                    DA.SetData(23, 10000000);
                }
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