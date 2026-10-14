Imports System.IO

''' <summary>
''' La liste des telechargements de Nflix (voir Telechargements) : ceux qui sont en cours, en attente, termines ou
''' echoues, avec leur avancement et leur vitesse. Demande de Jacob, 13/10/2026 : "rajoute la possibilite de
''' telecharger plusieurs film en simultane". Une seule fenetre, qui se cache a la fermeture.
''' </summary>
Public Class FormTelechargements

    Private Shared _instance As FormTelechargements

    ''' <summary>Montre la fenetre (la cree au besoin).</summary>
    Friend Shared Sub Montrer(proprietaire As Form)
        If _instance Is Nothing OrElse _instance.IsDisposed Then _instance = New FormTelechargements()
        If Not _instance.Visible Then
            If proprietaire IsNot Nothing Then _instance.Show(proprietaire) Else _instance.Show()
        End If
        _instance.WindowState = FormWindowState.Normal
        _instance.BringToFront()
        _instance.Rafraichir()
    End Sub

    Private Function EtatEnTexte(t As TelechargementIptv) As String
        Select Case t.Etat
            Case EtatTelechargement.Attente : Return If(t.Message <> "", t.Message, "En attente")
            Case EtatTelechargement.EnCours : Return "En cours"
            Case EtatTelechargement.Termine : Return "Terminé"
            Case EtatTelechargement.Annule : Return "Annulé (reprise possible)"
            Case EtatTelechargement.Echec : Return "Échec : " & t.Message
        End Select
        Return ""
    End Function

    Private Function AvancementEnTexte(t As TelechargementIptv) As String
        If t.Etat = EtatTelechargement.Attente AndAlso t.Recu = 0 Then Return ""
        If t.Total > 0 Then
            Return Telechargements.TailleLisible(t.Recu) & " / " & Telechargements.TailleLisible(t.Total)
        End If
        Return If(t.Recu > 0, Telechargements.TailleLisible(t.Recu), "")
    End Function

    ''' <summary>Colonne « % » (08/11/2026) : l'avancement en pourcentage, seul dans sa colonne.</summary>
    Private Function PourcentEnTexte(t As TelechargementIptv) As String
        If t.Etat = EtatTelechargement.Termine Then Return "100 %"
        If t.Total > 0 AndAlso t.Recu > 0 Then Return CInt(Math.Min(100L, t.Recu * 100L \ t.Total)) & " %"
        Return ""
    End Function

    ''' <summary>Temps restant et heure de fin estimee (ex. « 2 min  (vers 14:32) »), d'apres le reste a recevoir et la vitesse du moment.</summary>
    Private Function FinEstimeeEnTexte(t As TelechargementIptv) As String
        If t.Etat <> EtatTelechargement.EnCours OrElse t.Total <= 0 OrElse t.Vitesse <= 0 Then Return ""
        Dim secondes As Double = Math.Max(0L, t.Total - t.Recu) / CDbl(t.Vitesse)
        If secondes > 7 * 24 * 3600 Then Return ""
        Dim reste As TimeSpan = TimeSpan.FromSeconds(Math.Ceiling(secondes))
        Dim texte As String
        If reste.TotalSeconds < 60 Then
            texte = CInt(reste.TotalSeconds) & " s"
        ElseIf reste.TotalHours < 1 Then
            texte = CInt(Math.Ceiling(reste.TotalMinutes)) & " min"
        Else
            texte = CInt(Math.Floor(reste.TotalHours)) & " h " & reste.Minutes.ToString("00") & " min"
        End If
        Dim fin As DateTime = DateTime.Now.Add(reste)
        Return texte & "  (vers " & fin.ToString("HH:mm") & If(fin.Date <> DateTime.Today, " le " & fin.ToString("dd/MM"), "") & ")"
    End Function

    ''' <summary>Remet la grille a jour (les lignes gardent leur place, la selection aussi).</summary>
    Friend Sub Rafraichir()
        If IsDisposed Then Exit Sub
        Dim liste As List(Of TelechargementIptv) = Telechargements.Liste
        GrilleTelechargements.SuspendLayout()
        Try
            Do While GrilleTelechargements.Rows.Count > liste.Count
                GrilleTelechargements.Rows.RemoveAt(GrilleTelechargements.Rows.Count - 1)
            Loop
            Do While GrilleTelechargements.Rows.Count < liste.Count
                GrilleTelechargements.Rows.Add("", "", "", "", "", "")
            Loop
            For i As Integer = 0 To liste.Count - 1
                Dim t As TelechargementIptv = liste(i)
                Dim r As DataGridViewRow = GrilleTelechargements.Rows(i)
                r.Tag = t
                r.Cells(0).Value = t.Titre
                r.Cells(1).Value = EtatEnTexte(t)
                r.Cells(2).Value = AvancementEnTexte(t)
                r.Cells(3).Value = PourcentEnTexte(t)
                r.Cells(4).Value = If(t.Etat = EtatTelechargement.EnCours AndAlso t.Vitesse > 0, (t.Vitesse / 1048576.0).ToString("0.0") & " Mo/s", "")
                r.Cells(5).Value = FinEstimeeEnTexte(t)
            Next
        Finally
            GrilleTelechargements.ResumeLayout()
        End Try
        ButtonPause.Enabled = Not Telechargements.EnPause AndAlso Telechargements.Actifs() + Telechargements.EnAttente() > 0       ' (en pause : « Reprendre » la leve)
        Dim bilan As String = Telechargements.Bilan()
        LabelBilan.Text = If(bilan = "", If(liste.Count = 0, "Aucun téléchargement.", "Rien en cours."), "Téléchargements : " & bilan) &
                          "     (" & Telechargements.MaxSimultanes & " au plus en même temps)"
    End Sub

    Private Function SelectionnesEtLeurDossier() As List(Of TelechargementIptv)
        Dim l As New List(Of TelechargementIptv)
        For Each r As DataGridViewRow In GrilleTelechargements.SelectedRows
            Dim t As TelechargementIptv = TryCast(r.Tag, TelechargementIptv)
            If t IsNot Nothing Then l.Add(t)
        Next
        Return l
    End Function

    Private Sub FormTelechargements_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        AjusterLesColonnes()
        MoletteSousLaSouris.Suivre(Me)
        AddHandler Telechargements.Change, AddressOf QuandCaChange
        TimerRafraichir.Start()
        Rafraichir()
    End Sub

    ''' <summary>Largeurs FIXES (elles ne bougent plus a chaque rafraichissement), calculees une fois d'apres la police reelle (DPI, 4K) et le plus long contenu possible de chaque colonne.</summary>
    Private Sub AjusterLesColonnes()
        Dim police As Font = GrilleTelechargements.DefaultCellStyle.Font
        If police Is Nothing Then police = GrilleTelechargements.Font
        Dim entete As Font = If(GrilleTelechargements.ColumnHeadersDefaultCellStyle.Font, police)
        Dim marges As Integer = TextRenderer.MeasureText("MM", police).Width
        Dim regler =
            Sub(colonne As DataGridViewColumn, exemples As String())
                Dim largeur As Integer = TextRenderer.MeasureText(colonne.HeaderText, entete).Width + marges
                For Each ex As String In exemples
                    largeur = Math.Max(largeur, TextRenderer.MeasureText(ex, police).Width + marges)
                Next
                colonne.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                colonne.Width = largeur
            End Sub
        regler(ColonneEtat, {"Annulé (reprise possible)", "En attente", "Nouvel essai 8/8 à 23:59"})
        regler(ColonneAvancement, {"999,9 Mo / 999,9 Mo", "99,99 Go / 99,99 Go"})
        regler(ColonnePourcent, {"100 %"})
        regler(ColonneVitesse, {"99,9 Mo/s"})
        regler(ColonneFin, {"10 h 59 min  (vers 23:59 le 31/12)"})
        ColonneFin.Width = CInt(ColonneFin.Width * 0.75)             ' (trop large : 25 % de moins, la colonne Film prend la place)
        ' La fenetre s'elargit si besoin pour que le titre garde au moins 250 points.
        Dim fixes As Integer = ColonneEtat.Width + ColonneAvancement.Width + ColonnePourcent.Width + ColonneVitesse.Width + ColonneFin.Width
        Dim manque As Integer = fixes + 250 + SystemInformation.VerticalScrollBarWidth + 4 - GrilleTelechargements.Width
        If manque > 0 Then
            Dim ecran As Rectangle = Screen.FromControl(Me).WorkingArea
            Dim nouvelle As Integer = Math.Min(Me.Width + manque, ecran.Width)
            Me.MinimumSize = New Size(Math.Min(Math.Max(Me.MinimumSize.Width, nouvelle), ecran.Width), Me.MinimumSize.Height)
            Me.Width = nouvelle
            Me.Left = Math.Max(ecran.Left, Math.Min(Me.Left, ecran.Right - Me.Width))
        End If
    End Sub

    Private Sub QuandCaChange()
        If Visible Then Rafraichir()
    End Sub

    Private Sub TimerRafraichir_Tick(sender As Object, e As EventArgs) Handles TimerRafraichir.Tick
        If Visible Then Rafraichir()
    End Sub

    Private Sub FormTelechargements_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True                        ' les telechargements continuent : la fenetre se cache seulement
            Hide()
        Else
            RemoveHandler Telechargements.Change, AddressOf QuandCaChange
        End If
    End Sub

    Private Sub ButtonAnnulerTelechargement_Click(sender As Object, e As EventArgs) Handles ButtonAnnulerTelechargement.Click
        For Each t As TelechargementIptv In SelectionnesEtLeurDossier()
            Telechargements.Annuler(t)
        Next
    End Sub

    ''' <summary>Pause generale : les telechargements en cours s'arretent (ce qui est recu est garde) et plus aucun ne demarre. « Reprendre » la leve.</summary>
    Private Sub ButtonPause_Click(sender As Object, e As EventArgs) Handles ButtonPause.Click
        Telechargements.PauserTout()
        Rafraichir()
    End Sub

    Private Sub ButtonToutAnnuler_Click(sender As Object, e As EventArgs) Handles ButtonToutAnnuler.Click
        Dim n As Integer = Telechargements.Actifs() + Telechargements.EnAttente()
        If n = 0 Then Exit Sub
        If MessageBox.Show(Me, "Annuler les " & n & " téléchargements en cours ou en attente ?" & vbCrLf & "(ce qui est déjà reçu est gardé : « Reprendre » les relance)", "Tout annuler",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        Telechargements.AnnulerTout()
        Rafraichir()
    End Sub

    ''' <summary>« Reprendre » (09/11/2026, bouton unique) : leve la pause generale s'il y en a une, et relance les telechargements echoues ou annules
    ''' (ceux qui sont selectionnes ; sans selection, tous).</summary>
    Private Sub ButtonReprendre_Click(sender As Object, e As EventArgs) Handles ButtonReprendre.Click
        If Telechargements.EnPause Then Telechargements.ContinuerTout()
        Dim aRelancer As List(Of TelechargementIptv) = SelectionnesEtLeurDossier()
        If aRelancer.Count = 0 Then
            Dim maintenant As Long = Environment.TickCount64
            aRelancer = Telechargements.Liste.Where(Function(x) x.Etat = EtatTelechargement.Echec OrElse x.Etat = EtatTelechargement.Annule OrElse
                                                        (x.Etat = EtatTelechargement.Attente AndAlso x.ReessaiApres > maintenant)).ToList()     ' (y compris ceux qui attendent un nouvel essai : ils repartent tout de suite)
        End If
        For Each t As TelechargementIptv In aRelancer
            Telechargements.Reprendre(t)
        Next
        Rafraichir()
    End Sub

    Private Sub ButtonOuvrirLeDossier_Click(sender As Object, e As EventArgs) Handles ButtonOuvrirLeDossier.Click
        Dim choisis As List(Of TelechargementIptv) = SelectionnesEtLeurDossier()
        Dim t As TelechargementIptv = If(choisis.Count > 0, choisis(0), Telechargements.Liste.LastOrDefault())
        If t Is Nothing Then Exit Sub
        Try
            If File.Exists(t.Cible) Then
                Process.Start("explorer.exe", "/select,""" & t.Cible & """")
            ElseIf Directory.Exists(Path.GetDirectoryName(t.Cible)) Then
                Process.Start("explorer.exe", """" & Path.GetDirectoryName(t.Cible) & """")
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ButtonRetirerTermines_Click(sender As Object, e As EventArgs) Handles ButtonRetirerTermines.Click
        Telechargements.RetirerTermines()
    End Sub

    Private Sub ButtonFermer_Click(sender As Object, e As EventArgs) Handles ButtonFermer.Click
        Hide()
    End Sub

End Class
