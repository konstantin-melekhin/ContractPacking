Imports System.Deployment.Application
Imports System.Drawing.Printing
Imports System.IO
Imports Library3

Public Class WF_SberDevice
#Region "Переменные"
    Dim LOTID, IDApp, UnitCounter, PCBID, SNID, PalletNumber, BoxNumber, LabelScenario As Integer
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
        If LOTInfo(15) = 20 Then
            LabelScenario = 1
        ElseIf LOTInfo(15) = 18 Then
            LabelScenario = 2
        End If
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
                    ElseIf _stepArr.Count > 0 And _stepArr(4) = 37 And _stepArr(5) = 2 Then '' - станция взвешивания (_stepArr(4) = 25 Or _stepArr(4) = 30)
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
                insert into [FAS].[dbo].[Ct_PackingTable] (SNID,LOTID, LiterID,LiterIndex,PalletNum,BoxNum,UnitNum,PackingDate,UserID)values
                ({_SNInfo(2)},{ LOTID },{ PCInfo(8) },{ LOTInfo(17) },{ PalletNumber },{ BoxNumber },{ UnitCounter },current_timestamp,{ UserInfo(0) } )
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
            RawPrinterHelper.SendStringToPrinter(DefPrt, GetGroupLabel(SNArray, x, y, LabelScenario))
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
                If (LabelScenario = 1 And SNArray.Count = 21) Or (LabelScenario = 2 And SNArray.Count = 19) Then
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
            If (LabelScenario = 1 And SNArray.Count = 21) Or (LabelScenario = 2 And SNArray.Count = 19) Then
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
            from  (SELECT *, ROW_NUMBER() over(partition by snid order by stepdate desc) num FROM [FAS].[dbo].[Ct_OperLog] where LOTID = {LOTID} and  SNID  = {_snid}) tt
            where  tt.num = 1 "))
        Return newArr
    End Function
#End Region
#Region " 10. Групповая этикетка"
    Private Function GetGroupLabel(sn As ArrayList, x As Integer, y As Integer, w As Integer)
        Dim str As String
        If w = 1 Then
            str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO128,352^GFA,09216,09216,00032,:Z64:
