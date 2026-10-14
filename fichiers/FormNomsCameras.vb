Option Strict On
Option Explicit On

''' <summary>
''' Noms des cameras d'un ecran de plusieurs cameras (29/10/2026, Jacob : « le nom des cameras en bas a droite, en petit, pour camera IP et Blink »). Les images sont des fenetres VLC posees dans les cases de
''' Form3 : on ne peut pas ecrire dessus. Cette fenetre calque (WS_EX_LAYERED, transparence par pixel, les clics la traversent, jamais le focus) couvre toute la zone de l'ecran et porte tous les noms.
''' L'image est dessinee par EcranDeCameras (CalqueWin32.Appliquer) ; la fenetre n'a pas de controle.
''' </summary>
Public Class FormNomsCameras

    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            If Not DesignMode Then cp.ExStyle = cp.ExStyle Or CalqueWin32.WS_EX_LAYERED Or CalqueWin32.WS_EX_TRANSPARENT Or CalqueWin32.WS_EX_NOACTIVATE Or CalqueWin32.WS_EX_TOOLWINDOW
            Return cp
        End Get
    End Property

End Class
