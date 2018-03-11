using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using ILOG.CPLEX;
using ILOG.Concert;

namespace UrbanFormEHub
{
    /// <summary>
    /// Outputs. new struct per epsilon cut
    /// </summary>
    internal struct Ehub_outputs
    {

        internal double carbon;             // annual carbon.
        internal double cost;               // cost. levelized.
        internal double OPEX;               // annual operation cost.
        internal double CAPEX;              // capital cost. levelized.

        // Technology sizing
        internal double x_ac;               // air condition
        internal double[] x_pv;             // pv sizing [m2]
        internal double[] x_st;             // st sizing [m2]
        internal double x_hp_s;               // heat pump. assume it reaches peak heat temperatures as simplification.
        internal double x_chp_s;              // combined heat and power
        internal double x_boi_s;              // gas boiler
        internal double x_hp_m;               // heat pump. assume it reaches peak heat temperatures as simplification.
        internal double x_chp_m;              // combined heat and power
        internal double x_boi_m;              // gas boiler
        internal double x_hp_l;               // heat pump. assume it reaches peak heat temperatures as simplification.
        internal double x_chp_l;              // combined heat and power
        internal double x_boi_l;              // gas boiler
        internal double x_bat;              // battery 
        internal double x_tes;              // thermal storage

        // Operation. Time resolved.
        internal double[] x_elecpur;        // purchase from grid
        internal double[] x_feedin;         // feedin
        internal double[] x_batdischarge;   // battery discharge
        internal double[] x_batcharge;      // battery charge
        internal double[] x_batsoc;         // battery state of charge
        internal double[] x_tessoc;         // tes state of charge
        internal double[] x_tesdischarge_dhw;   // thermal energy storage (tes) discharge
        internal double[] x_tescharge_dhw;      // tes charge
        internal double[] x_tesdischarge_sh;   // thermal energy storage (tes) discharge
        internal double[] x_tescharge_sh;      // tes charge
        internal double[] x_st_optot_dhw;      // total st dhw operation
        internal double[] x_st_optot_sh;       // total st sh operation
        internal double[][] x_st_op_dhw;      // total st dhw operation
        internal double[][] x_st_op_sh;       // total st sh operation
        internal double[] x_hp_s_op_dhw;          // heat pump operation
        internal double[] x_boi_s_op_dhw;         // boiler operation
        internal double[] x_chp_s_op_dhw;       // chp operation heat
        internal double[] x_chp_s_dump_dhw;       // chp heat dumped
        internal double[] x_hp_m_op_dhw;          // heat pump operation
        internal double[] x_boi_m_op_dhw;         // boiler operation
        internal double[] x_chp_m_op_dhw;       // chp operation heat
        internal double[] x_chp_m_dump_dhw;       // chp heat dumped
        internal double[] x_hp_l_op_dhw;          // heat pump operation
        internal double[] x_boi_l_op_dhw;         // boiler operation
        internal double[] x_chp_l_op_dhw;       // chp operation heat
        internal double[] x_chp_l_dump_dhw;       // chp heat dumped
        internal double[] x_hp_s_op_sh;          // heat pump operation
        internal double[] x_boi_s_op_sh;         // boiler operation
        internal double[] x_chp_s_op_sh;       // chp operation heat
        internal double[] x_chp_s_dump_sh;       // chp heat dumped
        internal double[] x_hp_m_op_sh;          // heat pump operation
        internal double[] x_boi_m_op_sh;         // boiler operation
        internal double[] x_chp_m_op_sh;       // chp operation heat
        internal double[] x_chp_m_dump_sh;       // chp heat dumped
        internal double[] x_hp_l_op_sh;          // heat pump operation
        internal double[] x_boi_l_op_sh;         // boiler operation
        internal double[] x_chp_l_op_sh;       // chp operation heat
        internal double[] x_chp_l_dump_sh;       // chp heat dumped
        internal double[] x_chp_s_op_e;       // chp operation electricity
        internal double[] x_chp_m_op_e;       // chp operation electricity
        internal double[] x_chp_l_op_e;       // chp operation electricity

        internal double[] b_pvprod;     // total pv production
    }




    /// <summary>
    /// Energyhub class
    /// </summary>
    internal class Ehub
    {
        // Solar Thermal from shanshan, or ashouri
        // ashouri2013: q = A f (I - U ( Tstc - Tamb))
        // Fussbodenheizung rücklauf ~25°C      Vorlauf 35°C
        // radiator ~50°C       Vorlauf 60°C

        // AirCon COP depending on ambient temp. Gracik et al 2015, ->

        // economies of scale for energy technologies, like in george's paper
        // separate DHW and SH. that means, 2 different COP / efficiency curves for everything that is supply temperature dependant (ASHP)

        // add AirCon sizing to capacity cost

        // binaries for fix cost

        // minimum partload, not x % of capacity, but x % of minimum capacity, essentially a constant.


        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        // Take out partload... 
        // reduce to 3 weeks (winter, spring ,summer), and mirror for the second half of the year? via constraints? so soc[end] = soc[0]
        // use 12 average days
        // aggregate dhw and sh

        // get domestic hot water from pre-generated profiles, with stochasticity inside. scale up per m2 floor area

        //check data for ST (cost etc). check o&m cost

        // network cost and losses

        // lower and upper bounds of energy technologies according to george's thesis





        #region global variables and constants

        internal Ehub_outputs outputs;




        ///////////////////////////////////////////////////////////////
        // Constants. Mainly technology properties
        ///////////////////////////////////////////////////////////////

        // Technology properties (efficiency)
        // PV data
        internal const double pv_NCOT = 45;             // Nominal operating cell temperatuer
        internal const double pv_T_aNCOT = 20;          // reference Temperature for NCOT
        internal const double pv_P_NCOT = 800;          // irradiation of NCOT
        internal const double pv_beta_ref = 0.004;      // temperature coefficient
        internal const double pv_n_ref = 0.15;           // PV efficiency under NCOT     //ISE freiburg has 22,6 CIGS. 

        // ST Solar Thermal data
        internal const double st_Frta = 0.68;
        internal const double st_FrUl = 4.9;
        internal const double st_Tin_dhw = 65.0;
        internal const double st_Tin_sh = 25.0;

        // Air source heat pump data
        internal const double hp_pi1 = 13.39;           // coefficient 1
        internal const double hp_pi2 = -0.047;          // coefficient 2
        internal const double hp_pi3 = 1.109;           // coefficient 3
        internal const double hp_pi4 = 0.012;           // coefficient 4
        internal const double hp_sup_dhw = 65.0;        // supply temperature domestic hot water
        internal const double hp_sup_sh = 35.0;         // supply temperature space heating

        // Boiler data
        internal const double c_boi_eff = 0.94;         // efficiency (natural gas to heat)

        // Combined Heat and Power (CHP) data
        internal const double c_chp_s_eff = 0.33;          // efficiency (natural gas to electricity)
        internal const double c_chp_s_htp = 1.5757;         // heat to power ratio (for 1kW of heat, 1.73 kW of electricity)
        internal const double c_chp_s_minload = 2.5;      // minimum part load. 50% of 5kW
        internal const double c_chp_m_eff = 0.34;          // efficiency (natural gas to electricity)
        internal const double c_chp_m_htp = 1.5735;         // heat to power ratio (for 1kW of heat, 1.73 kW of electricity)
        internal const double c_chp_m_minload = 25;      // minimum part load. 50% of 50kW
        internal const double c_chp_l_eff = 0.35;          // efficiency (natural gas to electricity)
        internal const double c_chp_l_htp = 1.5714;         // heat to power ratio (for 1kW of heat, 1.73 kW of electricity)
        internal const double c_chp_l_minload = 100;      // minimum part load. 50% of 200kW
        internal const double c_chp_heatdump = 1;       // heat dump allowed (1 = 100%)

        // Air conditioning
        internal const double ac_cop = 3;                   // η=3

        // Battery data
        internal const double bat_ch_eff = 0.9;            // Battery charging efficiency
        internal const double bat_disch_eff = 0.9;         // Battery discharging efficiency
        internal const double bat_decay = 0.001;            // Battery hourly decay
        internal const double bat_max_ch = 0.4;             // Battery max charging rate. 
        internal const double bat_max_disch = 0.4;         // Battery max discharging rate. 
        internal const double bat_min_state = 0.3;          // Battery minimum state of charge. 

        // Thermal Energy Storage tank data
        internal const double tes_ch_eff = 0.9;             // Thermal energy storage (TES) charging efficiency
        internal const double tes_disch_eff = 0.9;          // TES discharging efficiency
        internal const double tes_decay = 0.01;            // TES heat loss
        internal const double tes_max_ch = 0.4;            // TES max charging rate
        internal const double tes_max_disch = 0.4;         // TES max discharging rate





        // put this to zero, if I wanna excluce embodied carbon.
        internal const double lca_pv = 0.0; // per m2. KBOB 2012. 8m2 assumed by george and magnolis
        //internal const double lca_pv_facade = 0.0; // per m2. KBOB 2012
        internal const double lca_st = 0.0;
        internal const double lca_battery = 0.0;         // per kWh installed. Hiremath et al. (2015)
        internal const double lca_therm = 0.0;              // per kWh installed
        internal const double lca_hp = 0.0;                 // per kW installed
        internal const double lca_chp = 0.0;                // per kW installed
        internal const double lca_ac = 0.0;                 // per kW installed
        internal const double lca_boiler = 0.0;             // per kW installed


        internal const double lca_gas = 0.198;                // natural gas from grid, per kWh


        // Economic data
        internal const double intrate = 0.04;

        internal const double CostPV = 400;             // Cost PV per m2. assume cheap production of CIGS
        internal const double CostST = 1000;
        internal const double CostHP_s = 1530;            // Cost ASHP per kW installed, < 50 kW
        internal const double CostHP_m = 910;            // Cost ASHP per kW installed, 50 - 200 kW
        internal const double CostHP_l = 770;            // Cost ASHP per kW installed, > 200 kW
        internal const double CostBoi_s = 640;            // Cost Boiler per kW installed, < 50 kW
        internal const double CostBoi_m = 330;            // Cost Boiler per kW installed, 50 - 200 kW
        internal const double CostBoi_l = 200;            // Cost Boiler per kW installed, > 200 kW
        internal const double CostCHP_s = 3100;           // Cost CHP per kW installed, < 50 kW
        internal const double CostCHP_m = 1530;           // Cost CHP per kW installed, 50 - 200 kW
        internal const double CostCHP_l = 790;           // Cost CHP per kW installed, > 200 kW
        internal const double CostAC = 360;             // AirCon capital cost per kW
        internal const double CostBat = 2000;            // Battery capital cost per kWh
        internal const double CostTES = 150;            // TES capital cost per kWh

        internal const double fixCostPV = 5750;
        internal const double fixCostST = 4000;
        internal const double fixCostHP_s = 6830;
        internal const double fixCostHP_m = 43130;
        internal const double fixCostHP_l = 87840;
        internal const double fixCostBoi_s = 11920;
        internal const double fixCostBoi_m = 32170;
        internal const double fixCostBoi_l = 66100;
        internal const double fixCostCHP_s = 43450;
        internal const double fixCostCHP_m = 124860;
        internal const double fixCostCHP_l = 299140;
        internal const double fixCostAC = 0;            //???????????
        internal const double fixCostBat = 0;
        internal const double fixCostTES = 1685;

        internal const double CostNet = 800;            // district heating network cost CHF/m
        internal const double CostNetExch = 100;        // district heating heat exchanger cost CHF/kW

        internal const double LifePV = 20;              // PV lifetime
        internal const double LifeST = 20;
        internal const double LifeHP = 20;              // HP lifetime 
        internal const double LifeBoi = 25;             // Boiler lifetime
        internal const double LifeCHP = 20;             // CHP lifetime
        internal const double LifeBat = 20;             // Battery lifetime
        internal const double LifeTES = 20;             // TES lifetime
        internal const double LifeAC = 20;              // AirCon lifetime
        internal const double LifeNet = 40;             // District heating network lifetime
        internal const double LifeNetExch = 20;         // DH network heating exchanger lifetime

