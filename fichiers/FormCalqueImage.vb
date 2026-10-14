Option Strict On
Option Explicit On

''' <summary>
''' Fenetre calque generale : montre UNE IMAGE (transparence comprise) a un endroit de l'ecran,
''' par-dessus l'image de Slide, photos comme videos, sans jamais prendre le clavier. Meme
''' technique que la legende et la montre (voir CalqueWin32), mais sans rien savoir de ce
''' qu'elle montre : le dessin est fait par celui qui s'en sert.
'''
''' Sert a la pochette de Deezer (CarteDeezer : elle recoit la souris, pour la taille) et a
''' l'annonce des commandes a distance (AnnonceEcran : les clics la traversent).
''' </summary>
Public Class FormCalqueImage

    ''' <summary>Vrai : la fenetre est transparente a la souris (les clics vont a ce qui est
    ''' dessous). A regler AVANT le premier affichage.</summary>
    Public Property ClicsTraversants As Boolean

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_LAYERED Or WS_EX_TOOLWINDOW Or WS_EX_NOACTIVATE
            If ClicsTraversants Then cp.ExStyle = cp.ExStyle Or WS_EX_TRANSPARENT
            Return cp
        End Get
    End Property

    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    ''' <summary>Montre <paramref name="image"/>, coin haut gauche en <paramref name="place"/>
    ''' (coordonnees de l'ecran). premierPlan : True quand Slide est en plein ecran (fenetre
    ''' independante, devant tout) ; False en mode fenetre (rattachee a
    ''' <paramref name="reference"/>, qu'elle suit).</summary>
    Public Sub Montrer(image As Bitmap, place As Point, reference As Form, premierPlan As Boolean)
        Dim voulu As New Rectangle(place, image.Size)
        If Me.Bounds <> voulu Then Me.Bounds = voulu
        CalqueWin32.DefinirPremierPlan(Me, reference, premierPlan)
        If Not Me.Visible Then Me.Show()
        CalqueWin32.Appliquer(Me, image)
        If premierPlan Then CalqueWin32.ReaffirmerPremierPlan(Me)
    End Sub

    Public Sub Masquer()
        If Me.Visible Then Me.Hide()
    End Sub

End Class
