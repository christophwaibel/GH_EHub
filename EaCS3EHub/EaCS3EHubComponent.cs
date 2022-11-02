using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EaCS3EHub.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
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
            : base("EnergyHub CompECD HS2022", "EHub2022",
                "Energy Hub component for the CompECD HS 2022 course at ETHZ",
                "EnergyHubs", "Solver")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // 0
            pManager.AddTextParameter("TechFile", "tech", "Filepath of technology.csv that contains energy system technology parameters", GH_ParamAccess.item);

            // 1 - 4
            pManager.AddNumberParameter("Heating Loads", "htg", "Annual hourly heating loads (kW/h)",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Cooling Loads", "clg", "Annual hourly cooling loads (kW/h)",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("DHW Loads", "dhw", "Annual hourly domestic hot water loads (kW/h)",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Electricity Loads", "elec", "Annual hourly electricity loads (kW/h)",
                GH_ParamAccess.list);

            // 5, 6
            pManager.AddNumberParameter("Solar Tech Areas", "srfs",
                "Surface areas for solar energy system technologies (PV, ST, PVT, ...) (m^2)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Solar Potentials", "sol",
                "Annual hourly solar potentials for each of the available surfaces (Wh)", GH_ParamAccess.tree);

            
            // 7, 8
            pManager.AddNumberParameter("GHI", "GHI",
                "Global Horizontal Irradiation annual hourly timeseries from an .epw", GH_ParamAccess.list);
            pManager.AddNumberParameter("dryBulb", "dryBulb",
                "Dry bulb temperature annual hourly timeseries from an .epw", GH_ParamAccess.list);

            // 9
            pManager.AddBooleanParameter("run", "run", "run ehub", GH_ParamAccess.item);

            // 10
            pManager.AddIntegerParameter("epsilon", "epsilon", "number of epsilon cuts. Min 1, default 3", GH_ParamAccess.item, 3);
            pManager[10].Optional = true;

            // 11 - 13
            pManager.AddGenericParameter("Building Package", "Building", "Building Package, containing loads, and construction cost and emissions. " +
                "If multiple building packages are provided, the solver will treat them as decision variable. Useful, e.g. to determine optimal retrofitting package.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Conversion Tech", "ConvTech", "Conversion Technologies considered in the ehub", GH_ParamAccess.list);
            pManager.AddGenericParameter("Storage Tech", "StorageTech", "Storage Technologies considered in the ehub", GH_ParamAccess.list);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // 0, 1
            pManager.AddNumberParameter("CAPEX", "capex",
                "Total annual installation cost of the energy system (based on NPV and lifetimes of technologies) (CHF/year).",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("OPEX", "opex",
                "Total annual operation cost of the energy system (CHF/year).", GH_ParamAccess.list);

            // 2
            pManager.AddNumberParameter("Emissions", "emissions",
                "Total annualized operational and embodied carbon emissions of the energy hub (CO2eq./year).",
                GH_ParamAccess.list);

        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string filePath = null;
            if(!DA.GetData(0, ref filePath)) return;
            if (!filePath.EndsWith(@"\"))
                filePath = filePath + @"\";

            var heating = new List<double>();
            var cooling = new List<double>();
            var dhw = new List<double>();
            var electricity = new List<double>();

            if(!DA.GetDataList(1, heating)) return;
            if(!DA.GetDataList(2, cooling)) return;
            if(!DA.GetDataList(3, dhw)) return;
            if(!DA.GetDataList(4, electricity)) return;

            var solarAreas = new List<double>();
            if(!DA.GetDataList(5, solarAreas)) return;

            GH_Structure<GH_Number> solarPotentials;
            if (!DA.GetDataTree(6, out solarPotentials)) return; 
            List<List<double>> solarPotList = new List<List<double>>();
            for (int i = 0; i < solarPotentials.Branches.Count; i++)
            {
                solarPotList.Add(new List<double>());
                foreach (GH_Number gooNumber in solarPotentials.get_Branch(i))
                {
                    double numb;
                    GH_Convert.ToDouble(gooNumber, out numb, GH_Conversion.Both);
                    solarPotList[i].Add(numb);
                }
            }

            var irradiance = new double[solarPotList.Count][];
            for (int i=0; i< solarPotList.Count; i++)
                irradiance[i] = solarPotList[i].ToArray();


            var ghi = new List<double>();
            var dryBulb = new List<double>();
            if(!DA.GetDataList(7, ghi)) return;
            if(!DA.GetDataList(8, dryBulb)) return;


            bool run = false;
            DA.GetData(9, ref run);

            int epsilonCuts = 3;
            DA.GetData(10, ref epsilonCuts);


            if (run)
            {
                try
                {
                    LoadTechParameters(filePath + "technologies.csv", out var technologyParameters);
                    technologyParameters.Add("NumberOfBuildingsInEHub", Convert.ToDouble(1));
                    technologyParameters.Add("Peak_Htg_" + Convert.ToString(0), heating.Max());
                    technologyParameters.Add("Peak_Clg_" + Convert.ToString(0), cooling.Max());
                    Rhino.RhinoApp.WriteLine("_____LOADING INPUTS COMPLETE_____");
                    Rhino.RhinoApp.WriteLine("_________________________________");
                    Rhino.RhinoApp.WriteLine("_____CLUSTERING TYPICAL DAYS_____");

                    int numberOfSolarAreas = solarAreas.Count;
                    int numBaseLoads = 5; // heating, cooling, electricity, ghi, tamb
                    int numLoads =
                        numBaseLoads +
                        numberOfSolarAreas; // heating, cooling, electricity, ghi, tamb, solar. however, solar will include several profiles.
                    const int hoursPerYear = 8760;
                    double[][] fullProfiles = new double[numLoads][];
                    string[] loadTypes = new string[numLoads];
                    bool[] peakDays = new bool[numLoads];
                    bool[] correctionLoad = new bool[numLoads];
                    for (int u = 0; u < numLoads; u++)
                        fullProfiles[u] = new double[hoursPerYear];
                    loadTypes[0] = "heating";
                    loadTypes[1] = "cooling";
                    loadTypes[2] = "electricity";
                    loadTypes[3] = "ghi";
                    loadTypes[4] = "Tamb";
                    peakDays[0] = true;
                    peakDays[1] = true;
                    peakDays[2] = true;
                    peakDays[3] = false;
                    peakDays[4] = false;
                    correctionLoad[0] = true;
                    correctionLoad[1] = true;
                    correctionLoad[2] = true;
                    correctionLoad[3] = false;
                    correctionLoad[4] = false;

                    bool[]
                        useForClustering =
                            new bool[fullProfiles
                                .Length]; // specificy here, which load is used for clustering. the others are just reshaped
                    for (int t = 0; t < hoursPerYear; t++)
                    {
                        fullProfiles[0][t] = heating[t] + dhw[t];
                        fullProfiles[1][t] = cooling[t];
                        fullProfiles[2][t] = electricity[t];
                        fullProfiles[3][t] = ghi[t];
                        fullProfiles[4][t] = dryBulb[t];
                        useForClustering[0] = true;
                        useForClustering[1] = true;
                        useForClustering[2] = true;
                        useForClustering[3] = true;
                        useForClustering[4] = false;
                    }

                    for (int u = 0; u < numberOfSolarAreas; u++)
                    {
                        useForClustering[u + numBaseLoads] = false;
                        peakDays[u + numBaseLoads] = false;
                        correctionLoad[u + numBaseLoads] = true;
                        loadTypes[u + numBaseLoads] = "solar";
                        for (int t = 0; t < hoursPerYear; t++)
                            fullProfiles[u + numBaseLoads][t] = irradiance[u][t];
                    }

                    // TO DO: load in GHI time series, add it to full profiles (right after heating, cooling, elec), and use it for clustering. exclude other solar profiles from clustering, but they need to be reshaped too
                    EhubMisc.HorizonReduction.TypicalDays typicalDays = EhubMisc.HorizonReduction.GenerateTypicalDays(
                        fullProfiles, loadTypes,
                        12, peakDays, useForClustering, correctionLoad, true);

                    /// Running Energy Hub
                    Rhino.RhinoApp.WriteLine("Solving MILP optimization model...");
                    double[][] typicalSolarLoads = new double[numberOfSolarAreas][];

                    // solar profiles negative or very small numbers. rounding floating numbers thing?
                    for (int u = 0; u < numberOfSolarAreas; u++)
                    {
                        typicalSolarLoads[u] = typicalDays.DayProfiles[numBaseLoads + u];
                        for (int t = 0; t < typicalSolarLoads[u].Length; t++)
                        {
                            if (typicalSolarLoads[u][t] < 0.1)
                                typicalSolarLoads[u][t] = 0.0;
                        }
                    }

                    // same for heating, cooling, elec demand... round very small numbers
                    for (int t = 0; t < typicalDays.DayProfiles[0].Length; t++)
                    for (int i = 0; i < 3; i++)
                        if (typicalDays.DayProfiles[i][t] < 0.001)
                            typicalDays.DayProfiles[i][t] = 0.0;


                    int[] clustersizePerTimestep = typicalDays.NumberOfDaysPerTimestep;
                    Ehub ehub = new Ehub(typicalDays.DayProfiles[0], typicalDays.DayProfiles[1],
                        typicalDays.DayProfiles[2],
                        typicalSolarLoads, solarAreas.ToArray(),
                        typicalDays.DayProfiles[4], technologyParameters,
                        clustersizePerTimestep);
                    ehub.Solve(epsilonCuts, true);

                    Rhino.RhinoApp.WriteLine("___________________________");
                    Rhino.RhinoApp.WriteLine("ENERGY HUB SOLVER COMPLETED");

                    var capex = new List<double>();
                    var opex = new List<double>();
                    var emissions = new List<double>();
                    for (int i = 0; i < ehub.Outputs.Length; i++)
                    {
                        capex.Add(ehub.Outputs[i].CAPEX);
                        opex.Add(ehub.Outputs[i].OPEX);
                        emissions.Add(ehub.Outputs[i].carbon);
                    }
                    DA.SetDataList(0, capex);
                    DA.SetDataList(1, opex);
                    DA.SetDataList(2, emissions);


                    WriteOutput("EaCS3_HS21_Results_Ehub", filePath, numberOfSolarAreas, ehub, typicalDays, numBaseLoads);
                    Rhino.RhinoApp.WriteLine("___________________________");
                    Rhino.RhinoApp.WriteLine("RESULTS WRITTEN TO" + filePath.ToString());
                }
                catch (Exception e)
                {
                    Rhino.RhinoApp.WriteLine(e.Message);
                    throw;
                }
            }




            // return results to outputs IN GH component


        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return Resources.IO_Energysytems;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("d3733eec-10ec-4f5b-936b-57f3e1a724f3"); }
        }



        static void LoadTechParameters(string inputFile, out Dictionary<string, double> technologyParameters)
        {
            // load technology parameters
            technologyParameters = new Dictionary<string, double>();


            string[] lines = File.ReadAllLines(inputFile);
            for (int li = 0; li < lines.Length; li++)
            {
                string[] split = lines[li].Split(new char[2] { ';', ',' });
                split = split.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                if (split.Length < 3)
                {
                    //WriteError();
                    Console.WriteLine("Reading line {0}:..... '{1}'", li + 1, lines[li]);
                    Console.Write("'{0}' contains {1} cells in line {2}, but it should contain at least 3 lines - the first two being strings and the third a number... Hit any key to abort the program: ",
                        inputFile, split.Length, li + 1);
                    Console.ReadKey();
                    return;
                }
                else
                {
                    if (technologyParameters.ContainsKey(split[0])) continue;
                    technologyParameters.Add(split[0], Convert.ToDouble(split[2]));
                }
            }
        }


        static void WriteOutput(string scenario, string path, int numberOfSolarAreas, Ehub ehub, EhubMisc.HorizonReduction.TypicalDays typicalDays, int numBaseLoads)
        {
            // check, if results folder exists in inputs folder
            string outputPath = path + @"results\";
            Directory.CreateDirectory(outputPath);

            // write a csv for each epsilon cut, name it "_e1", "_e2", .. etc
            List<string> header = new List<string>();
            // write units to 2nd row
            List<string> header_units = new List<string>();
            header.Add("Lev.Emissions");
            header_units.Add("kgCO2eq/a");
            header.Add("Lev.Cost");
            header_units.Add("CHF/a");
            header.Add("OPEX");
            header_units.Add("CHF/a");
            header.Add("CAPEX");
            header_units.Add("CHF/a");
            header.Add("DistrictHeatingCost");
            header_units.Add("CHF/a");
            header.Add("x_DistrictHeatingNetwork");
            header_units.Add("m");
            header.Add("x_coolingTower");
            header_units.Add("kWh");
            header.Add("x_Battery");
            header_units.Add("kWh");
            header.Add("x_TES");
            header_units.Add("kWh");
            header.Add("x_clgTES");
            header_units.Add("kWh");
            header.Add("x_CHP");
            header_units.Add("kW");
            header.Add("x_Boiler");
            header_units.Add("kW");
            header.Add("x_BiomassBoiler");
            header_units.Add("kW");
            header.Add("x_ASHP");
            header_units.Add("kW");
            header.Add("x_ElecChiller");
            header_units.Add("kW");
            header.Add("x_Battery_charge");
            header_units.Add("kWh");
            header.Add("x_Battery_discharge");
            header_units.Add("kWh");
            header.Add("x_Battery_soc");
            header_units.Add("kWh");
            header.Add("x_TES_charge");
            header_units.Add("kWh");
            header.Add("x_TES_discharge");
            header_units.Add("kWh");
            header.Add("x_TES_soc");
            header_units.Add("kWh");
            header.Add("x_clgTES_charge");
            header_units.Add("kWh");
            header.Add("x_clgTES_discharge");
            header_units.Add("kWh");
            header.Add("x_clgTES_soc");
            header_units.Add("kWh");
            header.Add("x_CHP_op_e");
            header_units.Add("kWh");
            header.Add("x_CHP_op_h");
            header_units.Add("kWh");
            header.Add("x_CHP_dump");
            header_units.Add("kWh");
            header.Add("x_Boiler_op");
            header_units.Add("kWh");
            header.Add("x_BiomassBoiler_op");
            header_units.Add("kWh");
            header.Add("x_ASHP_op");
            header_units.Add("kWh");
            header.Add("x_AirCon_op");
            header_units.Add("kWh");
            header.Add("x_GridPurchase");
            header_units.Add("kWh");
            header.Add("x_FeedIn");
            header_units.Add("kWh");
            header.Add("x_DR_elec_pos");
            header_units.Add("kWh");
            header.Add("x_DR_elec_neg");
            header_units.Add("kWh");
            header.Add("x_DR_heat_pos");
            header_units.Add("kWh");
            header.Add("x_DR_heat_neg");
            header_units.Add("kWh");
            header.Add("x_DR_cool_pos");
            header_units.Add("kWh");
            header.Add("x_DR_cool_neg");
            header_units.Add("kWh");
            for (int i = 0; i < numberOfSolarAreas; i++)
            {
                header.Add("x_PV_" + i);
                header_units.Add("sqm");
            }
            for (int i = 0; i < ehub.NumberOfBuildingsInDistrict; i++)
            {
                header.Add("x_HeatExchanger_DH_" + i);
                header_units.Add("kW");
            }
            header.Add("b_PV_totalProduction");
            header_units.Add("kWh");
            header.Add("TypicalHeating");
            header_units.Add("kWh");
            header.Add("TypicalCooling");
            header_units.Add("kWh");
            header.Add("TypicalElectricity");
            header_units.Add("kWh");
            header.Add("TypicalGHI");
            header_units.Add(@"W/sqm");
            header.Add("TypicalAmbientTemp");
            header_units.Add("deg C");
            header.Add("ClusterSize");
            header_units.Add("Days");
            header.Add("BiomassConsumed");
            header_units.Add("kWh");
            for (int i = 0; i < numberOfSolarAreas; i++)
            {
                header.Add("TypicalPotentialsSP_" + i);
                header_units.Add("kWh/m^2");
            }

            for (int e = 0; e < ehub.Outputs.Length; e++)
            {
                List<List<string>> outputString = new List<List<string>>();
                if (ehub.Outputs[e].infeasible)
                {
                    outputString.Add(new List<string> { "Infeasible" });
                    Console.WriteLine("--- Infeasible Solution ---");
                }
                else
                {
                    outputString.Add(header);
                    outputString.Add(header_units);

                    List<string> firstLine = new List<string>();
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].carbon));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].cost));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].OPEX));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].CAPEX));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].cost_dh));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dh));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtower));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_boi));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bmboi));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_hp));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_ac));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_charge[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_discharge[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_soc[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_charge[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_discharge[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_soc[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_charge[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_discharge[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_soc[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_e[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_h[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_dump[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_boi_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_bmboi_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_hp_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_ac_op[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_elecpur[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_feedin[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_elec_pos[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_elec_neg[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_heat_pos[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_heat_neg[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_cool_pos[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_cool_neg[0]));
                    for (int i = 0; i < numberOfSolarAreas; i++)
                        firstLine.Add(Convert.ToString(ehub.Outputs[e].x_pv[i]));
                    for (int i = 0; i < ehub.NumberOfBuildingsInDistrict; i++)
                        firstLine.Add(Convert.ToString(ehub.Outputs[e].x_hx_dh[i]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].b_pvprod[0]));

                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[0][0]));
                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[1][0]));
                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[2][0]));
                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[3][0]));
                    firstLine.Add(Convert.ToString(typicalDays.DayProfiles[4][0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].clustersize[0]));
                    firstLine.Add(Convert.ToString(ehub.Outputs[e].biomassConsumed));
                    for (int i = 0; i < numberOfSolarAreas; i++)
                        firstLine.Add(Convert.ToString(typicalDays.DayProfiles[numBaseLoads + i][0]));

                    outputString.Add(firstLine);

                    for (int t = 1; t < ehub.Outputs[e].x_elecpur.Length; t++)
                    {
                        List<string> newLine = new List<string>();
                        for (int skip = 0; skip < 15; skip++)
                            newLine.Add("");
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_charge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_discharge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bat_soc[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_charge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_discharge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_tes_soc[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_charge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_discharge[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_clgtes_soc[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_e[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_op_h[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_chp_dump[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_boi_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_bmboi_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_hp_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_ac_op[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_elecpur[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_feedin[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_elec_pos[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_elec_neg[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_heat_pos[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_heat_neg[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_cool_pos[t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].x_dr_cool_neg[t]));
                        for (int skip = 0; skip < numberOfSolarAreas; skip++)
                            newLine.Add("");
                        for (int skip = 0; skip < ehub.NumberOfBuildingsInDistrict; skip++)
                            newLine.Add("");
                        newLine.Add(Convert.ToString(ehub.Outputs[e].b_pvprod[t]));

                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[0][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[1][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[2][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[3][t]));
                        newLine.Add(Convert.ToString(typicalDays.DayProfiles[4][t]));
                        newLine.Add(Convert.ToString(ehub.Outputs[e].clustersize[t]));
                        newLine.Add("");
                        for (int i = 0; i < numberOfSolarAreas; i++)
                            newLine.Add(Convert.ToString(typicalDays.DayProfiles[numBaseLoads + i][t]));

                        outputString.Add(newLine);
                    }

                }

                using (var sw = new StreamWriter(outputPath + scenario + "_result_epsilon_" + e + ".csv"))
                {
                    foreach (List<string> line in outputString)
                    {
                        foreach (string cell in line)
                            sw.Write(cell + ";");
                        sw.Write(Environment.NewLine);
                    }

                    sw.Close();
                }

            }
        }

    }
}
