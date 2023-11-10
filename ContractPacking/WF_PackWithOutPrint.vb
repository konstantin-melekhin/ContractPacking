Imports System.Deployment.Application
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
    Dim StartStep, PreStep, NextStep, Litera, WNetto, WBrutto As String
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
        Controllabel.Text = ""
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
                            "HexSN = " & LOTInfo(18) &
                            "FASNumFormat2 = " & LOTInfo(19) &
                            "CustomerID = " & LOTInfo(20)
        Litera = If(LOTInfo(17) = 0, PCInfo(9), (PCInfo(9) & LOTInfo(17)))
        'Определить стартовый шаг, текущий и последующий
        Dim StepSequence_text As String
        If LOTID = 20279 And PCInfo(8) = 2 Then
            StepSequence_text = "1D06"
            Label25.Text = "РДВ без теста"
            Label25.Visible = True
        Else
            StepSequence_text = LOTInfo(14)
        End If
        'StepSequence = New String(Len(LOTInfo(14)) / 2 - 1) {}
        'For i = 0 To Len(LOTInfo(14)) - 1 Step 2
        '    Dim J As Integer
        '    StepSequence(J) = Mid(LOTInfo(14), i + 1, 2)
        '    J += 1
        'Next
        StepSequence = New String(Len(StepSequence_text) / 2 - 1) {}
        For i = 0 To Len(StepSequence_text) - 1 Step 2
            Dim J As Integer
            StepSequence(J) = Mid(StepSequence_text, i + 1, 2)
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
                'If SNFormat(1) = 1 Then 'была проверка по бумажному коду, теперь по номеру платы pcbid
                If SNFormat(1) = 2 Then
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
            If SelectListString($"use fas   select top 1 [Result] FROM [FAS].[dbo].[Pc_Testing_Results] where Result = 1 And [PCBID] = 
                                        (select IDLaser from SMDCOMPONETS.dbo.LazerBase where content ='{Sn}')").Count = 1 Then
                res = True
            ElseIf SelectListString($"use fas   select top 1 [Result] FROM [FAS].[dbo].[Pc_Testing_Results] where Result = 1 And MAC = '{Sn}'").Count = 1 Then
                res = True
            ElseIf SelectString($"use fas select СustomersID  from Contract_LOT where id = {LOTID}") = 8 Then
                'If SelectListString($"use fas   select top 1 [Result] FROM [FAS].[dbo].[Pc_Testing_Results] where Result = 1 and MAC = 
                '    '{SelectString($"use fas select MAC1 from Depo_SN_MAC where SN = '{Sn}'")}'").Count = 1 Then
                If SelectListString($"use fas   select top 1 [Result] FROM [FAS].[dbo].[Pc_Testing_Results] where Result = 1 and MAC = 
                    '{SelectString($"use fas select ID from Ct_FASSN_reg where SN = '
                    {SelectString($"use fas select SN from Depo_SN_MAC where MAC1 = '{Sn}' or MAC1 = '{Sn}' ")}")}'").Count = 1 Then
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
        Try
            If LOTID = 20428 And SelectString($"Select [Resistor] FROM [QA].[dbo].[TempDepoSP65_resistors] where [PCBSN] = '{SerialTextBox.Text}'") = False Then
                'If PCBID > 26577196 And PCBID < 27199354 Then
                Res(0) = False
                Mess = $"Плата не прошла замену резистора!{vbCrLf}Заблокировать плату!"
            End If
        Catch ex As Exception

        End Try


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
                            Res = If((PCBStepRes(0) = 62 Or (PCBStepRes(0) = 40)), True, False)
                            Mess = If(Res = False, $"Первая плата {SerialTextBox.Text & vbCrLf}не прошла контроль в ОТК!", "")
                        Else
                            If ((PCBStepRes(0) = PreStepID) Or (PCBStepRes(0) = 62) Or (PCBStepRes(0) = 40)) And PCBStepRes(1) = 2 Then
                                Res = True
                            ElseIf PCBStepRes(0) = 6 And PCBStepRes(1) = 2 Then
                                Res = True
                                Mess = ""
                            Else
                                Res = False
                                Mess = $"Плата {SerialTextBox.Text & vbCrLf}имеет не верный предыдущий шаг! Верните на тест!"
                            End If
                        End If
                    ElseIf PCBStepRes.Count = 0 And LOTID = 20424 Then
                        Res = True
                        Mess = ""
                    ElseIf PCBStepRes.Count = 0 Then
                        Mess = $"У платы {SerialTextBox.Text & vbCrLf}не найден предыдущий шаг!"
                    End If
                Case 1
                    'Res = True
                    Dim PCBStepRes As New ArrayList(SelectListString($"Use FAS
                select 
                tt.StepID,tt.TestResultID, tt.StepDate ,tt.SNID
                from  (SELECT *, ROW_NUMBER() over(partition by snid order by ID desc) num 
                FROM [FAS].[dbo].[Ct_OperLog] 
                where SNID  ={GetPCB_SNID(1)}) tt
                where  tt.num = 1"))
                    If PCBStepRes.Count <> 0 Then
                        If ((PCBStepRes(0) = PreStepID) Or (PCBStepRes(0) = 62) Or (PCBStepRes(0) = 40)) And PCBStepRes(1) = 2 Then
                            Res = True
                        ElseIf PCBStepRes(0) = 6 And PCBStepRes(1) = 2 Then
                            Res = True
                            Mess = ""
                        Else
                            Res = False
                            Mess = $"Плата {SerialTextBox.Text & vbCrLf}имеет не верный предыдущий шаг! Верните на тест!"
                        End If
                    ElseIf StartstepID = PCInfo(6) And LOTInfo(12) = True Then
                        Res = True
                    ElseIf LOTInfo(12) = False Then
                        Res = True
                    End If
                    'Mess = $"У платы {SerialTextBox.Text & vbCrLf}не найден предыдущий шаг!"
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
                insert into [FAS].[dbo].[Ct_PackingTable] (PCBID,SNID,LOTID, LiterID,LiterIndex,PalletNum,BoxNum,UnitNum,PackingDate,UserID,Descriptions)values
                ({If(PCBID = 0, "Null", PCBID)},{If(SNID = 0, "Null", SNID)},{LOTID},{PCInfo(8)},{LOTInfo(17)},{PalletNumber},{BoxNumber},{UnitCounter},current_timestamp,
                {UserInfo(0)},{If((LOTID = 20279 And PCInfo(8) = 2), "'RDW_SP20_Без_Теста'", "NULL")})
                update [FAS].[dbo].[FAS_PackingCounter] set [PalletCounter] = {PalletNumber},[BoxCounter] = {BoxNumber},[UnitCounter] = {UnitCounter} 
                where [LineID] = {PCInfo(2)} and [LOTID] = {LOTID}")
        If LOTID = 20112 Or LOTID = 20130 Or LOTID = 20142 Then
            Print(SelectString($"select content from SMDCOMPONETS.dbo.LazerBase where IDLaser  = {PCBID}"), CB_DefaultPrinter.Text)
        End If
        SNBufer = New ArrayList
        ShiftCounter(2)
        'печать групповой этикетки 
        If UnitCounter = LOTInfo(15) And (LOTInfo(20) = 8) Then '- -And LOTID = 20258 Or LOTID = 20263
            SerchBoxForPrint(LOTID, BoxNumber, PCInfo(8), LOTInfo(17))
            SNArray = GetSNFromGrid()
            Print(SNArray, CB_DefaultPrinter.Text, Num_X.Value, Num_Y.Value, LOTInfo(15))
        End If
        RunCommand($"insert into [FAS].[dbo].[Ct_OperLog] ([PCBID],[LOTID],[StepID],[TestResultID],[StepDate],
                    [StepByID],[LineID],[SNID])values
                    ({If(PCBID = 0, "Null", PCBID)},{LOTID},6,2,CURRENT_TIMESTAMP,{UserInfo(0)},{PCInfo(2)},{If(SNID = 0, "Null", SNID)})")
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
            SNArrayTemp.Add(Mid(LOTInfo(1), 1, 5) & "_" & DG_SelectedBox.Item(2, 0).Value & "_;" & DG_SelectedBox.Item(3, 0).Value)
            For i = 0 To DG_SelectedBox.Rows.Count - 1
                SNArrayTemp.Add(DG_SelectedBox.Item(1, i).Value)
            Next
        Else
            PrintLabel(Controllabel, "Корбка еще не закрыта!", 12, 193, Color.Red)
        End If
        Return SNArrayTemp
    End Function
    Private Function Print(SNArray As ArrayList, DefPrt As String, x As Integer, y As Integer, couninbox As Integer)
        If DefPrt <> "" Then
            RawPrinterHelper.SendStringToPrinter(DefPrt, GetGroupLabel(SNArray, x, y, couninbox))
            CB_ManualPrint.Checked = False
            Return True
        Else
            MsgBox("Принтер не выбран или не подключен")
            Return False
        End If
    End Function
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
        'GB_Printers.Location = New Point(670, 370)
        'GB_Printers.Visible = True
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
                    Print(SNArray, CB_DefaultPrinter.Text, Num_X.Value, Num_Y.Value, LOTInfo(15))
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
                Print(SNArray, CB_DefaultPrinter.Text, Num_X.Value, Num_Y.Value, LOTInfo(15))
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
#Region " 10. Групповая этикетка"
    Private Function GetGroupLabel(sn As ArrayList, x As Integer, y As Integer, CountInBox As Integer)
        Dim str As String
        Dim snp As New ArrayList
        For i = 0 To sn.Count - 1
            If i = 0 Then
                snp.Add(sn(0))
            Else
                snp.Add(SelectString($" SELECT [SN] FROM [FAS].[dbo].[Depo_SN_MAC] where [MAC1] = '{sn(i)}' or [MAC2] = '{sn(i)}'"))
            End If
        Next
        Dim ComModel As String = SelectString($"
            select [Commercial_Name] FROM [FAS].[dbo].[FAS_Models] where [ModelName] = '{LOTInfo(0)}'")

        Select Case CountInBox

            Case 12 'Редактируемая модель и ДАЦН
                str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO448,0^GFA,07168,07168,00004,:Z64:
eJztyjENAAAIBLF3gH+3KCAwMfWS25rcq2We53me53me53me53me53me53me52f/UQPavkSt:03A7
^FO32,256^GFA,04224,04224,00012,:Z64:
eJztljFuwzAMRSWoqEYfwUfx0ayiQ6/lTh57hBrokDXZUiAtm0n/CbVTOw6axZyeDJv8pChazm12rUWz7ia8h89vsbekRTuI6xNF8INOi+Y22ow+/7YH5/L7jy7IT6rFHbQNiHUAf4mDqSjejgpW1AQyI1gS/rUOhVX4tMn6q2R5H6sn5Rifzcgp87HJ3A+VuMsJT3H4mOCD/PhP+fcnxXUmPa7FXjTSf64PmhgtOcu4Xw32F/5d0QMKEKAzSL8LyuvsXgHmcCDDT+EfcamHOl0L/cyL+S419lLLWk3Wh5yyBOQSBzm9UJM0yqiP39fQowMTV/Bi23rpl20z+c62ndnLVtTnNWtz9iad1mcO1nejvBPHHvwyhxUr9uKwe0fcOecUvHjmY09XnQuE5d2Sz23QgrOxxqUzjN8ni+e12LNXMRMiXo+8usL70hx90W4Lf6jbP+IaWzMSyvEAmXfi2+Wy2Sr7AcFE+uQ=:40B8
^FO448,608^GFA,06528,06528,00012,:Z64:
eJztmD1u20AQRocg4E0RYF2qMHZukCNY13JhiAxc5Bi+CoEUuQaPYCCFbcDIRoUx8xahAq1Ixw7EqR5U7PzsN7NDiZy7PRi135+Mw88X50fnmH9NsubkfB2NUzqdNeeTOYKrTcF5NGwzapVRq3ygPiO4v8bxWsWRjHOK8+GX8TDOBvEzryLfWts6NkWtDtVnulaKXLT3Qw/XJE0z6hOGW8TjgtAZXG1FfS4QD+JE78Ss03yPOqhzOoqhPXC838HXMfpxrtcP7nRGX6uHJjrI9O+5N2bPSuc57hNGbHn69845FLNuBMMvXLEktTmGQm6VzbnOrqNtlg55R8VlL8OLxfYB7FIa4420xknw1lM/A2bFiLyep9/3FpoR6mQ7OneDIbVEvSl7HL6KGBgbYk4SjJkjcz/G/um9t6NzxGznMN2SMT85S3fe13ILEd/EKv4Iml9nwt+t0tcGnMCKt1sVz9E3l1X71d/xtu+sZ0Pv+tQ++Jl4x+nr6j+p1WEecCbaNKAFG+w80nERQf8G5rVQbEu9NcWePBqv3xGvVtTnfL8jPssX94X7qt1LCw1AJ/U7qvO876O3tUvsJBvsXUk6Y+VMuAP/AD9yB2NvjsblLEIQrANWgHebP4xn/Vb6w86t19b3+jRbrG48cw6/T30+gaN4v8ReTOeKSN+C976sv+IguykOD40JMbwE09t+tTFuMvorY550eTAu/meDzmvsNxZXR/w=:5216
^FO96,928^GFA,06656,06656,00008,:Z64:
eJzlmD9u2zAUxqkKMJeCXDsIYo9QoEuCGvaVAnRIgBqRT+C5Q9GzKBfICTJo61YkUwNUtVrofZ8NsqGkKI7jum/5ATb5/vGRj5RSRyxvwFmEWff0s4Es1t2MyaUgacqWaXPbUjf3QlO3NJOFUE/BDDQtrTWYJ0zXwqSGnftuN2Jim3UnNcbppTAly4EG5jIhQdwp425q6JV4rcpbGmU88neO4zzqoV7aiUlyi7zVfh5tsxLaa7Gbfhcma/hBykJq1JVewi/4qdSk0/6jxaKuHDgP6u0cdX1ufH5cgRj3HjxloVz4dp5a933zQ9GV0KFg5/CrABfYt2fmYb4DTxC/QyEUEfbJvvL8lwzMa0FW8Jf61z4ZR4H9UCwDPbFz67LHz93Is58zB25/I3vfb6XQoi7CfTeVc1Vd5NAT2Xf8fzrz51Mf9esq4sgjJSk99q2fxTh9JUzvMP8XWIAOZF5iC6wroZX4jEOfRtxmJnkwJ+gnIT9LvtIbyW+KvpXwWOI+pT+hvFTf3BT+SIbyv95/dpbHV/JzhfW8g38/fT9zV7XMnMzKTDcd5sXYK+hLSSN2t/UptKUsjFWynjnqMUN9xmgwjvOoh3pZP7Sr5qFjB5KvXe+jHtlXf/tn7tE43tK69KiNcDLxyd/D8dRD6cuzQrxKs89Wwob+QE81UN+OJGF7pv3g2uiwD3IrzHQ3j/f88CW9Qr/6Br9+wC/003wGv3IQB0aMx563bCSPPS/bC7LQNU1AP94+v1zNerpWD83/c0FiYnjR9OxvOVL4/uZ7PST/jwkbkfsinK98fuL7OuRX4Qdw7Duk7521wDjej3lg8wDnxXbsd8Ghfgy9LzzVj71/B8GDNQveo5vvQTeB/gjHxn1o7+KXroO3r4WxOHvj5cUCceuq297Bn9dHJL8BjHhakQ==:B5A8
^FO128,768^GFA,19840,19840,00020,:Z64:
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:AB0C
^FO256,832^GFA,22272,22272,00024,:Z64:
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:FE9F
^FO288,192^GFA,01536,01536,00008,:Z64:
eJzVkzEOwjAMRRMFYabmAlVzBUYmuBZbc7QepRJDVzaKBA0S/h6I6qZs4OUNsb/tX9eYf4+KmMHHWcq7FvbKdA8mTUyfQM9siPVqN0959+gr9aIn+nZUBqmRf8z6g+GGtEtcZEjLVNqqLOmV6vPYObwrPhb9BB181fy0PfIHpoc/wkbzcWBWQ9Z37XdEFL/DHomHjGfUjxlxp6Fb7ru2f0Ce75lO9kkiUKhX7unbOdS7k70n0fnUM7gLQ/Nz/Mh/vTHmXbfFnBTZV+paZs/70J317BNztx3zhPVkXYxFCXUKV8YLJWWKCw==:18CA
^FO384,192^GFA,01280,01280,00008,:Z64:
eJzV0jEOwjAMBdBWkfAWcwCIr1EkClfvxgZX6BGQWJBAhOHHQ6om7oBA/OUNiR3FSdP8ezYeHmReXS/FjZDukJ9QXjAEuGfY0by6HiSv137a313z8zvDY6xr1X/7HI0bIJ+hnHL75G7qBW6TS+c4jXWfdlim1edTWa9g6X+Z/yxJaU6l+fz4XdoR+9wNdfRAH4qQBffznPRUlWOsauQNZ+xWLw==:7A01
^FO320,192^GFA,01536,01536,00008,:Z64:
eJzVk01uwjAQhRNZqneeLlmAzTFACvHFkOAgHIZdd+UGVW5QJDaRGuEu3hukWDjpqmrf5lvM/3hcVf9fSwe24TnVXpLpQNuDMoDhDnoPNgJu7HOq3YdxvObT/OY6rr+ZYUzTnIv/7TprstG8R5JzR91HouFwBgMc6oRFCRn7PdghY6w8869H9VS63/wdI6l3UXrHBal3IwmJYoElsY+FYAwfBhJ9uBZjON6NsP6D7MOeYLcf8DefiK+1/EHvtpts58d6fQFL+5m9d9Kyv/zeVeZM/3fGv425I7c5L+DqktWd+WeZ6g5+5oY4+4U8NoESMJ8T0tlJSkqT/JP6BjgVZ8c=:FB51
^FO288,96^GFA,00512,00512,00008,:Z64:
eJxjYKA5YIPS/A3YaXbs2mQI0PL/G/DShPTjAoTMZWDGT9NcP1UBAGhQIJM=:661F
^FO320,64^GFA,00512,00512,00008,:Z64:
eJxjYBgygI9Mmv//f7w0LsDBwAimeRj4IeoZ7CF0A1T/QSj9+Q+YZv79AKLxfwOErocaJA+l2SlzD00AAHGkGpo=:FC27
^FO384,64^GFA,00512,00512,00008,:Z64:
eJxjYBgigI9Mmv//f7w0LsDBwAimeRj4IeoZ7CF0A1T/QSj9+Q+YZv79AKLxfwOErocaJA+l2SlzD40AAKbWGpo=:D4A7
^FO0,1344^GFA,04992,04992,00012,:Z64:
eJztl0FuxCAMRZ1mwTI3qK/QE4Sr9CBRmKPlKDlCllRF0C5G9kcCdTIJ0lTKX71FwAZ/AyE6LJfSrcTpJPassR5jGUrBLsAb5BzL+X8r268y8+aEh9UKm8VoYOqACbksyMEAM7CtcH3/g0wfp1E4TO/CfuIayzx+RNb1equxJmROVOJ50D3/MMrUa55EGz2rs/xWY5oh2EncPOdD0rrzabktMjs4gNIKDHZw8P2QvHCX1FcE3qO+wvBNn/eXxk0bsA6FsIQ7m+V8YE9MVq8B+AFhG8U3nSVaYQa2UXvc5SxxHfR7yHp/Hwc4K6ZRc3CjU4b9t3AmM5hgADaZB1Zh0imrCg24df+2yLmmT+qFZ9J6BWLk27McyRbZQfHcon6oeaPuE/WGgV7OfKLLOqQmdcf5V2UH5w9Dvxs4NjpYLzmoNty/2QBq69u9urz3orrepXeV36XXnbJDl5fu2veP81/8gE/y/C3tgWOFU5HzF7cF5gr/pd8/B1l7E4bb+iW48XovHdIPLfrUFw==:0528
^FO0,960^GFA,02304,02304,00012,:Z64:
eJzt00EOgyAQBdAxLrrkAk28SBOOBjerR+EYLAxTbCzzMRIDrakL/+olBhxnHKIrWR5gUzDbfZfO3ttLS+k4xh5nIuXkbf/38d/7XRRcNCx+31/rypqHE/mUM4KzCmzE/cbo5uAugI24j7XJSfYL55pHMImfYq/Fo3gicQCzAwdwdn8yaWb38Y05pAem8F0d9lmDsT/Yt6yf2OdShkKjq/fFUps9/Ku/cmw52OMutO7FahbFIZmdfq59ZTMvN0hSpA==:61AD
^FT85,1329^A0B,50,50^FH\^FD{LOTInfo(0)}^FS
^BY2,3,83^FT190,630^BCB,,N,N
^FD>:{snp(0).Split(";")(0) & Mid(Integer.Parse(snp(0).Split(";")(1)).ToString("00000"), 1, 1)}>5{Mid(Integer.Parse(snp(0).Split(";")(1)).ToString("00000"), 2)}^FS
^FT248,630^A0B,50,50^FH\^FD{sn(0).Split(";")(0) & Integer.Parse(sn(0).Split(";")(1)).ToString("00000")}^FS
^FT48,290^BQN,2,5
^FH\^FDLA,{ snp(1)}\0D\ {snp(2)}\0D\ {snp(3)}\0D\ {snp(4)}\0D\ {snp(5)}\0D\ {snp(6)}\0D\ {snp(7)}\0D\ {snp(8)}\0D\ {snp(9)}\0D\ {snp(10)}\0D\ {snp(11)}\0D\ {snp(12)}^FS
^BY4,3,44^FT573,1719^BCB,,Y,N
^FD>;{snp(1)}^FS
^BY4,3,44^FT681,1719^BCB,,Y,N
^FD>;{snp(2)}^FS
^BY4,3,44^FT789,1719^BCB,,Y,N
^FD>;{snp(3)}^FS
^BY4,3,44^FT573,1259^BCB,,Y,N
^FD>;{snp(4)}^FS
^BY4,3,44^FT681,1259^BCB,,Y,N
^FD>;{snp(5)}^FS
^BY4,3,44^FT789,1259^BCB,,Y,N
^FD>;{snp(6)}^FS
^BY4,3,44^FT573,841^BCB,,Y,N
^FD>;{snp(7)}^FS
^BY4,3,44^FT681,841^BCB,,Y,N
^FD>;{snp(8)}^FS
^BY4,3,44^FT789,841^BCB,,Y,N
^FD>;{snp(9)}^FS
^BY4,3,44^FT573,422^BCB,,Y,N
^FD>;{snp(10)}^FS
^BY4,3,44^FT681,422^BCB,,Y,N
^FD>;{snp(11)}^FS
^BY4,3,44^FT789,422^BCB,,Y,N
^FD>;{snp(12)}^FS
^FO273,42^GB167,355,3^FS
^FT325,204^A0B,33,33^FH\^FD{LOTInfo(15)}^FS
^FT372,204^A0B,33,33^FH\^FD{WBrutto}^FS
^FT420,204^A0B,33,33^FH\^FD{WNetto}^FS
^FT85,973^A0B,50,50^FH\^FD{ComModel}^FS
^PQ1,0,1,Y^XZ

"
            Case 24
                str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO448,0^GFA,07168,07168,00004,:Z64:
eJztyjENAAAIBLF3gH+3KCAwMfWS25rcq2We53me53me53me53me53me53me52f/UQPavkSt:03A7
^FO32,320^GFA,03840,03840,00012,:Z64:
eJztljFuhDAQRW05ikuOwFE4GkQpci1SUeYIQUqx7W63kTaZbOX/rMAGAgopmOoZwcyf8Xiwc//Dolm7Ch/h81PsrdGi7sXlhSL4QatFtY42o8+f7c659P69C/LTlOIW2nrEOoE/xMFUFG9nBctqApkRLAl/WofMCnxaJf1FY2kfiwflGB/NyE3ic5W46wtxmxIe4/A2wif58e/y7y+K60x6XI29qKT/Wh80MVpyknG/Kuwv/LusBxQgQGeQfheU19W9AkzhQIafzD/iUg91uhr6mRfznWvspZq1Gq0PuUkSkEvs5fRGTZpBRn38sYQeHZi4gGfb3kvfbJ/JG9t+Zm9bVp/npM3Zi3RalzhY1w7yQRw78NMUVqzYicPhFXGnnFPw7JmPPV10LhCWd0s+t14LzsYSl84wfJ/Mnpdiz17FTIh4PfLqCu9zc/RZu838oe7/iN/YkpGQjwfI3IjXy2UD+wLHKPrk:C179
^FO448,608^GFA,06528,06528,00012,:Z64:
eJztmD1u20AQRocg4E0RYF2qMHZukCNY13JhiAxc5Bi+CoEUuQaPYCCFbcDIRoUx8xahAq1Ixw7EqR5U7PzsN7NDiZy7PRi135+Mw88X50fnmH9NsubkfB2NUzqdNeeTOYKrTcF5NGwzapVRq3ygPiO4v8bxWsWRjHOK8+GX8TDOBvEzryLfWts6NkWtDtVnulaKXLT3Qw/XJE0z6hOGW8TjgtAZXG1FfS4QD+JE78Ss03yPOqhzOoqhPXC838HXMfpxrtcP7nRGX6uHJjrI9O+5N2bPSuc57hNGbHn69845FLNuBMMvXLEktTmGQm6VzbnOrqNtlg55R8VlL8OLxfYB7FIa4420xknw1lM/A2bFiLyep9/3FpoR6mQ7OneDIbVEvSl7HL6KGBgbYk4SjJkjcz/G/um9t6NzxGznMN2SMT85S3fe13ILEd/EKv4Iml9nwt+t0tcGnMCKt1sVz9E3l1X71d/xtu+sZ0Pv+tQ++Jl4x+nr6j+p1WEecCbaNKAFG+w80nERQf8G5rVQbEu9NcWePBqv3xGvVtTnfL8jPssX94X7qt1LCw1AJ/U7qvO876O3tUvsJBvsXUk6Y+VMuAP/AD9yB2NvjsblLEIQrANWgHebP4xn/Vb6w86t19b3+jRbrG48cw6/T30+gaN4v8ReTOeKSN+C976sv+IguykOD40JMbwE09t+tTFuMvorY550eTAu/meDzmvsNxZXR/w=:5216
^FO96,928^GFA,06400,06400,00008,:Z64:
eJzlmD9u2zAUxqkKMJeCXDsIYo9QoEuKGvaVAnRIgBqRT+C5Q9GzKBfICTJo61YkUwNUtVrofZ8NsqGkyK7jum/5ATb5/vGRj5RSB5RX4CzCrHv6+UAW627G5EqQNGXLtLlrqZsHoalbmslCqKdgBpqW1hrME6ZrYVLDzkO3GzGxzbqTGuP0UpiS5UADc5mQIO6UcTc19Eq8VuUtjTIe+TvHcR71UC/txCS5Q95qP4+2WQntjdhNvwmTNfwgZSE16kov4Rf8VGrSaf/JYlFXDpwH9XaBur4wPj+sQIx7C75joVz6dnat+775oehK6FCwc/hVgAvs23PzON+AZ4jfoRCKCPvkUHn+QwbmtSAr+Ev9a5+Mo8B+KJaBnti5ddXj537kr58zR25/Iwffb6XQoi7CfTeVc1Vd5tAT2Xf8fzrz51Mf9esq4sgTJSk99q2fxTh9LUzvMf8nWIAOZF5iC6wroZX4jEOfRtxmJnkwZ+gnIT9JvtJbyW+KvpXwWOI+pT+hPFff3BT+SIbyv95/9pbHF/JzhfW8h38/fD9zV7XMnMzKTDcd5sXYK+hLSSN2t/UptKUsjFWynjnqMUN9xmgwjvOoh3pZP7Sr5qFjR5Kvfe+jHjlUf/tn7tE43tK69KiNcDLxyd/D8dRD6cuzQrxKs89Wwob+QE81UN+eJGF7pv3g2uiwD3IrzHQ3T/f88CW9Rr/6Cr++wy/003wGv3IQB0aMp563bCRPPS/bC7LQNU1AP94+v1zNerpRj83/fUFiYnjR9OxvOVL4/uZ7PST/jwkbkfssnK98fuT7OuQX4Xtw7Duk7521wDjej3lg8wDnxXbsd8Ghfgy9L+zqx8G/g+DBmgXv0c33oNtAf4Rj4z62d/Fz18Hrl8JYnL3x8mKBuHXVbe/oz+sd5BeXe1qR:0F3D
^FO128,736^GFA,19840,19840,00020,:Z64:
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:0584
^FO256,832^GFA,21504,21504,00024,:Z64:
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:94FB
^FO288,256^GFA,01536,01536,00008,:Z64:
eJzNkzEOwjAMRRMFYabmAlVzBUYmuBZbe7QepRJDVzaKBE0l/D0Q1U2RGOrlDbG/7V/XmH9FQczgm1nKuxb2znQvJo1MH0HPrIj1SjdPeffoK/WiJ/p2UAYpkX9O+oPhgbRbs8gQl6m0VZnTy9WncXB4V3zM+gk6+Kr5aTvk90wPf4SV5mPPLPqk79rviMh+hyMSTwmvqB8S4k5Du9x3bf+APN8xnewTRSBTr9zTr3Oodyd7j6LzrWdwF4bm59jIf70z5lO3x5zUsK/U1syO96En69k35q5b5gXryboYiyLqFG48Jt3Vigs=:52E3
^FO384,256^GFA,01280,01280,00008,:Z64:
eJzN0j0OwjAMBeBWkfAWcwCIr1Ekfq7ejQ2u0CMgsSCBCMOLh1RN3KEC3vINiR3FSdMslY2HR5lW10txA6QH5BeUNwwB7hl2NK2uB8nrtZ/2d7f8/M7wFOta9d8+R+N6yBco59xDcjf2CrfJuXMcx7pP28/T6rNU1itY+l/mP0tSmlNpPj9+l3bAPndHHT3RhyJkwf08Jz1V5Rir/mk+z9RWLw==:BB56
^FO320,256^GFA,01280,01280,00008,:Z64:
eJy9kz1uAjEQhXdlCXeelBRgcwyQCL4YEhyEw9DRwQ2ivUGQaFYCxSneGyRbeJciymu+Yv7H46b5O80cuAmvqfaaTAfaHpQHGH5A78G1gEv7mmr3IY/XfJrfXPP6yxHGNMyx+P+usyDXmndPcu6o+0g07I5ggEObsCghY78FO2SMjWf+RVZPpfst3zGSehe1d5ySejeSkChWWBP7mArG8OFBog+3wRiOdyOs/yT7sAfY7Rf8zTfiWy2/07vtBtt5Wx8TsLaf0XsnLfsr711ljvQ/M/6U85NclbyA80tRd+SfFWo7+Jkb4uwdeWwCJWA+J6Szg5SUBpnpFxKsZ8c=:351A
^FO288,128^GFA,00768,00768,00008,:Z64:
eJxjYBgxgA1K8zdgp9mxa5MhQMv/b8BLE9KPCxAyl4EZP01z/cMCAACuoSCT:BDD0
^FO320,128^GFA,00512,00512,00008,:Z64:
eJxjYCAZ8JFJ8///j5fGBTgYGME0DwM/RD2DPYRugOo/CKU//wHTzL8fQDT+b4DQ9VCD5KE0O2XuGVYAAOoQGpo=:73BE
^FO384,128^GFA,00512,00512,00008,:Z64:
eJxjYCAR8JFJ8///j5fGBTgYGME0DwM/RD2DPYRugOo/CKU//wHTzL8fQDT+b4DQ9VCD5KE0O2XuGWYAAB9RGpo=:F20E
^FO32,1312^GFA,04992,04992,00012,:Z64:
eJztlkFuhiAQhYfY1CVH8CKNXKUHMcXezKNwBJMuyoJI2fzMo4EYfiWxqW/1LRQfzJtBon8i732OxVm8jlTHitnqGdiAT5f3/82sv/KsDPOw6MhylsTqgAXtSYAHCayAdYHL528jOztFtvYt8mrHAivmCVkza/6uRVZblt3AZ/4umam3zMLQkzotbwUOG6Czub3nQ+JGUqd5myPDkQi/AEMc4Hka/Bq585wrguxRX2B4pk/6K64TvBlg8LMClzwfqJFM6jVQjfAQe/fCq7iPyApYOx6UPmGu9Qb9bpPer2PLs4LsxB62iWvnoY4aZrKCEAzAMsnAEpkgDgUJ24Ab928Lz0Ut9BrZUaxXeHcEVk+zI53jUDrIw+d+Nso54WxI7uU0JxzDI2pSa4gwzhaCbYVeZpYwNjrYL+EAhfs3eUE0zW217uxdU/d/6WNVQxndd0qF7iw9VjVUoT+TBxiZv/6lV2BX4C3HeE8FaWBV4D11nvfehOG2vgQ33u9l9QPbX7II:F2EB
^FO32,960^GFA,01920,01920,00012,:Z64:
eJzN1DEOhCAQBdAxFpZ7BK6xHdeyWj0aR6LcgvjFZFc+BmLAdcNUr5DJMMMoUhUjeUm7w7lzZ+VZV1YUPXzcaBFlpCHff99roSiRBuUvdWHNuiG3OKOOrMiUc0iMbjvJu0CmegZg32sN2A+3mufg/RPvV7Alz8FvCXZkGLIjR/nDb2YCzNcPwKXqj+7Vc5+nTH+4b1E/uc+50OlGl++LVNrSW/2Vfcvp/VvyUrsXh1lkh4STfh7991gBuFArsw==:BDA9
^BY2,3,83^FT190,637^BCB,,N,N
^FD>:{sn(0).Split(";")(0) & Mid(Integer.Parse(sn(0).Split(";")(1)).ToString("00000"), 1, 1)}>5{Mid(Integer.Parse(sn(0).Split(";")(1)).ToString("00000"), 2)}^FS
^FT248,630^A0B,50,50^FH\^FD{sn(0).Split(";")(0) & Integer.Parse(sn(0).Split(";")(1)).ToString("00000")}^FS
^FT48,315^BQN,2,4
^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}\0D\{sn(11)}\0D\{sn(12)}\0D\{sn(13)}\0D\{sn(14)}\0D\{sn(15)}\0D\{sn(16)}\0D\{sn(17)}\0D\{sn(18)}\0D\{sn(19)}\0D\{sn(20)}\0D\{sn(21)}\0D\{sn(22)}\0D\{sn(23)}\0D\{sn(24)}^FS
^BY4,3,44^FT573,1719^BCB,,Y,N
^FD>;{sn(1)}^FS
^BY4,3,44^FT681,1719^BCB,,Y,N
^FD>;{sn(2)}^FS
^BY4,3,44^FT789,1719^BCB,,Y,N
^FD>;{sn(3)}^FS
^BY4,3,44^FT897,1719^BCB,,Y,N
^FD>;{sn(4)}^FS
^BY4,3,44^FT1005,1719^BCB,,Y,N
^FD>;{sn(5)}^FS
^BY4,3,44^FT1113,1719^BCB,,Y,N
^FD>;{sn(6)}^FS
^BY4,3,44^FT573,1298^BCB,,Y,N
^FD>;{sn(7)}^FS
^BY4,3,44^FT681,1298^BCB,,Y,N
^FD>;{sn(8)}^FS
^BY4,3,44^FT789,1298^BCB,,Y,N
^FD>;{sn(9)}^FS
^BY4,3,44^FT897,1298^BCB,,Y,N
^FD>;{sn(10)}^FS
^BY4,3,44^FT1005,1298^BCB,,Y,N
^FD>;{sn(11)}^FS
^BY4,3,44^FT1113,1298^BCB,,Y,N
^FD>;{sn(12)}^FS
^BY4,3,44^FT573,876^BCB,,Y,N
^FD>;{sn(13)}^FS
^BY4,3,44^FT681,876^BCB,,Y,N
^FD>;{sn(14)}^FS
^BY4,3,44^FT789,876^BCB,,Y,N
^FD>;{sn(15)}^FS
^BY4,3,44^FT897,876^BCB,,Y,N
^FD>;{sn(16)}^FS
^BY4,3,44^FT1005,876^BCB,,Y,N
^FD>;{sn(17)}^FS
^BY4,3,44^FT1113,876^BCB,,Y,N
^FD>;{sn(18)}^FS
^BY4,3,44^FT573,454^BCB,,Y,N
^FD>;{sn(19)}^FS
^BY4,3,44^FT681,454^BCB,,Y,N
^FD>;{sn(20)}^FS
^BY4,3,44^FT789,454^BCB,,Y,N
^FD>;{sn(21)}^FS
^BY4,3,44^FT897,454^BCB,,Y,N
^FD>;{sn(22)}^FS
^BY4,3,44^FT1005,454^BCB,,Y,N
^FD>;{sn(23)}^FS
^BY4,3,44^FT1113,454^BCB,,Y,N
^FD>;{sn(24)}^FS
^FO273,91^GB167,355,3^FS
^FT325,253^A0B,33,33^FH\^FD{LOTInfo(15)}^FS
^FT372,253^A0B,33,33^FH\^FD4,085^FS
^FT420,253^A0B,33,33^FH\^FD2,448^FS
^FT100,1311^A0B,50,50^FH\^FD{Mid(LOTInfo(1), 25, 6)}^FS
^FT100,955^A0B,50,50^FH\^FD{Mid(LOTInfo(1), 37)}^FS
^PQ1,0,1,Y^XZ

"
            Case 48
                str = $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO288,0^GFA,14336,14336,00008,:Z64:
eJztzLERAAAEADGdtY1uB1Qu33yXiLOydufz+Xw+n8/n8/l8Pp/P5/P5fD6fz+fz+Xw+n8/n8/l8Pp/P5/P5fD6fz+fz+Xw+f+K/rAG8bmor:789A
^FO320,608^GFA,06528,06528,00012,:Z64:
eJztmEFOwzAQRWfIwstwg1yBJQtErsIRWLJAcjgJV2lv4ooLJOqCIFU1RUj+35JT4qbQApnVa9pmxuM/40lE/oKVwJu6CWzrBdiCvXcpVt+B164NvJrA3h/MwpxrSn8tt+DaMzeBrV+QXxd4W4L7IrC2cDCGhZnuE92f/XI8HGdF8fO69PBUSUH/raJcwW+cH2YXQqC1aF+MyYlLMufn0cBXDUHsdHIwZ1uUn/YO8awRp0XtiO/aJL+BtSNejWH40o78vr2S31H6AWfrp8iq32FGaLqhEPi67/EF16yhNQpvadkmrxtiy70Oa1f6iZIrTkl277IcW2Zxzr1rtE3TIW/8wHZN4KPFdgamIpeBG7kK7CStnw31im1J63pOn+81aaYknRSkE0PNgrVEetNonnlOx7AZOJuc3CfXSGsfYz+777fEVGvcTIuIXWDupeaFeEn9s8nic9D83BP2W6YvfSJeEXcoR+1Ieg/WBb6ucI5fG9TsA/SpvdhwzxbneORr+TtyNczRnNPgA+VKKFdieBCh+kWuhGeec9DV/Bzxhc3PEZ+2uMD9ab9y51KN59IpMyp40vPR99puDsFM0tDc5QzlhHpCX1P/Qd9QTz3Ec23SeqNeNHCuIYTT9Z/5WWm//bNam8/rw+xoeTvWyHyi/DTEraBeegk634UZIv0O/vAVeCPGpdhKBV7g3X5NM1VFvb300KfxNLhH79ka+Wl7B9uSgRw=:4FF6
^FT74,313^BQN,2,4
^FH\^FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}\0D\{sn(11)}\0D\{sn(12)}\0D\{sn(13)}\0D\{sn(14)}\0D\{sn(15)}\0D\{sn(16)}\0D\{sn(17)}\0D\{sn(18)}\0D\{sn(19)}\0D\{sn(20)}\0D\{sn(21)}\0D\{sn(22)}\0D\{sn(23)}\0D\{sn(24)}^FS
^BY4,3,44^FT450,1715^BCB,,Y,N
^FD>;{sn(1)}^FS
^BY4,3,44^FT558,1715^BCB,,Y,N
^FD>;{sn(2)}^FS
^BY4,3,44^FT666,1715^BCB,,Y,N
^FD>;{sn(3)}^FS
^BY4,3,44^FT774,1715^BCB,,Y,N
^FD>;{sn(4)}^FS
^BY4,3,44^FT882,1715^BCB,,Y,N
^FD>;{sn(5)}^FS
^BY4,3,44^FT990,1715^BCB,,Y,N
^FD>;{sn(6)}^FS
^BY4,3,44^FT450,1293^BCB,,Y,N
^FD>;{sn(7)}^FS
^BY4,3,44^FT558,1293^BCB,,Y,N
^FD>;{sn(8)}^FS
^BY4,3,44^FT666,1293^BCB,,Y,N
^FD>;{sn(9)}^FS
^BY4,3,44^FT774,1293^BCB,,Y,N
^FD>;{sn(10)}^FS
^BY4,3,44^FT882,1293^BCB,,Y,N
^FD>;{sn(11)}^FS
^BY4,3,44^FT990,1293^BCB,,Y,N
^FD>;{sn(12)}^FS
^BY4,3,44^FT450,871^BCB,,Y,N
^FD>;{sn(13)}^FS
^BY4,3,44^FT558,871^BCB,,Y,N
^FD>;{sn(14)}^FS
^BY4,3,44^FT666,871^BCB,,Y,N
^FD>;{sn(15)}^FS
^BY4,3,44^FT774,871^BCB,,Y,N
^FD>;{sn(16)}^FS
^BY4,3,44^FT882,871^BCB,,Y,N
^FD>;{sn(17)}^FS
^BY4,3,44^FT990,871^BCB,,Y,N
^FD>;{sn(18)}^FS
^BY4,3,44^FT450,449^BCB,,Y,N
^FD>;{sn(19)}^FS
^BY4,3,44^FT558,449^BCB,,Y,N
^FD>;{sn(20)}^FS
^BY4,3,44^FT666,449^BCB,,Y,N
^FD>;{sn(21)}^FS
^BY4,3,44^FT774,449^BCB,,Y,N
^FD>;{sn(22)}^FS
^BY4,3,44^FT882,449^BCB,,Y,N
^FD>;{sn(23)}^FS
^BY4,3,44^FT990,449^BCB,,Y,N
^FD>;{sn(24)}^FS
^PQ1,0,1,Y^XZ
"
                '^FH\FDLA,{sn(1)}\0D\{sn(2)}\0D\{sn(3)}\0D\{sn(4)}\0D\{sn(5)}\0D\{sn(6)}\0D\{sn(7)}\0D\{sn(8)}\0D\{sn(9)}\0D\{sn(10)}\0D\{sn(11)}\0D\{sn(12)}\0D\{sn(13)}\0D\{sn(14)}\0D\{sn(15)}\0D\{sn(16)}\0D\{sn(17)}\0D\{sn(18)}\0D\{sn(19)}\0D\{sn(20)}\0D\{sn(21)}\0D\{sn(22)}\0D\{sn(23)}\0D\{sn(24)}^FS
                str += $"
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^JUS^LRN^CI0^XZ
^XA
^MMT
^PW1181
^LL1772
^LS0
^FO448,0^GFA,07168,07168,00004,:Z64:
eJztyjENAAAIBLF3gH+3KCAwMfWS25rcq2We53me53me53me53me53me53me52f/UQPavkSt:03A7
^FO32,320^GFA,03840,03840,00012,:Z64:
eJztljFuhDAQRW05ikuOwFE4GkQpci1SUeYIQUqx7W63kTaZbOX/rMAGAgopmOoZwcyf8Xiwc//Dolm7Ch/h81PsrdGi7sXlhSL4QatFtY42o8+f7c659P69C/LTlOIW2nrEOoE/xMFUFG9nBctqApkRLAl/WofMCnxaJf1FY2kfiwflGB/NyE3ic5W46wtxmxIe4/A2wif58e/y7y+K60x6XI29qKT/Wh80MVpyknG/Kuwv/LusBxQgQGeQfheU19W9AkzhQIafzD/iUg91uhr6mRfznWvspZq1Gq0PuUkSkEvs5fRGTZpBRn38sYQeHZi4gGfb3kvfbJ/JG9t+Zm9bVp/npM3Zi3RalzhY1w7yQRw78NMUVqzYicPhFXGnnFPw7JmPPV10LhCWd0s+t14LzsYSl84wfJ/Mnpdiz17FTIh4PfLqCu9zc/RZu838oe7/iN/YkpGQjwfI3IjXy2UD+wLHKPrk:C179
^FO448,608^GFA,06528,06528,00012,:Z64:
eJztmD1u20AQRocg4E0RYF2qMHZukCNY13JhiAxc5Bi+CoEUuQaPYCCFbcDIRoUx8xahAq1Ixw7EqR5U7PzsN7NDiZy7PRi135+Mw88X50fnmH9NsubkfB2NUzqdNeeTOYKrTcF5NGwzapVRq3ygPiO4v8bxWsWRjHOK8+GX8TDOBvEzryLfWts6NkWtDtVnulaKXLT3Qw/XJE0z6hOGW8TjgtAZXG1FfS4QD+JE78Ss03yPOqhzOoqhPXC838HXMfpxrtcP7nRGX6uHJjrI9O+5N2bPSuc57hNGbHn69845FLNuBMMvXLEktTmGQm6VzbnOrqNtlg55R8VlL8OLxfYB7FIa4420xknw1lM/A2bFiLyep9/3FpoR6mQ7OneDIbVEvSl7HL6KGBgbYk4SjJkjcz/G/um9t6NzxGznMN2SMT85S3fe13ILEd/EKv4Iml9nwt+t0tcGnMCKt1sVz9E3l1X71d/xtu+sZ0Pv+tQ++Jl4x+nr6j+p1WEecCbaNKAFG+w80nERQf8G5rVQbEu9NcWePBqv3xGvVtTnfL8jPssX94X7qt1LCw1AJ/U7qvO876O3tUvsJBvsXUk6Y+VMuAP/AD9yB2NvjsblLEIQrANWgHebP4xn/Vb6w86t19b3+jRbrG48cw6/T30+gaN4v8ReTOeKSN+C976sv+IguykOD40JMbwE09t+tTFuMvorY550eTAu/meDzmvsNxZXR/w=:5216
^FO96,928^GFA,06400,06400,00008,:Z64:
eJzlmD9u2zAUxqkKMJeCXDsIYo9QoEuKGvaVAnRIgBqRT+C5Q9GzKBfICTJo61YkUwNUtVrofZ8NsqGkyK7jum/5ATb5/vGRj5RSB5RX4CzCrHv6+UAW627G5EqQNGXLtLlrqZsHoalbmslCqKdgBpqW1hrME6ZrYVLDzkO3GzGxzbqTGuP0UpiS5UADc5mQIO6UcTc19Eq8VuUtjTIe+TvHcR71UC/txCS5Q95qP4+2WQntjdhNvwmTNfwgZSE16kov4Rf8VGrSaf/JYlFXDpwH9XaBur4wPj+sQIx7C75joVz6dnat+775oehK6FCwc/hVgAvs23PzON+AZ4jfoRCKCPvkUHn+QwbmtSAr+Ev9a5+Mo8B+KJaBnti5ddXj537kr58zR25/Iwffb6XQoi7CfTeVc1Vd5tAT2Xf8fzrz51Mf9esq4sgTJSk99q2fxTh9LUzvMf8nWIAOZF5iC6wroZX4jEOfRtxmJnkwZ+gnIT9JvtJbyW+KvpXwWOI+pT+hPFff3BT+SIbyv95/9pbHF/JzhfW8h38/fD9zV7XMnMzKTDcd5sXYK+hLSSN2t/UptKUsjFWynjnqMUN9xmgwjvOoh3pZP7Sr5qFjR5Kvfe+jHjlUf/tn7tE43tK69KiNcDLxyd/D8dRD6cuzQrxKs89Wwob+QE81UN+eJGF7pv3g2uiwD3IrzHQ3T/f88CW9Rr/6Cr++wy/003wGv3IQB0aMp563bCRPPS/bC7LQNU1AP94+v1zNerpRj83/fUFiYnjR9OxvOVL4/uZ7PST/jwkbkfssnK98fuT7OuQX4Xtw7Duk7521wDjej3lg8wDnxXbsd8Ghfgy9L+zqx8G/g+DBmgXv0c33oNtAf4Rj4z62d/Fz18Hrl8JYnL3x8mKBuHXVbe/oz+sd5BeXe1qR:0F3D
^FO128,736^GFA,19840,19840,00020,:Z64:
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:0584
^FO256,832^GFA,21504,21504,00024,:Z64:
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:94FB
^FO288,256^GFA,01536,01536,00008,:Z64:
eJzNkzEOwjAMRRMFYabmAlVzBUYmuBZbe7QepRJDVzaKBE0l/D0Q1U2RGOrlDbG/7V/XmH9FQczgm1nKuxb2znQvJo1MH0HPrIj1SjdPeffoK/WiJ/p2UAYpkX9O+oPhgbRbs8gQl6m0VZnTy9WncXB4V3zM+gk6+Kr5aTvk90wPf4SV5mPPLPqk79rviMh+hyMSTwmvqB8S4k5Du9x3bf+APN8xnewTRSBTr9zTr3Oodyd7j6LzrWdwF4bm59jIf70z5lO3x5zUsK/U1syO96En69k35q5b5gXryboYiyLqFG48Jt3Vigs=:52E3
^FO384,256^GFA,01280,01280,00008,:Z64:
eJzN0j0OwjAMBeBWkfAWcwCIr1Ekfq7ejQ2u0CMgsSCBCMOLh1RN3KEC3vINiR3FSdMslY2HR5lW10txA6QH5BeUNwwB7hl2NK2uB8nrtZ/2d7f8/M7wFOta9d8+R+N6yBco59xDcjf2CrfJuXMcx7pP28/T6rNU1itY+l/mP0tSmlNpPj9+l3bAPndHHT3RhyJkwf08Jz1V5Rir/mk+z9RWLw==:BB56
^FO320,256^GFA,01280,01280,00008,:Z64:
eJy9kz1uAjEQhXdlCXeelBRgcwyQCL4YEhyEw9DRwQ2ivUGQaFYCxSneGyRbeJciymu+Yv7H46b5O80cuAmvqfaaTAfaHpQHGH5A78G1gEv7mmr3IY/XfJrfXPP6yxHGNMyx+P+usyDXmndPcu6o+0g07I5ggEObsCghY78FO2SMjWf+RVZPpfst3zGSehe1d5ySejeSkChWWBP7mArG8OFBog+3wRiOdyOs/yT7sAfY7Rf8zTfiWy2/07vtBtt5Wx8TsLaf0XsnLfsr711ljvQ/M/6U85NclbyA80tRd+SfFWo7+Jkb4uwdeWwCJWA+J6Szg5SUBpnpFxKsZ8c=:351A
^FO288,128^GFA,00768,00768,00008,:Z64:
eJxjYBgxgA1K8zdgp9mxa5MhQMv/b8BLE9KPCxAyl4EZP01z/cMCAACuoSCT:BDD0
^FO320,128^GFA,00512,00512,00008,:Z64:
eJxjYCAZ8JFJ8///j5fGBTgYGME0DwM/RD2DPYRugOo/CKU//wHTzL8fQDT+b4DQ9VCD5KE0O2XuGVYAAOoQGpo=:73BE
^FO384,128^GFA,00512,00512,00008,:Z64:
eJxjYCAR8JFJ8///j5fGBTgYGME0DwM/RD2DPYRugOo/CKU//wHTzL8fQDT+b4DQ9VCD5KE0O2XuGWYAAB9RGpo=:F20E
^FO32,1312^GFA,04992,04992,00012,:Z64:
eJztlkFuhiAQhYfY1CVH8CKNXKUHMcXezKNwBJMuyoJI2fzMo4EYfiWxqW/1LRQfzJtBon8i732OxVm8jlTHitnqGdiAT5f3/82sv/KsDPOw6MhylsTqgAXtSYAHCayAdYHL528jOztFtvYt8mrHAivmCVkza/6uRVZblt3AZ/4umam3zMLQkzotbwUOG6Czub3nQ+JGUqd5myPDkQi/AEMc4Hka/Bq585wrguxRX2B4pk/6K64TvBlg8LMClzwfqJFM6jVQjfAQe/fCq7iPyApYOx6UPmGu9Qb9bpPer2PLs4LsxB62iWvnoY4aZrKCEAzAMsnAEpkgDgUJ24Ab928Lz0Ut9BrZUaxXeHcEVk+zI53jUDrIw+d+Nso54WxI7uU0JxzDI2pSa4gwzhaCbYVeZpYwNjrYL+EAhfs3eUE0zW217uxdU/d/6WNVQxndd0qF7iw9VjVUoT+TBxiZv/6lV2BX4C3HeE8FaWBV4D11nvfehOG2vgQ33u9l9QPbX7II:F2EB
^FO32,960^GFA,01920,01920,00012,:Z64:
eJzN1DEOhCAQBdAxFpZ7BK6xHdeyWj0aR6LcgvjFZFc+BmLAdcNUr5DJMMMoUhUjeUm7w7lzZ+VZV1YUPXzcaBFlpCHff99roSiRBuUvdWHNuiG3OKOOrMiUc0iMbjvJu0CmegZg32sN2A+3mufg/RPvV7Alz8FvCXZkGLIjR/nDb2YCzNcPwKXqj+7Vc5+nTH+4b1E/uc+50OlGl++LVNrSW/2Vfcvp/VvyUrsXh1lkh4STfh7991gBuFArsw==:BDA9
^BY2,3,83^FT190,637^BCB,,N,N
^FD>:{sn(0).Split(";")(0) & Mid(Integer.Parse(sn(0).Split(";")(1)).ToString("00000"), 1, 1)}>5{Mid(Integer.Parse(sn(0).Split(";")(1)).ToString("00000"), 2)}^FS
^FT248,630^A0B,50,50^FH\^FD{sn(0).Split(";")(0) & Integer.Parse(sn(0).Split(";")(1)).ToString("00000")}^FS
^FT48,315^BQN,2,4
^FH\^FDLA,{sn(25)}\0D\{sn(26)}\0D\{sn(27)}\0D\{sn(28)}\0D\{sn(29)}\0D\{sn(30)}\0D\{sn(31)}\0D\{sn(32)}\0D\{sn(33)}\0D\{sn(34)}\0D\{sn(35)}\0D\{sn(36)}\0D\{sn(37)}\0D\{sn(38)}\0D\{sn(39)}\0D\{sn(40)}\0D\{sn(41)}\0D\{sn(42)}\0D\{sn(43)}\0D\{sn(44)}\0D\{sn(45)}\0D\{sn(46)}\0D\{sn(47)}\0D\{sn(48)}^FS
^BY4,3,44^FT573,1719^BCB,,Y,N
^FD>;{sn(25)}^FS
^BY4,3,44^FT681,1719^BCB,,Y,N
^FD>;{sn(26)}^FS
^BY4,3,44^FT789,1719^BCB,,Y,N
^FD>;{sn(27)}^FS
^BY4,3,44^FT897,1719^BCB,,Y,N
^FD>;{sn(28)}^FS
^BY4,3,44^FT1005,1719^BCB,,Y,N
^FD>;{sn(29)}^FS
^BY4,3,44^FT1113,1719^BCB,,Y,N
^FD>;{sn(30)}^FS
^BY4,3,44^FT573,1298^BCB,,Y,N
^FD>;{sn(31)}^FS
^BY4,3,44^FT681,1298^BCB,,Y,N
^FD>;{sn(32)}^FS
^BY4,3,44^FT789,1298^BCB,,Y,N
^FD>;{sn(33)}^FS
^BY4,3,44^FT897,1298^BCB,,Y,N
^FD>;{sn(34)}^FS
^BY4,3,44^FT1005,1298^BCB,,Y,N
^FD>;{sn(35)}^FS
^BY4,3,44^FT1113,1298^BCB,,Y,N
^FD>;{sn(36)}^FS
^BY4,3,44^FT573,876^BCB,,Y,N
^FD>;{sn(37)}^FS
^BY4,3,44^FT681,876^BCB,,Y,N
^FD>;{sn(38)}^FS
^BY4,3,44^FT789,876^BCB,,Y,N
^FD>;{sn(39)}^FS
^BY4,3,44^FT897,876^BCB,,Y,N
^FD>;{sn(40)}^FS
^BY4,3,44^FT1005,876^BCB,,Y,N
^FD>;{sn(41)}^FS
^BY4,3,44^FT1113,876^BCB,,Y,N
^FD>;{sn(42)}^FS
^BY4,3,44^FT573,454^BCB,,Y,N
^FD>;{sn(43)}^FS
^BY4,3,44^FT681,454^BCB,,Y,N
^FD>;{sn(44)}^FS
^BY4,3,44^FT789,454^BCB,,Y,N
^FD>;{sn(45)}^FS
^BY4,3,44^FT897,454^BCB,,Y,N
^FD>;{sn(46)}^FS
^BY4,3,44^FT1005,454^BCB,,Y,N
^FD>;{sn(47)}^FS
^BY4,3,44^FT1113,454^BCB,,Y,N
^FD>;{sn(48)}^FS
^FO273,91^GB167,355,3^FS
^FT325,253^A0B,33,33^FH\^FD{LOTInfo(15)}^FS
^FT372,253^A0B,33,33^FH\^FD6,850^FS
^FT420,253^A0B,33,33^FH\^FD5,184^FS
^FT100,1311^A0B,50,50^FH\^FD{Mid(LOTInfo(1), 25, 6)}^FS
^FT100,955^A0B,50,50^FH\^FD{Mid(LOTInfo(1), 37)}^FS
^PQ1,0,1,Y^XZ
"
        End Select
        Return str
    End Function
#End Region
End Class








