Imports System.Deployment.Application
Imports System.Drawing.Printing
Imports System.IO
Imports Library3

Public Class WF_SberDevice
#Region "Переменные"
    Dim LOTID, IDApp, UnitCounter, PCBID, SNID, PalletNumber, BoxNumber, LabelScenario As Integer
    Dim ds As New DataSet
    Dim LenSN_SMT, LenSN_FAS, StartStepID, PreStepID, NextStepID As Integer
    Dim StartStep, PreStep, NextStep, Litera, WNetto, WBrutto As String
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
        Controllabel.Text = ""
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
        If LOTInfo(15) = 20 And LOTInfo(20) = 34 Then
            LabelScenario = 1
        ElseIf LOTInfo(15) = 18 And LOTInfo(20) = 34 Then
            LabelScenario = 2
        ElseIf LOTInfo(15) = 20 And LOTInfo(20) = 44 Then
            LabelScenario = 3
        ElseIf LOTInfo(15) = 10 And LOTInfo(20) = 44 Then
            If LOTInfo(0) = "T1100" Then
                LabelScenario = 4
            ElseIf LOTInfo(0) = "T800" Then
                LabelScenario = 5
            End If
        End If
        L_PalletCapacity.Text = LOTInfo(16)
        L_Liter.Text = If(LOTInfo(17) = 0, PCInfo(9), PCInfo(9) & " " & LOTInfo(17))
        'Запуск программы
        '___________________________________________________________
        GetWeight()
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
    Private Function GetWeight()
        Dim weight As String = SelectString($"SELECT  Weight FROM [FAS].[dbo].[FAS_Models]where [ModelName] = '{LOTInfo(0)}'")
        If weight = "" Then
            GB_GetWeight.Location = New Point(10, 12)
            GB_GetWeight.Visible = True
            TB_Netto.Focus()
        Else
            WNetto = weight.Split(";")(0)
            WBrutto = weight.Split(";")(1)
        End If
    End Function
    Private Sub BT_SeveWeight_Click(sender As Object, e As EventArgs) Handles BT_SeveWeight.Click
        If TB_Netto.Text <> "" And TB_Brutto.Text Then
            RunCommand($"USE FAS update [FAS].[dbo].[FAS_Models] set Weight = '{TB_Netto.Text};{TB_Brutto.Text};' where ModelName  ='{LOTInfo(0)}'")
            GB_GetWeight.Visible = False
            GetWeight()
        Else
            MsgBox("Не заполнены поля ввода массы! Заполните и повторите сохранение!")
        End If
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
                        'ElseIf _stepArr.Count > 0 And _stepArr(4) = 25 And _stepArr(5) = 2 Then '' - станция взвешивания (_stepArr(4) = 25 Or _stepArr(4) = 30)
                    ElseIf _stepArr.Count > 0 And _stepArr(4) = PreStepid And _stepArr(5) = 2 Then '' - станция взвешивания (_stepArr(4) = 25 Or _stepArr(4) = 30)
                        'проверка задвоения и наличия номера в базе
                        If CheckDublicate(_stepArr(2)) = True Then
                            WriteDB(_stepArr)
                            PrintLabel(Controllabel, $"Серийный номер {SerialTextBox.Text} { vbCrLf}определен и записан в базу!", 12, 193, Color.Green)
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
        SNID = New Integer
        SNFormat = GetSNFormat(LOTInfo(3), LOTInfo(8), LOTInfo(19).Split(";")(2), SerialTextBox.Text, LOTInfo(18), LOTInfo(2), LOTInfo(7))
        Res = SNFormat(0)
        Mess = SNFormat(3)
        col = If(Res = False, Color.Red, Color.Green)
        PrintLabel(Controllabel, Mess, 12, 193, col)
        SerialTextBox.Enabled = Res
        'SNID = If(SNFormat(1) = 2,
        '        SelectInt($"USE FAS Select [ID] FROM [FAS].[dbo].[Ct_FASSN_reg] where SN = '{SerialTextBox.Text}'"),
        '        SelectInt($"USE FAS SELECT [ID] FROM [FAS].[dbo].[Ct_FASSN_reg] where LOTID = {LOTID} and right (SN, 7) = '{CInt("&H" & Mid(SerialTextBox.Text, 7, 6))}'"))
        SNID = SelectInt($"USE FAS 
                        select id FROM [FAS].[dbo].[Ct_FASSN_reg] 
                        where sn = (select top (1) sn  FROM [FAS].[dbo].[CT_Aquarius] 
                        where IMEI = '{SerialTextBox.Text}' or IMEI2 = '{SerialTextBox.Text}' or SN = '{SerialTextBox.Text}')")

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
                insert into [FAS].[dbo].[Ct_PackingTable] (pcbid,SNID,LOTID, LiterID,LiterIndex,PalletNum,BoxNum,UnitNum,PackingDate,UserID)values
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
            GB_GetWeight.Location = New Point(10, 12)
            GB_GetWeight.Visible = True
            TB_Netto.Focus()
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

    Private Sub GB_ManualPrint_Enter(sender As Object, e As EventArgs) Handles GB_ManualPrint.Enter

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
                If SNArray.Count = LOTInfo(15) + 1 Then
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
            If SNArray.Count = LOTInfo(15) + 1 Then
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
            from  (SELECT *, ROW_NUMBER() over(partition by snid order by stepdate desc) num FROM [FAS].[dbo].[Ct_OperLog] where SNID  = {_snid}) tt
            where  tt.num = 1 "))
        Return newArr
    End Function
#End Region
#Region " 10. Групповая этикетка"
    Private Function GetGroupLabel(sn As ArrayList, x As Integer, y As Integer, w As Integer)
        'For i = 1 To 9
        '    sn.Add($"RD00R570EPF00361{i}{i}")
        'Next
        'For i = 1 To 8
        '    sn.Add($"RD00R570EPF003{i}{i}61")
        'Next

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
eJztXc2PHMd1r+4WOYtdYkc+GOJhzRnIlwUVaJnbYGVv74GIrwpgwjkE0OSW4/pgmAkoTWcXiAgZIP+ENKQLMQTIK0EH3BYShLcoASLkIkWdGAgWK4BaA3G0Xq2mU++9+nhV0697jXxcwge73Z5589vqqlfvu5pKvaSX9P+H0l6OS+pyL8/W0XovT37aj7P9rB9nb70fp16/18szzf6yHyd71MtTqX8XvpnPHY56IfA0jRuPeq+dJWsWHmennSf5uw17u6euCTj7bnKPlDDPWeYm94a07tk+ExpBfrLLg/YvBJwViSdzOMm7Es+BW+y15qidJ/n4X+ztqDkVgAo3z0/vPhN41G17syfNIaP6AjyV6p+qWq328kwlnJTLobBeiZexqbjux/PC3Iryk1Tu9o76gTBSP//5L6W1uOPuhs2JwLNuh6NWXxQCT1YJX3Cq/2d4Vh3P6rNC4Bm6u5H4XH6P5w+l+fGf31Eb7SyZ3wvyfv/Ht+2tvO5c//TLjyzPRcFwCoHJkSjPjC6yv+R96vfUtrjfZ6/YO1lvNO4PDJu6neXj535yPxdgfrW/X9h7P1UBJX9D+vBVAQLp4AAWKVsAxKbAkw0AZ9hUSg3g0oqzv6+v6x/UwNiuV814NmB35N85b8f51QHMzwR4bqs323k+fg48N/Ri6f8IclgcP0OcEte9aGeanVscWX7yI8QZd8gzEeL06Gd8rh453Ejqjv1FtH6gef5Umh+1e1OZ9XrvpjDPep8WWtvDZUuS1YT8DZAfcd3TY2cv1LydBVZJ9YghTI29yIS664bqRFstLM5aUwo8aHgQR96nqMMR59ndv5Z49iyO6EdpdWFxxHVPGvA3Nm7u6pWT5If0ag4qQ7TvieeR5Xk81pfr87JXnpF65BDpYnpVsDvqeglXEJ2dubQWM/hiAMIo28EzEPRhU2gJWDxoZ0kqWKRRp5XPCqd/OngGyDPt4EkLMFqof2QcXCTUG+oVkSdFnrG+StOcMBxpuexzPejCqV5R9rnEZa+vj+38iDhNoxfjGu4ycTwN7Dxcry6cU+BUXeNBSlH19Lu9/1dk9pdAyeefKRBlzSBoZ6ufcS+L7mH26Y/19bs/29WbQ9oWGf14rP+bnknjIR7U0jPhjxkcdBGGQtyUKY+TSHbHj0eJ8SkbjxifcpyFwMPGMxB0S4bxzvWzUl+3SmE8qFdxvVTTzmLipi3gWROmh9Oo6udBn6yHRPkxBBA9w0n2+v/MWtM/lhG6CDc7eXbAP1zrfjDUP7KeNzzjLnuBhPrwByw2bMUplXqifq8XZ09dEb5+PVXOPxTtO8jPVXiuujtuWun0D4nnCuhV0b5nGAzkvzwrZPueoWbOG71gor+aIs71+byQ468scZ93jKe0t8dzIT+W/EVtb2eNsMGyA7fYTSPs9+S5SyL8/Ov/bOdRDQxiTQq9iN6DRRp18zyASdk5Lrt4kCbTqp9njH+r6OK5MZ3qaybtixXIim2Cd7d6X/TnF4ij5Tn/RNgXJGMTwHnrWIgvCGeyB/vitiTPd+8Cz3at5XkiyTNeN0AXTm+I8gPXlWEBOFI+oXC3u1c7cZDqieAoZP52+qqEQ0HOKo5HePYENeFleK76j37UzpN+BYs0fFajPBftTAkkeUcnNewLQebJe5qcjCGDLAZg6B/udSt6wNncHHfyqAvggCq8Me7GAXfF4OxKPOBlTMoSdpqUp/3Oa/r6+z8FORzIca6ycdNQyNsw+z69JskqKq+Vu4XG2RDl2QnNVJZDJzQd8ux4qomIg0kNcOfrJ5J+xvFg3DTdk3A+hL81fKwR/q0W7dcfKOPPzySerIGiBcYFucSjgRTY06me7EKKB9WbpbGnqZiQIvlR3XK4etQXfxH14QBNjKPZRVc740GilQtkhrvjU6LhBfyxRNQYjIRI+XclWqi0E40yNtI+VWQv8HYo2XeyX3i/3ukjTTp5kn/6seO5Iuhw0jk7i1pfV6T8M5Z3ULeoK1K+BWOcbYgHNY7AMwB8mh8RJ63tmNNHYp0RnAOcn0zyoxhOJtqUQeXGc1Ny6KWcKqdO39sM5wLBEqsTiZTNfdEURamNx0+K+Oys/iXPD4NEUerhEfLzAU8vR+DCRDS6AM7iA/yfBx04yde0Gc47cLKPiGcBU14KOO+TEH8Ncyj4q5mpX3ykr3Mpfjf13AQuUh4gY5tK0gkDtqkkO7jPmgCk/OpBfzI0+wic5hSn5rrE1FCustbXmcgD/uEO8ryQ+hMwi7Dx0wpCH2loOD2TcRmV4pcJ44tgqpYJcdL9zkQbxhciDu0vjC+ytLM+aHA668uTzTpaugCnguvVH9YgklJ+/glcNnPNk34p1fXIV4EYhJaujUj2sDXhsaTT2JwkAovKStVbB/F6XqlLlcDzxOMMa4Fnw+MIGURmT9P5o7KVh9YL6xc9ehVtZSblAYiIpzvhS/ar6Kw7kB3838Zh8yPi0PygPc0+k/yNx8zuSP0/XisnP5dsE/Jg/lmlz8btPCiH5LeImWP0eQyPJD80nm73mXhwIKL6GXgcMTG6ikYdHU2xXklJH8BJftOZ38DxZGVnfgNxWLgWETkZiFN0+0KI010XNjLfqZ8xPh2IOGhscN2zQtLP3tjI+8L3J8jy7I1NUkn9JGTfS7g/lnLvaN8pozWT/ATsTxjWACTl58nYjPam7V/b8ShjL2SerD9PklF/Qmd+I6H+BMQR/WfqT0C7LPqZWPe8cWOvi4f6E8ZTzVNLA3plV1+u7sH3nWl1tYrzI7azIVGef9TJQ/UCNGUiUYUImh2QCry61r50gUkx8uRrxcnxkD9PcYoLmonXGVCq75i4sjAf0gK75jzKHxKOUxun7Kqs0zwJP8QezMzz4PVG8FgKZ2LYhpO5UGMGgx85HhoP6QS37qiJDz1OWVie9IW1F7iJZp6ncTiemjP9i4bxgPxsv85Z1GwBaSO/ay/BUkUdF7len6DT55Fa8ltAVEa8c4Tq1AFBKj7n6YzLyziJjiKWNkDcAdKcpnIzh6HZYtBdWFIwX/f5jJVQNb0ehTrD5gv2WMlvYX7iesFakNfNvgQn9c1FyEOVcsfzPurVWP00/LGMEz+OeGb8sYzzHeNs8QGmpJ8Jx9uvILFk4i9asGHVzpOy8ay7PxCsejCedbf+h1x6jFNIiaRVt/5N8FwUv5vnetV/ygrb6dOn+rqN9VxPg2Y5HxLtC/1YzVJWDdedfRjJhqddb7ubxUF7A5yv6yXNedIeSPg+ojW96lzmSVmAPnzqFDRUSWf+wahfC/N+15yiz5tSr3xpeUih4BIOD+yjzJqCdyMmz8GoD9+vNPvzyj1WoX/s9Qb2h+8U8CPXTwv9dAnjyWgvTxXTq/g102P7rn6qntoPSR+6CaL+cMrTuhmJ9SribALO2lduPHAZsPE4nMGBfS4EZDr84JEdz05hl5EshdfPHy/sc91xD0a8TyyPKs6VyYefuvXaxetVx4NPtEH13NDXilrcMT9fdvuHgyUJD9JgKwVc4wpRFvxoVLcBB/vUOt9jsCKl/TAJarLJf9yxPKwfey3YXxkWCTZhflgdf9Q0THGYPiuwpayOn2ueI8YDk7IDrsbE90POGq5YyfkeQpaV9fdqFjZowlm7V4T7tOGKg3DQb/H1bq1+HrIHIxyUFV9/B1s68zymvgOL7+vvQz2bXLOS810q3v88bODvuQlKTrDePQ7GA8nz1O9l6rMy/Uj2uXBrsf2Ok0X1LxcX5LhP/UTP5w+MzvT94fmJvTD67q1d3u8HmRUCi2kbWyOBZsAzauMZfWw/PQSeYRuP90JQbyx1+oT9q7HeQBnI4JK6EwuE4wQoMTrTQnicNa+jUD+vP1zm8Tim3h26s+QpL/mHwJOW7TjkH6Iee8U9rYCjIfL79pfY3JVF40Gc2Sd2vVpxsP/wz0/XOU/reKauHyCeH1PfqXk/wCzEyUyfTM3r763r9eb3a14/bV13XK/K1WFx9WL5wVYkjVMwnCUZg33qx4Py7HmYSvbPhTwjty9Y/aJpvrA8ILR+X7DzF14t4ZbwkTHrqfP9ADk4P8wpyUtl9vvYBamgExLGgzmfyP8Z6qkI2mKg7XAn9HsHsT8P2ZqhCihrztJD7kjBZEQlhyzUq0qd316u8oT6Wa0dgVN4tMzD/KgRZiNuhzyHTeBALwqwrVFqHuyOV2MJ3UaZFvAPa/8IdBtVBgeBjTM53ujB0pb+pdgXVe8shylL/UirTl85wv4EuaHW0yW5X93RVi30Z1q6CcLZU+PTUvX0tKfGp920ve9VXRzX58fQv1F08eD87Hpp1Mv8OC5I51/VcN7T/f8Bd1cNYX/C1Lf7HdY8+WLqFyrA0c78oOF++MLiVA5Hgwy5n3AMXjPiHN2zjpoGGTEHiAQHcfx+1yCHXBBxbtEXZXqjaf6K8+Aabewijt0uOe5Bz4MxYA7x6crKisdpaiaJmGSJ5DA/07ud+wD47Rshz8moYfrQJmm+5TyHtdZATPemxKPlp3CfzerBgutns0NvBX5LnQU8KMMYVjK7U2lVynhwTtAA7l719ivAIf2MGcgIh8UX2I+NynU6YXZwsWS/ULmKOCb/DBF3dVXAMfEglM+5PQ1wLNw59CNJOEQQM8XjiQPUd7Sdr0UclJn0TK/W8TNb34nGk/zh2zg/Wq69PLfOT15qxTpn+bGW+YkOjMbjQX8jaiBqn59unDaKxmMNRogzny/mjz3PGtyugj1dcT0PM9yDTJ5BBnF/jdzsNxEPNrXk32ie/NEzAQebWjb+pNLffLYh4FzB8ynQd3rk/LolHIyXIbfM5DniwaYW7Pdj8tOEhplwoJ7L/LEI5x7kxzYJx9Yvfv41kPd/UMdiXl3uz6R63KTu6K8jwrpwJfXpmYfb0jzHTx8VXUyAIPYtMxL7lhndFOsFD/pym3p+ft13HEDPzzf9LylIPrlAn8M9f/5UarJjTQCvSn3LrAlgOhQOqDCc6VDq8/TNDbXYG+bfDzDdkOq5D/35d7FvGRf7e59XHfJD9QvqO51EXmJII4iXxf5MIhxIfdrp11GO9aRz6XAgb8n9mXjF4KDyPDAhPh+FsQ0m6LcK5x9ieO3zWo03qJ62IEz3+bEjVF5R5RTdE6/naRBR8ADbiMenwHP9YR08B0QXPO7G+mDot0AMUvC6DMhP5IeDO1+xGA1xML/hCcOCJsKJCHj+tYlwWni+5DgtReVRqDOTqmUh80UaJMiOWpz40Tmd/rDUlovW8ekWD2aCVJDlOYFh+9+wx/Lxaa15Wje2d6t13D1Y8sORkud/bP9+FR6Y90XubN862YhT+T/gi/fZZea3ZDwP6dfLqyLA4XUZzENi9nbgXlUCOAEP/rjG8dihzSIemqCpUuyVJ4DD7EKAY/ua2scDOMmBfVXJ0nhQfgBH7bt5DnnoPCy1LScsj8THQ3XqqAIbjcfIzzTgicZjmqLKECfiuaSWqfnw9e8vXh+3fONbNWO/xROrn0Y8eE4cU+QZBJetOBjWYNbFd97EOHiLiVuvpyL/JzkBIR5+UXXgkB67BufjYhzG4+opHTiuniLjKFcHEXHY+WXfv9qB86ntg30dadw2HvHcFnsuiYefy5b6e5MTMDkb3e3C3+B6PSy7eDDlNujusV8FhPS4k+d3owv1G4c8K34mcFKwAz89CuYnjpsGJ7uKFYdLuMTx17CsFavC70Y4eH55A/ttnJGvYTNxe1ooc37HG/k9KE6yeBmv2N/i+zdOVRh3429Nv01tPrwT4pj8M/Xt2AdbtOJsQkS4Yh/sPeBZGg/g+FfUxDiYEcW4idmvaDyYMtmAfpuExbkBTopNz+tzeGwnz7MiGA81PUdyGOGQb0j1C/eRjk8XgYZ2hpnt9xjH21P395bGQ/R2F47xdRcBThXMj23ym3XhoJq5/qIueJ9MOJ7k15DYivtkorxNCfOzPQeeQxcvV5H8gP55NdKrEQ76G6h/xPmx+rATR6UtOFVyAZyDRXGfjQf1M9TffXQx+/Qnza13GA/g5G9PFaNWu5OHfSmx/cK0+qXQN47t4AJvPwhSvbE9NaWMoOc9xnmjJLguHENBqBzjFDH3Mk7rIYel+YFr1P8T4ZD+ieKLOZJLQBOOqaestA3O1weRBgIPfkzOi9SXS+MxZnDY3s1I9TijUdeftPP4Pli11LcV8phK7riVxzRB9lcllFor+nkucBwI82Nhu8Qg/hXFeixkKVhbiiFa92Hp/jR/d1lA77r8Rl5KhvzITTPWC4oWFt9PG9ULDKGSLmw0HtULDIVlkrheQB8e1pxxGNULkEafwweuH3uIMu95UB9un8IHT+ymp/jU5+tQr5JdPrFVOR14NsdLPi3a93rCcCpf9yQ9Rna5sLFevtCzk3N9qNz5ZTs/+SnUHWK9Gvb35vUw1PMun+kpqjuw8Xg6rCN7AV4PnR90pnVWhfYL4/ecJkyoO6RYhEM5XHX96rE9/dap6pGrx8X21Mt2fmpvY/v+2q692/szO/tLfoKj+q1SGA/jGRX9OM4/FPwNxetxXTj2bsn/cVRdBMfxyOPZc36vjNOw/IY0PzPJbyHC97PNJf+HKPYP5w+DugOWrNbDeGfJLsPthqrC0Yc8x/AStOi9WLEdxHwCxoO7Eo6N48Z6njeFugPpBHqf3lWh7sDi07ju0BafTiUc5o+J9QvCwb4dcTz0XOu/CHBuIb3t5wfP2kMeYCq9O0LVsMkz6kuRLOY5Dg2CMD+emGi/axioXwg8qNyxj1Z+vwSdH4RLT/0CceT6BSrlifSlIdCo7X6Go6OzXpzkt5+147CGiezRPY9TMh5e7n7/ssXxfVYrYJp9PpPqDuhL5Pftx1dgV7LewoHLP8/+3iqOdTSpRYiDev7MvR97hKbQ8/j6xZ6Tn7wAk1qGOEhefkZFkO9l9RRfvyCc2vG8wK5efL+Eq1/kULj1E5T5fuOgPyrYp6i8MD8W9GvNzuMTP6MfVfz9LXoozVHcN7gT7dNKs7FzE3jFPNLP3n3H4hSaZ/n8xZjLc1MlTeUrS6yfreF16rBeQDilnqMHDxxP0hRxv1/0fgmt59twxgFPFeCw90fFOFHvXKSfgWepv47yY0lo351A05kd7Dcer7mccDVolvr9sN94z41AD2Y536LOCrDLrh+gGiyW+iGRfD2uif0xx8PXXf9Ztl4ep9pw8nO0tWxPV8DV8PXl2ensfIlnGL6PSPuZp9wfo3j5ac3lOQeB5+d3sP5+MoW63mXOE/ireiiTI41z5hyXEWwu3yfD/FX/3pUhbNLY70V/NS8CnlkbziWXsFsDHrbfC/b+H+s/Z6A0OE+L36sa3GEhTnw+Dg4G+JOdFOTkUYsbvDaPhS6YT6AYrVjhfGy18RFRr4rnEM37RZHqbrOBdPt2P8/Of/sFf/QqRqksTPlV088vvBiU3g+5jhYtnB8Pk/wGkvK4302r+DIMHuAweuP0tJUnqW4Tz1RfpT49dFvo/IWS+vSmuKdIH0rl7jFueHrfqdS+MUUN1v3enlrRe5trmUWjgEB0vrcn259D0vDNtAPH9oeDXycD+XM3Ms4tWC/pzZkBjS/AcyGYTiDcaje632uE8cXyPAdii+/ZJrvzjneLhg1vz8Q64/ovCqX4sSfeVmnii8F9pYI0TMbcH6N/MgDLPrzOedh5EKqfYl3vsh1oqm0lO7BH8QX6P6n7pzAGFXQWOB5Tzx0rXs/dQv+n9jzufByrL8OxraV+ANwWvi58CibDPSU9F54f9P80h47b+Mm2hGwT5sMPXC+6hniDDdrnE7IDd44Mz215B5HNlZNG6A9Xh/7BWD+Ae4vUMHpF82uVWiIqlTIHMUAgwiJ+fEIu3Bd6tWAofoIwp4rgqWkNSCoqxMwcz+gf4PKTyk6ngjZuHEruz5XB/92G/Z6V2wHPyE50gvkW0hs12YvhEQVcboNnNcUXpb6a/mcdQuTn9umQp3DxqfohfaYdDRxKZifa2HeuV/Vq4US7jn7+HmkjPuBD4VBCnAl2RBr3R+PQUKynwHGM26IdDRqKTR9ZHPjMVHm1FNJQLI7xf9DumEWEnyOP81woXgY/06ZL4ee4YM6Twnou2kFSjYRBC2bHg/sodUcHDQ9qHHuCMDxl63hwot27xj744G7ULwpzg2se9pwFcSWeo8RLEBtzRyOhfzdB8wwMT8JwZucOB092WRwbF+CXC8KxPBaHnTfP/hnzfhnx1IwHcfD9Y6YvJSG5OGHjQR70o4w+pA4ZmFnbC0L1gp0XtdM/+DI/HL89eZj597MZfUg4MNEOh70vhfQhjYfjJH97sxWHxlOY5yoczj6c2uPPZcfD5ufgWvuz0/zguqcHfp4TPofs/aJWC7l5tutlfDnK1D0gnCpaL6LgnKYgP4GfgGKBPE4OYZGCPgcaSsHHA6EO7lPbJwM8KM92X1j9M/V70O+LaL+Pw30a7K8W/QMNaOE+pfw8zrQxg4iDPC36x+DAz7GI5nEu2/FYHHdQONQ/pJ8tjgvjazM/WBfG+qldu5FdbsuTYDJ9hJUjr1dpGM4Q+v4fO56hWWDfioYDo/cxWhyzwG6hCyrpob/x1Yvg9xaPvyrVroUdx5bFYVbZrntiDLKzg21HdoxBdvaUWp7DuufM7A873wP822HPJL3Kwdt3inRCHjpCxo4GY0EY3x/luj/pUCfzN7AIR/rw1Axy0ER+CzYZ0/s3vjV9cegccv8HW0dJbzw2/XVoH5gfRSkK0hsDn+9dNDwD+giUDuKk7p/iWcqrDzyOyyeEdbT0zL3vwvuHA+RxjzWo4cdX8dbhwHFq5q9uodPD9bMdkDde75kBAI9//89WUNajwZv3CDkcGNCu45kqRt7Rf9ywbqhCtdKlW+2fv6SX9JJe0kt6SRen/wLOx+Hp:4CF9
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
        ElseIf w = 3 Then
            str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO608,32^GFA,08192,08192,00032,:Z64:
eJzt0bFq3EAQgOFZtphG7L7AWiryAgduzuSwXkmpfIGD2+BCTdC9gIlfZQ4V11zIK4xw4XbdKSC8WV0k3+UBAjbM3+zCB7sDAyBJ0ocuO8ZFZhftr3a1OlK7sO11oGudkbi4uLi4uLi4uLi4uLi4uLi4uLi4uPh79vmSOp6vH8Ux2wfrb2HAQfufkTeagW8xfp/cZu1Q8vZbnDxirzjWb14AJo9ttAN6Fbt06tE9zG7zdegOdQPWg3o6NE53T4Szl2BMVXUH3UDhQbf3jcNuT3ae8w4cwBd61rtQesAanDGsKJ/9efQXYr3rPyfH5I41ubNfBeyI8XH45MHiZvTCryZWByh63BPb3GkPBl+dWXIBy8l1DcWA6f0czMmjMxUXNHv6MM/hK/Hq5A7rB8OsuJo9kjNwQ7yeXD+Y2MO/XiUP7uy/6dKz5N2e/7rWP9ylA4LJl+uuJTPOv9R144A75rPboQxpP7vh5Gk/wJEuHPuCt3Ws+9HVK6bptv7CVbBUYLzn0WGjAnAJby5J/6s/k7veOQ==:20D1
^FO0,448^GFA,04736,04736,00148,:Z64:
eJzt07ENADAIwDD+v7hbeaAD6gJC9gVZEgEAAMAGd57THfCgqWZkU/dj/EtuLuEY:662E
^FO64,128^GFA,13312,13312,00052,:Z64:
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:B177
^FO64,32^GFA,04096,04096,00032,:Z64:
eJztlr9r21AQx09ShUMDsTOIrKZTaaAZO3iQA+2uQaJr/wQPgTdWkC1/hWiW0qF0DNLSP6FDs5tOxoPJKKyi17v3S1L8ZAXapeAzRrKPj766u3f3HsDBDvZfmyus338qbNLnfr6Rlvb4g0TavMc/K6Td9fgVnsR2t7vRZvf7mk/s/uOqUB97AIHh7QGcbNY3Un/Zw0fOPn5WZCATkFn9yCk+svq/YuKl/qrzv/vF8GDjHZXPqkhdmYCy4/dqzYNV32HyioV/adNv8dYCav2iSNUKyPHXdD/vNM9o6W+Mvoaewmv9KlX1z2280+XZv9cv0sLET5A7FXnA2+kOf8om4NNzsCjMj4z+6rXUD3kJYZ3CmC+9elwbfq54XIxiQdCijAPqCKx9WlTZDBOwFbzH+RI433o156mOX/Nv2MJhbAGMXeGVzbX+stFHvkS+xis+6JG+fz/HfoxpQTnJffKd4s/T8zx7ZeIf1xfI3nKPZ5KPwInEFJD5P1t4VyNUF7ekj7Ireg2V/3GJkf8Gfl1DuMvHEGDkmMJE3JJ+Cd4dtYGqf0iZKyX/QPE/1v/gsNECea2/Tm9W6ZHRV7x4lYfH8ZMo3lDljD7VP5uZ+EPMneLDXR5FMe+oD0a/nf8uT5ch/W79ia+qbUtf1v+ypY9F39Ff48fED7CjT6zgf0KAvNb/YdZ/BnCet/mP1+34EZ1AbERHV8A6+iscg5vPWvRI1b/RF7uA7Btqn079sf9zV/c/z8a1e+fx66b+yEe+iT8asQRX3/uOvki/mH9i/ddi/bfqD2b+ic0gpoha9c89M/946RJPfdTEH/uGZxg59t8Z9aHRX52Y+RuWcIFtM+ZZK//zwPCYeUoAmDlE9c8KMQC3IIaP+wLEF8z8m+/bgFH/l23+a0P+ch+P8d8289/KvxvQ3wzo7z1ANPPffgBpePv++3R9Oz8rPqn5/83qHz5/aFta/f4Af6yPP0U6wFvdg+evofObOvz0n/+Cva8/fP6EZ2/J+vCDHewv7A8sdLPk:EC28
^FO0,1664^GFA,04736,04736,00148,:Z64:
eJzt06ERADAIADH2n7iuyBpEHdyRTPDmIwAAAJ47z+kOKGj6M7Kp+zHYKAHo7OEY:EE4D
^FO96,1664^GFA,04992,04992,00052,:Z64:
eJzt1DGO2zAQBVAKKljyCLxIAF5lj5EiwGihQqWPkKNEwRYpcwUZe4Bw4YYL0PyZoSxRdrQKUqXhNLYsPVr6nJFStWrVqlWrVq3/U+g+PkcY937W8Edm96RFODK7Jx3ikdk9SUj/bAAcmd0F0R+Zfs80OAqbnndNUmiAzngJqUFnJ5IDZTDNhjrlJmV9G62f/7dN6kmMS2I0JjEuignZNGz5K8WNiXJ/6DgJNnyhGIllMRqhRWyQ2ug8DRvzzIyNzUYO2MScAX9qBkAbydNpY3pgZMMbPJtRmUvM+2PADJov+cNEN9IQ3KuYPlEx9pJMgI7UR3j6uppEfbBi2pdseIHFBHtO/Oyn4F4ezSDPMQSlowXkgE3Ixo2w02fjP7VXNt9u5kkMkA0VI2PAs5CN4h27M1+K0diYUUxXDNjk/pM+4ERuxm6N3Bv3wN+Mu2xMfDS/PF2z4W7j3Rpk0SFQzm02b6mYnMFqZBZ04Kxn05esbyZnjRMvh5+e3meDxLfzGp2sT0PZ08Wse1oMzQb4wc9BS+90qzHSPrl3cPL0lo3LUZ35yR8MVnPr0WKklTk0WUoHd41lFubcyizAeDpnI2MmQSeddLDv8X7mOg52avgr4sbIOLMxQUcdDId0N9udKrOd2Hxf3g9WtdPH7579qqZWrVq1ai31G5x4XmA=:F43A
^FT907,278^BQN,2,3
^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}\0D\{sn(11)}\0D\{sn(12)}\0D\{sn(13)}\0D\{sn(14)}\0D\{sn(15)}\0D\{sn(16)}\0D\{sn(17)}\0D\{sn(18)}\0D\{sn(19)}\0D\{sn(20)}^FS
^FT47,451^A0N,58,57^FH\^FDCarton No.:^FS
^FT329,451^A0N,58,57^FH\^FD{Integer.Parse(sn(0).Split(";")(0)).ToString("00000")}^FS
^FT497,452^A0N,56,57^FH\^FD{sn(0).Split(";")(1)}{If(LOTInfo(17) = 0, "", LOTInfo(17))}^FS
^FT782,341^A0N,58,57^FH\^FDQTY:^FS
^FT759,396^A0N,58,57^FH\^FDN.W.:^FS
^FT759,451^A0N,58,57^FH\^FDG.W.:^FS
^FT913,396^A0N,58,57^FH\^FD{WNetto} kg^FS
^FT913,451^A0N,58,57^FH\^FD{WBrutto} kg^FS
^FT913,341^A0N,58,57^FH\^FD20 PCS^FS
^BY2,3,72^FT106,563^BCN,,N,N
^FD>:{Mid(sn(1), 1, 12)}>5{Mid(sn(1), 13)}^FS
^FT135,596^A0N,33,33^FH\^FDSN: {sn(1)}^FS
^BY2,3,72^FT653,563^BCN,,N,N
^FD>:{Mid(sn(2), 1, 12)}>5{Mid(sn(2), 13)}^FS
^FT682,596^A0N,33,33^FH\^FDSN: {sn(2)}^FS
^BY2,3,72^FT106,683^BCN,,N,N
^FD>:{Mid(sn(3), 1, 12)}>5{Mid(sn(3), 13)}^FS
^FT135,716^A0N,33,33^FH\^FDSN: {sn(3)}^FS
^BY2,3,72^FT653,683^BCN,,N,N
^FD>:{Mid(sn(4), 1, 12)}>5{Mid(sn(4), 13)}^FS
^FT682,716^A0N,33,33^FH\^FDSN: {sn(4)}^FS
^BY2,3,72^FT106,803^BCN,,N,N
^FD>:{Mid(sn(5), 1, 12)}>5{Mid(sn(5), 13)}^FS
^FT135,836^A0N,33,33^FH\^FDSN: {sn(5)}^FS
^BY2,3,72^FT653,803^BCN,,N,N
^FD>:{Mid(sn(6), 1, 12)}>5{Mid(sn(6), 13)}^FS
^FT682,836^A0N,33,33^FH\^FDSN: {sn(6)}^FS
^BY2,3,72^FT106,924^BCN,,N,N
^FD>:{Mid(sn(7), 1, 12)}>5{Mid(sn(7), 13)}^FS
^FT135,957^A0N,33,33^FH\^FDSN: {sn(7)}^FS
^BY2,3,72^FT653,924^BCN,,N,N
^FD>:{Mid(sn(8), 1, 12)}>5{Mid(sn(8), 13)}^FS
^FT682,957^A0N,33,33^FH\^FDSN: {sn(8)}^FS
^BY2,3,72^FT106,1044^BCN,,N,N
^FD>:{Mid(sn(9), 1, 12)}>5{Mid(sn(9), 13)}^FS
^FT135,1077^A0N,33,33^FH\^FDSN: {sn(9)}^FS
^BY2,3,72^FT653,1044^BCN,,N,N
^FD>:{Mid(sn(10), 1, 12)}>5{Mid(sn(10), 13)}^FS
^FT682,1077^A0N,33,33^FH\^FDSN: {sn(10)}^FS
^BY2,3,72^FT106,1165^BCN,,N,N
^FD>:{Mid(sn(11), 1, 12)}>5{Mid(sn(11), 13)}^FS
^FT135,1198^A0N,33,33^FH\^FDSN: {sn(11)}^FS
^BY2,3,72^FT653,1165^BCN,,N,N
^FD>:{Mid(sn(12), 1, 12)}>5{Mid(sn(12), 13)}^FS
^FT682,1198^A0N,33,33^FH\^FDSN: {sn(12)}^FS
^BY2,3,72^FT106,1285^BCN,,N,N
^FD>:{Mid(sn(13), 1, 12)}>5{Mid(sn(13), 13)}^FS
^FT135,1318^A0N,33,33^FH\^FDSN: {sn(13)}^FS
^BY2,3,72^FT653,1285^BCN,,N,N
^FD>:{Mid(sn(14), 1, 12)}>5{Mid(sn(14), 13)}^FS
^FT682,1318^A0N,33,33^FH\^FDSN: {sn(14)}^FS
^BY2,3,72^FT106,1405^BCN,,N,N
^FD>:{Mid(sn(15), 1, 12)}>5{Mid(sn(15), 13)}^FS
^FT135,1438^A0N,33,33^FH\^FDSN: {sn(15)}^FS
^BY2,3,72^FT653,1405^BCN,,N,N
^FD>:{Mid(sn(16), 1, 12)}>5{Mid(sn(16), 13)}^FS
^FT682,1438^A0N,33,33^FH\^FDSN: {sn(16)}^FS
^BY2,3,72^FT106,1526^BCN,,N,N
^FD>:{Mid(sn(17), 1, 12)}>5{Mid(sn(17), 13)}^FS
^FT135,1559^A0N,33,33^FH\^FDSN: {sn(17)}^FS
^BY2,3,72^FT653,1526^BCN,,N,N
^FD>:{Mid(sn(18), 1, 12)}>5{Mid(sn(18), 13)}^FS
^FT682,1559^A0N,33,33^FH\^FDSN: {sn(18)}^FS
^BY2,3,72^FT106,1646^BCN,,N,N
^FD>:{Mid(sn(19), 1, 12)}>5{Mid(sn(19), 13)}^FS
^FT135,1679^A0N,33,33^FH\^FDSN: {sn(19)}^FS
^BY2,3,72^FT653,1646^BCN,,N,N
^FD>:{Mid(sn(20), 1, 12)}>5{Mid(sn(20), 13)}^FS
^FT682,1679^A0N,33,33^FH\^FDSN: {sn(20)}^FS
^PQ1,0,1,Y^XZ


