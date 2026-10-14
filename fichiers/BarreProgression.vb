Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D

''' <summary>
''' Barre de progression fine et arrondie (ecran de demarrage). Meme usage que la ProgressBar
''' qu'elle remplace : Value de 0 a Maximum, affectee depuis Module1 pendant le chargement.
''' Chaque changement redessine tout de suite : le chargement tourne sur le thread de
''' l'interface, sans laisser a Windows le temps de repeindre.
''' </summary>
Public Class BarreProgression
    Inherits Control

    Private _valeur As Integer
    Private _maximum As Integer = 100

    Public Property CouleurPiste As Color = Color.FromArgb(52, 52, 56)
    Public Property CouleurBarre As Color = Color.FromArgb(10, 132, 255)

    Public Sub New()
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or
                 ControlStyles.UserPaint Or ControlStyles.ResizeRedraw Or ControlStyles.SupportsTransparentBackColor, True)
        BackColor = Color.Transparent
    End Sub

    Public Property Maximum As Integer
        Get
            Return _maximum
        End Get
        Set(v As Integer)
            _maximum = Math.Max(1, v)
            Redessiner()
        End Set
    End Property

    Public Property Value As Integer
        Get
            Return _valeur
        End Get
        Set(v As Integer)
            v = Math.Max(0, Math.Min(_maximum, v))
            If v = _valeur Then Exit Property
            _valeur = v
            Redessiner()
        End Set
    End Property

    Private Sub Redessiner()
        Invalidate()
        If IsHandleCreated AndAlso Visible Then Update()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim zone As New Rectangle(0, 0, ClientSize.Width, ClientSize.Height)
        Using p As New SolidBrush(CouleurPiste)
            Remplir(g, p, zone)
        End Using
        Dim largeur As Integer = CInt(zone.Width * _valeur / CDbl(_maximum))
        If largeur >= zone.Height Then
            Using p As New SolidBrush(CouleurBarre)
                Remplir(g, p, New Rectangle(0, 0, largeur, zone.Height))
            End Using
        End If
    End Sub

    Private Shared Sub Remplir(g As Graphics, pinceau As Brush, zone As Rectangle)
        If zone.Width <= 0 OrElse zone.Height <= 0 Then Exit Sub
        Dim d As Integer = zone.Height
        Using chemin As New GraphicsPath()
            chemin.AddArc(zone.Left, zone.Top, d, d, 90, 180)
            chemin.AddArc(zone.Right - d, zone.Top, d, d, 270, 180)
            chemin.CloseFigure()
            g.FillPath(pinceau, chemin)
        End Using
    End Sub

End Class
