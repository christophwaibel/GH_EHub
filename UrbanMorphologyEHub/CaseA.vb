Imports ILOG.CPLEX
Imports ILOG.Concert
Imports System.IO
Imports excel = Microsoft.Office.Interop.Excel


Public Class CaseA
    Public _solved As Boolean
    Public _ObjVal As Double
    Public _cap() As Double
    Public _gen()()() As Double
    Public _op()() As Double
    Public _dump()() As Double
    Public _epur() As Double
    Public _esell() As Double

    Public _BatCap As Double
    Public _Charge_Bat() As Double
    Public _Discharge_Bat() As Double
    Public _Stored_Bat() As Double

    Public _TESCap As Double
    Public _Charge_TES() As Double
    Public _Discharge_TES() As Double
    Public _Stored_TES() As Double

    Public _CO2Emissions As Double


    Public Sub RunCaseA(_YourInputPath As String, _period As Integer, _co2reduc As Double, _
                        _maxFacadeArea As List(Of Double), _solarPot As List(Of List(Of Double)), _
                        _cooling As Double(), _heating As Double(), _elec As Double())

        Dim cpl As New Cplex()


        '_________________________________________________________________________
        '/////////////////////////////////////////////////////////////////////////
        'INPUT PARAMETERS
        Dim p As Integer    'period
        Dim t As Integer    'tech
        Dim u As Integer    'endUse '; 0 = electricity; 1 = heating


        '       //INDEX DECLARATIONS
        Dim Tech() As String = {"HP", "Boiler", "CHP", _
                                "PV1", "PV2", "PV3", "PV4", "PV5"}
        Dim Period As Integer = _period '1 hour periods
        Dim EndUse As Integer = 2       '0 = electricity; 1 = heating; ......2 = cooling

        '       //SOLAR POTENTIALS
        'Dim LoadSolarData As String = _YourInputPath & "\SolarData.xls"
        'Dim Solar1 As Double() = OpenExcelGetData(LoadSolarData, Period, "A")    'average solar radiation in each period (W/m2)
        'Dim Solar2 As Double() = OpenExcelGetData(LoadSolarData, Period, "B")
        'Dim Solar3 As Double() = OpenExcelGetData(LoadSolarData, Period, "C")
        'Dim Solar4 As Double() = OpenExcelGetData(LoadSolarData, Period, "D")
        'Dim Solar5 As Double() = OpenExcelGetData(LoadSolarData, Period, "E")
        Dim Solar1 As Double() = _solarPot(0).ToArray
        Dim Solar2 As Double() = _solarPot(1).ToArray
        Dim Solar3 As Double() = _solarPot(2).ToArray
        Dim Solar4 As Double() = _solarPot(3).ToArray
        Dim Solar5 As Double() = _solarPot(4).ToArray

        '       //DEMAND DATA
        'Dim LoadDemandData As String = _YourInputPath & "\Demand.xls"
        'Dim abc() As String = {"A", "B", "C", "D", "E", "F", "G", "H", "I"}
        Dim Demand(EndUse)() As Double                                      'Demand [kWh]
        'For u = 0 To EndUse - 1                                                 'in ILOG IDE: float Demand[Period][EndUse] , here Demand(EndUse)(Period)
        '    Demand(u) = New Double(Period - 1) {}
        '    Demand(u) = OpenExcelGetData(LoadDemandData, Period, abc(u))
        'Next
        Demand(0) = _elec



        Demand(1) = _heating
        Demand(2) = _cooling
        Dim maxCoolDemand As Double = 0
        For p = 0 To Period - 1
            If Demand(2)(p) > maxCoolDemand Then
                maxCoolDemand = Demand(2)(p)
            End If


            Demand(0)(p) = Demand(0)(p) + (((1 / 3) * Demand(2)(p)))
        Next



        '       //TECHNOLOGY DATA
        Dim MaxCap() As Double = {50, 100, 50, 10, 10, 10, 10, 10}               'Maximum capacity (kW)
        Dim CapCost() As Double = {1000, 200, 1500, 300, 300, 300, 300, 300}     'Capital cost (chf per kW)
        Dim OMFCost() As Double = {0, 0, 0, 0, 0, 0, 0, 0}                   'Fixed O&M cost (chf per kW)
        Dim OMVCost() As Double = {0.1, 0.01, 0.021, 0.06, 0.06, 0.06, 0.06, 0.06}    'Variable O&M cost (chf per kWh)
        Dim Efficiency() As Double = {3.2, 0.94, 0.3, 0.18, 0.18, 0.18, 0.18, 0.18}   'Generation efficiency (%)
        Dim Lifetime() As Double = {20, 30, 20, 20, 20, 20, 20, 20}             'Unit lifetime (years)
        Dim htp() As Double = {1, 1, 1.73, 0, 0, 0, 0, 0}                    'heat to power ratio
        Dim MinLoad() As Double = {0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0}         'Minimum acceptable load (%)
        Dim Appl() As Double = {2, 3, 5, 11, 12, 13, 14, 15}                    'Grouping of technologies
        Dim CO2EF() As Double = {0, 0.198, 0.198, 0, 0, 0, 0, 0}             'technology CO2 emission factor (based on fuel type)
        Dim Area() As Double = {0, 0, 0, 10, 10, 10, 10, 10}                      'Area of solar panel

        '       //Battery Data
        Dim CostBat As Double = 350                                 'Battery capital cost per kWh
        Dim LifeBat As Double = 20                                  'Battery lifetime
        Dim ch_eff As Double = 0.92                                  'Battery charging efficiency
        Dim disch_eff As Double = 0.92                               'Battery discharging efficiency
        Dim decay As Double = 0.001                                 'Battery hourly decay
        Dim max_ch As Double = 0.3                                  'Battery max charging rate
        Dim max_disch As Double = 0.33                              'Battery max discharging rate
        Dim min_state As Double = 0.3                               'Battery minimum state of charge

        '       //Thermal Energy Storage Tank Data
        Dim CostTES As Double = 100                                 'TES capital cost (chf/kW)
        Dim LifeTES As Double = 17                                  'TES lifetime
        Dim ceff_TES As Double = 0.9                                'TES charging efficiency
        Dim deff_TES As Double = 0.9                                'TES discharging efficiency
        Dim heatloss As Double = 0.001                              'TES heat loss
        Dim maxch_TES As Double = 0.25                              'TES max charging rate
        Dim maxdisch_TES As Double = 0.25                           'TES max discharging rate

        '       //SOLAR DATA
        Dim MaxRoof1 As Double = _maxFacadeArea(0)              'Maximum available roof area
        Dim MaxRoof2 As Double = _maxFacadeArea(1)
        Dim MaxRoof3 As Double = _maxFacadeArea(2)
        Dim MaxRoof4 As Double = _maxFacadeArea(3)
        Dim MaxRoof5 As Double = _maxFacadeArea(4)                               'Roof

        Dim OtherModEff As Double = 1                            'Cabling efficiency (85-95%), inverter efficiency (95-99%), and temperature efficiency (90-95%)

        '       //ECONOMIC DATA
        Dim LoadEpriceData As String = _YourInputPath & "\ElecBuySell.xls"
        Dim eprice As Double() = OpenExcelGetData(LoadEpriceData, Period, "A")
        'Dim eprice As Double = 0.24                                 'electricity price
        Dim gprice As Double = 0.09                                 'gas price
        Dim esell As Double = 0.14                                  'Feed in tarrif
        Dim intrate As Double = 0.08                                'interest rate

        Dim Annuity(Tech.Length - 1) As Double
        For i As Integer = 0 To Tech.Length - 1
            Annuity(i) = intrate / (1 - (1 / ((1 + intrate) ^ (Lifetime(i)))))         'Annuity factor for each tehnology
        Next
        Dim AnnuityTES As Double = intrate / (1 - (1 / ((2 + intrate) ^ (LifeTES))))    'Annuity factor for the TES
        Dim AnnuityBat As Double = intrate / (1 - (1 / ((1 + intrate) ^ (LifeBat))))    'Annuity factor for the battery

        '       //EMISSIONS/ENVIRONMENTAL DATA
        Dim GridCO2EF As Double = 0.594                     'Grid CO2 Emission Factor
        Dim NGCO2EF As Double = 0.237                       'Natural Gas CO2 Emission Factor
        Dim CO2Limit As Double = _co2reduc                  'Target emissions reduction (%)
        Dim hdump As Double = 1                             'Heat dump limit (%)
        Dim RefBoilerEff As Double = 0.94                   'Reference boiler efficiency
        Dim RefACEff As Double = 3


        '       //MISC INPUT PARAMETERS
        'Ability of each technology to generate each end use. ()(0) == 1 -> electricity; ()(1) == 1 -> heat
        Dim Suit(Tech.Length - 1)() As Integer
        For i = 0 To Tech.Length - 1
            Suit(i) = New Integer(EndUse - 1) {}
        Next
        Suit(0) = {0, 1}
        Suit(1) = {0, 1}
        Suit(2) = {1, 1}
        Suit(3) = {1, 0}
        Suit(4) = {1, 0}
        Suit(5) = {1, 0}
        Suit(6) = {1, 0}
        Suit(7) = {1, 0}
        Dim M1 As Double = 100000000                        'Arbitrary large number

        '/////////////////////////////////////////////////////////////////////////
        '_________________________________________________________________________










        '_________________________________________________________________________
        '/////////////////////////////////////////////////////////////////////////
        'MODEL DECLARATIONS

        '//DECISION VARIABLES
        Dim Unit As INumVar() = cpl.IntVarArray(Tech.Length, 0, Integer.MaxValue)   'Number of units of each discrete tehnology that are purchased
        Dim ElecPur As INumVar() = cpl.NumVarArray(Period, 0.0, System.Double.MaxValue) 'Electricity purchased from grid during each time period
        Dim ElecSell As INumVar() = cpl.NumVarArray(Period, 0.0, System.Double.MaxValue) 'Excess electricity sold back to the grid during every time period

        'Number of units operating in each time interval
        Dim Operate(Period - 1)() As INumVar
        'Gen[Period][Tech][EndUse]; //Energy generation by each technology durig each time period
        Dim Gen()()() As INumVar
        Gen = New INumVar(Period - 1)()() {}
        'Excess heat that is dumped by CHP
        Dim dump(Period - 1)() As INumVar
        For p = 0 To Period - 1
            Operate(p) = cpl.IntVarArray(Tech.Length, 0.0, Integer.MaxValue)
            dump(p) = cpl.NumVarArray(Tech.Length, 0.0, System.Double.MaxValue)
            Gen(p) = New INumVar(Tech.Length - 1)() {}
            For t = 0 To Tech.Length - 1
                Gen(p)(t) = New INumVar(EndUse - 1) {}
                For u = 0 To EndUse - 1
                    Gen(p)(t)(u) = cpl.NumVar(0.0, System.Double.MaxValue)
                Next u
            Next t
        Next p

        Dim y1 As INumVar() = cpl.BoolVarArray(Period)      'binary control variable

        Dim BatCap As INumVar = cpl.NumVar(0.0, System.Double.MaxValue) 'Battery capacity installed
        Dim Charge_Bat As INumVar() = cpl.NumVarArray(Period, 0.0, System.Double.MaxValue)  'Electricity used to charge the battery (kWh)
        Dim Discharge_Bat As INumVar() = cpl.NumVarArray(Period, 0.0, System.Double.MaxValue) 'Electricity discharged from the battery (kWh)
        Dim Stored_Bat As INumVar() = cpl.NumVarArray(Period, 0.0, System.Double.MaxValue) 'Electricity currently stored in the battery (kWh)

        Dim TESCap As INumVar = cpl.NumVar(0.0, System.Double.MaxValue) 'TES capacity installed
        Dim Charge_TES As INumVar() = cpl.NumVarArray(Period, 0.0, System.Double.MaxValue) 'Electricity used to charge the TES (kWh)
        Dim Discharge_TES As INumVar() = cpl.NumVarArray(Period, 0.0, System.Double.MaxValue) 'Electricity discharged from the TES (kWh)
        Dim Stored_TES As INumVar() = cpl.NumVarArray(Period, 0.0, System.Double.MaxValue) 'Electricity currently stored in the TES (kWh)

        'Dim CO2Emissions As INumVar = cpl.NumVar(0.0, System.Double.MaxValue) 'Total CO2 emissions fot the optimal energy system over the entire time horizon (kgCO2)


        ' OBJECTIVE FUNCTION DECOMPOSED BY TYPE OF COST
        Dim CapCostTech As ILinearNumExpr = cpl.LinearNumExpr() 'Annual capital cost of technologies
        Dim OMFCostTotal As ILinearNumExpr = cpl.LinearNumExpr()    'Fixed O&M cost of technologies

        For t = 0 To Tech.Length - 1
            CapCostTech.AddTerm((MaxCap(t) * Annuity(t) * CapCost(t)), Unit(t))
            OMFCostTotal.AddTerm((MaxCap(t) * OMFCost(t)), Unit(t))
        Next

        Dim CapCostTES As INumExpr = cpl.Prod((AnnuityTES * CostTES), TESCap)  'Annual capital cost of the TES 
        Dim CapCostBattery As INumExpr = cpl.Prod((AnnuityBat * CostBat), BatCap)  'Annual capital cost of the battery 

        Dim OMVCostTotal As ILinearNumExpr = cpl.LinearNumExpr()    'Variable O&M cost of technologies
        Dim NGCost1 As ILinearNumExpr = cpl.LinearNumExpr()         'Cost of natural gas consumed by CHP
        Dim NGCost2 As ILinearNumExpr = cpl.LinearNumExpr()         'Cost of natural gas consumed by boiler
        Dim ElecCost As ILinearNumExpr = cpl.LinearNumExpr()        'Cost of electricty purchased
        Dim ElecSold As ILinearNumExpr = cpl.LinearNumExpr()        'Cost of electricty sold back to grid









        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Dim acCostOMFTotal, acCostOMVTotal, acCostCap As Double
        Dim acCostOMF As Double = 0
        Dim acCostOMV As Double = 0.01
        Dim acCap As Double = 5 '5kw Capacity per AC unit
        Dim acCostPerKW As Double = 360 'CHF per KW
        Dim acLifetime As Integer = 20
        Dim acAnnuity As Double
        acAnnuity = intrate / (1 - (1 / ((1 + intrate) ^ (acLifetime))))
        acCostOMFTotal = 0
        acCostOMVTotal = 0
        acCostCap = Math.Ceiling((maxCoolDemand / acCap)) * (acCostPerKW * acCap) * acAnnuity

        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!








        For p = 0 To Period - 1
            For t = 0 To Tech.Length - 1
                For u = 0 To EndUse - 1
                    OMVCostTotal.AddTerm(OMVCost(t), Gen(p)(t)(u))
                    If Appl(t) = 5 And u = 0 Then
                        NGCost1.AddTerm((gprice / Efficiency(t)), Gen(p)(t)(u))
                    ElseIf Appl(t) = 3 And u = 1 Then
                        NGCost2.AddTerm((gprice / Efficiency(t)), Gen(p)(t)(u))
                    End If
                Next
            Next
            ElecCost.AddTerm(eprice(p), ElecPur(p))
            ElecSold.AddTerm(esell, ElecSell(p))



            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            acCostOMFTotal = acCostOMFTotal + acCostOMF * Demand(2)(p)
            acCostOMVTotal = acCostOMVTotal + acCostOMV * Demand(2)(p)

            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Next
        '/////////////////////////////////////////////////////////////////////////
        '_________________________________________________________________________








        '_________________________________________________________________________
        '/////////////////////////////////////////////////////////////////////////
        'MODEL

        '//OBJECTIVE FUNCTION

        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        cpl.AddMinimize(cpl.Diff(cpl.Sum(cpl.Sum(CapCostTech, acCostCap), CapCostTES, CapCostBattery, _
                                  cpl.Sum(OMFCostTotal, acCostOMFTotal), cpl.Sum(OMVCostTotal, acCostOMVTotal), _
                                  NGCost1, NGCost2, ElecCost), ElecSold))

        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!





        '//CONSTRAINTS
        '  //ENERGY BALANCE CONSTRAINTS 
        ' Dim acElecDemand As Double = 0
        For p = 0 To Period - 1
            '  //Electricity generated plus electricity purchased must be greater than or equal electricity demand
            Dim elecgenExpr As ILinearNumExpr = cpl.LinearNumExpr()
            Dim elecdemandExpr As ILinearNumExpr = cpl.LinearNumExpr()
            For t = 0 To Tech.Length - 1
                elecgenExpr.AddTerm(Gen(p)(t)(0), Suit(t)(0))
                If Tech(t) = "HP" Then
                    elecdemandExpr.AddTerm((1 / Efficiency(t)), Gen(p)(t)(1))
                    'ElseIf Tech(t) = "AC" Then
                    '    elecdemandExpr.AddTerm((1 / Efficiency(t)), Gen(p)(t)(2))
                End If

                '  //OPERATION CONSTRAINTS
                'Number of units operating in each time interval has to be less than or equal to the number of units that were purchased
                cpl.AddLe(Operate(p)(t), Unit(t))
            Next
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            'acElecDemand = ((1 / RefACEff) * Demand(2)(p))

            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            elecgenExpr.AddTerm(1, ElecPur(p))
            elecgenExpr.AddTerm(disch_eff, Discharge_Bat(p))
            elecdemandExpr.AddTerm((1 / ch_eff), Charge_Bat(p))
            elecdemandExpr.AddTerm(1, ElecSell(p))
            ' cpl.AddEq(elecgenExpr, cpl.Sum(Demand(0)(p), cpl.Sum(elecdemandExpr, acElecDemand)))
            cpl.AddEq(elecgenExpr, cpl.Sum(Demand(0)(p), elecdemandExpr))



            '  //Heat generated must be greater than or equal heat demand
            Dim heatgenExpr As ILinearNumExpr = cpl.LinearNumExpr()
            Dim heatdemandExpr As ILinearNumExpr = cpl.LinearNumExpr()
            For t = 0 To Tech.Length - 1
                heatgenExpr.AddTerm(Gen(p)(t)(1), Suit(t)(1))
            Next
            heatgenExpr.AddTerm(deff_TES, Discharge_TES(p))
            heatdemandExpr.AddTerm((1 / ceff_TES), Charge_TES(p))
            cpl.AddEq(heatgenExpr, cpl.Sum(Demand(1)(p), heatdemandExpr))

            ''  //Cooling generated must be greater than or equal cooling demand
            'Dim coolgenExpr As ILinearNumExpr = cpl.LinearNumExpr()
            'For t = 0 To Tech.Length - 1
            '    coolgenExpr.AddTerm(Gen(p)(t)(2), Suit(t)(2))
            'Next
            'cpl.AddEq(coolgenExpr, Demand(2)(p))

        Next


        For p = 0 To Period - 1
            '  //SELLING and BUYING ELECTRICITY 
            cpl.AddLe(ElecPur(p), cpl.Prod(M1, y1(p)))
            cpl.AddLe(ElecSell(p), cpl.Prod(M1, cpl.Sum(1, cpl.Prod(-1, y1(p)))))

            For t = 0 To Tech.Length - 1
                If Appl(t) = 5 Then
                    '  //CAPACITY CONSTRAINTS AND TECHNOLOGY MODELS
                    '  //CHP
                    Dim minchpExpr As ILinearNumExpr = cpl.LinearNumExpr()
                    Dim maxchpExpr As ILinearNumExpr = cpl.LinearNumExpr()
                    minchpExpr.AddTerm((MaxCap(t) * Suit(t)(0) * MinLoad(t)), Operate(p)(t))
                    maxchpExpr.AddTerm((MaxCap(t) * Suit(t)(0)), Operate(p)(t))
                    cpl.AddGe(Gen(p)(t)(0), minchpExpr)
                    cpl.AddLe(Gen(p)(t)(0), maxchpExpr)

                    '  //Heat Recovery from CHP units is less than or equal to electricity generation by CHP units times heat to power ratio of each unit
                    Dim heatrecovchpExpr As ILinearNumExpr = cpl.LinearNumExpr()
                    Dim elecgenhtpchpExpr As ILinearNumExpr = cpl.LinearNumExpr()
                    heatrecovchpExpr.AddTerm(1, Gen(p)(t)(1))
                    heatrecovchpExpr.AddTerm(1, dump(p)(t))
                    elecgenhtpchpExpr.AddTerm(htp(t), Gen(p)(t)(0))
                    cpl.AddEq(heatrecovchpExpr, elecgenhtpchpExpr)

                    '  //Limiting the amount of heat that chps can dump
                    cpl.AddLe(dump(p)(t), cpl.Prod(hdump, Gen(p)(t)(1)))    'HeatDump

                Else
                    '  //Limiting the amount of heat that chps can dump
                    cpl.AddEq(dump(p)(t), 0)                                'HeatDump2
                End If
            Next
        Next

        '  //BOILERS
        For p = 0 To Period - 1
            For t = 0 To Tech.Length - 1
                If Appl(t) = 3 Then
                    For u = 0 To EndUse - 1
                        Dim minbExpr As ILinearNumExpr = cpl.LinearNumExpr()
                        Dim maxbExpr As ILinearNumExpr = cpl.LinearNumExpr()
                        minbExpr.AddTerm((MaxCap(t) * Suit(t)(u) * MinLoad(t)), Operate(p)(t))
                        maxbExpr.AddTerm((MaxCap(t) * Suit(t)(u)), Operate(p)(t))
                        cpl.AddGe(Gen(p)(t)(u), minbExpr)
                        cpl.AddLe(Gen(p)(t)(u), maxbExpr)
                    Next
                End If
            Next
        Next


        '  //PV1
        For p = 0 To Period - 1
            Dim solarareaExpr1 As ILinearNumExpr = cpl.LinearNumExpr()
            For t = 0 To Tech.Length - 1
                If Appl(t) = 11 Then
                    solarareaExpr1.AddTerm(Area(t), Unit(t))
                    For u = 0 To EndUse - 1
                        Dim solarcapExpr As ILinearNumExpr = cpl.LinearNumExpr()
                        solarcapExpr.AddTerm((Area(t) * (Solar1(p) / 1000) * _
                                              Efficiency(t) * OtherModEff * Suit(t)(u)), _
                                               Unit(t))
                        cpl.AddEq(Gen(p)(t)(u), solarcapExpr)
                    Next
                End If
            Next
            cpl.AddLe(solarareaExpr1, MaxRoof1)
        Next
        '  //PV2
        For p = 0 To Period - 1
            Dim solarareaExpr2 As ILinearNumExpr = cpl.LinearNumExpr()
            For t = 0 To Tech.Length - 1
                If Appl(t) = 12 Then
                    solarareaExpr2.AddTerm(Area(t), Unit(t))
                    For u = 0 To EndUse - 1
                        Dim solarcapExpr As ILinearNumExpr = cpl.LinearNumExpr()
                        solarcapExpr.AddTerm((Area(t) * (Solar2(p) / 1000) * _
                                              Efficiency(t) * OtherModEff * Suit(t)(u)), _
                                               Unit(t))
                        cpl.AddEq(Gen(p)(t)(u), solarcapExpr)
                    Next
                End If
            Next
            cpl.AddLe(solarareaExpr2, MaxRoof2)
        Next
        '  //PV3
        For p = 0 To Period - 1
            Dim solarareaExpr3 As ILinearNumExpr = cpl.LinearNumExpr()
            For t = 0 To Tech.Length - 1
                If Appl(t) = 13 Then
                    solarareaExpr3.AddTerm(Area(t), Unit(t))
                    For u = 0 To EndUse - 1
                        Dim solarcapExpr As ILinearNumExpr = cpl.LinearNumExpr()
                        solarcapExpr.AddTerm((Area(t) * (Solar3(p) / 1000) * _
                                              Efficiency(t) * OtherModEff * Suit(t)(u)), _
                                               Unit(t))
                        cpl.AddEq(Gen(p)(t)(u), solarcapExpr)
                    Next
                End If
            Next
            cpl.AddLe(solarareaExpr3, MaxRoof3)
        Next
        '  //PV4
        For p = 0 To Period - 1
            Dim solarareaExpr4 As ILinearNumExpr = cpl.LinearNumExpr()
            For t = 0 To Tech.Length - 1
                If Appl(t) = 14 Then
                    solarareaExpr4.AddTerm(Area(t), Unit(t))
                    For u = 0 To EndUse - 1
                        Dim solarcapExpr As ILinearNumExpr = cpl.LinearNumExpr()
                        solarcapExpr.AddTerm((Area(t) * (Solar4(p) / 1000) * _
                                              Efficiency(t) * OtherModEff * Suit(t)(u)), _
                                               Unit(t))
                        cpl.AddEq(Gen(p)(t)(u), solarcapExpr)
                    Next
                End If
            Next
            cpl.AddLe(solarareaExpr4, MaxRoof4)
        Next
        '  //PV5
        For p = 0 To Period - 1
            Dim solarareaExpr5 As ILinearNumExpr = cpl.LinearNumExpr()
            For t = 0 To Tech.Length - 1
                If Appl(t) = 15 Then
                    solarareaExpr5.AddTerm(Area(t), Unit(t))
                    For u = 0 To EndUse - 1
                        Dim solarcapExpr As ILinearNumExpr = cpl.LinearNumExpr()
                        solarcapExpr.AddTerm((Area(t) * (Solar5(p) / 1000) * _
                                              Efficiency(t) * OtherModEff * Suit(t)(u)), _
                                               Unit(t))
                        cpl.AddEq(Gen(p)(t)(u), solarcapExpr)
                    Next
                End If
            Next
            cpl.AddLe(solarareaExpr5, MaxRoof5)
        Next


        '  //HP
        For p = 0 To Period - 1
            For t = 0 To Tech.Length - 1
                If Appl(t) = 2 Then
                    For u = 0 To EndUse - 1
                        cpl.AddGe(Gen(p)(t)(u), cpl.Prod(Operate(p)(t), (MaxCap(t) * Suit(t)(u) * MinLoad(t))))
                        cpl.AddLe(Gen(p)(t)(u), cpl.Prod(Operate(p)(t), (MaxCap(t) * Suit(t)(u))))
                    Next
                End If
            Next
        Next

        ''  //AC
        'For p = 0 To Period - 1
        '    For t = 0 To Tech.Length - 1
        '        If Appl(t) = 20 Then
        '            For u = 0 To EndUse - 1
        '                cpl.AddGe(Gen(p)(t)(u), cpl.Prod(Operate(p)(t), (MaxCap(t) * Suit(t)(u) * MinLoad(t))))
        '                cpl.AddLe(Gen(p)(t)(u), cpl.Prod(Operate(p)(t), (MaxCap(t) * Suit(t)(u))))
        '            Next
        '        End If
        '    Next
        'Next


        '  //BATTERY MODEL
        For p = 1 To Period - 1
            Dim batstateExpr As ILinearNumExpr = cpl.LinearNumExpr()    'batstate
            batstateExpr.AddTerm((1 - decay), Stored_Bat(p - 1))
            batstateExpr.AddTerm(1, Charge_Bat(p))
            batstateExpr.AddTerm(-1, Discharge_Bat(p))
            cpl.AddEq(Stored_Bat(p), batstateExpr)
        Next

        cpl.AddEq(Stored_Bat(0), cpl.Prod(BatCap, min_state))           'initial state of battery
        cpl.AddEq(Discharge_Bat(0), 0)                                  'initial discharging of battery

        For p = 0 To Period - 1
            cpl.AddGe(Stored_Bat(p), cpl.Prod(BatCap, min_state))       'min state of charge
            cpl.AddLe(Charge_Bat(p), cpl.Prod(BatCap, max_ch))          'battery charging
            cpl.AddLe(Discharge_Bat(p), cpl.Prod(BatCap, max_disch))    'battery discharging
            cpl.AddLe(Stored_Bat(p), BatCap)                            'battery sizing
        Next

        '  //TES Model
        For p = 1 To Period - 1
            Dim tesstateExpr As ILinearNumExpr = cpl.LinearNumExpr()    'TESstate
            tesstateExpr.AddTerm((1 - heatloss), Stored_TES(p - 1))
            tesstateExpr.AddTerm(1, Charge_TES(p))
            tesstateExpr.AddTerm(-1, Discharge_TES(p))
            cpl.AddEq(Stored_TES(p), tesstateExpr)
        Next
        cpl.AddEq(Stored_TES(0), Stored_TES(Period - 1))                'TESstate0
        cpl.AddEq(Discharge_TES(0), 0)                                  'TESdischarging0

        For p = 0 To Period - 1
            cpl.AddLe(Charge_TES(p), cpl.Prod(TESCap, maxch_TES))       'TEScharging
            cpl.AddLe(Discharge_TES(p), cpl.Prod(TESCap, maxdisch_TES)) 'TESdischarging
            cpl.AddLe(Stored_TES(p), TESCap)                            'TESsizing
        Next

        '  //EMISSIONS TARGETS
        'Dim totalemsExpr As ILinearNumExpr = cpl.LinearNumExpr()
        Dim CO2Emissions As ILinearNumExpr = cpl.LinearNumExpr()
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Dim acEms As Double = 0

        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        For p = 0 To Period - 1
            CO2Emissions.AddTerm(GridCO2EF, ElecPur(p))
            'totalemsExpr.AddTerm(GridCO2EF, ElecPur(p))
            For t = 0 To Tech.Length - 1
                For u = 0 To EndUse - 1
                    If Tech(t) <> "CHP" Then
                        'totalemsExpr.AddTerm((CO2EF(t) / Efficiency(t)), Gen(p)(t)(u))
                        CO2Emissions.AddTerm((CO2EF(t) / Efficiency(t)), Gen(p)(t)(u))
                    ElseIf u = 0 And Tech(t) = "CHP" Then
                        'totalemsExpr.AddTerm((CO2EF(t) / Efficiency(t)), Gen(p)(t)(u))
                        CO2Emissions.AddTerm((CO2EF(t) / Efficiency(t)), Gen(p)(t)(u))
                    End If
                Next
            Next

            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            'Add AC emmissions, which are not variable yet
            acEms = acEms + (GridCO2EF * Demand(2)(p) / RefACEff)

            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Next

        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        'cpl.AddEq(CO2Emissions, cpl.Sum(totalemsExpr, acEms))

        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Dim emlimit As Double = 0
        For p = 0 To Period - 1
            emlimit = emlimit + Demand(0)(p) * GridCO2EF
            emlimit = emlimit + (NGCO2EF * Demand(1)(p) / RefBoilerEff)
            ' emlimit = emlimit + (GridCO2EF * Demand(2)(p) / RefACEff)   'its already in demand(0), coz I hadded demand(2) to that before
        Next
        emlimit = emlimit * (1 - CO2Limit)
        cpl.AddLe(cpl.Sum(CO2Emissions, acEms), emlimit)

        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!




        '/////////////////////////////////////////////////////////////////////////
        '_________________________________________________________________________





        cpl.SetParam(Cplex.Param.MIP.Tolerances.MIPGap, 0.05)

        '_________________________________________________________________________
        '/////////////////////////////////////////////////////////////////////////
        'SOLVE
        If cpl.Solve() Then

            System.Console.WriteLine(("Solution status = " + cpl.GetStatus().ToString))
            System.Console.WriteLine()
            System.Console.WriteLine(("Total Cost = " & cpl.ObjValue))

            'read result values
            System.Console.WriteLine()
            For t = 0 To Tech.Length - 1
                System.Console.WriteLine(Tech(t) + " capacity:" + ControlChars.Tab _
                                           + CStr(cpl.GetValue(Unit(t))))
            Next


            _solved = True
            _ObjVal = cpl.ObjValue
            _cap = New Double(Tech.Length - 1) {}
            For t = 0 To Tech.Length - 1
                _cap(t) = cpl.GetValue(Unit(t))
            Next


            _dump = New Double(Period - 1)() {}
            _epur = New Double(Period - 1) {}
            _esell = New Double(Period - 1) {}
            _gen = New Double(Period - 1)()() {}
            _op = New Double(Period - 1)() {}
            _Charge_Bat = New Double(Period - 1) {}
            _Discharge_Bat = New Double(Period - 1) {}
            _Stored_Bat = New Double(Period - 1) {}
            _Charge_TES = New Double(Period - 1) {}
            _Discharge_TES = New Double(Period - 1) {}
            _Stored_TES = New Double(Period - 1) {}
            For p = 0 To Period - 1
                _gen(p) = New Double(Tech.Length - 1)() {}
                _op(p) = New Double(Tech.Length - 1) {}
                _dump(p) = New Double(Tech.Length - 1) {}
                For t = 0 To Tech.Length - 1
                    _gen(p)(t) = New Double(EndUse - 1) {}
                    _op(p)(t) = cpl.GetValue(Operate(p)(t))
                    _dump(p)(t) = cpl.GetValue(dump(p)(t))
                    For u = 0 To EndUse - 1
                        _gen(p)(t)(u) = cpl.GetValue(Gen(p)(t)(u))
                    Next
                Next
                _epur(p) = cpl.GetValue(ElecPur(p))
                _esell(p) = cpl.GetValue(ElecSell(p))
                _Charge_Bat(p) = cpl.GetValue(Charge_Bat(p))
                _Discharge_Bat(p) = cpl.GetValue(Discharge_Bat(p))
                _Stored_Bat(p) = cpl.GetValue(Stored_Bat(p))
                _Charge_TES(p) = cpl.GetValue(Charge_TES(p))
                _Discharge_TES(p) = cpl.GetValue(Discharge_TES(p))
                _Stored_TES(p) = cpl.GetValue(Stored_TES(p))
            Next
            _BatCap = cpl.GetValue(BatCap)
            _TESCap = cpl.GetValue(TESCap)
            _CO2Emissions = cpl.GetValue(cpl.Sum(CO2Emissions, acEms))
        Else
            _solved = False
            _ObjVal = Nothing
            _gen = Nothing
            _cap = Nothing
            _op = Nothing
        End If



        cpl.End()
        '/////////////////////////////////////////////////////////////////////////
        '_________________________________________________________________________
    End Sub


    Friend Function OpenExcelGetData(ByVal fileNameAndPath As String, ByVal rowIndex As Integer, _
                                   ByVal columnIndex As String) As Double()
        Dim oExcelApp As New excel.Application
        Dim oExcelBook As excel.Workbook
        Dim oExcelSheet As excel.Worksheet
        Dim sheetNumber As Integer = 1

        Dim oData(rowIndex - 1) As Double

        Try
            oExcelBook = oExcelApp.Workbooks.Open(fileNameAndPath)
            oExcelSheet = CType(oExcelBook.Worksheets(sheetNumber), excel.Worksheet)

            'Read data
            Dim excelRange As String = columnIndex & rowIndex.ToString()
            For i As Integer = 1 To rowIndex
                excelRange = columnIndex & i.ToString()
                oData(i - 1) = oExcelSheet.Range(excelRange).Value
            Next

            oExcelApp.Workbooks.Close()
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

        Return oData
    End Function

End Class
