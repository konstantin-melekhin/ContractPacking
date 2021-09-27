Imports System.Deployment.Application
Imports System.Drawing.Printing
Imports System.IO
Imports Library3

Public Class WF_SberDevice
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
            where P.LOTID = {LOTID} And BoxNum = {LastPackCounter(1)} And LiterID = {PCInfo(8)}
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
#Region " условия для возврата в окно настроек"
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
                    ElseIf _stepArr.Count > 0 And _stepArr(4) = 1 And _stepArr(5) = 2 Then
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
            SerchBoxForPrint(LOTID, BoxNumber, PCInfo(8))
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
    Private Sub SerchBoxForPrint(LotID As Integer, BoxNum As Integer, LiterID As Integer) 'LitName As String,
        'SELECT  [UnitNum] as '№',l.Content AS 'Серийный номер платы',Lit.LiterName as 'Литера' ,[BoxNum]as 'Номер коробки'
        SQL = "use fas
                SELECT  [UnitNum] as '№',F.SN AS 'Серийный номер платы',Lit.LiterName as 'Литера' ,[BoxNum]as 'Номер коробки' 
                FROM [FAS].[dbo].[Ct_PackingTable] as P
                left join [SMDCOMPONETS].[dbo].[LazerBase] as L On l.IDLaser = PCBID
                left join dbo.Ct_FASSN_reg as F On F.ID =P.SNID
                left join dbo.FAS_Liter as Lit On Lit.ID = P.LiterID
                where p.lotid =" & LotID & "and literid = " & LiterID & " and BoxNum = " & BoxNum & "order by UnitNum
                " 'and LiterName= '" & LitName & "'
        LoadGridFromDB(DG_SelectedBox, SQL)
    End Sub

    Private Function GetSNFromGrid()
        Dim SNArrayTemp As New ArrayList
        If DG_SelectedBox.Rows.Count > 0 Then
            SNArrayTemp.Add(DG_SelectedBox.Item(3, 0).Value & " " & DG_SelectedBox.Item(2, 0).Value)
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

    '    Private Function GetGroupLabel(sn As ArrayList, x As Integer, y As Integer)
    '        Return $"
    '     ^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
    '^XA
    '^MMT
    '^PW1181
    '^LL1772
    '^LS0
    '^FO352,1600^GFA,09600,09600,00100,:Z64:
    'eJzt1kFu2zAQAEAKOujIJ/Ap7M8oIwcf/YR+xUUPPuYJVdAHREEOYWGC290lKYsSFbSuHRQBFzBkh6sdRSKpFaJGjRo13osW/uHkBvo/SZOYdknV4z0M9QGGhuPdDfNJDPgIY/hyb4Oz/kejdXiOF6J7whNNvxyaH4MRUzkcGsqmIZ8VNoMelTNWOl5TEp+j+gV9C2MHPf4QNMxDGvjYQgsN7OhLz6mCslpAw4CQVj94qtLAsU0+GdppMgwMWGjED/QSXJsMHYYA+JgZlMpGBzA2dB4bmn6sDIcGpmERix/oFfgWjpMRKvAxM0wyJI/AUVrz3VOVpWGc8tJhEStCIejxAhsYgmF4CCvEYzQ6HIVkKKCbC0MwODU3wMlo4MfziXSByaDbBg4r8HHboP+FDapSMEC6eKGUEY0xN2RuSBiaydCTAT9LxksoEIrsscAJ6z/B0QQjDPvOc4rPjVMyLOD8w6lDRqiyNg6uwzKhwAPNKDTsZPguGFMKGyOnivjURmFVMtql8Uo34GK0ydDBeN0wFM2kZBgycBHKX/C6YTTwiLO3xRJ0aVzCDMolg24iGdKFiwjG80jfojHQXoIZZFCV3czAIm/8rGOhuQFsvEGsvTLsXxmiZMiFAbmhl4amjPO5aIRNYm10EGclDzUr4+SKxnnLkAVDZEa6jCuNZ9pQ10YTjedg6IVhysbLpqELRpsby3tlTr5gwAtEg/eANHdx3r77PB7j8zjkBjwsjVG+bzzSAoNsDXYuGWHtLdYgpuVrUOFuhYsfYkpuHHDuXoxpL5HROGwalLqHtJco2ktws94wFBUCCfM98RvYyeC9MO2JwAV2XOiUjLQnBmO/MiQb8eRpb98lI+3ph3hMRrjpe8j2djJSlZnhyZDpZJfeUU18f3gesrG+nRv9ZEzvKNyuqb4rGOE9CKV37eUVmd65bNDTOGYGv2vJEGvDBUPQe5Z7Bs89QzJiO3HpGaKBpbBeNCT8CD0DG9wzrAzHvY8PvY/j3qeJfYmTi95nZmBqMDr4GnofvAWl3gd7K5zatJ/40MNZ7uGwDwwzJrZ3KvVwwdijMWBqMFpQo+DzyVj0cLPozj70ogPZ2XipF+VThvSNBua96IYhOrf+27WxZcj7Gx3Yde6NDVM2uuGGBvXUhVBX/XdlAyfkUMrWVz2lTeNYyja+9NfrjBZKc03QyrqhUb4pVt3Q2LjeUV5j1KhRo0aNGjVq1KhRo0aNGp8ifgPNNWec:5A80
    '^BY2,3,41^FT1049,1065^BCI,,N,N
    '^FD>:" & Mid(sn(1), 1, 2) & ">5" & Mid(sn(1), 3) & "^FS
    '^FT1055,1109^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,1109^A0I,29,28^FH\^FD" & sn(1) & "^FS
    '^BY2,3,41^FT1049,958^BCI,,N,N
    '^FD>:" & Mid(sn(2), 1, 2) & ">5" & Mid(sn(2), 3) & "^FS
    '^FT1055,1003^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,1003^A0I,29,28^FH\^FD" & sn(2) & "^FS
    '^BY2,3,41^FT1049,847^BCI,,N,N
    '^FD>:" & Mid(sn(3), 1, 2) & ">5" & Mid(sn(3), 3) & "^FS
    '^FT1055,891^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,891^A0I,29,28^FH\^FD" & sn(3) & "^FS
    '^BY2,3,41^FT1049,741^BCI,,N,N
    '^FD>:" & Mid(sn(4), 1, 2) & ">5" & Mid(sn(4), 3) & "^FS
    '^FT1055,785^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,785^A0I,29,28^FH\^FD" & sn(4) & "^FS
    '^BY2,3,41^FT1049,629^BCI,,N,N
    '^FD>:" & Mid(sn(5), 1, 2) & ">5" & Mid(sn(5), 3) & "^FS
    '^FT1055,674^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,674^A0I,29,28^FH\^FD" & sn(5) & "^FS
    '^BY2,3,41^FT1049,516^BCI,,N,N
    '^FD>:" & Mid(sn(6), 1, 2) & ">5" & Mid(sn(6), 3) & "^FS
    '^FT1055,561^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,561^A0I,29,28^FH\^FD" & sn(6) & "^FS
    '^BY2,3,41^FT1049,405^BCI,,N,N
    '^FD>:" & Mid(sn(7), 1, 2) & ">5" & Mid(sn(7), 3) & "^FS
    '^FT1055,449^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,449^A0I,29,28^FH\^FD" & sn(7) & "^FS
    '^BY2,3,41^FT1049,299^BCI,,N,N
    '^FD>:" & Mid(sn(8), 1, 2) & ">5" & Mid(sn(8), 3) & "^FS
    '^FT1055,343^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,343^A0I,29,28^FH\^FD" & sn(8) & "^FS
    '^BY2,3,41^FT1049,187^BCI,,N,N
    '^FD>:" & Mid(sn(9), 1, 2) & ">5" & Mid(sn(9), 3) & "^FS
    '^FT1055,232^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,232^A0I,29,28^FH\^FD" & sn(9) & "^FS
    '^BY2,3,41^FT1049,79^BCI,,N,N
    '^FD>:" & Mid(sn(10), 1, 2) & ">5" & Mid(sn(10), 3) & "^FS
    '^FT1055,124^A0I,29,28^FH\^FDS/N:^FS
    '^FT1002,124^A0I,29,28^FH\^FD" & sn(10) & "^FS
    '^BY2,3,41^FT564,1065^BCI,,N,N
    '^FD>:" & Mid(sn(11), 1, 2) & ">5" & Mid(sn(11), 3) & "^FS
    '^FT571,1109^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,1109^A0I,29,28^FH\^FD" & sn(11) & "^FS
    '^BY2,3,41^FT564,958^BCI,,N,N
    '^FD>:" & Mid(sn(12), 1, 2) & ">5" & Mid(sn(12), 3) & "^FS
    '^FT571,1003^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,1003^A0I,29,28^FH\^FD" & sn(12) & "^FS
    '^BY2,3,41^FT564,847^BCI,,N,N
    '^FD>:" & Mid(sn(13), 1, 2) & ">5" & Mid(sn(13), 3) & "^FS
    '^FT571,891^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,891^A0I,29,28^FH\^FD" & sn(13) & "^FS
    '^BY2,3,41^FT564,741^BCI,,N,N
    '^FD>:" & Mid(sn(14), 1, 2) & ">5" & Mid(sn(14), 3) & "^FS
    '^FT571,785^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,785^A0I,29,28^FH\^FD" & sn(14) & "^FS
    '^BY2,3,41^FT564,629^BCI,,N,N
    '^FD>:" & Mid(sn(15), 1, 2) & ">5" & Mid(sn(15), 3) & "^FS
    '^FT571,674^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,674^A0I,29,28^FH\^FD" & sn(15) & "^FS
    '^BY2,3,41^FT564,516^BCI,,N,N
    '^FD>:" & Mid(sn(16), 1, 2) & ">5" & Mid(sn(16), 3) & "^FS
    '^FT571,561^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,561^A0I,29,28^FH\^FD" & sn(16) & "^FS
    '^BY2,3,41^FT564,405^BCI,,N,N
    '^FD>:" & Mid(sn(17), 1, 2) & ">5" & Mid(sn(17), 3) & "^FS
    '^FT571,449^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,449^A0I,29,28^FH\^FD" & sn(17) & "^FS
    '^BY2,3,41^FT564,299^BCI,,N,N
    '^FD>:" & Mid(sn(18), 1, 2) & ">5" & Mid(sn(18), 3) & "^FS
    '^FT571,343^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,343^A0I,29,28^FH\^FD" & sn(18) & "^FS
    '^BY2,3,41^FT564,187^BCI,,N,N
    '^FD>:" & Mid(sn(19), 1, 2) & ">5" & Mid(sn(19), 3) & "^FS
    '^FT571,232^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,232^A0I,29,28^FH\^FD" & sn(19) & "^FS
    '^BY2,3,41^FT564,79^BCI,,N,N
    '^FD>:" & Mid(sn(20), 1, 2) & ">5" & Mid(sn(20), 3) & "^FS
    '^FT571,124^A0I,29,28^FH\^FDS/N:^FS
    '^FT518,124^A0I,29,28^FH\^FD" & sn(20) & "^FS
    '^FT367,1615^A0I,67,67^FH\^FD" & sn(0) & "^FS
    '^FT440,1600^BQN,2,5
    '^FDLA," & sn(1) & ";" & sn(2) & ";" & sn(3) & ";" & sn(4) & ";" & sn(5) & ";" & sn(6) & ";" & sn(7) & ";" & sn(8) & ";" & sn(9) & ";" & sn(10) & ";" & sn(11) & ";" & sn(12) & ";" & sn(13) & ";" & sn(14) & ";" & sn(15) & ";" & sn(16) & ";" & sn(17) & ";" & sn(18) & ";" & sn(19) & ";" & sn(20) & ";^FS
    '^PQ1,0,1,Y^XZ
    '"
    '    End Function
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
                SerchBoxForPrint(SearchSNList(1), SearchSNList(3), PCInfo(8))
                SNArray = GetSNFromGrid()
                GetGroupLabel(SNArray, PrinterInfo(0).Split(";")(1), PrinterInfo(0).Split(";")(2))
                TB_ScanSN.Clear()
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
            SerchBoxForPrint(LOTID, NumBox.Value, PCInfo(8))
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
        SQL = "use fas
                SELECT  l.Content,p.[LOTID],Lit.LiterName ,[BoxNum]
                FROM [FAS].[dbo].[Ct_PackingTable] as P
                left join [SMDCOMPONETS].[dbo].[LazerBase] as L On l.IDLaser = PCBID
                left join dbo.Ct_FASSN_reg as F On F.ID =P.SNID
                left join dbo.FAS_Liter as Lit On Lit.ID = P.LiterID
                where l.Content = '" & Sn & "'"
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
    Private Function GetGroupLabel(sn As ArrayList, x As Integer, y As Integer)
        Return $"
    ^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO352,1600^GFA,09600,09600,00100,:Z64:
