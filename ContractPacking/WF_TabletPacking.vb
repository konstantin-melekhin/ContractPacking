Imports System.Deployment.Application
Imports System.Drawing.Printing
Imports System.IO
Imports Library3

Public Class WF_TabletPacking
#Region "Переменные"
    Dim LOTID, IDApp, UnitCounter, PCBID, SNID, PalletNumber, BoxNumber As Integer
    Dim ds As New DataSet
    Dim LenSN_SMT, LenSN_FAS, StartStepID, PreStepID, NextStepID As Integer
    Dim StartStep, PreStep, NextStep, Litera As String
    Dim PCInfo As New ArrayList() 'PCInfo = (App_ID, App_Caption, lineID, LineName, StationName,CT_ScanStep)
    Dim LOTInfo As New ArrayList() 'LOTInfo = (Model,LOT,SMTRangeChecked,SMTStartRange,SMTEndRange,ParseLog)
    Dim ShiftCounterInfo As New ArrayList() 'ShiftCounterInfo = (ShiftCounterID,ShiftCounter,LOTCounter)
    Dim SNBufer As New ArrayList 'SNBufer = (BooLSMT (Занят или свободен),SMTSN,BooLFAS (Занят или свободен),FASSN )
    Dim StepSequence As String()
    Dim SNFormat As ArrayList
    Dim PrinterInfo() As String
#End Region
#Region "Загрузка формы"
    Public Sub New(LOTID As Integer, IDApp As Integer)
        InitializeComponent()
        Me.LOTID = LOTID
        Me.IDApp = IDApp
    End Sub
    Private Sub WF_SberDevice_Load(sender As Object, e As EventArgs) Handles MyBase.Load
#Region "Обнаружение принтеров и установка дефолтного принтера"
        For Each item In PrinterSettings.InstalledPrinters
            If InStr(item.ToString(), "ZDesigner") Then
                CB_DefaultPrinter.Items.Add(item.ToString())
            End If
            If CB_DefaultPrinter.Items.Count <> 0 Then
                CB_DefaultPrinter.Text = CB_DefaultPrinter.Items(0)
            Else
                PrintLabel(Controllabel, "Ни один принтер не подключен!", 12, 234, Color.Red)
            End If
        Next
        If CB_DefaultPrinter.Items.Count = 0 Then
            PrintLabel(Controllabel, "Ни один принтер не подключен!", 12, 234, Color.Red)
        End If
        GetCoordinats()
