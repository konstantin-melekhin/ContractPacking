Imports Library3
Public Class WF_NS220
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

    Private Sub WorkForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Настройка COM порта
        PrintSerialPort.PortName = "com3"
        PrintSerialPort.BaudRate = 115200
        'Требуется печать или нет
        Try
            PrintSerialPort.Open()
            PrintSerialPort.Close()
        Catch ex As Exception
            PrintLabel(Controllabel, "Проверьте подключение ком порта!", 12, 193, Color.Red) ' если не настроен ком порт для печати
            SerialTextBox.Enabled = False
        End Try
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
    'очистка Серийного номера при ошибке
    Private Sub BT_ClearSN_Click(sender As Object, e As EventArgs) Handles BT_ClearSN.Click
        SerialTextBox.Clear()
        SerialTextBox.Enabled = True
        SNBufer = New ArrayList()
        SerialTextBox.Focus()
    End Sub
    'Часы в программе
    Private Sub CurrentTimeTimer_Tick(sender As Object, e As EventArgs) Handles CurrentTimeTimer.Tick
        CurrrentTimeLabel.Text = TimeString
    End Sub 'Часы в программе
    'регистрация пользователя
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
    '_________________________________________________________________________________________________________________
    'начало работы приложения FAS Scanning Station
    '________________________________________________________________________________________________________________

    Private Sub SerialTextBox_KeyDown(sender As Object, e As KeyEventArgs) Handles SerialTextBox.KeyDown
        If e.KeyCode = Keys.Enter Then 'And (SerialTextBox.TextLength = LenSN_SMT Or SerialTextBox.TextLength = LenSN_FAS) Then
            'определение формата номера
            If GetFTSN(LOTInfo(12)) = True Then
                'проверка диапазона номера
                If CheckRange(SNFormat) = True Then
                    'проверка задвоения и наличия номера в базе
                    If CheckDublicate(SerialTextBox.Text, GetPcbID(SNFormat)) = True Then
                        Dim Mess As String
                        If LOTInfo(12) = False Then ' если номер двойной
                            If SNBufer.Count = 0 Then
                                Select Case SNFormat(1)
                                    Case 1 ' запись в буфер СМТ номера
                                        SNBufer = New ArrayList From {True, SerialTextBox.Text, False, ""}
                                        Mess = "SMT номер " & SerialTextBox.Text & " определен!" & vbCrLf &
                                       "Отсканируйте номер FAS!"
                                    Case 2 'запись в буфер FAS номера
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
                                Case 1 ' одиночный СМТ номер
                                    SNID = 0
                                    WriteDB(SerialTextBox.Text, "")
                                    Mess = "SMT номер " & SerialTextBox.Text & " определен и " & vbCrLf &
                                    "записан в базу!"
                                Case 2 ' одиночный ФАС номер
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
            End If
        End If
        SerialTextBox.Focus()
    End Sub


    '1. Определение формата номера
    Private Function GetFTSN(SingleSN As Boolean) As Boolean
        Dim col As Color, Mess As String, Res As Boolean
        SNFormat = New ArrayList()
        SNFormat = Library3.GetSNFormat(LOTInfo(3), LOTInfo(8), SerialTextBox.Text, LOTInfo(18), LOTInfo(2), LOTInfo(7))
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
                    End If
                End If
            End If
        End If
        col = If(Res = False, Color.Red, Color.Green)
        PrintLabel(Controllabel, Mess, 12, 193, col)
        SNTBEnabled(Res)
        Return Res
    End Function

    '2 проверка диапазона
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



    '3 поиск ID PCB в базе гравировщика И SNID в базе FASSN_reg
    Private Function GetPcbID(SNFormat As ArrayList) As ArrayList
        Dim Res As New ArrayList(), Mess As String, Col As Color
        Select Case SNFormat(1)
            Case 1
                PCBID = SelectInt("USE SMDCOMPONETS Select [IDLaser] FROM [SMDCOMPONETS].[dbo].[LazerBase] where Content = '" & SerialTextBox.Text & "'")
                Res.Add(PCBID <> 0)
                Res.Add(PCBID)
                Res.Add(SNFormat(1))
                Mess = If(PCBID = 0, "SMT номер " & SerialTextBox.Text & vbCrLf & "не зарегистрирован в базе гравировщика!", "")
            Case 2
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
    '4. Проверка предыдущего шага и дубликатов
    Private Function CheckDublicate(SN As String, GetPCB_SNID As ArrayList) As Boolean
        Dim Res, Res1, Res2 As Boolean, SQL As String, Mess As String, Col As Color
        'Проверка предыдущего шага
        If GetPCB_SNID(0) = True Then
            Select Case GetPCB_SNID(2)
                Case 1
                    'Dim PCBStepRes As New ArrayList(SelectListString("USE FAS SELECT [StepID],[TestResult],[ScanDate],[SNID]
                    '        FROM [FAS].[dbo].[Ct_StepResult] where [PCBID] = " & GetPCB_SNID(1)))
                    'Res1 = If(PCBStepRes.Count <> 0, (PCBStepRes(0) = PreStepID And PCBStepRes(1) = 2), False)
                    'Mess = If(Res1 = False, "Плата " & SerialTextBox.Text & vbCrLf & "имеет не верный предыдущий шаг!", "")
                    Res1 = True
                Case 2
                    Res1 = True
            End Select
            'проверка задвоения в базе
            Dim PackedSN As ArrayList
            Select Case GetPCB_SNID(2)
                Case 1
                    SQL = "Use FAS SELECT L.Content,S.SN,Lit.LiterName + cast ([LiterIndex] as nvarchar),[PalletNum],[BoxNum],[UnitNum],[PackingDate],U.UserName
                        FROM [FAS].[dbo].[Ct_PackingTable] as P
                        left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = P.PCBID
                        Left join Ct_FASSN_reg as S On S.ID = P.SNID
                        Left join FAS_Liter as Lit On Lit.ID = P.LiterID
                        Left join FAS_Users as U On U.UserID = P.UserID
                        where PCBID = " & GetPCB_SNID(1)
                    PackedSN = New ArrayList(SelectListString(SQL))
                    Res2 = (PackedSN.Count = 0)
                    If Res1 = True And Res2 = False Or Res1 = False And Res2 = False Then
                        Mess = If(PackedSN.Count <> 0, "Плата " & SerialTextBox.Text & " уже упакована!" & vbCrLf &
                                                   "Литера - " & PackedSN(2) & " Паллет - " & PackedSN(3) & " Групповая - " & PackedSN(4) & " № - " & PackedSN(5) & vbCrLf &
                                                   "Дата - " & PackedSN(6), "")
                    End If

                Case 2
                    SQL = "Use FAS SELECT L.Content,S.SN,Lit.LiterName + cast ([LiterIndex] as nvarchar),[PalletNum],[BoxNum],[UnitNum],[PackingDate],U.UserName
                        FROM [FAS].[dbo].[Ct_PackingTable] as P
                        left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = P.PCBID
                        Left join Ct_FASSN_reg as S On S.ID = P.SNID
                        Left join FAS_Liter as Lit On Lit.ID = P.LiterID
                        Left join FAS_Users as U On U.UserID = P.UserID
                        where SNID = " & GetPCB_SNID(1)
                    PackedSN = New ArrayList(SelectListString(SQL))
                    Mess = If(PackedSN.Count <> 0, "Плата " & SerialTextBox.Text & " уже упакована!" & vbCrLf &
                            "Литера - " & PackedSN(2) & " Паллет - " & PackedSN(3) & " Групповая - " & PackedSN(4) & " № - " & PackedSN(5) & vbCrLf &
                            "Дата - " & PackedSN(6), "")
                    Res2 = (PackedSN.Count = 0)
            End Select
            Res = (Res1 * Res2)
            Col = If(Res = False, Color.Red, Color.Green)
            PrintLabel(Controllabel, Mess, 12, 193, Col)
            SNTBEnabled(Res)
            Return Res
        Else
            Return False
        End If
    End Function


    '5. Запись в базу данных и в Рабочий грид
    Dim TableColumn As ArrayList
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
        TableColumn = New ArrayList() From {UnitCounter, SMTSN, FASSN, Litera, PalletNumber, BoxNumber, Date.Now}
        Dim row = ds.Tables(0).NewRow()
        Dim i = 0
        For Each item In TableColumn
            row.Item(i) = item
            i += 1
        Next
        ds.Tables(0).Rows.Add(row)
        DG_Packing.DataSource = ds
        DG_Packing.Sort(DG_Packing.Columns(0), System.ComponentModel.ListSortDirection.Descending)
        RunCommand(" use FAS
                insert into [FAS].[dbo].[Ct_PackingTable] (PCBID,SNID,LOTID, LiterID,LiterIndex,PalletNum,BoxNum,UnitNum,PackingDate,UserID)values
                (" & If(PCBID = 0, "Null", PCBID) & "," & If(SNID = 0, "Null", SNID) & "," & LOTID & "," & PCInfo(8) & "," & LOTInfo(17) & "," & PalletNumber & "," & BoxNumber & "," & UnitCounter & ",current_timestamp," & UserInfo(0) & ")
                update [FAS].[dbo].[FAS_PackingCounter] set [PalletCounter] = " & PalletNumber & ",[BoxCounter] = " & BoxNumber & ",[UnitCounter] = " & UnitCounter & " 
                where [LineID] = " & PCInfo(2) & " and [LOTID] = " & LOTID)
        SNBufer = New ArrayList
        ShiftCounter(2)
        'печать групповой этикетки 
        If UnitCounter = LOTInfo(15) Then '
            SerchBoxForPrint(LOTID, BoxNumber, PCInfo(8))
            SNArray = GetSNFromGrid()
            PrintGroupLabel(SNArray)
        End If

        If PCBID <> 0 Then
            RunCommand("USE FAS Update [FAS].[dbo].[Ct_StepResult] 
                    set StepID = 6, TestResult = 2, ScanDate = CURRENT_TIMESTAMP, SNID = " & SNID & "
                    where PCBID = " & PCBID)
            RunCommand("insert into [FAS].[dbo].[Ct_OperLog] ([PCBID],[LOTID],[StepID],[TestResultID],[StepDate],
                    [StepByID],[LineID],[SNID])values
                    (" & PCBID & "," & LOTID & ",6,2,CURRENT_TIMESTAMP," & UserInfo(0) & "," & PCInfo(2) & "," & SNID & ")")
        End If



    End Sub

    '6. 'Счетчик продукции
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

    '6. деактивация ввода серийника
    Private Sub SNTBEnabled(Res As Boolean)
        SerialTextBox.Enabled = Res
        BT_Pause.Focus()
    End Sub


    '7. печать групповой
    Dim SNArray As New ArrayList
    Dim SQL As String
    Private Sub SerchBoxForPrint(LotID As Integer, BoxNum As Integer, LiterID As Integer) 'LitName As String,
        SQL = "use fas
                SELECT  [UnitNum] as '№',l.Content AS 'Серийный номер платы',Lit.LiterName as 'Литера' ,[BoxNum]as 'Номер коробки' 
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
            For i = 0 To DG_SelectedBox.Rows.Count - 1
                SNArrayTemp.Add(Mid(DG_SelectedBox.Item(1, i).Value, 1, 4) & ">5" & Mid(DG_SelectedBox.Item(1, i).Value, 5))
            Next
            SNArrayTemp.Add(DG_SelectedBox.Item(3, 0).Value)
        Else
            PrintLabel(Controllabel, "Корбка еще не закрыта!", 12, 193, Color.Red)
        End If
        Return SNArrayTemp
    End Function

    Private Sub PrintGroupLabel(sn As ArrayList)
        Dim Content As String = "
^XA~TA000~JSN^LT0^MNW^MTT^PON^PMN^LH0,0^JMA^MD10^JUS^LRN^CI0^XZ
~DG000.GRF,08448,088,
,::::::::::::::::::::::::::::::::::::::::kM03C0780gY01FE0,01FF801FF80jV03E0780gW01FHFE0,01FF801FF80jV01E0F80gW07FHFE0,01FF801FF80jV01FHFgX01FIFC0,01FF801FF80jW0HFE0gW03FIFC0,01FF801FF80jW07FC0gW07FIF80,01FF801FF80jW03F80gW07FHFE,01FF801FF80lX0HFC,01FF801FF80lX0HF0,01FF801FF80H0HF80S03FE0K03F0T01F80gK01FF0Q03FE0gI03FE0K03F0J0HF8001FEFF0,01FF801FF8003FHFH01FFC007FF001FHFC007F8FFC0J01FIFC3FC7FE03FF00FFC7FJFC0FKF8007FFE003FIFJ0IFC007FC0FFE0J0HF807F800FHFC007F8FFC0H03FHFH01FIFE003FE01FE0FF81FFC0,01FF801FF800FIF801FFC00FHFH03FHFE007F9FFE0J01FIFC3FCFHF01FF00FF87FJFC0FKF801FIFH03FIFC003FHFE007FC0FFE0J0HF81FF803FHFE007F9FFE0H0JF801FJFH03FE07FE0FF81FFC0,01FF801FF801FIFC01FFE00FHFH07FIF807FBFHFK01FIFC3FDFHF81FF00FF87FJFC0FKF803FIF803FIFE007FIFH07FC1FFE0J0HF83FF807FIFH07FBFHFH01FIFC01FJF803FE0FFE0FF83FFC0,01FLF803FIFE01FFE00FHFH0KF807FJF80I01FIFC3FJFC1FF80FF87FJFC0FKF807FIFC03FJFH0KF807FC1FFE0J0HF87FF80FJF807FJF803FIFE03FJFC03FE1FFE0FF83FFC0,01FLF807FJF01FHF01FHF01FJFC07FJF80I01FIFC3FJFC0FF81FF07FJFC0FKF80FJFE03FJF01FJFC07FC3FFE0J0HF87FF81FJFC07FJF807FJF03FJFE03FE1FFE0FF87FFC0,01FLF807FE3FF81FHF01FHF03FF07FE07FF0FFC0I01FIFC3FF87FE0FF81FF07FJFC0FKF80FFC7FF03FE1FF01FF8FFE07FC3FFE0J0HF8FHF81FF8FFE07FF0FFC07FE3FF83FFC7FF03FE3FFE0FF87FFC0,01FLF80FF80FF81FHF83FHF03FE03FE07FE07FC0I01FIFC3FF03FE07FC1FF07FC07FC0FF80FF81FF01FF03FE1FF03FE03FE07FC7FFE0J0HF8FF803FE03FE07FE07FC0FF80FF83FF01FF03FE3FE00FF8FHFC0,01FLF80FF80FF81FHF83FHF03FC01FE07FE07FC0I01FF0H03FF03FE07FC1FE07FC07FC0FF80FF81FF01FF03FE1FF03FE03FE07FC7FFE0J0HF9FF003FE03FE07FE07FC0FF80FF83FF01FF03FE7FC00FF8FHFC0,01FLF81FF007FC1FHF83FHF07FC01FF07FC03FE0I01FF0H03FE01FF03FC3FE07FC07FC0FF80FF83FE00FF83FE3FE07FC01FF07FCFHFE0J0HFBFE007FC01FF07FC03FE1FF007FC3FE00FF83FEFF800FF9FHFC0,01FLF81FF007FC1FHFC7FHF07FKF07FC03FE0I01FF0H03FE01FF03FE3FE07FC07FC0FF80FF83FE00FF83FIFC07FC01FF07FDFHFE0J0JFE007FC01FF07FC03FE1FF007FC3FE00FF83FIF800FFBFHFC0,01FLF81FF007FC1FEFC7EFF07FKF07FC03FE0I01FF0H03FE01FF03FE3FC07FC07FC0FF80FF83FE00FF83FIF807FC01FF07FDFHFE0J0JFC007FC01FF07FC03FE1FF007FC3FE00FF83FIFI0HFBFHFC0,01FF801FF81FF007FC1FEFEFEFF07FKF07FC03FE0I01FF0H03FE01FF01FE3FC07FC07FC0FF80FF83FE00FF83FIF807FC01FF07FJFE0J0JF8007FC01FF07FC03FE1FF007FC3FE00FF83FHFE0H0LFC0,01FF801FF81FF007FC1FEFEFEFF07FKF07FC03FE0I01FF0H03FE01FF01FE7F807FC07FC0FF80FF83FE00FF83FIFE07FC01FF07FJFE0J0JFE007FC01FF07FC03FE1FF007FC3FE00FF83FIF800FKFC0,01FF801FF81FF007FC1FE7EFCFF07FKF07FC03FE0I01FF0H03FE01FF00FF7F807FC07FC0FF80FF83FE00FF83FJF07FC01FF07FHFBFE0J0KFH07FC01FF07FC03FE1FF007FC3FE00FF83FIFC00FIF7FC0,01FF801FF81FF007FC1FE7FFCFF07FC0J07FC03FE0I01FF0H03FE01FF00FF7F807FC07FC0FF80FF83FE00FF83FE1FF87FC01FF07FHFBFE0J0KF807FC01FF07FC03FE1FF007FC3FE00FF83FIFE00FIF7FC0,01FF801FF81FF007FC1FE3FF8FF07FC0J07FC03FE0I01FF0H03FE01FF00FF7F007FC07FC0FF80FF83FE00FF83FE0FF87FC01FF07FHF3FE0J0HF9FFC07FC01FF07FC03FE1FF007FC1FE00FF83FE7FF00FHFE7FC0,01FF801FF80FF80FF81FE3FF8FF03FE0J07FE07FE0I01FF0H03FF03FF007FHFH07FC07FC0FF80FF81FF01FF03FE0FF83FE03FE07FHF3FE0J0HF8FFC03FE03FE07FE07FE0FF80FF81FF01FF03FE3FF00FHFE7FC0,01FF801FF80FF80FF81FE3FF8FF03FE03E007FE07FC0I01FF0H03FF03FE007FHFH07FC07FC0FF80FF81FF01FF03FE0FF83FE03FE07FFE3FE0J0HF87FE03FE03FE07FE07FC0FF80FF81FF01FF03FE1FF80FHFC7FC0,01FF801FF807FE3FF01FE1FF0FF03FF0FFE07FF0FFC0I01FF0H03FF87FE003FFE007FC07FC0FF80FF80FFC7FE03FE1FF81FF8FFC07FFC3FE0J0HF87FF01FF8FFC07FF0FFC07FE3FF00FF87FE03FE1FFC0FHF87FC0,01FF801FF807FJF01FE1FF0FF01FJFC07FJFC0I01FF0H03FJFE003FFE007FC07FC0FF80FF80FJFE03FJF81FJFC07FFC3FE0J0HF83FF01FJFC07FJFC07FJFH0KFE03FE0FFC0FHF87FC0,01FF801FF803FIFE01FE0FE0FF00FJF807FJF80I01FF0H03FJFC003FFE007FC07FC0FF80FF807FIFC03FJFH0KF807FF83FE0J0HF83FF80FJF807FJF803FIFE007FIFC03FE0FFE0FHF07FC0,01FF801FF801FIFC01FE0FE0FF00FJF807FJFK01FF0H03FJF8001FFC007FC07FC0FF80FF803FIF803FJFH07FIFH07FF83FE0J0HF81FF807FIFH07FJFH01FIFC003FIF803FE07FE0FHF07FC0,01FF801FF800FIF801FE07E0FF007FHFE007FIFE0J01FF0H03FJFI01FFC007FC07FC0FF80FF801FIFH03FIFE003FHFE007FF03FE0J0HF80FFC03FHFE007FIFE0H0JF8001FIFH03FE03FF0FFE07FC0,01FF801FF8003FFE001FE07C0FF001FHFC007FDFFC0J01FF0H03FEFFE0I0HF8007FC07FC0FF80FF8007FFC003FIFJ0IF8007FF03FE0J0HF80FFE00FHF8007FDFFC0H03FFE0I0IFE003FE03FF8FFE07FC0,S0HF80S07FE0H07FC3F0Q03FE1F80I0HF80X01FF0Q03FE0gI03FE0H07FC3F0J0HF80I01FF0,gV07FC0S03FE0K01FF80iL07FC,gV07FC0S03FE0K01FF0iM07FC,gV07FC0S03FE0I0103FF0iM07FC,gV07FC0S03FE0I01FHFE0iM07FC,:gV07FC0S03FE0I01FHFC0iM07FC,gV07FC0S03FE0J0IFC0iM07FC,gV07FC0S03FE0J0IFiO07FC,iG0HFC,,::::::::::^XA
^MMT
^PW1181
^LL1772
^LS0
^FT192,224^XG000.GRF,1,1^FS
^FT302,138^A0N,66,64^FH\^FDAquarius CMP NS220^FS
^FT897,206^A0N,54,52^FH\^FD" & sn(20) & " " & Litera & "^FS
^BY3,3,90^FT141,337^BCN,,Y,N
^FD>:" & sn(0) & "^FS
^BY3,3,90^FT141,491^BCN,,Y,N
^FD>:" & sn(1) & "^FS
^BY3,3,90^FT141,637^BCN,,Y,N
^FD>:" & sn(2) & "^FS
^BY3,3,90^FT141,796^BCN,,Y,N
^FD>:" & sn(3) & "^FS
^BY3,3,90^FT141,943^BCN,,Y,N
^FD>:" & sn(4) & "^FS
^BY3,3,90^FT141,1099^BCN,,Y,N
^FD>:" & sn(5) & "^FS
^BY3,3,90^FT141,1238^BCN,,Y,N
^FD>:" & sn(6) & "^FS
^BY3,3,90^FT141,1397^BCN,,Y,N
^FD>:" & sn(7) & "^FS
^BY3,3,90^FT141,1544^BCN,,Y,N
^FD>:" & sn(8) & "^FS
^BY3,3,90^FT141,1701^BCN,,Y,N
^FD>:" & sn(9) & "^FS
^BY3,3,90^FT656,337^BCN,,Y,N
^FD>:" & sn(10) & "^FS
^BY3,3,90^FT656,491^BCN,,Y,N
^FD>:" & sn(11) & "^FS
^BY3,3,90^FT656,637^BCN,,Y,N
^FD>:" & sn(12) & "^FS
^BY3,3,90^FT656,796^BCN,,Y,N
^FD>:" & sn(13) & "^FS
^BY3,3,90^FT656,943^BCN,,Y,N
^FD>:" & sn(14) & "^FS
^BY3,3,90^FT656,1099^BCN,,Y,N
^FD>:" & sn(15) & "^FS
^BY3,3,90^FT656,1238^BCN,,Y,N
^FD>:" & sn(16) & "^FS
^BY3,3,90^FT656,1397^BCN,,Y,N
^FD>:" & sn(17) & "^FS
^BY3,3,90^FT656,1544^BCN,,Y,N
^FD>:" & sn(18) & "^FS
^BY3,3,90^FT656,1701^BCN,,Y,N
^FD>:" & sn(19) & "^FS
^PQ1,0,1,Y^XZ
"

        PrintSerialPort.Open()
        PrintSerialPort.Write(Content) 'ответ в COM порт
        PrintSerialPort.Close()
        CB_ManualPrint.Checked = False
    End Sub


    '8. Ручная печать групповой
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
                PrintGroupLabel(SNArray)
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
                PrintGroupLabel(SNArray)
            Else
                PrintLabel(Controllabel, "Корбка еще не закрыта!", 12, 193, Color.Red)
            End If
            NumBox.Value += 1
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




End Class