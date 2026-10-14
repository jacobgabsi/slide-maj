Option Strict On
Option Explicit On

Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Text.RegularExpressions

''' <summary>
''' Plusieurs listes IPTV (demande de Jacob, 13/10/2026 : "onglet IPTV, rajoute la possibilite d'avoir
''' plusieurs IPTV" ; trois suffisent), dont des comptes XTREAM ("mon 2eme IPTV ne marche pas car ce n'est
''' pas un .m3u mais un xtream").
'''
''' Trois sources, chacune avec son NOM, son ADRESSE et, pour un compte Xtream, son UTILISATEUR et son
''' MOT DE PASSE, reglees dans le cadre "IPTV" de l'onglet Divers. La premiere est l'IPTV d'avant : meme
''' reglage (IPTV_Url), meme liste.
'''
''' Deux sortes de sources :
'''  - liste .m3u : l'adresse est celle de la liste, telechargee telle quelle ;
'''  - compte Xtream : l'adresse est celle du serveur (http://serveur:port) et l'utilisateur et le mot de
'''    passe sont renseignes. Slide interroge l'API du serveur (player_api.php) et fabrique la liste
'''    (data\iptv_N.m3u), au meme format que celles que Slide sait deja lire. Certains serveurs refusent
'''    de donner leur liste .m3u (get.php repond une erreur) alors que l'API fonctionne.
'''    Une adresse "...get.php?username=...&password=..." est aussi acceptee : si le serveur refuse la
'''    liste, l'utilisateur et le mot de passe qu'elle porte servent a interroger l'API.
'''    Les films et les series du compte sont reprises a part (data\iptv_N_films.m3u, iptv_N_series.m3u),
'''    au premier besoin : voir plus bas.
'''
''' Le reste de Slide lit UN fichier de chaines, data\iptv.m3u (DossierNflixParDefault) : choix d'une
''' chaine, rafraichissement du jour... Il n'a donc pas change. Ce fichier est la liste de la source
''' ACTIVE ; chaque source garde la sienne a part (data\iptv_1.m3u, iptv_2.m3u, iptv_3.m3u) et la
''' fenetre de choix des chaines (Form14) permet de changer de source active.
'''
''' Une tache de la minuterie garde l'adresse de sa chaine (!/Url:) : elle ne depend pas de la liste.
''' Elle note en plus le nom de sa source (!/Source:), pour information.
'''
''' (Les noms des membres portent "Iptv" : le code ancien de Slide a des variables implicites "Nom", "Url",
''' "Fichier"... qu'un membre de ce nom ferait confondre.)
''' </summary>
Friend Module IptvSources

    ''' <summary>Nombre de sources IPTV.</summary>
    Friend Const NombreIptv As Integer = 3

    Private _selection As Integer = -1

    ''' <summary>Numero (0 a 2) de la source active : celle dont la liste est dans data\iptv.m3u.</summary>
    Friend ReadOnly Property SelectionIptv As Integer
        Get
            If _selection < 0 Then _selection = Math.Max(0, Math.Min(NombreIptv - 1, My.Settings.IPTV_Source))
            Return _selection
        End Get
    End Property

    ''' <summary>Nom d'une source ("IPTV 1" tant qu'elle n'en a pas).</summary>
    Friend Function NomIptv(i As Integer) As String
        Dim n As String = ""
        Select Case i
            Case 0 : n = My.Settings.IPTV_Nom1
            Case 1 : n = My.Settings.IPTV_Nom2
            Case 2 : n = My.Settings.IPTV_Nom3
        End Select
        n = Nettoyer(n)
        Return If(n = "", "IPTV " & (i + 1), n)
    End Function

    ''' <summary>
    ''' Le bloc que donne un fournisseur Xtream, colle tel quel dans le champ adresse :
    ''' "Domain:http://serveur:port,Username:xxx,Password:yyy" (13/10/2026 : c'est ce que contenait la 2eme IPTV
    ''' de Jacob). Rend Vrai s'il contient une adresse, un utilisateur ET un mot de passe ; ils sont alors
    ''' separes. Une adresse "...get.php?username=...&password=..." n'est pas un bloc (":" seulement).
    ''' </summary>
    Private Function LireLeBloc(texte As String, ByRef serveur As String, ByRef utilisateur As String, ByRef motDePasse As String) As Boolean
        serveur = "" : utilisateur = "" : motDePasse = ""
        If String.IsNullOrWhiteSpace(texte) Then Return False
        Dim u As Match = Regex.Match(texte, "(?i)\buser(?:name)?\s*:\s*([^,;\s]+)")
        Dim p As Match = Regex.Match(texte, "(?i)\bpass(?:word)?\s*:\s*([^,;\s]+)")
        Dim d As Match = Regex.Match(texte, "(?i)(https?://[^,;\s]+)")
        If Not (u.Success AndAlso p.Success AndAlso d.Success) Then Return False
        utilisateur = u.Groups(1).Value.Trim()
        motDePasse = p.Groups(1).Value.Trim()
        serveur = ServeurDe(d.Groups(1).Value)
        Return serveur <> "" AndAlso utilisateur <> "" AndAlso motDePasse <> ""
    End Function

    ''' <summary>Une adresse collee depuis la page du fournisseur porte souvent son etiquette ("Domain:http://...",
    ''' "URL : http://...", guillemets) : tout ce qui precede "http://" ou "https://" est retire. Sans cela,
    ''' "Domain:http://..." n'est pas une adresse (13/10/2026 : "The 'domain' scheme is not supported").</summary>
    Private Function NettoyerAdresse(adresse As String) As String
        Dim a As String = If(adresse, "").Trim().Trim(""""c, "'"c, "<"c, ">"c).Trim()
        Dim p As Integer = a.IndexOf("http://", StringComparison.OrdinalIgnoreCase)
        Dim q As Integer = a.IndexOf("https://", StringComparison.OrdinalIgnoreCase)
        If p < 0 OrElse (q >= 0 AndAlso q < p) Then p = q
        If p > 0 Then a = a.Substring(p)
        Return a.Trim()
    End Function

    ''' <summary>Le reglage de l'adresse tel quel (peut etre un bloc colle du fournisseur).</summary>
    Private Function UrlBrute(i As Integer) As String
        Select Case i
            Case 0 : Return If(My.Settings.IPTV_Url, "")
            Case 1 : Return If(My.Settings.IPTV_Url2, "")
            Case 2 : Return If(My.Settings.IPTV_Url3, "")
        End Select
        Return ""
    End Function

    ''' <summary>Adresse d'une source : celle de sa liste .m3u, ou celle du serveur Xtream ("" : pas configuree).</summary>
    Friend Function UrlIptv(i As Integer) As String
        Dim a As String = UrlBrute(i)
        Dim serveur As String = "", u As String = "", p As String = ""
        If LireLeBloc(a, serveur, u, p) Then Return serveur
        Return NettoyerAdresse(a)
    End Function

    ''' <summary>Utilisateur du compte Xtream d'une source ("" : liste .m3u).</summary>
    Friend Function UtilisateurIptv(i As Integer) As String
        Dim u As String = ""
        Select Case i
            Case 0 : u = My.Settings.IPTV_Utilisateur1
            Case 1 : u = My.Settings.IPTV_Utilisateur2
            Case 2 : u = My.Settings.IPTV_Utilisateur3
        End Select
        u = If(u, "").Trim()
        If u = "" Then
            Dim s As String = "", ub As String = "", p As String = ""
            If LireLeBloc(UrlBrute(i), s, ub, p) Then Return ub
        End If
        Return u
    End Function

    ''' <summary>Mot de passe du compte Xtream (garde chiffre dans les reglages, comme celui de Deezer).</summary>
    Friend Function MotDePasseIptv(i As Integer) As String
        Dim p As String = ""
        Select Case i
            Case 0 : p = My.Settings.IPTV_MotDePasse1
            Case 1 : p = My.Settings.IPTV_MotDePasse2
            Case 2 : p = My.Settings.IPTV_MotDePasse3
        End Select
        Dim clair As String = MusiqueDeezer.Deproteger(p)
        If clair = "" Then
            Dim s As String = "", u As String = "", pb As String = ""
            If LireLeBloc(UrlBrute(i), s, u, pb) Then Return pb
        End If
        Return clair
    End Function

    ''' <summary>La liste gardee de cette source.</summary>
    Friend Function FichierIptv(i As Integer) As String
        Return Path.Combine(Path.GetDirectoryName(DossierNflixParDefault), "iptv_" & (i + 1) & ".m3u")
    End Function

    ''' <summary>Un nom ne contient ni retour a la ligne ni le separateur des reglages d'une tache ("!/").</summary>
    Private Function Nettoyer(texte As String) As String
        Dim t As String = If(texte, "").Replace("!/", " ").Replace(vbCr, " ").Replace(vbLf, " ").Trim()
        Return If(t.Length > 24, t.Substring(0, 24).Trim(), t)
    End Function

    ''' <summary>Au lancement de Slide : l'adresse active (IPTV_url) est celle de la source active.</summary>
    Friend Sub Initialiser()
        Try
            ReconcilierLaSourceActive()
        Catch ex As Exception
            DiagLog("IPTV : source active : " & ex.Message)
        End Try
        IPTV_url = UrlIptv(SelectionIptv)
    End Sub

    ''' <summary>Enregistre ce qui est saisi dans les options. Un reglage d'une source qui n'est pas active a
    ''' change : sa liste gardee ne vaut plus rien, elle est supprimee (la suivante sera telechargee). Celle
    ''' de la source active se met a jour par "Maj" ou par le rafraichissement du jour, comme avant.</summary>
    Friend Sub Enregistrer(noms As String(), urls As String(), utilisateurs As String(), motsDePasse As String())
        For i As Integer = 0 To NombreIptv - 1
            Dim nouveauNom As String = Nettoyer(If(i < noms.Length, noms(i), ""))
            Dim nouvelleUrl As String = If(i < urls.Length, NettoyerAdresse(urls(i)), "")
            Dim nouvelUtilisateur As String = If(i < utilisateurs.Length, If(utilisateurs(i), "").Trim(), "")
            Dim nouveauMotDePasse As String = If(i < motsDePasse.Length, If(motsDePasse(i), ""), "")
            Dim serveurBloc As String = "", utilisateurBloc As String = "", motDePasseBloc As String = ""
            If LireLeBloc(If(i < urls.Length, urls(i), ""), serveurBloc, utilisateurBloc, motDePasseBloc) Then
                nouvelleUrl = serveurBloc
                If nouvelUtilisateur = "" Then nouvelUtilisateur = utilisateurBloc
                If nouveauMotDePasse = "" Then nouveauMotDePasse = motDePasseBloc
            End If
            Dim change As Boolean = nouvelleUrl <> UrlIptv(i) OrElse nouvelUtilisateur <> UtilisateurIptv(i) OrElse
                                    nouveauMotDePasse <> MotDePasseIptv(i)
            If change Then
                For Each genre As GenreIptv In {GenreIptv.Films, GenreIptv.Series}
                    Try
                        If File.Exists(FichierDuGenre(i, genre)) Then File.Delete(FichierDuGenre(i, genre))
                    Catch ex As Exception
                    End Try
                Next
            End If
            If change AndAlso i <> SelectionIptv Then
                Try
                    If File.Exists(FichierIptv(i)) Then File.Delete(FichierIptv(i))
                Catch ex As Exception
                End Try
            End If
            Dim protege As String = If(change, MusiqueDeezer.Proteger(nouveauMotDePasse), Nothing)
            Select Case i
                Case 0
                    My.Settings.IPTV_Nom1 = nouveauNom : My.Settings.IPTV_Url = nouvelleUrl : My.Settings.IPTV_Utilisateur1 = nouvelUtilisateur
                    If change Then My.Settings.IPTV_MotDePasse1 = protege
                Case 1
                    My.Settings.IPTV_Nom2 = nouveauNom : My.Settings.IPTV_Url2 = nouvelleUrl : My.Settings.IPTV_Utilisateur2 = nouvelUtilisateur
                    If change Then My.Settings.IPTV_MotDePasse2 = protege
                Case 2
                    My.Settings.IPTV_Nom3 = nouveauNom : My.Settings.IPTV_Url3 = nouvelleUrl : My.Settings.IPTV_Utilisateur3 = nouvelUtilisateur
                    If change Then My.Settings.IPTV_MotDePasse3 = protege
            End Select
        Next
        IPTV_url = UrlIptv(SelectionIptv)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Telechargement
    ' ------------------------------------------------------------------------------------------

    Private _client As HttpClient

    Private Function Client() As HttpClient
        If _client Is Nothing Then
            _client = New HttpClient With {.Timeout = TimeSpan.FromMinutes(5)}
            ' Le meme agent que celui du rafraichissement du jour d'avant (certains serveurs refusent les inconnus).
            _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Header)
        End If
        Return _client
    End Function

    ''' <summary>"http://serveur:port" d'une adresse (avec ou sans chemin, avec ou sans "http://").</summary>
    Private Function ServeurDe(adresse As String) As String
        Dim a As String = If(adresse, "").Trim()
        If a = "" Then Return ""
        If Not a.Contains("://") Then a = "http://" & a
        Dim u As Uri = Nothing
        If Not Uri.TryCreate(a, UriKind.Absolute, u) OrElse String.IsNullOrEmpty(u.Host) Then Return ""
        Return u.Scheme & "://" & u.Authority
    End Function

    ''' <summary>Valeur d'un parametre de la requete d'une adresse ("" s'il n'y est pas).</summary>
    Private Function ParametreDe(adresse As String, nom As String) As String
        Dim q As Integer = If(adresse, "").IndexOf("?"c)
        If q < 0 Then Return ""
        For Each morceau As String In adresse.Substring(q + 1).Split("&"c)
            Dim e As Integer = morceau.IndexOf("="c)
            If e > 0 AndAlso String.Equals(morceau.Substring(0, e), nom, StringComparison.OrdinalIgnoreCase) Then
                Return Uri.UnescapeDataString(morceau.Substring(e + 1))
            End If
        Next
        Return ""
    End Function

    ''' <summary>Telecharge la liste d'une source dans son fichier, sans toucher a l'ancienne en cas d'echec.
    ''' Compte Xtream (utilisateur renseigne) : par l'API du serveur. Sinon : la liste .m3u, et si le serveur la
    ''' refuse alors que l'adresse porte un utilisateur et un mot de passe : par l'API.</summary>
    Friend Function Telecharger(i As Integer, ByRef erreur As String) As Boolean
        erreur = ""
        Dim adresse As String = UrlIptv(i)
        If adresse = "" Then
            erreur = "Cette IPTV n'a pas d'adresse."
            Return False
        End If
        Dim utilisateur As String = UtilisateurIptv(i)
        Dim motDePasse As String = MotDePasseIptv(i)
        Dim serveur As String = ServeurDe(adresse)
        Dim explicite As Boolean = utilisateur <> ""
        If Not explicite Then
            utilisateur = ParametreDe(adresse, "username")
            motDePasse = ParametreDe(adresse, "password")
        End If

        If explicite Then
            If serveur = "" Then
                erreur = "Adresse du serveur incorrecte."
                Return False
            End If
            If motDePasse = "" Then
                erreur = "Le mot de passe n'est pas renseigné pour « " & NomIptv(i) & " » (utilisateur " & utilisateur & ")." & vbCrLf &
                         "Saisissez-le dans Option, onglet Divers, rubrique IPTV (colonne « Mot de passe » de la ligne " & (i + 1) & "), puis OK."
                Return False
            End If
            Return TelechargerXtream(i, serveur, utilisateur, motDePasse, erreur)
        End If

        If TelechargerM3u(i, adresse, erreur) Then Return True
        If serveur <> "" AndAlso utilisateur <> "" AndAlso motDePasse <> "" Then
            Dim refus As String = erreur
            Dim erreurXtream As String = ""
            If TelechargerXtream(i, serveur, utilisateur, motDePasse, erreurXtream) Then
                erreur = ""
                DiagLog("IPTV " & (i + 1) & " : liste .m3u refusee par le serveur (" & refus & "), liste faite par l'API Xtream")
                Return True
            End If
            erreur = refus & vbCrLf & "Par l'API Xtream : " & erreurXtream
        End If
        Return False
    End Function

    Private Sub Terminer(i As Integer, provisoire As String)
        File.Move(provisoire, FichierIptv(i), True)
        If i = SelectionIptv Then CopierVersLActive()
    End Sub

    ''' <summary>Liste .m3u telechargee telle quelle. Une reponse qui n'en est pas une (page d'erreur, JSON) est
    ''' refusee : elle remplacerait la bonne liste.</summary>
    Private Function TelechargerM3u(i As Integer, adresse As String, ByRef erreur As String) As Boolean
        Dim provisoire As String = FichierIptv(i) & ".tmp"
        Try
            Directory.CreateDirectory(Path.GetDirectoryName(provisoire))
            If adresse.StartsWith("file:", StringComparison.OrdinalIgnoreCase) Then
                File.Copy(New Uri(adresse).LocalPath, provisoire, True)
            Else
                Dim a As String = adresse
                Task.Run(Sub()
                             Using reponse As HttpResponseMessage = Client().GetAsync(a, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult()
                                 If Not reponse.IsSuccessStatusCode Then
                                     Throw New IOException("le serveur répond " & CInt(reponse.StatusCode) & " " & reponse.ReasonPhrase)
                                 End If
                                 Using flux As Stream = reponse.Content.ReadAsStreamAsync().GetAwaiter().GetResult(),
                                       sortie As FileStream = File.Create(provisoire)
                                     flux.CopyTo(sortie)
                                 End Using
                             End Using
                         End Sub).GetAwaiter().GetResult()
            End If
            Dim debut As String
            Using lecture As New StreamReader(provisoire, Encoding.UTF8, True)
                Dim tampon(511) As Char
                Dim n As Integer = lecture.Read(tampon, 0, tampon.Length)
                debut = New String(tampon, 0, n)
            End Using
            If debut.IndexOf("#EXTM3U", StringComparison.OrdinalIgnoreCase) < 0 AndAlso debut.IndexOf("#EXTINF", StringComparison.OrdinalIgnoreCase) < 0 Then
                Throw New IOException("la réponse n'est pas une liste .m3u (compte Xtream ? renseignez l'utilisateur et le mot de passe)")
            End If
            Terminer(i, provisoire)
            Return True
        Catch ex As Exception
            erreur = ex.Message
            Try
                If File.Exists(provisoire) Then File.Delete(provisoire)
            Catch
            End Try
            DiagLog("IPTV " & (i + 1) & " : liste .m3u non telechargee (" & ex.Message & ")")
            Return False
        End Try
    End Function

    Private Function Lire(adresse As String) As String
        Dim a As String = adresse
        Return Task.Run(Function() Client().GetStringAsync(a).GetAwaiter().GetResult()).GetAwaiter().GetResult()
    End Function

    Private Function TexteDe(element As JsonElement, nom As String) As String
        Dim v As JsonElement = Nothing
        If element.ValueKind <> JsonValueKind.Object OrElse Not element.TryGetProperty(nom, v) Then Return ""
        Select Case v.ValueKind
            Case JsonValueKind.String : Return If(v.GetString(), "")
            Case JsonValueKind.Number : Return v.GetRawText()
            Case Else : Return ""
        End Select
    End Function

    ''' <summary>Dans une liste .m3u, ni guillemet ni virgule ni saut de ligne dans un nom (Slide les separe ainsi).</summary>
    Private Function PourM3u(texte As String) As String
        Return If(texte, "").Replace(""""c, "'"c).Replace(","c, " "c).Replace("!/", " ").Replace(vbCr, " ").Replace(vbLf, " ").Trim()
    End Function

    ''' <summary>Fabrique la liste d'un compte Xtream par l'API du serveur : controle du compte, categories,
    ''' chaines en direct. Une reponse refusee ne touche pas a la liste en place.</summary>
    Private Function TelechargerXtream(i As Integer, serveur As String, utilisateur As String, motDePasse As String,
                                       ByRef erreur As String) As Boolean
        Dim provisoire As String = FichierIptv(i) & ".tmp"
        Try
            Directory.CreateDirectory(Path.GetDirectoryName(provisoire))
            Dim identifiants As String = "username=" & Uri.EscapeDataString(utilisateur) & "&password=" & Uri.EscapeDataString(motDePasse)
            Dim api As String = serveur & "/player_api.php?" & identifiants

            ' 1. Le compte : accepte, actif.
            Using compte As JsonDocument = JsonDocument.Parse(Lire(api))
                Dim info As JsonElement = Nothing
                If compte.RootElement.ValueKind <> JsonValueKind.Object OrElse Not compte.RootElement.TryGetProperty("user_info", info) Then
                    Throw New IOException("réponse inattendue du serveur (ce n'est pas un compte Xtream, ou identifiants refusés)")
                End If
                Dim auth As String = TexteDe(info, "auth")
                If auth <> "" AndAlso auth <> "1" Then Throw New IOException("compte refusé (utilisateur ou mot de passe incorrect)")
                Dim etat As String = TexteDe(info, "status")
                If etat <> "" AndAlso Not String.Equals(etat, "Active", StringComparison.OrdinalIgnoreCase) Then
                    Throw New IOException("compte non actif (" & etat & ")")
                End If
            End Using

            ' 2. Les categories (les groupes de la fenetre de choix).
            Dim categories As New Dictionary(Of String, String)
            Using doc As JsonDocument = JsonDocument.Parse(Lire(api & "&action=get_live_categories"))
                If doc.RootElement.ValueKind = JsonValueKind.Array Then
                    For Each c As JsonElement In doc.RootElement.EnumerateArray()
                        Dim id As String = TexteDe(c, "category_id")
                        If id <> "" Then categories(id) = PourM3u(TexteDe(c, "category_name"))
                    Next
                End If
            End Using

            ' 3. Les chaines : le format que Slide lit (tvg-name, tvg-logo, group-title, puis l'adresse du flux).
            Dim nombre As Integer = 0
            Dim base As String = serveur & "/live/" & Uri.EscapeDataString(utilisateur) & "/" & Uri.EscapeDataString(motDePasse) & "/"
            Using doc As JsonDocument = JsonDocument.Parse(Lire(api & "&action=get_live_streams"))
                If doc.RootElement.ValueKind <> JsonValueKind.Array Then Throw New IOException("liste des chaînes illisible")
                Using w As New StreamWriter(provisoire, False, New UTF8Encoding(False))
                    w.WriteLine("#EXTM3U")
                    For Each e As JsonElement In doc.RootElement.EnumerateArray()
                        Dim id As String = TexteDe(e, "stream_id")
                        If id = "" Then Continue For
                        Dim nom As String = PourM3u(TexteDe(e, "name"))
                        If nom = "" Then nom = "Chaîne " & id
                        Dim groupe As String = Nothing
                        If Not categories.TryGetValue(TexteDe(e, "category_id"), groupe) OrElse String.IsNullOrEmpty(groupe) Then groupe = "Divers"
                        Dim logo As String = PourM3u(TexteDe(e, "stream_icon")).Replace(" ", "%20")
                        w.WriteLine("#EXTINF:-1 tvg-id="""" tvg-name=""" & nom & """ tvg-logo=""" & logo & """ group-title=""" & groupe & """," & nom)
                        w.WriteLine(base & id & ".ts")
                        nombre += 1
                    Next
                End Using
            End Using
            If nombre = 0 Then Throw New IOException("le compte n'a aucune chaîne en direct")
            Terminer(i, provisoire)
            DiagLog("IPTV " & (i + 1) & " : liste Xtream faite par l'API (" & nombre & " chaines, " & categories.Count & " groupes)")
            Return True
        Catch ex As Exception
            erreur = If(TypeOf ex Is JsonException, "réponse du serveur illisible (" & ex.Message & ")", ex.Message)
            Try
                If File.Exists(provisoire) Then File.Delete(provisoire)
            Catch
            End Try
            DiagLog("IPTV " & (i + 1) & " : liste Xtream non faite (" & erreur & ")")
            Return False
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Films et series d'un compte Xtream (demande de Jacob, 13/10/2026 : "ajoute aussi les films et
    ' series VOD"). Le catalogue d'un compte est enorme (100 604 films et 23 323 series pour la 2eme IPTV
    ' de Jacob) : il est garde a part des chaines (data\iptv_N_films.m3u, data\iptv_N_series.m3u),
    ' fabrique au premier besoin puis une fois par jour, et la fenetre Nflix l'affiche a la place des
    ' chaines quand on choisit "Films" ou "Series". Les chaines (data\iptv.m3u) ne changent pas.
    '
    ' Un film est une ligne comme une chaine (son adresse est celle du film). Une serie n'a pas d'adresse
    ' a elle : ses episodes ne s'obtiennent que serie par serie (get_series_info). La ligne d'une serie
    ' porte donc "slide-serie:<numero>" a la place de l'adresse, et ses episodes sont demandes quand
    ' on la choisit (EpisodesDeLaSerie).
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Ce que la fenetre de choix affiche.</summary>
    Friend Enum GenreIptv
        Chaines = 0
        Films = 1
        Series = 2
    End Enum

    ''' <summary>A la place de l'adresse, dans le catalogue des series.</summary>
    Friend Const PrefixeSerie As String = "slide-serie:"

    ''' <summary>Genre affiche par la fenetre de choix. Les chaines a chaque ouverture (Form4, Choix_IPTV).</summary>
    Friend GenreAffiche As GenreIptv = GenreIptv.Chaines

    ''' <summary>Un episode d'une serie.</summary>
    Friend Class EpisodeIptv
        Friend Saison As Integer
        Friend Numero As Integer
        Friend TitreEpisode As String = ""
        Friend AdresseEpisode As String = ""
        Public Overrides Function ToString() As String
            Return Code() & "   " & TitreEpisode
        End Function
        ''' <summary>"S01E02".</summary>
        Friend Function Code() As String
            Return "S" & Saison.ToString("00") & "E" & Numero.ToString("00")
        End Function
    End Class

    ''' <summary>Le catalogue d'une source ("iptv_2_films.m3u"). Pour les chaines : la liste de la source.</summary>
    Friend Function FichierDuGenre(i As Integer, genre As GenreIptv) As String
        Dim dossier As String = Path.GetDirectoryName(DossierNflixParDefault)
        Select Case genre
            Case GenreIptv.Films : Return Path.Combine(dossier, "iptv_" & (i + 1) & "_films.m3u")
            Case GenreIptv.Series : Return Path.Combine(dossier, "iptv_" & (i + 1) & "_series.m3u")
        End Select
        Return FichierIptv(i)
    End Function

    ''' <summary>La liste que la fenetre de choix lit : les chaines de la source active (data\iptv.m3u), ou
    ''' son catalogue de films ou de series.</summary>
    Friend Function ListeAffichee() As String
        If GenreAffiche = GenreIptv.Chaines Then Return DossierNflixParDefault
        Return FichierDuGenre(SelectionIptv, GenreAffiche)
    End Function

    ''' <summary>Serveur, utilisateur et mot de passe d'une source qui est un compte Xtream (saisis, ou portes
    ''' par une adresse get.php). Faux pour une liste .m3u ordinaire.</summary>
    Friend Function CompteXtream(i As Integer, ByRef serveur As String, ByRef utilisateur As String, ByRef motDePasse As String) As Boolean
        Dim adresse As String = UrlIptv(i)
        serveur = ServeurDe(adresse)
        utilisateur = UtilisateurIptv(i)
        motDePasse = MotDePasseIptv(i)
        If utilisateur = "" Then
            utilisateur = ParametreDe(adresse, "username")
            motDePasse = ParametreDe(adresse, "password")
        End If
        Return serveur <> "" AndAlso utilisateur <> "" AndAlso motDePasse <> ""
    End Function

    ''' <summary>La source est un compte Xtream (elle a donc des films et des series a chercher).</summary>
    Friend Function EstXtream(i As Integer) As Boolean
        Dim s As String = "", u As String = "", p As String = ""
        Return CompteXtream(i, s, u, p)
    End Function

    ''' <summary>Nombre d'entrees d'une liste .m3u (0 si elle n'existe pas).</summary>
    Friend Function NombreDEntrees(fichier As String) As Integer
        Dim n As Integer = 0
        Try
            If Not File.Exists(fichier) Then Return 0
            For Each ligne As String In File.ReadLines(fichier, Encoding.UTF8)
                If ligne.StartsWith("#EXTINF", StringComparison.Ordinal) Then n += 1
            Next
        Catch ex As Exception
        End Try
        Return n
    End Function

    ''' <summary>Fabrique le catalogue (films ou series) d'un compte Xtream par l'API du serveur, sans toucher a
    ''' l'ancien en cas d'echec. Les chaines : Telecharger.</summary>
    Friend Function TelechargerCatalogue(i As Integer, genre As GenreIptv, ByRef erreur As String) As Boolean
        erreur = ""
        If genre = GenreIptv.Chaines Then Return Telecharger(i, erreur)
        Dim serveur As String = "", utilisateur As String = "", motDePasse As String = ""
        If Not CompteXtream(i, serveur, utilisateur, motDePasse) Then
            erreur = "Les films et les séries demandent un compte Xtream (adresse du serveur, utilisateur et mot de passe)."
            Return False
        End If
        Dim films As Boolean = (genre = GenreIptv.Films)
        Dim fichier As String = FichierDuGenre(i, genre)
        Dim provisoire As String = fichier & ".tmp"
        Try
            Directory.CreateDirectory(Path.GetDirectoryName(provisoire))
            Dim api As String = serveur & "/player_api.php?username=" & Uri.EscapeDataString(utilisateur) & "&password=" & Uri.EscapeDataString(motDePasse)

            Dim categories As New Dictionary(Of String, String)
            Using doc As JsonDocument = JsonDocument.Parse(Lire(api & If(films, "&action=get_vod_categories", "&action=get_series_categories")))
                If doc.RootElement.ValueKind = JsonValueKind.Array Then
                    For Each c As JsonElement In doc.RootElement.EnumerateArray()
                        Dim id As String = TexteDe(c, "category_id")
                        If id <> "" Then categories(id) = PourM3u(TexteDe(c, "category_name"))
                    Next
                End If
            End Using

            Dim nombre As Integer = 0
            Dim base As String = serveur & "/movie/" & Uri.EscapeDataString(utilisateur) & "/" & Uri.EscapeDataString(motDePasse) & "/"
            Using doc As JsonDocument = JsonDocument.Parse(Lire(api & If(films, "&action=get_vod_streams", "&action=get_series")))
                If doc.RootElement.ValueKind <> JsonValueKind.Array Then Throw New IOException("catalogue illisible")
                Using w As New StreamWriter(provisoire, False, New UTF8Encoding(False))
                    w.WriteLine("#EXTM3U")
                    For Each e As JsonElement In doc.RootElement.EnumerateArray()
                        Dim id As String = TexteDe(e, If(films, "stream_id", "series_id"))
                        If id = "" Then Continue For
                        Dim nom As String = PourM3u(TexteDe(e, "name"))
                        If nom = "" Then nom = If(films, "Film ", "Série ") & id
                        Dim groupe As String = Nothing
                        If Not categories.TryGetValue(TexteDe(e, "category_id"), groupe) OrElse String.IsNullOrEmpty(groupe) Then groupe = "Divers"
                        Dim logo As String = PourM3u(TexteDe(e, If(films, "stream_icon", "cover"))).Replace(" ", "%20")
                        w.WriteLine("#EXTINF:-1 tvg-id="""" tvg-name=""" & nom & """ tvg-logo=""" & logo & """ group-title=""" & groupe & """," & nom)
                        If films Then
                            Dim extension As String = Regex.Replace(TexteDe(e, "container_extension"), "[^A-Za-z0-9]", "")
                            If extension = "" Then extension = "mp4"
                            w.WriteLine(base & id & "." & extension)
                        Else
                            w.WriteLine(PrefixeSerie & id)
                        End If
                        nombre += 1
                    Next
                End Using
            End Using
            If nombre = 0 Then Throw New IOException("le compte n'a aucun " & If(films, "film", "série"))
            File.Move(provisoire, fichier, True)
            DiagLog("IPTV " & (i + 1) & " : catalogue des " & If(films, "films", "series") & " fait par l'API (" & nombre & ", " & categories.Count & " categories)")
            Return True
        Catch ex As Exception
            erreur = If(TypeOf ex Is JsonException, "réponse du serveur illisible (" & ex.Message & ")", ex.Message)
            Try
                If File.Exists(provisoire) Then File.Delete(provisoire)
            Catch
            End Try
            DiagLog("IPTV " & (i + 1) & " : catalogue des " & If(films, "films", "series") & " non fait (" & erreur & ")")
            Return False
        End Try
    End Function

    ''' <summary>Le catalogue d'un genre est utilisable : present et de moins d'un jour, ou fabrique maintenant.
    ''' Si le serveur ne repond pas, un catalogue plus ancien est garde (mieux que rien).</summary>
    Friend Function AssurerLeCatalogue(i As Integer, genre As GenreIptv, ByRef erreur As String) As Boolean
        erreur = ""
        If genre = GenreIptv.Chaines Then Return True          ' rechercher recree la liste des chaines si elle manque
        Dim fichier As String = FichierDuGenre(i, genre)
        Try
            If File.Exists(fichier) AndAlso (Date.Now - File.GetLastWriteTime(fichier)).TotalHours < 24 Then Return True
        Catch ex As Exception
        End Try
        If TelechargerCatalogue(i, genre, erreur) Then Return True
        Return File.Exists(fichier)
    End Function

    ''' <summary>
    ''' Les listes de la recherche "partout" de Nflix : celle qui est affichee d'abord, puis les autres contenus du
    ''' compte Xtream - chaines, films, series (leur catalogue est fait au besoin, quelques secondes la
    ''' premiere fois). <paramref name="avertissement"/> : ce qui n'a pas pu etre charge.
    ''' </summary>
    Friend Function ListesDeToutLeContenu(i As Integer, affiche As GenreIptv, ByRef avertissement As String) As List(Of String)
        avertissement = ""
        Dim liste As New List(Of String)
        liste.Add(If(affiche = GenreIptv.Chaines, DossierNflixParDefault, FichierDuGenre(i, affiche)))
        If Not EstXtream(i) Then Return liste
        For Each g As GenreIptv In {GenreIptv.Chaines, GenreIptv.Films, GenreIptv.Series}
            If g = affiche Then Continue For
            If g = GenreIptv.Chaines Then
                If File.Exists(DossierNflixParDefault) Then liste.Add(DossierNflixParDefault)
                Continue For
            End If
            Dim erreur As String = ""
            If AssurerLeCatalogue(i, g, erreur) Then
                liste.Add(FichierDuGenre(i, g))
            Else
                avertissement &= If(avertissement = "", "", vbCrLf) & If(g = GenreIptv.Films, "Films", "Séries") & " : " & erreur
            End If
        Next
        Return liste
    End Function

    ''' <summary>Les episodes d'une serie du catalogue, classes par saison puis par numero. Liste vide (et l'erreur
    ''' dite) si le serveur ne repond pas ou si la serie n'a aucun episode.</summary>
    Friend Function EpisodesDeLaSerie(i As Integer, numeroDeSerie As String, ByRef erreur As String) As List(Of EpisodeIptv)
        erreur = ""
        Dim liste As New List(Of EpisodeIptv)
        Try
            Dim serveur As String = "", utilisateur As String = "", motDePasse As String = ""
            If Not CompteXtream(i, serveur, utilisateur, motDePasse) Then
                erreur = "Cette IPTV n'est pas un compte Xtream."
                Return liste
            End If
            Dim api As String = serveur & "/player_api.php?username=" & Uri.EscapeDataString(utilisateur) & "&password=" & Uri.EscapeDataString(motDePasse)
            Dim base As String = serveur & "/series/" & Uri.EscapeDataString(utilisateur) & "/" & Uri.EscapeDataString(motDePasse) & "/"
            Using doc As JsonDocument = JsonDocument.Parse(Lire(api & "&action=get_series_info&series_id=" & Uri.EscapeDataString(numeroDeSerie)))
                Dim episodes As JsonElement = Nothing
                If doc.RootElement.ValueKind = JsonValueKind.Object AndAlso doc.RootElement.TryGetProperty("episodes", episodes) Then
                    If episodes.ValueKind = JsonValueKind.Object Then
                        For Each saison As JsonProperty In episodes.EnumerateObject()
                            AjouterLesEpisodes(liste, saison.Value, base, saison.Name)
                        Next
                    ElseIf episodes.ValueKind = JsonValueKind.Array Then
                        For Each saison As JsonElement In episodes.EnumerateArray()
                            AjouterLesEpisodes(liste, saison, base, "")
                        Next
                    End If
                End If
            End Using
            liste.Sort(Function(a As EpisodeIptv, b As EpisodeIptv) If(a.Saison <> b.Saison, a.Saison.CompareTo(b.Saison), a.Numero.CompareTo(b.Numero)))
            If liste.Count = 0 Then erreur = "Cette série n'a aucun épisode pour le moment."
        Catch ex As Exception
            erreur = If(TypeOf ex Is JsonException, "réponse du serveur illisible (" & ex.Message & ")", ex.Message)
            liste.Clear()
            DiagLog("IPTV " & (i + 1) & " : episodes de la serie " & numeroDeSerie & " non lus (" & erreur & ")")
        End Try
        Return liste
    End Function

    ''' <summary>Les episodes d'une saison : un tableau d'episodes (ou un episode seul).</summary>
    Private Sub AjouterLesEpisodes(liste As List(Of EpisodeIptv), element As JsonElement, base As String, nomDeSaison As String)
        If element.ValueKind = JsonValueKind.Array Then
            For Each e As JsonElement In element.EnumerateArray()
                AjouterLesEpisodes(liste, e, base, nomDeSaison)
            Next
            Exit Sub
        End If
        If element.ValueKind <> JsonValueKind.Object Then Exit Sub
        Dim id As String = TexteDe(element, "id")
        If id = "" Then Exit Sub
        Dim ep As New EpisodeIptv
        Integer.TryParse(TexteDe(element, "season"), ep.Saison)
        If ep.Saison = 0 Then Integer.TryParse(nomDeSaison, ep.Saison)
        Integer.TryParse(TexteDe(element, "episode_num"), ep.Numero)
        Dim titre As String = TexteDe(element, "title").Replace("!/", " ").Trim()
        ep.TitreEpisode = If(titre = "", "Épisode " & ep.Numero, titre)
        Dim extension As String = Regex.Replace(TexteDe(element, "container_extension"), "[^A-Za-z0-9]", "")
        If extension = "" Then extension = "mp4"
        ep.AdresseEpisode = base & id & "." & extension
        liste.Add(ep)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Telechargement d'un film ou d'un episode (bouton "Telecharger" de la fenetre Nflix ; demande de
    ' Jacob, 13/10/2026 : "rajoute un bouton pour telecharger un film rapidement").
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Le dossier des films telecharges : Videos\Nflix.</summary>
    Friend Function DossierDesTelechargements() As String
        Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Nflix")
    End Function

    ''' <summary>Un titre devient un nom de fichier (caracteres interdits retires, longueur limitee).</summary>
    Friend Function NomDeFichier(titre As String) As String
        Dim n As String = If(titre, "")
        For Each c As Char In Path.GetInvalidFileNameChars()
            n = n.Replace(c, " "c)
        Next
        n = Regex.Replace(n, "\s+", " ").Trim().TrimEnd("."c, " "c)
        If n.Length > 120 Then n = n.Substring(0, 120).Trim()
        Return If(n = "", "Film", n)
    End Function

    ''' <summary>L'extension du fichier d'une adresse de film (".mkv"), "" si elle n'en a pas de plausible.</summary>
    Friend Function ExtensionDeLAdresse(adresse As String) As String
        Try
            Dim e As String = Path.GetExtension(New Uri(adresse).AbsolutePath).ToLowerInvariant()
            If e.Length >= 2 AndAlso e.Length <= 5 Then Return e
        Catch ex As Exception
        End Try
        Return ""
    End Function

    ''' <summary>L'adresse est celle d'un film ou d'un episode (pas d'une chaine en direct, qui ne finit jamais).</summary>
    Friend Function EstTelechargeable(adresse As String) As Boolean
        If String.IsNullOrWhiteSpace(adresse) OrElse adresse.StartsWith(PrefixeSerie, StringComparison.Ordinal) Then Return False
        If adresse.Contains("/movie/", StringComparison.OrdinalIgnoreCase) OrElse adresse.Contains("/series/", StringComparison.OrdinalIgnoreCase) Then Return True
        Select Case ExtensionDeLAdresse(adresse)
            Case ".mkv", ".mp4", ".avi", ".m4v", ".mov", ".wmv", ".flv", ".mpg", ".mpeg", ".webm"
                Return True
        End Select
        Return False
    End Function

    ' Telechargement RAPIDE et SUR (demande de Jacob, 13/10/2026 : "peut tu accelerer le telechargement ?",
    ' apres l'echec "The response ended prematurely" : le serveur avait coupe la connexion au milieu d'un film
    ' de plusieurs Go). Deux changements :
    '  - le fichier est demande par PORTIONS (Range) de 8 Mo, par plusieurs connexions a la fois : 4 sur le
    '    compte de Jacob, mesure : 11,5 Mo/s avec 1 connexion, 38 Mo/s avec 4 ;
    '  - une portion coupee est redemandee toute seule (jusqu'a 3 echecs de suite par connexion, puis des
    '    tours de reprise, avec moins de connexions si le serveur en refuse) : une coupure n'arrete plus le
    '    telechargement. Les portions finies sont notees dans "cible.part.suivi" : un telechargement
    '    interrompu (annule, Slide ferme) repart ou il en etait.
    ' Un serveur qui ne sait pas servir des portions : un seul flux, comme avant.

    ''' <summary>Nombre de connexions en parallele d'un telechargement.</summary>
    Friend ConnexionsDeTelechargement As Integer = 4

    ''' <summary>Plafond de connexions vers le serveur pour TOUS les telechargements a la fois (plusieurs films
    ''' en meme temps se partagent ces connexions : le compte n'est pas noye).</summary>
    Private ReadOnly _limiteDeConnexions As New Threading.SemaphoreSlim(6, 6)

    ''' <summary>Taille d'une portion demandee au serveur.</summary>
    Friend TailleDeBloc As Long = 8L * 1048576

    Private Function ClientDeTelechargement() As HttpClient
        Dim gestionnaire As New SocketsHttpHandler With {.MaxConnectionsPerServer = 16, .ConnectTimeout = TimeSpan.FromSeconds(20)}
        Dim c As New HttpClient(gestionnaire) With {.Timeout = Threading.Timeout.InfiniteTimeSpan}
        c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Header)
        Return c
    End Function

    ''' <summary>Taille du fichier et le serveur sait-il en servir des portions ?</summary>
    Private Async Function SonderLeFichier(client As HttpClient, adresse As String, jeton As Threading.CancellationToken) As Task(Of KeyValuePair(Of Long, Boolean))
        Using requete As New HttpRequestMessage(HttpMethod.Get, adresse)
            requete.Headers.Range = New Headers.RangeHeaderValue(0, 0)
            Using reponse As HttpResponseMessage = Await client.SendAsync(requete, HttpCompletionOption.ResponseHeadersRead, jeton).ConfigureAwait(False)
                If Not reponse.IsSuccessStatusCode Then
                    Throw New IOException("le serveur répond " & CInt(reponse.StatusCode) & " " & reponse.ReasonPhrase)
                End If
                Dim type As String = If(reponse.Content.Headers.ContentType?.MediaType, "")
                If type.StartsWith("text/", StringComparison.OrdinalIgnoreCase) OrElse type.Contains("json", StringComparison.OrdinalIgnoreCase) Then
                    Throw New IOException("le serveur n'a pas donné le film (réponse : " & type & ")")
                End If
                Dim intervalle As Headers.ContentRangeHeaderValue = reponse.Content.Headers.ContentRange
                If reponse.StatusCode = Net.HttpStatusCode.PartialContent AndAlso intervalle IsNot Nothing AndAlso intervalle.Length.HasValue Then
                    Return New KeyValuePair(Of Long, Boolean)(intervalle.Length.Value, True)
                End If
                Dim taille As Long = If(reponse.Content.Headers.ContentLength.HasValue, reponse.Content.Headers.ContentLength.Value, -1L)
                Return New KeyValuePair(Of Long, Boolean)(taille, False)
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Telecharge <paramref name="adresse"/> dans <paramref name="cible"/> (ecrit sous "cible.part", renomme a la fin).
    ''' Rend la taille du fichier. <paramref name="progres"/> recoit (octets recus, taille totale ou -1) quelques
    ''' fois par seconde. Annule par <paramref name="jeton"/> : le travail fait est garde pour la reprise.
    ''' </summary>
    Friend Async Function TelechargerFichier(adresse As String, cible As String, progres As IProgress(Of KeyValuePair(Of Long, Long)),
                                             jeton As Threading.CancellationToken) As Task(Of Long)
        Directory.CreateDirectory(Path.GetDirectoryName(cible))
        Using client As HttpClient = ClientDeTelechargement()
            Dim sonde As KeyValuePair(Of Long, Boolean) = Await SonderLeFichier(client, adresse, jeton).ConfigureAwait(False)
            If sonde.Value AndAlso sonde.Key > 0 Then
                Return Await TelechargerEnPortions(client, adresse, cible, sonde.Key, progres, jeton).ConfigureAwait(False)
            End If
            Await _limiteDeConnexions.WaitAsync(jeton).ConfigureAwait(False)
            Try
                Return Await TelechargerEnUnFlux(client, adresse, cible, progres, jeton).ConfigureAwait(False)
            Finally
                _limiteDeConnexions.Release()
            End Try
        End Using
    End Function

    Private Async Function TelechargerEnPortions(client As HttpClient, adresse As String, cible As String, total As Long,
                                                 progres As IProgress(Of KeyValuePair(Of Long, Long)),
                                                 jeton As Threading.CancellationToken) As Task(Of Long)
        Dim partiel As String = cible & ".part"
        Dim suivi As String = cible & ".part.suivi"
        Dim bloc As Long = Math.Max(65536L, TailleDeBloc)
        Dim nb As Integer = CInt((total + bloc - 1) \ bloc)
        Dim faits(nb - 1) As Boolean

        ' Reprise : le suivi des portions finies, ou le debut d'un ancien fichier d'un seul tenant.
        Dim lu As Boolean = False
        Try
            If File.Exists(suivi) Then
                Dim lignes As String() = File.ReadAllLines(suivi)
                If lignes.Length >= 2 AndAlso lignes(0) = total & ";" & bloc AndAlso lignes(1).Length = nb Then
                    For k As Integer = 0 To nb - 1
                        faits(k) = (lignes(1)(k) = "1"c)
                    Next
                    lu = True
                End If
            End If
        Catch ex As Exception
        End Try
        If Not lu AndAlso File.Exists(partiel) Then
            Dim taille As Long = New FileInfo(partiel).Length
            If taille < total Then
                For k As Integer = 0 To CInt(Math.Min(nb, taille \ bloc)) - 1
                    faits(k) = True
                Next
            End If
        End If
        Dim tailleDe As Func(Of Integer, Long) = Function(k As Integer) Math.Min(total, (k + 1) * bloc) - k * bloc

        Dim recu As Long = 0
        For k As Integer = 0 To nb - 1
            If faits(k) Then recu += tailleDe(k)
        Next

        Dim verrou As New Object
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim dernierRapport As Long = -1000
        Dim dernierSuivi As Long = 0
        Dim noter As Action =
            Sub()
                SyncLock verrou
                    Try
                        Dim sb As New StringBuilder(nb)
                        For k As Integer = 0 To nb - 1
                            sb.Append(If(faits(k), "1"c, "0"c))
                        Next
                        File.WriteAllText(suivi, total & ";" & bloc & vbLf & sb.ToString())
                    Catch ex As Exception
                    End Try
                End SyncLock
            End Sub
        Dim rapporter As Action(Of Boolean) =
            Sub(forcer As Boolean)
                If progres Is Nothing Then Exit Sub
                SyncLock verrou
                    If forcer OrElse chrono.ElapsedMilliseconds - dernierRapport >= 300 Then
                        dernierRapport = chrono.ElapsedMilliseconds
                        progres.Report(New KeyValuePair(Of Long, Long)(Threading.Interlocked.Read(recu), total))
                    End If
                End SyncLock
            End Sub

        Dim connexions As Integer = Math.Max(1, ConnexionsDeTelechargement)
        Dim toursSansProgres As Integer = 0
        Dim derniereErreur As String = ""
        Try
            Using poignee As Microsoft.Win32.SafeHandles.SafeFileHandle = File.OpenHandle(partiel, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, FileOptions.Asynchronous)
                If RandomAccess.GetLength(poignee) <> total Then RandomAccess.SetLength(poignee, total)
                Do
                    Dim attente As New System.Collections.Concurrent.ConcurrentQueue(Of Integer)
                    For k As Integer = 0 To nb - 1
                        If Not faits(k) Then attente.Enqueue(k)
                    Next
                    If attente.IsEmpty Then Exit Do
                    Dim avant As Integer = attente.Count
                    Dim refus As Integer = 0
                    Dim travailleurs As New List(Of Task)
                    For w As Integer = 1 To Math.Min(connexions, attente.Count)
                        travailleurs.Add(Task.Run(
                            Async Function() As Task
                                Dim echecs As Integer = 0
                                Dim k As Integer
                                Do While attente.TryDequeue(k)
                                    jeton.ThrowIfCancellationRequested()
                                    Dim debut As Long = k * bloc
                                    Dim longueur As Long = tailleDe(k)
                                    Dim ecrit As Long = 0
                                    Dim pause As Integer = 0
                                    Await _limiteDeConnexions.WaitAsync(jeton).ConfigureAwait(False)
                                    Try
                                        Using requete As New HttpRequestMessage(HttpMethod.Get, adresse)
                                            requete.Headers.Range = New Headers.RangeHeaderValue(debut, debut + longueur - 1)
                                            Using reponse As HttpResponseMessage = Await client.SendAsync(requete, HttpCompletionOption.ResponseHeadersRead, jeton).ConfigureAwait(False)
                                                If reponse.StatusCode <> Net.HttpStatusCode.PartialContent Then
                                                    Threading.Interlocked.Increment(refus)
                                                    Throw New IOException("le serveur répond " & CInt(reponse.StatusCode))
                                                End If
                                                Using flux As Stream = Await reponse.Content.ReadAsStreamAsync(jeton).ConfigureAwait(False)
                                                    Dim tampon(262143) As Byte
                                                    Do While ecrit < longueur
                                                        Dim n As Integer = Await flux.ReadAsync(tampon, 0, CInt(Math.Min(tampon.Length, longueur - ecrit)), jeton).ConfigureAwait(False)
                                                        If n = 0 Then Throw New IOException("connexion coupée par le serveur")
                                                        Await RandomAccess.WriteAsync(poignee, New ReadOnlyMemory(Of Byte)(tampon, 0, n), debut + ecrit, jeton).ConfigureAwait(False)
                                                        ecrit += n
                                                        Threading.Interlocked.Add(recu, n)
                                                        rapporter(False)
                                                    Loop
                                                End Using
                                            End Using
                                        End Using
                                        faits(k) = True
                                        echecs = 0
                                        If chrono.ElapsedMilliseconds - Threading.Interlocked.Read(dernierSuivi) >= 3000 Then
                                            Threading.Interlocked.Exchange(dernierSuivi, chrono.ElapsedMilliseconds)
                                            noter()
                                        End If
                                    Catch ex As OperationCanceledException When jeton.IsCancellationRequested
                                        Threading.Interlocked.Add(recu, -ecrit)
                                        Throw
                                    Catch ex As Exception
                                        Threading.Interlocked.Add(recu, -ecrit)
                                        derniereErreur = ex.Message
                                        attente.Enqueue(k)                     ' la portion sera redemandee
                                        echecs += 1
                                        If echecs >= 3 Then Exit Do            ' cette connexion renonce (le tour suivant en refera)
                                        pause = 500 * echecs * echecs
                                    Finally
                                        _limiteDeConnexions.Release()
                                    End Try
                                    If pause > 0 Then Await Task.Delay(pause, jeton).ConfigureAwait(False)
                                Loop
                            End Function))
                    Next
                    Await Task.WhenAll(travailleurs).ConfigureAwait(False)
                    noter()
                    Dim restantes As Integer = faits.Count(Function(x) Not x)
                    If restantes = 0 Then Exit Do
                    ' Un tour sans portion finie : le serveur ne suit pas. Moins de connexions, puis on renonce.
                    If restantes >= avant Then
                        toursSansProgres += 1
                        If toursSansProgres >= 4 Then
                            Throw New IOException("le serveur coupe la connexion (" & derniereErreur & "). Relancez le téléchargement plus tard pour le reprendre.")
                        End If
                    Else
                        toursSansProgres = 0
                    End If
                    If refus > 0 OrElse restantes >= avant Then connexions = Math.Max(1, connexions \ 2)
                    Await Task.Delay(1500, jeton).ConfigureAwait(False)
                Loop
            End Using
        Catch
            noter()
            Throw
        End Try
        rapporter(True)
        If faits.Any(Function(x) Not x) Then Throw New IOException("téléchargement incomplet")
        Try
            If File.Exists(suivi) Then File.Delete(suivi)
        Catch ex As Exception
        End Try
        File.Move(partiel, cible, True)
        Return total
    End Function

    ''' <summary>Un seul flux (le serveur ne sait pas servir des portions). Un fichier partiel est repris si le serveur
    ''' le permet.</summary>
    Private Async Function TelechargerEnUnFlux(client As HttpClient, adresse As String, cible As String,
                                               progres As IProgress(Of KeyValuePair(Of Long, Long)),
                                               jeton As Threading.CancellationToken) As Task(Of Long)
        Dim partiel As String = cible & ".part"
        Dim debut As Long = If(File.Exists(partiel), New FileInfo(partiel).Length, 0L)
        Using requete As New HttpRequestMessage(HttpMethod.Get, adresse)
            If debut > 0 Then requete.Headers.Range = New Headers.RangeHeaderValue(debut, Nothing)
            Using reponse As HttpResponseMessage = Await client.SendAsync(requete, HttpCompletionOption.ResponseHeadersRead, jeton).ConfigureAwait(False)
                If debut > 0 AndAlso reponse.StatusCode = Net.HttpStatusCode.RequestedRangeNotSatisfiable Then
                    File.Delete(partiel)
                    Return Await TelechargerEnUnFlux(client, adresse, cible, progres, jeton).ConfigureAwait(False)
                End If
                If Not reponse.IsSuccessStatusCode Then
                    Throw New IOException("le serveur répond " & CInt(reponse.StatusCode) & " " & reponse.ReasonPhrase)
                End If
                Dim reprise As Boolean = debut > 0 AndAlso reponse.StatusCode = Net.HttpStatusCode.PartialContent
                If Not reprise Then debut = 0
                Dim total As Long = -1
                If reponse.Content.Headers.ContentLength.HasValue Then total = reponse.Content.Headers.ContentLength.Value + debut
                Dim recu As Long = debut
                Using flux As Stream = Await reponse.Content.ReadAsStreamAsync(jeton).ConfigureAwait(False)
                    Using sortie As New FileStream(partiel, If(reprise, FileMode.Append, FileMode.Create), FileAccess.Write, FileShare.Read, 1 << 20, True)
                        Dim tampon(1048575) As Byte
                        Dim chrono As Stopwatch = Stopwatch.StartNew()
                        Dim dernier As Long = -1000
                        Do
                            Dim n As Integer = Await flux.ReadAsync(tampon, 0, tampon.Length, jeton).ConfigureAwait(False)
                            If n = 0 Then Exit Do
                            Await sortie.WriteAsync(tampon, 0, n, jeton).ConfigureAwait(False)
                            recu += n
                            If progres IsNot Nothing AndAlso chrono.ElapsedMilliseconds - dernier >= 300 Then
                                dernier = chrono.ElapsedMilliseconds
                                progres.Report(New KeyValuePair(Of Long, Long)(recu, total))
                            End If
                        Loop
                    End Using
                End Using
                If total >= 0 AndAlso recu < total Then Throw New IOException("téléchargement incomplet (" & recu & " octets sur " & total & ")")
                If progres IsNot Nothing Then progres.Report(New KeyValuePair(Of Long, Long)(recu, total))
                File.Move(partiel, cible, True)
                Return recu
            End Using
        End Using
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Fiche d'un film ou d'une serie : le pitch (demande de Jacob, 13/10/2026 : "quand je click sur un
    ' film rajoute le pitch"). Le catalogue ne l'a pas : il est demande au serveur du compte quand on
    ' choisit un titre (get_vod_info pour un film, get_series_info pour une serie), puis garde en memoire.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Ce que le serveur dit d'un film ou d'une serie.</summary>
    Friend Class FicheIptv
        Friend Synopsis As String = ""
        Friend Genre As String = ""
        Friend Annee As String = ""
        Friend Duree As String = ""
        Friend Note As String = ""
        Friend Realisateur As String = ""
        Friend Acteurs As String = ""

        ''' <summary>Le texte affiche sous l'apercu : une ligne de details, puis le resume.</summary>
        Friend Function EnTexte() As String
            Dim details As New List(Of String)
            If Genre <> "" Then details.Add(Genre)
            If Annee <> "" Then details.Add(Annee)
            If Duree <> "" Then details.Add(Duree)
            If Note <> "" Then details.Add("note " & Note)
            Dim lignes As New List(Of String)
            If details.Count > 0 Then lignes.Add(String.Join("  ·  ", details))
            lignes.Add(If(Synopsis = "", "(pas de résumé pour ce titre)", Synopsis))
            If Realisateur <> "" Then lignes.Add("De : " & Realisateur)
            If Acteurs <> "" Then lignes.Add("Avec : " & Acteurs)
            Return String.Join(vbCrLf, lignes)
        End Function
    End Class

    Private ReadOnly _fiches As New Dictionary(Of String, FicheIptv)

    ''' <summary>"01:52:03" ou "6723" (secondes) ou "112" (minutes) -> "1 h 52". "" si on ne comprend pas.</summary>
    Private Function DureeLisible(brut As String) As String
        Dim b As String = If(brut, "").Trim()
        If b = "" Then Return ""
        Dim minutes As Integer = -1
        Dim ts As TimeSpan
        If b.Contains(":"c) AndAlso TimeSpan.TryParse(b, ts) Then
            minutes = CInt(Math.Round(ts.TotalMinutes))
        Else
            Dim n As Double
            If Double.TryParse(b, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, n) Then
                minutes = If(n > 600, CInt(Math.Round(n / 60)), CInt(Math.Round(n)))
            End If
        End If
        If minutes <= 0 Then Return ""
        Return If(minutes >= 60, (minutes \ 60) & " h " & (minutes Mod 60).ToString("00"), minutes & " min")
    End Function

    Private Function Premier(element As JsonElement, ParamArray noms As String()) As String
        For Each n As String In noms
            Dim t As String = TexteDe(element, n).Trim()
            If t <> "" Then Return t
        Next
        Return ""
    End Function

    ''' <summary>La fiche lue dans l'objet "info" d'une reponse du serveur.</summary>
    Private Function FicheDe(info As JsonElement) As FicheIptv
        Dim f As New FicheIptv
        If info.ValueKind <> JsonValueKind.Object Then Return f
        f.Synopsis = Premier(info, "plot", "description", "overview").Replace(vbCr, "").Replace("<br>", vbCrLf).Trim()
        f.Genre = Premier(info, "genre")
        Dim sortie As String = Premier(info, "releasedate", "release_date", "releaseDate", "year")
        Dim m As Match = Regex.Match(sortie, "(19|20)\d\d")
        f.Annee = If(m.Success, m.Value, "")
        f.Duree = DureeLisible(Premier(info, "duration", "episode_run_time"))
        Dim note As String = Premier(info, "rating", "rating_5based")
        Dim x As Double
        If Double.TryParse(note, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, x) AndAlso x > 0 Then
            f.Note = x.ToString("0.0", Globalization.CultureInfo.CurrentCulture)
        End If
        f.Realisateur = Premier(info, "director")
        f.Acteurs = Premier(info, "cast", "actors")
        Return f
    End Function

    ''' <summary>
    ''' La fiche d'un titre du catalogue de la source <paramref name="i"/> : cle "film:ID" ou "serie:ID". Rend Nothing si
    ''' le serveur ne repond pas. A appeler en tache de fond (une requete).
    ''' </summary>
    Friend Function FicheDuTitre(i As Integer, cle As String) As FicheIptv
        Dim gardee As FicheIptv = Nothing
        Dim cleComplete As String = i & "|" & cle
        SyncLock _fiches
            If _fiches.TryGetValue(cleComplete, gardee) Then Return gardee
        End SyncLock
        Try
            Dim serveur As String = "", utilisateur As String = "", motDePasse As String = ""
            If Not CompteXtream(i, serveur, utilisateur, motDePasse) Then Return Nothing
            Dim film As Boolean = cle.StartsWith("film:", StringComparison.Ordinal)
            Dim numero As String = cle.Substring(cle.IndexOf(":"c) + 1)
            If numero = "" Then Return Nothing
            Dim api As String = serveur & "/player_api.php?username=" & Uri.EscapeDataString(utilisateur) & "&password=" & Uri.EscapeDataString(motDePasse) &
                                If(film, "&action=get_vod_info&vod_id=", "&action=get_series_info&series_id=") & Uri.EscapeDataString(numero)
            Dim fiche As FicheIptv
            Using doc As JsonDocument = JsonDocument.Parse(Lire(api))
                Dim info As JsonElement = Nothing
                If doc.RootElement.ValueKind <> JsonValueKind.Object OrElse Not doc.RootElement.TryGetProperty("info", info) Then Return Nothing
                fiche = FicheDe(info)
            End Using
            SyncLock _fiches
                If _fiches.Count > 400 Then _fiches.Clear()
                _fiches(cleComplete) = fiche
            End SyncLock
            Return fiche
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>Le numero d'un film dans son adresse (".../movie/utilisateur/mot de passe/1234.mkv" -> "1234"), "" sinon.</summary>
    Friend Function NumeroDuFilm(adresse As String) As String
        Dim a As String = If(adresse, "")
        Dim p As Integer = a.IndexOf("/movie/", StringComparison.OrdinalIgnoreCase)
        If p < 0 Then Return ""
        Dim dernier As String = a.Substring(a.LastIndexOf("/"c) + 1)
        Dim point As Integer = dernier.IndexOf("."c)
        If point > 0 Then dernier = dernier.Substring(0, point)
        Dim n As Long
        Return If(Long.TryParse(dernier, n), dernier, "")
    End Function

    ' ------------------------------------------------------------------------------------------
    ' La liste ACTIVE (data\iptv.m3u) et la source a qui elle appartient.
    '
    ' Demande de Jacob, 13/10/2026 : "quand je click sur source, il ne se passe rien". Cause : la source
    ' choisie (IPTV_Source) n'etait pas enregistree a la fermeture de Slide, alors que data\iptv.m3u gardait
    ' la liste de la source choisie la veille. Au lancement suivant Slide croyait que iptv.m3u etait la
    ' liste de la source 1 : changer de source rangeait cette liste (celle de la source 2) sous le nom de
    ' la source 1, et les deux sources affichaient la meme liste.
    ' Maintenant : le numero de la source choisie est enregistre, et un petit fichier (iptv_active.txt) dit
    ' a quelle source appartient data\iptv.m3u. Une liste gardee dont les chaines ne sont pas du serveur de
    ' sa source (liste rangee par erreur) est refaite au lieu d'etre reprise.
    ' ------------------------------------------------------------------------------------------

    Private Function FichierMarque() As String
        Return Path.Combine(Path.GetDirectoryName(DossierNflixParDefault), "iptv_active.txt")
    End Function

    ''' <summary>La source a qui appartient data\iptv.m3u (-1 : inconnue, anciennes versions).</summary>
    Private Function SourceDeLaListeActive() As Integer
        Try
            Dim n As Integer
            If File.Exists(FichierMarque()) AndAlso Integer.TryParse(File.ReadAllText(FichierMarque()).Trim(), n) AndAlso n >= 0 AndAlso n < NombreIptv Then Return n
        Catch ex As Exception
        End Try
        Return -1
    End Function

    Private Sub NoterLaSourceDeLaListeActive(i As Integer)
        Try
            File.WriteAllText(FichierMarque(), i.ToString())
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>La liste est bien celle de la source <paramref name="i"/> : pour un compte Xtream, ses flux sont
    ''' ceux du serveur du compte. Pour une liste .m3u ordinaire on ne peut pas le savoir : Vrai.</summary>
    Friend Function ListeCorrespond(i As Integer, fichier As String) As Boolean
        Try
            Dim serveur As String = "", u As String = "", p As String = ""
            If Not CompteXtream(i, serveur, u, p) OrElse Not File.Exists(fichier) Then Return True
            Dim hoteDuCompte As String = New Uri(serveur).Host
            Using lecture As New StreamReader(fichier, Encoding.UTF8, True)
                Dim ligne As String = lecture.ReadLine()
                Dim vus As Integer = 0
                Do While ligne IsNot Nothing AndAlso vus < 400
                    vus += 1
                    If ligne.Length > 0 AndAlso ligne(0) <> "#"c Then
                        Dim flux As Uri = Nothing
                        If Uri.TryCreate(ligne.Trim(), UriKind.Absolute, flux) AndAlso flux.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then
                            Return String.Equals(flux.Host, hoteDuCompte, StringComparison.OrdinalIgnoreCase)
                        End If
                    End If
                    ligne = lecture.ReadLine()
                Loop
            End Using
        Catch ex As Exception
        End Try
        Return True
    End Function

    ''' <summary>Au lancement : data\iptv.m3u est la liste de la source marquee ; sans marque (anciennes versions),
    ''' on cherche la source dont elle est la liste, et celle-ci devient la source active.</summary>
    Private Sub ReconcilierLaSourceActive()
        If Not File.Exists(DossierNflixParDefault) Then Exit Sub
        Dim marque As Integer = SourceDeLaListeActive()
        If marque >= 0 Then
            If marque <> SelectionIptv Then ChoisirLaSource(marque)
            Exit Sub
        End If
        If ListeCorrespond(SelectionIptv, DossierNflixParDefault) Then
            NoterLaSourceDeLaListeActive(SelectionIptv)
            Exit Sub
        End If
        For i As Integer = 0 To NombreIptv - 1
            If i <> SelectionIptv AndAlso UrlIptv(i) <> "" AndAlso EstXtream(i) AndAlso ListeCorrespond(i, DossierNflixParDefault) Then
                DiagLog("IPTV : la liste active est celle de la source " & (i + 1) & " : elle devient la source active")
                ChoisirLaSource(i)
                NoterLaSourceDeLaListeActive(i)
                Exit Sub
            End If
        Next
    End Sub

    ''' <summary>Retient la source choisie, aussi pour le prochain lancement.</summary>
    Private Sub ChoisirLaSource(i As Integer)
        _selection = i
        My.Settings.IPTV_Source = i
        Try
            My.Settings.Save()
        Catch ex As Exception
            DiagLog("IPTV : source choisie non enregistree : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La liste gardee de la source active devient la liste lue par Slide (data\iptv.m3u).</summary>
    Friend Sub CopierVersLActive()
        Try
            If File.Exists(FichierIptv(SelectionIptv)) Then
                File.Copy(FichierIptv(SelectionIptv), DossierNflixParDefault, True)
                NoterLaSourceDeLaListeActive(SelectionIptv)
            End If
        Catch ex As Exception
            DiagLog("IPTV : copie de la liste active : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La liste lue par Slide (data\iptv.m3u) est gardee pour la source active, SI elle lui appartient
    ''' (marque, ou a defaut les chaines du serveur de la source) : sinon elle ecraserait la liste d'une autre.</summary>
    Friend Sub MemoriserLActive()
        Try
            If Not File.Exists(DossierNflixParDefault) Then Exit Sub
            Dim marque As Integer = SourceDeLaListeActive()
            Dim sienne As Boolean = If(marque >= 0, marque = SelectionIptv, ListeCorrespond(SelectionIptv, DossierNflixParDefault))
            If sienne Then File.Copy(DossierNflixParDefault, FichierIptv(SelectionIptv), True)
        Catch ex As Exception
            DiagLog("IPTV : liste active gardee : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Change de source active. Rend Faux (et dit pourquoi) si sa liste n'est ni gardee ni
    ''' telechargeable : la source active ne change alors pas.</summary>
    Friend Function Activer(i As Integer, ByRef erreur As String) As Boolean
        erreur = ""
        If i < 0 OrElse i >= NombreIptv Then Return False
        If i = SelectionIptv Then Return True
        If UrlIptv(i) = "" AndAlso Not File.Exists(FichierIptv(i)) Then
            erreur = "L'IPTV « " & NomIptv(i) & " » n'a pas d'adresse : renseignez-la dans les options (onglet Divers)."
            Return False
        End If
        ' La liste de la source quittee est mise de cote d'abord : rien ne se perd si le telechargement echoue.
        MemoriserLActive()
        If Not File.Exists(FichierIptv(i)) OrElse Not ListeCorrespond(i, FichierIptv(i)) Then
            If UrlIptv(i) = "" Then
                erreur = "L'IPTV « " & NomIptv(i) & " » n'a pas d'adresse : renseignez-la dans les options (onglet Divers)."
                Return False
            End If
            If Not Telecharger(i, erreur) Then Return False
        End If
        ChoisirLaSource(i)
        IPTV_url = UrlIptv(i)
        CopierVersLActive()
        Return True
    End Function

End Module
