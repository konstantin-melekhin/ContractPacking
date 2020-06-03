Imports Library3


Public Class WorkForm
    Dim LOTID, IDApp, UnitCounter As Integer
    Public Sub New(LOTID As Integer, IDApp As Integer)
        InitializeComponent()
        Me.LOTID = LOTID
        Me.IDApp = IDApp
    End Sub

    Dim LenSN, StartStepID As Integer, PreStepID As Integer, NextStepID As Integer
    Dim StartStep As String, PreStep As String, NextStep As String
    Dim PCInfo As New ArrayList() 'PCInfo = (App_ID, App_Caption, lineID, LineName, StationName,CT_ScanStep)
    Dim LOTInfo As New ArrayList() 'LOTInfo = (Model,LOT,SMTRangeChecked,SMTStartRange,SMTEndRange,ParseLog)
    Dim ShiftCounterInfo As New ArrayList() 'ShiftCounterInfo = (ShiftCounterID,ShiftCounter,LOTCounter)
    Dim StepSequence As String()
    Dim Yield As Double
    Private Sub WorkForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'получение данных о станции
        LoadGridFromDB(DG_StepList, "USE FAS SELECT [ID],[StepName],[Description] FROM [FAS].[dbo].[Ct_StepScan]")
        PCInfo = GetPCInfo(IDApp)
        LabelAppName.Text = PCInfo(7)
        Label_StationName.Text = PCInfo(5)
        Lebel_StationLine.Text = PCInfo(3)
        'TextBox1.Text = "App_ID = " & PCInfo(0) & vbCrLf &
        '                "App_Caption = " & PCInfo(1) & vbCrLf &
        '                "lineID = " & PCInfo(2) & vbCrLf &
        '                "LineName = " & PCInfo(3) & vbCrLf &
        '                "StationID = " & PCInfo(4) & vbCrLf &
        '                "StationName = " & PCInfo(5) & vbCrLf &
        '                "CT_ScanStepID = " & PCInfo(6) & vbCrLf &
        '                "CT_ScanStep = " & PCInfo(7) & vbCrLf 'PCInfo
        '                "LiterID " & PCInfo(8) & vbCrLf &
        '                "LiterName = " & PCInfo(9)
        'получение данных о текущем лоте
        LOTInfo = GetCurrentContractLot(LOTID)
        LenSN = GetLenSN(LOTInfo(2))
        'TextBox2.Text = "Model = " & LOTInfo(0) & vbCrLf &
        '                "LOT = " & LOTInfo(1) & vbCrLf &
        '                "SMTNumberFormat = " & LOTInfo(2) & vbCrLf &
        '                "SMTRangeChecked = " & LOTInfo(3) & vbCrLf &
        '                "SMTStartRange = " & LOTInfo(4) & vbCrLf &
        '                "SMTEndRange = " & LOTInfo(5) & vbCrLf &
        '                "ParseLog = " & LOTInfo(6) & vbCrLf &
        '                "StepSequence = " & LOTInfo(7) & vbCrLf &
        '                "LenSN = " & LenSN & vbCrLf 'LOTInfo & vbCrLf &
        '                "BoxCapacity = " & LOTInfo(8) & vbCrLf &
        '                "PalletCapacity = " & LOTInfo(9) & vbCrLf &
        '                "LiterIndex = " & LOTInfo(10)

        'Определить стартовый шаг, текущий и последующий
        StepSequence = New String(Len(LOTInfo(7)) / 2 - 1) {}
        For i = 0 To Len(LOTInfo(7)) - 1 Step 2
            Dim J As Integer
            StepSequence(J) = Mid(LOTInfo(7), i + 1, 2)
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
        L_BoxCapacity.Text = LOTInfo(8)
        L_PalletCapacity.Text = LOTInfo(9)
        L_Liter.Text = If(LOTInfo(10) = 0, PCInfo(9), PCInfo(9) & " " & LOTInfo(10))
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

        'If LOTInfo(8) <> LastPackCounter(2) Then
        LoadGridFromDB(DG_Packing, "use FAS
            SELECT UnitNum as '№',l.Content AS 'SMT Номер',SN.SN AS 'FAS Номер',(Lit.LiterName + ' ' + cast(LiterIndex as nvarchar (5))) AS 'Литера' 
            ,PalletNum as 'Паллет', BoxNum as 'Групповая', Format(PackingDate,'dd.MM.yyyy HH:mm:ss') as 'Дата'
            FROM [FAS].[dbo].[Ct_PackingTable] as P
            Left join SMDCOMPONETS.dbo.LazerBase as L On L.IDLaser = p.PCBID
            Left join [FAS].[dbo].Ct_FASSN_reg as Sn On Sn.ID = p.SNID
            Left join [FAS].[dbo].FAS_Liter as Lit On Lit.ID = p.LiterID
            where P.LOTID = 20059 and BoxNum = 5  and LiterID = 9
            order by UnitNum desc")
        'End If
        'where P.LOTID = " & LOTID & " And BoxNum = " & LastPackCounter(1) & " And LiterID = " & PCInfo(8) & "

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
End Class