#End Region
        Dim myVersion As Version
        If ApplicationDeployment.IsNetworkDeployed Then
            myVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion
        End If
        LB_SW_Wers.Text = String.Concat("v", myVersion)
        Dim a As String = "1;A"
        Dim k = Integer.Parse(a.Split(";")(0)).ToString("0:00000")
        Dim k1 = Integer.Parse(a.Split(";")(0)).ToString("00000")
        Dim k2 = Mid(Integer.Parse(a.Split(";")(0)).ToString("00000"), 1, 4) & ">6" & Mid(Integer.Parse(a.Split(";")(0)).ToString("00000"), 5)
        Dim k3 = Integer.Parse(a.Split(";")(0)).ToString("00000")
        'получение данных о станции
        LoadGridFromDB(DG_StepList, "USE FAS SELECT [ID],[StepName],[Description] FROM [FAS].[dbo].[Ct_StepScan]")
        PCInfo = GetPCInfo(IDApp)
        LabelAppName.Text = PCInfo(7)
        Label_StationName.Text = PCInfo(5)
        Lebel_StationLine.Text = PCInfo(3)
        TextBox1.Text = "App_ID = " & PCInfo(0) & vbCrLf &
                            "App_Caption = " & PCInfo(1) & vbCrLf &
                            "lineID = " & PCInfo(2) & vbCrLf &
                            "LineName = " & PCInfo(3) & vbCrLf &
                            "StationID = " & PCInfo(4) & vbCrLf &
                            "StationName = " & PCInfo(5) & vbCrLf &
                            "CT_ScanStepID = " & PCInfo(6) & vbCrLf &
                            "CT_ScanStep = " & PCInfo(7) & vbCrLf &
                            "LiterID " & PCInfo(8) & vbCrLf &
                            "LiterName = " & PCInfo(9)
        'получение данных о текущем лоте
        LOTInfo = GetCurrentContractLot(LOTID)
        LenSN_SMT = If(LOTInfo(2) = True, GetLenSN(LOTInfo(3)), 1)
        LenSN_FAS = If(LOTInfo(7) = True, GetLenSN(LOTInfo(8)), 1)
        TextBox2.Text = "Model = " & LOTInfo(0) & vbCrLf &
                            "LOT = " & LOTInfo(1) & vbCrLf &
                            "CheckFormatSN_SMT = " & LOTInfo(2) & vbCrLf &
                            "SMTNumberFormat = " & LOTInfo(3) & vbCrLf &
                            "SMTRangeChecked = " & LOTInfo(4) & vbCrLf &
                            "SMTStartRange = " & LOTInfo(5) & vbCrLf &
                            "SMTEndRange = " & LOTInfo(6) & vbCrLf &
                            "CheckFormatSN_FAS = " & LOTInfo(7) & vbCrLf &
                            "FASNumberFormat = " & LOTInfo(8) & vbCrLf &
                            "FASRangeChecked = " & LOTInfo(9) & vbCrLf &
                            "FASStartRange = " & LOTInfo(10) & vbCrLf &
                            "FASEndRange = " & LOTInfo(11) & vbCrLf &
                            "SingleSN = " & LOTInfo(12) & vbCrLf &
                            "ParseLog = " & LOTInfo(13) & vbCrLf &
                            "StepSequence = " & LOTInfo(14) & vbCrLf &
                            "BoxCapacity = " & LOTInfo(15) & vbCrLf &
                            "PalletCapacity = " & LOTInfo(16) & vbCrLf &
                            "LiterIndex = " & LOTInfo(17) & vbCrLf &
                            "HexSN = " & LOTInfo(18)
        Litera = If(LOTInfo(17) = 0, PCInfo(9), (PCInfo(9) & LOTInfo(17)))
        'Определить стартовый шаг, текущий и последующий
        StepSequence = New String(Len(LOTInfo(14)) / 2 - 1) {}
        For i = 0 To Len(LOTInfo(14)) - 1 Step 2
            Dim J As Integer
            StepSequence(J) = Mid(LOTInfo(14), i + 1, 2)
            J += 1
        Next
        For i = 0 To StepSequence.Count - 1
            If Convert.ToInt32(StepSequence(i), 16) = PCInfo(6) Then
                StartStepID = Convert.ToInt32(StepSequence(0), 16)
                PreStepID = If(i <> 0, Convert.ToInt32(StepSequence(i - 1), 16), 0)
                NextStepID = If(i <> StepSequence.Count - 1, Convert.ToInt32(StepSequence(i + 1), 16), 0)
                For Each row As DataGridViewRow In DG_StepList.Rows
                    Dim j As Integer
                    If StartStepID = DG_StepList.Item(0, j).Value Then
                        StartStep = DG_StepList.Item(1, j).Value
                    ElseIf PreStepID = DG_StepList.Item(0, j).Value Then
                        PreStep = DG_StepList.Item(1, j).Value
                    ElseIf NextStepID = DG_StepList.Item(0, j).Value Then
                        NextStep = DG_StepList.Item(1, j).Value
                    End If
                    j += 1
                Next
                If PreStepID = StartStepID Then
                    PreStep = StartStep
                End If
                Exit For
            End If
        Next
        L_LOT.Text = LOTInfo(1)
        L_Model.Text = LOTInfo(0)
        L_BoxCapacity.Text = LOTInfo(15)
        L_PalletCapacity.Text = LOTInfo(16)
        L_Liter.Text = If(LOTInfo(17) = 0, PCInfo(9), PCInfo(9) & " " & LOTInfo(17))
        'Запуск программы
        '___________________________________________________________
        GB_UserData.Location = New Point(10, 12)
        TB_RFIDIn.Focus()
        'запуск счетчика продукции за день
        CurrentTimeTimer.Start()
        ShiftCounterInfo = ShiftCounterStart(PCInfo(4), IDApp, LOTID)
        Label_ShiftCounter.Text = ShiftCounterInfo(1)
        LB_LOTCounter.Text = ShiftCounterInfo(2)

        'Последняя упакованная коробка
        Dim LastPackCounter As ArrayList = New ArrayList(GetLastPack(LOTID, PCInfo(2)))
        BoxNum.Text = LastPackCounter(1)
        NextBoxNum.Text = LastPackCounter(1) + 1
        PalletNum.Text = LastPackCounter(0)
        UnitCounter = LastPackCounter(2)
        If LOTInfo(15) <> LastPackCounter(2) Then
            LoadGridFromDB2(DG_Packing, $"use FAS
            SELECT UnitNum as '№',SN.SN AS 'FAS Номер',(Lit.LiterName + ' ' + cast(LiterIndex as nvarchar (5))) AS 'Литера' 
            ,PalletNum as 'Паллет', BoxNum as 'Групповая', Format(PackingDate,'dd.MM.yyyy HH:mm:ss') as 'Дата'
            FROM [FAS].[dbo].[Ct_PackingTable] as P
            Left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = p.PCBID
            Left join [FAS].[dbo].Ct_FASSN_reg as Sn On Sn.ID = p.SNID
            Left join [FAS].[dbo].FAS_Liter as Lit On Lit.ID = p.LiterID
            where P.LOTID = {LOTID} And BoxNum = {LastPackCounter(1)} And LiterID = {PCInfo(8)} and literindex = {LOTInfo(17)}
            order by UnitNum desc", ds)
        ElseIf LOTInfo(15) = LastPackCounter(2) Then
            LoadGridFromDB2(DG_Packing, $"use FAS
            SELECT UnitNum as '№',SN.SN AS 'FAS Номер',(Lit.LiterName + ' ' + cast(LiterIndex as nvarchar (5))) AS 'Литера' 
            ,PalletNum as 'Паллет', BoxNum as 'Групповая', Format(PackingDate,'dd.MM.yyyy HH:mm:ss') as 'Дата'
            FROM [FAS].[dbo].[Ct_PackingTable] as P
            Left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = p.PCBID
            Left join [FAS].[dbo].Ct_FASSN_reg as Sn On Sn.ID = p.SNID
            Left join [FAS].[dbo].FAS_Liter as Lit On Lit.ID = p.LiterID
            where P.LOTID = 0
            order by UnitNum desc", ds) '  P.LOTID = 0 - требуется для загрузки пустой таблицы
            BoxNum.Text = LastPackCounter(1) + 1
            NextBoxNum.Text = LastPackCounter(1) + 2
            UnitCounter = 1
            If LastPackCounter(1) Mod LOTInfo(16) = 0 Then
                PalletNum.Text = LastPackCounter(0) + 1
            End If
        End If
        'определение стартовых данных для упаковки
        PalletNumber = PalletNum.Text
        BoxNumber = BoxNum.Text
    End Sub

#End Region
#Region "очистка Серийного номера при ошибке"
    Private Sub BT_ClearSN_Click(sender As Object, e As EventArgs) Handles BT_ClearSN.Click
        SerialTextBox.Clear()
        Controllabel.Text = ""
        SerialTextBox.Enabled = True
        SNBufer = New ArrayList()
        SerialTextBox.Focus()
    End Sub
#End Region
#Region "Часы в программе"
    'Часы в программе
    Private Sub CurrentTimeTimer_Tick(sender As Object, e As EventArgs) Handles CurrentTimeTimer.Tick
        CurrrentTimeLabel.Text = TimeString
    End Sub 'Часы в программе
#End Region
#Region "регистрация пользователя"
    Dim UserInfo As New ArrayList()
    Private Sub TB_RFIDIn_KeyDown(sender As Object, e As KeyEventArgs) Handles TB_RFIDIn.KeyDown
        TB_RFIDIn.MaxLength = 10
        If e.KeyCode = Keys.Enter And TB_RFIDIn.TextLength = 10 Then ' если длина номера равна 10, то запускаем процесс
            UserInfo = GetUserData(TB_RFIDIn.Text, GB_UserData, GB_WorkAria, L_UserName, TB_RFIDIn)
            '"UserID = " & UserInfo(0) & vbCrLf &
            '"Name = " & UserInfo(1) & vbCrLf &
            '"User Group = " & UserInfo(2) & vbCrLf  'UserInfo
            SerialTextBox.Focus()
        ElseIf e.KeyCode = Keys.Enter Then
            TB_RFIDIn.Clear()
        End If
    End Sub 'регистрация пользователя
#End Region
#Region "условия для возврата в окно настроек"
    Dim OpenSettings As Boolean
    Private Sub Button_Click(sender As Object, e As EventArgs) Handles BT_OpenSettings.Click, BT_LogInClose.Click
        OpenSettings = True
        Me.Close()
    End Sub
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Dim Question As String
        Question = If(OpenSettings = True, "Вы подтверждаете возврат в окно настроек?", "Вы подтверждаете выход из программы?")
        Select Case MsgBox(Question, MsgBoxStyle.YesNo, "")
            Case MsgBoxResult.Yes
                e.Cancel = False
                If OpenSettings = True Then
                    SettingsForm.Show()
                End If
            Case MsgBoxResult.No
                e.Cancel = True
        End Select
        OpenSettings = False
    End Sub ' условия для возврата в окно настроек
#End Region
#Region "Обработка поля ввода серийного номера"
    'начало работы приложения FAS Scanning Station
    Private Sub SerialTextBox_KeyDown(sender As Object, e As KeyEventArgs) Handles SerialTextBox.KeyDown
        Dim _stepArr As ArrayList
        If e.KeyCode = Keys.Enter Then 'And (SerialTextBox.TextLength = LenSN_SMT Or SerialTextBox.TextLength = LenSN_FAS) Then
            'определение формата номера
            If GetFTSN() = True Then
                'проверка диапазона номера
                If CheckRange(SNFormat) = True Then
                    _stepArr = New ArrayList(GetPreStep(SNID))
                    If _stepArr.Count = 0 Then
                        PrintLabel(Controllabel, SerialTextBox.Text & " не не был зарегистрирован на FAS Start!", 12, 234, Color.Red)
                    ElseIf _stepArr.Count > 0 And _stepArr(4) = PreStepID And _stepArr(5) = 2 Then ''_stepArr(4) = 37 - станция взвешивания
                        'проверка задвоения и наличия номера в базе
                        If CheckDublicate(_stepArr(2)) = True Then
                            WriteDB(_stepArr)
                            PrintLabel(Controllabel, $"Приемник {SerialTextBox.Text} { vbCrLf}определен и записан в базу!", 12, 193, Color.Green)
                            SerialTextBox.Clear()
                        End If
                    ElseIf _stepArr.Count > 0 And _stepArr(4) = 6 And _stepArr(5) = 2 Then
                        'проверка задвоения и наличия номера в базе
                        If CheckDublicate(_stepArr(2)) = True Then
                            PrintLabel(Controllabel, $"Приемник {SerialTextBox.Text} { vbCrLf}имеет статус упакован, но не найден в таблице упакованных!{ vbCrLf}Отложите приемник в сторону, вызовите технолога!", 12, 193, Color.Red)
                            SerialTextBox.Enabled = False
                        End If
                    Else
                        Dim Mess As String
                        Mess = $"Приемник {SerialTextBox.Text } { vbCrLf }имеет не верный предыдущий шаг { vbCrLf }''{SelectString($"Use FAS SELECT [StepName]  FROM [FAS].[dbo].[Ct_StepScan] where ID = {_stepArr(4)}")}''!"
                        PrintLabel(Controllabel, Mess, 12, 193, Color.Red)
                        SerialTextBox.Enabled = False
                    End If
                End If
            Else
                'если введен не верный номер
                PrintLabel(Controllabel, $"{SerialTextBox.Text}  не соответствует шаблону!", 12, 180, Color.Red)
                SerialTextBox.Enabled = False
                BT_Pause.Focus()
            End If
        End If
        SerialTextBox.Focus()
    End Sub
#End Region
#Region "1. Определение формата номера"
    Public Function GetFTSN() As Boolean
        Dim col As Color, Mess As String, Res As Boolean
        SNFormat = New ArrayList()
        SNFormat = GetSNFormat(LOTInfo(3), LOTInfo(8), "", SerialTextBox.Text, LOTInfo(18), LOTInfo(2), LOTInfo(7))
        Res = SNFormat(0)
        Mess = SNFormat(3)
        col = If(Res = False, Color.Red, Color.Green)
        PrintLabel(Controllabel, Mess, 12, 193, col)
        SerialTextBox.Enabled = Res
        SNID = If(SNFormat(1) = 2,
                SelectInt($"USE FAS Select [ID] FROM [FAS].[dbo].[Ct_FASSN_reg] where SN = '{SerialTextBox.Text}'"),
                SelectInt($"USE FAS SELECT [ID] FROM [FAS].[dbo].[Ct_FASSN_reg] where LOTID = {LOTID} and right (SN, 7) = '{CInt("&H" & Mid(SerialTextBox.Text, 7, 6))}'"))
        Return Res
    End Function
#End Region
#Region "2. проверка диапазона"
    Private Function CheckRange(SNFormat As ArrayList) As Boolean
        Dim res As Boolean
        Dim ChekRange As Boolean, StartRange As Integer, EndRange As Integer
        Select Case SNFormat(1)
            Case 1
                ChekRange = LOTInfo(4)
                StartRange = LOTInfo(5)
                EndRange = LOTInfo(6)
            Case 2
                ChekRange = LOTInfo(9)
                StartRange = LOTInfo(10)
                EndRange = LOTInfo(11)
        End Select

        If ChekRange = True Then
            If StartRange <= SNFormat(2) And SNFormat(2) <= EndRange Then
                res = True
            Else
                res = False
                PrintLabel(Controllabel, "Номер " & SerialTextBox.Text & vbCrLf & "вне диапазона выбранного лота!", 12, 193, Color.Red)
                SerialTextBox.Enabled = False
            End If
        Else
            res = True
        End If
        Return res
    End Function
#End Region
#Region "4. Проверка дубликатов"
    Private Function CheckDublicate(_snid As Integer) As Boolean
        Dim Res As Boolean, SQL As String, Mess As String, Col As Color
        SQL = $"Use FAS SELECT L.Content,S.SN,Lit.LiterName + cast ([LiterIndex] as nvarchar),[PalletNum],[BoxNum],[UnitNum],[PackingDate],U.UserName
                        FROM [FAS].[dbo].[Ct_PackingTable] as P
                        left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = P.PCBID
                        Left join Ct_FASSN_reg as S On S.ID = P.SNID
                        Left join FAS_Liter as Lit On Lit.ID = P.LiterID
                        Left join FAS_Users as U On U.UserID = P.UserID
                        where SNID = {_snid}"
        Dim PackedSN = New ArrayList(SelectListString(SQL))
        Mess = If(PackedSN.Count <> 0, "Приемник " & SerialTextBox.Text & " уже упакован!" & vbCrLf &
                            "Литера - " & PackedSN(2) & " Паллет - " & PackedSN(3) & " Групповая - " & PackedSN(4) & " № - " & PackedSN(5) & vbCrLf &
                            "Дата - " & PackedSN(6), "")
        Res = (PackedSN.Count = 0)
        Col = If(Res = False, Color.Red, Color.Green)
        PrintLabel(Controllabel, Mess, 12, 193, Col)
        SNTBEnabled(Res)
        Return Res
    End Function
#End Region
#Region "5. Запись в базу данных и в Рабочий грид"
    Dim TableColumn As ArrayList
    Private Sub WriteDB(_SNInfo As ArrayList)
        If UnitCounter = LOTInfo(15) Then
            ds.Clear() 'если юнит каунтер = емкости коробки, то очищаем грид коробки и увеличиваем счетчик на 1
            'если текущий номер коробки делится на объем паллета без остатка, то увеличиваем номер паллета
            PalletNumber = If(BoxNumber Mod LOTInfo(16) = 0, PalletNumber + 1, PalletNumber)
            PalletNum.Text = PalletNumber

            BoxNumber += 1
            BoxNum.Text = BoxNumber
            NextBoxNum.Text = BoxNumber + 1
        End If
        'юнит каунтер = определяется количеством строк в гриде
        UnitCounter = DG_Packing.RowCount + 1
        'список для записи в грид упаковки
        TableColumn = New ArrayList() From {UnitCounter, _SNInfo(3), Litera, PalletNumber, BoxNumber, Date.Now}
        Dim row = ds.Tables(0).NewRow()
        Dim i = 0
        For Each item In TableColumn
            row.Item(i) = item
            i += 1
        Next
        ds.Tables(0).Rows.Add(row)
        DG_Packing.DataSource = ds
        DG_Packing.Sort(DG_Packing.Columns(0), System.ComponentModel.ListSortDirection.Descending)
        RunCommand($"use FAS
                insert into [FAS].[dbo].[Ct_PackingTable] (PCBID,SNID,LOTID, LiterID,LiterIndex,PalletNum,BoxNum,UnitNum,PackingDate,UserID)values
                ({_SNInfo(0)},{_SNInfo(2)},{ LOTID },{ PCInfo(8) },{ LOTInfo(17) },{ PalletNumber },{ BoxNumber },{ UnitCounter },current_timestamp,{ UserInfo(0) } )
                update [FAS].[dbo].[FAS_PackingCounter] set [PalletCounter] = { PalletNumber },[BoxCounter] = { BoxNumber },[UnitCounter] = { UnitCounter } 
                where [LineID] = { PCInfo(2) } and [LOTID] = {LOTID}")
        ShiftCounter(2)
        'печать групповой этикетки 
        If UnitCounter = LOTInfo(15) Then '
            SerchBoxForPrint(LOTID, BoxNumber, PCInfo(8), LOTInfo(17))
            SNArray = GetSNFromGrid()
            Print(SNArray, CB_DefaultPrinter.Text, Num_X.Value, Num_Y.Value)
        End If
        RunCommand($"insert into [FAS].[dbo].[Ct_OperLog] ([PCBID],[LOTID],[StepID],[TestResultID],[StepDate],[StepByID],[LineID],[SNID])values
                    ({_SNInfo(0)},{ LOTID },6,2,CURRENT_TIMESTAMP,{ UserInfo(0) },{ PCInfo(2) },{_SNInfo(2)})")
    End Sub
#End Region
#Region "6.1 'Счетчик продукции"
    Private Sub ShiftCounter(StepRes As Integer)
        ShiftCounterInfo(1) += 1
        ShiftCounterInfo(2) += 1
        If StepRes = 2 Then
            ShiftCounterInfo(3) += 1
        Else
            ShiftCounterInfo(4) += 1
        End If
        Label_ShiftCounter.Text = ShiftCounterInfo(1)
        LB_LOTCounter.Text = ShiftCounterInfo(2)
        ShiftCounterUpdateCT(PCInfo(4), PCInfo(0), ShiftCounterInfo(0), ShiftCounterInfo(1), ShiftCounterInfo(2),
                                 ShiftCounterInfo(3), ShiftCounterInfo(4))
    End Sub
    Private Sub BT_SetPrinter_Click(sender As Object, e As EventArgs) Handles BT_SetPrinter.Click
        If GB_Printers.Visible = False Then
            GB_Printers.Visible = True
            GB_Printers.Location = New Point(650, 60)
            GB_StationInfo.Visible = False
        Else
            GB_Printers.Visible = False
            GB_StationInfo.Visible = True
        End If
    End Sub
#End Region
#Region "6.2 деактивация ввода серийника"
    Private Sub SNTBEnabled(Res As Boolean)
        SerialTextBox.Enabled = Res
        BT_Pause.Focus()
    End Sub
#End Region
#Region "7. печать групповой"
    Dim SNArray As New ArrayList
    Dim SQL As String
    Private Sub SerchBoxForPrint(LotID As Integer, BoxNum As Integer, LiterID As Integer, literIndex As Integer) 'LitName As String,
        'SELECT  [UnitNum] as '№',l.Content AS 'Серийный номер платы',Lit.LiterName as 'Литера' ,[BoxNum]as 'Номер коробки'
        SQL = $"use fas
                SELECT  [UnitNum] as '№',F.SN AS 'Серийный номер платы',Lit.LiterName as 'Литера' ,[BoxNum]as 'Номер коробки' 
                FROM [FAS].[dbo].[Ct_PackingTable] as P
                left join [SMDCOMPONETS].[dbo].[LazerBase] as L On l.IDLaser = PCBID
                left join dbo.Ct_FASSN_reg as F On F.ID =P.SNID
                left join dbo.FAS_Liter as Lit On Lit.ID = P.LiterID
                where p.lotid ={LotID} and literid = {LiterID} And LiterIndex = {literIndex} and BoxNum = {BoxNum} order by UnitNum
                " 'and LiterName= '" & LitName & "'
        LoadGridFromDB(DG_SelectedBox, SQL)
    End Sub
    Private Function GetSNFromGrid()
        Dim SNArrayTemp As New ArrayList
        If DG_SelectedBox.Rows.Count > 0 Then
            SNArrayTemp.Add(DG_SelectedBox.Item(3, 0).Value & ";" & DG_SelectedBox.Item(2, 0).Value)
            For i = 0 To DG_SelectedBox.Rows.Count - 1
                SNArrayTemp.Add(DG_SelectedBox.Item(1, i).Value)
            Next
        Else
            PrintLabel(Controllabel, "Корбка еще не закрыта!", 12, 193, Color.Red)
        End If
        Return SNArrayTemp
    End Function
    Private Sub GetCoordinats()
        Try
            PrinterInfo = File.ReadAllLines("C:\IP_TV_LabelSet\Coordinats_Gr.csv")
        Catch ex As Exception
            PrinterInfo = New String(0) {$"{CB_DefaultPrinter.Items(0)};0;0;"}
            IO.Directory.CreateDirectory("C:\IP_TV_LabelSet\")
            File.Create("C:\IP_TV_LabelSet\Coordinats_Gr.csv").Close()
            File.WriteAllLines("C:\IP_TV_LabelSet\Coordinats_Gr.csv", PrinterInfo)
        End Try
        CB_DefaultPrinter.Text = PrinterInfo(0).Split(";")(0)
        Num_X.Value = PrinterInfo(0).Split(";")(1)
        Num_Y.Value = PrinterInfo(0).Split(";")(2)
    End Sub
    Private Sub BT_Save_Coordinats_Click(sender As Object, e As EventArgs) Handles BT_Save_Coordinats.Click
        PrinterInfo(0) = $"{CB_DefaultPrinter.SelectedItem};{Num_X.Value};{Num_Y.Value}"
        File.WriteAllLines("C:\IP_TV_LabelSet\Coordinats_Gr.csv", PrinterInfo)
        GetCoordinats()
    End Sub

    Private Function Print(SNArray As ArrayList, DefPrt As String, x As Integer, y As Integer)
        If DefPrt <> "" Then
            RawPrinterHelper.SendStringToPrinter(DefPrt, GetGroupLabel(SNArray, x, y))
            CB_ManualPrint.Checked = False
            Return True
        Else
            MsgBox("Принтер не выбран или не подключен")
            Return False
        End If
    End Function
#End Region
#Region "8. Ручная печать групповой"
    Private Sub CB_ManualPrint_CheckedChanged(sender As Object, e As EventArgs) Handles CB_ManualPrint.CheckedChanged
        If CB_ManualPrint.Checked = True Then
            GB_ManualPrint.Visible = True
            SerialTextBox.Enabled = False
        Else
            GB_ManualPrint.Visible = False
            SerialTextBox.Enabled = True
        End If
    End Sub
    Dim SearchSNList As New ArrayList

    Private Sub TB_ScanSN_KeyDown(sender As Object, e As KeyEventArgs) Handles TB_ScanSN.KeyDown
        If e.KeyCode = Keys.Enter Then
            SearchSNList = SerchSN(TB_ScanSN.Text)
            If SearchSNList.Count <> 0 Then
                SerchBoxForPrint(SearchSNList(1), SearchSNList(3), PCInfo(8), LOTInfo(17))
                SNArray = GetSNFromGrid()
                If SNArray.Count = 6 Then
                    Print(SNArray, CB_DefaultPrinter.Text, Num_X.Value, Num_Y.Value)
                    'GetGroupLabel(SNArray, PrinterInfo(0).Split(";")(1), PrinterInfo(0).Split(";")(2))
                    TB_ScanSN.Clear()
                Else
                    PrintLabel(Controllabel, "Корбка еще не закрыта!", 12, 193, Color.Red)
                End If
            Else
                TB_ScanSN.Clear()
                PrintLabel(Controllabel, "Номер не найден в базе!", 12, 193, Color.Red)
                Exit Sub
            End If
        End If
    End Sub
    Private Sub NumBox_KeyDown(sender As Object, e As KeyEventArgs) Handles NumBox.KeyDown
        If e.KeyCode = Keys.Enter Then
            System.Threading.Thread.Sleep(1000)
            SerchBoxForPrint(LOTID, NumBox.Value, PCInfo(8), LOTInfo(17))
            SNArray = GetSNFromGrid()
            If SNArray.Count = 21 Then
                Print(SNArray, CB_DefaultPrinter.Text, Num_X.Value, Num_Y.Value)
                NumBox.Value += 1
            Else
                PrintLabel(Controllabel, "Корбка еще не закрыта!", 12, 193, Color.Red)
            End If
        End If
    End Sub
    Private Function SerchSN(Sn As String)
        SQL = $"use fas
                SELECT  l.Content,p.[LOTID],Lit.LiterName ,[BoxNum],LiterIndex
                FROM [FAS].[dbo].[Ct_PackingTable] as P
                left join [SMDCOMPONETS].[dbo].[LazerBase] as L On l.IDLaser = PCBID
                left join dbo.Ct_FASSN_reg as F On F.ID =P.SNID
                left join dbo.FAS_Liter as Lit On Lit.ID = P.LiterID
                where l.Content = '{Sn}' or F.Sn = '{Sn}'"
        Return SelectListString(SQL) 'IB365MC001409
    End Function
#End Region
#Region "9. Проверка предыдущего шага и загрузка данных о плате"
    Private Function GetPreStep(_snid As Integer) As ArrayList
        Dim newArr As ArrayList = New ArrayList(SelectListString($"Use FAS 
                        select tt.PCBID,
            (select Content from SMDCOMPONETS.dbo.LazerBase where IDLaser =  tt.PCBID) ,
            tt.SNID, 
            (select SN from Ct_FASSN_reg Rg where ID =  tt.SNID),
            tt.StepID,tt.TestResultID, tt.StepDate 
            from  (SELECT *, ROW_NUMBER() over(partition by snid order by stepdate desc) num FROM 
            [FAS].[dbo].[Ct_OperLog] where LOTID = {LOTID} and  SNID  = {_snid}) tt
            where  tt.num = 1 "))
        Return newArr
    End Function
#End Region
#Region " 10. Групповая этикетка"
    Private Function GetGroupLabel(sn As ArrayList, x As Integer, y As Integer)
        Dim str As String = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO64,128^GFA,10752,10752,00056,:Z64:
eJzt2j2OnDAYBmAjCpccwUfhYpHM0RylSJkjLNEWaYnSUCAc8B/+A148mdnZaFwkC8uzmM8ftjEQ8iqv8qhSS69MuJNB6VFGQzejrg2dFEXVlHLAWB07MDJN7MALZLGTHeR44gTkEoYFpkrdiLgknGBAaeqggCbNsBTEpc2ANUSbcQJwvNBlGHQL5hzQ8LrZo1YEGl6BKbpM1A1RWIGEoSYORU5EDkg0lWZdoSNx2pw7doObE9fdzbUm7P/GiadzXOo0frD7EroedJXkRS7utFEXd9r3dnFn/3+54fO7yvX5fN239QRHjkvRqM6HyqlaB4NmGzpOXKvuqcXVdjCAXM/Vz4uj6//rrwTkpKoZlXOz7Ku9Sz92P6XqfRbHln0q+0fEvUvVKy+uXfapoybE/ZIqEotb6qz7oBlxqnTW6e1rbt1nN0En7KBsN0FnAimq+Hegq3dcX+XdYBy1m5ETGTexwI0us0/cWjnTTS73hqoS5EgVuI7YhDl2M1EHUpUmbG3zFnKT/oeqaLAVtybRjt2oT0DNFG4y+NSZs1AzhZtUV3HJdasbrztbbYq6xnMDMaeGnPRdjTq661TicNWczZFTuxM33tdV5oa/m1sr9lFOvBzq5F5+7rvj++EjXf/Ebv++fVZ3Uz9Y2u8W9fNXxodtXNkdj+iZm/TpY8cTN+qBWTu2M96+y8RNbpwm++O7TN3s5gXEzCd4Op/IODV/sa7Rc8KcG4me7DvXUzPv0W6oM/MluVXdOf3HtNub19kpaZ2bDxK34pk6uxzFDl2fuPV0K68y81biVmZF7GbiVv0CJ2xS7jhv9S1wnXXcbMZ5tj3G553+qyRyszswcu7mYVtbec5fpbDujzTPR8qpWiXPVb231Gvdb2mexyQh2xpk6DpvCdW6QR9oXLWFc3Ozv7jhHFc/+51DF7nRX5F2jqkDrWNbTjnXb9H0HFUHUhNFl7me6/w1H+cIN9ejC3MrnM4Fazeb2yvWTcGCO+5G//IuOO8h7pITdZkLuzTYzeF7CNiFYcHdEL4XgF0YTtyF4bzgaKFjha4tdLzQyTL3tdB9e7D7Xuh+FLq3Qic/pxM3uaPSPoXrTh17CnfKlHikozkHvKfMvH6HXOZ1P/QettRlPkuA3jNnHfIhRKnjGdcDrs04Ueg6wLGMA1jxdxel33lkEgZo9mzDYx8G8cT1kEsbQkAubQiIpQEFv0NKAgOFk6SB6UEXX2AHuqjl8Q/XwpYQsHtc+QtFg+HS:A78E
^FO544,128^GFA,02688,02688,00028,:Z64:
eJzt011qxCAQB/ARH3z0CB7FoynsxYQ+9BrCHmCFfRF28F81aeu6CS39oH3IEALmR8JkPoiOOOKnQiCTxXqQ/H7/ljG5f2OFsGelUPnAXCCyJ24wm0AiwplJoxlWQ71I1ZQkrlxrFAZzgkGmpqRwaznH2WzNVaO+DKQN880EkGdzEcFkyxKaB7OnDIK30SRTn8tHIxNt1KxYzCaqJRsUqyzK8O/dCunVaNOcr5buzDwn1KPKzkvWifAlAz5r+hJ/2dLdvKhL2DeEtS61Znu21vPBhj7EwST82D9Zhv4ttt33bjvzstj2nHVb5vPW5nM00TPpc32dZ7fbsg/ntg9xtmWPnrh+wL/Za/T9o6EoRxxxxB/GC3jEsbs=:9B59
^FO576,192^GFA,02304,02304,00024,:Z64:
eJzt1DtuwzAMAFAJHjTyCO4ReoACOprcmyk30RE0ahDEkrTVMAndoV06mAZi+AWKxY/i3BVXXPH3CN32hJvcoT6wRyyWL4jN8oDYLQfEYfmKiJbHBqansuwbffbsTCfEbLksiYVdSrLKak8e6dvGvqLzIxbeh6fNv1PSgz1h9iN9uyRHzqXyA/PdKbkNKpWqTl/64Vm80RrlsaYCNQzo5NvdU14rVGjLeHL6db48+U05vbVBXWlH/ujq4S6wF4d+L+7PTkksHWrMLn2e+fil0/3NcK7bx4nzw4tzH2+vzv1F02keNpXv9JS97TRvup7TZT5V/Ye8Ulo7dL+Ud93f6cDnKMo8BJ6H6Tw0D/NzuJxHPW+Hy/nV8zkduANqnqfvWRT+PPtLuOLfxBfNa0Tq:5EE1
^FO608,288^GFA,01920,01920,00020,:Z64:
eJzt0z2OwyAQBWAsFy5pUy1H4WIrwdG4ybLaYstQUiDeDj9xyGCtXKXyKHLCZ4MH51mIq95TK2bT8EIBuEchjGsGxGaJfjdbgFTtt5jtywHVfjKdb1M3Glpm8mHfEGtupvIKV8zDbqm3EgU1s+Ryg918+RQzTsZm1KYK1bSTYTKvulGbNKWY8rRI7YYOW7cwm4y0DrMtGlutbIfaqpbQjQZr6l8Q3BbkUybQtzuaiedMh3Om/D/23NtuwzMY7Tab/TgwLbkZp78CN2/ATQfdczBYlKNhzxCzkjVuJZN8bsluZEYZp//m1ehdoJAzU7hP1614RPul0oF9HtjtwK666g31B410Bo0=:1AD0
^FO608,352^GFA,01920,01920,00020,:Z64:
eJzt0ztywyAQgOHVUFByBI7C0cCTIsfIUUKqlLkCPoFJt5ogNuIhFEuW7NKZ0V8xX7XALMDR0T+IB+gigHSgPSi7bTGb8Mm0KUaTEe6b2TS8MpuMY0cBqM5H7kHz1YY/hslYYEPsqOiVxclCtf6G4Ww/sRhHatan43jn0VhYmtfNkE6bJrx+CXVm1B9rU+fRgIRTr7hn8jubdOpztn5tIpuWVs7Gh2xvVn75hb1beWnGaG1dNSOWppK5yaAYGUH7poulr26m0uvLYrQy20zeMJEtArtjPJnYMtOMZRv3ZLKjp+oXkSAgSg==:CFDF
^FO608,448^GFA,01920,01920,00020,:Z64:
eJzt0ztuxCAQBmAQBSVHmKP4aCClyDFylNCl3Ctwg1ASaZbJjHlkg5PVSukijyxkfbZ4/ih11h+L0oEsIbeQlM9qi82A6GCeiN9cFvOhd0fSIRuVYZotL2bYymKWghcrt1YV8GRs0TwnakM4lOe7QZZfD2aaXb8sKcNjGDTXqqkpL0eHYXXaPk00Hz9YmTaWg7bQYrqyGVwt+/tmqYq57J+wb/Pvpsil7bk8YJC2twfMQ4Rhsi+7vUS45MVeI7zfszQsuGHQbRNL84yaUXAjiRwAczDcz5eNY9JNsiEHAc32/ZMMTYvNJGurSSZRrPLnbpLdshpnPLG5W+O7EIaFbnJnxLidpnyf6Vln/cv6BGX8GH8=:0AFB
^FO736,128^GFA,04992,04992,00052,:Z64:
eJzt1jGunDAQANCxKCjdpvNFovhaKVZrRylyLd8gV6BLS+kCMfHYHtuAIX9/vlJEjIS03vUDMcwMC3DHHf9vSIBxfmH/53AoC2pJyxER40eV1gYnEH5nnsmYxqyNodXrBu2ZQX9q3JnJl+mZ6U8mLhqzgJr7BsCfmtF3DVwZ8R6zvG7gX5nniRFHY6+NDI/Og154sw6FqVKpJiNmGF3YZKtRlgwiGxNWKi0bM4Qy2pkhnyaahYzbmGk8Gv4qmpXMtDVyZ0b0Mm2K5juSmbdGeb0xITVyknNJmKHzZLOysW3ewirctBuqUVbB4IGf6ZnRuR+LEXsDRwONkcHEnko1Go3umdR4ezP48+t82RhXjPTn9/M8mEc0Zmbj3mDopzRDkpnebhzXge+ZR9cA19t6dR0RTl7MwmbAgzE83C3I1lg2gku0a3Q19FU2IYMXxvSN9hcGrewZuexNHkC0WKFrShJ6ZukbsZ0hG0MN2DNg3mG2vd01j72RH2H0wSzV+Gjo+HjDM+RoDBtu3a4JT4xMvh9ty3xjEzZqV+cb4RV0kwMqvGhCG37NZoVQo1bN1eA3nD6V50NzOZoRkecbNXSd8WRCAU+6vH8km6Eak4yrRieT0zSyEdVo/EnGVqPwVzA8EAY28e2WjMIwR9MOn4zEHy6/LWMSuM1lmSsxY6b95yhoe5kHd9xxxx133PE38RsxyV0s:5F3E
^FO736,352^GFA,02304,02304,00024,:Z64:
eJzt0z1OxDAQBeBZbZHSNyA3wVfhGFSMEQXlHoGjYMQB9gqmoiQdW1h5jP+NkgCiQBQ7xYvzaYt4ZpboXOc612+LTTsPgCn5E1dR1J/7GGVcumv5pfs1126Pw7R0tgrH6Nq2ZAMz4m3ptyANJD/amp8cp5p8NxO75Azf8t4TLGefWx5OBKOzo+XDRKCxuKn56NYddjdnh4UtmVwll7uX7P2CtCvJcNUpCq146yhj2vDTur9v+KuvfhklJT/7cq/wdDmJn+bShx3CF6aUuVSXZ5hhSplv7XPoQJg3Z+c8F7yImZBxT0ybY/DcZzmVuYdJRY/7o60u+yOTDb+K8zWjK3s1yibIe9oH8bKHSjaHjUr7Y9Q0IPVhDyfvIZOTz32TpR9M/5co/aTgXX3vqtNpw5HnFO/Qu2l+07vl6r5z6ZQt5+sNv+pcx44va4wdX9YAv+oEt+7/sD4Afd5sbQ==:80A7
^FO736,448^GFA,02304,02304,00024,:Z64:
eJzt0zFSQjEQBuAwFK98NzA3MVfxGFYmDIUlR/AohvEAXCFWlr5OisxbdzebsECeWlqwzPDIF2CSfxNjbvXPCwK++XCCAYgGSF0f4XjhI7uF3HUHc9c9QN/3Cx4u3bJDKLPKE2+L7UfPPXdpDbvp2n0c4cDuonYIFj6Vyzo3gDuH4ofYd07S7Tmf7Wx8Ku45ScnzORuIXnw2Lf/dEXfuxOmfB/6xf5lwZbZ6aP6aLlz+H+JqFocIsfWLfSyOe299UX5nXCoJkKfmhl3yOXOrfDp3HFF+Ho59/1rwj9z8Xvtbrvui58kxbclhBbTO6pvm+ETBWd41dq3mTDnQWPqO86Uv8I4zoZ5DfLU+su+r175Tv3As599FV88PJk/fKvfFpnquqFM4lvtlUz2HdBJ8qPdxnAYoOazx5vqwlvuLliU3XPgQyn0vVfM05Kp+91HptOAgN4j3oD2c/El79M2zckwq1s+PC/6g3IHaoirLiV/XALnrBlLfb3WrP9Y3EV9zBg==:0C17
^FO736,288^GFA,00768,00768,00008,:Z64:
eJztzrENgDAMBEAjCspsAJuQ0eLRsgErwBYUKI+UvKUECUFJwTfnxvaLfD8DoLXupZO5ljt3+nhxKYPHng04aKIwtRYReREasqN4+/DQoDgXu0TBPbCZ3cXW/rU+nv0m9nXs34NvgsqfNieMYFSU:E4EC
^FO736,192^GFA,03072,03072,00032,:Z64:
eJzt1E1uwyAQBWAQC5YcgaP4aDjKQXoVR11k2SPEVRfdUnWDFOqXB2lsC2g2VVW18kjIP5+xxjBjIbbYYostfi8cEJcrFf+d9+urn3LeNmnso+1r7x6REu1F9xLdKCdhnwH0ix8xCHC49whP7wo/YZTAKHD+QKC7yr0CZ/IuYsNpGvmZq6PwaIMOHFOH49TwYILxJuhosaPLlMr6+7yOxuv0FCTkpEofVZQj3Wu+u+1CLK5LH9TExQ3W88gMamfOAsGm97jdZFpuFrenhrv7zrWdvXuoXa3d1q7xOrtruIGefWp5uO/Wz45Y+qg4hZ7Xd5cSrdbfLvvzhZvZfenc/+zX/a89pL3LNYZ9qp7SWX82uFv9YSidtWu4vp/12/KRHrMHib509o/GmTNz/9T+hEHhLQrk/oMo/QghcWCNsH9Traw8RXdg+TqeWPa/qKMbbmf1/2Pz7/tfjQu21Oa4:0863
^FO1024,1440^GFA,02048,02048,00016,:Z64:
eJztz70NgCAQhmEIBaUjsIk3GozGKI5gaWH4/CmMd4WJaAhR3u6p7julWl/PJm5CZAa3BsLZRtgKd8IOhnsW58dr+0E43jNfk+temHKthT0wlfTT/TXYYSsdpt14zWs+VPTvr90q2ALVvbPL:0DFA
^FO640,1536^GFA,06144,06144,00064,:Z64:
eJzt1kFu3SAQBmCQFyx9g3KRStwsIHXRY/Qo9U3KEVhSCTH9Z8AOOPHLa7vooqZ5jZ/NZzCeGaLU3e52t7vd7W53u9vvNpv+imuqT/ShfHVtIQpP+MtBVqLtCX85iCWKz/irQRzRhwvI/moQ/2BtRn81CEX3dz6Y8qEvyl14TWp5xq8Xk9SVf/qRtIWOOw/eZAlUG7FiYfAYHBAvQRkK/JwIiG3JquL7Jk+exOOUxzzx0TgDEbyy6IDzKom3FMU7SuKtSKLSPQ4l2A3xVQ/vupcgYCfeUxbv+L22yBXPgb7I/AquEryfPNzkvYzMZ5o3iEF8gsUdX71Ju8dMpTd130bevclIlMjJwn2rZk+zX7o3hb2mL00ic+T92ezSmt3m4jteeV3xr/DvpXnMQLwPLX5sXPGxESsX6gKvZ49VaV6LXw4ZWvy6zWS7rRHrHipetl/O/oW9+tx9Fnn46MLCnnuos8daon/z6oHHseIwrSu8OXvffHt+XEB89Tty/fD4cngLv84efVpvV3B8eE6DFhcP/MvgDY1+j4I6e/c9ePvjXe9OvvQsuvCI323y/uSzRIGavUf+uW89/vPk6Z35v/EV3vf81ZwPvP6y2pp+zp4ee6kfZfR6fn8t/mdPOPKVPTBuMXhk0dv4YS/xs5lA6tPsTZFxi0ri88m3WTRvN5O4bhEi0Rf2mN6atcReyz8Umjl/Dt/yJ3HdQjYFn5vH/iGxC93mf8rf3Y/5O3jsX8lwdNDXwmMudZ3rh9t9qx8R9SNwNvkk3nFqcX4hUlyROChD/Spc5cSP9Qv/Bx/F292jbjp+jt0f9bOvf6+fm519vyXfJHVfp/q9+16/w0pcfeE38UZKP7yVnRjrUOq0f+zx1/YP3qfK6DW+ikdJcbyOLr9M+9ce/+P+Rbx/BfHKxeZ5G9zgUSHF8+sb80+tqf2xhwV93f9UK5/KRPXn7fb/1t/tbnf7L9svFxJy1w==:E4D4
^FO576,1472^GFA,05376,05376,00056,:Z64:
eJztlTuOGzEMhjlQoVJH0FF0s6UWW2zpI/gqClJsmSOsghQuo24VYCCFv6SZsZ218yoCBEMYFj3kp8fwF02022677bbbbrv9unGt8879e45M/CPsrzibxHloP5uLMV9ksWSpdB4BxzOZz9XrWppbqycuNNUqMRflM9VMbGZy3iEyOEkgc6pBuOZKvozebFyb0RXhKiKDU0j6WhFVPT/JGGznkmxSHhFzJfdYERmcFhfJwg03I+quOJnyksOGkCzc5l5wtnPBPVW9cTa7ZIoB19yoZ51d5IB35jLVaGP1nFi4hMjCRZPMLMmpu1HNJtnIfuVcYM/RReEQGZwLOpuswDU3TjNobjVy8wTOeQ42uuc278J5lU2aGgdXuClecNoLJ8sLh8glV844PD/jZACX3CEsypRvJtlYJHDdVSKl9XxSU0jKM+nO2ZucydQO1bgH9YYSd+4YqC3/LicSICdcrx+Dq4MTjbk7HPQZh85Yv4kCF26o/QYX1MY5fYKEOlcg5jv7lAWwSOMOJ0yx7FPf47Kpn3AocPbwIkd1gwstcuN96tmgitWsXBzcMSJyu+6iU6i0ca8vUsql7tH+oJdNZ1Ss1E63mZ15fRbpLDpLNmxc0/W06RqcHTtypoITvYTBrXrp90ht90i4ODxyumrZJ+6R3IenjMj1vU3djVOxicdJOvdhubczItd9InU3qoKan3OoH/rE42y3+q19KS1u09rQtSoaUvW9LxWzcWsfTDT6YNPMuEeDC60P+tJUf9V3E42+Ky3qtHKzFp0dw+i7iJz3+S8ivUSjz6epfky133epj0jVBLbo84zIwhl5IbEQONPeTSBW38rGQXzcZCH1UOd/OT//O+N3n+7czv0Ot9tu/499Bx3EGzM=:869D
^BY2,3,45^FT78,875^BCN,,N,N
^FD>:{Mid(sn(1), 1, 11)}>5{Mid(sn(1), 12)}^FS
^FT153,904^A0N,29,28^FH\^FD{sn(1)}^FS
^FT78,907^A0N,29,28^FH\^FDS/N:^FS
^BY2,3,45^FT78,995^BCN,,N,N
^FD>:{Mid(sn(2), 1, 11)}>5{Mid(sn(2), 12)}^FS
^FT153,1024^A0N,29,28^FH\^FD{sn(2)}^FS
^FT78,1027^A0N,29,28^FH\^FDS/N:^FS
^BY2,3,45^FT78,1123^BCN,,N,N
^FD>:{Mid(sn(3), 1, 11)}>5{Mid(sn(3), 12)}^FS
^FT153,1152^A0N,29,28^FH\^FD{sn(3)}^FS
^FT78,1155^A0N,29,28^FH\^FDS/N:^FS
^BY2,3,45^FT78,1240^BCN,,N,N
^FD>:{Mid(sn(4), 1, 11)}>5{Mid(sn(4), 12)}^FS
^FT153,1269^A0N,29,28^FH\^FD{sn(4)}^FS
^FT78,1272^A0N,29,28^FH\^FDS/N:^FS
^BY2,3,45^FT78,1366^BCN,,N,N
^FD>:{Mid(sn(5), 1, 11)}>5{Mid(sn(5), 12)}^FS
^FT153,1395^A0N,29,28^FH\^FD{sn(5)}^FS
^FT78,1398^A0N,29,28^FH\^FDS/N:^FS
^BY3,3,45^FT705,871^BCN,,N,N
^FD>;35527268007001>64^FS
^FT801,900^A0N,29,28^FH\^FD355272680070014^FS
^FT705,900^A0N,29,31^FH\^FDIMEI:^FS
^BY3,3,45^FT705,995^BCN,,N,N
^FD>;35527268007001>64^FS
^FT801,1024^A0N,29,28^FH\^FD355272680070014^FS
^FT705,1024^A0N,29,31^FH\^FDIMEI:^FS
^BY3,3,45^FT709,1123^BCN,,N,N
^FD>;35527268007001>64^FS
^FT805,1152^A0N,29,28^FH\^FD355272680070014^FS
^FT709,1152^A0N,29,31^FH\^FDIMEI:^FS
^BY3,3,45^FT705,1240^BCN,,N,N
^FD>;35527268007001>64^FS
^FT801,1269^A0N,29,28^FH\^FD355272680070014^FS
^FT705,1269^A0N,29,31^FH\^FDIMEI:^FS
^BY3,3,45^FT705,1362^BCN,,N,N
^FD>;35527268007001>64^FS
^FT801,1391^A0N,29,28^FH\^FD355272680070014^FS
^FT705,1391^A0N,29,31^FH\^FDIMEI:^FS
^BY3,2,95^FT99,1606^BEN,,Y,N
^FD4680059764083^FS
^FT78,1502^A0N,38,48^FH\^FDEAN Code:^FS
^BY3,3,95^FT78,681^BCN,,N,N
^FD>:LTab2>5022300300001^FS
^FT153,719^A0N,38,38^FH\^FDLTab2022300300001^FS
^FT78,577^A0N,38,38^FH\^FDCarton No:^FS
^PQ1,0,1,Y^XZ
"
        Return str
    End Function
#End Region
End Class

