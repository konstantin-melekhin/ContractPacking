﻿Imports System.Deployment.Application
Imports System.Drawing.Printing
Imports System.IO
Imports Library3

Public Class WF_PackWithOutPrint
    Public Sub New(LOTID As Integer, IDApp As Integer)
        InitializeComponent()
        Me.LOTID = LOTID
        Me.IDApp = IDApp
    End Sub

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
    Dim UserInfo As New ArrayList()
    Dim TableColumn As ArrayList
    Dim PrinterInfo() As String
#Region "Загрузка рабочей формы"
    Private Sub WF_PackWithOutPrint_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim myVersion As Version
        If ApplicationDeployment.IsNetworkDeployed Then
            myVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion
        End If
        CB_TestRes.Checked = True
        LB_SW_Wers.Text = String.Concat("v", myVersion)
#Region "Обнаружение принтеров и установка дефолтного принтера"
        For Each item In PrinterSettings.InstalledPrinters
            If InStr(item.ToString(), "ZDesigner") Then
                CB_DefaultPrinter.Items.Add(item.ToString())
            End If
        Next
        If CB_DefaultPrinter.Items.Count = 0 Then
            PrintLabel(Controllabel, "Ни один принтер не подключен!", 12, 234, Color.Red)
        Else
            CB_DefaultPrinter.Text = CB_DefaultPrinter.Items(0)
        End If
        GetCoordinats()