        internal static double annuityPV = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifePV)))));
        internal static double annuityST = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeST)))));
        internal static double annuityHP = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeHP)))));
        internal static double annuityBoi = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeBoi)))));
        internal static double annuityCHP = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeCHP)))));
        internal static double annuityAC = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeAC)))));
        internal static double annuityBat = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeBat)))));
        internal static double annuityTES = intrate / (1 - (1 / (Math.Pow((1 + intrate), (LifeTES)))));

        internal static double c_pv = CostPV * annuityPV;       // levelized cost PV
        internal static double c_st = CostST * annuityST;
        internal static double c_hp_s = CostHP_s * annuityHP;       // levelized cost HP
        internal static double c_hp_m = CostHP_m * annuityHP;       // levelized cost HP
        internal static double c_hp_l = CostHP_l * annuityHP;       // levelized cost HP
        internal static double c_boi_s = CostBoi_s * annuityBoi;    // levelized cost boiler
        internal static double c_boi_m = CostBoi_m * annuityBoi;    // levelized cost boiler
        internal static double c_boi_l = CostBoi_l * annuityBoi;    // levelized cost boiler
        internal static double c_chp_s = CostCHP_s * annuityCHP;    // levelized cost CHP
        internal static double c_chp_m = CostCHP_m * annuityCHP;    // levelized cost CHP
        internal static double c_chp_l = CostCHP_l * annuityCHP;    // levelized cost CHP
        internal static double c_ac = CostAC * annuityAC;       // levelized cost AirCon
        internal static double c_bat = CostBat * annuityBat;    // levelized cost battery
        internal static double c_tes = CostTES * annuityTES;    // levelized cost TES

        internal static double fc_pv = fixCostPV * annuityPV;
        internal static double fc_st = fixCostST * annuityST;
        internal static double fc_hp_s = fixCostHP_s * annuityHP;
        internal static double fc_hp_m = fixCostHP_m * annuityHP;
        internal static double fc_hp_l = fixCostHP_l * annuityHP;
        internal static double fc_boi_s = fixCostBoi_s * annuityBoi;
        internal static double fc_boi_m = fixCostBoi_m * annuityBoi;
        internal static double fc_boi_l = fixCostBoi_l * annuityBoi;
        internal static double fc_chp_s = fixCostCHP_s * annuityCHP;
        internal static double fc_chp_m = fixCostCHP_m * annuityCHP;
        internal static double fc_chp_l = fixCostCHP_l * annuityCHP;
        internal static double fc_ac = fixCostAC * annuityAC;
        internal static double fc_bat = fixCostBat * annuityBat;
        internal static double fc_tes = fixCostTES * annuityTES;

        internal static double c_pv_om = 0.0;                   // operating maintenance cost per kWh
        internal static double c_st_om = 0.0;
        internal static double c_hp_om = 0.0;
        internal static double c_boi_om = 0.0;
        internal static double c_chp_om = 0.0;
        internal static double c_tes_om = 0.0;
        internal static double c_bat_om = 0.0;

        internal static double c_gas = 0.113;                    // natural gas per kWh

        internal const double M = 9999999;  // big M method



        ///////////////////////////////////////////////////////////////
        // Inputs
        ///////////////////////////////////////////////////////////////
        private double[] b_dhw;                 // domestic hot water demand [kWh]
        private double[] b_sh;                  // space heating demand [kWh]
        private double[] b_cool;                // cooling demand [kWh]
        private double[] b_elec;                // electricity demand [kWh]
        private double[] c_grid;                // dynamic grid electricity cost [CHF/kWh]
        private double[] c_feedin;              // dynamic feedin tarif [CHF/kWh]
        private double[] a_carbon;              // dynamic carbon emission factor [g-CO2/kWh eq.]

        private double[][] a_solar;             // solar potentials. [W/m2]. array: [60][horizon]
        private double[] b_solar_area;          // available areas for pv [m2]

        private double[][] c_pv_eff;            // pv efficiency, time and pv patch dependant 
        private double[][] c_st_eff_dhw;        // st efficiency for domestic hot water, time and pv patch dependant
        private double[][] c_st_eff_sh;         // st efficiency for space heating, time and pv patch dependant
        private double[] c_hp_eff_dhw;          // hp efficiency for domestic hot water, time dependant 
        private double[] c_hp_eff_sh;           // hp efficiency for space heating, time dependant
        private double[] c_ac_eff;              // ac efficiency for cooling, time dependant

        private double[] c_num_of_days;            // number of days, that each typical day represents. should sum to 365. length of horizon. 24x same day.

        private double b_peakcool, b_peaksh, b_peakdhw, b_peakelec; //peak demands
        private double maxTEScap, maxBatcap;    // max capacities for thermal storage and battery. limited according to building floor area

        ///////////////////////////////////////////////////////////////
        // Properties
        ///////////////////////////////////////////////////////////////
        internal int horizon;       //timesteps, hourly. 10 days are 240 h


        #endregion



        /// <summary>
        /// Create new energy hub object.
        /// </summary>
        /// <param name="path">Path to inputs.</param>
        internal Ehub(string path, bool crbmin, double? crbconstr = null)
        {
            // ===================================================================
            // Pre-processing. Loading profiles etc.
            // ===================================================================
            //load profiles
            this.ReadInput(path,
                out this.b_elec, out this.b_sh, out this.b_dhw,
                out this.b_cool, out this.c_grid, out this.c_feedin, out this.a_carbon,
                out this.a_solar, out this.b_solar_area, out this.c_pv_eff,
                out this.c_st_eff_sh, out this.c_st_eff_dhw,
                out this.c_hp_eff_sh, out this.c_hp_eff_dhw, out this.c_ac_eff,
                out this.b_peakcool, out this.b_peaksh, out this.b_peakdhw, out this.b_peakelec,
                out this.maxBatcap, out this.maxTEScap);

            //reduce horizon to 6 weeks
            this.ReduceHorizon_6weeks(ref this.b_elec, ref this.b_sh, ref this.b_dhw,
                ref this.b_cool, ref this.c_grid, ref this.c_feedin, ref this.a_carbon, ref this.a_solar,
                ref this.c_pv_eff, ref this.c_st_eff_sh, ref this.c_st_eff_dhw,
                ref this.c_hp_eff_sh, ref this.c_hp_eff_dhw, ref this.c_ac_eff,
                out this.c_num_of_days);


            // horizon
            this.horizon = b_elec.Length;


            //solve
            this.outputs = this.ehubmodel_dhw(crbmin, crbconstr);


        }




        /// <summary>
        /// ehub with domestic hot water and space heating separate. and min partload
        /// </summary>
        /// <param name="bln_mincarbon"></param>
        /// <param name="carbonconstraint"></param>
        /// <returns></returns>
        private Ehub_outputs ehubmodel_dhw_minpartload(bool bln_mincarbon, double? carbonconstraint = null)
        {

            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // Initialize solver and other
            Cplex cpl = new Cplex();
            Ehub_outputs outs = new Ehub_outputs();



            int solarspots = this.b_solar_area.Length;   // number of different PV patches



            double b_co2_target = double.MaxValue;
            bool bln_co2target = false;


            if (!carbonconstraint.IsNullOrDefault())
            {
                b_co2_target = Convert.ToDouble(carbonconstraint);
                bln_co2target = true;
            }


            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // VARIABLES
            //int techs = 11;      // 0=HP_s, 1=HP_m, 2=HP_l, 3=Boi_s, 4=Boi_m, 5=Boi_l, 6=CHP_s, 7=CHP_m, 8=CHP_l. 9=PV, 10=ST, TES and Bat are separate
            //int demands = 3;    // 0 = elec, 1 = sh, 2 = dhw 
            //int[][] suit = new int[techs][];
            //suit[0] = new int[3] { 0, 1, 1 };   //hp_s
            //suit[1] = new int[3] { 0, 1, 1 };   //hp_m
            //suit[2] = new int[3] { 0, 1, 1 };   //hp_l
            //suit[3] = new int[3] { 0, 1, 1 };   //boi_s
            //suit[4] = new int[3] { 0, 1, 1 };   //boi_m
            //suit[5] = new int[3] { 0, 1, 1 };   //boi_l
            //suit[6] = new int[3] { 1, 1, 1 };   //chp_s
            //suit[7] = new int[3] { 1, 1, 1 };   //chp_m
            //suit[8] = new int[3] { 1, 1, 1 };   //chp_l
            //suit[9] = new int[3] { 1, 0, 0 };   //pv
            //suit[10] = new int[3] { 0, 1, 1 };   //st


            //AC - not variable, since only one cooling tech
            double x_AC = this.b_peakcool;                                  // sizing in kW. not a variable
            double[] x_ac_op = new double[this.horizon];                  // operation in kWh. not variable.
            double y_ac = (x_AC > 0) ? 1 : 0;

            // PV
            INumVar[] x_pv = new INumVar[solarspots];                          // m2 installed per patch (pvspots)
            for (int i = 0; i < solarspots; i++)
                x_pv[i] = cpl.NumVar(0, this.b_solar_area[i]);              // max available patch area in m2
            INumVar[] y1 = new INumVar[this.horizon];                       // binary control variable. to avoid selling and purchasing from and to the grid at the same time. from portia. 1 = electricity is being purchased
            INumVar[] x_purchase = new INumVar[this.horizon];               // purchase electricity from grid
            INumVar[] x_feedin = new INumVar[this.horizon];                 // feeding in PV electricity into the grid
            INumVar y_pv_fc = cpl.BoolVar();

            // ST
            INumVar[] x_st = new INumVar[solarspots];
            // variables for operating solar collector? or just like pv, no explicit operation variable
            INumVar[][] x_st_op_sh = new INumVar[solarspots][];
            INumVar[][] x_st_op_dhw = new INumVar[solarspots][];
            INumVar[][] x_st_dump_sh = new INumVar[solarspots][];
            INumVar[][] x_st_dump_dhw = new INumVar[solarspots][];
            for (int i = 0; i < solarspots; i++)
            {
                x_st[i] = cpl.NumVar(0, this.b_solar_area[i]);
                x_st_op_sh[i] = new INumVar[this.horizon];
                x_st_op_dhw[i] = new INumVar[this.horizon];
                x_st_dump_dhw[i] = new INumVar[this.horizon];
                x_st_dump_sh[i] = new INumVar[this.horizon];
            }
            INumVar y_st_fc = cpl.BoolVar();

            // HP
            INumVar x_hp_s = cpl.NumVar(5, 50);                           // capacity, in kW
            INumVar[] x_hp_s_op_sh = new INumVar[this.horizon];             // operation, in kWh, space heating
            INumVar[] x_hp_s_op_dhw = new INumVar[this.horizon];            // operation, in kWh, domestic hot water
            INumVar x_hp_m = cpl.NumVar(50, 200);                           // capacity, in kW
            INumVar[] x_hp_m_op_sh = new INumVar[this.horizon];                // operation, in kWh
            INumVar[] x_hp_m_op_dhw = new INumVar[this.horizon];                // operation, in kWh
            INumVar x_hp_l = cpl.NumVar(200, System.Double.MaxValue);       // capacity, in kW
            INumVar[] x_hp_l_op_sh = new INumVar[this.horizon];                // operation, in kWh
            INumVar[] x_hp_l_op_dhw = new INumVar[this.horizon];                // operation, in kWh
            INumVar y_hp_s_fc = cpl.BoolVar();
            INumVar y_hp_m_fc = cpl.BoolVar();
            INumVar y_hp_l_fc = cpl.BoolVar();

            // Boiler
            INumVar x_boi_s = cpl.NumVar(5, 50);                          // capacity, in kW
            INumVar[] x_boi_s_op_sh = new INumVar[this.horizon];               // operation, in kWh
            INumVar[] x_boi_s_op_dhw = new INumVar[this.horizon];               // operation, in kWh
            INumVar x_boi_m = cpl.NumVar(50, 200);                          // capacity, in kW
            INumVar[] x_boi_m_op_sh = new INumVar[this.horizon];               // operation, in kWh
            INumVar[] x_boi_m_op_dhw = new INumVar[this.horizon];               // operation, in kWh
            INumVar x_boi_l = cpl.NumVar(200, System.Double.MaxValue);      // capacity, in kW
            INumVar[] x_boi_l_op_sh = new INumVar[this.horizon];               // operation, in kWh
            INumVar[] x_boi_l_op_dhw = new INumVar[this.horizon];               // operation, in kWh
            INumVar y_boi_s_fc = cpl.BoolVar();
            INumVar y_boi_m_fc = cpl.BoolVar();
            INumVar y_boi_l_fc = cpl.BoolVar();

            // CHP
            INumVar x_chp_s = cpl.NumVar(5, 50);                          // capacity, in kW
            INumVar[] y_chp_op_s = new INumVar[this.horizon];              //binary for min part load
            INumVar[] x_chp_s_op_e = new INumVar[this.horizon];               // operation electricity, in kWh
            INumVar[] x_chp_s_op_dhw = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_s_dump_dhw = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar[] x_chp_s_op_sh = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_s_dump_sh = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar x_chp_m = cpl.NumVar(50, 200);                          // capacity, in kW
            INumVar[] y_chp_op_m = new INumVar[this.horizon];              //binary for min part load
            INumVar[] x_chp_m_op_e = new INumVar[this.horizon];               // operation electricity, in kWh
            INumVar[] x_chp_m_op_dhw = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_m_dump_dhw = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar[] x_chp_m_op_sh = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_m_dump_sh = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar x_chp_l = cpl.NumVar(200, System.Double.MaxValue);      // capacity, in kW
            INumVar[] y_chp_op_l = new INumVar[this.horizon];              //binary for min part load
            INumVar[] x_chp_l_op_e = new INumVar[this.horizon];               // operation electricity, in kWh
            INumVar[] x_chp_l_op_dhw = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_l_dump_dhw = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar[] x_chp_l_op_sh = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_l_dump_sh = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar y_chp_s_fc = cpl.BoolVar();
            INumVar y_chp_m_fc = cpl.BoolVar();
            INumVar y_chp_l_fc = cpl.BoolVar();

            // Thermal storage
            INumVar x_tes = cpl.NumVar(20, this.maxTEScap);               // 40 cubic meter size. thats like 4m x 5m x 2m. (x m3 * 35)
            INumVar[] x_tes_soc = new INumVar[this.horizon];              // state-of-charge, in kWh
            INumVar[] x_tes_ch_sh = new INumVar[this.horizon];               // charging storage, in kW
            INumVar[] x_tes_dis_sh = new INumVar[this.horizon];              // discharging storage, in kW
            INumVar[] x_tes_ch_dhw = new INumVar[this.horizon];               // charging storage, in kW
            INumVar[] x_tes_dis_dhw = new INumVar[this.horizon];              // discharging storage, in kW
            INumVar y_tes_fc = cpl.BoolVar();

            // Battery
            INumVar x_bat = cpl.NumVar(1, this.maxBatcap);                  // defined by urban form
            INumVar[] x_bat_ch = new INumVar[this.horizon];                 // charge battery [kW]
            INumVar[] x_bat_dis = new INumVar[this.horizon];                // discharge battery [kW]
            INumVar[] x_bat_soc = new INumVar[this.horizon];                // state-of-charge, stored electricity [kW]
            INumVar y_bat_fc = cpl.BoolVar();

            for (int t = 0; t < this.horizon; t++)
            {
                y1[t] = cpl.BoolVar();
                x_purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_feedin[t] = cpl.NumVar(0, System.Double.MaxValue);

                //x_ac_op[t] = cpl.NumVar(0, x_AC);

                for (int i = 0; i < solarspots; i++)
                {
                    x_st_op_sh[i][t] = cpl.NumVar(0, System.Double.MaxValue);
                    x_st_op_dhw[i][t] = cpl.NumVar(0, System.Double.MaxValue);
                    x_st_dump_sh[i][t] = cpl.NumVar(0, System.Double.MaxValue);
                    x_st_dump_dhw[i][t] = cpl.NumVar(0, System.Double.MaxValue);
                }

                x_chp_s_op_e[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_op_e[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_op_e[t] = cpl.NumVar(0, System.Double.MaxValue);
                y_chp_op_s[t] = cpl.BoolVar();
                y_chp_op_m[t] = cpl.BoolVar();
                y_chp_op_l[t] = cpl.BoolVar();

                x_hp_s_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_s_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_s_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_s_dump_sh[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_m_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_m_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_dump_sh[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_l_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_l_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_dump_sh[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_tes_ch_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_tes_dis_sh[t] = cpl.NumVar(0, System.Double.MaxValue);


                x_hp_s_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_s_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_s_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_s_dump_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_m_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_m_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_dump_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_l_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_l_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_dump_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_tes_ch_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_tes_dis_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);


                x_bat_ch[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_bat_dis[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_tes_soc[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_bat_soc[t] = cpl.NumVar(0, System.Double.MaxValue);
            }
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////





            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // CONSTRAINTS

            //constraint, saying x_st[spot] + x_pv[spot] <= area[spot]
            for (int i = 0; i < solarspots; i++)
            {
                cpl.AddGe(b_solar_area[i], cpl.Sum(x_st[i], x_pv[i]));
            }



            // meetings demands
            for (int t = 0; t < this.horizon; t++)
            {
                //cooling is not a variable, since only one tech
                x_ac_op[t] = this.b_cool[t];    //op cost and co2 is taken care of via electricity


                // electricity demand must be met by pv, grid, battery, and chp
                //      minus electricty sold to the grid, charging the battery, and for operating the heat pump
                ILinearNumExpr pvproduction = cpl.LinearNumExpr();
                ILinearNumExpr elecgeneration = cpl.LinearNumExpr();
                ILinearNumExpr elecadditionaldemand = cpl.LinearNumExpr();
                for (int i = 0; i < solarspots; i++)
                {
                    elecgeneration.AddTerm(this.a_solar[i][t] * 0.001 * this.c_pv_eff[i][t], x_pv[i]);
                    pvproduction.AddTerm(this.a_solar[i][t] * 0.001 * this.c_pv_eff[i][t], x_pv[i]);     // in kW
                }
                elecgeneration.AddTerm(1, x_purchase[t]);
                elecgeneration.AddTerm(1, x_bat_dis[t]);
                elecgeneration.AddTerm(c_chp_s_htp, x_chp_s_op_e[t]);
                elecgeneration.AddTerm(c_chp_m_htp, x_chp_m_op_e[t]);
                elecgeneration.AddTerm(c_chp_l_htp, x_chp_l_op_e[t]);
                elecadditionaldemand.AddTerm(1, x_feedin[t]);
                elecadditionaldemand.AddTerm(1, x_bat_ch[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_sh[t], x_hp_s_op_sh[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_sh[t], x_hp_m_op_sh[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_sh[t], x_hp_l_op_sh[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_dhw[t], x_hp_s_op_dhw[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_dhw[t], x_hp_m_op_dhw[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_dhw[t], x_hp_l_op_dhw[t]);
                cpl.AddEq(cpl.Diff(elecgeneration, cpl.Sum(c_ac_eff[t] * this.b_cool[t], elecadditionaldemand)), this.b_elec[t]);


                // space heating must be met by boiler, hp, chp, st, and TES
                //      minus heat for TES 
                ILinearNumExpr shgeneration = cpl.LinearNumExpr();
                ILinearNumExpr shadditionaldemand = cpl.LinearNumExpr();
                shadditionaldemand.AddTerm(1, x_tes_ch_sh[t]);
                shadditionaldemand.AddTerm(1, x_chp_s_dump_sh[t]);
                shadditionaldemand.AddTerm(1, x_chp_m_dump_sh[t]);
                shadditionaldemand.AddTerm(1, x_chp_l_dump_sh[t]);
                shgeneration.AddTerm(1, x_tes_dis_sh[t]);
                shgeneration.AddTerm(1, x_boi_s_op_sh[t]);
                shgeneration.AddTerm(1, x_hp_s_op_sh[t]);
                shgeneration.AddTerm(1, x_chp_s_op_sh[t]);
                shgeneration.AddTerm(1, x_boi_m_op_sh[t]);
                shgeneration.AddTerm(1, x_hp_m_op_sh[t]);
                shgeneration.AddTerm(1, x_chp_m_op_sh[t]);
                shgeneration.AddTerm(1, x_boi_l_op_sh[t]);
                shgeneration.AddTerm(1, x_hp_l_op_sh[t]);
                shgeneration.AddTerm(1, x_chp_l_op_sh[t]);
                for (int i = 0; i < solarspots; i++)
                {
                    shgeneration.AddTerm(1, x_st_op_sh[i][t]);
                    shadditionaldemand.AddTerm(1, x_st_dump_sh[i][t]);
                }
                cpl.AddEq(cpl.Diff(shgeneration, shadditionaldemand), this.b_sh[t]);


                // domestic hot water must be met by boiler, hp, chp, st, and TES
                //      minus heat for TES 
                ILinearNumExpr dhwgeneration = cpl.LinearNumExpr();
                ILinearNumExpr dhwadditionaldemand = cpl.LinearNumExpr();
                dhwadditionaldemand.AddTerm(1, x_tes_ch_dhw[t]);
                dhwadditionaldemand.AddTerm(1, x_chp_s_dump_dhw[t]);
                dhwadditionaldemand.AddTerm(1, x_chp_m_dump_dhw[t]);
                dhwadditionaldemand.AddTerm(1, x_chp_l_dump_dhw[t]);
                dhwgeneration.AddTerm(1, x_tes_dis_dhw[t]);
                dhwgeneration.AddTerm(1, x_boi_s_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_hp_s_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_chp_s_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_boi_m_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_hp_m_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_chp_m_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_boi_l_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_hp_l_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_chp_l_op_dhw[t]);
                for (int i = 0; i < solarspots; i++)
                {
                    dhwgeneration.AddTerm(1, x_st_op_dhw[i][t]);
                    dhwadditionaldemand.AddTerm(1, x_st_dump_dhw[i][t]);
                }
                cpl.AddEq(cpl.Diff(dhwgeneration, dhwadditionaldemand), this.b_dhw[t]);

                //pv production must be greater or equal feedin
                cpl.AddGe(pvproduction, x_feedin[t]);

                // donnot allow feedin and purchase at the same time. y = 1 means electricity is being purchased
                cpl.AddLe(x_purchase[t], cpl.Prod(M, y1[t]));
                cpl.AddLe(x_feedin[t], cpl.Prod(M, cpl.Diff(1, y1[t])));

                cpl.AddLe(x_chp_s_op_e[t], cpl.Prod(M, y_chp_op_s[t]));
                cpl.AddLe(x_chp_m_op_e[t], cpl.Prod(M, y_chp_op_m[t]));
                cpl.AddLe(x_chp_l_op_e[t], cpl.Prod(M, y_chp_op_l[t]));
            }



            for (int t = 0; t < this.horizon; t++)
            {
                // ST
                ILinearNumExpr stdump = cpl.LinearNumExpr();
                ILinearNumExpr stutil = cpl.LinearNumExpr();
                for (int i = 0; i < solarspots; i++)
                {
                    stdump.AddTerm(1, x_st_dump_dhw[i][t]);
                    stdump.AddTerm(1, x_st_dump_sh[i][t]);
                    stutil.AddTerm(1, x_st_op_dhw[i][t]);
                    stutil.AddTerm(1, x_st_op_sh[i][t]);

                    ILinearNumExpr sttotal = cpl.LinearNumExpr();
                    sttotal.AddTerm(Math.Pow(this.c_st_eff_sh[i][t], -1), x_st_dump_sh[i][t]);
                    sttotal.AddTerm(Math.Pow(this.c_st_eff_sh[i][t], -1), x_st_op_sh[i][t]);
                    sttotal.AddTerm(Math.Pow(this.c_st_eff_dhw[i][t], -1), x_st_dump_dhw[i][t]);
                    sttotal.AddTerm(Math.Pow(this.c_st_eff_dhw[i][t], -1), x_st_op_dhw[i][t]);
                    cpl.AddEq(cpl.Prod(this.a_solar[i][t] * 0.001, x_st[i]), sttotal);     // solar thermal production and dump is equal the solar potential of the sized m2 patch
                }
                //cpl.AddLe(stdump, stutil);  //dumping must be smaller than utilization


                // CHP 
                cpl.AddGe(x_chp_s_op_e[t], cpl.Prod(y_chp_op_s[t], c_chp_s_minload));
                cpl.AddGe(x_chp_m_op_e[t], cpl.Prod(y_chp_op_m[t], c_chp_m_minload));
                cpl.AddGe(x_chp_l_op_e[t], cpl.Prod(y_chp_op_l[t], c_chp_l_minload));
                cpl.AddLe(x_chp_s_op_e[t], x_chp_s);
                cpl.AddLe(x_chp_m_op_e[t], x_chp_m);
                cpl.AddLe(x_chp_l_op_e[t], x_chp_l);
                // heat recovery (SH + DHW) and heat dump from CHP is equal to electricity generation by CHP times heat to power ratio
                ILinearNumExpr chpheatrecov = cpl.LinearNumExpr();
                ILinearNumExpr chpheatfromelec = cpl.LinearNumExpr();
                chpheatrecov.AddTerm(1, x_chp_s_op_dhw[t]);
                chpheatrecov.AddTerm(1, x_chp_s_dump_dhw[t]);
                chpheatrecov.AddTerm(1, x_chp_m_op_dhw[t]);
                chpheatrecov.AddTerm(1, x_chp_m_dump_dhw[t]);
                chpheatrecov.AddTerm(1, x_chp_l_op_dhw[t]);
                chpheatrecov.AddTerm(1, x_chp_l_dump_dhw[t]);
                chpheatrecov.AddTerm(1, x_chp_s_op_sh[t]);
                chpheatrecov.AddTerm(1, x_chp_s_dump_sh[t]);
                chpheatrecov.AddTerm(1, x_chp_m_op_sh[t]);
                chpheatrecov.AddTerm(1, x_chp_m_dump_sh[t]);
                chpheatrecov.AddTerm(1, x_chp_l_op_sh[t]);
                chpheatrecov.AddTerm(1, x_chp_l_dump_sh[t]);
                chpheatfromelec.AddTerm(c_chp_s_htp, x_chp_s_op_e[t]);
                cpl.AddEq(chpheatrecov, chpheatfromelec);
                // Limiting the amount of heat that chps can dump
                cpl.AddLe(x_chp_s_dump_dhw[t], cpl.Prod(c_chp_heatdump, x_chp_s_op_dhw[t]));
                cpl.AddLe(x_chp_m_dump_dhw[t], cpl.Prod(c_chp_heatdump, x_chp_m_op_dhw[t]));
                cpl.AddLe(x_chp_l_dump_dhw[t], cpl.Prod(c_chp_heatdump, x_chp_l_op_dhw[t]));
                cpl.AddLe(x_chp_s_dump_sh[t], cpl.Prod(c_chp_heatdump, x_chp_s_op_sh[t]));
                cpl.AddLe(x_chp_m_dump_sh[t], cpl.Prod(c_chp_heatdump, x_chp_m_op_sh[t]));
                cpl.AddLe(x_chp_l_dump_sh[t], cpl.Prod(c_chp_heatdump, x_chp_l_op_sh[t]));

                // Boiler 
                cpl.AddLe(cpl.Sum(x_boi_s_op_dhw[t], x_boi_s_op_sh[t]), x_boi_s);
                cpl.AddLe(cpl.Sum(x_boi_m_op_dhw[t], x_boi_m_op_sh[t]), x_boi_m);
                cpl.AddLe(cpl.Sum(x_boi_l_op_dhw[t], x_boi_l_op_sh[t]), x_boi_l);

                // HP 
                cpl.AddLe(cpl.Sum(x_hp_s_op_dhw[t], x_hp_s_op_sh[t]), x_hp_s);
                cpl.AddLe(cpl.Sum(x_hp_m_op_dhw[t], x_hp_m_op_sh[t]), x_hp_m);
                cpl.AddLe(cpl.Sum(x_hp_l_op_dhw[t], x_hp_l_op_sh[t]), x_hp_l);
            }

            // TES
            for (int t = 0; t < this.horizon - 1; t++)
            {
                ILinearNumExpr tesstate = cpl.LinearNumExpr();
                tesstate.AddTerm((1 - tes_decay), x_tes_soc[t]);
                tesstate.AddTerm(tes_ch_eff, x_tes_ch_sh[t]);
                tesstate.AddTerm(tes_ch_eff, x_tes_ch_dhw[t]);
                tesstate.AddTerm(-1 / tes_disch_eff, x_tes_dis_dhw[t]);
                tesstate.AddTerm(-1 / tes_disch_eff, x_tes_dis_sh[t]);
                cpl.AddEq(x_tes_soc[t + 1], tesstate);
            }
            cpl.AddEq(x_tes_soc[0], x_tes_soc[this.horizon - 1]);
            cpl.AddEq(x_tes_dis_dhw[0], 0);
            cpl.AddEq(x_tes_dis_sh[0], 0);
            for (int t = 0; t < this.horizon; t++)
            {
                cpl.AddLe(cpl.Sum(x_tes_ch_dhw[t], x_tes_ch_sh[t]), cpl.Prod(x_tes, tes_max_ch));
                cpl.AddLe(cpl.Sum(x_tes_dis_dhw[t], x_tes_dis_sh[t]), cpl.Prod(x_tes, tes_max_disch));
                cpl.AddLe(x_tes_soc[t], x_tes);
            }


            // Battery model
            for (int t = 0; t < this.horizon - 1; t++)
            {
                ILinearNumExpr batstate = cpl.LinearNumExpr();          // losses when charging, discharging, and decay
                batstate.AddTerm((1 - bat_decay), x_bat_soc[t]);
                batstate.AddTerm(bat_ch_eff, x_bat_ch[t]);
                batstate.AddTerm(-1 / bat_disch_eff, x_bat_dis[t]);
                cpl.AddEq(x_bat_soc[t + 1], batstate);
            }
            cpl.AddGe(x_bat_soc[0], cpl.Prod(x_bat, bat_min_state));    // initial state of battery >= min_state
            cpl.AddEq(x_bat_soc[0], cpl.Diff(x_bat_soc[this.horizon - 1], x_bat_dis[this.horizon - 1])); //initial state also = state(end of year)
            cpl.AddEq(x_bat_dis[0], 0);                                  // initial discharging of battery

            for (int t = 0; t < this.horizon; t++)
            {
                cpl.AddGe(x_bat_soc[t], cpl.Prod(x_bat, bat_min_state));    // min state of charge
                cpl.AddLe(x_bat_ch[t], cpl.Prod(x_bat, bat_max_ch));         // battery charging
                cpl.AddLe(x_bat_dis[t], cpl.Prod(x_bat, bat_max_disch));     // battery discharging
                cpl.AddLe(x_bat_soc[t], x_bat);                             // battery sizing
            }






            //fix cost indicator

            // ST and PV
            ILinearNumExpr st_totcap = cpl.LinearNumExpr();
            ILinearNumExpr pv_totcap = cpl.LinearNumExpr();
            for (int i = 0; i < solarspots; i++)
            {
                st_totcap.AddTerm(1, x_st[i]);
                pv_totcap.AddTerm(1, x_pv[i]);
            }
            cpl.AddLe(st_totcap, cpl.Prod(M, y_st_fc)); //fixcost indicator
            cpl.AddLe(pv_totcap, cpl.Prod(M, y_pv_fc));

            //CHP
            cpl.AddLe(x_chp_s, cpl.Prod(M, y_chp_s_fc));
            cpl.AddLe(x_chp_m, cpl.Prod(M, y_chp_m_fc));
            cpl.AddLe(x_chp_l, cpl.Prod(M, y_chp_l_fc));

            //Boi
            cpl.AddLe(x_boi_s, cpl.Prod(M, y_boi_s_fc));
            cpl.AddLe(x_boi_m, cpl.Prod(M, y_boi_m_fc));
            cpl.AddLe(x_boi_l, cpl.Prod(M, y_boi_l_fc));

            //HP
            cpl.AddLe(x_hp_s, cpl.Prod(M, y_hp_s_fc));
            cpl.AddLe(x_hp_m, cpl.Prod(M, y_hp_m_fc));
            cpl.AddLe(x_hp_l, cpl.Prod(M, y_hp_l_fc));

            //TES
            cpl.AddLe(x_tes, cpl.Prod(M, y_tes_fc));

            //BAt
            cpl.AddLe(x_bat, cpl.Prod(M, y_bat_fc));

            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // co2 constraint
            ILinearNumExpr a_co2 = cpl.LinearNumExpr();
            for (int t = 0; t < this.horizon; t++)
            {
                // co2 emissions by purchase from grid and heat pump
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t], x_purchase[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_dhw[t]), x_hp_s_op_dhw[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_dhw[t]), x_hp_m_op_dhw[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_dhw[t]), x_hp_l_op_dhw[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_sh[t]), x_hp_s_op_sh[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_sh[t]), x_hp_m_op_sh[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_sh[t]), x_hp_l_op_sh[t]);
                // co2 by natural gas
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_s_op_dhw[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_m_op_dhw[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_l_op_dhw[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_s_op_sh[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_m_op_sh[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_l_op_sh[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_chp_s_eff), x_chp_s_op_e[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_chp_m_eff), x_chp_m_op_e[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_chp_l_eff), x_chp_l_op_e[t]);
            }

            //for (int i = 0; i < pvspots - 6; i++)
            //    a_co2.AddTerm(lca_pv_facade, x_pv[i]);
            for (int i = 0; i < solarspots; i++)
                a_co2.AddTerm(lca_pv, x_pv[i]);

            a_co2.AddTerm(lca_boiler, x_boi_s);
            a_co2.AddTerm(lca_hp, x_hp_s);
            a_co2.AddTerm(lca_chp, x_chp_s);
            a_co2.AddTerm(lca_therm, x_tes);
            a_co2.AddTerm(lca_battery, x_bat);


            if (bln_co2target && !bln_mincarbon) cpl.AddLe(cpl.Sum(lca_ac * x_AC, a_co2), b_co2_target);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // PROBLEM OBJECTIVE FUNCTION
            ILinearNumExpr OPEX = cpl.LinearNumExpr();
            ILinearNumExpr CAPEX = cpl.LinearNumExpr();
            for (int i = 0; i < solarspots; i++)
            {
                CAPEX.AddTerm(c_pv, x_pv[i]);
                CAPEX.AddTerm(c_st, x_st[i]);
            }
            CAPEX.AddTerm(c_hp_s, x_hp_s);
            CAPEX.AddTerm(c_hp_m, x_hp_m);
            CAPEX.AddTerm(c_hp_l, x_hp_l);
            CAPEX.AddTerm(c_boi_s, x_boi_s);
            CAPEX.AddTerm(c_boi_m, x_boi_m);
            CAPEX.AddTerm(c_boi_l, x_boi_l);
            CAPEX.AddTerm(c_chp_s, x_chp_s);
            CAPEX.AddTerm(c_chp_m, x_chp_m);
            CAPEX.AddTerm(c_chp_l, x_chp_l);
            CAPEX.AddTerm(c_tes, x_tes);
            CAPEX.AddTerm(c_bat, x_bat);

            CAPEX.AddTerm(fc_pv, y_pv_fc);
            CAPEX.AddTerm(fc_st, y_st_fc);
            CAPEX.AddTerm(fc_boi_s, y_boi_s_fc);
            CAPEX.AddTerm(fc_boi_m, y_boi_m_fc);
            CAPEX.AddTerm(fc_boi_l, y_boi_l_fc);
            CAPEX.AddTerm(fc_hp_s, y_hp_s_fc);
            CAPEX.AddTerm(fc_hp_m, y_hp_m_fc);
            CAPEX.AddTerm(fc_hp_l, y_hp_l_fc);
            CAPEX.AddTerm(fc_chp_s, y_chp_s_fc);
            CAPEX.AddTerm(fc_chp_m, y_chp_m_fc);
            CAPEX.AddTerm(fc_chp_l, y_chp_l_fc);
            CAPEX.AddTerm(fc_bat, y_bat_fc);
            CAPEX.AddTerm(fc_tes, y_tes_fc);


            for (int t = 0; t < this.horizon; t++)
            {
                OPEX.AddTerm(this.c_grid[t] * this.c_num_of_days[t], x_purchase[t]);
                OPEX.AddTerm(-1.0 * this.c_feedin[t] * this.c_num_of_days[t], x_feedin[t]);

                for (int i = 0; i < solarspots; i++)
                {
                    OPEX.AddTerm((c_pv_om * this.a_solar[i][t] * 0.001 * this.c_pv_eff[i][t] * this.c_num_of_days[t]), x_pv[i]);
                    OPEX.AddTerm((c_st_om * this.c_num_of_days[t]), x_st_op_dhw[i][t]);
                    OPEX.AddTerm((c_st_om * this.c_num_of_days[t]), x_st_op_sh[i][t]);
                }

                OPEX.AddTerm((c_gas / c_chp_s_eff) * this.c_num_of_days[t], x_chp_s_op_e[t]);
                OPEX.AddTerm((c_gas / c_chp_m_eff) * this.c_num_of_days[t], x_chp_m_op_e[t]);
                OPEX.AddTerm((c_gas / c_chp_l_eff) * this.c_num_of_days[t], x_chp_l_op_e[t]);
                OPEX.AddTerm(c_chp_om * this.c_num_of_days[t], x_chp_s_op_e[t]);
                OPEX.AddTerm(c_chp_om * this.c_num_of_days[t], x_chp_m_op_e[t]);
                OPEX.AddTerm(c_chp_om * this.c_num_of_days[t], x_chp_l_op_e[t]);

                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_s_op_dhw[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_m_op_dhw[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_l_op_dhw[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_s_op_sh[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_m_op_sh[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_l_op_sh[t]);

                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_s_op_dhw[t]);
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_m_op_dhw[t]);
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_l_op_dhw[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_s_op_dhw[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_m_op_dhw[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_l_op_dhw[t]);
                OPEX.AddTerm(c_tes_om * this.c_num_of_days[t], x_tes_ch_dhw[t]);
                OPEX.AddTerm(c_tes_om * this.c_num_of_days[t], x_tes_dis_dhw[t]);

                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_s_op_sh[t]);
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_m_op_sh[t]);
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_l_op_sh[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_s_op_sh[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_m_op_sh[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_l_op_sh[t]);
                OPEX.AddTerm(c_tes_om * this.c_num_of_days[t], x_tes_ch_sh[t]);
                OPEX.AddTerm(c_tes_om * this.c_num_of_days[t], x_tes_dis_sh[t]);

                OPEX.AddTerm(c_bat_om * this.c_num_of_days[t], x_bat_ch[t]);
                OPEX.AddTerm(c_bat_om * this.c_num_of_days[t], x_bat_dis[t]);
            }
            // cost minimization
            if (!bln_mincarbon) cpl.AddMinimize(cpl.Sum(OPEX, cpl.Sum(CAPEX, c_ac * x_AC + y_ac * fc_ac)));
            // co2 minimization
            if (bln_mincarbon) cpl.AddMinimize(a_co2);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // SOLVE and CPLEX SETTINGS
            cpl.SetParam(Cplex.Param.ClockType, 2);     // 2 = measuring time in wall clock time. 1 = cpu time
            cpl.SetParam(Cplex.Param.TimeLimit, 60);
            //cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.05);
            cpl.SetOut(null);
            cpl.Solve();

            //Console.WriteLine("mip gap: {0}", cpl.GetMIPRelativeGap());
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // OUTPUTS
            //for (int i = 0; i < pvspots; i++)
            //    Console.WriteLine("pv spot {0}, area installed: {1}", i, cpl.GetValue(x_pv[i]));


            //if (bln_mincarbon)
            //{
            //    Console.WriteLine("cost: " + cpl.GetValue(cpl.Sum(OPEX, CAPEX)));
            //}
            //else
            //    Console.WriteLine("cost: " + cpl.ObjValue);

            //Console.WriteLine("OPEX: {0}... CAPEX: {1}", cpl.GetValue(OPEX), cpl.GetValue(CAPEX));

            //Console.WriteLine("total co2 emissions: {0}", cpl.GetValue(a_co2));
            //Console.WriteLine("tot pur: {0}, tot sold: {1}", cpl.GetValue(cpl.Sum(x_purchase)), cpl.GetValue(cpl.Sum(x_feedin)));
            //Console.ReadKey();

            if (bln_mincarbon)
            {
                outs.cost = cpl.GetValue(cpl.Sum(OPEX, CAPEX));
                outs.carbon = cpl.ObjValue;
            }
            else
            {
                outs.cost = cpl.ObjValue;
                outs.carbon = cpl.GetValue(a_co2);
            }

            outs.x_elecpur = new double[this.horizon];
            outs.x_feedin = new double[this.horizon];
            outs.b_pvprod = new double[this.horizon];


            outs.x_batcharge = new double[this.horizon];
            outs.x_batdischarge = new double[this.horizon];
            outs.x_batsoc = new double[this.horizon];
            outs.x_tescharge_dhw = new double[this.horizon];
            outs.x_tesdischarge_dhw = new double[this.horizon];
            outs.x_tessoc = new double[this.horizon];
            outs.x_hp_s_op_dhw = new double[this.horizon];
            outs.x_boi_s_op_dhw = new double[this.horizon];
            outs.x_chp_s_op_e = new double[this.horizon];
            outs.x_chp_s_op_dhw = new double[this.horizon];
            outs.x_chp_s_dump_dhw = new double[this.horizon];

            outs.x_pv = new double[this.b_solar_area.Length];
            for (int t = 0; t < this.horizon; t++)
            {
                outs.x_elecpur[t] = cpl.GetValue(x_purchase[t]);
                outs.x_feedin[t] = cpl.GetValue(x_feedin[t]);
                outs.x_batcharge[t] = cpl.GetValue(x_bat_ch[t]);
                outs.x_batdischarge[t] = cpl.GetValue(x_bat_dis[t]);
                outs.x_batsoc[t] = cpl.GetValue(x_bat_soc[t]);
                outs.x_tescharge_dhw[t] = cpl.GetValue(x_tes_ch_dhw[t]);
                outs.x_tesdischarge_dhw[t] = cpl.GetValue(x_tes_dis_dhw[t]);
                outs.x_tessoc[t] = cpl.GetValue(x_tes_soc[t]);
                outs.x_hp_s_op_dhw[t] = cpl.GetValue(x_hp_s_op_dhw[t]);
                outs.x_boi_s_op_dhw[t] = cpl.GetValue(x_boi_s_op_dhw[t]);
                outs.x_chp_s_op_e[t] = cpl.GetValue(x_chp_s_op_e[t]);
                outs.x_chp_s_op_dhw[t] = cpl.GetValue(x_chp_s_op_dhw[t]);
                outs.x_chp_s_dump_dhw[t] = cpl.GetValue(x_chp_s_dump_dhw[t]);

                outs.b_pvprod[t] = 0;
                for (int i = 0; i < this.b_solar_area.Length; i++)
                {
                    double pvprodnow = this.a_solar[i][t] * 0.001 * this.c_pv_eff[i][t] * cpl.GetValue(x_pv[i]);
                    outs.b_pvprod[t] += pvprodnow;
                }
            }
            for (int i = 0; i < this.b_solar_area.Length; i++)
                outs.x_pv[i] = cpl.GetValue(x_pv[i]);

            outs.x_hp_s = cpl.GetValue(x_hp_s);
            outs.x_boi_s = cpl.GetValue(x_boi_s);
            outs.x_chp_s = cpl.GetValue(x_chp_s);
            outs.x_ac = x_AC;
            outs.x_tes = cpl.GetValue(x_tes);
            outs.x_bat = cpl.GetValue(x_bat);


            outs.OPEX = cpl.GetValue(OPEX);
            outs.CAPEX = cpl.GetValue(CAPEX);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////




            return outs;
        }

        /// <summary>
        /// ehub with domestic hot water and space heating separate.
        /// </summary>
        /// <param name="bln_mincarbon"></param>
        /// <param name="carbonconstraint"></param>
        /// <returns></returns>
        private Ehub_outputs ehubmodel_dhw(bool bln_mincarbon, double? carbonconstraint = null)
        {

            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // Initialize solver and other
            Cplex cpl = new Cplex();
            Ehub_outputs outs = new Ehub_outputs();



            int solarspots = this.b_solar_area.Length;   // number of different PV patches



            double b_co2_target = double.MaxValue;
            bool bln_co2target = false;


            if (!carbonconstraint.IsNullOrDefault())
            {
                b_co2_target = Convert.ToDouble(carbonconstraint);
                bln_co2target = true;
            }


            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // VARIABLES
            //int techs = 11;      // 0=HP_s, 1=HP_m, 2=HP_l, 3=Boi_s, 4=Boi_m, 5=Boi_l, 6=CHP_s, 7=CHP_m, 8=CHP_l. 9=PV, 10=ST, TES and Bat are separate
            //int demands = 3;    // 0 = elec, 1 = sh, 2 = dhw 



            //AC - not variable, since only one cooling tech
            double x_AC = this.b_peakcool;                                  // sizing in kW. not a variable
            double[] x_ac_op = new double[this.horizon];                  // operation in kWh. not variable.
            double y_ac = (x_AC > 0) ? 1 : 0;

            // PV
            INumVar[] x_pv = new INumVar[solarspots];                          // m2 installed per patch (pvspots)
            for (int i = 0; i < solarspots; i++)
                x_pv[i] = cpl.NumVar(0, this.b_solar_area[i]);              // max available patch area in m2
            INumVar[] y1 = new INumVar[this.horizon];                       // binary control variable. to avoid selling and purchasing from and to the grid at the same time. from portia. 1 = electricity is being purchased
            INumVar[] x_purchase = new INumVar[this.horizon];               // purchase electricity from grid
            INumVar[] x_feedin = new INumVar[this.horizon];                 // feeding in PV electricity into the grid
            INumVar y_pv = cpl.BoolVar();

            // ST
            INumVar[] x_st = new INumVar[solarspots];
            // variables for operating solar collector? or just like pv, no explicit operation variable
            INumVar[][] x_st_op_sh = new INumVar[solarspots][];
            INumVar[][] x_st_op_dhw = new INumVar[solarspots][];
            for (int i = 0; i < solarspots; i++)
            {
                x_st[i] = cpl.NumVar(0, this.b_solar_area[i]);
                x_st_op_sh[i] = new INumVar[this.horizon];
                x_st_op_dhw[i] = new INumVar[this.horizon];
            }
            INumVar y_st = cpl.BoolVar();

            // HP
            INumVar x_hp_s = cpl.NumVar(0, 50);                           // capacity, in kW
            INumVar[] x_hp_s_op_sh = new INumVar[this.horizon];             // operation, in kWh, space heating
            INumVar[] x_hp_s_op_dhw = new INumVar[this.horizon];            // operation, in kWh, domestic hot water
            INumVar x_hp_m = cpl.NumVar(0, 200);                           // capacity, in kW
            INumVar[] x_hp_m_op_sh = new INumVar[this.horizon];                // operation, in kWh
            INumVar[] x_hp_m_op_dhw = new INumVar[this.horizon];                // operation, in kWh
            INumVar x_hp_l = cpl.NumVar(0, System.Double.MaxValue);       // capacity, in kW
            INumVar[] x_hp_l_op_sh = new INumVar[this.horizon];                // operation, in kWh
            INumVar[] x_hp_l_op_dhw = new INumVar[this.horizon];                // operation, in kWh
            INumVar y_hp_s = cpl.BoolVar();
            INumVar y_hp_m = cpl.BoolVar();
            INumVar y_hp_l = cpl.BoolVar();

            // Boiler
            INumVar x_boi_s = cpl.NumVar(0, 50);                          // capacity, in kW
            INumVar[] x_boi_s_op_sh = new INumVar[this.horizon];               // operation, in kWh
            INumVar[] x_boi_s_op_dhw = new INumVar[this.horizon];               // operation, in kWh
            INumVar x_boi_m = cpl.NumVar(0, 200);                          // capacity, in kW
            INumVar[] x_boi_m_op_sh = new INumVar[this.horizon];               // operation, in kWh
            INumVar[] x_boi_m_op_dhw = new INumVar[this.horizon];               // operation, in kWh
            INumVar x_boi_l = cpl.NumVar(0, System.Double.MaxValue);      // capacity, in kW
            INumVar[] x_boi_l_op_sh = new INumVar[this.horizon];               // operation, in kWh
            INumVar[] x_boi_l_op_dhw = new INumVar[this.horizon];               // operation, in kWh
            INumVar y_boi_s = cpl.BoolVar();
            INumVar y_boi_m = cpl.BoolVar();
            INumVar y_boi_l = cpl.BoolVar();

            // CHP
            INumVar x_chp_s = cpl.NumVar(0, 50);                          // capacity, in kW
            //INumVar[] y_chp_op_s = new INumVar[this.horizon];              //binary for min part load
            INumVar[] x_chp_s_op_e = new INumVar[this.horizon];               // operation electricity, in kWh
            INumVar[] x_chp_s_op_dhw = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_s_dump_dhw = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar[] x_chp_s_op_sh = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_s_dump_sh = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar x_chp_m = cpl.NumVar(0, 200);                          // capacity, in kW
            //INumVar[] y_chp_op_m = new INumVar[this.horizon];              //binary for min part load
            INumVar[] x_chp_m_op_e = new INumVar[this.horizon];               // operation electricity, in kWh
            INumVar[] x_chp_m_op_dhw = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_m_dump_dhw = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar[] x_chp_m_op_sh = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_m_dump_sh = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar x_chp_l = cpl.NumVar(0, System.Double.MaxValue);      // capacity, in kW
            //INumVar[] y_chp_op_l = new INumVar[this.horizon];              //binary for min part load
            INumVar[] x_chp_l_op_e = new INumVar[this.horizon];               // operation electricity, in kWh
            INumVar[] x_chp_l_op_dhw = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_l_dump_dhw = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar[] x_chp_l_op_sh = new INumVar[this.horizon];               // operation heat, in kWh
            INumVar[] x_chp_l_dump_sh = new INumVar[this.horizon];               // dumping heat, in kWh
            INumVar y_chp_s = cpl.BoolVar();
            INumVar y_chp_m = cpl.BoolVar();
            INumVar y_chp_l = cpl.BoolVar();

            // Thermal storage
            INumVar x_tes = cpl.NumVar(0, this.maxTEScap);               // 40 cubic meter size. thats like 4m x 5m x 2m. (x m3 * 35)
            INumVar[] x_tes_soc = new INumVar[this.horizon];              // state-of-charge, in kWh
            INumVar[] x_tes_ch_sh = new INumVar[this.horizon];               // charging storage, in kW
            INumVar[] x_tes_dis_sh = new INumVar[this.horizon];              // discharging storage, in kW
            INumVar[] x_tes_ch_dhw = new INumVar[this.horizon];               // charging storage, in kW
            INumVar[] x_tes_dis_dhw = new INumVar[this.horizon];              // discharging storage, in kW
            INumVar y_tes = cpl.BoolVar();

            // Battery
            INumVar x_bat = cpl.NumVar(0, this.maxBatcap);                  // defined by urban form
            INumVar[] x_bat_ch = new INumVar[this.horizon];                 // charge battery [kW]
            INumVar[] x_bat_dis = new INumVar[this.horizon];                // discharge battery [kW]
            INumVar[] x_bat_soc = new INumVar[this.horizon];                // state-of-charge, stored electricity [kW]
            INumVar y_bat = cpl.BoolVar();

            for (int t = 0; t < this.horizon; t++)
            {
                y1[t] = cpl.BoolVar();
                x_purchase[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_feedin[t] = cpl.NumVar(0, System.Double.MaxValue);

                //x_ac_op[t] = cpl.NumVar(0, x_AC);

                for (int i = 0; i < solarspots; i++)
                {
                    x_st_op_dhw[i][t] = cpl.NumVar(0, System.Double.MaxValue);
                    x_st_op_sh[i][t] = cpl.NumVar(0, System.Double.MaxValue);
                }

                x_chp_s_op_e[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_op_e[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_op_e[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_s_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_s_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_s_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_s_dump_sh[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_m_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_m_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_dump_sh[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_l_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_l_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_op_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_dump_sh[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_tes_ch_sh[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_tes_dis_sh[t] = cpl.NumVar(0, System.Double.MaxValue);


                x_hp_s_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_s_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_s_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_s_dump_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_m_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_m_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_m_dump_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_hp_l_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_boi_l_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_op_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_chp_l_dump_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_tes_ch_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_tes_dis_dhw[t] = cpl.NumVar(0, System.Double.MaxValue);


                x_bat_ch[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_bat_dis[t] = cpl.NumVar(0, System.Double.MaxValue);

                x_tes_soc[t] = cpl.NumVar(0, System.Double.MaxValue);
                x_bat_soc[t] = cpl.NumVar(0, System.Double.MaxValue);
            }
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////





            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // CONSTRAINTS

            //constraint, saying x_st[spot] + x_pv[spot] <= area[spot]
            for (int i = 0; i < solarspots; i++)
            {
                cpl.AddGe(b_solar_area[i], cpl.Sum(x_st[i], x_pv[i]));
            }



            // meetings demands
            for (int t = 0; t < this.horizon; t++)
            {
                //cooling is not a variable, since only one tech
                x_ac_op[t] = this.b_cool[t];    //op cost and co2 is taken care of via electricity


                // electricity demand must be met by pv, grid, battery, and chp
                //      minus electricty sold to the grid, charging the battery, and for operating the heat pump
                ILinearNumExpr pvproduction = cpl.LinearNumExpr();
                ILinearNumExpr elecgeneration = cpl.LinearNumExpr();
                ILinearNumExpr elecadditionaldemand = cpl.LinearNumExpr();
                for (int i = 0; i < solarspots; i++)
                {
                    elecgeneration.AddTerm(this.a_solar[i][t] * 0.001 * this.c_pv_eff[i][t], x_pv[i]);
                    pvproduction.AddTerm(this.a_solar[i][t] * 0.001 * this.c_pv_eff[i][t], x_pv[i]);     // in kW
                }
                elecgeneration.AddTerm(1, x_purchase[t]);
                elecgeneration.AddTerm(1, x_bat_dis[t]);
                elecgeneration.AddTerm(c_chp_s_htp, x_chp_s_op_e[t]);
                elecgeneration.AddTerm(c_chp_m_htp, x_chp_m_op_e[t]);
                elecgeneration.AddTerm(c_chp_l_htp, x_chp_l_op_e[t]);
                elecadditionaldemand.AddTerm(1, x_feedin[t]);
                elecadditionaldemand.AddTerm(1, x_bat_ch[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_sh[t], x_hp_s_op_sh[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_sh[t], x_hp_m_op_sh[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_sh[t], x_hp_l_op_sh[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_dhw[t], x_hp_s_op_dhw[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_dhw[t], x_hp_m_op_dhw[t]);
                elecadditionaldemand.AddTerm(1 / c_hp_eff_dhw[t], x_hp_l_op_dhw[t]);
                cpl.AddEq(cpl.Diff(elecgeneration, cpl.Sum(c_ac_eff[t] * this.b_cool[t], elecadditionaldemand)), this.b_elec[t]);


                // space heating must be met by boiler, hp, chp, st, and TES
                //      minus heat for TES 
                ILinearNumExpr shgeneration = cpl.LinearNumExpr();
                ILinearNumExpr shadditionaldemand = cpl.LinearNumExpr();
                shadditionaldemand.AddTerm(1, x_tes_ch_sh[t]);
                shadditionaldemand.AddTerm(1, x_chp_s_dump_sh[t]);
                shadditionaldemand.AddTerm(1, x_chp_m_dump_sh[t]);
                shadditionaldemand.AddTerm(1, x_chp_l_dump_sh[t]);
                shgeneration.AddTerm(1, x_tes_dis_sh[t]);
                shgeneration.AddTerm(1, x_boi_s_op_sh[t]);
                shgeneration.AddTerm(1, x_hp_s_op_sh[t]);
                shgeneration.AddTerm(1, x_chp_s_op_sh[t]);
                shgeneration.AddTerm(1, x_boi_m_op_sh[t]);
                shgeneration.AddTerm(1, x_hp_m_op_sh[t]);
                shgeneration.AddTerm(1, x_chp_m_op_sh[t]);
                shgeneration.AddTerm(1, x_boi_l_op_sh[t]);
                shgeneration.AddTerm(1, x_hp_l_op_sh[t]);
                shgeneration.AddTerm(1, x_chp_l_op_sh[t]);
                for (int i = 0; i < solarspots; i++)
                {
                    shgeneration.AddTerm(1, x_st_op_sh[i][t]);
                }
                cpl.AddEq(cpl.Diff(shgeneration, shadditionaldemand), this.b_sh[t]);


                // domestic hot water must be met by boiler, hp, chp, st, and TES
                //      minus heat for TES 
                ILinearNumExpr dhwgeneration = cpl.LinearNumExpr();
                ILinearNumExpr dhwadditionaldemand = cpl.LinearNumExpr();
                dhwadditionaldemand.AddTerm(1, x_tes_ch_dhw[t]);
                dhwadditionaldemand.AddTerm(1, x_chp_s_dump_dhw[t]);
                dhwadditionaldemand.AddTerm(1, x_chp_m_dump_dhw[t]);
                dhwadditionaldemand.AddTerm(1, x_chp_l_dump_dhw[t]);
                dhwgeneration.AddTerm(1, x_tes_dis_dhw[t]);
                dhwgeneration.AddTerm(1, x_boi_s_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_hp_s_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_chp_s_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_boi_m_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_hp_m_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_chp_m_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_boi_l_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_hp_l_op_dhw[t]);
                dhwgeneration.AddTerm(1, x_chp_l_op_dhw[t]);
                for (int i = 0; i < solarspots; i++)
                {
                    dhwgeneration.AddTerm(1, x_st_op_dhw[i][t]);
                }
                cpl.AddEq(cpl.Diff(dhwgeneration, dhwadditionaldemand), this.b_dhw[t]);

                //pv production must be greater or equal feedin
                cpl.AddGe(pvproduction, x_feedin[t]);

                // donnot allow feedin and purchase at the same time. y = 1 means electricity is being purchased
                cpl.AddLe(x_purchase[t], cpl.Prod(M, y1[t]));
                cpl.AddLe(x_feedin[t], cpl.Prod(M, cpl.Diff(1, y1[t])));

            }



            for (int t = 0; t < this.horizon; t++)
            {
                // ST
                for (int i = 0; i < solarspots; i++)
                {
                    ILinearNumExpr stutil = cpl.LinearNumExpr();
                    stutil.AddTerm(Math.Min(System.Double.MaxValue, Math.Pow(this.c_st_eff_dhw[i][t], -1)), x_st_op_dhw[i][t]);
                    stutil.AddTerm(Math.Min(System.Double.MaxValue, Math.Pow(this.c_st_eff_sh[i][t], -1)), x_st_op_sh[i][t]);
                    cpl.AddGe(cpl.Prod(this.a_solar[i][t] * 0.001, x_st[i]), stutil);
                }



                // CHP 
                cpl.AddLe(x_chp_s_op_e[t], x_chp_s);
                cpl.AddLe(x_chp_m_op_e[t], x_chp_m);
                cpl.AddLe(x_chp_l_op_e[t], x_chp_l);
                // heat recovery (SH + DHW) and heat dump from CHP is equal to electricity generation by CHP times heat to power ratio
                ILinearNumExpr chpheatrecov_s = cpl.LinearNumExpr();
                ILinearNumExpr chpheatfromelec_s = cpl.LinearNumExpr();
                chpheatrecov_s.AddTerm(1, x_chp_s_op_dhw[t]);
                chpheatrecov_s.AddTerm(1, x_chp_s_dump_dhw[t]);
                chpheatrecov_s.AddTerm(1, x_chp_s_op_sh[t]);
                chpheatrecov_s.AddTerm(1, x_chp_s_dump_sh[t]);
                chpheatfromelec_s.AddTerm(c_chp_s_htp, x_chp_s_op_e[t]);
                cpl.AddEq(chpheatrecov_s, chpheatfromelec_s);
                ILinearNumExpr chpheatrecov_m = cpl.LinearNumExpr();
                ILinearNumExpr chpheatfromelec_m = cpl.LinearNumExpr();
                chpheatrecov_m.AddTerm(1, x_chp_m_op_dhw[t]);
                chpheatrecov_m.AddTerm(1, x_chp_m_dump_dhw[t]);
                chpheatrecov_m.AddTerm(1, x_chp_m_op_sh[t]);
                chpheatrecov_m.AddTerm(1, x_chp_m_dump_sh[t]);
                chpheatfromelec_m.AddTerm(c_chp_m_htp, x_chp_m_op_e[t]);
                cpl.AddEq(chpheatrecov_m, chpheatfromelec_m);
                ILinearNumExpr chpheatrecov_l = cpl.LinearNumExpr();
                ILinearNumExpr chpheatfromelec_l = cpl.LinearNumExpr();
                chpheatrecov_l.AddTerm(1, x_chp_l_op_dhw[t]);
                chpheatrecov_l.AddTerm(1, x_chp_l_dump_dhw[t]);
                chpheatrecov_l.AddTerm(1, x_chp_l_op_sh[t]);
                chpheatrecov_l.AddTerm(1, x_chp_l_dump_sh[t]);
                chpheatfromelec_l.AddTerm(c_chp_l_htp, x_chp_l_op_e[t]);
                cpl.AddEq(chpheatrecov_l, chpheatfromelec_l);
                // Limiting the amount of heat that chps can dump
                cpl.AddLe(x_chp_s_dump_dhw[t], cpl.Prod(c_chp_heatdump, x_chp_s_op_dhw[t]));
                cpl.AddLe(x_chp_m_dump_dhw[t], cpl.Prod(c_chp_heatdump, x_chp_m_op_dhw[t]));
                cpl.AddLe(x_chp_l_dump_dhw[t], cpl.Prod(c_chp_heatdump, x_chp_l_op_dhw[t]));
                cpl.AddLe(x_chp_s_dump_sh[t], cpl.Prod(c_chp_heatdump, x_chp_s_op_sh[t]));
                cpl.AddLe(x_chp_m_dump_sh[t], cpl.Prod(c_chp_heatdump, x_chp_m_op_sh[t]));
                cpl.AddLe(x_chp_l_dump_sh[t], cpl.Prod(c_chp_heatdump, x_chp_l_op_sh[t]));

                // Boiler 
                cpl.AddLe(cpl.Sum(x_boi_s_op_dhw[t], x_boi_s_op_sh[t]), x_boi_s);
                cpl.AddLe(cpl.Sum(x_boi_m_op_dhw[t], x_boi_m_op_sh[t]), x_boi_m);
                cpl.AddLe(cpl.Sum(x_boi_l_op_dhw[t], x_boi_l_op_sh[t]), x_boi_l);

                // HP 
                cpl.AddLe(cpl.Sum(x_hp_s_op_dhw[t], x_hp_s_op_sh[t]), x_hp_s);
                cpl.AddLe(cpl.Sum(x_hp_m_op_dhw[t], x_hp_m_op_sh[t]), x_hp_m);
                cpl.AddLe(cpl.Sum(x_hp_l_op_dhw[t], x_hp_l_op_sh[t]), x_hp_l);
            }

            // TES
            for (int t = 0; t < this.horizon - 1; t++)
            {
                ILinearNumExpr tesstate = cpl.LinearNumExpr();
                tesstate.AddTerm((1 - tes_decay), x_tes_soc[t]);
                tesstate.AddTerm(tes_ch_eff, x_tes_ch_sh[t]);
                tesstate.AddTerm(tes_ch_eff, x_tes_ch_dhw[t]);
                tesstate.AddTerm(-1 / tes_disch_eff, x_tes_dis_dhw[t]);
                tesstate.AddTerm(-1 / tes_disch_eff, x_tes_dis_sh[t]);
                cpl.AddEq(x_tes_soc[t + 1], tesstate);
            }
            cpl.AddEq(x_tes_soc[0], x_tes_soc[this.horizon - 1]);
            cpl.AddEq(x_tes_dis_dhw[0], 0);
            cpl.AddEq(x_tes_dis_sh[0], 0);
            for (int t = 0; t < this.horizon; t++)
            {
                cpl.AddLe(cpl.Sum(x_tes_ch_dhw[t], x_tes_ch_sh[t]), cpl.Prod(x_tes, tes_max_ch));
                cpl.AddLe(cpl.Sum(x_tes_dis_dhw[t], x_tes_dis_sh[t]), cpl.Prod(x_tes, tes_max_disch));
                cpl.AddLe(x_tes_soc[t], x_tes);
            }


            // Battery model
            for (int t = 0; t < this.horizon - 1; t++)
            {
                ILinearNumExpr batstate = cpl.LinearNumExpr();          // losses when charging, discharging, and decay
                batstate.AddTerm((1 - bat_decay), x_bat_soc[t]);
                batstate.AddTerm(bat_ch_eff, x_bat_ch[t]);
                batstate.AddTerm(-1 / bat_disch_eff, x_bat_dis[t]);
                cpl.AddEq(x_bat_soc[t + 1], batstate);
            }
            cpl.AddGe(x_bat_soc[0], cpl.Prod(x_bat, bat_min_state));    // initial state of battery >= min_state
            cpl.AddEq(x_bat_soc[0], cpl.Diff(x_bat_soc[this.horizon - 1], x_bat_dis[this.horizon - 1])); //initial state also = state(end of year)
            cpl.AddEq(x_bat_dis[0], 0);                                  // initial discharging of battery

            for (int t = 0; t < this.horizon; t++)
            {
                cpl.AddGe(x_bat_soc[t], cpl.Prod(x_bat, bat_min_state));    // min state of charge
                cpl.AddLe(x_bat_ch[t], cpl.Prod(x_bat, bat_max_ch));         // battery charging
                cpl.AddLe(x_bat_dis[t], cpl.Prod(x_bat, bat_max_disch));     // battery discharging
                cpl.AddLe(x_bat_soc[t], x_bat);                             // battery sizing
            }



            //min sizing indicator
            cpl.AddGe(x_boi_s, cpl.Prod(5, y_boi_s));
            cpl.AddGe(x_boi_m, cpl.Prod(50, y_boi_m));
            cpl.AddGe(x_boi_l, cpl.Prod(200, y_boi_l));
            cpl.AddGe(x_hp_s, cpl.Prod(5, y_hp_s));
            cpl.AddGe(x_hp_m, cpl.Prod(50, y_hp_m));
            cpl.AddGe(x_hp_l, cpl.Prod(200, y_hp_l));
            cpl.AddGe(x_chp_s, cpl.Prod(5, y_chp_s));
            cpl.AddGe(x_chp_m, cpl.Prod(50, x_chp_m));
            cpl.AddGe(x_chp_l, cpl.Prod(200, y_chp_l));
            cpl.AddGe(x_tes, cpl.Prod(20, y_tes));
            cpl.AddGe(x_bat, cpl.Prod(1, y_bat));



            //fix cost indicator

            // ST and PV
            ILinearNumExpr st_totcap = cpl.LinearNumExpr();
            ILinearNumExpr pv_totcap = cpl.LinearNumExpr();
            for (int i = 0; i < solarspots; i++)
            {
                st_totcap.AddTerm(1, x_st[i]);
                pv_totcap.AddTerm(1, x_pv[i]);
            }
            cpl.AddLe(st_totcap, cpl.Prod(M, y_st)); //fixcost indicator
            cpl.AddLe(pv_totcap, cpl.Prod(M, y_pv));

            //CHP
            cpl.AddLe(x_chp_s, cpl.Prod(M, y_chp_s));
            cpl.AddLe(x_chp_m, cpl.Prod(M, y_chp_m));
            cpl.AddLe(x_chp_l, cpl.Prod(M, y_chp_l));

            //Boi
            cpl.AddLe(x_boi_s, cpl.Prod(M, y_boi_s));
            cpl.AddLe(x_boi_m, cpl.Prod(M, y_boi_m));
            cpl.AddLe(x_boi_l, cpl.Prod(M, y_boi_l));

            //HP
            cpl.AddLe(x_hp_s, cpl.Prod(M, y_hp_s));
            cpl.AddLe(x_hp_m, cpl.Prod(M, y_hp_m));
            cpl.AddLe(x_hp_l, cpl.Prod(M, y_hp_l));

            //TES
            cpl.AddLe(x_tes, cpl.Prod(M, y_tes));

            //BAt
            cpl.AddLe(x_bat, cpl.Prod(M, y_bat));

            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // co2 constraint
            ILinearNumExpr a_co2 = cpl.LinearNumExpr();
            for (int t = 0; t < this.horizon; t++)
            {
                // co2 emissions by purchase from grid and heat pump
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t], x_purchase[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_dhw[t]), x_hp_s_op_dhw[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_dhw[t]), x_hp_m_op_dhw[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_dhw[t]), x_hp_l_op_dhw[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_sh[t]), x_hp_s_op_sh[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_sh[t]), x_hp_m_op_sh[t]);
                a_co2.AddTerm((this.a_carbon[t] / 1000) * this.c_num_of_days[t] * (1 / c_hp_eff_sh[t]), x_hp_l_op_sh[t]);
                // co2 by natural gas
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_s_op_dhw[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_m_op_dhw[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_l_op_dhw[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_s_op_sh[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_m_op_sh[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_boi_eff), x_boi_l_op_sh[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_chp_s_eff), x_chp_s_op_e[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_chp_m_eff), x_chp_m_op_e[t]);
                a_co2.AddTerm(lca_gas * this.c_num_of_days[t] * (1 / c_chp_l_eff), x_chp_l_op_e[t]);
            }


            for (int i = 0; i < solarspots; i++)
                a_co2.AddTerm(lca_pv, x_pv[i]);

            a_co2.AddTerm(lca_boiler, x_boi_s);
            a_co2.AddTerm(lca_hp, x_hp_s);
            a_co2.AddTerm(lca_chp, x_chp_s);
            a_co2.AddTerm(lca_therm, x_tes);
            a_co2.AddTerm(lca_battery, x_bat);


            if (bln_co2target && !bln_mincarbon) cpl.AddLe(cpl.Sum(lca_ac * x_AC, a_co2), b_co2_target);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // PROBLEM OBJECTIVE FUNCTION
            ILinearNumExpr OPEX = cpl.LinearNumExpr();
            ILinearNumExpr CAPEX = cpl.LinearNumExpr();
            for (int i = 0; i < solarspots; i++)
            {
                CAPEX.AddTerm(c_pv, x_pv[i]);
                CAPEX.AddTerm(c_st, x_st[i]);
            }
            CAPEX.AddTerm(c_hp_s, x_hp_s);
            CAPEX.AddTerm(c_hp_m, x_hp_m);
            CAPEX.AddTerm(c_hp_l, x_hp_l);
            CAPEX.AddTerm(c_boi_s, x_boi_s);
            CAPEX.AddTerm(c_boi_m, x_boi_m);
            CAPEX.AddTerm(c_boi_l, x_boi_l);
            CAPEX.AddTerm(c_chp_s, x_chp_s);
            CAPEX.AddTerm(c_chp_m, x_chp_m);
            CAPEX.AddTerm(c_chp_l, x_chp_l);
            CAPEX.AddTerm(c_tes, x_tes);
            CAPEX.AddTerm(c_bat, x_bat);

            CAPEX.AddTerm(fc_pv, y_pv);
            CAPEX.AddTerm(fc_st, y_st);
            CAPEX.AddTerm(fc_boi_s, y_boi_s);
            CAPEX.AddTerm(fc_boi_m, y_boi_m);
            CAPEX.AddTerm(fc_boi_l, y_boi_l);
            CAPEX.AddTerm(fc_hp_s, y_hp_s);
            CAPEX.AddTerm(fc_hp_m, y_hp_m);
            CAPEX.AddTerm(fc_hp_l, y_hp_l);
            CAPEX.AddTerm(fc_chp_s, y_chp_s);
            CAPEX.AddTerm(fc_chp_m, y_chp_m);
            CAPEX.AddTerm(fc_chp_l, y_chp_l);
            CAPEX.AddTerm(fc_bat, y_bat);
            CAPEX.AddTerm(fc_tes, y_tes);


            for (int t = 0; t < this.horizon; t++)
            {
                OPEX.AddTerm(this.c_grid[t] * this.c_num_of_days[t], x_purchase[t]);
                OPEX.AddTerm(-1.0 * this.c_feedin[t] * this.c_num_of_days[t], x_feedin[t]);

                for (int i = 0; i < solarspots; i++)
                {
                    OPEX.AddTerm((c_pv_om * this.a_solar[i][t] * 0.001 * this.c_pv_eff[i][t] * this.c_num_of_days[t]), x_pv[i]);
                    OPEX.AddTerm((c_st_om * this.c_num_of_days[t]), x_st_op_dhw[i][t]);
                    OPEX.AddTerm((c_st_om * this.c_num_of_days[t]), x_st_op_sh[i][t]);
                }

                OPEX.AddTerm((c_gas / c_chp_s_eff) * this.c_num_of_days[t], x_chp_s_op_e[t]);
                OPEX.AddTerm((c_gas / c_chp_m_eff) * this.c_num_of_days[t], x_chp_m_op_e[t]);
                OPEX.AddTerm((c_gas / c_chp_l_eff) * this.c_num_of_days[t], x_chp_l_op_e[t]);
                OPEX.AddTerm(c_chp_om * this.c_num_of_days[t], x_chp_s_op_e[t]);
                OPEX.AddTerm(c_chp_om * this.c_num_of_days[t], x_chp_m_op_e[t]);
                OPEX.AddTerm(c_chp_om * this.c_num_of_days[t], x_chp_l_op_e[t]);

                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_s_op_dhw[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_m_op_dhw[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_l_op_dhw[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_s_op_sh[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_m_op_sh[t]);
                OPEX.AddTerm((c_gas / c_boi_eff) * this.c_num_of_days[t], x_boi_l_op_sh[t]);

                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_s_op_dhw[t]);
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_m_op_dhw[t]);
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_l_op_dhw[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_s_op_dhw[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_m_op_dhw[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_l_op_dhw[t]);
                OPEX.AddTerm(c_tes_om * this.c_num_of_days[t], x_tes_ch_dhw[t]);
                OPEX.AddTerm(c_tes_om * this.c_num_of_days[t], x_tes_dis_dhw[t]);

                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_s_op_sh[t]);
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_m_op_sh[t]);
                OPEX.AddTerm(c_hp_om * this.c_num_of_days[t], x_hp_l_op_sh[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_s_op_sh[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_m_op_sh[t]);
                OPEX.AddTerm(c_boi_om * this.c_num_of_days[t], x_boi_l_op_sh[t]);
                OPEX.AddTerm(c_tes_om * this.c_num_of_days[t], x_tes_ch_sh[t]);
                OPEX.AddTerm(c_tes_om * this.c_num_of_days[t], x_tes_dis_sh[t]);

                OPEX.AddTerm(c_bat_om * this.c_num_of_days[t], x_bat_ch[t]);
                OPEX.AddTerm(c_bat_om * this.c_num_of_days[t], x_bat_dis[t]);
            }
            // cost minimization
            if (!bln_mincarbon) cpl.AddMinimize(cpl.Sum(OPEX, cpl.Sum(CAPEX, c_ac * x_AC + y_ac * fc_ac)));
            // co2 minimization
            if (bln_mincarbon) cpl.AddMinimize(a_co2);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////






            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // SOLVE and CPLEX SETTINGS
            cpl.SetParam(Cplex.Param.ClockType, 2);     // 2 = measuring time in wall clock time. 1 = cpu time
            cpl.SetParam(Cplex.Param.TimeLimit, 60);
            //cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.05);
            //cpl.SetOut(null);
            cpl.Solve();

            //Console.WriteLine("mip gap: {0}", cpl.GetMIPRelativeGap());
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////





            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////
            // OUTPUTS
            //for (int i = 0; i < pvspots; i++)
            //    Console.WriteLine("pv spot {0}, area installed: {1}", i, cpl.GetValue(x_pv[i]));


            //if (bln_mincarbon)
            //{
            //    Console.WriteLine("cost: " + cpl.GetValue(cpl.Sum(OPEX, CAPEX)));
            //}
            //else
            //    Console.WriteLine("cost: " + cpl.ObjValue);

            //Console.WriteLine("OPEX: {0}... CAPEX: {1}", cpl.GetValue(OPEX), cpl.GetValue(CAPEX));

            //Console.WriteLine("total co2 emissions: {0}", cpl.GetValue(a_co2));
            //Console.WriteLine("tot pur: {0}, tot sold: {1}", cpl.GetValue(cpl.Sum(x_purchase)), cpl.GetValue(cpl.Sum(x_feedin)));
            //Console.ReadKey();

            if (bln_mincarbon)
            {
                outs.cost = cpl.GetValue(cpl.Sum(OPEX, CAPEX));
                outs.carbon = cpl.ObjValue;
            }
            else
            {
                outs.cost = cpl.ObjValue;
                outs.carbon = cpl.GetValue(a_co2);
            }

            outs.x_elecpur = new double[this.horizon];
            outs.x_feedin = new double[this.horizon];
            outs.b_pvprod = new double[this.horizon];

            outs.x_batcharge = new double[this.horizon];
            outs.x_batdischarge = new double[this.horizon];
            outs.x_batsoc = new double[this.horizon];
            outs.x_tessoc = new double[this.horizon];
            outs.x_tescharge_dhw = new double[this.horizon];
            outs.x_tesdischarge_dhw = new double[this.horizon];
            outs.x_tescharge_sh = new double[this.horizon];
            outs.x_tesdischarge_sh = new double[this.horizon];

            outs.x_st_optot_dhw = new double[this.horizon];
            outs.x_st_optot_sh = new double[this.horizon];
            outs.x_st_op_dhw = new double[solarspots][];
            outs.x_st_op_sh = new double[solarspots][];
            for (int i = 0; i < solarspots; i++)
            {
                outs.x_st_op_dhw[i] = new double[this.horizon];
                outs.x_st_op_sh[i] = new double[this.horizon];
            }

            outs.x_hp_s_op_dhw = new double[this.horizon];
            outs.x_boi_s_op_dhw = new double[this.horizon];
            outs.x_chp_s_op_dhw = new double[this.horizon];
            outs.x_chp_s_dump_dhw = new double[this.horizon];
            outs.x_hp_m_op_dhw = new double[this.horizon];
            outs.x_boi_m_op_dhw = new double[this.horizon];
            outs.x_chp_m_op_dhw = new double[this.horizon];
            outs.x_chp_m_dump_dhw = new double[this.horizon];
            outs.x_hp_l_op_dhw = new double[this.horizon];
            outs.x_boi_l_op_dhw = new double[this.horizon];
            outs.x_chp_l_op_dhw = new double[this.horizon];
            outs.x_chp_l_dump_dhw = new double[this.horizon];

            outs.x_hp_s_op_sh = new double[this.horizon];
            outs.x_boi_s_op_sh = new double[this.horizon];
            outs.x_chp_s_op_sh = new double[this.horizon];
            outs.x_chp_s_dump_sh = new double[this.horizon];
            outs.x_hp_m_op_sh = new double[this.horizon];
            outs.x_boi_m_op_sh = new double[this.horizon];
            outs.x_chp_m_op_sh = new double[this.horizon];
            outs.x_chp_m_dump_sh = new double[this.horizon];
            outs.x_hp_l_op_sh = new double[this.horizon];
            outs.x_boi_l_op_sh = new double[this.horizon];
            outs.x_chp_l_op_sh = new double[this.horizon];
            outs.x_chp_l_dump_sh = new double[this.horizon];

            outs.x_chp_s_op_e = new double[this.horizon];
            outs.x_chp_m_op_e = new double[this.horizon];
            outs.x_chp_l_op_e = new double[this.horizon];


            outs.x_pv = new double[this.b_solar_area.Length];
            outs.x_st = new double[this.b_solar_area.Length];
            for (int t = 0; t < this.horizon; t++)
            {
                outs.x_elecpur[t] = cpl.GetValue(x_purchase[t]);
                outs.x_feedin[t] = cpl.GetValue(x_feedin[t]);
                outs.x_batcharge[t] = cpl.GetValue(x_bat_ch[t]);
                outs.x_batdischarge[t] = cpl.GetValue(x_bat_dis[t]);
                outs.x_batsoc[t] = cpl.GetValue(x_bat_soc[t]);
                outs.x_tessoc[t] = cpl.GetValue(x_tes_soc[t]);
                outs.x_tescharge_dhw[t] = cpl.GetValue(x_tes_ch_dhw[t]);
                outs.x_tesdischarge_dhw[t] = cpl.GetValue(x_tes_dis_dhw[t]);
                outs.x_tescharge_sh[t] = cpl.GetValue(x_tes_ch_sh[t]);
                outs.x_tesdischarge_sh[t] = cpl.GetValue(x_tes_dis_sh[t]);
                outs.x_hp_s_op_dhw[t] = cpl.GetValue(x_hp_s_op_dhw[t]);
                outs.x_boi_s_op_dhw[t] = cpl.GetValue(x_boi_s_op_dhw[t]);
                outs.x_chp_s_op_dhw[t] = cpl.GetValue(x_chp_s_op_dhw[t]);
                outs.x_chp_s_dump_dhw[t] = cpl.GetValue(x_chp_s_dump_dhw[t]);
                outs.x_hp_m_op_dhw[t] = cpl.GetValue(x_hp_m_op_dhw[t]);
                outs.x_boi_m_op_dhw[t] = cpl.GetValue(x_boi_m_op_dhw[t]);
                outs.x_chp_m_op_dhw[t] = cpl.GetValue(x_chp_m_op_dhw[t]);
                outs.x_chp_m_dump_dhw[t] = cpl.GetValue(x_chp_m_dump_dhw[t]);
                outs.x_hp_l_op_dhw[t] = cpl.GetValue(x_hp_l_op_dhw[t]);
                outs.x_boi_l_op_dhw[t] = cpl.GetValue(x_boi_l_op_dhw[t]);
                outs.x_chp_l_op_dhw[t] = cpl.GetValue(x_chp_l_op_dhw[t]);
                outs.x_chp_l_dump_dhw[t] = cpl.GetValue(x_chp_l_dump_dhw[t]);
                outs.x_hp_s_op_sh[t] = cpl.GetValue(x_hp_s_op_sh[t]);
                outs.x_boi_s_op_sh[t] = cpl.GetValue(x_boi_s_op_sh[t]);
                outs.x_chp_s_op_sh[t] = cpl.GetValue(x_chp_s_op_sh[t]);
                outs.x_chp_s_dump_sh[t] = cpl.GetValue(x_chp_s_dump_sh[t]);
                outs.x_hp_m_op_sh[t] = cpl.GetValue(x_hp_m_op_sh[t]);
                outs.x_boi_m_op_sh[t] = cpl.GetValue(x_boi_m_op_sh[t]);
                outs.x_chp_m_op_sh[t] = cpl.GetValue(x_chp_m_op_sh[t]);
                outs.x_chp_m_dump_sh[t] = cpl.GetValue(x_chp_m_dump_sh[t]);
                outs.x_hp_l_op_sh[t] = cpl.GetValue(x_hp_l_op_sh[t]);
                outs.x_boi_l_op_sh[t] = cpl.GetValue(x_boi_l_op_sh[t]);
                outs.x_chp_l_op_sh[t] = cpl.GetValue(x_chp_l_op_sh[t]);
                outs.x_chp_l_dump_sh[t] = cpl.GetValue(x_chp_l_dump_sh[t]);
                outs.x_chp_s_op_e[t] = cpl.GetValue(x_chp_s_op_e[t]);
                outs.x_chp_m_op_e[t] = cpl.GetValue(x_chp_m_op_e[t]);
                outs.x_chp_l_op_e[t] = cpl.GetValue(x_chp_l_op_e[t]);

                outs.b_pvprod[t] = 0;
                outs.x_st_optot_dhw[t] = 0;
                outs.x_st_optot_sh[t] = 0;
                for (int i = 0; i < this.b_solar_area.Length; i++)
                {
                    double pvprodnow = this.a_solar[i][t] * 0.001 * this.c_pv_eff[i][t] * cpl.GetValue(x_pv[i]);
                    outs.b_pvprod[t] += pvprodnow;
                    outs.x_st_optot_dhw[t] += cpl.GetValue(x_st_op_dhw[i][t]);
                    outs.x_st_optot_sh[t] += cpl.GetValue(x_st_op_sh[i][t]);

                    outs.x_st_op_dhw[i][t] = cpl.GetValue(x_st_op_dhw[i][t]);
                    outs.x_st_op_sh[i][t] = cpl.GetValue(x_st_op_sh[i][t]);
                }
            }
            for (int i = 0; i < this.b_solar_area.Length; i++)
            {
                outs.x_pv[i] = cpl.GetValue(x_pv[i]);
                outs.x_st[i] = cpl.GetValue(x_st[i]);
            }

            outs.x_ac = x_AC;
            outs.x_tes = cpl.GetValue(x_tes);
            outs.x_bat = cpl.GetValue(x_bat);

            outs.x_hp_s = cpl.GetValue(x_hp_s);
            outs.x_boi_s = cpl.GetValue(x_boi_s);
            outs.x_chp_s = cpl.GetValue(x_chp_s);
            outs.x_hp_m = cpl.GetValue(x_hp_m);
            outs.x_boi_m = cpl.GetValue(x_boi_m);
            outs.x_chp_m = cpl.GetValue(x_chp_m);
            outs.x_hp_l = cpl.GetValue(x_hp_l);
            outs.x_boi_l = cpl.GetValue(x_boi_l);
            outs.x_chp_l = cpl.GetValue(x_chp_l);


            outs.OPEX = cpl.GetValue(OPEX);
            outs.CAPEX = cpl.GetValue(CAPEX);
            //_________________________________________________________________________
            ///////////////////////////////////////////////////////////////////////////




            return outs;
        }



        #region read inputs and write outputs

        /// <summary>
        /// Reads input data and performs horizon reduction according to horizonmode
        /// </summary>
        /// <param name="path">input data path, containing 8760 profiles.</param>
        /// <param name="grid_price"></param>
        /// <param name="feedin_price"></param>
        /// <param name="carbon"></param>
        /// <param name="elec_demand"></param>
        /// <param name="SH_demand"></param>
        /// <param name="DHW_demand"></param>
        /// <param name="cool_demand"></param>
        /// <param name="solar"></param>
        /// <param name="pv_area"></param>
        /// <param name="PV_efficiency"></param>
        /// <param name="ST_efficiency_sh"></param>
        /// <param name="ST_efficiency_dhw"></param>
        /// <param name="HP_COP_sh"></param>
        /// <param name="HP_COP_dhw"></param>
        /// <param name="AC_COP"></param>
        private void ReadInput(string path,
            out double[] elec_demand,
            out double[] SH_demand, out double[] DHW_demand,
            out double[] cool_demand,
            out double[] grid_price, out double[] feedin_price, out double[] carbon,
            out double[][] solar, out double[] pv_area,
            out double[][] PV_efficiency,
            out  double[][] ST_efficiency_sh, out double[][] ST_efficiency_dhw,
            out double[] HP_COP_sh, out double[] HP_COP_dhw,
            out double[] AC_COP,
            out double peakcool, out double peaksh, out double peakdhw, out double peakelec,
            out double maxBatCap, out double maxTESCap)
        {
            // 8 input profiles:
            // 0: elec                                      -> elec_demand.csv
            // 1: space heating SH low temperature 35°C     -> sh_demand.csv
            // 2: domestic hot water DH 65°C                -> dhw_demand.csv
            // 3: cool                                      -> cool_demand.csv
            // 4: grid                                      -> grid.csv
            // 5: feedin                                    -> feedin.csv
            // 6: carbon                                    -> carbon.csv
            // 7: solar. contains all facade patches.       -> solar.csv




            // 0: elec_demand
            using (var reader = new StreamReader(path + @"inputs\elec_demand.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                elec_demand = listA.ToArray();
            }
            peakelec = elec_demand.Max();

            int horizon = elec_demand.Length;

            // 1: SH_demand
            using (var reader = new StreamReader(path + @"inputs\sh_demand.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                SH_demand = listA.ToArray();
            }
            peaksh = SH_demand.Max();

            // 2: DHW_demand
            using (var reader = new StreamReader(path + @"inputs\dhw_demand.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                DHW_demand = listA.ToArray();
            }
            peakdhw = DHW_demand.Max();

            // 3: cool_demand
            using (var reader = new StreamReader(path + @"inputs\cool_demand.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                cool_demand = listA.ToArray();
            }
            peakcool = cool_demand.Max();

            // 4: grid_price
            using (var reader = new StreamReader(path + @"inputs\grid.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                grid_price = listA.ToArray();
            }

            // 5: feedin_price
            using (var reader = new StreamReader(path + @"inputs\feedin.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                feedin_price = listA.ToArray();
            }

            // 6: carbon
            using (var reader = new StreamReader(path + @"inputs\carbon.csv"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                carbon = listA.ToArray();
            }


            //pv area; defines also how many solar potentials exist
            pv_area = new double[0];
            using (StreamReader file = new StreamReader(path + @"inputs\solar_AREAS.txt"))
            {
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    char[] delimiters = new char[] { ';' };
                    string[] parts = line.Split(delimiters);
                    pv_area = new double[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        pv_area[i] = Convert.ToDouble(parts[i]);
                    }
                }
                file.Close();
            }


            //pv containts multiple sensor point profiles at different facade patches.
            // 7: pv potentials
            solar = new double[pv_area.Length][];
            using (var reader = new StreamReader(path + @"inputs\solar_pot.csv"))       //lines -> 8760 timesteps. columns -> profiles for each sensor point
            {
                List<List<double>> listA = new List<List<double>>();
                for (int i = 0; i < solar.Length; i++)
                {
                    listA.Add(new List<double>());
                }

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    char[] delimiters = new char[] { ';' };
                    string[] parts = line.Split(delimiters);
                    for (int i = 0; i < solar.Length; i++)
                    {
                        listA[i].Add(Convert.ToDouble(parts[i]));
                    }
                }
                for (int i = 0; i < solar.Length; i++)
                {
                    solar[i] = listA[i].ToArray();
                }
            }





            //load ambient temperature
            double[] temperature;
            using (var reader = new StreamReader(path + @"inputs\ambient_temperature.txt"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                temperature = listA.ToArray();
            }






            // PV_efficiency and ST_efficiency
            PV_efficiency = new double[solar.Length][];
            ST_efficiency_dhw = new double[solar.Length][];
            ST_efficiency_sh = new double[solar.Length][];
            for (int i = 0; i < PV_efficiency.Length; i++)
            {
                PV_efficiency[i] = calc_PV_efficiency(temperature, solar[i], pv_NCOT, pv_T_aNCOT, pv_P_NCOT, pv_beta_ref, pv_n_ref);
                ST_efficiency_dhw[i] = calc_ST_efficiency(temperature, solar[i], st_Tin_dhw, st_Frta, st_FrUl);
                ST_efficiency_sh[i] = calc_ST_efficiency(temperature, solar[i], st_Tin_sh, st_Frta, st_FrUl);
            }


            // HP efficiency
            HP_COP_sh = calc_HP_COP(temperature, hp_sup_sh, hp_pi1, hp_pi2, hp_pi3, hp_pi4);
            HP_COP_dhw = calc_HP_COP(temperature, hp_sup_dhw, hp_pi1, hp_pi2, hp_pi3, hp_pi4);

            // AC efficiency
            AC_COP = calc_AC_COP(temperature);



            //load max storage capacities
            using (var reader = new StreamReader(path + @"inputs\storagecap.txt"))
            {
                List<double> listA = new List<double>();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    listA.Add(Convert.ToDouble(line));
                }
                maxBatCap = listA[0];
                maxTESCap = listA[1];
            }

        }

        /// <summary>
        /// Reduces horizon down to 1 week per 2 months. returns arrays of 6 * 7 * 24 hours. weighting of cost etc has to be done in the milp
        /// </summary>
        private void ReduceHorizon_6weeks(
            ref double[] elec_demand,
            ref double[] SH_demand, ref double[] DHW_demand,
            ref double[] cool_demand,
            ref double[] grid_price, ref double[] feedin_price, ref double[] carbon,
            ref double[][] solar,
            ref double[][] PV_efficiency,
            ref double[][] ST_efficiency_sh, ref double[][] ST_efficiency_dhw,
            ref double[] HP_COP_sh, ref double[] HP_COP_dhw,
            ref double[] AC_COP,
            out double[] num_days)
        {
            // t=0-23 is tuesday
            // jan  [313, 480]
            // mar  [1657, 1824]
            // may  [3169, 3336]
            // july [4681, 4848]
            // sep  [6025, 6192]
            // nov  [7537, 7704]

            double[] test = new double[6];

            int redhor = 6 * 7 * 24;
            num_days = new double[redhor];

            for (int i = 0; i < 7; i++) //first week in january. counts for h=0 to h=(1657-481)/2 + 481 = 1069 h +half of december (8017 - 8759)/2=371
            {
                for (int h = 0; h < 24; h++)
                {
                    num_days[i * 24 + h] = 1069 / 24 + 371 / 24;
                }
            }
            for (int i = 7; i < 14; i++) //second week in march. counts for h=1069 to h=(3169-1825)/2 + 1825= 2497 h 
            {
                for (int h = 0; h < 24; h++)
                {
                    num_days[i * 24 + h] = 2497 / 24 - 1069 / 24;
                }
            }
            for (int i = 14; i < 21; i++) //third week in may. counts for h=2497 to h=(4681-3337)/2 + 3337= 4009 h 
            {
                for (int h = 0; h < 24; h++)
                {
                    num_days[i * 24 + h] = 4009 / 24 - 2497 / 24;
                }
            }
            for (int i = 21; i < 28; i++) //fourth week in july. counts for h=4009 to h=(6025-4849)/2 + 4849= 5437 h 
            {
                for (int h = 0; h < 24; h++)
                {
                    num_days[i * 24 + h] = 5437 / 24 - 4009 / 24;
                }
            }
            for (int i = 28; i < 35; i++) //fifth week in sept. counts for h=5437 to h=(7537-6193)/2 + 6193= 6865 h 
            {
                for (int h = 0; h < 24; h++)
                {
                    num_days[i * 24 + h] = 6865 / 24 - 5437 / 24;
                }
            }
            for (int i = 35; i < 42; i++) //sixth week in november. counts for h=6865 to h=8759 - half of december (8017 - 8759)/2=371
            {
                for (int h = 0; h < 24; h++)
                {
                    num_days[i * 24 + h] = 8760 / 24 - 6865 / 24 - 371 / 24;
                }
            }


            int p = 0;

            double[] copy_elec = new double[redhor];
            double[] copy_sh = new double[redhor];
            double[] copy_dhw = new double[redhor];
            double[] copy_cool = new double[redhor];
            double[] copy_grid = new double[redhor];
            double[] copy_feedin = new double[redhor];
            double[] copy_carbon = new double[redhor];
            double[] copy_hpsh = new double[redhor];
            double[] copy_hpdhw = new double[redhor];
            double[] copy_ac = new double[redhor];

            double[][] copy_solar = new double[solar.Length][];
            double[][] copy_pveff = new double[solar.Length][];
            double[][] copy_steffsh = new double[solar.Length][];
            double[][] copy_steffdhw = new double[solar.Length][];
            for (int i = 0; i < solar.Length; i++)
            {
                copy_solar[i] = new double[6 * 7 * 24];
                copy_pveff[i] = new double[6 * 7 * 24];
                copy_steffsh[i] = new double[6 * 7 * 24];
                copy_steffdhw[i] = new double[6 * 7 * 24];
            }

            // jan  [313, 480]
            this.copyinputs(ref p, ref copy_elec, ref copy_sh, ref copy_dhw, ref copy_cool,
                ref copy_grid, ref copy_feedin, ref copy_carbon, ref copy_hpsh, ref copy_hpdhw,
                ref copy_ac, ref copy_solar, ref copy_pveff, ref copy_steffsh, ref copy_steffdhw,
                elec_demand, SH_demand, DHW_demand, cool_demand, grid_price, feedin_price, carbon, HP_COP_sh,
                HP_COP_dhw, AC_COP, solar, PV_efficiency, ST_efficiency_sh, ST_efficiency_dhw,
                313, 481);
            // mar  [1657, 1824]
            this.copyinputs(ref p, ref copy_elec, ref copy_sh, ref copy_dhw, ref copy_cool,
                ref copy_grid, ref copy_feedin, ref copy_carbon, ref copy_hpsh, ref copy_hpdhw,
                ref copy_ac, ref copy_solar, ref copy_pveff, ref copy_steffsh, ref copy_steffdhw,
                elec_demand, SH_demand, DHW_demand, cool_demand, grid_price, feedin_price, carbon, HP_COP_sh,
                HP_COP_dhw, AC_COP, solar, PV_efficiency, ST_efficiency_sh, ST_efficiency_dhw,
                1657, 1825);
            // may  [3169, 3336]
            this.copyinputs(ref p, ref copy_elec, ref copy_sh, ref copy_dhw, ref copy_cool,
                ref copy_grid, ref copy_feedin, ref copy_carbon, ref copy_hpsh, ref copy_hpdhw,
                ref copy_ac, ref copy_solar, ref copy_pveff, ref copy_steffsh, ref copy_steffdhw,
                elec_demand, SH_demand, DHW_demand, cool_demand, grid_price, feedin_price, carbon, HP_COP_sh,
                HP_COP_dhw, AC_COP, solar, PV_efficiency, ST_efficiency_sh, ST_efficiency_dhw,
                3169, 3337);
            // july [4681, 4848]
            this.copyinputs(ref p, ref copy_elec, ref copy_sh, ref copy_dhw, ref copy_cool,
                ref copy_grid, ref copy_feedin, ref copy_carbon, ref copy_hpsh, ref copy_hpdhw,
                ref copy_ac, ref copy_solar, ref copy_pveff, ref copy_steffsh, ref copy_steffdhw,
                elec_demand, SH_demand, DHW_demand, cool_demand, grid_price, feedin_price, carbon, HP_COP_sh,
                HP_COP_dhw, AC_COP, solar, PV_efficiency, ST_efficiency_sh, ST_efficiency_dhw,
                4681, 4849);
            // sep  [6025, 6192]
            this.copyinputs(ref p, ref copy_elec, ref copy_sh, ref copy_dhw, ref copy_cool,
                ref copy_grid, ref copy_feedin, ref copy_carbon, ref copy_hpsh, ref copy_hpdhw,
                ref copy_ac, ref copy_solar, ref copy_pveff, ref copy_steffsh, ref copy_steffdhw,
                elec_demand, SH_demand, DHW_demand, cool_demand, grid_price, feedin_price, carbon, HP_COP_sh,
                HP_COP_dhw, AC_COP, solar, PV_efficiency, ST_efficiency_sh, ST_efficiency_dhw,
                6025, 6193);
            // nov  [7537, 7704]
            this.copyinputs(ref p, ref copy_elec, ref copy_sh, ref copy_dhw, ref copy_cool,
                ref copy_grid, ref copy_feedin, ref copy_carbon, ref copy_hpsh, ref copy_hpdhw,
                ref copy_ac, ref copy_solar, ref copy_pveff, ref copy_steffsh, ref copy_steffdhw,
                elec_demand, SH_demand, DHW_demand, cool_demand, grid_price, feedin_price, carbon, HP_COP_sh,
                HP_COP_dhw, AC_COP, solar, PV_efficiency, ST_efficiency_sh, ST_efficiency_dhw,
                7537, 7705);




            elec_demand = new double[redhor];
            SH_demand = new double[redhor];
            DHW_demand = new double[redhor];
            cool_demand = new double[redhor];
            grid_price = new double[redhor];
            feedin_price = new double[redhor];
            carbon = new double[redhor];
            HP_COP_sh = new double[redhor];
            HP_COP_dhw = new double[redhor];
            AC_COP = new double[redhor];
            solar = new double[copy_solar.Length][];
            PV_efficiency = new double[copy_solar.Length][];
            ST_efficiency_sh = new double[copy_solar.Length][];
            ST_efficiency_dhw = new double[copy_solar.Length][];


            copy_elec.CopyTo(elec_demand, 0);
            copy_sh.CopyTo(SH_demand, 0);
            copy_dhw.CopyTo(DHW_demand, 0);
            copy_cool.CopyTo(cool_demand, 0);
            copy_grid.CopyTo(grid_price, 0);
            copy_feedin.CopyTo(feedin_price, 0);
            copy_carbon.CopyTo(carbon, 0);
            copy_hpsh.CopyTo(HP_COP_sh, 0);
            copy_hpdhw.CopyTo(HP_COP_dhw, 0);
            copy_ac.CopyTo(AC_COP, 0);
            for (int i = 0; i < copy_solar.Length; i++)
            {
                solar[i] = new double[redhor];
                PV_efficiency[i] = new double[redhor];
                ST_efficiency_sh[i] = new double[redhor];
                ST_efficiency_dhw[i] = new double[redhor];

                copy_solar[i].CopyTo(solar[i], 0);
                copy_pveff[i].CopyTo(PV_efficiency[i], 0);
                copy_steffsh[i].CopyTo(ST_efficiency_sh[i], 0);
                copy_steffdhw[i].CopyTo(ST_efficiency_dhw[i], 0);
            }

        }


        private void copyinputs(ref int p,
            ref double[] copy_elec, ref double[] copy_sh, ref double[] copy_dhw,
            ref double[] copy_cool, ref double[] copy_grid, ref double[] copy_feedin, ref double[] copy_carbon,
            ref double[] copy_hpsh, ref double[] copy_hpdhw, ref double[] copy_ac,
            ref double[][] copy_solar, ref double[][] copy_pveff, ref double[][] copy_steffsh, ref double[][] copy_steffdhw,
            double[] elec_demand, double[] SH_demand, double[] DHW_demand, double[] cool_demand, double[] grid_price, double[] feedin_price,
            double[] carbon, double[] HP_COP_sh, double[] HP_COP_dhw, double[] AC_COP, double[][] solar, double[][] PV_efficiency,
            double[][] ST_efficiency_sh, double[][] ST_efficiency_dhw,
            int tstart, int tend)
        {
            for (int t = tstart; t < tend; t += 24)
            {
                for (int h = 0; h < 24; h++)
                {
                    copy_elec[p] = elec_demand[t + h];
                    copy_sh[p] = SH_demand[t + h];
                    copy_dhw[p] = DHW_demand[t + h];
                    copy_cool[p] = cool_demand[t + h];
                    copy_grid[p] = grid_price[t + h];
                    copy_feedin[p] = feedin_price[t + h];
                    copy_carbon[p] = carbon[t + h];
                    copy_hpsh[p] = HP_COP_sh[t + h];
                    copy_hpdhw[p] = HP_COP_dhw[t + h];
                    copy_ac[p] = AC_COP[t + h];
                    for (int i = 0; i < solar.Length; i++)
                    {
                        copy_solar[i][p] = solar[i][t + h];
                        copy_pveff[i][p] = PV_efficiency[i][t + h];
                        copy_steffsh[i][p] = ST_efficiency_sh[i][t + h];
                        copy_steffdhw[i][p] = ST_efficiency_dhw[i][t + h];
                    }
                    p++;
                }
            }
        }


        /// <summary>
        /// Sorting profiles of typical days according to day of the year. So I can assume seasonal storage.
        /// </summary>
        /// <param name="loc_days"></param>
        /// <param name="num_of_days"></param>
        /// <param name="elec_demand"></param>
        /// <param name="heat_demand"></param>
        /// <param name="cool_demand"></param>
        /// <param name="grid_price"></param>
        /// <param name="feedin_price"></param>
        /// <param name="carbon"></param>
        /// <param name="solar"></param>
        /// <param name="solareff"></param>
        private void SortInputsSeasons_Typicaldays(int[] loc_days,
            ref int[] num_of_days,
            ref double[] elec_demand, ref double[] heat_demand, ref double[] cool_demand,
            ref double[] grid_price, ref double[] feedin_price, ref double[] carbon,
          ref double[][] solar, ref double[][] solareff, ref double[] hpeff)
        {
            int horizon = elec_demand.Length;

            int[] indices = new int[loc_days.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;

            Array.Sort(loc_days, indices);


            double[] elec_copy = new double[horizon];
            double[] heat_copy = new double[horizon];
            double[] cool_copy = new double[horizon];
            double[] grid_copy = new double[horizon];
            double[] feedin_copy = new double[horizon];
            double[] carbon_copy = new double[horizon];
            double[][] solar_copy = new double[solar.Length][];
            double[][] solareff_copy = new double[solar.Length][];
            double[] hpeff_copy = new double[horizon];
            int[] numdays_copy = new int[horizon];


            elec_demand.CopyTo(elec_copy, 0);
            heat_demand.CopyTo(heat_copy, 0);
            cool_demand.CopyTo(cool_copy, 0);
            grid_price.CopyTo(grid_copy, 0);
            feedin_price.CopyTo(feedin_copy, 0);
            carbon.CopyTo(carbon_copy, 0);
            hpeff.CopyTo(hpeff_copy, 0);
            num_of_days.CopyTo(numdays_copy, 0);
            for (int s = 0; s < solar.Length; s++)
            {
                solar_copy[s] = new double[solar[s].Length];
                solareff_copy[s] = new double[solareff[s].Length];
                solar[s].CopyTo(solar_copy[s], 0);
                solareff[s].CopyTo(solareff_copy[s], 0);
            }

            elec_demand = new double[horizon];
            heat_demand = new double[horizon];
            cool_demand = new double[horizon];
            grid_price = new double[horizon];
            feedin_price = new double[horizon];
            carbon = new double[horizon];
            hpeff = new double[horizon];
            num_of_days = new int[horizon];
            solar = new double[solar_copy.Length][];
            solareff = new double[solareff_copy.Length][];
            for (int s = 0; s < solar.Length; s++)
            {
                solar[s] = new double[horizon];
                solareff[s] = new double[horizon];
            }

            int t = 0;
            for (int i = 0; i < indices.Length; i++)
            {
                for (int h = 0; h < 24; h++)
                {
                    elec_demand[t] = elec_copy[h + (indices[i] * 24)];
                    heat_demand[t] = heat_copy[h + (indices[i] * 24)];
                    cool_demand[t] = cool_copy[h + (indices[i] * 24)];
                    grid_price[t] = grid_copy[h + (indices[i] * 24)];
                    feedin_price[t] = feedin_copy[h + (indices[i] * 24)];
                    carbon[t] = carbon_copy[h + (indices[i] * 24)];
                    hpeff[t] = hpeff_copy[h + (indices[i] * 24)];
                    num_of_days[t] = numdays_copy[h + (indices[i] * 24)];
                    for (int s = 0; s < solar_copy.Length; s++)
                    {
                        solar[s][t] = solar_copy[s][h + (indices[i] * 24)];
                        solareff[s][t] = solareff_copy[s][h + (indices[i] * 24)];
                    }

                    t++;
                }
            }
        }



        #endregion

        #region Calculate efficiencies


        /// <summary>
        /// 
        /// </summary>
        /// <param name="Tamb"></param>
        /// <param name="I"></param>
        /// <param name="pv_NCOT"></param>
        /// <param name="pv_T_aNCOT"></param>
        /// <param name="pv_P_NCOT"></param>
        /// <param name="pv_beta_ref"></param>
        /// <param name="pv_n_ref"></param>
        /// <returns></returns>
        private static double[] calc_PV_efficiency(double[] Tamb, double[] I,
    double pv_NCOT, double pv_T_aNCOT, double pv_P_NCOT, double pv_beta_ref, double pv_n_ref)
        {
            int horizon = 8760;
            double[] nPV = new double[horizon];

            for (int t = 0; t < horizon; t++)
            {
                double Tcell = Tamb[t] + ((pv_NCOT - pv_T_aNCOT) / pv_P_NCOT) * I[t];
                nPV[t] = pv_n_ref * (1 - pv_beta_ref * (Tcell - 25));
            }
            return nPV;
        }

        /// <summary>
        /// Omu, Hsieh, Orehounig 2016, eqt. (2)
        /// </summary>
        /// <param name="Tamb"></param>
        /// <param name="I"></param>
        /// <param name="Tin"></param>
        /// <param name="Frta"></param>
        /// <param name="FrUl"></param>
        /// <returns></returns>
        private static double[] calc_ST_efficiency(double[] Tamb, double[] I, double Tin, double Frta, double FrUl)
        {
            int horizon = 8760;
            double[] nST = new double[horizon];
            for (int t = 0; t < horizon; t++)
            {
                nST[t] = Frta - ((FrUl * (Tin - Tamb[t])) / I[t]);
                if (nST[t] < 0) nST[t] = 0;
            }
            return nST;
        }

        /// <summary>
        /// calculate 8760 cop of ASHP, according to Ashouri et al 2013
        /// </summary>
        /// <param name="Tamb"></param>
        /// <param name="Tsup"></param>
        /// <returns></returns>
        private static double[] calc_HP_COP(double[] Tamb, double Tsup, double hp_pi1, double hp_pi2, double hp_pi3, double hp_pi4)
        {
            int horizon = 8760;
            double[] COP_HP = new double[horizon];

            for (int t = 0; t < horizon; t++)
            {
                COP_HP[t] = hp_pi1 * Math.Exp(hp_pi2 * (Tsup - Tamb[t])) + hp_pi3 * Math.Exp(hp_pi4 * (Tsup - Tamb[t]));
            }
            return COP_HP;
        }

        /// <summary>
        /// Calculating ambient temperature depending COP of an Aircon.
        /// according to Ryu, Lee, Kim (2013). Optimum placement of top discharge outdoor unit installed near a wall. Eqt. (14)
        /// </summary>
        /// <param name="Tamb"></param>
        /// <returns></returns>
        private static double[] calc_AC_COP(double[] Tamb)
        {
            int horizon = 8760;
            double[] COP_AC = new double[horizon];
            for (int t = 0; t < horizon; t++)
            {
                //COP_AC[t] = 12 - 0.35 * Tamb[t] + 0.0034 * Math.Pow(Tamb[t], 2); //Ryu et al 203
                COP_AC[t] = (638.95 - 4.238 * Tamb[t]) / (100 + 3.534 * Tamb[t]); //Choi, Lee, Kim 2005, foud in Gracik et al 2015
            }
            return COP_AC;
        }



        /// <summary>
        /// Calculate time resolved PV efficiency
        /// </summary>
        /// <remarks>Source: Garcia-Domingo et al. (2014), found in Mavromatidis et al (2015).</remarks>
        /// <param name="temperature">Ambient temperature. 8760 Time series.</param>
        /// <param name="irradiation">Solar irradiation on PV cell. Time series of length of the horizon.</param>
        /// <param name="loc_days">day of the year for each typical day. required to match the temperatures to the solar potentials.</param>
        /// <returns>Time resolved PV efficiency.</returns>
        private double[] calc_PV_efficiency(double[] temperature, double[] irradiation, int[] loc_days)
        {
            int horizon = irradiation.Length;

            double[] nPV = new double[horizon];

            int t = 0;
            for (int l = 0; l < loc_days.Length; l++)
            {
                for (int h = 0; h < 24; h++)
                {
                    double temp = temperature[h + (24 * (loc_days[l] - 1))];
                    double Tcell = temp + ((pv_NCOT - pv_T_aNCOT) / pv_P_NCOT) * irradiation[t];
                    nPV[t] = pv_n_ref * (1 - pv_beta_ref * (Tcell - 25));
                    t++;
                }
            }
            return nPV;
        }

        /// <summary>
        /// Calculate COP of heat pump. depends on constants and ambient temperature.
        /// </summary>
        /// <param name="temperature"></param>
        /// <param name="loc_days"></param>
        /// <returns></returns>
        private double[] calc_HP_COP(double[] temperature, int[] loc_days)
        {
            int horizon = loc_days.Length * 24;

            double[] nHP = new double[horizon];

            int t = 0;
            for (int l = 0; l < loc_days.Length; l++)
            {
                for (int h = 0; h < 24; h++)
                {
                    double temp = temperature[h + (24 * (loc_days[l] - 1))];
                    nHP[t] = hp_pi1 * Math.Exp(hp_pi2 * (hp_sup_dhw - temp)) + hp_pi3 * Math.Exp(hp_pi4 * (hp_sup_dhw - temp));
                    t++;
                }
            }
            return nHP;
        }


        #endregion
    }


    public static class Misc
    {
        public static bool IsNullOrDefault<T>(this Nullable<T> value) where T : struct
        {
            return default(T).Equals(value.GetValueOrDefault());
        }
    }
}
