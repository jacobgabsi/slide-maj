Option Strict Off
Option Explicit On

Imports System.Threading.Tasks

''' <summary>
''' Reglages de Form4 modifiables par la minuterie (20/10/2026 : "regarde dans toutes les fonctions de la minuterie ce que je peux
''' modifier dans les reglages, peut etre rajouter dans la minuterie"). Chaque reglage est une ACTION de la case Action d'une ligne :
''' "libelle : valeur" (par exemple "Durée d'affichage d'une photo : 10 s", "Animer les photos : Oui"). Les lignes existent sous les types
''' Photo Mode, Video Mode et Son (pochette) ; ce qui ne rentre pas dans un type existant va dans le type "Autres".
'''
''' Une action change le reglage EN COURS (comme les autres taches de la minuterie) : le fichier des reglages n'est pas modifie, la tache
''' BOOT ou la suivante le change a nouveau.
''' </summary>
Friend Module ReglagesMinuterie

    Friend Const TypePhoto As String = "Photo Mode"
    Friend Const TypeVideo As String = "Video Mode"
    Friend Const TypeSon As String = "Son"
    Friend Const TypeAutres As String = "Autres"

    Private Class Entree
        Friend Type As String
        Friend Texte As String
        Friend Appliquer As Action
    End Class

    Private _entrees As List(Of Entree)
    Private _parCle As Dictionary(Of String, Entree)

    Private ReadOnly Coins As String() = {"En bas à gauche", "En bas à droite", "En haut à gauche", "En haut à droite"}
    Private ReadOnly CodesCoins As String() = {"Bgauche", "Bdroite", "Hgauche", "Hdroite"}

    ' ------------------------------------------------------------------------------------------

    Private Sub Ajouter(type As String, texte As String, appliquer As Action)
        _entrees.Add(New Entree With {.Type = type, .Texte = texte, .Appliquer = appliquer})
    End Sub

    Private Sub Booleen(type As String, libelle As String, appliquer As Action(Of Boolean))
        Ajouter(type, libelle & " : Oui", Sub() appliquer(True))
        Ajouter(type, libelle & " : Non", Sub() appliquer(False))
    End Sub

    Private Sub Choix(type As String, libelle As String, noms As String(), appliquer As Action(Of Integer))
        For i As Integer = 0 To noms.Length - 1
            Dim rang As Integer = i
            Ajouter(type, libelle & " : " & noms(i), Sub() appliquer(rang))
        Next
    End Sub

    Private Sub Entier(type As String, libelle As String, valeurs As Integer(), unite As String, appliquer As Action(Of Integer))
        For Each v As Integer In valeurs
            Dim valeur As Integer = v
            Ajouter(type, libelle & " : " & valeur & If(unite <> "", " " & unite, ""), Sub() appliquer(valeur))
        Next
    End Sub

    Private Sub Construire()
        If _entrees IsNot Nothing Then Exit Sub
        _entrees = New List(Of Entree)

        Dim pourcents As Integer() = {50, 75, 100, 125, 150, 200}

        ' ---- Photos (type Photo Mode) ----
        Entier(TypePhoto, "Durée d'affichage d'une photo", {3, 5, 8, 10, 15, 20, 30, 45, 60}, "s", Sub(v) DurréeAffichageDuneImage = v)
        Entier(TypePhoto, "Séquence de photos", {30, 60, 90, 120, 180, 240, 300, 600}, "s", Sub(v) DurréePhoto = v)
        Booleen(TypePhoto, "Seulement les photos",
                Sub(b)
                    If onlyphotos <> b Then FlagExit = True : FlagStop = False          ' (comme le bouton OK : le diaporama repart)
                    onlyphotos = b
                End Sub)
        Booleen(TypePhoto, "Photos au format album", Sub(b) PhotoAlbum = b)
        Entier(TypePhoto, "Album : photos à la fois (max.)", {2, 3, 4, 5, 6, 8, 10, 12}, "", Sub(v) AlbumNombreMax = v)
        Booleen(TypePhoto, "Album : nombre de photos aléatoire", Sub(b) AlbumNombreAleatoire = b)
        Booleen(TypePhoto, "Photos au format 16/9", Sub(b) FormatPhoto169 = b)
        Booleen(TypePhoto, "Afficher la localité", Sub(b) AfficheLieu = b)
        Booleen(TypePhoto, "Afficher la date", Sub(b) AfficheDate = b)
        Booleen(TypePhoto, "Légende centrée sous le PIP", Sub(b) LegendeCentreeSousPip = b)
        Booleen(TypePhoto, "Localité et date dans les bandes noires", Sub(b) BandesActives = b)
        Choix(TypePhoto, "Coin de la légende", Coins, Sub(i) LegendeCoin = CodesCoins(i))
        Choix(TypePhoto, "Place de la localité dans les bandes", {"En haut", "Au milieu", "En bas"}, Sub(i) BandesPosition = {"Haut", "Milieu", "Bas"}(i))
        Entier(TypePhoto, "Taille de la légende", pourcents, "%", Sub(v) LegendeTaillePourcent = v)
        Entier(TypePhoto, "Taille de la localité dans les bandes", pourcents, "%", Sub(v) BandesTaillePourcent = v)
        Booleen(TypePhoto, "Animer les photos", Sub(b) AnimerLesPhotos = b)
        Choix(TypePhoto, "Arrivée des photos", {"Au hasard", "Fondu", "Poussée", "Volet", "Zoom", "Flou", "Cercle", "Fondu au noir", "Ouverture", "Stores", "Losange", "Recouvrement", "Rotation", "Retournement", "Fondu lent", "Aucune"},
              Sub(i) AnimationArrivee = i)
        Choix(TypePhoto, "Mouvement des photos", {"Au hasard", "Avancer", "Reculer", "Glisser", "Balancer", "Aucun"}, Sub(i) AnimationMouvement = i)
        Choix(TypePhoto, "Zoom des photos", {"Normal", "Fort", "Très fort", "Aucun"}, Sub(i) AnimationPhotoForce = i)
        Choix(TypePhoto, "Couleurs des photos", {"Normales", "Noir et blanc"}, Sub(i) AnimationPhotoCouleurs = i)

        ' ---- Videos (type Video Mode) ----
        Entier(TypeVideo, "Séquence de vidéos", {30, 60, 90, 120, 180, 240, 300, 600}, "s", Sub(v) DurréeVideo = v)
        Entier(TypeVideo, "Taille du PIP par défaut", {10, 15, 20, 25, 30, 40, 50}, "%", Sub(v) PipSizeDefault = v)
        Booleen(TypeVideo, "Seulement les vidéos",
                Sub(b)
                    If onlyvideos <> b Then FlagExit = True : FlagStop = False
                    onlyvideos = b
                End Sub)
        Booleen(TypeVideo, "Plan B en cas d'erreur", Sub(b) PlanB = b)
        Booleen(TypeVideo, "Fenêtre sans rebord",
                Sub(b)
                    Borderless = b
                    If FullScreen <> True Then Form3.FormBorderStyle = If(b, System.Windows.Forms.FormBorderStyle.None, System.Windows.Forms.FormBorderStyle.Sizable)
                End Sub)
        Booleen(TypeVideo, "Volume boost", Sub(b) VLCBoost = b)
        Entier(TypeVideo, "Valeur du volume boost", {10, 20, 30, 40, 50}, "%", Sub(v) VolumeVLCBoost = v)
        Booleen(TypeVideo, "Animer les vidéos", Sub(b) AnimerLesVideos = b)
        Choix(TypeVideo, "Transition des vidéos", {"Au hasard", "Fondu", "Poussée", "Volet", "Cercle", "Zoom", "Fondu au noir", "Ouverture", "Stores", "Fondu lent", "Aucune"},
              Sub(i) AnimationVideo = i)
        Choix(TypeVideo, "Mouvement des vidéos", {"Au hasard", "Avancer", "Reculer", "Glisser", "Aucun"}, Sub(i) AnimationVideoMouvement = i)
        Choix(TypeVideo, "Zoom des vidéos", {"Normal", "Fort", "Très fort", "Aucun"}, Sub(i) AnimationVideoForce = i)
        Choix(TypeVideo, "Couleurs des vidéos", {"Normales", "Noir et blanc"}, Sub(i) AnimationVideoCouleurs = i)
        Choix(TypeVideo, "Bandes noires des vidéos", {"Les laisser", "Agrandir la vidéo"}, Sub(i) AnimationVideoBandes = If(i = 1, 2, 0))
        Booleen(TypeVideo, "Fond flou des vidéos", Sub(b) FondFlouVideo = b)

        ' ---- Pochette de l'album (type Son) ----
        Booleen(TypeSon, "Pochette de l'album", Sub(b) PochetteChange(Sub() DeezerCarte = b))
        Choix(TypeSon, "Place de la pochette", Coins, Sub(i) PochetteChange(Sub() DeezerCarteCoin = CodesCoins(i)))
        Entier(TypeSon, "Taille de la pochette", {50, 75, 100, 150, 200}, "%", Sub(v) PochetteChange(Sub() DeezerCartePourcent = v))
        Entier(TypeSon, "Pochette affichée pendant", {5, 10, 15, 30, 60}, "s", Sub(v) PochetteChange(Sub() DeezerCarteSecondes = v))
        Ajouter(TypeSon, "Pochette toujours affichée", Sub() PochetteChange(Sub() DeezerCarteSecondes = 0))

        ' ---- Autres ----
        Booleen(TypeAutres, "Mode hors ligne (sans Internet)", Sub(b) SansInternet = b)
        Booleen(TypeAutres, "Contrôle à distance",
                Sub(b)
                    If ControlAdistance <> b Then
                        ControlAdistance = b
                        Call GestionControlAdistance(b)
                    End If
                End Sub)
        For i As Integer = 0 To IptvSources.NombreIptv - 1
            Dim rang As Integer = i
            Ajouter(TypeAutres, "Mettre à jour la liste IPTV : " & (i + 1), Sub() MajIptv(rang))
        Next
        Ajouter(TypeAutres, "Mettre à jour la liste IPTV : toutes", Sub() MajIptv(-1))
        Ajouter(TypeAutres, "Chercher et installer la mise à jour de Slide", Sub() MajSlide())

        _parCle = New Dictionary(Of String, Entree)
        For Each e As Entree In _entrees
            _parCle(Cle(e.Type, e.Texte)) = e
        Next
    End Sub

    Private Function Cle(type As String, action As String) As String
        Return UCase(If(type, "").Trim()) & "|" & UCase(If(action, "").Trim())
    End Function

    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les actions de ce type de tache (vide pour un type sans reglage).</summary>
    Friend Function Actions(type As String) As List(Of String)
        Construire()
        Dim liste As New List(Of String)
        For Each e As Entree In _entrees
            If String.Equals(e.Type, If(type, "").Trim(), StringComparison.OrdinalIgnoreCase) Then liste.Add(e.Texte)
        Next
        Return liste
    End Function

    ''' <summary>Vrai si l'action est un reglage de ce registre.</summary>
    Friend Function Connue(type As String, action As String) As Boolean
        Construire()
        Return _parCle.ContainsKey(Cle(type, action))
    End Function

    ''' <summary>Execute le reglage (a l'heure de la ligne). Rend Vrai si l'action est un reglage du registre.</summary>
    Friend Function Executer(type As String, action As String) As Boolean
        Construire()
        Dim e As Entree = Nothing
        If Not _parCle.TryGetValue(Cle(type, action), e) Then Return False
        Try
            e.Appliquer()
            DiagLog("Minuterie : réglage '" & e.Texte & "' (" & e.Type & ")")
        Catch ex As Exception
            DiagLog("Minuterie : réglage '" & action & "' impossible : " & ex.Message)
        End Try
        Return True
    End Function

    ' ------------------------------------------------------------------------------------------


    ' ------------------------------------------------------------------------------------------
    ' Groupes de reglages pour la fenetre de choix multiples (FormReglages) : une ligne par reglage, avec ses valeurs.
    ' ------------------------------------------------------------------------------------------

    Friend Class GroupeReglage
        Friend Libelle As String = ""
        Friend Valeurs As New List(Of String)       ' l'action complete de chaque valeur ; vide : reglage sans valeur (la ligne est l'action elle-meme)
        Friend EstOnOff As Boolean                  ' paire « X ON » / « X OFF » d'un appareil Tuya : deux boutons ON / OFF sur la ligne (au lieu d'une liste)
    End Class

    Private Const SeparateurValeur As String = " : "

    ''' <summary>Les reglages de ce type, groupes par libelle ("Animer les photos" : Oui / Non...). <paramref name="statiques"/> : les actions
    ''' deja proposees par le type (Croissant, Aleatoire OFF...), sans valeur, placees en premier.</summary>
    Friend Function Groupes(type As String, statiques As IEnumerable(Of String)) As List(Of GroupeReglage)
        Construire()
        Dim liste As New List(Of GroupeReglage)
        If statiques IsNot Nothing Then
            Dim actions As List(Of String) = statiques.ToList()
            ' Tuya (30/10/2026) : « Applique Salon ON » et « Applique Salon OFF » ne font plus qu'UNE ligne (« Applique Salon », liste ON / OFF) ; l'action ecrite dans la tache reste « Applique Salon ON ».
            Dim dejaFaits As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim jumelles As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)
            If String.Equals(If(type, "").Trim(), "Tuya", StringComparison.OrdinalIgnoreCase) Then
                For Each s As String In actions
                    Dim base As String = BaseOnOff(s)
                    If base = "" Then Continue For
                    If Not jumelles.ContainsKey(base) Then jumelles(base) = New List(Of String)
                    jumelles(base).Add(s)
                Next
            End If
            For Each s As String In actions
                Dim base As String = If(jumelles.Count > 0, BaseOnOff(s), "")
                If base <> "" AndAlso jumelles(base).Count >= 2 Then
                    If dejaFaits.Add(base) Then
                        Dim g As New GroupeReglage With {.Libelle = base, .EstOnOff = (jumelles(base).Count = 2)}
                        g.Valeurs.AddRange(jumelles(base).OrderBy(Function(a) If(a.EndsWith(" ON", StringComparison.OrdinalIgnoreCase), 0, 1)))
                        liste.Add(g)
                    End If
                Else
                    liste.Add(New GroupeReglage With {.Libelle = s})
                End If
            Next
        End If
        For Each e As Entree In _entrees
            If Not String.Equals(e.Type, If(type, "").Trim(), StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim coupe As Integer = e.Texte.IndexOf(SeparateurValeur, StringComparison.Ordinal)
            If coupe <= 0 Then
                liste.Add(New GroupeReglage With {.Libelle = e.Texte})
                Continue For
            End If
            Dim libelle As String = e.Texte.Substring(0, coupe)
            Dim groupe As GroupeReglage = liste.FirstOrDefault(Function(g) g.Valeurs.Count > 0 AndAlso g.Libelle = libelle)
            If groupe Is Nothing Then
                groupe = New GroupeReglage With {.Libelle = libelle}
                liste.Add(groupe)
            End If
            groupe.Valeurs.Add(e.Texte)
        Next
        Return liste
    End Function
    ''' <summary>"Applique Salon ON" -> "Applique Salon" ; "" si l'action ne finit pas par ON ou OFF.</summary>
    Private Function BaseOnOff(action As String) As String
        If action.EndsWith(" ON", StringComparison.OrdinalIgnoreCase) Then Return action.Substring(0, action.Length - 3).Trim()
        If action.EndsWith(" OFF", StringComparison.OrdinalIgnoreCase) Then Return action.Substring(0, action.Length - 4).Trim()
        Return ""
    End Function

    Private Sub PochetteChange(changement As Action)
        changement()
        Pochette()
    End Sub

    Private Sub Pochette()
        Try
            CarteDeezer.Rafraichir()
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Telecharge la liste IPTV numero <paramref name="i"/> (toutes si -1), en tache de fond ; les films et series d'un compte Xtream aussi.</summary>
    Private Sub MajIptv(i As Integer)
        Task.Run(
            Sub()
                Try
                    Dim debut As Integer = If(i < 0, 0, i)
                    Dim fin As Integer = If(i < 0, IptvSources.NombreIptv - 1, i)
                    For n As Integer = debut To fin
                        If IptvSources.UrlIptv(n) = "" Then Continue For
                        Dim erreur As String = ""
                        Dim ok As Boolean = IptvSources.Telecharger(n, erreur)
                        If ok AndAlso IptvSources.EstXtream(n) Then
                            Dim e1 As String = "", e2 As String = ""
                            IptvSources.TelechargerCatalogue(n, IptvSources.GenreIptv.Films, e1)
                            IptvSources.TelechargerCatalogue(n, IptvSources.GenreIptv.Series, e2)
                        End If
                        DiagLog("Minuterie : liste IPTV " & (n + 1) & If(ok, " mise à jour", " non téléchargée (" & erreur & ")"))
                    Next
                Catch ex As Exception
                    DiagLog("Minuterie : mise à jour IPTV impossible : " & ex.Message)
                End Try
            End Sub)
    End Sub

    ''' <summary>Cherche une version plus recente de Slide ; s'il y en a une, elle est installee et Slide se relance (comme la verification quotidienne).</summary>
    Private Async Sub MajSlide()
        Try
            DiagLog("Minuterie : recherche d'une mise à jour de Slide")
            Dim bilan As BilanMaj = Await MiseAJour.Verifier(True, Nothing)
            DiagLog("Mise a jour : " & bilan.Message)
            If bilan.Resultat = ResultatMaj.Preparee Then
                DiagLog("Mise a jour : installation demandée par la minuterie (Slide se relance)")
                RelanceDeNuit.RelancerMaintenant()
            End If
        Catch ex As Exception
            DiagLog("Minuterie : mise à jour de Slide impossible : " & ex.Message)
        End Try
    End Sub

End Module
