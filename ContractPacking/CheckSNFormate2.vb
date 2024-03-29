﻿Public Module CheckSNFormate2
    'функция определения длины серийного номера
    Public Function GetLenSN(Format As String) As Integer
        Dim Coordinats As Integer() = New Integer(2) {}
        For i = 0 To 5 Step 2
            Dim J As Integer
            Coordinats(J) = Convert.ToInt32(Mid(Mid(Format, Len(Format) - 5), i + 1, 2), 16)
            J += 1
        Next
        Return (Coordinats(0) + Coordinats(1) + Coordinats(2))
    End Function
    'функция определения координат серийного номера
    Public Function GetCoordinats(Format As String) As Array
        Dim Coordinats As Integer() = New Integer(2) {}
        For i = 0 To 5 Step 2
            Dim J As Integer
            Coordinats(J) = Convert.ToInt32(Mid(Mid(Format, Len(Format) - 5), i + 1, 2), 16)
            J += 1
        Next
        Return Coordinats
    End Function
#Region " 'функция определения формата серийного номера для двух номеров "
    Public Function GetSNFormat2(FormatSMT As String, FormatFAS As String, SN As String, HexSN As Boolean, CheckSMTFormat As Boolean, CheckFASFormat As Boolean) As ArrayList
        Dim Coordinats() As Integer
        Dim Res As ArrayList = New ArrayList()
        Dim VarSN As Integer
        Dim ForCount As Integer = FormatSMT.Split(";").Count + 1
        ' i = 1 --Номер FAS, 1 < i < ForCount --Номер SMT, i = ForCount --Номер не определен

        For i = 1 To ForCount
            If i <> ForCount Then
                Dim SNBase As String
                If CheckSMTFormat = True And CheckFASFormat = True Then
                    Coordinats = GetCoordinats(If(i = 1, FormatFAS, FormatSMT.Split(";")(i - 2)))
                    SNBase = If(i = 1, FormatFAS, FormatSMT.Split(";")(i - 2))
                ElseIf CheckSMTFormat = False And CheckFASFormat = True Then
                    If i = 1 Then
                        Coordinats = GetCoordinats(FormatFAS)
                        SNBase = FormatFAS
                    End If
                ElseIf CheckSMTFormat = True And CheckFASFormat = False Then
                    If i > 1 Then
                        Coordinats = GetCoordinats(FormatSMT.Split(";")(i - 2))
                        SNBase = FormatSMT.Split(";")(i - 2)
                    End If
                End If
                'Coordinats = GetCoordinats(If(i = 1, FormatSMT, FormatFAS))
                'SNBase = If(i = 1, FormatSMT, FormatFAS)
                If Coordinats Is Nothing Then
                Else
                    Dim MascBase As String = Mid(SNBase, 1, Coordinats(0)) + Mid(SNBase, Coordinats(0) + Coordinats(1) + 1, Coordinats(2))
                    Dim MascSN As String = Mid(SN, 1, Coordinats(0)) + Mid(SN, Coordinats(0) + Coordinats(1) + 1, Coordinats(2))
                    If (MascBase = MascSN) = True And (Len(SN)) = (Coordinats(0) + Coordinats(1) + Coordinats(2)) Then
                        Res.Add(True) 'Res(0)
                        Res.Add(i) 'Res(1)
                        If i > 1 Or (i = 1 And HexSN = False) Then
                            Try
                                VarSN = Convert.ToInt32(Mid(SN, Coordinats(0) + 1, Coordinats(1)))
                            Catch ex As Exception
                                VarSN = 0
                            End Try
                        ElseIf i = 1 And HexSN = True Then
                            Try
                                VarSN = CInt("&H" & Mid(SN, Coordinats(0) + 1, Coordinats(1)))
                            Catch ex As Exception
                                VarSN = 0
                            End Try
                        End If
                        Res.Add(VarSN) 'Res(2)
                        Exit For
                    End If
                End If
            Else
                Res.Add(False) 'Res(0)
                Res.Add(i) 'Res(1)
                Res.Add(0) 'Res(2)
            End If
        Next

        Select Case Res(1)
            Case 1
                Res.Add("Формат номера " & SN & vbCrLf & "соответствует FAS!")
            Case ForCount
                Res.Add("Формат номера " & SN & vbCrLf & "не соответствует выбранному лоту!")
            Case 1 To ForCount
                Res.Add("Формат номера " & SN & vbCrLf & "соответствует SMT!") 'Res(3) ' Текст сообщения
        End Select
        Return Res
    End Function

