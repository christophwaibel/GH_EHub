Imports System.Collections.Generic

Imports Grasshopper.Kernel
Imports Rhino.Geometry
Imports Grasshopper.Kernel.Types

Public Class GHCaseB
    Inherits GH_Component
    ''' <summary>
    ''' Initializes a new instance of the GHCaseB class.
    ''' </summary>
    Public Sub New()
        MyBase.New("CaseB - EHub", "CaseB-EHub", _
                    "Case B - EHub MILP Solver, using n-Solar Potentials and 3 Demands (heating, cooling, electricity). Demands and potentials will be merged to 12 average days.", _
                    "EnergyHubs", "Examples")
    End Sub

    ''' <summary>
    ''' Registers all the input parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterInputParams(pManager As GH_Component.GH_InputParamManager)
        pManager.AddNumberParameter("Cooling Demand", "Cooling D", "Cooling Demand, in [W/h], 8760 numbers", GH_ParamAccess.list)
        pManager.AddNumberParameter("Heating Demand", "Heating D", "Heatin Demand, in [W/h], 8760 numbers", GH_ParamAccess.list)
        pManager.AddNumberParameter("Electricity Demand", "Elec D", "Electricity Demand, in [W/h], 8760 numbers", GH_ParamAccess.list)
        pManager.AddGenericParameter("Solar Potentials", "Solar Pot.", "Solar Potentials, in [W/m2]. List of (list of(double))", GH_ParamAccess.tree)
        pManager.AddNumberParameter("Max Solar Areas", "Max Solar Areas", "List of max Solar areas for PV, in [m2]", GH_ParamAccess.list)

        pManager.AddBooleanParameter("Run Solver!", "blnRun", "Start the Solver", GH_ParamAccess.item)
        pManager.AddIntegerParameter("Horizon", "intHorizon", "Time Horizon in hours, max 288 (12 days).", GH_ParamAccess.item)
        pManager.AddTextParameter("Input Path", "strInputPath", "Input Path containing data", GH_ParamAccess.item)
        pManager.AddNumberParameter("CO2 Reduction Target", "dblCO2Reduc", "CO2 reduction target in % (0 - 1)", GH_ParamAccess.item)
        pManager.AddNumberParameter("Tot Area", "dblTotArea", "Total Floor Area of all buildings in m2", GH_ParamAccess.item)

        pManager.AddBooleanParameter("CO2 trading", "blnCO2Trade", "Allow CO2 trading into the grid? Yes or No", GH_ParamAccess.item)
    End Sub

    ''' <summary>
    ''' Registers all the output parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterOutputParams(pManager As GH_Component.GH_OutputParamManager)
        pManager.AddBooleanParameter("Solved?", "blnSolved", "Has the solver found a solution?", GH_ParamAccess.item)
        pManager.AddNumberParameter("Objective Value", "dblCost", "Objective function value", GH_ParamAccess.item)
        pManager.AddNumberParameter("Units of Technology", "intUnits(t)", "No Units of Technologies", GH_ParamAccess.list)
        pManager.AddGenericParameter("Generation Matrix Electricity", "mtGeneration(p)(t)(el)", "Generation schedule of technologies for electricity over entire horizon", GH_ParamAccess.item)
        pManager.AddGenericParameter("Generation Matrix Heating", "mtGeneration(p)(t)(ht)", "Generation schedule of technologies for heating over entire horizon", GH_ParamAccess.item)
        pManager.AddGenericParameter("Operation Matrix", "mtOperation(p)(t)", "Operation schedule of technologies over entire horizon", GH_ParamAccess.item)
        pManager.AddNumberParameter("Battery Capacity", "dblBatCap", "Installed battery capacity", GH_ParamAccess.item)
        pManager.AddNumberParameter("Charge Battery", "dblCharge_Bat", "charging schedule of battery", GH_ParamAccess.list)
        pManager.AddNumberParameter("Discharge Battery", "dblDischarge_Bat", "discharging schedule of battery", GH_ParamAccess.list)
        pManager.AddNumberParameter("Stored Battery", "dblStored_Bat", "storing schedule of battery", GH_ParamAccess.list)

        pManager.AddNumberParameter("TES Capacity", "dblTESCap", "Installed Thermal Energy Storage capacity", GH_ParamAccess.item)
        pManager.AddNumberParameter("Charge TES", "dblCharge_TES", "charging schedule of Thermal Energy Storage", GH_ParamAccess.list)
        pManager.AddNumberParameter("Discharge TES", "dblDischarge_TES", "discharging schedule of Thermal Energy Storage", GH_ParamAccess.list)
        pManager.AddNumberParameter("Stored TES", "dblStored_TES", "storing schedule of Thermal Energy Storage", GH_ParamAccess.list)

        pManager.AddNumberParameter("CO2", "dblCO2", "CO2 Emissions", GH_ParamAccess.item)

        pManager.AddGenericParameter("Dump", "mtDump", "dumped heat per technology", GH_ParamAccess.item)
        pManager.AddNumberParameter("ElecPur", "dblEpur", "schedule of electricity purchased", GH_ParamAccess.list)
        pManager.AddNumberParameter("ElecSell", "dblEsell", "schedule of electricity sold", GH_ParamAccess.list)

        'pManager.AddGenericParameter("Generation Matrix Cooling", "mtGeneration(p)(t)(cl)", "Generation schedule of technologies for cooling over entire horizon", GH_ParamAccess.item)

        pManager.AddNumberParameter("averaged electricity demand", "dblAvgElec", "12 days averaged electricity demand", GH_ParamAccess.list)
        pManager.AddNumberParameter("averaged heating demand", "dblAvgHeat", "12 days averaged heating demand", GH_ParamAccess.list)
        pManager.AddNumberParameter("averaged cooling demand", "dblAvgCool", "12 days averaged cooling demand", GH_ParamAccess.list)

        pManager.AddNumberParameter("AC units", "AC units", "AC units, 5kW per unit", GH_ParamAccess.item)
    End Sub

    ''' <summary>
    ''' This is the method that actually does the work.
    ''' </summary>
    ''' <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    Protected Overrides Sub SolveInstance(DA As IGH_DataAccess)

        Dim coolList As New List(Of Double)
        If (Not DA.GetDataList(0, coolList)) Then Return
        Dim cool As Double() = coolList.ToArray

        Dim heatList As New List(Of Double)
        If (Not DA.GetDataList(1, heatList)) Then Return
        Dim heat As Double() = heatList.ToArray

        Dim elecList As New List(Of Double)
        If (Not DA.GetDataList(2, elecList)) Then Return
        Dim elec As Double() = elecList.ToArray


        Dim sol As Data.GH_Structure(Of IGH_Goo) = Nothing
        If (Not DA.GetDataTree(3, sol)) Then Return
        Dim solarPotList As New List(Of List(Of Double))



        For intFacade As Integer = 0 To sol.Branches.Count - 1
            solarPotList.Add(New List(Of Double))
            For Each gooNumber As IGH_Goo In sol.Branch(intFacade)
                If (gooNumber Is Nothing) Then
                    solarPotList(intFacade).Add(Double.NaN)
                Else
                    Dim number As Double = Double.NaN
                    GH_Convert.ToDouble(gooNumber, number, GH_Conversion.Both)
                    solarPotList(intFacade).Add(number)
                End If
            Next
        Next




        Dim solMaxArea As New List(Of Double)
        If (Not DA.GetDataList(4, solMaxArea)) Then Return

        'Call MsgBox("hi")


        Dim run As Boolean
        If (Not DA.GetData(5, run)) Then Return
        If run <> True Then Return

        Dim period As Integer
        If (Not DA.GetData(6, period)) Then Return

        Dim strInput As String
        If (Not DA.GetData(7, strInput)) Then Return

        Dim co2reduc As Double
        If (Not DA.GetData(8, co2reduc)) Then Return

        Dim totarea As Double
        If (Not DA.GetData(9, totarea)) Then Return

        Dim blnco2trade As Boolean
        If (Not DA.GetData(10, blnco2trade)) Then Return
        Dim ehub As CaseB_2 = New CaseB_2
        ehub.RunCaseA(strInput, period, co2reduc, _
                     solMaxArea, solarPotList, cool, heat, elec, _
                     totarea, blnco2trade)




        DA.SetData(0, ehub._solved)
        If ehub._solved = True Then
            Dim dVarCap As New List(Of Double)
            For i = 0 To ehub._cap.Length - 1
                dVarCap.Add(ehub._cap(i))
            Next

            DA.SetData(1, ehub._ObjVal)
            DA.SetDataList(2, dVarCap)


            'cuold add a slider to go through all enduse. only display that matrix of selected enduse
            'electricity
            Dim matr0 As New Matrix(ehub._gen.Length, ehub._gen(0).Length)
            Dim j As Integer
            For i = 0 To ehub._gen.Length - 1
                For j = 0 To ehub._gen(i).Length - 1
                    matr0(i, j) = ehub._gen(i)(j)(0)
                Next
            Next
            DA.SetData(3, matr0)

            'heating
            Dim matr1 As New Matrix(ehub._gen.Length, ehub._gen(0).Length)
            For i = 0 To ehub._gen.Length - 1
                For j = 0 To ehub._gen(i).Length - 1
                    matr1(i, j) = ehub._gen(i)(j)(1)
                Next
            Next
            DA.SetData(4, matr1)

            'cooling
            'Dim matr3 As New Matrix(ehub._gen.Length, ehub._gen(0).Length)
            'For i = 0 To ehub._gen.Length - 1
            '    For j = 0 To ehub._gen(i).Length - 1
            '        'matr3(i, j) = ehub._gen(i)(j)(2)
            '        matr3(i, j) = cool(i)
            '    Next
            'Next
            'DA.SetData(18, matr3)

            'operation
            Dim matr2 As New Matrix(ehub._op.Length, ehub._op(0).Length)
            For i = 0 To ehub._op.Length - 1
                For j = 0 To ehub._op(i).Length - 1
                    matr2(i, j) = ehub._op(i)(j)
                Next
            Next
            DA.SetData(5, matr2)


            DA.SetData(6, ehub._BatCap)
            Dim Charge_Bat As New List(Of Double)
            Dim Discharge_Bat As New List(Of Double)
            Dim Stored_Bat As New List(Of Double)
            For i = 0 To ehub._Charge_Bat.Length - 1
                Charge_Bat.Add(ehub._Charge_Bat(i))
                Discharge_Bat.Add(ehub._Discharge_Bat(i))
                Stored_Bat.Add(ehub._Stored_Bat(i))
            Next
            DA.SetDataList(7, Charge_Bat)
            DA.SetDataList(8, Discharge_Bat)
            DA.SetDataList(9, Stored_Bat)

            DA.SetData(10, ehub._TESCap)
            Dim Charge_TES As New List(Of Double)
            Dim Discharge_TES As New List(Of Double)
            Dim Stored_TES As New List(Of Double)
            For i = 0 To ehub._Charge_TES.Length - 1
                Charge_TES.Add(ehub._Charge_TES(i))
                Discharge_TES.Add(ehub._Discharge_TES(i))
                Stored_TES.Add(ehub._Stored_TES(i))
            Next
            DA.SetDataList(11, Charge_TES)
            DA.SetDataList(12, Discharge_TES)
            DA.SetDataList(13, Stored_TES)

            DA.SetData(14, ehub._CO2Emissions)

            'heat dump
            Dim mtDump As New Matrix(ehub._dump.Length, ehub._dump(0).Length)
            For i = 0 To ehub._dump.Length - 1
                For j = 0 To ehub._dump(i).Length - 1
                    mtDump(i, j) = ehub._dump(i)(j)
                Next
            Next
            DA.SetData(15, mtDump)

            Dim epur As New List(Of Double)
            Dim esell As New List(Of Double)
            For i = 0 To ehub._esell.Length - 1
                epur.Add(ehub._epur(i))
                esell.Add(ehub._esell(i))
            Next
            DA.SetDataList(16, epur)
            DA.SetDataList(17, esell)

            Dim avgdaysheat As New List(Of Double)
            Dim avgdayscool As New List(Of Double)
            Dim avgdayselect As New List(Of Double)
            For i = 0 To ehub._demand(0).Length - 1
                avgdayselect.Add(ehub._demand(0)(i))
                avgdaysheat.Add(ehub._demand(1)(i))
                avgdayscool.Add(ehub._demand(2)(i))
            Next

            DA.SetDataList(18, avgdayselect)
            DA.SetDataList(19, avgdaysheat)
            DA.SetDataList(20, avgdayscool)

            Dim acunits As Double
            acunits = ehub._ACunits
            DA.SetData(21, acunits)
        Else
            DA.SetData(1, 999999999999999999)
        End If





    End Sub

    ''' <summary>
    ''' Provides an Icon for every component that will be visible in the User Interface.
    ''' Icons need to be 24x24 pixels.
    ''' </summary>
    Protected Overrides ReadOnly Property Icon() As System.Drawing.Bitmap
        Get
            'You can add image files to your project resources and access them like this:
            ' return Resources.IconForThisComponent;
            Return My.Resources.CaseB
        End Get
    End Property

    ''' <summary>
    ''' Gets the unique ID for this component. Do not change this ID after release.
    ''' </summary>
    Public Overrides ReadOnly Property ComponentGuid() As Guid
        Get
            Return New Guid("{0b31764d-4c65-43db-8754-1a9ebd5d410c}")
        End Get
    End Property
End Class