eJztmLFOwzAQhl156ILiN0heg8FqXoVHcMVAkCoSiZ1XwhJ7n8ESO4IdEYbGYun9f5XETYv8Lx0+6Xw+x3e/q1RWVlbWxcr0xxVO5Ne+/ljp78OvkF+oeszrvoPxb9895G4bYHzn0vLmC/N6yF+szxven3mGeLJMhXn92mHe+0McYX+PpD4uBBg/NWf5sf2x+rD6TlahYH6Mt0MHScUtWb8s8PbWL5jrPY6/+iD1eSL13RBekvgK89g/JVWkP8bvU5LVAXKnAswvNd9pDzm7f6w++gfi6cr3D8Y/1/0bK9MFmF/tCQ+YN5+YuybA/JwlvCTxDclvjflGY16ucH6FuoM8amn/e6HrB8ZviL+wdYd562F85m9T+7OGcLv1kFdkPho2P8h8/osz7vwaEndHOGufbYf5Q8D8nsxPR+aPqwgn/cPh5SfzqKX9mzi/Sf+dy59JYjz6N7Y/SXP5t3x+x/Xfz08R/8r4pfhviUf/LX6/e7y/6L9FLfz+jf5FrN/gX0Q+k3+ZyiWd6l/k9y3mif3L0v53+f+/Z+ovqbgl68/1vpd0rv6SlZWVlXWF+gWblDol:CABD
^FO320,160^GFA,01536,01536,00008,:Z64:
eJzlkzEOwjAMRRNZIkvVMHaoFI7AioQoV0Fcgo0cLUfpETIyIFJUfw+tmhYGhBBeXqX8bzt2o9Tvhw7TXLLdmZSYNvmeJTFr5KnUPEUnPslj0rBOJkxKYYrPL9BBuQdvyNsOZRbHB/ACWRzK8vU+HAZ1LOjAGudVhgW4At3I7xb6//aeR3kI8xbqhDy5++dYgu/OQ6kj+g64h5D7qM+oc0Jf2zDLYgd/wzSOfSQ/5Cg2a2bkxevYsLxlPXnT+43nORu851dJnoS9XwfOpyPeUXowr9iXbaf7/IvoAILacDI=:C139
^FO96,896^GFA,31104,31104,00036,:Z64:
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:4CF9
^FT159,878^BQN,2,3
^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}\0D\{sn(11)}\0D\{sn(12)}\0D\{sn(13)}\0D\{sn(14)}\0D\{sn(15)}\0D\{sn(16)}\0D\{sn(17)}\0D\{sn(18)}\0D\{sn(19)}\0D\{sn(20)}^FS
^FO437,33^GB0,1697,5^FS
^BY2,3,84^FT570,1716^BCB,,Y,N
^FD>:{Mid(sn(1), 1, 10)}>5{Mid(sn(1), 11)}^FS
^BY2,3,84^FT570,1297^BCB,,Y,N
^FD>:{Mid(sn(2), 1, 10)}>5{Mid(sn(2), 11)}^FS
^BY2,3,84^FT570,879^BCB,,Y,N
^FD>:{Mid(sn(3), 1, 10)}>5{Mid(sn(3), 11)}^FS
^BY2,3,84^FT570,460^BCB,,Y,N
^FD>:{Mid(sn(4), 1, 10)}>5{Mid(sn(4), 11)}^FS
^BY2,3,84^FT695,1716^BCB,,Y,N
^FD>:{Mid(sn(5), 1, 10)}>5{Mid(sn(5), 11)}^FS
^BY2,3,84^FT695,1297^BCB,,Y,N
^FD>:{Mid(sn(6), 1, 10)}>5{Mid(sn(6), 11)}^FS
^BY2,3,84^FT695,879^BCB,,Y,N
^FD>:{Mid(sn(7), 1, 10)}>5{Mid(sn(7), 11)}^FS
^BY2,3,84^FT695,460^BCB,,Y,N
^FD>:{Mid(sn(8), 1, 10)}>5{Mid(sn(8), 11)}^FS
^BY2,3,84^FT820,1716^BCB,,Y,N
^FD>:{Mid(sn(9), 1, 10)}>5{Mid(sn(9), 11)}^FS
^BY2,3,84^FT820,1297^BCB,,Y,N
^FD>:{Mid(sn(10), 1, 10)}>5{Mid(sn(10), 11)}^FS
^BY2,3,84^FT820,879^BCB,,Y,N
^FD>:{Mid(sn(11), 1, 10)}>5{Mid(sn(11), 11)}^FS
^BY2,3,84^FT820,460^BCB,,Y,N
^FD>:{Mid(sn(12), 1, 10)}>5{Mid(sn(12), 11)}^FS
^BY2,3,84^FT945,1716^BCB,,Y,N
^FD>:{Mid(sn(13), 1, 10)}>5{Mid(sn(13), 11)}^FS
^BY2,3,84^FT945,1297^BCB,,Y,N
^FD>:{Mid(sn(14), 1, 10)}>5{Mid(sn(14), 11)}^FS
^BY2,3,84^FT945,879^BCB,,Y,N
^FD>:{Mid(sn(15), 1, 10)}>5{Mid(sn(15), 11)}^FS
^BY2,3,84^FT945,460^BCB,,Y,N
^FD>:{Mid(sn(16), 1, 10)}>5{Mid(sn(16), 11)}^FS
^BY2,3,84^FT1071,1716^BCB,,Y,N
^FD>:{Mid(sn(17), 1, 10)}>5{Mid(sn(17), 11)}^FS
^BY2,3,84^FT1071,1297^BCB,,Y,N
^FD>:{Mid(sn(18), 1, 10)}>5{Mid(sn(18), 11)}^FS
^BY2,3,84^FT1071,879^BCB,,Y,N
^FD>:{Mid(sn(19), 1, 10)}>5{Mid(sn(19), 11)}^FS
^BY2,3,84^FT1071,460^BCB,,Y,N
^FD>:{Mid(sn(20), 1, 10)}>5{Mid(sn(20), 11)}^FS
^BY3,3,181^FT336,321^BCB,,N,N
^FD>;{Mid(Integer.Parse(sn(0).Split(";")(0)).ToString("00000"), 1, 4) & ">6" & Mid(Integer.Parse(sn(0).Split(";")(0)).ToString("00000"), 5)}^FS
^FT364,180^A0B,29,28^FH\^FD{Integer.Parse(sn(0).Split(";")(0)).ToString("00000")}^FS
^FT364,106^A0B,29,28^FH\^FD{sn(0).Split(";")(1)}{LOTInfo(17)}^FS
^PQ1,0,1,Y^XZ
"
        ElseIf w = 2 Then
            str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO128,352^GFA,09216,09216,00032,:Z64:
