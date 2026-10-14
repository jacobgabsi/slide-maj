''' <summary>
''' Petite fenetre d'avancement d'un traitement long (22/10/2026 : barre d'avancement du redecoupage des videos, bouton "Maintenant"). Elle ne bloque rien : le traitement
''' appelle <see cref="Avancer"/> de temps en temps, et <see cref="ArretDemande"/> devient vrai si on clique sur "Arreter". Les controles sont dans le Concepteur.
''' </summary>
Public Class FormAvancement

    ''' <summary>Vrai apres un clic sur "Arreter" (ou la fermeture de la fenetre).</summary>
    Public ArretDemande As Boolean

    ''' <summary>Titre de la fenetre et total a atteindre.</summary>
    Public Sub Preparer(titre As String, total As Integer)
        LabelTitre.Text = titre
        ProgressBar1.Minimum = 0
        ProgressBar1.Maximum = Math.Max(1, total)
        ProgressBar1.Value = 0
        LabelDetail.Text = "0 / " & total
    End Sub

    ''' <summary>Met la barre et le texte a jour (fait sur total).</summary>
    Public Sub Avancer(fait As Integer, total As Integer, Optional detail As String = "")
        ProgressBar1.Maximum = Math.Max(1, total)
        ProgressBar1.Value = Math.Max(0, Math.Min(ProgressBar1.Maximum, fait))
        LabelDetail.Text = fait & " / " & total & If(detail <> "", "   " & detail, "")
    End Sub

    Private Sub FormAvancement_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ArretDemande = True
    End Sub

    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub FormAvancement_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
