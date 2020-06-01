Imports Library3


Public Class WorkForm
    Dim LOTID, IDApp As Integer
    Public Sub New(LOTIDWF As Integer, IDApp As Integer)
        InitializeComponent()
        Me.LOTID = LOTIDWF
        Me.IDApp = IDApp
    End Sub

    Dim BoxNumber, PalletNumber, BoxCapacity, PalletCapacity, LineID, StationID As Integer
    Dim ModelName, LineNumber, Liter, LiterID, LiterIndex, UnitNumber, PCBID As String
    Private Sub WorkForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load























        'Устанавливаем дефолты при загоузке формы 
        'Переносим константы из формы настроек в рабочую форму. Данные получаем из таблицы M_Lots
        DG_CheckSN.Visible = False
        Controllabel.Text = ""
        IDApp = SettingsForm.IDApp
        L_AppName.Text = SettingsForm.IDApp
        StationID = SettingsForm.StationID
        L_StationName.Text = SettingsForm.Label_StationName.Text
        L_Line.Text = SettingsForm.Lebel_StationLine.Text
        LineID = SettingsForm.LineID
        LineNumber = L_Line.Text
        L_LOT.Text = SettingsForm.LOTCode
        L_FullLot.Text = SettingsForm.FullLot
        L_Model.Text = SettingsForm.Model
        ModelName = L_Model.Text
        L_BoxCapacity.Text = SettingsForm.BoxCapacity
        BoxCapacity = L_BoxCapacity.Text
        L_PalletCapacity.Text = SettingsForm.PalletCapacity
        PalletCapacity = L_PalletCapacity.Text
        LOTID = SettingsForm.LOTID
        Liter = SelectString("use fas select LiterName FROM [FAS].[dbo].[FAS_Liter] where Description = '" & LineNumber & "'")
        LiterID = SelectString("use fas select ID FROM [FAS].[dbo].[FAS_Liter] where Description = '" & LineNumber & "'")
        LiterIndex = SettingsForm.LitIndex
        If SettingsForm.LitIndex = 0 Then
            L_Liter.Text = Liter
        Else
            L_Liter.Text = Liter & LiterIndex
            Liter = L_Liter.Text
        End If
        '-------------------------------------------------------
        'Количество ранее упакованных приемников для выбранной линии забираем из базы
        Sql = "use fas
        SELECT PalletCounter
        FROM [FAS].[dbo].[FAS_PackingCounter] where lotid = " & LOTID & " and LineID=" & LineID
        'если запрос не находит в базе данных для текущего лота и линии, то создает начальную запись в базе (единицы)
        If SelectString(Sql) = "" Then
            Sql = "use fas
            insert into [FAS].[dbo].[FAS_PackingCounter] (PalletCounter,BoxCounter,UnitCounter,LineID,LOTID) values (1,1,1," & LineID & "," & LOTID & ")"
            RunCommand(Sql)
            BoxNumber = 1
            BoxNum.Text = BoxNumber
            NextBoxNum.Text = BoxNumber + 1
            PalletNumber = 1
            PalletNum.Text = PalletNumber
            UnitNumber = 1
        Else
            'если данные найдены, то выгружаем их в грид для обработки
            Sql = "use fas
            SELECT PalletCounter,BoxCounter,UnitCounter,LineID,LOTID 
            FROM [FAS].[dbo].[FAS_PackingCounter] where lotid = " & LOTID & " and LineID=" & LineID
            LoadGridFromDB(DG_PackingCounter, Sql)
            BoxNumber = DG_PackingCounter.Rows(0).Cells(1).Value
            BoxNum.Text = BoxNumber
            NextBoxNum.Text = BoxNumber + 1
            PalletNumber = DG_PackingCounter.Rows(0).Cells(0).Value
            PalletNum.Text = PalletNumber
            UnitNumber = DG_PackingCounter.Rows(0).Cells(2).Value
            'если в базе указана последняя коробка не полная, то выполняется условие
            If UnitNumber <> BoxCapacity Then
                Sql = "USE FAS
                SELECT UnitNum as '№', SN.FullSTBSN as Serial, (lit.LiterName + format (pack.LiterIndex, ''))as Litera,  BoxNum as GroupBox, PalletNum as Pallet
                ,FORMAT(Pack.PackingDate,'dd.MM.yyyy HH:mm:ss') as ScanDate
                FROM [FAS].[dbo].[FAS_PackingGS] as Pack
                left join FAS_Start as SN ON SN.SerialNumber = Pack.SerialNumber
                Left join FAS_Liter as Lit on Lit.ID = Pack.LiterID
                where PalletNum = " & PalletNumber & " and BoxNum = " & BoxNumber & " and Pack.LiterID = " & LiterID & " and pack.LiterIndex = " & SettingsForm.LitIndex & " and Pack.LOTID = " & LOTID & "
                order by UnitNum desc"
                LoadGridFromDB(DG_NotFullBox, Sql)
                If DG_NotFullBox.Rows.Count <> 0 Then
                    Dim U As Integer
                    Dim S, L, B, P, D As String
                    For j = 0 To DG_NotFullBox.RowCount - 1
                        U = DG_NotFullBox.Rows(j).Cells(0).Value
                        S = DG_NotFullBox.Rows(j).Cells(1).Value
                        L = DG_NotFullBox.Rows(j).Cells(2).Value
                        B = DG_NotFullBox.Rows(j).Cells(3).Value
                        P = DG_NotFullBox.Rows(j).Cells(4).Value
                        D = DG_NotFullBox.Rows(j).Cells(5).Value
                        Me.DG_Packing.Rows.Add(U, S, L, B, P, D)
                        'сортировка griв по выбранному столбцу
                        DG_Packing.Sort(DG_Packing.Columns(0), System.ComponentModel.ListSortDirection.Descending)
                    Next
                Else
                    MsgBox("Не полная коробка не сформирована!, перезапустите приложение!")
                End If
            End If
        End If
        '___________________________________________________________
        GB_UserData.Location = New Point(10, 12)
        TB_RFIDIn.Focus()
        'запуск счетчика продукции за день
        GetTimeSec()
        ShiftCounterStart(CurentTimeSec, StationID, IDApp)
        ShiftCounterID = ShiftPapameters(0)
        ShiftCounter = ShiftPapameters(1)
        Label_ShiftCounter.Text = ShiftCounter
    End Sub

    'регистрация пользователя
    Dim RFID As String
    Private Sub TB_RFIDIn_KeyDown(sender As Object, e As KeyEventArgs) Handles TB_RFIDIn.KeyDown
        TB_RFIDIn.MaxLength = 10
        If e.KeyCode = Keys.Enter And TB_RFIDIn.TextLength = 10 Then ' если длина номера равна 10, то запускаем процесс
            GetUserData(TB_RFIDIn.Text, GB_UserData, GB_WorkAria, L_UserName, TB_RFIDIn)
            UserID = UserData(0)
            UserGroup = UserData(1)
            SerialTextBox.Focus()
        ElseIf e.KeyCode = Keys.Enter Then
            TB_RFIDIn.Clear()
        End If
    End Sub

    ' условия для возврата в окно настроек
    Private Sub BT_OpenSettings_Click(sender As Object, e As EventArgs) Handles BT_OpenSettings.Click, BT_LogOut.Click
        If MsgBox("Вы уверены в том, что собираетесь выйти из прриложения?", vbYesNo) = vbYes Then
            SettingsForm.Show()
            Me.Close()
        End If
    End Sub
    '' условия для выхода из приложения
    Private Sub BT_CloseApp_Click(sender As Object, e As EventArgs) Handles BT_CloseApp.Click
        If MsgBox("Вы уверены в том, что собираетесь выйти из прриложения?", vbYesNo) = vbYes Then
            Me.Close()
        End If
    End Sub
    'запуск таймера
    Private Sub CurrentTimeTimer_Tick(sender As Object, e As EventArgs) Handles CurrentTimeTimer.Tick
        CurrentTimeTimer.Start()
        CurrrentTimeLabel.Text = TimeString
    End Sub
    Dim CurentTimeSec As Integer

    Private Sub GetTimeSec()
        CurrentTimeTimer.Start()
        CurrrentTimeLabel.Text = TimeString
        CurentTimeSec = CurrrentTimeLabel.Text.Substring(0, 2) * 3600 + CurrrentTimeLabel.Text.Substring(3, 2) * 60 + CurrrentTimeLabel.Text.Substring(6, 2)
    End Sub
    '

    'условие счетчика единичной продукции
    Dim GridCounter As Integer
    Private Sub UnitCounter()
        ScanDateLabel.Text = Now ' записываем дату сканирования в окно программы
        UnitNumber = DG_Packing.RowCount + 1
        GridCounter = UnitNumber
        ' заполняем строку таблицы
        Me.DG_Packing.Rows.Add(GridCounter, SerialTextBox.Text, Liter, BoxNum.Text, PalletNum.Text, ScanDateLabel.Text)
        If DG_Packing.Rows.Count <> 0 Then
            DG_Packing.Sort(DG_Packing.Columns(0), System.ComponentModel.ListSortDirection.Descending)
        End If
        If DG_Packing.RowCount = BoxCapacity Then
            DG_Packing.BackgroundColor = Color.Green
        Else
            DG_Packing.BackgroundColor = Color.Gold
        End If
    End Sub
    'условие счета групповых коробок и паллет
    Private Sub BoxAndPalletCounter()
        UnitNumber = 1
        GridCounter = UnitNumber
        BoxNumber = BoxNumber + 1
        BoxNum.Text = BoxNumber
        NextBoxNum.Text = BoxNumber + 1
        DG_Packing.RowCount = 0
        DG_Packing.BackgroundColor = Color.Gold
        Me.DG_Packing.Rows.Add(GridCounter, SerialTextBox.Text, Liter, BoxNum.Text, PalletNum.Text, ScanDateLabel.Text)
        'Далеее запускаем считалку паллет 
        If BoxNum.Text Mod PalletCapacity = 1 Then
            PalletNumber = PalletNumber + 1
            PalletNum.Text = PalletNumber
        End If
    End Sub

    'окно ввода серийного номера
    Private Sub SerialTextBox_KeyDown(sender As Object, e As KeyEventArgs) Handles SerialTextBox.KeyDown
        IsUsed = False And IsActiv = False And IsUploaded = False And IsPacked = False
        DG_CheckSN.Visible = False
        'Обрабатываем нажатие  Enter и  ввода серийного номера в поле сериного номера со всеми условиями согласно настрое лота
        If e.KeyCode = Keys.Enter And DG_Packing.RowCount < BoxCapacity And SerialTextBox.TextLength = 23 Then
            If CheckCurrentDublicate(SerialTextBox.Text) = True Then
                If CheckSN(Mid(SerialTextBox.Text, 16)) = True Then
                    UnitCounter()
                    WriteToDB(Mid(SerialTextBox.Text, 16))
                    PrintLabel(Controllabel, SerialTextBox.Text & " номер успешно " & vbCrLf & "добавлен!", 26, 198, Color.Green)
                End If
            End If
            ' условие инкримента номера групповой коробки (групповая заполнилась)
        ElseIf e.KeyCode = Keys.Enter And DG_Packing.RowCount = BoxCapacity And SerialTextBox.TextLength = 23 Then
            If CheckCurrentDublicate(SerialTextBox.Text) = True Then
                If CheckSN(Mid(SerialTextBox.Text, 16)) = True Then
                    BoxAndPalletCounter()
                    WriteToDB(Mid(SerialTextBox.Text, 16))
                    PrintLabel(Controllabel, SerialTextBox.Text & " номер успешно " & vbCrLf & "добавлен!", 26, 198, Color.Green)
                End If
            End If
            ' условие если длина номера не соответствует заданной длине.
        ElseIf e.KeyCode = Keys.Enter Then
            PrintLabel(Controllabel, SerialTextBox.Text & " не верный номер!", 26, 198, Color.Red)
            DG_Packing.BackgroundColor = Color.Red
        End If
        SerialTextBox.Clear()
    End Sub

    Private Function CheckCurrentDublicate(SN As String) As Boolean
        'проверка случайного сканирования номера повторно
        Dim Res As Boolean
        If DG_Packing.RowCount > 0 Then
            For j = 0 To DG_Packing.RowCount - 1
                If SN = DG_Packing.Rows(j).Cells(1).Value Then
                    Res = False
                    PrintLabel(Controllabel, SN & " номер уже был " & vbCrLf & "отсканирован в этой коробке!", 26, 198, Color.Red)
                    DG_Packing.BackgroundColor = Color.Red
                    Exit For
                Else
                    Res = True
                End If
            Next
        Else
            Res = True
        End If
        Return Res
    End Function
    ' Проверка введенного полного серийного номера по короткому номеру (последние 8 знаков)!!!!
    Dim IsUsed, IsActiv, IsUploaded, IsWeighted, IsPacked, inRepair As Boolean
    Private Function CheckSN(SN As String) As Boolean
        Dim Res As Boolean
        'поиск отсканированного номера в базе и определение статуса чекпоинтов
        Sql = "Use FAS 
        SELECT [IsUsed],[IsActive],[IsUploaded],[IsWeighted],[IsPacked],[InRepair], ST.PCBID
        FROM [FAS].[dbo].[FAS_SerialNumbers] as SN
        left join FAS_Start as ST On ST.SerialNumber = SN.SerialNumber
        where LOTID = " & LOTID & " and SN.SerialNumber =" & SN
        LoadGridFromDB(DG_CheckSN, Sql)
        If DG_CheckSN.Rows.Count = 0 Then
            Res = False
        Else
            IsUsed = DG_CheckSN.Rows(0).Cells(0).Value
            IsActiv = DG_CheckSN.Rows(0).Cells(1).Value
            IsUploaded = DG_CheckSN.Rows(0).Cells(2).Value
            IsWeighted = DG_CheckSN.Rows(0).Cells(3).Value
            IsPacked = DG_CheckSN.Rows(0).Cells(4).Value
            inRepair = DG_CheckSN.Rows(0).Cells(5).Value
            PCBID = DG_CheckSN.Rows(0).Cells(6).Value
        End If
        'проверка статусов чекпоинтов
        If IsUsed = True And IsActiv = True And IsUploaded = True And IsWeighted = True And IsPacked = False And inRepair = False Then
            Res = True
        ElseIf IsUsed = True And IsActiv = False And IsPacked = False And inRepair = True Then
            Res = False
            PrintLabel(Controllabel, SN & vbCrLf & "находится в ремонте!", 26, 198, Color.Red)
            DG_Packing.BackgroundColor = Color.Red
        ElseIf IsUsed = True And IsActiv = True And IsUploaded = False And IsPacked = False And inRepair = False Then
            Res = False
            PrintLabel(Controllabel, SN & vbCrLf & "не прошит в приемник!", 26, 198, Color.Red)
            DG_Packing.BackgroundColor = Color.Red
        ElseIf IsUsed = True And IsActiv = True And IsUploaded = True And IsWeighted = False And IsPacked = False And inRepair = False Then
            Res = False
            PrintLabel(Controllabel, SN & vbCrLf & "не прошед весовой контроль!", 26, 198, Color.Red)
            DG_Packing.BackgroundColor = Color.Red
        ElseIf IsUsed = True And IsActiv = True And IsUploaded = True And IsWeighted = True And IsPacked = True And inRepair = False Then
            Res = False
            Sql = "SELECT UnitNum as '№', SN.FullSTBSN as Serial, (lit.LiterName + format (pack.LiterIndex, ''))as Litera, PalletNum as Pallet, BoxNum as GroupBox,  Pack.UnitNum as Unit
                ,(FORMAT(Pack.PackingDate, 'dd.MM.yyyy HH.mm.ss'))as ScanDate
                FROM [FAS].[dbo].[FAS_PackingGS] as Pack
                left join FAS_Start as SN ON SN.SerialNumber = Pack.SerialNumber
                Left join FAS_Liter as Lit on Lit.ID = Pack.LiterID
                where pack.SerialNumber = " & SN
            LoadGridFromDB(DG_IsPacked, Sql)
            PrintLabel(Controllabel, "Номер " & DG_IsPacked.Rows(0).Cells(0).Value & " уже упакован!" & vbCrLf &
                       "Литер " & DG_IsPacked.Rows(0).Cells(1).Value & ", Паллет " & DG_IsPacked.Rows(0).Cells(2).Value &
                       ", Группоавая " & DG_IsPacked.Rows(0).Cells(3).Value & ", Приемник № " & DG_IsPacked.Rows(0).Cells(4).Value & vbCrLf &
                        "Дата упаковки " & DG_IsPacked.Rows(0).Cells(5).Value, 26, 198, Color.Red) ''Если что, то исправить шривт на 15
            DG_Packing.BackgroundColor = Color.Red
        Else
            Res = False
            DG_CheckSN.Location = New Point(19, 415)
            DG_CheckSN.Size = New Size(1047, 142)
            DG_CheckSN.Visible = True
        End If
        Return Res
    End Function

    'запись в базу при успешных проверках номера
    Private Sub WriteToDB(SN As String)
        Sql = "USE FAS
           insert into [FAS].[dbo].[FAS_PackingGS] ([SerialNumber],[LiterID],[LiterIndex],[PalletNum],[BoxNum],[UnitNum],[PackingDate],[PackingByID], [LOTID]) 
            values (" & SN & "," & LiterID & "," & LiterIndex & "," & PalletNumber & "," & BoxNumber & "," & UnitNumber &
            ",CURRENT_TIMESTAMP," & UserID & "," & LOTID & ")"
        RunCommand(Sql)
        Sql = "USE FAS update [FAS].[dbo].[FAS_PackingCounter] set PalletCounter = " & PalletNumber & ",BoxCounter = " & BoxNumber &
            ",UnitCounter = " & UnitNumber & " where LineID = " & LineID & " and LOTID = " & LOTID
        RunCommand(Sql)
        'обновление таблицы счетчика дневного выпуска на линии
        ShiftCounter = ShiftCounter + 1
        Label_ShiftCounter.Text = ShiftCounter
        ShiftCounterUpdate(ShiftCounter, ShiftCounterID)
        'Обновление SerialNumbers
        Sql = "use fas update [FAS].[dbo].[FAS_SerialNumbers] set IsPacked = 1 where SerialNumber = " & SN
        RunCommand(Sql)
        AddToOperLogFasEnd(PCBID, LineID, StationID, IDApp, UserID, SN)
    End Sub
End Class