#End Region
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
            LoadGridFromDB2(DG_Packing, "use FAS
            SELECT UnitNum as '№',l.Content AS 'SMT Номер',SN.SN AS 'FAS Номер',(Lit.LiterName + ' ' + cast(LiterIndex as nvarchar (5))) AS 'Литера' 
            ,PalletNum as 'Паллет', BoxNum as 'Групповая', Format(PackingDate,'dd.MM.yyyy HH:mm:ss') as 'Дата'
            FROM [FAS].[dbo].[Ct_PackingTable] as P
            Left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = p.PCBID
            Left join [FAS].[dbo].Ct_FASSN_reg as Sn On Sn.ID = p.SNID
            Left join [FAS].[dbo].FAS_Liter as Lit On Lit.ID = p.LiterID
            where P.LOTID = " & LOTID & " And BoxNum = " & LastPackCounter(1) & " And LiterID = " & PCInfo(8) & "
            order by UnitNum desc", ds)
        ElseIf LOTInfo(15) = LastPackCounter(2) Then
            LoadGridFromDB2(DG_Packing, "use FAS
            SELECT UnitNum as '№',l.Content AS 'SMT Номер',SN.SN AS 'FAS Номер',(Lit.LiterName + ' ' + cast(LiterIndex as nvarchar (5))) AS 'Литера' 
            ,PalletNum as 'Паллет', BoxNum as 'Групповая', Format(PackingDate,'dd.MM.yyyy HH:mm:ss') as 'Дата'
            FROM [FAS].[dbo].[Ct_PackingTable] as P
            Left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = p.PCBID
            Left join [FAS].[dbo].Ct_FASSN_reg as Sn On Sn.ID = p.SNID
            Left join [FAS].[dbo].FAS_Liter as Lit On Lit.ID = p.LiterID
            where P.LOTID = 0
            order by UnitNum desc", ds)
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
#Region "Очистка поля ввода номера"
    Private Sub BT_ClearSN_Click(sender As Object, e As EventArgs) Handles BT_ClearSN.Click
        CB_Reprint.Checked = False
        CB_Technik_Reprint.Checked = False
        SerialTextBox.Clear()
        SerialTextBox.Enabled = True
        SNBufer = New ArrayList()
        Controllabel.Text = ""
        SerialTextBox.Focus()
    End Sub
#End Region
#Region "Часы в программе"
    Private Sub CurrentTimeTimer_Tick(sender As Object, e As EventArgs) Handles CurrentTimeTimer.Tick
        CurrrentTimeLabel.Text = TimeString
    End Sub 'Часы в программе
#End Region
#Region "Регистрация пользователя"
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
#Region "Условия для возврата в окно настроек"
    ' условия для возврата в окно настроек
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
    '_________________________________________________________________________________________________________________
    'начало работы приложения FAS Scanning Station
    '________________________________________________________________________________________________________________
#Region "Окно ввода серийного номера платы"
    Private Sub SerialTextBox_KeyDown(sender As Object, e As KeyEventArgs) Handles SerialTextBox.KeyDown
        If e.KeyCode = Keys.Enter Then 'And (SerialTextBox.TextLength = LenSN_SMT Or SerialTextBox.TextLength = LenSN_FAS) Then
            'определение формата номера

            If GetFTSN(LOTInfo(12)) = True Then
                If SNFormat(1) = 1 Then
                    If CheckTestRes(SerialTextBox.Text) = False Then
                        PrintLabel(Controllabel, $"Серийный {SerialTextBox.Text} номер не прошел тестирование!{vbCrLf}Повторите тест!", 12, 193, Color.Red)
                        SerialTextBox.Enabled = False
                        Exit Sub
                    End If
                End If
                If CB_Reprint.Checked = False And CB_Technik_Reprint.Checked = False Then
                    'проверка диапазона номера
                    If CheckRange(SNFormat) = True Then
                        'проверка задвоения и наличия номера в базе
                        If CheckDublicate(GetPcbID(SNFormat)) = True Then
                            Dim Mess As String
                            If LOTInfo(12) = False Then ' если номер двойной
                                If SNBufer.Count = 0 Then
                                    Select Case SNFormat(1)
                                        Case Is > 1 ' запись в буфер СМТ номера
                                            SNBufer = New ArrayList From {True, SerialTextBox.Text, False, ""}
                                            Mess = "SMT номер " & SerialTextBox.Text & " определен!" & vbCrLf &
                                               "Отсканируйте номер FAS!"
                                        Case 1 'запись в буфер FAS номера
                                            SNBufer = New ArrayList From {False, "", True, SerialTextBox.Text}
                                            Mess = "FAS номер " & SerialTextBox.Text & " определен!" & vbCrLf &
                                               "Отсканируйте номер SMT!"
                                    End Select
                                    'если в буфере имеется СМТ номер
                                ElseIf SNBufer.Count <> 0 And SNBufer(0) = True And SNBufer(2) = False Then
                                    'Запись в базу
                                    WriteDB(SNBufer(1), SerialTextBox.Text)
                                    Mess = "Номера определены и записаны в базу!"
                                    'если в буфере имеется СМТ номер
                                ElseIf SNBufer.Count <> 0 And SNBufer(0) = False And SNBufer(2) = True Then
                                    'Запись в базу
                                    WriteDB(SerialTextBox.Text, SNBufer(3))
                                    Mess = "Номера определены и записаны в базу!"
                                End If
                            Else LOTInfo(12) = True  'SingleSN
                                Select Case SNFormat(1)
                                    Case > 1 ' одиночный СМТ номер
                                        SNID = 0
                                        WriteDB(SerialTextBox.Text, "")
                                        Mess = "SMT номер " & SerialTextBox.Text & " определен и " & vbCrLf &
                                            "записан в базу!"
                                    Case = 1 ' одиночный ФАС номер
                                        PCBID = 0
                                        WriteDB("", SerialTextBox.Text)
                                        Mess = "FAS номер " & SerialTextBox.Text & " определен и" & vbCrLf &
                                            "записан в базу!"

                                End Select
                            End If
                            PrintLabel(Controllabel, Mess, 12, 193, Color.Green)
                            SerialTextBox.Clear()
                        End If
                    End If
                ElseIf CB_Reprint.Checked = True Then
                    Dim value As Boolean
                    For i = 0 To DG_Packing.Rows.Count - 1
                        If SerialTextBox.Text = DG_Packing.Item(1, i).Value Then
                            value = True
                            Exit For
                        End If
                    Next
                    If value = True Then
                        Print(SerialTextBox.Text, CB_DefaultPrinter.Text)
                        CB_Reprint.Checked = False
                        SerialTextBox.Clear()
                    Else
                        PrintLabel(Controllabel, $"Номер не найден в списке {vbCrLf}данной групповой коробки!", 12, 193, Color.Red)
                        SerialTextBox.Enabled = False
                    End If
                ElseIf CB_Technik_Reprint.Checked = True Then
                    Print(SerialTextBox.Text, CB_DefaultPrinter.Text)
                    'CB_Technik_Reprint.Checked = False
                    SerialTextBox.Clear()
                End If
            End If
        End If
        SerialTextBox.Focus()
    End Sub
#End Region
#Region "'1. Определение формата номера и результата теста по серийному/MAC номеру бумажному"
    Private Function GetFTSN(SingleSN As Boolean) As Boolean

        'Return True
        'Exit Function

        Dim col As Color, Mess As String, Res As Boolean
        SNFormat = New ArrayList()
        SNFormat = CheckSNFormate2.GetSNFormat2(LOTInfo(19), LOTInfo(8), SerialTextBox.Text, LOTInfo(18), LOTInfo(2), LOTInfo(7))
        Res = SNFormat(0)
        Mess = SNFormat(3)
        'SNFormat(0) ' Результат проверки True/False
        'SNFormat(1) ' 1 - SMT/ 2 - FAS / 3 - Неопределен
        'SNFormat(2) ' Переменный номер
        'SNFormat(3) ' Текст сообщения
        If Res = True Then
            If SingleSN = False Then
                If SNBufer.Count <> 0 Then
                    If SNBufer(1) = SerialTextBox.Text Or SNBufer(3) = SerialTextBox.Text Then
                        Mess = "Этот номер " & SerialTextBox.Text & " уже был отсканирован. " & vbCrLf &
                        "Сбросьте ошибку и повторите сканирование обоих" & vbCrLf & "номеров платы заново!"
                        Res = False
                    ElseIf SNBufer(3) <> "" And SNFormat(1) = 1 Then
                        Mess = "SMT номер уже был отсканирован. " & vbCrLf &
                        "Сбросьте ошибку и повторите сканирование обоих" & vbCrLf & "номеров платы заново!"
                        Res = False
                    ElseIf SNBufer(1) <> "" And SNFormat(1) > 1 Then
                        Mess = "FAS номер уже был отсканирован. " & vbCrLf &
                        "Сбросьте ошибку и повторите сканирование обоих" & vbCrLf & "номеров платы заново!"
                        Res = False
                    End If
                End If
            End If
        End If
        col = If(Res = False, Color.Red, Color.Green)
        PrintLabel(Controllabel, Mess, 12, 193, col)
        SNTBEnabled(Res)
        Return Res
    End Function
    Private Function CheckTestRes(Sn As String) As Boolean
        If CB_TestRes.Checked = True Then
            Dim res As Boolean
            If SelectListString($"use fas   select top 1 [Result] FROM [FAS].[dbo].[Pc_Testing_Results] where Result = 1 and MAC = '{Sn}'").Count = 1 Then
                res = True
            ElseIf Selectstring($"use fas select СustomersID  from Contract_LOT where id = {lotid}") = 8 Then
                If SelectListString($"use fas   select top 1 [Result] FROM [FAS].[dbo].[Pc_Testing_Results] where Result = 1 and MAC = 
                    '{SelectString($"use fas select MAC1 from Depo_SN_MAC where SN = '{Sn}'")}'").Count = 1 Then
                    res = True
                End If
            Else
                res = SelectListString($"use fas select ResultFileName from  [FAS].[dbo].[Fas_Depo_Test_Result] where SN = 
                                        (SELECT CONVERT(INT, CONVERT(VARBINARY, '0x00000000' + 
                                         SUBSTRING((select MAC1 from Depo_SN_MAC where SN = '{Sn}'),7,6), 1)))").Count = 1

            End If
            Return res
        Else
            Return True
        End If
    End Function
    Private Sub Label5_DoubleClick(sender As Object, e As EventArgs) Handles Label5.DoubleClick
        If CB_TestRes.Visible = True Then
            CB_TestRes.Visible = False
        Else
            CB_TestRes.Visible = True
        End If
    End Sub

#End Region
#Region "'2. Проверка диапазона"
    Private Function CheckRange(SNFormat As ArrayList) As Boolean
        Dim res As Boolean
        Dim ChekRange As Boolean, StartRange As Integer, EndRange As Integer
        Select Case SNFormat(1)
            Case 1
                ChekRange = LOTInfo(9)
                StartRange = LOTInfo(10)
                EndRange = LOTInfo(11)

            Case Is > 1
                ChekRange = LOTInfo(4)
                StartRange = LOTInfo(5)
                EndRange = LOTInfo(6)

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
#Region "'3. Поиск ID PCB в базе гравировщика И SNID в базе FASSN_reg"
    Private Function GetPcbID(SNFormat As ArrayList) As ArrayList
        Dim Res As New ArrayList(), Mess As String, Col As Color
        Select Case SNFormat(1)
            Case Is > 1
                PCBID = SelectInt("USE SMDCOMPONETS Select [IDLaser] FROM [SMDCOMPONETS].[dbo].[LazerBase] where Content = '" & SerialTextBox.Text & "'")
                Res.Add(PCBID <> 0)
                Res.Add(PCBID)
                Res.Add(SNFormat(1))
                Mess = If(PCBID = 0, "SMT номер " & SerialTextBox.Text & vbCrLf & "не зарегистрирован в базе гравировщика!", "")
            Case 1
                SNID = SelectInt("USE FAS SELECT [ID] FROM [FAS].[dbo].[Ct_FASSN_reg] where SN = '" & SerialTextBox.Text & "'")
                If SNID = 0 Then
                    SNID = SelectInt("USE FAS " & vbCrLf & "
                       insert into [FAS].[dbo].[Ct_FASSN_reg] ([SN],[LOTID],[UserID],[AppID],[LineID],[RegDate]) values" & vbCrLf & "
                       ('" & SerialTextBox.Text & "'," & LOTID & "," & UserInfo(0) & "," & PCInfo(0) & "," & PCInfo(2) & ", CURRENT_TIMESTAMP)" & vbCrLf & "
                       WAITFOR delay '00:00:00:100'" & vbCrLf & "
                       SELECT [ID] FROM [FAS].[dbo].[Ct_FASSN_reg] where SN = '" & SerialTextBox.Text & "'")
                End If

                Res.Add(SNID <> 0)
                Res.Add(SNID)
                Res.Add(SNFormat(1))
                Mess = If(SNID = 0, "FAS номер " & SerialTextBox.Text & vbCrLf & "не зарегистрирован в базе Ct_FASSN_reg!", "")
        End Select
        Col = If(Res(0) = False, Color.Red, Color.Green)
        PrintLabel(Controllabel, Mess, 12, 193, Col)
        SNTBEnabled(Res(0))
        Return Res
    End Function
#End Region
#Region "'4. Проверка предыдущего шага и дубликатов"
    Private Function CheckDublicate(GetPCB_SNID As ArrayList) As Boolean
        Dim Res As Boolean, SQL As String, Mess As String, Col As Color
        'Проверка предыдущего шага 
        If GetPCB_SNID(0) = True Then
            Select Case GetPCB_SNID(2)
                Case Is > 1
                    Dim PCBStepRes As New ArrayList(SelectListString($"Use FAS
                select 
                tt.StepID,tt.TestResultID, tt.StepDate ,tt.SNID
                from  (SELECT *, ROW_NUMBER() over(partition by pcbid order by ID desc) num 
                FROM [FAS].[dbo].[Ct_OperLog] 
                where PCBID  ={GetPCB_SNID(1)}) tt
                where  tt.num = 1"))
                    If PCBStepRes.Count <> 0 Then
                        If PalletNumber = 1 And BoxNumber = 1 And DG_Packing.RowCount = 0 Then
                            Res = If(PCBStepRes(0) = 40, True, False)
                            Mess = If(Res = False, $"Первая плата {SerialTextBox.Text & vbCrLf}не прошла контроль в ОТК!", "")
                        Else
                            If (PCBStepRes(0) = PreStepID Or PCBStepRes(0) = 40) And PCBStepRes(1) = 2 Then
                                Res = True
                            ElseIf PCBStepRes(0) = 6 And PCBStepRes(1) = 2 Then
                                Res = True
                                Mess = ""
                            Else
                                Res = False
                                Mess = $"Плата {SerialTextBox.Text & vbCrLf}имеет не верный предыдущий шаг! Верните на тест!"
                            End If
                        End If
                    ElseIf PCBStepRes.Count = 0 Then
                        Mess = $"У платы {SerialTextBox.Text & vbCrLf}не найден предыдущий шаг!"
                    End If
                Case 1
                    Res = True
            End Select
            'проверка задвоения в базе
            If Res = True Then
                Dim PackedSN As ArrayList
                Select Case GetPCB_SNID(2)
                    Case Is > 1
                        SQL = "Use FAS SELECT L.Content,S.SN,Lit.LiterName + cast ([LiterIndex] as nvarchar),[PalletNum],[BoxNum],[UnitNum],
                        [PackingDate],U.UserName,p.LOTID
                        FROM [FAS].[dbo].[Ct_PackingTable] as P
                        left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = P.PCBID
                        Left join Ct_FASSN_reg as S On S.ID = P.SNID
                        Left join FAS_Liter as Lit On Lit.ID = P.LiterID
                        Left join FAS_Users as U On U.UserID = P.UserID
                        where PCBID = " & GetPCB_SNID(1)
                        PackedSN = New ArrayList(SelectListString(SQL))
                        If PackedSN.Count = 0 Then
                            Mess = ""
                        ElseIf PackedSN.Count <> 0 And (LOTID = 20176 Or LOTID = 20212) And LOTID <> PackedSN(8) Then
                            RunCommand($"delete [FAS].[dbo].[Ct_PackingTable] where pcbid = {GetPCB_SNID(1)}")
                            PackedSN = New ArrayList(SelectListString(SQL))
                            Mess = ""
                        ElseIf PackedSN.Count <> 0 Then
                            Mess = "Плата " & SerialTextBox.Text & " уже упакована!" & vbCrLf &
                            "Литера - " & PackedSN(2) & " Паллет - " & PackedSN(3) & " Групповая - " & PackedSN(4) & " № - " & PackedSN(5) & vbCrLf &
                            "Дата - " & PackedSN(6)
                        End If


                        'Mess = If(PackedSN.Count <> 0, "Плата " & SerialTextBox.Text & " уже упакована!" & vbCrLf &
                        '    "Литера - " & PackedSN(2) & " Паллет - " & PackedSN(3) & " Групповая - " & PackedSN(4) & " № - " & PackedSN(5) & vbCrLf &
                        '    "Дата - " & PackedSN(6), "")
                        Res = (PackedSN.Count = 0)
                    Case 1
                        SQL = "Use FAS SELECT L.Content,S.SN,Lit.LiterName + cast ([LiterIndex] as nvarchar),[PalletNum],[BoxNum],[UnitNum],[PackingDate],U.UserName
                        FROM [FAS].[dbo].[Ct_PackingTable] as P
                        left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = P.PCBID
                        Left join Ct_FASSN_reg as S On S.ID = P.SNID
                        Left join FAS_Liter as Lit On Lit.ID = P.LiterID
                        Left join FAS_Users as U On U.UserID = P.UserID
                        where SNID = " & GetPCB_SNID(1)
                        PackedSN = New ArrayList(SelectListString(SQL))
                        If PackedSN.Count = 0 Then
                            Mess = ""
                        ElseIf PackedSN.Count <> 0 And (LOTID = 20176 Or LOTID = 20212) Then
                            RunCommand($"delete [FAS].[dbo].[Ct_PackingTable] where snid = {GetPCB_SNID(1)}")
                            PackedSN = New ArrayList(SelectListString(SQL))
                            Mess = ""
                        ElseIf PackedSN.Count <> 0 Then
                            Mess = "Плата " & SerialTextBox.Text & " уже упакована!" & vbCrLf &
                            "Литера - " & PackedSN(2) & " Паллет - " & PackedSN(3) & " Групповая - " & PackedSN(4) & " № - " & PackedSN(5) & vbCrLf &
                            "Дата - " & PackedSN(6)
                        End If

                        'Mess = If(PackedSN.Count <> 0, "Плата " & SerialTextBox.Text & " уже упакована!" & vbCrLf &
                        '    "Литера - " & PackedSN(2) & " Паллет - " & PackedSN(3) & " Групповая - " & PackedSN(4) & " № - " & PackedSN(5) & vbCrLf &
                        '    "Дата - " & PackedSN(6), "")
                        Res = (PackedSN.Count = 0)
                End Select
            End If
            Col = If(Res = False, Color.Red, Color.Green)
            PrintLabel(Controllabel, Mess, 12, 193, Col)
            SNTBEnabled(Res)
            Return Res
        Else
            Return False
        End If
    End Function
#End Region
#Region "'5. Запись в базу данных и в Рабочий грид"
    Private Sub WriteDB(SMTSN As String, FASSN As String)
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
        'If LOTInfo(20) = 8 Then
        '    TableColumn = New ArrayList() From {UnitCounter, SMTSN, FASSN, $"F8CC6E0{Hex(Mid(FASSN, 5))}", Litera, PalletNumber, BoxNumber, Date.Now}
        'Else
        TableColumn = New ArrayList() From {UnitCounter, SMTSN, FASSN, Litera, PalletNumber, BoxNumber, Date.Now}
        'End If

        Dim row = ds.Tables(0).NewRow()
        Dim i = 0
        For Each item In TableColumn
            row.Item(i) = item
            i += 1
        Next
        ds.Tables(0).Rows.Add(row)
        DG_Packing.DataSource = ds
        DG_Packing.Sort(DG_Packing.Columns(0), System.ComponentModel.ListSortDirection.Descending)
        RunCommand($" use FAS
                insert into [FAS].[dbo].[Ct_PackingTable] (PCBID,SNID,LOTID, LiterID,LiterIndex,PalletNum,BoxNum,UnitNum,PackingDate,UserID)values
                ({If(PCBID = 0, "Null", PCBID)},{If(SNID = 0, "Null", SNID)},{LOTID},{PCInfo(8)},{LOTInfo(17)},{PalletNumber},{BoxNumber},{UnitCounter},current_timestamp,{UserInfo(0)})
                update [FAS].[dbo].[FAS_PackingCounter] set [PalletCounter] = {PalletNumber},[BoxCounter] = {BoxNumber},[UnitCounter] = {UnitCounter} 
                where [LineID] = {PCInfo(2)} and [LOTID] = {LOTID}")
        If LOTID = 20112 Or LOTID = 20130 Or LOTID = 20142 Then
            Print(SelectString($"select content from SMDCOMPONETS.dbo.LazerBase where IDLaser  = {PCBID}"), CB_DefaultPrinter.Text)
        End If
        SNBufer = New ArrayList
        ShiftCounter(2)
        RunCommand($"insert into [FAS].[dbo].[Ct_OperLog] ([PCBID],[LOTID],[StepID],[TestResultID],[StepDate],
                    [StepByID],[LineID],[SNID])values
                    ({If(PCBID = 0, "Null", PCBID)},{LOTID},6,2,CURRENT_TIMESTAMP,{UserInfo(0)},{PCInfo(2)},{If(SNID = 0, "Null", SNID)})")
    End Sub
#End Region
#Region "'6. Счетчик продукции"
    Private Sub ShiftCounter(StepRes As Integer)
        ShiftCounterInfo(1) += 1
        ShiftCounterInfo(2) += 1
        Label_ShiftCounter.Text = ShiftCounterInfo(1)
        LB_LOTCounter.Text = ShiftCounterInfo(2)
        ShiftCounterUpdateCT(PCInfo(4), PCInfo(0), ShiftCounterInfo(0), ShiftCounterInfo(1), ShiftCounterInfo(2))
    End Sub
#End Region
#Region "'7. Деактивация ввода серийника"
    Private Sub SNTBEnabled(Res As Boolean)
        SerialTextBox.Enabled = Res
        BT_Pause.Focus()
    End Sub
#End Region
#Region "'8. Печать SN Aquarius"
    Private Sub BT_PrintSet_Click(sender As Object, e As EventArgs) Handles BT_PrintSet.Click
        GB_Printers.Location = New Point(670, 370)
        GB_Printers.Visible = True
    End Sub
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
        GB_Printers.Visible = False
    End Sub

    Private Function Print(SN As String, DefPrt As String)
        If DefPrt <> "" Then
            RawPrinterHelper.SendStringToPrinter(DefPrt, GetLabelContent(SN))
            Return True
        Else
            MsgBox("Принтер не выбран или не подключен")
            Return False
        End If
    End Function

    Private Function GetLabelContent(SN As String)
        Dim x = Num_X.Value, y = Num_Y.Value
        Dim count As Integer
        If CB_Reprint.Checked = False And CB_Technik_Reprint.Checked = False Then
            count = 2
        ElseIf CB_Technik_Reprint.Checked = True Or CB_Reprint.Checked = True Then
            count = NumReprintCount.Value
        End If
        Dim Str As String
        If LOTID = 20142 Then
            Str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW354
^LL0150
^LS0
^BY2,3,57^FT{21 + x},{77 + y}^BCN,,Y,N
^FD>:{Mid(SN, 1, 7)}>5{Mid(SN, 8)}^FS
^PQ{count},0,1,Y^XZ
"
        Else
            If CInt(Mid(SN, 7, 6)) <= 5099 Then
                Str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW354
^LL0150
^LS0
^BY2,3,57^FT{32 + x},{77 + y}^BCN,,Y,N
^FD>:{Mid(SN, 1, 6)}>5{Mid(SN, 7)}^FS
^PQ{count},0,1,Y^XZ
"
            ElseIf CInt(Mid(SN, 7, 6)) >= 5100 Then
                Str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW354
^LL0150
^LS0
^BY2,3,57^FT{32 + x},{77 + y}^BCN,,Y,N
^FD>:{Mid(SN, 1, 6)}>5{Mid(SN, 7)}^FS
^FT{314 + x},{94 + y}^A0N,17,16^FH\^FD2^FS
^PQ{count},0,1,Y^XZ
"
            End If
        End If
        Return Str
    End Function

    Private Sub CB_Reprint_CheckedChanged(sender As Object, e As EventArgs) Handles CB_Reprint.CheckedChanged
        SerialTextBox.Focus()
    End Sub

    Private Sub CB_Technik_Reprint_CheckedChanged(sender As Object, e As EventArgs) Handles CB_Technik_Reprint.CheckedChanged
        GB_ReprinUser.Visible = True
    End Sub

    Private Sub TB_RFID_ReprintUser_KeyDown(sender As Object, e As KeyEventArgs) Handles TB_RFID_ReprintUser.KeyDown
        If e.KeyCode = Keys.Enter And TB_RFIDIn.TextLength = 10 Then
            Dim Usr As Integer = (SelectInt($"USE FAS SELECT [UsersGroupID] FROM [FAS].[dbo].[FAS_Users] where [RFID] = '{TB_RFID_ReprintUser.Text}' and IsActiv = 1"))
            If Usr = 3 Or Usr = 1 Then
                TB_RFID_ReprintUser.Clear()
                GB_ReprinUser.Visible = False
                GB_Printers.Visible = False
                SerialTextBox.Focus()
            Else
                PrintLabel(Controllabel, $"Пользователь не является {vbCrLf}техником технологом!", 12, 193, Color.Red)
                SerialTextBox.Enabled = False
            End If
        End If

    End Sub



#End Region
End Class