eJztl71qwzAUhRU0ZCn2G9iv0UHYr9JHkMlQD6E2dO8zORSaLc8g6F7avdSlYJMl95wQS3Ya9K1fuLrW74lSkUgkcrWk/Wncmf5Gx3fUF9jbvoW+6jvoNzviWzwDoX2zc7C/Zo/7z/ctrK/esJ6K/la4vx77clhfifv3DnpbOVjf2rC+/sK+HPoX5+cVf1/6DPXxd0J96uP5g/3Ndf4uXb/x/Enk5HyVw/pJGO2gt8rB/kL7re6gL8n+ZPOjf6CeTqJgf8w3wxYJ5Q0ZP0vw561fsNcHXH/1QebnicxvQXxGPCEd7hdx/3XEO+zrT+xt7WB/1hCfkfop6W+NfaGxz1a4v0Q9QD9ypfl36fEd83c5rmvKFvumg/VZPrLOibX/mOpr4k3VQZ+T9zFl7wd5n491Llu/mtTdEs+uz6bF/tFhvyHvpyXvj82JJ/eHxcNP9iNL5zfx/Sb3r698JsH8mN/Y90n4ym9x/U5z6+unSH5l/lryt+TH/C3u3wP+vjF/i3jK31Pzizh/Q34Rvaf8MtVLnJtf5P+32AfOL0vn39nGF/F0v4Tyhozv6/+9xFz3SyQSiUT+Ib8dB53k:EF98
^FO320,160^GFA,01536,01536,00008,:Z64:
eJzlkzEOwjAMRRNZIkvVMHaoFI7AioQoV0Fcgo0cLUfpETIyIFJUfw+tmhYGhBBeXqX8bzt2o9Tvhw7TXLLdmZSYNvmeJTFr5KnUPEUnPslj0rBOJkxKYYrPL9BBuQdvyNsOZRbHB/ACWRzK8vU+HAZ1LOjAGudVhgW4At3I7xb6//aeR3kI8xbqhDy5++dYgu/OQ6kj+g64h5D7qM+oc0Jf2zDLYgd/wzSOfSQ/5Cg2a2bkxevYsLxlPXnT+43nORu851dJnoS9XwfOpyPeUXowr9iXbaf7/IvoAILacDI=:C139
^FO128,1088^GFA,10240,10240,00016,:Z64:
eJztmk2u3DYMgGUYiDcBfIEA6hFygAK6VhYB7KIHq4NexEAX3Q7QjReGWYs/4o/fTNJm0deXEZB4vjfPFCWRlES+lP437T0/3/HzAz8Hfv4svNBzF17x0TXe6NGYPoyND3zkxoCPSRk7AOXaQW94w/+V66fRcO2gwMbcA8yneLjJQKF2ALAK16/ol6jl89UBQBBFZzga1w5U/Dny810jvsr+AjQIagX+MuJrB2DE1w6gac8aG/HYgWpLGlvxtQMrLjltSePtIY+u+yrPvz859VEf23/V1wwfx2M7yGF8Uxg/+Pk5xS12wEOY31Pc+U8FlvPzaDQGWj8R2FVZnWrc8/qLQFpqXVCyn/zA3m7J2+OKvShjz1Ow93LXH6K/0MjVn1jxxjww8cfor9Lep29vbvmSm0382ttftM+63vaFUtd3Vsb1Xpw4azAdsgrskXV9B2TtYERWA8qBqz2Y9T95ZacXdZb6w8b46qgjxK4HwxsqJQp3+KkLnNoAOnrTxB/saQrc7KEnTUuwn3yf00s8Hm+Lc+R78xHn7zq/yJPyinxvvWR9m0GG9Rf7aDxVTUe7n8zVKBujqU1qbzXU+P0G5mI42vcY7D/6Q/SX6F/kf6tyCf44hng8hHidQnxPxe8HxhleTfvscbTjzSns39s5RTbe7CnEmy7Em1/BxRf4HVx8gT8tA/Fq2C5Ix3yzfAye987Y51ZPKCaerPWXbXypJl4MVxO38aay858LL97fPKcLT09+MF/gHPS187O9+TbNyZ4PR4w1en6k479GUAx27TzKx/++MR3/2/kVDWpT++frQNt/+Tqg9la3o0PPz7hdHYNnMFcUtNbfAoPl2XJ3jia7+LqhSkvjG6o8t+GsOCQd3lKHvAfelGd3nWReAgvS+WWy23kypxHh2xvm1xXvnvHzm1rz53df4SHyfGV73qj3W8dr4Fvg7Su8Bz6+wvDk7+TO8+vyx+/l/k369wePnU8o8pFAm823pWk992TdwDq8L9kNDu9Ls8qCX8BsiDnclwrAH2A20BLuSxPNv8nfPmS5T22Oe8978rzZ/MCW633K5Avq/crcn9beM9637P0p+MOF02P+z/35tfGbji8/xPlh8Akjmx5LP6Xq43oA/piSy798Ih9tvFFMbgL3wAfN6cLYAfFqeHQ8I0v8qOf7/ALb/G0xXIdW4ItZz8q2nnXy4fk8/hdr7+eXkV3+EwyPgCnJf8Y3x90jHq7ce54x3j3gwfMSuWbjDK/Z5kvOzmrG1MRXXNzIbT67l9jkexOxyeeicSlPlU0Ks1RfNJwBE7aNR8CC4So8VFsy9QIsMNkNFW13Usah5jm5NgR+th+o+fJuGkI9d/D1NpeeELYSOrAvjGLy3MqMR6pZ+LTMycXDFeObxr/VVXDrYchW/AbOp4hArIyaAEyZnal10OGbg3aAYzEZFayfmIw8leq0pk2sc0R7iVRUPsrZlRX4lDiVzhWYz4mr34UUOqSUm6nuc7CCLKbuFtTTwHWgOnttnLgbkMI0zgFnD3sCqQvlC6+kEPa6iMKFeFYFmZuCM/2wuzArvHBiiRRWJgXxDyumvU0FhizkooyiZUZXDpMjl7yFB54a5S1RIq3jSdob39qSK8uSN54uPFOirpOpqQoq5wuvjknh+99HjvJL6J/1uae/Hd/mOIX5sNy3+RbTEjbzPe1Jiou6XtnwYdbv/L8oj1Tnbus/ir2wfWSqU85XplYgucJZCXXyWPdkv/Z1Unvv2tvQk5kUG0cwDjA3N2aFsnPj5r9FFOAviijA/i83V4kP8rcNI3ckfykg8aUH8c6d1Ra9NpFL8yD9UGTT+FZj55w1/mU+38u45Lwv477kF5i1vky8CLPAOTmB9j4OLv5f/v4i1ns7CPXdHPafZ/u37W9YSiGT:2DA2
^FO256,896^GFA,13312,13312,00016,:Z64:
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:97A6
^FT159,878^BQN,2,3
^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}\0D\{sn(11)}\0D\{sn(12)}\0D\{sn(13)}\0D\{sn(14)}\0D\{sn(15)}\0D\{sn(16)}\0D\{sn(17)}\0D\{sn(18)}^FS
^FO437,33^GB0,1697,5^FS
^BY2,3,84^FT570,1716^BCB,,Y,N
^FD>:{Mid(sn(1), 1, 10)}>5{Mid(sn(1), 11)}^FS
^BY2,3,84^FT570,1297^BCB,,Y,N
^FD>:{Mid(sn(2), 1, 10)}>5{Mid(sn(2), 11)}^FS
^BY2,3,84^FT570,879^BCB,,Y,N
^FD>:{Mid(sn(3), 1, 10)}>5{Mid(sn(3), 11)}^FS
^BY2,3,84^FT570,460^BCB,,Y,N
^FD>:{Mid(sn(4), 1, 10)}>5{Mid(sn(4), 11)}^FS
^BY2,3,84^FT695,1716^BCB,,Y,N
^FD>:{Mid(sn(5), 1, 10)}>5{Mid(sn(5), 11)}^FS
^BY2,3,84^FT695,1297^BCB,,Y,N
^FD>:{Mid(sn(6), 1, 10)}>5{Mid(sn(6), 11)}^FS
^BY2,3,84^FT695,879^BCB,,Y,N
^FD>:{Mid(sn(7), 1, 10)}>5{Mid(sn(7), 11)}^FS
^BY2,3,84^FT695,460^BCB,,Y,N
^FD>:{Mid(sn(8), 1, 10)}>5{Mid(sn(8), 11)}^FS
^BY2,3,84^FT820,1716^BCB,,Y,N
^FD>:{Mid(sn(9), 1, 10)}>5{Mid(sn(9), 11)}^FS
^BY2,3,84^FT820,1297^BCB,,Y,N
^FD>:{Mid(sn(10), 1, 10)}>5{Mid(sn(10), 11)}^FS
^BY2,3,84^FT820,879^BCB,,Y,N
^FD>:{Mid(sn(11), 1, 10)}>5{Mid(sn(11), 11)}^FS
^BY2,3,84^FT820,460^BCB,,Y,N
^FD>:{Mid(sn(12), 1, 10)}>5{Mid(sn(12), 11)}^FS
^BY2,3,84^FT945,1716^BCB,,Y,N
^FD>:{Mid(sn(13), 1, 10)}>5{Mid(sn(13), 11)}^FS
^BY2,3,84^FT945,1297^BCB,,Y,N
^FD>:{Mid(sn(14), 1, 10)}>5{Mid(sn(14), 11)}^FS
^BY2,3,84^FT945,879^BCB,,Y,N
^FD>:{Mid(sn(15), 1, 10)}>5{Mid(sn(15), 11)}^FS
^BY2,3,84^FT945,460^BCB,,Y,N
^FD>:{Mid(sn(16), 1, 10)}>5{Mid(sn(16), 11)}^FS
^BY2,3,84^FT1071,1716^BCB,,Y,N
^FD>:{Mid(sn(17), 1, 10)}>5{Mid(sn(17), 11)}^FS
^BY2,3,84^FT1071,1297^BCB,,Y,N
^FD>:{Mid(sn(18), 1, 10)}>5{Mid(sn(18), 11)}^FS
^BY3,3,181^FT336,321^BCB,,N,N
^FD>;{Mid(Integer.Parse(sn(0).Split(";")(0)).ToString("00000"), 1, 4) & ">6" & Mid(Integer.Parse(sn(0).Split(";")(0)).ToString("00000"), 5)}^FS
^FT364,180^A0B,29,28^FH\^FD{Integer.Parse(sn(0).Split(";")(0)).ToString("00000")}^FS
^FT364,106^A0B,29,28^FH\^FD{sn(0).Split(";")(1)}{LOTInfo(17)}^FS
^FT136,502^A0B,83,81^FH\^FDQTY: 18 PCS^FS
^FT94,864^A0B,42,40^FH\^FDN.W.:^FS
^FT146,864^A0B,42,40^FH\^FDG.W.:^FS
^FT94,758^A0B,42,40^FH\^FD6,192 kg^FS
^FT146,758^A0B,42,40^FH\^FD6,575 kg^FS
^PQ1,0,1,Y^XZ
"
        End If
        Return str
    End Function