"
#Region "старый EAN код"
            '            str = $"
            '^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
            '^XA
            '^MMT
            '^PW1180
            '^LL1772
            '^LS0
            '^FO608,32^GFA,08192,08192,00032,:Z64:
            'eJzt0rFqwzAQBuAzGrQY6QXc6BUCXVQo9Su5U1UIRKGDx75AaV8jWxU8ZEnJK8h0yOpsHkTUs7CTQudCC/cv1vFZnNAJgEKh/Ovku8hyOb/dN3nYuWaeh3kI1/vckZOTk5OTk5OTk5OTk5OTk5OTk5OTk/9lnxaY3WX5X5znm066O+h5kPYj+gVo1d1BmFzmTSi75erEg/LoEYzCMk6ugKPHJkr8ZLGN0Kdy6qNAzoxpm1qAqSD73NpeQ7ut7eglCFHp5NUcWPO0Ss4mf4ACqht3YM9dpYHXwNblBsvJD+jmcfB+cA5cl63z3/yqK4/ob8EYkHzB9fLoPB8924LqVYv+WpQ9CH4S2tw7L8ftrAYV0n4r1OCx0HDj/Gx0bDibpf5WyDUUvF4nv508ukKk81sBGp29nwY3P/zl7Fj67uI5Ol4Y/lZBwZK3Gz/dLwcx3m9hOtCstlpXbeMuLs/z8egRdCrt2Xmv/DDfvnSgsxPO3y/rb57h+1CwYF5aPCG+D2kVPzuF8lv5AvmNtxU=:FD27
            '^FO64,160^GFA,11648,11648,00052,:Z64:
            'eJzt2j1u3DgUB3AKLNiF7RZBeI0UwfBKe4CBRMNFyj3CHmVluEjpI4SGC5emkcJchCvu/5HUt2TPBLtBiqGD2B7pNx/k4+MTZcYu7dIu7YdbE6M79dyP4ueYDx/ON6o738iQv9eMn2z6M88xffthI1sm/MZB1e6ag3bUJ9pwrwx+15WXbRM71vgKX8qoKzyj5q68pWQaj/4zuhVzU8UwN2JiYmAxtivDwWZGjkbTU8JYOTcixn1jY9UpuzaumRs1MW3DvXBzU7Natjh5z5jUJ9qpN4yemesto4ycm2ZimPpsK79lbt4y+JqbGG9i9PhWzPeVOeBLRb9pIoacP39bm1DjeLdj7LY5Hun4jnGaf3vYM9eTPmDVYPy2qQP6QN/OTN9vPGj+crsan8PB47jcNgwGb2EZBysj23f7JsfbQS+NVTczw1dxfVBuboTXV1PT8dX8yYb6IMb4pYOhAJj0wdykeXqQtvQ1mcBqPs651NdhalI+YMXwwVTj3E5jOjM572TTFeNxaMwhKXb81OT8dhBtiVEyDoeUHY0ZcxU1vZn5Xm8X82ubS7u0X6j9/u7nmE+fzjc6nG/UVgn5RhP2fPOfNO4ZX35GbV43WHdUZMMKavETiy6Vc1QVMXpM2MVzAmBtyaZLhtMaNDVyYVAjNyhfk6myETHMjVqYWqFatTMjaX18zXSy7WQxvBi7MHph3sOIuVGlpN01uL543xuxY5q1Yb2R2aA0m5u4NCi45Mpcv24iasFiVB5TlAfXVHfEmM39y9IEJntzWJkYYR4e16+jXzPtpjHNYzHHDWNr8fhl1Qfj+PQG0T4Yt2XkfeiNH03fbwKPPX1eGnHvi2nc2lQwUSzMx7VR5v2rpgowZUwbW0yrr6emW75OwOfRxbTZSNfMXicsTaduqG5EZejJ4HuUFACTPlgZTNLBmGzEOOdSX/ulQTJovruZ4ePcTmO6MpR0noth2VRjDkmx45YG9S4u9JKpi2G6Hc1Wrhpbn8nV3gkXc2mXdmknNCzkQ8MlO1Y1ZMmIK2/kwtVqvGliW4yMpxtbZaNQfZ1qXDEa1depxhfTpJXgFGP4YLCybYOVqUJFCYlWd3eqYXnjRJnAR1P2sXVLZQyRP27tsPs7GM0mRsRWtXgDqHUfsPLgg97d+bT7S5fhg6mZrwaDAklZHqjWfUzbec3T15B2f+eGNmB6o6IjQ7XuE+3qWSxaefe3N+LR4m1PjY5eOSzNtCMc79O/svubDQqI6JfmOhmqdTv5DBMan3d/sxFUbqetu1RwpT5tqpBNjTdINb/XLu/+JoM4wFtYmJoF5WWoUCmn1Zrj4/Vlb443lOiMCpLRBGbJAMN42gqHuZ4YK11cGZZNR4/DWNnmndxijEB3CLs0QQWM4tKgNoCh2NCmmNJvZI7qyOK2uYopgtam1ke+Y0xj6H/ZLs3hcBRdvW0odncMjc+mUZvGKl0fVbsyOQ6kFQbSTOO6w5iq41GZ7fEx0qlsJvOnriZmHQcGz4HxmZsGda4KMN1mvCGHUO6AoWp/EtfSH9UtxTUZxHU3iWsY11E9M8k7GnOOTJo/llMGXM0fmugs5zfXz1PhjjIZ1JRsOU87mj+anrztDeUDGBH/4i/RkHn6mnZ/B4NUkcPV9EZEQ4ZHxb9RzLPm7s5N806Dy2/6Ma8LrgyQ4vaIUbp2gYySX1J+6402uMzHj3n9Wdwv6zeAp3n0rXYxv7a5tEv7/xtS4C2SjcmFJ62EdOvJpx1OpBqJY1Qc5t+YNHMTRiO7/iwdstG03ZRy18x0vUm7kkxaejnK1WQaetCsTCymotNwmDJjFWM26XtbdkEn5iob3tGGB9OWSc/pfhkMrMHCwJpkkFYp4RqsBVe5oITpVG+8ziZogwWIxYXRxQgssTjcWBQ1PK2MtFubCl++NOomG9lVtGuCZ9dYImmrlXKFbLGgyqWRU2OKsTOj9ozqKmSgij6x572xokWBoL7uGF2T4S+W/UMjn4yw4p7uPP65Y+qaMp14tNXfyYQwmA9q3zz7wdDIDea3pUmbXXgg1PylY1ghiSZjesOWRl/RKo/ApHu6BnUSTqMakQrKPVNihzsyrUY305ttqn1TYtQsTapjdk2XjUD1h9NoqxvBhCCnoNgzdEHVtDQgKKts3gBN9wSE2zX0GNaeVMmJZzBcv6SLJ75vWjKWyvAHx3G10TXUfe4Vk3oZxxaGttXPNuZEcz+ahjG6E17i+jTTpa3ffv5smtwH9743VyjPaHxeNS1Ll1FpGPEAMlxFv9AfHdG275bRLV09sU72JsYbJKG6zzujQcw4MhQ7yTQpLBndbqGtXZiS3043pu7z6KZhyagUq4xu0cQWNXA9z9dzE7IZ1gUyqIHr+bowNy6bYf0hI+gvHkRef4pJrTr7b8Yu5kfNpZ3Z/gUTRza9:A43D
            '^FO64,64^GFA,04096,04096,00032,:Z64:
            'eJzt1jtuxCAQAFAjCkrapOIm4SqJUqRMyhSRsJSL+SjcIJQuEJPhY/xZPqtom6w826z8ZAx4ZswwnHHnIcGHrjGDGDUXyWsDqORzmUlisGWni7v29GoT5NnHjk9FF2BZa4ESDGm7Tm6Krro+kdIGkrQfUHEKi49Xuq35Y8fVzZy4tlO4b5f4krgLSIHN2bWKrtAlJjIHTUG4ovtExqSkIKHkxCeaAktBhYo4OvUO3mNFHB2LwY/haMr4ncvg4d7sdqBzqPK4fm45uv9NKjii8cMkFzNzzBH4hkGakhvqmB2azqOLkktDHJ8HVXV94bv5owN6fNRx/d7x3/9z2KwPe+LRfzoOPl9bjpnEl/0Pvu6vd8zkp8Hmm3bvz7vZ5gf2/KOPIvUPvGiHh5Q/2R1Z+guMWF4p/1afWfZN/q6u5d5j/q8+5v4W6sfF+lmdrB7ujXWW/YtlV7iyUONm458iu7T+gxLrP/uHzI47y2DXZ9CfVfbL8A5tf+n4a8ffOv7ecXUbLx9Aeq6u9vL3G9s1tH0JXXRxtU9F751/euen3vmrd37rnf/yBuiKLxMsTz8/oDb8GWec8ef4BWMpOuQ=:C4E8
            '^FT907,278^BQN,2,3
            '^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}\0D\{sn(11)}\0D\{sn(12)}\0D\{sn(13)}\0D\{sn(14)}\0D\{sn(15)}\0D\{sn(16)}\0D\{sn(17)}\0D\{sn(18)}\0D\{sn(19)}\0D\{sn(20)}^FS
            '^FO10,487^GB1151,0,5^FS
            '^FT47,475^A0N,58,57^FH\^FDCarton No.:^FS
            '^FT329,475^A0N,58,57^FH\^FD{Integer.Parse(sn(0).Split(";")(0)).ToString("00000")}^FS
            '^FT497,476^A0N,56,57^FH\^FD{sn(0).Split(";")(1)}{LOTInfo(17)}^FS
            '^FT782,365^A0N,58,57^FH\^FDQTY:^FS
            '^FT759,420^A0N,58,57^FH\^FDN.W.:^FS
            '^FT759,475^A0N,58,57^FH\^FDG.W.:^FS
            '^FT913,420^A0N,58,57^FH\^FD{WNetto} kg^FS
            '^FT913,475^A0N,58,57^FH\^FD{WBrutto} kg^FS
            '^FT913,365^A0N,58,57^FH\^FD20 PCS^FS
            '^BY2,3,72^FT106,586^BCN,,N,N
            '^FD>:{Mid(sn(1), 1, 12)}>5{Mid(sn(1), 13)}^FS
            '^FT135,619^A0N,33,33^FH\^FDSN: {sn(1)}^FS
            '^BY2,3,72^FT653,586^BCN,,N,N
            '^FD>:{Mid(sn(2), 1, 12)}>5{Mid(sn(2), 13)}^FS
            '^FT682,619^A0N,33,33^FH\^FDSN: {sn(2)}^FS
            '^BY2,3,72^FT106,707^BCN,,N,N
            '^FD>:{Mid(sn(3), 1, 12)}>5{Mid(sn(3), 13)}^FS
            '^FT135,740^A0N,33,33^FH\^FDSN: {sn(3)}^FS
            '^BY2,3,72^FT653,707^BCN,,N,N
            '^FD>:{Mid(sn(4), 1, 12)}>5{Mid(sn(4), 13)}^FS
            '^FT682,740^A0N,33,33^FH\^FDSN: {sn(4)}^FS
            '^BY2,3,72^FT106,827^BCN,,N,N
            '^FD>:{Mid(sn(5), 1, 12)}>5{Mid(sn(5), 13)}^FS
            '^FT135,860^A0N,33,33^FH\^FDSN: {sn(5)}^FS
            '^BY2,3,72^FT653,827^BCN,,N,N
            '^FD>:{Mid(sn(6), 1, 12)}>5{Mid(sn(6), 13)}^FS
            '^FT682,860^A0N,33,33^FH\^FDSN: {sn(6)}^FS
            '^BY2,3,72^FT106,948^BCN,,N,N
            '^FD>:{Mid(sn(7), 1, 12)}>5{Mid(sn(7), 13)}^FS
            '^FT135,981^A0N,33,33^FH\^FDSN: {sn(7)}^FS
            '^BY2,3,72^FT653,948^BCN,,N,N
            '^FD>:{Mid(sn(8), 1, 12)}>5{Mid(sn(8), 13)}^FS
            '^FT682,981^A0N,33,33^FH\^FDSN: {sn(8)}^FS
            '^BY2,3,72^FT106,1068^BCN,,N,N
            '^FD>:{Mid(sn(9), 1, 12)}>5{Mid(sn(9), 13)}^FS
            '^FT135,1101^A0N,33,33^FH\^FDSN: {sn(9)}^FS
            '^BY2,3,72^FT653,1068^BCN,,N,N
            '^FD>:{Mid(sn(10), 1, 12)}>5{Mid(sn(10), 13)}^FS
            '^FT682,1101^A0N,33,33^FH\^FDSN: {sn(10)}^FS
            '^BY2,3,72^FT106,1188^BCN,,N,N
            '^FD>:{Mid(sn(11), 1, 12)}>5{Mid(sn(11), 13)}^FS
            '^FT135,1221^A0N,33,33^FH\^FDSN: {sn(11)}^FS
            '^BY2,3,72^FT653,1188^BCN,,N,N
            '^FD>:{Mid(sn(12), 1, 12)}>5{Mid(sn(12), 13)}^FS
            '^FT682,1221^A0N,33,33^FH\^FDSN: {sn(12)}^FS
            '^BY2,3,72^FT106,1309^BCN,,N,N
            '^FD>:{Mid(sn(13), 1, 12)}>5{Mid(sn(13), 13)}^FS
            '^FT135,1342^A0N,33,33^FH\^FDSN: {sn(13)}^FS
            '^BY2,3,72^FT653,1309^BCN,,N,N
            '^FD>:{Mid(sn(14), 1, 12)}>5{Mid(sn(14), 13)}^FS
            '^FT682,1342^A0N,33,33^FH\^FDSN: {sn(14)}^FS
            '^BY2,3,72^FT106,1429^BCN,,N,N
            '^FD>:{Mid(sn(15), 1, 12)}>5{Mid(sn(15), 13)}^FS
            '^FT135,1462^A0N,33,33^FH\^FDSN: {sn(15)}^FS
            '^BY2,3,72^FT653,1429^BCN,,N,N
            '^FD>:{Mid(sn(16), 1, 12)}>5{Mid(sn(16), 13)}^FS
            '^FT682,1462^A0N,33,33^FH\^FDSN: {sn(16)}^FS
            '^BY2,3,72^FT106,1549^BCN,,N,N
            '^FD>:{Mid(sn(17), 1, 12)}>5{Mid(sn(17), 13)}^FS
            '^FT135,1582^A0N,33,33^FH\^FDSN: {sn(17)}^FS
            '^BY2,3,72^FT653,1549^BCN,,N,N
            '^FD>:{Mid(sn(18), 1, 12)}>5{Mid(sn(18), 13)}^FS
            '^FT682,1582^A0N,33,33^FH\^FDSN: {sn(18)}^FS
            '^BY2,3,72^FT106,1670^BCN,,N,N
            '^FD>:{Mid(sn(19), 1, 12)}>5{Mid(sn(19), 13)}^FS
            '^FT135,1703^A0N,33,33^FH\^FDSN: {sn(19)}^FS
            '^BY2,3,72^FT653,1670^BCN,,N,N
            '^FD>:{Mid(sn(20), 1, 12)}>5{Mid(sn(20), 13)}^FS
            '^FT682,1703^A0N,33,33^FH\^FDSN: {sn(20)}^FS
            '^PQ1,0,1,Y^XZ
            '"
