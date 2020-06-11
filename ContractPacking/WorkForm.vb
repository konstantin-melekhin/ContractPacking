Imports Library3


Public Class WorkForm
    Public Sub New(LOTID As Integer, IDApp As Integer)
        InitializeComponent()
        Me.LOTID = LOTID
        Me.IDApp = IDApp
    End Sub

    Dim LOTID, IDApp, UnitCounter As Integer
    Dim ds As New DataSet
    Dim LenSN_SMT, LenSN_FAS, StartStepID As Integer, PreStepID As Integer, NextStepID As Integer
    Dim StartStep As String, PreStep As String, NextStep As String
    Dim PCInfo As New ArrayList() 'PCInfo = (App_ID, App_Caption, lineID, LineName, StationName,CT_ScanStep)
    Dim LOTInfo As New ArrayList() 'LOTInfo = (Model,LOT,SMTRangeChecked,SMTStartRange,SMTEndRange,ParseLog)
    Dim ShiftCounterInfo As New ArrayList() 'ShiftCounterInfo = (ShiftCounterID,ShiftCounter,LOTCounter)
    Dim SNBufer As ArrayList 'SNBufer = (BooLSMT (Занят или свободен),SMTSN,BooLFAS (Занят или свободен),FASSN )
    Dim StepSequence As String()
    Dim PCBID, SNID As Integer
    Dim SNFormat As ArrayList

    Private Sub WorkForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SNBufer = New ArrayList()
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
                        "PreRackStage = " & LOTInfo(18)

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
            where P.LOTID = 20059 and BoxNum = 5  and LiterID = 9
            order by UnitNum desc", ds)
        End If
        'where P.LOTID = " & LOTID & " And BoxNum = " & LastPackCounter(1) & " And LiterID = " & PCInfo(8) & "

    End Sub


    Dim TableColumn As ArrayList = New ArrayList() From {13, "SMT номер", "FAS номер", "Литера", 5, 22, Date.Now}
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim row = ds.Tables(0).NewRow()
        Dim i = 0
        For Each item In TableColumn
            row.Item(i) = item
            i += 1
        Next
        ds.Tables(0).Rows.Add(row)
        DG_Packing.DataSource = ds
        DG_Packing.Sort(DG_Packing.Columns(0), System.ComponentModel.ListSortDirection.Descending)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        ds.Clear()
    End Sub

    Private Sub BT_ClearSN_Click(sender As Object, e As EventArgs) Handles BT_ClearSN.Click
        SerialTextBox.Clear()
        SerialTextBox.Enabled = True
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
            'TextBox3.Text = "UserID = " & UserInfo(0) & vbCrLf &
            '            "Name = " & UserInfo(1) & vbCrLf &
            '            "User Group = " & UserInfo(2) & vbCrLf  'UserInfo
            SerialTextBox.Focus()
        ElseIf e.KeyCode = Keys.Enter Then
            TB_RFIDIn.Clear()
        End If
    End Sub 'регистрация пользователя
    ' условия для возврата в окно настроек
    Dim OpenSettings As Boolean
    Private Sub Button_Click(sender As Object, e As EventArgs) Handles BT_OpenSettings.Click, BT_LOGInClose.Click
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
            If GetFTSN(LOTInfo(12)) = True Then
                If CheckRange(SNFormat) = True Then
                    CheckDublicate(SerialTextBox.Text, GetPcbID(SNFormat))
                End If
            End If
            'если введен не верный номер
        ElseIf e.KeyCode = Keys.Enter And (SerialTextBox.TextLength = 1 Or SerialTextBox.TextLength = 1) Then
            PrintLabel(Controllabel, SerialTextBox.Text & " не верный номер", 12, 193, Color.Red)
            'CurrentLogUpdate(Label_ShiftCounter.Text, SerialTextBox.Text, "Ошибка", "", "Плата имеет не верный номер")
            SerialTextBox.Enabled = False
            'BT_Pause.Focus()
        End If
    End Sub


    '1. Определение формата номера
    Private Function GetFTSN(SingleSN As Boolean) As Boolean
        SNFormat = New ArrayList()
        SNFormat = GetSNFormat(LOTInfo(3), LOTInfo(8), SerialTextBox.Text)
        'SNFormat(0) ' Результат проверки True/False
        'SNFormat(1) ' 1 - SMT/ 2 - FAS / 3 - Неопределен
        'SNFormat(2) ' Переменный номер
        'SNFormat(3) ' Текст сообщения
        'SNFormat(4) ' Координата X
        'SNFormat(5) ' Координата Y
        'SNFormat(6) ' Color
        'SNFormat(7) ' SerialTextBox.Enabled  - True/False
        If SNFormat(0) = True Then
            If SingleSN = False Then
                If SNBufer.Count = 0 Then
                    SNBufer = New ArrayList()
                End If
            End If
        Else
            PrintLabel(Controllabel, SNFormat(3), SNFormat(4), SNFormat(5), SNFormat(6))
            SerialTextBox.Enabled = SNFormat(7)
        End If
        Return SNFormat(0)
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
            If StartRange >= SNFormat(2) And SNFormat(2) <= EndRange Then
                res = True
            Else
                res = False
                PrintLabel(Controllabel, "Номера " & SerialTextBox.Text & vbCrLf & " вне диапазона выбранного лота!", 12, 193, Color.Red)
                SerialTextBox.Enabled = False
            End If
        Else
            res = True
        End If
        Return res
    End Function



    '3 поиск ID PCB в базе гравировщика
    Private Function GetPcbID(SNFormat As ArrayList) As ArrayList
        Dim Res As New ArrayList()
        Select Case SNFormat(1)
            Case 1
                PCBID = SelectInt("USE SMDCOMPONETS SELECT [IDLaser] FROM [SMDCOMPONETS].[dbo].[LazerBase] where Content = '" & SerialTextBox.Text & "'")
                Res.Add(PCBID <> 0)
                Res.Add(PCBID)
                Res.Add(SNFormat(1))
                Res.Add(If(PCBID = 0, "Плата " & SerialTextBox.Text & " не зарегистрирована", ""))
                Res.Add(12)
                Res.Add(193)
                Res.Add(Color.Red)
                Res.Add(False)
            Case 2
                SNID = SelectInt("USE FAS SELECT [ID] FROM [FAS].[dbo].[Ct_FASSN_reg] where SN = '" & SerialTextBox.Text & "'")

                Res.Add(SNID <> 0)
                Res.Add(SNID)
                Res.Add(SNFormat(1))
                Res.Add(If(SNID = 0, "Плата " & SerialTextBox.Text & " не зарегистрирована", ""))
                Res.Add(12)
                Res.Add(193)
                Res.Add(Color.Red)
                Res.Add(False)
        End Select
        PrintLabel(Controllabel, Res(3), Res(4), Res(5), Res(6))
        SerialTextBox.Enabled = Res(7)
        Return Res
    End Function
    '4. Проверка предыдущего шага и дубликатов
    Private Function CheckDublicate(SN As String, GetPCB_SNID As ArrayList) As Boolean
        Dim Res As Boolean, SQL As String, Mess As String, Col As Color
        'Проверка предыдущего шага
        Select Case GetPCB_SNID(2)
            Case 1
                Dim PCBStepRes As New ArrayList(SelectListString("USE FAS SELECT [StepID],[TestResult],[ScanDate],[SNID]
                            FROM [FAS].[dbo].[Ct_StepResult] where [PCBID] = " & GetPCB_SNID(1)))
                Res = If(PCBStepRes.Count <> 0, (PCBStepRes(0) = PreStepID And PCBStepRes(1) = 2), False)
                Mess = If(Res = False, "Плата " & SerialTextBox.Text & " имеет не верный предыдущий шаг!", "")

            Case 2
                Res = (SNBufer.Count = 0)
                Mess = If(Res = False, "Плата " & SerialTextBox.Text & " имеет не верный предыдущий шаг!", "")

        End Select

        'проверка случайного сканирования номера повторно
        If Res = True Then

            If DG_Packing.RowCount > 0 Then
                For j = 0 To DG_Packing.RowCount - 1
                    If SN = DG_Packing.Item(1, j).Value Or SN = DG_Packing.Item(2, j).Value Then
                        Res = False
                        PrintLabel(Controllabel, SN & " номер уже был " & vbCrLf & "отсканирован в этой коробке!", 26, 198, Color.Red)
                        DG_Packing.BackgroundColor = Color.Red
                        SerialTextBox.Enabled = False
                        Exit For
                    Else
                        Res = True
                    End If
                Next

            Else
                Res = True
            End If
            If Res = True Then
                Select Case GetPCB_SNID(2)
                    Case 1
                        SQL = "Use FAS SELECT L.Content,S.SN,Lit.LiterName + cast ([LiterIndex] as nvarchar),[PalletNum],[BoxNum],[UnitNum],[PackingDate],U.UserName
                        FROM [FAS].[dbo].[Ct_PackingTable] as P
                        left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = P.PCBID
                        Left join Ct_FASSN_reg as S On S.ID = P.SNID
                        Left join FAS_Liter as Lit On Lit.ID = P.LiterID
                        Left join FAS_Users as U On U.UserID = P.UserID
                        where PCBID = " & GetPCB_SNID(1)
                        Res = (SelectListString(SQL).Count = 0)
                    Case 2
                        SQL = "Use FAS SELECT L.Content,S.SN,Lit.LiterName + cast ([LiterIndex] as nvarchar),[PalletNum],[BoxNum],[UnitNum],[PackingDate],U.UserName
                        FROM [FAS].[dbo].[Ct_PackingTable] as P
                        left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = P.PCBID
                        Left join Ct_FASSN_reg as S On S.ID = P.SNID
                        Left join FAS_Liter as Lit On Lit.ID = P.LiterID
                        Left join FAS_Users as U On U.UserID = P.UserID
                        where SNID = " & GetPCB_SNID(1)
                        Res = (SelectListString(SQL).Count = 0)
                End Select
            End If
        End If

        Col = If(Res = False, Color.Red, Color.Green)
        PrintLabel(Controllabel, Mess, 12, 193, Col)
        SerialTextBox.Enabled = Res
        Return Res
    End Function




End Class