Option Strict On
Option Explicit On

Imports System.IO

''' <summary>Ou en est un telechargement.</summary>
Friend Enum EtatTelechargement
    Attente
    EnCours
    Termine
    Echec
    Annule
End Enum

''' <summary>Un film ou un episode a telecharger, et ce qu'on en sait.</summary>
Friend NotInheritable Class TelechargementIptv
    Friend Titre As String = ""
    Friend Adresse As String = ""
    Friend Cible As String = ""
    Friend Etat As EtatTelechargement = EtatTelechargement.Attente
    Friend Recu As Long
    Friend Total As Long = -1
    ''' <summary>Octets par seconde (mesure sur la derniere seconde).</summary>
    Friend Vitesse As Double
    Friend Message As String = ""
    Friend Jeton As Threading.CancellationTokenSource
    Friend ReadOnly Chrono As Stopwatch = Stopwatch.StartNew()
    Friend DernierInstant As Long = -1
    Friend DernierRecu As Long
    ''' <summary>Nouveaux essais automatiques deja faits apres un echec (voir Telechargements.MaxEssais).</summary>
    Friend Essais As Integer
    ''' <summary>Heure (TickCount64) avant laquelle un nouvel essai ne demarre pas.</summary>
    Friend ReessaiApres As Long
End Class

''' <summary>
''' Les telechargements de films et d'episodes de Nflix, plusieurs a la fois (demande de Jacob, 13/10/2026 :
''' "rajoute la possibilite de telecharger plusieurs film en simultane").
'''
''' Une file : au plus <see cref="MaxSimultanes"/> telechargements en cours, les autres attendent leur tour.
''' Chacun garde ses portions (voir IptvSources.TelechargerFichier), et l'ensemble partage un plafond de
''' connexions vers le serveur (IptvSources.ConnexionsEnTout) : le compte n'est pas noye.
'''
''' Tout se passe sur le fil de l'interface (les rapports d'avancement y reviennent) : aucune synchronisation.
''' </summary>
Friend Module Telechargements

    ''' <summary>Nombre de telechargements en cours en meme temps.</summary>
    Friend MaxSimultanes As Integer = 3

    Friend ReadOnly Liste As New List(Of TelechargementIptv)

    ''' <summary>Nouveaux essais automatiques apres un echec (14/10/2026 : "3 en echec, relances a la main, ensuite c'etait
    ''' bon") : chacun reprend ou le precedent s'est arrete, apres une attente qui s'allonge.</summary>
    Friend MaxEssais As Integer = 8
    ''' <summary>Secondes avant le 1er, 2e... nouvel essai : 10 s, 30 s, 2 mn, 30 mn, 1 h, 3 h, 6 h, 12 h (choix de Jacob).</summary>
    Private ReadOnly _delais As Integer() = {10, 30, 120, 1800, 3600, 10800, 21600, 43200}

    ''' <summary>Leve a chaque changement (nouveau, avancement, fin...), sur le fil de l'interface.</summary>
    Friend Event Change()

    Friend Function TailleLisible(octets As Long) As String
        If octets >= 1073741824L Then Return (octets / 1073741824.0).ToString("0.00") & " Go"
        Return (octets / 1048576.0).ToString("0.0") & " Mo"
    End Function

    ''' <summary>Met un telechargement dans la file (et le demarre si une place est libre). Rend Nothing s'il y est deja.</summary>
    Friend Function Ajouter(titre As String, adresse As String, cible As String) As TelechargementIptv
        For Each x As TelechargementIptv In Liste
            If String.Equals(x.Cible, cible, StringComparison.OrdinalIgnoreCase) AndAlso
               (x.Etat = EtatTelechargement.Attente OrElse x.Etat = EtatTelechargement.EnCours) Then Return Nothing
        Next
        ' un ancien essai echoue ou annule pour la meme cible : il est remplace
        Liste.RemoveAll(Function(x) String.Equals(x.Cible, cible, StringComparison.OrdinalIgnoreCase))
        Dim t As New TelechargementIptv With {.Titre = titre, .Adresse = adresse, .Cible = cible}
        Liste.Add(t)
        Sauvegarder()
        RaiseEvent Change()
        Demarrer()
        Return t
    End Function

    Friend Function Actifs() As Integer
        Return Liste.Where(Function(x) x.Etat = EtatTelechargement.EnCours).Count()
    End Function

    Friend Function EnAttente() As Integer
        Return Liste.Where(Function(x) x.Etat = EtatTelechargement.Attente).Count()
    End Function

    Private Sub Demarrer()
        Do While Actifs() < Math.Max(1, MaxSimultanes)
            Dim maintenant As Long = Environment.TickCount64
            Dim suivant As TelechargementIptv = Liste.FirstOrDefault(Function(x) x.Etat = EtatTelechargement.Attente AndAlso x.ReessaiApres <= maintenant)
            If suivant Is Nothing Then Exit Do
            Lancer(suivant)
        Loop
    End Sub

    Private Async Sub Lancer(t As TelechargementIptv)
        t.Etat = EtatTelechargement.EnCours
        t.Jeton = New Threading.CancellationTokenSource()
        t.Message = ""
        Dim progres As New Progress(Of KeyValuePair(Of Long, Long))(
            Sub(kv)
                t.Recu = kv.Key
                t.Total = kv.Value
                Dim ms As Long = t.Chrono.ElapsedMilliseconds
                If t.DernierInstant < 0 Then
                    t.DernierInstant = ms
                    t.DernierRecu = kv.Key
                ElseIf ms - t.DernierInstant >= 1000 Then
                    t.Vitesse = (kv.Key - t.DernierRecu) * 1000.0 / (ms - t.DernierInstant)
                    t.DernierInstant = ms
                    t.DernierRecu = kv.Key
                End If
                RaiseEvent Change()
            End Sub)
        Dim delaiAvantReessai As Integer = 0
        Try
            Dim taille As Long = Await IptvSources.TelechargerFichier(t.Adresse, t.Cible, progres, t.Jeton.Token)
            t.Recu = taille
            t.Total = taille
            t.Vitesse = 0
            t.Etat = EtatTelechargement.Termine
            t.Message = ""
        Catch ex As OperationCanceledException
            t.Etat = EtatTelechargement.Annule
            t.Vitesse = 0
        Catch ex As Exception
            t.Vitesse = 0
            If t.Essais < MaxEssais Then
                ' Echec : nouvel essai automatique (les portions deja recues sont gardees, il reprend ou il s'est arrete).
                delaiAvantReessai = _delais(Math.Min(t.Essais, _delais.Length - 1))
                t.Essais += 1
                t.Etat = EtatTelechargement.Attente
                t.ReessaiApres = Environment.TickCount64 + delaiAvantReessai * 1000L
                t.Message = "Nouvel essai " & t.Essais & "/" & MaxEssais & " à " & Date.Now.AddSeconds(delaiAvantReessai).ToString("HH:mm") & " (" & ex.Message & ")"
                DiagLog("Telechargement : echec, nouvel essai " & t.Essais & "/" & MaxEssais & " dans " & delaiAvantReessai & " s : " & t.Titre & " (" & ex.Message & ")")
            Else
                t.Etat = EtatTelechargement.Echec
                t.Message = ex.Message
                DiagLog("Telechargement : echec definitif apres " & t.Essais & " nouvel(s) essai(s) : " & t.Titre & " (" & ex.Message & ")")
            End If
        End Try
        t.Jeton.Dispose()
        t.Jeton = Nothing
        If delaiAvantReessai > 0 Then SurveillerLesReessais()
        Sauvegarder()
        RaiseEvent Change()
        Demarrer()
    End Sub


    ' ------------------------------------------------------------------------------------------
    ' Reprise apres une relance de Slide (14/10/2026 : "reprendre les telechargements si Slide est relance")
    '
    ' La file (ceux qui attendent, courent ou ont echoue) est ecrite dans data\Telechargements.txt a chaque
    ' changement d'etat. Au lancement de Slide, ils sont remis dans la file : les portions deja recues sont sur
    ' le disque (cible.part et cible.part.suivi, voir IptvSources.TelechargerFichier) et le telechargement reprend
    ' ou il s'etait arrete. Les nouveaux essais gardent leur heure.
    ' Une ligne : titre, adresse, cible, essais deja faits, heure du prochain essai (UTC, ticks), A (a reprendre) ou E (echec).
    ' ------------------------------------------------------------------------------------------

    Private Function FichierDeLaFile() As String
        Return Path.Combine(DossierReglages, "Telechargements.txt")
    End Function

    Private Function SansTabulation(texte As String) As String
        Return If(texte, "").Replace(vbTab, " ").Replace(vbCr, " ").Replace(vbLf, " ")
    End Function

    Private Sub Sauvegarder()
        Try
            Dim lignes As New List(Of String)
            For Each x As TelechargementIptv In Liste
                Dim code As String
                Select Case x.Etat
                    Case EtatTelechargement.Attente, EtatTelechargement.EnCours : code = "A"
                    Case EtatTelechargement.Echec : code = "E"
                    Case Else : Continue For                       ' termine ou annule : rien a reprendre
                End Select
                Dim heure As Long = 0
                If x.Etat = EtatTelechargement.Attente AndAlso x.ReessaiApres > Environment.TickCount64 Then
                    heure = DateTime.UtcNow.AddMilliseconds(x.ReessaiApres - Environment.TickCount64).Ticks
                End If
                lignes.Add(String.Join(vbTab, SansTabulation(x.Titre), SansTabulation(x.Adresse), SansTabulation(x.Cible), x.Essais, heure, code))
            Next
            If lignes.Count = 0 Then
                If File.Exists(FichierDeLaFile()) Then File.Delete(FichierDeLaFile())
            Else
                Directory.CreateDirectory(Path.GetDirectoryName(FichierDeLaFile()))
                File.WriteAllLines(FichierDeLaFile(), lignes)
            End If
        Catch ex As Exception
            DiagLog("Telechargements : file non enregistree (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Au lancement de Slide : remet dans la file les telechargements que la fermeture a interrompus.</summary>
    Friend Sub ReprendreApresRelance()
        Try
            If Liste.Count > 0 OrElse Not File.Exists(FichierDeLaFile()) Then Exit Sub
            Dim repris As Integer = 0, echecs As Integer = 0
            For Each ligne As String In File.ReadAllLines(FichierDeLaFile())
                Dim p As String() = ligne.Split(ControlChars.Tab)
                If p.Length < 6 OrElse p(2) = "" OrElse p(1) = "" Then Continue For
                ' deja entierement la (telecharge a la main, ou fini juste avant la fermeture) : rien a faire
                If File.Exists(p(2)) AndAlso Not File.Exists(p(2) & ".part") Then Continue For
                Dim t As New TelechargementIptv With {.Titre = p(0), .Adresse = p(1), .Cible = p(2)}
                Dim essais As Integer = 0
                Integer.TryParse(p(3), essais)
                t.Essais = essais
                If p(5) = "E" Then
                    t.Etat = EtatTelechargement.Echec
                    t.Message = "échoué avant la fermeture de Slide : « Reprendre » pour le relancer"
                    echecs += 1
                Else
                    t.Etat = EtatTelechargement.Attente
                    Dim ticks As Long = 0
                    Long.TryParse(p(4), ticks)
                    If ticks > DateTime.UtcNow.Ticks Then
                        Dim ms As Double = (ticks - DateTime.UtcNow.Ticks) / 10000.0
                        t.ReessaiApres = Environment.TickCount64 + CLng(ms)
                        t.Message = "Nouvel essai " & t.Essais & "/" & MaxEssais & " à " & Date.Now.AddMilliseconds(ms).ToString("HH:mm")
                    End If
                    repris += 1
                End If
                Liste.Add(t)
            Next
            If repris + echecs > 0 Then
                DiagLog("Telechargements : " & repris & " repris apres le lancement, " & echecs & " en echec a relancer")
                SurveillerLesReessais()
                RaiseEvent Change()
                Demarrer()
            End If
        Catch ex As Exception
            DiagLog("Telechargements : reprise impossible (" & ex.Message & ")")
        End Try
    End Sub

    Private _minuteur As System.Windows.Forms.Timer

    ''' <summary>Un minuteur (toutes les 5 s, sur le fil de l'interface) relance les telechargements dont l'heure du
    ''' nouvel essai est arrivee ; il s'arrete quand plus aucun n'attend.</summary>
    Private Sub SurveillerLesReessais()
        If _minuteur Is Nothing Then
            _minuteur = New System.Windows.Forms.Timer With {.Interval = 5000}
            AddHandler _minuteur.Tick,
                Sub(s As Object, e As EventArgs)
                    Dim maintenant As Long = Environment.TickCount64
                    If Not Liste.Any(Function(x) x.Etat = EtatTelechargement.Attente AndAlso x.ReessaiApres > maintenant) Then _minuteur.Stop()
                    Demarrer()
                    RaiseEvent Change()
                End Sub
        End If
        If Not _minuteur.Enabled Then _minuteur.Start()
    End Sub

    ''' <summary>Annule un telechargement en attente ou en cours (ce qui est deja recu est garde pour une reprise).</summary>
    Friend Sub Annuler(t As TelechargementIptv)
        If t.Etat = EtatTelechargement.Attente Then
            t.Etat = EtatTelechargement.Annule
            Sauvegarder()
            RaiseEvent Change()
        ElseIf t.Etat = EtatTelechargement.EnCours AndAlso t.Jeton IsNot Nothing Then
            t.Jeton.Cancel()
        End If
    End Sub

    ''' <summary>Remet dans la file un telechargement echoue ou annule : il reprend ou il s'est arrete.</summary>
    Friend Sub Reprendre(t As TelechargementIptv)
        If t.Etat <> EtatTelechargement.Echec AndAlso t.Etat <> EtatTelechargement.Annule Then Exit Sub
        t.Etat = EtatTelechargement.Attente
        t.Message = ""
        t.Essais = 0                      ' une reprise a la main redonne tous les essais automatiques
        t.ReessaiApres = 0
        t.DernierInstant = -1
        Sauvegarder()
        RaiseEvent Change()
        Demarrer()
    End Sub

    ''' <summary>Retire de la liste ceux qui sont termines ou annules.</summary>
    Friend Sub RetirerTermines()
        Liste.RemoveAll(Function(x) x.Etat = EtatTelechargement.Termine OrElse x.Etat = EtatTelechargement.Annule)
        Sauvegarder()
        RaiseEvent Change()
    End Sub

    ''' <summary>"2 en cours, 1 en attente - 25,3 Mo/s" ("" : rien a dire).</summary>
    Friend Function Bilan() As String
        Dim actifs As Integer = Telechargements.Actifs()
        Dim attente As Integer = EnAttente()
        If actifs = 0 AndAlso attente = 0 Then Return ""
        Dim vitesse As Double = Liste.Where(Function(x) x.Etat = EtatTelechargement.EnCours).Sum(Function(x) x.Vitesse)
        Dim s As String = actifs & " en cours"
        If attente > 0 Then s &= ", " & attente & " en attente"
        If vitesse > 0 Then s &= "  -  " & (vitesse / 1048576.0).ToString("0.0") & " Mo/s"
        Return s
    End Function

    ''' <summary>Avancement de l'ensemble des telechargements en cours, 0 a 100 (-1 : tailles inconnues).</summary>
    Friend Function Avancement() As Integer
        Dim recu As Long = 0, total As Long = 0
        For Each x As TelechargementIptv In Liste
            If x.Etat = EtatTelechargement.EnCours AndAlso x.Total > 0 Then
                recu += x.Recu
                total += x.Total
            End If
        Next
        If total <= 0 Then Return -1
        Return CInt(Math.Min(100L, recu * 100L \ total))
    End Function

End Module