#End Region

        ElseIf w = 4 Then
            str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO608,32^GFA,08192,08192,00032,:Z64:
eJzt0jFqwzAUBuAnNGgx0gUc6woZVSj1lbRVhUAcOngpyQVKe42ODh6ypPQKNhmyqnTxIKI+m9jJ0LXQwvuXJ/mzkJAeAIVC+ddJ9pEnan77USdhX9XzpJqHr9rvK3JycnJycnJycnJycnJycnJycnJycvK/7OMAs78Mk3/iItl6Vd1BJ4Iq3mOzYF77HLrRVVKH3C9XJxF0gx550H65PY2uQaDHOiosLLbxPB330aAy59q6lOAssMNukxnWHsrRc5DSmsHtHHj9uJaGVRe/hxTsTXXkG28NiBKn+RanxdmP+ME99N71Lnpv4NpnPv9Efw3OgRKLmV96Njnbge50i/6S5h1IcdKds3w3Oi9Bh2F9IXXvUQcwohwdN8yyYf9CqjdIRfmSgVFi8lilcjh/IcGg82es2Q/+PLk1t9eeoLe7En+zkHL+hO7EdD8C5Pl+U+fB8HKdGev5lavpfRr04Z3iIV5cdLrp37fLKzAMq25je7o4w/7QsOCNKvCE2B9qhW0CFMov5xuzvITE:1342
^FO64,256^GFA,14336,14336,00056,:Z64:
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:5DD9
^FO64,64^GFA,10752,10752,00056,:Z64:
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:FFB8
^FO96,1600^GFA,05760,05760,00060,:Z64:
eJzt1b1uHCEQB3AQxZa8QCQehReLwkYu/FpYLlzmEULkwmWQUoQoiMl/Bu6OVW59ciKlyU5j64bffsDMrFJHHHHEEf9H2PLnNtCNBQvRej2jdzOTTW/NTCt2XssS5Zu2Xs+4v7A+uxsbDdt2bFx2rnq22ezYsJqbVu0cY1B656q3LZ2sS/LHjHU+TjaoBZtis9peBRDXxQmGojxRtJRs4lsVqZs0rAMKFfsWbTLFxSV3q8RiM9k6ymwN762WqhAbKGIVqoGzsPwMug5rkGHrqbDFwYhtw+IJsGKVrIV112zo1nKZny3hv7xvlxJWsaaydTlEJDWx1WSaxQofwx2yDtafbTXFZh/96mLT3SbPVlGvDVMXrHCROOthw8YmJ/Zdtz7izJAMvSaXwo9jU1Ptmo1WrBp2tZOteG62OBXsc4Cl2WLbk1hzsvliC2yT66JCXrFebODtRbKJxc3UxVJxD2er2eIE2S5UeRwMq7tNs/3ys7jH79esm610PZfJxn6DfRk26nKxYWt7TW7sM+zTy6jnMlv6ccu+PBf36alb3cSi58R+3Nh2xT7C3ufRv5N9GHV12mf63X6F/Sw24EDNZOuwU22wRW3gFPEDuuQOLyj9G9almsw1aWd7qiu/ih11JTVJuFET6yM6RKxLYs2mnk9W6tmlpjc2+26z9MJDM5s+GnbuowZbxTqpONO7E5bs3L94pW6n/r1YHEgKRISpcE98RlQvcwP73O08N7gIi9hF5ozYhdTZnubVOKMxrzCVsuVDyfIjf3/ZajQ429DaPCdHbfCcxAq8YZosz1a2mL4LctHXD/N8rsNO8xkb61K3mHhica2iuIuDWK4VLd9XWWaKfDnQk9Ns53iP31//Ju3HYf+NPeKII4444ogj3hi/AAeOBqA=:E1D3
^FT907,300^BQN,2,4
^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}^FS
^FO10,677^GB1151,0,5^FS
^FT47,594^A0N,58,57^FH\^FDCarton No.:^FS
^FT329,594^A0N,58,57^FH\^FD{Integer.Parse(sn(0).Split(";")(0)).ToString("00000")}^FS
^FT497,595^A0N,56,57^FH\^FD{sn(0).Split(";")(1)}{LOTInfo(17)}^FS
^FT782,484^A0N,58,57^FH\^FDQTY:^FS
^FT759,539^A0N,58,57^FH\^FDN.W.:^FS
^FT759,594^A0N,58,57^FH\^FDG.W.:^FS
^FT913,539^A0N,58,57^FH\^FD{WNetto} kg^FS
^FT913,594^A0N,58,57^FH\^FD{WBrutto} kg^FS
^FT913,484^A0N,58,57^FH\^FD10 PCS^FS
^BY2,3,72^FT125,812^BCN,,N,N
^FD>:{Mid(sn(1), 1, 4)}>5{Mid(sn(1), 5, 4)}>6{Mid(sn(1), 9, 3)}>5{Mid(sn(1), 12)}^FS
^FT154,845^A0N,33,33^FH\^FDSN: {sn(1)}^FS
^BY2,3,72^FT672,812^BCN,,N,N
^FD>:{Mid(sn(2), 1, 4)}>5{Mid(sn(2), 5, 4)}>6{Mid(sn(2), 9, 3)}>5{Mid(sn(2), 12)}^FS
^FT701,845^A0N,33,33^FH\^FDSN: {sn(2)}^FS
^BY2,3,72^FT125,978^BCN,,N,N
^FD>:{Mid(sn(3), 1, 4)}>5{Mid(sn(3), 5, 4)}>6{Mid(sn(3), 9, 3)}>5{Mid(sn(3), 12)}^FS
^FT154,1011^A0N,33,33^FH\^FDSN: {sn(3)}^FS
^BY2,3,72^FT672,978^BCN,,N,N
^FD>:{Mid(sn(4), 1, 4)}>5{Mid(sn(4), 5, 4)}>6{Mid(sn(4), 9, 3)}>5{Mid(sn(4), 12)}^FS
^FT701,1011^A0N,33,33^FH\^FDSN: {sn(4)}^FS
^BY2,3,72^FT125,1144^BCN,,N,N
^FD>:{Mid(sn(5), 1, 4)}>5{Mid(sn(5), 5, 4)}>6{Mid(sn(5), 9, 3)}>5{Mid(sn(5), 12)}^FS
^FT154,1177^A0N,33,33^FH\^FDSN: {sn(5)}^FS
^BY2,3,72^FT672,1144^BCN,,N,N
^FD>:{Mid(sn(6), 1, 4)}>5{Mid(sn(6), 5, 4)}>6{Mid(sn(6), 9, 3)}>5{Mid(sn(6), 12)}^FS
^FT701,1177^A0N,33,33^FH\^FDSN: {sn(6)}^FS
^BY2,3,72^FT125,1310^BCN,,N,N
^FD>:{Mid(sn(7), 1, 4)}>5{Mid(sn(7), 5, 4)}>6{Mid(sn(7), 9, 3)}>5{Mid(sn(7), 12)}^FS
^FT154,1343^A0N,33,33^FH\^FDSN: {sn(7)}^FS
^BY2,3,72^FT672,1310^BCN,,N,N
^FD>:{Mid(sn(8), 1, 4)}>5{Mid(sn(8), 5, 4)}>6{Mid(sn(8), 9, 3)}>5{Mid(sn(8), 12)}^FS
^FT701,1343^A0N,33,33^FH\^FDSN: {sn(8)}^FS
^BY2,3,72^FT117,1477^BCN,,N,N
^FD>:{Mid(sn(9), 1, 4)}>5{Mid(sn(9), 5, 4)}>6{Mid(sn(9), 9, 3)}>5{Mid(sn(9), 12)}^FS
^FT146,1510^A0N,33,33^FH\^FDSN: {sn(9)}^FS
^BY2,3,72^FT665,1477^BCN,,N,N
^FD>:{Mid(sn(10), 1, 4)}>5{Mid(sn(10), 5, 4)}>6{Mid(sn(10), 9, 3)}>5{Mid(sn(10), 12)}^FS
^FT694,1510^A0N,33,33^FH\^FDSN: {sn(10)}^FS
^FO10,1575^GB1151,0,5^FS
^PQ1,0,1,Y^XZ
"
        ElseIf w = 5 Then
            str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO608,32^GFA,08192,08192,00032,:Z64:
