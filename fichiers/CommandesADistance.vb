Option Strict On
Option Explicit On

Imports System.IO
Imports System.Threading.Tasks

''' <summary>
''' Reception des commandes a distance (SlideControlAdistance) : chaque commande est un fichier
''' vide, du nom de la commande, cree dans le dossier partage "ControleAdistance".
'''
''' Refait le 08/10/2026 (Jacob : "Volume.Moins et Plus : parfois reaction immediate et parfois
''' apres 30 secondes"). Avant, tout reposait sur les avis de Windows (FileSystemWatcher), sur un
''' lecteur RESEAU :
'''  - un avis en retard = une commande en retard ; un avis perdu = une commande jamais executee ;
'''  - si la surveillance tombait (coupure du reseau), elle n'etait relancee qu'a la minute suivante,
'''    et chaque relance ajoutait un abonnement : les commandes etaient ensuite executees en double.
''' Maintenant :
'''  - les avis restent le chemin rapide (un seul abonnement, surveillance relancee si elle tombe) ;
'''  - un RATTRAPAGE regarde le dossier chaque seconde et execute ce qui y traine depuis deux passages ;
'''  - s'il a du rattraper une commande, c'est que les avis ne sont pas fiables ici : pendant dix
'''    minutes il prend tout lui-meme, deux fois par seconde, et les avis sont laisses de cote ;
'''  - une commande = un fichier PRESENT : il est efface avant l'execution, donc jamais execute deux
'''    fois, quel que soit le chemin par lequel il est vu ;
'''  - le journal dit par ou la commande est arrivee et si le fil de l'interface l'a fait attendre.
''' L'execution elle-meme reste dans Module2.GestionControlAdistanceSub2, dans le fil de l'interface.
''' </summary>
Friend Module CommandesADistance

    Private ReadOnly _surveillance As FileSystemWatcher = fsw       ' (celle de Module1, pour l'ancien test "fsw.EnableRaisingEvents")
    Private WithEvents _rattrapage As New System.Windows.Forms.Timer With {.Interval = 1000}
    Private _dossier As String = ""
    Private _listeEnCours As Boolean
    Private _derniereErreur As Date = Date.MinValue

    ''' <summary>Nom (en majuscules) -> moment de la derniere execution : deux avis de Windows pour
    ''' un seul fichier (cree, puis modifie) ne font qu'une execution.</summary>
    Private ReadOnly _executes As New Dictionary(Of String, Date)
    ''' <summary>Nom -> moment ou le rattrapage l'a vu pour la premiere fois dans le dossier.</summary>
    Private ReadOnly _vus As New Dictionary(Of String, Date)
    ''' <summary>Fichiers impossibles a effacer (droits) : le rattrapage ne les reprend pas.</summary>
    Private ReadOnly _ineffacables As New HashSet(Of String)
    ''' <summary>Jusqu'a cette heure, le rattrapage prend tout et les avis de Windows sont ignores.</summary>
    Private _rattrapageSeulJusqua As Date = Date.MinValue
    Private Const RattrapageSeulMinutes As Double = 10

    Private ReadOnly Property RattrapageSeul As Boolean
        Get
            Return Now < _rattrapageSeulJusqua
        End Get
    End Property

    ''' <summary>Met la reception en service sur <paramref name="dossier"/> (a appeler du fil de l'interface).</summary>
    Friend Sub Armer(dossier As String)
        Try
            _dossier = dossier
            RemoveHandler _surveillance.Created, AddressOf Avis
            RemoveHandler _surveillance.Changed, AddressOf Avis
            RemoveHandler _surveillance.Error, AddressOf Panne
            _surveillance.EnableRaisingEvents = False
            _surveillance.SynchronizingObject = Nothing         ' on repasse nous-memes par le fil de l'interface (mesure de l'attente)
            _surveillance.Path = dossier
            _surveillance.Filter = "*.*"
            _surveillance.NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.FileName
            AddHandler _surveillance.Created, AddressOf Avis
            AddHandler _surveillance.Changed, AddressOf Avis
            AddHandler _surveillance.Error, AddressOf Panne
            _surveillance.EnableRaisingEvents = True
        Catch ex As Exception
            ' Dossier injoignable : le rattrapage prend le relais et la surveillance sera retentee.
            If (Now - _derniereErreur).TotalMinutes >= 5 Then
                _derniereErreur = Now
                DiagLog("Telecommande : surveillance du dossier impossible (" & ex.Message & "), le rattrapage continue")
            End If
        End Try
        _rattrapage.Start()
    End Sub

    Friend Sub Desarmer()
        Try
            _rattrapage.Stop()
            _surveillance.EnableRaisingEvents = False
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Avis de Windows : un fichier est apparu (fil quelconque). On note l'heure et on
    ''' passe au fil de l'interface.</summary>
    Private Sub Avis(sender As Object, e As FileSystemEventArgs)
        Dim recu As Date = Now
        Try
            If RattrapageSeul Then Exit Sub
            Dim principale As Form = FenetrePrincipale()
            If principale Is Nothing OrElse principale.IsDisposed OrElse Not principale.IsHandleCreated Then Exit Sub
            principale.BeginInvoke(New Action(Sub() Traiter(e.FullPath, e.Name, recu, False)))
        Catch ex As Exception
            ' fenetre en cours de fermeture
        End Try
    End Sub

    ''' <summary>La surveillance est tombee (reseau coupe, trop d'avis d'un coup) : on la relance.</summary>
    Private Sub Panne(sender As Object, e As ErrorEventArgs)
        Try
            DiagLog("Telecommande : surveillance du dossier interrompue (" & e.GetException().Message & "), relancee")
            Dim principale As Form = FenetrePrincipale()
            If principale IsNot Nothing AndAlso Not principale.IsDisposed AndAlso principale.IsHandleCreated Then
                principale.BeginInvoke(New Action(Sub() Armer(_dossier)))
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Regulierement : ce qui traine dans le dossier est execute. La liste du dossier est
    ''' lue hors du fil de l'interface (un lecteur reseau absent peut faire attendre longtemps).</summary>
    Private Async Sub Rattrapage_Tick(sender As Object, e As EventArgs) Handles _rattrapage.Tick
        If _listeEnCours OrElse _dossier = "" Then Exit Sub
        _listeEnCours = True
        Try
            Dim dossier As String = _dossier
            Dim fichiers As String() = Await Task.Run(Function()
                                                          Try
                                                              Return Directory.GetFiles(dossier)
                                                          Catch ex As Exception
                                                              Return Array.Empty(Of String)()
                                                          End Try
                                                      End Function)
            Dim presents As New HashSet(Of String)(fichiers.Select(Function(f) Path.GetFileName(f).ToUpperInvariant()))
            For Each parti As String In _vus.Keys.Where(Function(n) Not presents.Contains(n)).ToList() : _vus.Remove(parti) : Next
            _ineffacables.RemoveWhere(Function(n) Not presents.Contains(n))
            Dim seul As Boolean = RattrapageSeul
            For Each chemin As String In fichiers
                Dim nom As String = Path.GetFileName(chemin), cle As String = nom.ToUpperInvariant()
                If _ineffacables.Contains(cle) Then Continue For
                If Not seul Then
                    ' Les avis de Windows marchent peut-etre : on leur laisse le temps (le fichier doit
                    ' etre encore la au passage suivant).
                    Dim vu As Date
                    If Not _vus.TryGetValue(cle, vu) Then _vus(cle) = Now : Continue For
                    If (Now - vu).TotalMilliseconds < 700 Then Continue For
                End If
                _vus.Remove(cle)
                If Traiter(chemin, nom, Now, True) AndAlso Not seul Then
                    DiagLog("Telecommande : l'avis de Windows n'est pas arrive a temps -> pendant " & CInt(RattrapageSeulMinutes) &
                            " minutes, le dossier est regarde deux fois par seconde et les avis sont laisses de cote")
                End If
            Next
            ' Rattrapage seul : deux passages par seconde ; sinon un.
            Dim rythme As Integer = If(RattrapageSeul, 500, 1000)
            If _rattrapage.Interval <> rythme Then _rattrapage.Interval = rythme
            ' La surveillance s'est arretee sans prevenir : on la relance.
            If Not _surveillance.EnableRaisingEvents Then Armer(dossier)
        Catch ex As Exception
            DiagLog("Telecommande (rattrapage) : " & ex.Message)
        Finally
            _listeEnCours = False
        End Try
    End Sub

    ''' <summary>Execute une commande (fil de l'interface) si son fichier est la. Le fichier est
    ''' efface AVANT l'execution : il ne peut pas etre execute deux fois, et un second appui pendant
    ''' l'execution cree un nouveau fichier, qui sera vu. Renvoie True si la commande a ete executee.</summary>
    Private Function Traiter(chemin As String, nom As String, recu As Date, parRattrapage As Boolean) As Boolean
        Try
            If String.IsNullOrWhiteSpace(nom) Then Return False
            Dim cle As String = nom.ToUpperInvariant()
            If Not parRattrapage Then
                If RattrapageSeul Then Return False
                ' Deux avis de Windows pour un seul fichier (cree, puis modifie) : le second est ignore
                ' sans meme regarder le dossier.
                Dim avant As Date
                If _executes.TryGetValue(cle, avant) AndAlso (Now - avant).TotalMilliseconds < 300 Then Return False
            End If
            ' Le fichier n'est plus la : la commande a deja ete executee (par l'autre chemin).
            If Not File.Exists(chemin) Then Return False
            ' On l'efface. Il peut etre encore tenu un instant par la telecommande qui vient de le creer.
            Dim efface As Boolean = False, raison As String = ""
            For essai As Integer = 1 To 6
                Try
                    File.Delete(chemin)
                    efface = True
                    Exit For
                Catch ex As UnauthorizedAccessException
                    raison = ex.Message
                    Exit For
                Catch ex As IOException
                    raison = ex.Message
                    Threading.Thread.Sleep(25)
                End Try
            Next
            If Not efface Then
                ' Impossible a effacer : la commande est executee quand meme (comme avant), une fois ;
                ' le rattrapage ne la reprendra pas tant que ce fichier reste la.
                If _ineffacables.Add(cle) Then DiagLog("Telecommande : " & nom & " : fichier non efface (" & raison & ")")
            End If
            _executes(cle) = Now
            If parRattrapage Then _rattrapageSeulJusqua = Now.AddMinutes(RattrapageSeulMinutes)
            Dim attente As Double = (Now - recu).TotalMilliseconds
            DiagLog("Telecommande : " & nom & If(parRattrapage, " (par le rattrapage)", "") &
                    If(attente >= 500, " (a attendu " & CInt(attente) & " ms que Slide soit disponible)", ""))
            ' En tres gros au centre de l'ecran (demandes de Jacob, 08/10/2026 : les commandes de Deezer,
            ' puis "Volume.Plus et Volume.Moins ne s'affichent pas a l'ecran comme Deezer.Next").
            Dim annoncee As Boolean = cle.StartsWith("DEEZER.") OrElse cle.StartsWith("VOLUME.")
            If annoncee Then AnnonceEcran.Montrer(nom)
            GestionControlAdistanceSub2(Nothing, Nothing, nom)
            ' Pour le volume : sa nouvelle valeur, dessous.
            If cle.StartsWith("VOLUME.") Then AnnonceEcran.Preciser("Volume.", "Volume général : " & VolumeGeneral & " %")
            Return True
        Catch ex As Exception
            DiagLog("Telecommande (" & nom & ") : " & ex.Message)
            Return False
        End Try
    End Function

End Module
