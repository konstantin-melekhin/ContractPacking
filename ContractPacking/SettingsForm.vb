Imports Library3
Imports System.Deployment.Application
Public Class SettingsForm
    ReadOnly IDApp As Integer = 11
    Dim CustamerID As Integer = 4
    Dim PCInfo As New ArrayList() 'PCInfo = (App_ID, App_Caption, lineID, LineName, StationName,CT_ScanStep)
    Private Sub SettingsForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim myVersion As Version
        If ApplicationDeployment.IsNetworkDeployed Then
            myVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion
        End If
        LB_SW_Wers.Text = String.Concat("v", myVersion)

        PCInfo = GetPCInfo(IDApp)
        If PCInfo.Count = 0 Then
            DG_LOTListPresent.Visible = False
            ShowLineSettings()
        Else
            LabelAppName.Text = PCInfo(1)
            Label_StationName.Text = PCInfo(5)
            LB_CurrentStep.Text = PCInfo(7)
            Lebel_StationLine.Text = PCInfo(3)
            TextBox1.Text = "App_ID = " & PCInfo(0) & vbCrLf &
                            "App_Caption = " & PCInfo(1) & vbCrLf &
                            "lineID = " & PCInfo(2) & vbCrLf &
                            "LineName = " & PCInfo(3) & vbCrLf &
                            "StationID = " & PCInfo(4) & vbCrLf &
                            "StationName = " & PCInfo(5) & vbCrLf &
                            "CT_ScanStepID = " & PCInfo(6) & vbCrLf &
                            "CT_ScanStep = " & PCInfo(7) & vbCrLf
        End If
        'загружаем список лотов в грид
        GetLotList_ContractStation(DG_LotList, CustamerID)
        GetLotList()
    End Sub 'Загрузка формы настроек
    Private Sub GetLotList()
        For i = 0 To DG_LotList.RowCount - 1
            DG_LOTListPresent.Rows.Add(DG_LotList.Item(0, i).Value, DG_LotList.Item(1, i).Value,
                                       DG_LotList.Item(2, i).Value, DG_LotList.Item(3, i).Value)
        Next
        DG_LOTListPresent.Sort(DG_LOTListPresent.Columns(3), System.ComponentModel.ListSortDirection.Descending)
    End Sub 'Запись списка лотов в LOT List Presenter
    'Обновление списка лотов
    Private Sub BT_RefreshLOT_Click(sender As Object, e As EventArgs) Handles BT_RefreshLOT.Click
        DG_LOTListPresent.Rows.Clear()
        GetLotList_ContractStation(DG_LotList, 4)
        GetLotList()
    End Sub 'Обновление списка лотов
    '_______________________________________________________________________________________________________________
    'Модуль настройки линий
    Private LineID, StepID As Integer
    Private Sub BT_SaveLine_Click(sender As Object, e As EventArgs) Handles BT_SaveLine.Click
        If CB_Line.Text = "" Or CB_Steps.Text = "" Then
            MsgBox(Prompt:="Выберите номер линии и название требуемой операции!")
        Else
            'определяем id выбранной линии
            For J = 0 To DG_LineList.Rows.Count - 1
                If CB_Line.Text = DG_LineList.Rows(J).Cells(1).Value Then
                    LineID = DG_LineList.Rows(J).Cells(0).Value
                    Exit For
                End If
            Next
            'определяем id выбранной операции
            For J = 0 To DG_Steps.Rows.Count - 1
                If CB_Steps.Text = DG_Steps.Rows(J).Cells(1).Value Then
                    StepID = DG_Steps.Rows(J).Cells(0).Value
                    Exit For
                End If
            Next
            'если приложение найдено, то обновляем только номер линии
            If DG_AppList.Rows.Count <> 0 Then
                RunCommand("use FAS
            update [FAS].[dbo].[FAS_App_ListForPC]  set LineID = " & LineID & ", CreateDate = CURRENT_TIMESTAMP , 
            [CT_ScanStep] = " & StepID & "
            where StationID = " & PCInfo(4) & " and App_ID = " & IDApp)
            Else
                'если не найдено, то создаем новую запись с названием этого ПК и приложения
                RunCommand("use FAS
            insert into [FAS].[dbo].[FAS_App_ListForPC] (App_ID,LineID,StationID,CreateDate,[CT_ScanStep]) 
            values (" & IDApp & "," & LineID & "," & GetStationID() & ",CURRENT_TIMESTAMP, " & StepID & ")")
            End If
            'обновляем список приложений для данного ПК
            AppRefresh()
        End If
    End Sub

    'список приложений для данного ПК
    Private Sub AppRefresh()
        LoadGridFromDB(DG_AppList, "use FAS
            SELECT Ap.App_Caption,L.LineName,St.StationName,Step.StepName
              FROM [FAS].[dbo].[FAS_App_ListForPC] as List
              left join [FAS].[dbo].[FAS_Applications] as Ap On Ap.App_ID = List.App_ID
              left join [FAS].[dbo].[FAS_Stations] as St On St.StationID = List.StationID
              left join [FAS].[dbo].[FAS_Lines] as L ON l.LineID = List.lineID
              left join [FAS].[dbo].[Ct_StepScan] as Step On Step.ID = CT_ScanStep
              where  List.StationID = '" & GetStationID() & "' and List.App_ID = " & IDApp)

    End Sub

    'Обработка кнопки настройки линии
    Private Sub LB_SelectLine_Click(sender As Object, e As EventArgs) Handles LB_SelectLine.Click
        ShowLineSettings()
        ''Загружаем  данные из таблице App_List_For_PC 
        AppRefresh()
    End Sub
    'Модуль вызова окна настройки линий
    Private Sub ShowLineSettings()
        GB_SelectLine.Visible = True
        GB_SelectLine.Location = New Point(13, 13)
        GB_SelectLine.Size = New Size(1192, 565)
        'загружаем список линий
        LoadGridFromDB(DG_LineList, PackLineList)
        'Выводим названия линий FAS в combobox
        If DG_LineList.Rows.Count <> 0 Then
            For J = 0 To DG_LineList.Rows.Count - 1
                CB_Line.Items.Add(DG_LineList.Rows(J).Cells(1).Value)
            Next
        Else
            MsgBox("Список линий не сформирован, возможно проблемы с сетью!" & vbCr & "Попробуйте перезапустить приложение")
        End If
        'загружаем список операций 
        LoadGridFromDB(DG_Steps, "SELECT [ID],[StepName],[Description] FROM [FAS].[dbo].[Ct_StepScan] where ID = 6")
        'Выводим названия линий FAS в combobox
        If DG_Steps.Rows.Count <> 0 Then
            For i = 0 To DG_Steps.Rows.Count - 1
                CB_Steps.Items.Add(DG_Steps.Rows(i).Cells(1).Value)
            Next
            CB_Steps.Text = DG_Steps.Rows(0).Cells(1).Value
        Else
            MsgBox("Список линий не сформирован, возможно проблемы с сетью!" & vbCr & "Попробуйте перезапустить приложение")
        End If
    End Sub
    'Возврат к настройкам станции
    Private Sub BT_CloseLineSet_Click(sender As Object, e As EventArgs) Handles BT_CloseLineSet.Click
        GB_SelectLine.Visible = False
        Dim frm = New SettingsForm
        frm.Show()
        Me.Close()
    End Sub 'Модуль настройки линий
    '_______________________________________________________________________________________________________________________________
    'Модуль запуска программы (переход в WorkForm)
    'Опредеяем номер выбранной строки в таблице лотов
    Public selRowNum, LOTID As Integer
    Private Sub DG_LOTListPresent_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles DG_LOTListPresent.CellClick
        selRowNum = DG_LOTListPresent.CurrentCell.RowIndex
    End Sub
    'Обработка кнопки запуск
    Private Sub BT_SelectLot_Click(sender As Object, e As EventArgs) Handles BT_SelectLot.Click
        'определяем LOTCode и LOTID
        If DG_LOTListPresent.Rows.Count <> 0 Then
            LOTID = DG_LOTListPresent.Item(3, selRowNum).Value
            'Dim WF As New WF_NS220(LOTID, IDApp)
            'Dim WF As New WF_SberDevice(LOTID, IDApp)
            Dim WF As New Aqarius_AQB365MC(LOTID, IDApp)
            'Dim WF As New WF_PackWithOutPrint(LOTID, IDApp) 'текущая программа для упаковки контрактных плат
            'Dim WF As New WF_WihtOutLaser(LOTID, IDApp)
            WF.Controllabel.Text = ""
            WF.Show()
            Me.Close()
        Else
            MsgBox("Список ЛОТов отсутствует!")
            Exit Sub
        End If
    End Sub 'Модуль запуска программы (переход в WorkForm)

End Class