#End Region

#Region " 'функция определения формата серийного номера для трех номеров "
    Public Function GetLOTSNFormat(FormatSMT As String) As String
        Dim Coordinats() As Integer = GetCoordinats(FormatSMT)
        Dim MascBase As String = Mid(FormatSMT, 1, Coordinats(0)) + Mid(FormatSMT, Coordinats(0) + Coordinats(1) + 1, Coordinats(2))
        Return MascBase
    End Function

    Public Function GetSNFormat(FormatSMT As String, FormatFAS As String, FormatFAS2 As String, SN As String, HexSN As Boolean, CheckSMTFormat As Boolean, CheckFASFormat As Boolean) As ArrayList
        Dim Coordinats() As Integer
        Dim Res As ArrayList = New ArrayList()
        Dim VarSN As Integer
        ' i = 1 --Номер SMT, i = 2 --Номер FAS, i = 3 --Номер FAS2, i = 4 --Номер не определен
        For i = 1 To 4
            If i <> 4 Then
                Dim SNBase As String
                If CheckSMTFormat = True And CheckFASFormat = True Then
                    Coordinats = GetCoordinats(If(i = 1, FormatSMT, If(i = 2, FormatFAS, FormatFAS2)))
                    SNBase = If(i = 1, FormatSMT, If(i = 2, FormatFAS, FormatFAS2))
                ElseIf CheckSMTFormat = False And CheckFASFormat = True Then
                    If i = 2 Then
                        Coordinats = GetCoordinats(FormatFAS)
                        SNBase = FormatFAS
                    End If
                ElseIf CheckSMTFormat = True And CheckFASFormat = False Then
                    If i = 1 Then
                        Coordinats = GetCoordinats(FormatSMT)
                        SNBase = FormatSMT
                    End If
                End If
                'Coordinats = GetCoordinats(If(i = 1, FormatSMT, FormatFAS))
                'SNBase = If(i = 1, FormatSMT, FormatFAS)
                If Coordinats Is Nothing Then
                Else
                    Dim MascBase As String = Mid(SNBase, 1, Coordinats(0)) + Mid(SNBase, Coordinats(0) + Coordinats(1) + 1, Coordinats(2))
                    Dim MascSN As String = Mid(SN, 1, Coordinats(0)) + Mid(SN, Coordinats(0) + Coordinats(1) + 1, Coordinats(2))
                    If (MascBase = MascSN) = True Then
                        Res.Add(True) 'Res(0)
                        Res.Add(i) 'Res(1)
                        If i = 1 Or (i = 2 And HexSN = False) Then
                            Try
                                VarSN = Convert.ToInt32(Mid(SN, Coordinats(0) + 1, Coordinats(1)))
                            Catch ex As Exception
                                VarSN = 0
                            End Try
                        ElseIf i = 2 And HexSN = True Then
                            VarSN = CInt("&H" & Mid(SN, Coordinats(0) + 1, Coordinats(1)))
                        ElseIf i = 3 Then
                            VarSN = CInt("&H" & Mid(SN, Coordinats(0) + 1, Coordinats(1)))
                        End If
                        Res.Add(VarSN) 'Res(2)
                        Exit For
                    End If
                End If
            Else
                Res.Add(False) 'Res(0)
                Res.Add(i) 'Res(1)
                Res.Add(0) 'Res(2)
            End If
        Next

        Select Case Res(1)
            Case 1
                Res.Add($"Формат номера {SN & vbCrLf }соответствует SMT!") 'Res(3) ' Текст сообщения
            Case 2
                Res.Add($"Формат номера {SN & vbCrLf }соответствует FAS!")
            Case 3
                Res.Add($"Формат номера {SN & vbCrLf }соответствует FAS2!")
            Case 4
                Res.Add($"Формат номера {SN & vbCrLf }не соответствует выбранному лоту!")
        End Select
        Return Res
    End Function
#End Region



End Module