#End Region
End Class


'^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
'^XA
'^MMT
'^PW1181
'^LL1772
'^LS0
'^FO128,352^GFA,09216,09216,00032,:Z64:
'eJztmMFthDAQRb3ygUu0dACNWEsrKcFRDuGwClSwNVFBanAFUXKPwipKrFx2/kfYLGwy7/qk8diI8QdjFEVRNks5XiZM9Le+/lzK/rsDob/QDMQH7Ns37H0bYH/eEV+R+iXpr8D+YLGvdri/vbmHPiLVv5afS0dKTPXi+ZL+S+IL4m0/b9+50PcP+62/f/YD163HHvpmHKB3NkDvTYD9Le2PdoA+7k/y7HzsJ9Tp7A3sj3k2v1K9I+tXe7y94oS9fcH1d6/kfJ7J+R6Ir4gn6PzEfuX5+W/y99z7467GdV3TY98NsH77HqD3IYi1v0j1LfHuYYC+Jvdjye4Pcj//1pn3/FpS90g8G59dj/1TwP6R3J+e3D++Jp7MD4+XT/aRtfObeH+T+Zsrn0kwH/Mb259Ervymz+8yf/35GZJfmd9K/hb/P5ywj/lbIuZvkUz5OzW/iOf3k19Enym/pHqJqflF/r7FfuH8snb+Xf//d6b5spR3ZP1c3/cS15oviqIoyg1yBqgajgc=:9CF5
'^FO320,160^GFA,01536,01536,00008,:Z64:
'eJzlkzEOwjAMRRNZIkvVMHaoFI7AioQoV0Fcgo0cLUfpETIyIFJUfw+tmhYGhBBeXqX8bzt2o9Tvhw7TXLLdmZSYNvmeJTFr5KnUPEUnPslj0rBOJkxKYYrPL9BBuQdvyNsOZRbHB/ACWRzK8vU+HAZ1LOjAGudVhgW4At3I7xb6//aeR3kI8xbqhDy5++dYgu/OQ6kj+g64h5D7qM+oc0Jf2zDLYgd/wzSOfSQ/5Cg2a2bkxevYsLxlPXnT+43nORu851dJnoS9XwfOpyPeUXowr9iXbaf7/IvoAILacDI=:C139
'^FO96,896^GFA,31104,31104,00036,:Z64:
'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:4CF9
'^FT159,878^BQN,2,3
'^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}\0D\{sn(11)}\0D\{sn(12)}\0D\{sn(13)}\0D\{sn(14)}\0D\{sn(15)}\0D\{sn(16)}\0D\{sn(17)}\0D\{sn(18)}\0D\{sn(19)}\0D\{sn(20)}^FS
'^FO437,33^GB0,1697,5^FS
'^BY2,3,84^FT570,1716^BCB,,Y,N
'^FD>:{Mid(sn(1), 1, 10)}>5{Mid(sn(1), 11)}^FS
'^BY2,3,84^FT570,1297^BCB,,Y,N
'^FD>:{Mid(sn(2), 1, 10)}>5{Mid(sn(2), 11)}^FS
'^BY2,3,84^FT570,879^BCB,,Y,N
'^FD>:{Mid(sn(3), 1, 10)}>5{Mid(sn(3), 11)}^FS
'^BY2,3,84^FT570,460^BCB,,Y,N
'^FD>:{Mid(sn(4), 1, 10)}>5{Mid(sn(4), 11)}^FS
'^BY2,3,84^FT695,1716^BCB,,Y,N
'^FD>:{Mid(sn(5), 1, 10)}>5{Mid(sn(5), 11)}^FS
'^BY2,3,84^FT695,1297^BCB,,Y,N
'^FD>:{Mid(sn(6), 1, 10)}>5{Mid(sn(6), 11)}^FS
'^BY2,3,84^FT695,879^BCB,,Y,N
'^FD>:{Mid(sn(7), 1, 10)}>5{Mid(sn(7), 11)}^FS
'^BY2,3,84^FT695,460^BCB,,Y,N
'^FD>:{Mid(sn(8), 1, 10)}>5{Mid(sn(8), 11)}^FS
'^BY2,3,84^FT820,1716^BCB,,Y,N
'^FD>:{Mid(sn(9), 1, 10)}>5{Mid(sn(9), 11)}^FS
'^BY2,3,84^FT820,1297^BCB,,Y,N
'^FD>:{Mid(sn(10), 1, 10)}>5{Mid(sn(10), 11)}^FS
'^BY2,3,84^FT820,879^BCB,,Y,N
'^FD>:{Mid(sn(11), 1, 10)}>5{Mid(sn(11), 11)}^FS
'^BY2,3,84^FT820,460^BCB,,Y,N
'^FD>:{Mid(sn(12), 1, 10)}>5{Mid(sn(12), 11)}^FS
'^BY2,3,84^FT945,1716^BCB,,Y,N
'^FD>:{Mid(sn(13), 1, 10)}>5{Mid(sn(13), 11)}^FS
'^BY2,3,84^FT945,1297^BCB,,Y,N
'^FD>:{Mid(sn(14), 1, 10)}>5{Mid(sn(14), 11)}^FS
'^BY2,3,84^FT945,879^BCB,,Y,N
'^FD>:{Mid(sn(15), 1, 10)}>5{Mid(sn(15), 11)}^FS
'^BY2,3,84^FT945,460^BCB,,Y,N
'^FD>:{Mid(sn(16), 1, 10)}>5{Mid(sn(16), 11)}^FS
'^BY2,3,84^FT1071,1716^BCB,,Y,N
'^FD>:{Mid(sn(17), 1, 10)}>5{Mid(sn(17), 11)}^FS
'^BY2,3,84^FT1071,1297^BCB,,Y,N
'^FD>:{Mid(sn(18), 1, 10)}>5{Mid(sn(18), 11)}^FS
'^BY2,3,84^FT1071,879^BCB,,Y,N
'^FD>:{Mid(sn(19), 1, 10)}>5{Mid(sn(19), 11)}^FS
'^BY2,3,84^FT1071,460^BCB,,Y,N
'^FD>:{Mid(sn(20), 1, 10)}>5{Mid(sn(20), 11)}^FS
'^BY3,3,181^FT336,321^BCB,,N,N
'^FD>;{Mid(Integer.Parse(sn(0).Split(";")(0)).ToString("00000"), 1, 4) & ">6" & Mid(Integer.Parse(sn(0).Split(";")(0)).ToString("00000"), 5)}^FS
'^FT364,180^A0B,29,28^FH\^FD{Integer.Parse(sn(0).Split(";")(0)).ToString("00000")}^FS
'^FT364,106^A0B,29,28^FH\^FD{sn(0).Split(";")(1)}{LOTInfo(17)}^FS
'^PQ1,0,1,Y^XZ