eJzt1kFu2zAQAEAKOujIJ/Ap7M8oIwcf/YR+xUUPPuYJVdAHREEOYWGC290lKYsSFbSuHRQBFzBkh6sdRSKpFaJGjRo13osW/uHkBvo/SZOYdknV4z0M9QGGhuPdDfNJDPgIY/hyb4Oz/kejdXiOF6J7whNNvxyaH4MRUzkcGsqmIZ8VNoMelTNWOl5TEp+j+gV9C2MHPf4QNMxDGvjYQgsN7OhLz6mCslpAw4CQVj94qtLAsU0+GdppMgwMWGjED/QSXJsMHYYA+JgZlMpGBzA2dB4bmn6sDIcGpmERix/oFfgWjpMRKvAxM0wyJI/AUVrz3VOVpWGc8tJhEStCIejxAhsYgmF4CCvEYzQ6HIVkKKCbC0MwODU3wMlo4MfziXSByaDbBg4r8HHboP+FDapSMEC6eKGUEY0xN2RuSBiaydCTAT9LxksoEIrsscAJ6z/B0QQjDPvOc4rPjVMyLOD8w6lDRqiyNg6uwzKhwAPNKDTsZPguGFMKGyOnivjURmFVMtql8Uo34GK0ydDBeN0wFM2kZBgycBHKX/C6YTTwiLO3xRJ0aVzCDMolg24iGdKFiwjG80jfojHQXoIZZFCV3czAIm/8rGOhuQFsvEGsvTLsXxmiZMiFAbmhl4amjPO5aIRNYm10EGclDzUr4+SKxnnLkAVDZEa6jCuNZ9pQ10YTjedg6IVhysbLpqELRpsby3tlTr5gwAtEg/eANHdx3r77PB7j8zjkBjwsjVG+bzzSAoNsDXYuGWHtLdYgpuVrUOFuhYsfYkpuHHDuXoxpL5HROGwalLqHtJco2ktws94wFBUCCfM98RvYyeC9MO2JwAV2XOiUjLQnBmO/MiQb8eRpb98lI+3ph3hMRrjpe8j2djJSlZnhyZDpZJfeUU18f3gesrG+nRv9ZEzvKNyuqb4rGOE9CKV37eUVmd65bNDTOGYGv2vJEGvDBUPQe5Z7Bs89QzJiO3HpGaKBpbBeNCT8CD0DG9wzrAzHvY8PvY/j3qeJfYmTi95nZmBqMDr4GnofvAWl3gd7K5zatJ/40MNZ7uGwDwwzJrZ3KvVwwdijMWBqMFpQo+DzyVj0cLPozj70ogPZ2XipF+VThvSNBua96IYhOrf+27WxZcj7Gx3Yde6NDVM2uuGGBvXUhVBX/XdlAyfkUMrWVz2lTeNYyja+9NfrjBZKc03QyrqhUb4pVt3Q2LjeUV5j1KhRo0aNGjVq1KhRo0aNGp8ifgPNNWec:5A80
^BY2,3,41^FT1049,1065^BCI,,N,N
^FD>:" & Mid(sn(1), 1, 2) & ">5" & Mid(sn(1), 3) & "^FS
^FT1055,1109^A0I,29,28^FH\^FDS/N:^FS
^FT1002,1109^A0I,29,28^FH\^FD" & sn(1) & "^FS
^BY2,3,41^FT1049,958^BCI,,N,N
^FD>:" & Mid(sn(2), 1, 2) & ">5" & Mid(sn(2), 3) & "^FS
^FT1055,1003^A0I,29,28^FH\^FDS/N:^FS
^FT1002,1003^A0I,29,28^FH\^FD" & sn(2) & "^FS
^BY2,3,41^FT1049,847^BCI,,N,N
^FD>:" & Mid(sn(3), 1, 2) & ">5" & Mid(sn(3), 3) & "^FS
^FT1055,891^A0I,29,28^FH\^FDS/N:^FS
^FT1002,891^A0I,29,28^FH\^FD" & sn(3) & "^FS
^BY2,3,41^FT1049,741^BCI,,N,N
^FD>:" & Mid(sn(4), 1, 2) & ">5" & Mid(sn(4), 3) & "^FS
^FT1055,785^A0I,29,28^FH\^FDS/N:^FS
^FT1002,785^A0I,29,28^FH\^FD" & sn(4) & "^FS
^BY2,3,41^FT1049,629^BCI,,N,N
^FD>:" & Mid(sn(5), 1, 2) & ">5" & Mid(sn(5), 3) & "^FS
^FT1055,674^A0I,29,28^FH\^FDS/N:^FS
^FT1002,674^A0I,29,28^FH\^FD" & sn(5) & "^FS
^BY2,3,41^FT1049,516^BCI,,N,N
^FD>:" & Mid(sn(6), 1, 2) & ">5" & Mid(sn(6), 3) & "^FS
^FT1055,561^A0I,29,28^FH\^FDS/N:^FS
^FT1002,561^A0I,29,28^FH\^FD" & sn(6) & "^FS
^BY2,3,41^FT1049,405^BCI,,N,N
^FD>:" & Mid(sn(7), 1, 2) & ">5" & Mid(sn(7), 3) & "^FS
^FT1055,449^A0I,29,28^FH\^FDS/N:^FS
^FT1002,449^A0I,29,28^FH\^FD" & sn(7) & "^FS
^BY2,3,41^FT1049,299^BCI,,N,N
^FD>:" & Mid(sn(8), 1, 2) & ">5" & Mid(sn(8), 3) & "^FS
^FT1055,343^A0I,29,28^FH\^FDS/N:^FS
^FT1002,343^A0I,29,28^FH\^FD" & sn(8) & "^FS
^BY2,3,41^FT1049,187^BCI,,N,N
^FD>:" & Mid(sn(9), 1, 2) & ">5" & Mid(sn(9), 3) & "^FS
^FT1055,232^A0I,29,28^FH\^FDS/N:^FS
^FT1002,232^A0I,29,28^FH\^FD" & sn(9) & "^FS
^BY2,3,41^FT1049,79^BCI,,N,N
^FD>:" & Mid(sn(10), 1, 2) & ">5" & Mid(sn(10), 3) & "^FS
^FT1055,124^A0I,29,28^FH\^FDS/N:^FS
^FT1002,124^A0I,29,28^FH\^FD" & sn(10) & "^FS
^BY2,3,41^FT564,1065^BCI,,N,N
^FD>:" & Mid(sn(11), 1, 2) & ">5" & Mid(sn(11), 3) & "^FS
^FT571,1109^A0I,29,28^FH\^FDS/N:^FS
^FT518,1109^A0I,29,28^FH\^FD" & sn(11) & "^FS
^BY2,3,41^FT564,958^BCI,,N,N
^FD>:" & Mid(sn(12), 1, 2) & ">5" & Mid(sn(12), 3) & "^FS
^FT571,1003^A0I,29,28^FH\^FDS/N:^FS
^FT518,1003^A0I,29,28^FH\^FD" & sn(12) & "^FS
^BY2,3,41^FT564,847^BCI,,N,N
^FD>:" & Mid(sn(13), 1, 2) & ">5" & Mid(sn(13), 3) & "^FS
^FT571,891^A0I,29,28^FH\^FDS/N:^FS
^FT518,891^A0I,29,28^FH\^FD" & sn(13) & "^FS
^FT367,1615^A0I,67,67^FH\^FD" & sn(0) & "^FS
^FT440,1600^BQN,2,5
^FDLA," & sn(1) & ";" & sn(2) & ";" & sn(3) & ";" & sn(4) & ";" & sn(5) & ";" & sn(6) & ";" & sn(7) & ";" & sn(8) & ";" & sn(9) & ";" & sn(10) & ";" & sn(11) & ";" & sn(12) & ";" & sn(13) & ";^FS
^PQ1,0,1,Y^XZ
"
    End Function
#End Region
End Class






