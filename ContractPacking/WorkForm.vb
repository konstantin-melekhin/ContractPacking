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




    End Sub
End Class