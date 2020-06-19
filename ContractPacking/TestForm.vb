Public Class TestForm
    Private Sub TestForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Function add(x As Integer)
        Return 1
    End Function

    Private Function add1(k As String)
        Return 2
    End Function
    Private Function add2()
        Return 3
    End Function




    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click




        TextBox3.Text = TextBox1.Text Mod TextBox2.Text
        'Dim hexString As String = "ABCDEF"
        'Dim decValue = CInt("&H" & hexString)
        'MsgBox(decValue)



        'Dim var = New Variable
        'Dim var2 = New Variable

        'var2.Test = 12
        ''var.Test = 2558
        'MsgBox(var.Test)
        'MsgBox(var2.Test)


        'Dim List As ArrayList = New ArrayList() From {add(3), add1(""), add2()}
        'For Each item In List
        '    If item = TextBox1.Text Then
        '        MsgBox("Ошибка")
        '        MsgBox(List.IndexOf(item))
        '        Exit For
        '    End If
        'Next
    End Sub








End Class