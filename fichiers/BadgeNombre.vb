Imports System.Drawing.Drawing2D

''' <summary>
''' Un rond avec un nombre a l'interieur (04/11/2026, Jacob : « en haut a droite de chaque tableau un rond avec le nombre de lignes remplies »). Controle simple, pose dans le Concepteur
''' (Form4) ; le nombre est donne par la propriete <see cref="Nombre"/>.
''' </summary>
Friend Class BadgeNombre
    Inherits Control

    Private _nombre As Integer

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or ControlStyles.SupportsTransparentBackColor, True)
        BackColor = Color.Transparent
        Size = New Size(34, 34)
        TabStop = False
    End Sub

    ''' <summary>Le nombre montre dans le rond.</summary>
    Public Property Nombre As Integer
        Get
            Return _nombre
        End Get
        Set(value As Integer)
            If value = _nombre Then Exit Property
            _nombre = value
            Invalidate()
        End Set
    End Property

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim cote As Integer = Math.Min(Width, Height) - 2
        Dim zone As New Rectangle((Width - cote) \ 2, (Height - cote) \ 2, cote, cote)
        Using fond As New SolidBrush(Color.FromArgb(10, 132, 255))
            g.FillEllipse(fond, zone)
        End Using
        Dim texte As String = _nombre.ToString()
        Dim taille As Single = If(texte.Length >= 4, 7.5F, If(texte.Length = 3, 9.0F, 11.0F))
        Using police As New Font("Segoe UI Semibold", taille, FontStyle.Bold)
            TextRenderer.DrawText(g, texte, police, zone, Color.White, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix)
        End Using
    End Sub

End Class