eJzt0jFq5DAUBuBnVKgx0gUc6wpTaiCsr+StosDAaEnhJiQXCNlrbKnBxTQTcgWJFNMqnQsx2mdjzwxsvZDA+xvr6bN4QhIAhUL51ikPmZVydfvel+ng+lXpVqln76UjJycnJycnJycnJycnJycnJycnJycn/8q+DDCHy/C7OC93UbofMPAk7Vv2myKq2EBaXJZ9auL214kn5dEzSyrmXV5cAUfPfZb4KXLIc7n0USBrY0LfCTAtFB/751oX4djZ2RsQotWTtytg/cOT0IU7ssXvoIJ2jRPPsdXAOyyb3ZUfccLcjz6MzkcPwV/5TWw+0X8nY0DyzU3cxuz47MUe1KAC+mvVDCD4SQ2m3Vo5L2cdqDStt0KNnlUCnV09Ozas66m/FfIPVLx7rWEd/O3i2VVi2r8VoNHZi4C18+Yffzk7lj5evMSJsO/wtxYqxh5HD345Xw5iPt/KRNCse6r1z/DhLi7P9+PR81zas/NB+fF+h8aBLk5T2V15ge9DwYZ5aXGH+D6kbfjZKZT/lb/4UUIx:7C0C
^FO64,256^GFA,14336,14336,00056,:Z64:
eJzt2r2O3DYQB3AJKtSFLxCAL2JEr+TSRXDU4gqXeSUttnCZRwiDK1xaBxdmsIIm/xlSXyuR2tsAhgOLCGKsTr87LT+GQ1JZdpSjHOUoR0HRRP3uTfaX7+ve//qY+/DuMVe5x5xu15eabN+VzWNu6xH+q1PddLHoInffOCLKKqqrWjd92Va1Ilzq5SN+idNNuaqY0RlqRqepWToVd0R2dIbs0q0bYnD5o65T7ehItUtXbbs+M4UrR3fqS4drM2eiLns/c11xv8tmzuU3jhJu+n7PLu/udxR354TTcETeEYnjj+Iu3+LObDqilt3nqEO7b7se7lPcFfR52xHcn5/ijsHgOJyM7hnuY9SVs/4yOKlPA/dXwrURV12cpsj4u3GnLrej03B9yql5/+zudm7m+lm/Ztdtu5xdV83G32wcVXGnpf18/0TFE8Z7u6hPF3Hdyg314tuvjbucrm50FTWTK+LOoV/T6+QU1aPj/mkjzsKh9kdX9Nno2u24e5SjHOUoRznKT1zo9kJYafD1yPop74q2IItkRLUhVUW6W/XILLuCmvIFk/a2Q65ELicsDycnK5GupLb8jORi26lWU7d0QCc4Ta78sk6ug+P7+6VDOnMOjuKuNmc8Vl3NXKf/hqsueFS1XhV7V2WmDs6n/vjKSPPwLQ2WPF1kFZl3v6G6zWnpkCKxy7BUyuMOSc6JsAoanfLuKXtKuCd256WzhV+0IKGNNHze/Y47sJaxb3WduKu9jg6LT3FI1uKO3MKhve50rXevg+vf9Pf+ef06ZLx3uu7WnQbX8zI0Wi+9d9evoV7MaarPlDM5t7u7vgRXnefOxV22dPo89BeTfUj0Fyw08GyOZm7sn1R2CcfjwZnLzIXxUKfGA9b157nD98P4sxxlzn1i/GnL431yqE9uQa5lDjHR8a4sxxdXPY/tx64Rl4oTSuLZzElPqfF/hXj2LRrPSomfk+slfnI0xPXyNR4/eX2UO12M4wF/ybHLqC7bp022Vx5dGh3u53RHOcoPXKhJ/LDiw4uqkf38im80+IyBUNUFbU8qk0N6gLDO27ziOOBmvMOfOmVgVxBPI3UufwDOitO0yrxvXMmPSg1zca04/KROOyVnCbaUB4Nz4sy+a/ksoVXyYJNrTJN4UNyjreFzAOZwWekd+fpNuUbOAZizKzpxSBLsvuvkrEFcPjj1XV2q9UbH5wA8U89ds+eUNcXarQ86Vq6luQv1adIPintK3lmdnHKhn9kdhw49c0N/qSh5VMaTDu/Izlx7x3hgh4GwcDaMv1S/Xn8/pHVhvCdbYlWfeDxxfEyXdkheQ7ufqTWoDu9UskIX/YVdEeISqmvHAVwmlw/PuXUGm3BZiGfjBsC9zvj6fJfOJ7w7+3F0YldJ+526N7ia+6f2rn+rw0LH8TJ83zW8yBjjkkJv5WXObr1g8bGMg5hkEFHr3XjWUj3F3QzLIioQwa3Zccs4j/Ge8+Rmdvv1cl7Jcj7V9Wdyabecx7zjK3vjL1vMm7yzVMmVvfG+nKfxH2KEzYp0fNkshdSsfbM7ylGOcpT/SSHkYJjrZLOTP1hcy52EUiPbggihPtsdwmp169zgMOfpDilew/tSBS4ZjvpDGF+5fnA8xyIDwU0GCQX/Pp6dhmnD3DqaOTnu538axZv9HLOHaYqiTjl8wsTA/zSazx94OgrTYr52dXB6BnjTsBQXpuEiTE1I/MRhHq/DKq/CrI4E4SL7kZIncD2Gab9cuaqZHDIKedFDyXsbLW7KQ5qh484gq8aCgE8BytbURlk0YB7SGrNyenCcjavaZI73aZGkKVtOjpIO1fOEDsCvw9w6E3P5dXB+6maHjCGkz33UFV8dd8h+23UrN9RL8eLUS5v13BHlFyxcu3KmIckbSrgvfdZzH/AODTe6euWkv6BjqItT0unwjMjn0HBzl9240M/QMdQzu5oHlk9n+O2dlAv9WovzD41BW/pN6LiT8ccDs2Bn5RVNPLdyRdrJwQae7Qmuqyxd/KDRfNCScnzEoIND/yR85FduKsSda8rxW0AILVknjjcckO/iK+LWb3HHLddz0Moxirile1mnylfUn/ZcznGM6I/gpPl8d3uTC9lohn5+l/toeUPe+ObzbhhHdzmSlVjeqj1XBOcHOo8lkPJuh56GhYtcxM8RkKa4tO0U31K4EAAL/zohR0UzxMG5w184s+N+Jo5nJ02j45fKxm2nbdcOrgmnOEYO5mozLn+2nRUX5pXJcVuGeWXbNd75eQzP1cq49K9qzuexW1d7p3kM2cEZXjwO8+am42jIzs/Tg1NW1nR+ng7OF9VkD5XD/RjuKEf5Ecu/97pyhA==:1EBD
^FO64,64^GFA,10752,10752,00056,:Z64:
eJzt2k2SnCAUAGC6WLj0CF4kFa6Sm+DNwlFIZTFbly6MhMcD5MfWJ+maySRS1dM9rV+p/D6gGbvTne70ockk6QIbUje1Xc6sZMYzZxTV9bkj36jI3UJ1OSM/4KNwZqQ5XjpFc13pNM31pSNm6FC6meZE6YgFIUtHLIiSEZtEVXxX3JwVxkhxHM9MnaK4DnNCtLgld5rieizpJjfljlTRBrzAKxypggrMQJrrtsr7Qve8QRRubHDyde55A3yZ+9rkbKP/ftUZdKbJPf4C1+/Ut9Lx293uP3Nf/InfvOM0F7iAUQScXA7d4D9IjBMerlu1jvvxf3MxckodHFEMh0nnOj+OJ67fcTCMa/+vAjf48fHEwduMt2u9dcKPVyfOnubyHr6fwEk/fpw4iW8YHYELx0+c+8JHR4t1v/D/M4cd5IhB8Rpi43PHvcNDwY2nDk9UvmjHi04XTh04e+rsqskbOIHnU1yPbrVFMVk3P9BNPDq4XTGxGPsmbrCZL6wDi04xrHhnbrYvcBpeHZSBJDi4FnyA6whwK9Vp70Z7Xd1hiHvupGsEkCepmwhOeQdfTM4NZLdcdpAf3HUqV92Ibn1/x3x/Hd1B/cQ22+Ig/YFbLjn/wEt3u0/ghqfuuNxl5ZgfzA/rWQwOLjrR6Ey7Y25Av+wUHJNlvxRc1U8EhwEVT51Et98PBuduyd7pRbe4mzQzL/p5dONTp8NaU+Zw0SEdH6r6EipaMY4xNjKMDXbdEmdzmbN/uNrGv9rpql67c6AA1Dbe1m5bYwoOw0H7kOBmHN8rF4MoE+uG/WCb5NRjHNLtu3lbKMxd8gjjjtPbXDxxeA/TgVN8z3HvYlxXuWQddPX1NMaR8xZHlm5JVjaiCwsJ8xa3lm5O1jPRPQwGR85tcXLhdDL1T53A59vi8sKprnTMxCqrknlA4dLlYe8kzlBMXNdRtVvTBR/vhhDILyyZ5+RuSZd5vetdPyVxXsPDvCp3c7py4x0PF/J3rVma0GleOza4vkUqzN5idcy7bscdJu/6Rjc0OtHoZKMzbe5Ho/v5zu7tspPmyTj97zrShk6rEx/vSPsIw+scab+jf2dXbTcS94F2nKK4alu03Y0Ut7P/R3Kt+43N+5uVI+6nitIR92+H0hH3i6t9Zk1zrfvhrfvvrfv9zb8vKDKU/HuGoiUpqisekMzyByTWFkhZSYx0d6c73elzpd8alDUY:8CEB
^FO96,1600^GFA,05760,05760,00060,:Z64:
eJzt1b1uHCEQB3AQxZa8QCQehReLwkYu/FpYLlzmEULkwmWQUoQoiMl/Bu6OVW59ciKlyU5j64bffsDMrFJHHHHEEf9H2PLnNtCNBQvRej2jdzOTTW/NTCt2XssS5Zu2Xs+4v7A+uxsbDdt2bFx2rnq22ezYsJqbVu0cY1B656q3LZ2sS/LHjHU+TjaoBZtis9peBRDXxQmGojxRtJRs4lsVqZs0rAMKFfsWbTLFxSV3q8RiM9k6ymwN762WqhAbKGIVqoGzsPwMug5rkGHrqbDFwYhtw+IJsGKVrIV112zo1nKZny3hv7xvlxJWsaaydTlEJDWx1WSaxQofwx2yDtafbTXFZh/96mLT3SbPVlGvDVMXrHCROOthw8YmJ/Zdtz7izJAMvSaXwo9jU1Ptmo1WrBp2tZOteG62OBXsc4Cl2WLbk1hzsvliC2yT66JCXrFebODtRbKJxc3UxVJxD2er2eIE2S5UeRwMq7tNs/3ys7jH79esm610PZfJxn6DfRk26nKxYWt7TW7sM+zTy6jnMlv6ccu+PBf36alb3cSi58R+3Nh2xT7C3ufRv5N9GHV12mf63X6F/Sw24EDNZOuwU22wRW3gFPEDuuQOLyj9G9almsw1aWd7qiu/ih11JTVJuFET6yM6RKxLYs2mnk9W6tmlpjc2+26z9MJDM5s+GnbuowZbxTqpONO7E5bs3L94pW6n/r1YHEgKRISpcE98RlQvcwP73O08N7gIi9hF5ozYhdTZnubVOKMxrzCVsuVDyfIjf3/ZajQ429DaPCdHbfCcxAq8YZosz1a2mL4LctHXD/N8rsNO8xkb61K3mHhica2iuIuDWK4VLd9XWWaKfDnQk9Ns53iP31//Ju3HYf+NPeKII4444ogj3hi/AAeOBqA=:E1D3
^FT907,300^BQN,2,4
^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}^FS
^FO10,677^GB1151,0,5^FS
^FT47,594^A0N,58,57^FH\^FDCarton No.:^FS
^FT329,594^A0N,58,57^FH\^FD{Integer.Parse(sn(0).Split(";")(0)).ToString("00000")}^FS
^FT497,595^A0N,56,57^FH\^FD{sn(0).Split(";")(1)}{LOTInfo(17)}^FS
^FT782,484^A0N,58,57^FH\^FDQTY:^FS
^FT759,539^A0N,58,57^FH\^FDN.W.:^FS
^FT759,594^A0N,58,57^FH\^FDG.W.:^FS
^FT913,539^A0N,58,57^FH\^FD{WNetto} kg^FS
^FT913,594^A0N,58,57^FH\^FD{WBrutto} kg^FS
^FT913,484^A0N,58,57^FH\^FD10 PCS^FS
^BY2,3,72^FT125,812^BCN,,N,N
^FD>:{Mid(sn(1), 1, 4)}>5{Mid(sn(1), 5, 4)}>6{Mid(sn(1), 9, 3)}>5{Mid(sn(1), 12)}^FS
^FT154,845^A0N,33,33^FH\^FDSN: {sn(1)}^FS
^BY2,3,72^FT672,812^BCN,,N,N
^FD>:{Mid(sn(2), 1, 4)}>5{Mid(sn(2), 5, 4)}>6{Mid(sn(2), 9, 3)}>5{Mid(sn(2), 12)}^FS
^FT701,845^A0N,33,33^FH\^FDSN: {sn(2)}^FS
^BY2,3,72^FT125,978^BCN,,N,N
^FD>:{Mid(sn(3), 1, 4)}>5{Mid(sn(3), 5, 4)}>6{Mid(sn(3), 9, 3)}>5{Mid(sn(3), 12)}^FS
^FT154,1011^A0N,33,33^FH\^FDSN: {sn(3)}^FS
^BY2,3,72^FT672,978^BCN,,N,N
^FD>:{Mid(sn(4), 1, 4)}>5{Mid(sn(4), 5, 4)}>6{Mid(sn(4), 9, 3)}>5{Mid(sn(4), 12)}^FS
^FT701,1011^A0N,33,33^FH\^FDSN: {sn(4)}^FS
^BY2,3,72^FT125,1144^BCN,,N,N
^FD>:{Mid(sn(5), 1, 4)}>5{Mid(sn(5), 5, 4)}>6{Mid(sn(5), 9, 3)}>5{Mid(sn(5), 12)}^FS
^FT154,1177^A0N,33,33^FH\^FDSN: {sn(5)}^FS
^BY2,3,72^FT672,1144^BCN,,N,N
^FD>:{Mid(sn(6), 1, 4)}>5{Mid(sn(6), 5, 4)}>6{Mid(sn(6), 9, 3)}>5{Mid(sn(6), 12)}^FS
^FT701,1177^A0N,33,33^FH\^FDSN: {sn(6)}^FS
^BY2,3,72^FT125,1310^BCN,,N,N
^FD>:{Mid(sn(7), 1, 4)}>5{Mid(sn(7), 5, 4)}>6{Mid(sn(7), 9, 3)}>5{Mid(sn(7), 12)}^FS
^FT154,1343^A0N,33,33^FH\^FDSN: {sn(7)}^FS
^BY2,3,72^FT672,1310^BCN,,N,N
^FD>:{Mid(sn(8), 1, 4)}>5{Mid(sn(8), 5, 4)}>6{Mid(sn(8), 9, 3)}>5{Mid(sn(8), 12)}^FS
^FT701,1343^A0N,33,33^FH\^FDSN: {sn(8)}^FS
^BY2,3,72^FT117,1477^BCN,,N,N
^FD>:{Mid(sn(9), 1, 4)}>5{Mid(sn(9), 5, 4)}>6{Mid(sn(9), 9, 3)}>5{Mid(sn(9), 12)}^FS
^FT146,1510^A0N,33,33^FH\^FDSN: {sn(9)}^FS
^BY2,3,72^FT665,1477^BCN,,N,N
^FD>:{Mid(sn(10), 1, 4)}>5{Mid(sn(10), 5, 4)}>6{Mid(sn(10), 9, 3)}>5{Mid(sn(10), 12)}^FS
^FT694,1510^A0N,33,33^FH\^FDSN: {sn(10)}^FS
^FO10,1575^GB1151,0,5^FS
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






