Imports System.Collections.Generic

Imports Grasshopper.Kernel
Imports Rhino.Geometry


Public Class GHCaseA_Test
    Inherits GH_Component
    ''' <summary>
    ''' Initializes a new instance of the GHCaseA class.
    ''' </summary>
    Public Sub New()
        MyBase.New("CaseA - CPlex Solver", "CaseA-CPex", _
                    "Case A - Step 2 CPlex Solver, using Solar Potentials from Step 1", _
                    "EnergyHubs", "Examples")
    End Sub

    ''' <summary>
    ''' Registers all the input parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterInputParams(pManager As GH_Component.GH_InputParamManager)
        pManager.AddBooleanParameter("Run Solver!", "blnRun", "Start the Solver", GH_ParamAccess.item)
        pManager.AddIntegerParameter("Horizon", "intHorizon", "Time Horizon in hours", GH_ParamAccess.item)
        pManager.AddTextParameter("Input Path", "strInputPath", "Input Path containing data", GH_ParamAccess.item)
        pManager.AddNumberParameter("CO2 Reduction", "dblCO2Reduc", "CO2 reduction target in % (0 - 1)", GH_ParamAccess.item)
        pManager.AddNumberParameter("WW Ratio", "dblWW", "Window to Wall Ratio in % (0-1)", GH_ParamAccess.list)
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

    End Sub

    ''' <summary>
    ''' This is the method that actually does the work.
    ''' </summary>
    ''' <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    Protected Overrides Sub SolveInstance(DA As IGH_DataAccess)


        Dim run As Boolean
        If (Not DA.GetData(0, run)) Then Return
        If run <> True Then Return

        Dim period As Integer
        If (Not DA.GetData(1, period)) Then Return

        Dim strInput As String
        If (Not DA.GetData(2, strInput)) Then Return

        Dim co2reduc As Double
        If (Not DA.GetData(3, co2reduc)) Then Return

        Dim wwratio As New List(Of Double)
        If (Not DA.GetDataList(4, wwratio)) Then Return

        Dim ehub As CaseA_Test = New CaseA_Test
        ehub.RunCaseA(strInput, period, co2reduc, _
                      wwratio.Item(0), wwratio.Item(1), wwratio.Item(2), wwratio.Item(3))


        Dim dVarCap As New List(Of Double)
        For i = 0 To ehub._cap.Length - 1
            dVarCap.Add(ehub._cap(i))
        Next

        DA.SetData(0, ehub._solved)
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

    End Sub

    ''' <summary>
    ''' Provides an Icon for every component that will be visible in the User Interface.
    ''' Icons need to be 24x24 pixels.
    ''' </summary>
    Protected Overrides ReadOnly Property Icon() As System.Drawing.Bitmap
        Get
            'You can add image files to your project resources and access them like this:
            ' return Resources.IconForThisComponent;
            Return My.Resources.test
        End Get
    End Property

    ''' <summary>
    ''' Gets the unique ID for this component. Do not change this ID after release.
    ''' </summary>
    Public Overrides ReadOnly Property ComponentGuid() As Guid
        Get
            Return New Guid("{cab82054-08ad-4772-a400-0e41ca1c74e8}")
        End Get
    End Property
End Class