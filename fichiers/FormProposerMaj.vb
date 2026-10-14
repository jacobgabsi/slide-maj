''' <summary>
''' La mise a jour proposee au lancement de Slide (comme PennyTop, 14/10/2026) : "Version X disponible (vous avez Y).
''' Mettre a jour maintenant ?". Slide tourne souvent sans personne devant l'ecran (television, demarrage de l'ordinateur) :
''' sans reponse apres <see cref="DelaiSecondes"/> secondes, la mise a jour se fait d'elle-meme (28/10/2026, avant : Slide demarrait sans),
''' pour que le diaporama ne reste pas bloque sur une question. "Annuler" la remet au prochain lancement.
''' Ferme avec DialogResult.Yes quand le paquet est telecharge, controle et pret a installer ; No sinon.
''' </summary>
Public Class FormProposerMaj

    Private ReadOnly _info As InfoVersion
    Private _restant As Integer
    Private _enCours As Boolean

    ''' <summary>Secondes d'attente avant de demarrer sans mise a jour (0 : attend la reponse).</summary>
    Friend Shared DelaiSecondes As Integer = 30

    ''' <summary>Ce qui a empeche la mise a jour ("" : rien).</summary>
    Friend Erreur As String = ""

    Public Sub New()
        InitializeComponent()          ' (constructeur sans parametre : requis par le Concepteur)
    End Sub

    Friend Sub New(info As InfoVersion)
        InitializeComponent()
        _info = info
    End Sub

    Private _directement As Boolean

    ''' <summary>Demande par la personne (clic droit > rechercher les mises a jour, 26/10/2026) : pas de question, le telechargement commence tout de suite.</summary>
    Friend Sub New(info As InfoVersion, directement As Boolean)
        Me.New(info)
        _directement = directement
    End Sub

    Private Sub FormProposerMaj_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        If _info Is Nothing Then Exit Sub
        LabelDetail.Text = "La version " & _info.Version & " est disponible (vous avez la " & SlideVersion & ")." & vbCrLf &
                           If(_info.Notes <> "", vbCrLf & _info.Notes & vbCrLf, "") & vbCrLf &
                           If(_info.AReprendre IsNot Nothing, _info.AReprendre.Count & " fichier(s) à mettre à jour (" & Math.Max(1, _info.OctetsAReprendre() \ 1048576) & " Mo)." & vbCrLf & vbCrLf, "") &
                           "Slide va se fermer, se mettre à jour puis redémarrer ; le diaporama reprendra là où il en était."
        _restant = DelaiSecondes
        MontrerLeCompte()
        If _directement Then
            _restant = 0
            MontrerLeCompte()
            BeginInvoke(New Action(Sub() Telecharger()))          ' (la fenetre montre la progression ; elle se ferme toute seule quand le paquet est pret)
        ElseIf _restant > 0 Then
            TimerCompte.Start()
        End If
    End Sub

    Private Sub MontrerLeCompte()
        LabelCompte.Text = If(_restant > 0, "Sans réponse, la mise à jour démarre toute seule dans " & _restant & " s (Annuler pour la reporter).", "")
    End Sub

    Private Sub TimerCompte_Tick(sender As Object, e As EventArgs) Handles TimerCompte.Tick
        _restant -= 1
        MontrerLeCompte()
        If _restant <= 0 Then
            TimerCompte.Stop()
            Telecharger()                         ' sans reponse : la mise a jour se fait d'elle-meme (28/10/2026) ; « Annuler » la remet au prochain lancement
        End If
    End Sub

    Private Sub ButtonMaj_Click(sender As Object, e As EventArgs) Handles ButtonMaj.Click
        Telecharger()
    End Sub

    Private Sub ButtonPlusTard_Click(sender As Object, e As EventArgs) Handles ButtonPlusTard.Click
        TimerCompte.Stop()
        DialogResult = DialogResult.No
        Close()
    End Sub

    ''' <summary>Telecharge et controle le paquet (avec la progression), puis ferme la fenetre : Slide l'installera.</summary>
    Private Async Sub Telecharger()
        If _enCours OrElse _info Is Nothing Then Exit Sub
        _enCours = True
        TimerCompte.Stop()
        ButtonMaj.Enabled = False
        ButtonPlusTard.Enabled = False
        LabelCompte.Text = "Téléchargement de la version " & _info.Version & "..."
        ProgressBarMaj.Style = ProgressBarStyle.Marquee
        ProgressBarMaj.Visible = True
        Dim progres As New Progress(Of KeyValuePair(Of Long, Long))(
            Sub(kv)
                If kv.Value > 0 Then
                    ProgressBarMaj.Style = ProgressBarStyle.Continuous
                    ProgressBarMaj.Value = CInt(Math.Min(100L, kv.Key * 100L \ kv.Value))
                    LabelCompte.Text = "Téléchargement : " & CInt(Math.Min(100L, kv.Key * 100L \ kv.Value)) & " %  (" & Telechargements.TailleLisible(kv.Key) & " / " & Telechargements.TailleLisible(kv.Value) & ")"
                End If
            End Sub)
        Try
            Await MiseAJour.Preparer(_info, MiseAJour.RacineDuProjet(AppPath), AppPath, progres)
            DialogResult = DialogResult.Yes
        Catch ex As Exception
            Erreur = ex.Message
            DiagLog("Mise a jour : echec du telechargement (" & ex.Message & ")")
            DialogResult = DialogResult.No
        End Try
        Close()                                   ' (fenetre non modale : sans cela elle restait a 100 % jusqu'a la croix rouge)
    End Sub

End Class
