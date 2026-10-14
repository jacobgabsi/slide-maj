Option Strict On
Option Explicit On

''' <summary>Fenetre de l'export / import des parametres (clic droit > Parametres) : un texte, et la case a cocher « minuterie ».</summary>
Public Class FormTransfert

    ''' <summary>Montre la fenetre. <paramref name="minuterie"/> : etat de la case a l'arrivee, puis choix de l'utilisateur ;
    ''' <paramref name="minuterieDisponible"/> faux : case grisee et decochee. Rend Faux si la fenetre est annulee.</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, titre As String, texte As String, texteCase As String,
                                    minuterieDisponible As Boolean, ByRef minuterie As Boolean) As Boolean
        Using f As New FormTransfert()
            f.Text = titre
            f.LabelTexte.Text = texte
            f.CheckBoxMinuterie.Text = texteCase
            f.CheckBoxMinuterie.Enabled = minuterieDisponible
            f.CheckBoxMinuterie.Checked = minuterieDisponible AndAlso minuterie
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            minuterie = f.CheckBoxMinuterie.Checked
            Return True
        End Using
    End Function

    Private Sub FormTransfert_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        AjusterLaHauteurDuTexte()
    End Sub

    ''' <summary>Le texte n'est jamais coupe : la fenetre grandit de ce qu'il faut (la case et les boutons descendent avec elle).</summary>
    Private Sub AjusterLaHauteurDuTexte()
        Dim voulu As Integer = TextRenderer.MeasureText(LabelTexte.Text, LabelTexte.Font, New Size(LabelTexte.Width, 0), TextFormatFlags.WordBreak).Height + 8
        Dim delta As Integer = voulu - LabelTexte.Height
        If delta <= 0 Then Exit Sub
        LabelTexte.Height += delta
        CheckBoxMinuterie.Top += delta
        ButtonOK.Top += delta
        ButtonAnnuler.Top += delta
        ClientSize = New Size(ClientSize.Width, ClientSize.Height + delta)
    End Sub

End Class
