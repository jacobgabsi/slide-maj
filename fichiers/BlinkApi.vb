Option Strict On
Option Explicit On

Imports System.Net
Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports System.Text.RegularExpressions

''' <summary>
''' Cameras Blink (Amazon), etape 1 (Jacob, 26/10/2026 : "un pip avec les cameras Blink, sans Docker, pour que Slide tourne sur n'importe quel PC") : connexion au compte Blink et liste des cameras.
'''
''' Blink n'a pas d'API publique : ce module suit celle de l'application mobile, telle que la decrit la bibliotheque communautaire blinkpy (OAuth v2 avec PKCE, code de verification en deux
''' etapes, puis les services « rest-{region}.immedia-semi.com »). Elle peut changer a tout moment ; les erreurs sont donc ecrites au journal (jamais le mot de passe, les jetons ni les codes).
''' L'adresse, le mot de passe (demande de Jacob, 26/10/2026 : "login et mot de passe Blink enregistres dans data\settings"), l'identifiant de l'appareil et le jeton de renouvellement sont
''' enregistres ENSEMBLE, chiffres pour l'utilisateur Windows de ce PC (data\settings\blink.dat : propre au poste, jamais publie, illisible sur un autre PC ou un autre compte Windows) ;
''' ils ne sont jamais ecrits au journal. Le mot de passe sert a se reconnecter tout seul quand le jeton de renouvellement n'est plus accepte. La vue en direct viendra a l'etape suivante.
''' </summary>
Friend Module BlinkApi

    Private Const UrlOauth As String = "https://api.oauth.blink.com"
    Private Const UrlAutoriser As String = UrlOauth & "/oauth/v2/authorize"
    Private Const UrlConnexion As String = UrlOauth & "/oauth/v2/signin"
    Private Const UrlVerification As String = UrlOauth & "/oauth/v2/2fa/verify"
    Private Const UrlJeton As String = UrlOauth & "/oauth/token"
    Private Const UrlNiveau As String = "https://rest-prod.immedia-semi.com/api/v1/users/tier_info"
    Private Const ClientOauth As String = "ios"
    Private Const Portee As String = "client"
    Private Const Retour As String = "immedia-blink://applinks.blink.com/signin/callback"
    Private Const AgentNavigateur As String = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.1 Mobile/15E148 Safari/604.1"
    Private Const AgentJeton As String = "Blink/2511191620 CFNetwork/3860.200.71 Darwin/25.1.0"
    Private Const AgentApi As String = "61.0ANDROID_29859371"
    Private Const BuildApp As String = "ANDROID_29859371"

    Friend Class CameraBlink
        Friend Id As String = ""
        Friend Nom As String = ""
        Friend ReseauId As String = ""
        ''' <summary>"camera" (sur le module Sync), "owl" (Mini) ou "doorbell" (sonnette video) : les chemins de l'API different.</summary>
        Friend Type As String = "camera"
        Friend Miniature As String = ""
        ''' <summary>Faux si Blink annonce la camera desactivee ou hors ligne (lu dans l'ecran d'accueil) : inutile d'ouvrir son direct. Vrai par defaut et quand Blink ne dit rien.</summary>
        Friend Connectee As Boolean = True
        ''' <summary>Les champs d'etat lus pour cette camera (enabled, status, signaux...), pour le journal.</summary>
        Friend Detail As String = ""
    End Class

    Private _email As String = ""
    Private _idAppareil As String = ""
    Private _jeton As String = ""
    Private _renouvellement As String = ""
    Private _expireLe As Date = Date.MinValue
    Private _niveau As String = ""
    Private _compte As String = ""
    Private _motDePasse As String = ""
    Private _motDePasseEnAttente As String = ""

    ' Entre le mot de passe et le code de verification : meme session (cookies) et meme verificateur PKCE.
    Private _cookies As CookieContainer
    Private _csrf As String = ""
    Private _verificateur As String = ""

    ''' <summary>Vrai si un jeton de renouvellement est connu (connecte, ou connectable sans mot de passe).</summary>
    Friend ReadOnly Property EstConnecte As Boolean
        Get
            Charger()
            Return _renouvellement <> ""
        End Get
    End Property

    Friend ReadOnly Property Adresse As String
        Get
            Charger()
            Return _email
        End Get
    End Property

    Private Function Fichier() As String
        Return DossierReglages & "\blink.dat"
    End Function

    Private _charge As Boolean

    Private Sub Charger()
        If _charge Then Exit Sub
        _charge = True
        Try
            If Not IO.File.Exists(Fichier()) Then Exit Sub
            Dim json As String = MusiqueDeezer.Deproteger(IO.File.ReadAllText(Fichier()).Trim())
            If json = "" Then Exit Sub
            Using doc As JsonDocument = JsonDocument.Parse(json)
                Dim r As JsonElement = doc.RootElement
                _email = Lire(r, "email") : _idAppareil = Lire(r, "appareil") : _renouvellement = Lire(r, "renouvellement")
                _niveau = Lire(r, "niveau") : _compte = Lire(r, "compte") : _motDePasse = Lire(r, "motdepasse")
            End Using
        Catch ex As Exception
            DiagLog("Blink : fichier de connexion illisible : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La connexion enregistree (compte, jetons, mot de passe) en clair, pour l'export des parametres ("" s'il n'y en a pas). Le fichier d'export est a garder en lieu sur, comme pour les autres secrets.</summary>
    Friend Function ExporterLaConnexion() As String
        Try
            If Not IO.File.Exists(Fichier()) Then Return ""
            Return MusiqueDeezer.Deproteger(IO.File.ReadAllText(Fichier()).Trim())
        Catch ex As Exception
            DiagLog("Blink : connexion non exportee : " & ex.Message)
            Return ""
        End Try
    End Function

    ''' <summary>Reprend une connexion exportee : elle est rechiffree pour ce poste, puis relue.</summary>
    Friend Sub ImporterLaConnexion(json As String)
        If json Is Nothing OrElse json.Trim() = "" Then Exit Sub
        IO.File.WriteAllText(Fichier(), MusiqueDeezer.Proteger(json))
        _charge = False
        Charger()
    End Sub

    Private Sub Enregistrer()
        Try
            Dim json As String = JsonSerializer.Serialize(New Dictionary(Of String, String) From {
                {"email", _email}, {"appareil", _idAppareil}, {"renouvellement", _renouvellement}, {"niveau", _niveau}, {"compte", _compte}, {"motdepasse", _motDePasse}})
            IO.File.WriteAllText(Fichier(), MusiqueDeezer.Proteger(json))
        Catch ex As Exception
            DiagLog("Blink : connexion non enregistree : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Le mot de passe enregistre (pour pre-remplir la fenetre), "" s'il n'y en a pas.</summary>
    Friend ReadOnly Property MotDePasse As String
        Get
            Charger()
            Return _motDePasse
        End Get
    End Property

    ''' <summary>Oublie le compte : le fichier est efface.</summary>
    Friend Sub Deconnecter()
        _email = "" : _jeton = "" : _renouvellement = "" : _niveau = "" : _compte = "" : _motDePasse = "" : _expireLe = Date.MinValue
        _cookies = Nothing : _csrf = "" : _verificateur = ""
        Try
            If IO.File.Exists(Fichier()) Then IO.File.Delete(Fichier())
        Catch ex As Exception
        End Try
        DiagLog("Blink : compte deconnecte")
    End Sub

    Private Function Lire(e As JsonElement, nom As String) As String
        Dim v As JsonElement
        If e.ValueKind = JsonValueKind.Object AndAlso e.TryGetProperty(nom, v) Then
            If v.ValueKind = JsonValueKind.String Then Return If(v.GetString(), "")
            If v.ValueKind = JsonValueKind.Number Then Return v.ToString()
            If v.ValueKind = JsonValueKind.True Then Return "True"
            If v.ValueKind = JsonValueKind.False Then Return "False"
        End If
        Return ""
    End Function

    Private Function Client(suivreLesRedirections As Boolean) As HttpClient
        If _cookies Is Nothing Then _cookies = New CookieContainer()            ' (apres la connexion, la session est oubliee : un conteneur Nothing faisait echouer HttpClientHandler)
        Dim h As New HttpClientHandler With {.AllowAutoRedirect = suivreLesRedirections, .UseCookies = True, .CookieContainer = _cookies}
        Return New HttpClient(h) With {.Timeout = TimeSpan.FromSeconds(30)}
    End Function

    Private Function Base64Url(octets As Byte()) As String
        Return Convert.ToBase64String(octets).TrimEnd("="c).Replace("+"c, "-"c).Replace("/"c, "_"c)
    End Function

    Private Function EnFormulaire(valeurs As IEnumerable(Of KeyValuePair(Of String, String))) As FormUrlEncodedContent
        Return New FormUrlEncodedContent(valeurs)
    End Function

    ''' <summary>
    ''' Connexion avec l'adresse et le mot de passe. Rend "OK" (connecte), "2FA" (un code de verification a ete envoye : voir VerifierLeCodeAsync) ou le message d'une erreur.
    ''' </summary>
    Friend Async Function ConnecterAsync(email As String, motDePasse As String) As Task(Of String)
        Try
            Charger()
            _email = email.Trim()
            _motDePasseEnAttente = motDePasse                          ' (enregistre avec la connexion, une fois qu'elle a reussi : voir ChoisirLaRegionAsync ; un mot de passe refuse n'est pas garde)
            If _idAppareil = "" Then _idAppareil = Guid.NewGuid().ToString().ToUpperInvariant()          ' (Blink refuse un identifiant qui n'est pas un UUID)
            _cookies = New CookieContainer()
            Dim aleatoire(31) As Byte
            RandomNumberGenerator.Fill(aleatoire)
            _verificateur = Base64Url(aleatoire)
            Dim defi As String = Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(_verificateur)))

            Using suivi As HttpClient = Client(True), direct As HttpClient = Client(False)
                ' 1) la demande d'autorisation (ouvre la session de connexion)
                Dim q As String = String.Join("&", {"app_brand=blink", "app_version=50.1", "client_id=" & ClientOauth, "code_challenge=" & defi, "code_challenge_method=S256", "device_brand=Apple",
                                                    "device_model=" & Uri.EscapeDataString("iPhone16,1"), "device_os_version=26.1", "hardware_id=" & _idAppareil,
                                                    "redirect_uri=" & Uri.EscapeDataString(Retour), "response_type=code", "scope=" & Portee})
                Dim r1 As HttpResponseMessage = Await suivi.SendAsync(Requete(HttpMethod.Get, UrlAutoriser & "?" & q, AgentNavigateur))
                If Not r1.IsSuccessStatusCode Then Return Echec("demande d'autorisation", r1)

                ' 2) la page de connexion (porte le jeton anti-falsification)
                Dim r2 As HttpResponseMessage = Await suivi.SendAsync(Requete(HttpMethod.Get, UrlConnexion, AgentNavigateur))
                If Not r2.IsSuccessStatusCode Then Return Echec("page de connexion", r2)
                _csrf = JetonCsrf(Await r2.Content.ReadAsStringAsync())
                If _csrf = "" Then Return "Blink a changé sa page de connexion (jeton introuvable)."

                ' 3) l'adresse et le mot de passe
                Dim m3 As HttpRequestMessage = Requete(HttpMethod.Post, UrlConnexion, AgentNavigateur)
                m3.Headers.TryAddWithoutValidation("Origin", UrlOauth)
                m3.Headers.TryAddWithoutValidation("Referer", UrlConnexion)
                m3.Content = EnFormulaire({KV("username", _email), KV("password", motDePasse), KV("csrf-token", _csrf)})
                Dim r3 As HttpResponseMessage = Await direct.SendAsync(m3)
                Dim etat As Integer = CInt(r3.StatusCode)
                If etat = 412 Then
                    RetenirLeCompte()
                    Return "2FA"
                End If
                If etat = 202 Then
                    Dim corps As String = Await r3.Content.ReadAsStringAsync()
                    If corps.Contains("tsv_state") OrElse corps.Contains("tsv_methods") OrElse corps.Contains("next_time_in_secs") Then
                        RetenirLeCompte()
                        Return "2FA"
                    End If
                    Return Echec("connexion (reponse inattendue)", r3)
                End If
                If etat = 401 OrElse etat = 403 Then Return "Adresse ou mot de passe refusés par Blink."
                If etat < 300 OrElse etat > 399 Then Return Echec("connexion", r3)
            End Using
            Return Await TerminerAsync()
        Catch ex As Exception
            DiagLog("Blink : connexion impossible : " & ex.GetType().Name & " : " & ex.Message)
            Return "Connexion impossible : " & ex.Message
        End Try
    End Function

    ''' <summary>Blink a accepte l'adresse et le mot de passe (il demande le code de controle, ou la connexion se poursuit) : ils sont enregistres tout de suite, pour que la fenetre les pre-remplisse
    ''' a la prochaine ouverture meme si le code de controle n'a pas encore ete donne. Un mot de passe refuse n'arrive jamais ici. Pas de jeton : EstConnecte reste Faux.</summary>
    Private Sub RetenirLeCompte()
        _motDePasse = _motDePasseEnAttente
        Enregistrer()
    End Sub

    ''' <summary>Le code de verification (recu par e-mail ou SMS) : termine la connexion commencee par ConnecterAsync.</summary>
    Friend Async Function VerifierLeCodeAsync(code As String) As Task(Of String)
        Try
            DiagLog("Blink : verification du code de controle (" & code.Trim().Length & " caracteres)")
            If _cookies Is Nothing OrElse _csrf = "" Then
                DiagLog("Blink : verification impossible, la session de connexion est perdue (recommencer avec l'adresse et le mot de passe)")
                Return "Recommencez la connexion (adresse et mot de passe) : la session est perdue."
            End If
            Using direct As HttpClient = Client(False)
                Dim m As HttpRequestMessage = Requete(HttpMethod.Post, UrlVerification, AgentNavigateur)
                m.Headers.TryAddWithoutValidation("Origin", UrlOauth)
                m.Headers.TryAddWithoutValidation("Referer", UrlConnexion)
                m.Content = EnFormulaire({KV("2fa_code", code.Trim()), KV("csrf-token", _csrf), KV("remember_me", "false")})
                Dim r As HttpResponseMessage = Await direct.SendAsync(m)
                Dim corps As String = Await r.Content.ReadAsStringAsync()
                DiagLog("Blink : verification du code : HTTP " & CInt(r.StatusCode) & " : " & If(corps.Length > 200, corps.Substring(0, 200), corps))
                If CInt(r.StatusCode) <> 201 Then Return "Code refusé par Blink (HTTP " & CInt(r.StatusCode) & ")."
                If Not corps.Contains("auth-completed") Then Return "Code de vérification non accepté."
            End Using
            Return Await TerminerAsync()
        Catch ex As Exception
            DiagLog("Blink : verification impossible : " & ex.GetType().Name & " : " & ex.Message)
            Return "Vérification impossible : " & ex.Message
        End Try
    End Function

    ''' <summary>Apres le mot de passe (et le code) : le code d'autorisation puis les jetons, puis la region du compte.</summary>
    Private Async Function TerminerAsync() As Task(Of String)
        Dim code As String
        Using direct As HttpClient = Client(False)
            ' Comme blinkpy : la meme adresse d'autorisation, SANS parametres (la demande est retenue par la session et ses cookies), sans suivre la redirection : le code est dans l'adresse
            ' de retour (« immedia-blink://...?code=... »).
            Dim m As HttpRequestMessage = Requete(HttpMethod.Get, UrlAutoriser, AgentNavigateur)
            m.Headers.TryAddWithoutValidation("Referer", UrlConnexion)
            Dim r As HttpResponseMessage = Await direct.SendAsync(m)
            Dim lieuBrut As String = ""
            If r.Headers.Contains("Location") Then lieuBrut = String.Join("", r.Headers.GetValues("Location"))
            DiagLog("Blink : code d'autorisation : HTTP " & CInt(r.StatusCode) & ", redirection vers « " & Regex.Replace(lieuBrut, "code=[^&]+", "code=***") & " »")
            If lieuBrut = "" Then Return Echec("code d'autorisation", r)
            Dim valeur As Match = Regex.Match(lieuBrut, "[?&]code=([^&]+)")
            If Not valeur.Success Then Return "Blink n'a pas donné de code d'autorisation."
            code = Uri.UnescapeDataString(valeur.Groups(1).Value)
        End Using
        Dim donnees As JsonElement
        Using suivi As HttpClient = Client(True)
            Dim m As HttpRequestMessage = Requete(HttpMethod.Post, UrlJeton, AgentJeton)
            m.Content = EnFormulaire({KV("app_brand", "blink"), KV("client_id", ClientOauth), KV("code", code), KV("code_verifier", _verificateur), KV("grant_type", "authorization_code"),
                                      KV("hardware_id", _idAppareil), KV("redirect_uri", Retour), KV("scope", Portee)})
            Dim r As HttpResponseMessage = Await suivi.SendAsync(m)
            If Not r.IsSuccessStatusCode Then Return Echec("echange du code", r)
            donnees = JsonDocument.Parse(Await r.Content.ReadAsStringAsync()).RootElement.Clone()
        End Using
        Dim resultat As String = RetenirLesJetons(donnees)
        If resultat <> "OK" Then Return resultat
        _csrf = "" : _verificateur = "" : _cookies = Nothing
        Return Await ChoisirLaRegionAsync()
    End Function

    Private Function RetenirLesJetons(donnees As JsonElement) As String
        _jeton = Lire(donnees, "access_token")
        Dim r As String = Lire(donnees, "refresh_token")
        If r <> "" Then _renouvellement = r
        Dim duree As Integer
        If Not Integer.TryParse(Lire(donnees, "expires_in"), duree) OrElse duree <= 0 Then duree = 3600
        _expireLe = Date.Now.AddSeconds(duree - 60)
        If _jeton = "" Then Return "Blink n'a pas donné de jeton."
        Return "OK"
    End Function

    Private Async Function ChoisirLaRegionAsync() As Task(Of String)
        Using c As HttpClient = Client(True)
            Dim m As HttpRequestMessage = Requete(HttpMethod.Get, UrlNiveau, AgentApi)
            m.Headers.TryAddWithoutValidation("Authorization", "Bearer " & _jeton)
            Dim r As HttpResponseMessage = Await c.SendAsync(m)
            If Not r.IsSuccessStatusCode Then Return Echec("region du compte", r)
            Using doc As JsonDocument = JsonDocument.Parse(Await r.Content.ReadAsStringAsync())
                _niveau = Lire(doc.RootElement, "tier")
                _compte = Lire(doc.RootElement, "account_id")
            End Using
        End Using
        If _niveau = "" OrElse _compte = "" Then Return "Blink n'a pas donné la région du compte."
        _motDePasse = _motDePasseEnAttente : _motDePasseEnAttente = ""
        Enregistrer()
        DiagLog("Blink : connecte (region " & _niveau & ")")
        Return "OK"
    End Function

    ''' <summary>Un jeton valable : renouvele par le jeton de renouvellement quand il approche de sa fin. "" si impossible (reconnexion avec le mot de passe necessaire).</summary>
    Private Async Function JetonValableAsync() As Task(Of String)
        Charger()
        If _jeton <> "" AndAlso Date.Now < _expireLe Then Return _jeton
        If _renouvellement = "" Then Return ""
        Using c As HttpClient = Client(True)
            Dim m As HttpRequestMessage = Requete(HttpMethod.Post, UrlJeton, AgentJeton)
            m.Content = EnFormulaire({KV("grant_type", "refresh_token"), KV("refresh_token", _renouvellement), KV("client_id", ClientOauth), KV("scope", Portee), KV("hardware_id", _idAppareil)})
            Dim r As HttpResponseMessage = Await c.SendAsync(m)
            If Not r.IsSuccessStatusCode Then
                DiagLog("Blink : renouvellement du jeton refuse (" & CInt(r.StatusCode) & ")")
                If _motDePasse <> "" AndAlso _email <> "" Then
                    ' Le mot de passe enregistre permet de se reconnecter sans rien demander (si Blink exige un nouveau code de controle, il faudra le saisir dans la fenetre des cameras).
                    Dim reconnexion As String = Await ConnecterAsync(_email, _motDePasse)
                    DiagLog("Blink : reconnexion automatique : " & If(reconnexion = "OK", "reussie", If(reconnexion = "2FA", "code de controle demande", "echec")))
                    If reconnexion = "OK" Then Return _jeton
                End If
                Return ""
            End If
            Using doc As JsonDocument = JsonDocument.Parse(Await r.Content.ReadAsStringAsync())
                If RetenirLesJetons(doc.RootElement) <> "OK" Then Return ""
            End Using
        End Using
        Enregistrer()
        Return _jeton
    End Function

    Private _champsJournalises As Boolean

    ''' <summary>L'etat annonce par Blink pour une camera (29/10/2026 : « un moyen pour savoir si la camera Blink est connectee ») : « enabled » (activee), « status » et les signaux. Une camera desactivee ou dont le statut
    ''' dit qu'elle est hors ligne est marquee non connectee. Le journal garde les NOMS des champs (une fois) et les valeurs d'etat de chaque camera, jamais d'identifiant ni d'adresse.</summary>
    Private Sub EtatDeLaCamera(e As JsonElement, cam As CameraBlink)
        Try
            If Not _champsJournalises Then
                _champsJournalises = True
                DiagLog("Blink : champs d'une camera : " & String.Join(", ", e.EnumerateObject().Select(Function(p) p.Name)))
            End If
            Dim champsEtat As New List(Of String)
            For Each p As JsonProperty In e.EnumerateObject()
                Select Case p.Name
                    Case "enabled", "status", "battery", "battery_state", "onboarded", "camera_status", "usage_rate"
                        champsEtat.Add(p.Name & "=" & p.Value.ToString())
                    Case "signals"
                        If p.Value.ValueKind = JsonValueKind.Object Then
                            For Each s As JsonProperty In p.Value.EnumerateObject()
                                champsEtat.Add("signals." & s.Name & "=" & s.Value.ToString())
                            Next
                        End If
                End Select
            Next
            cam.Detail = String.Join(" ", champsEtat)
            Dim active As JsonElement
            If e.TryGetProperty("enabled", active) AndAlso active.ValueKind = JsonValueKind.False Then cam.Connectee = False
            Dim statut As JsonElement
            If e.TryGetProperty("status", statut) AndAlso statut.ValueKind = JsonValueKind.String Then
                Dim s As String = statut.GetString().ToLowerInvariant()
                If s = "offline" OrElse s = "disconnected" OrElse s = "unavailable" Then cam.Connectee = False
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Les cameras du compte (cameras sur module Sync, Mini, sonnettes), d'apres l'ecran d'accueil de l'application. Lance une exception avec un message lisible en cas d'echec.</summary>
    Friend Async Function ListerLesCamerasAsync() As Task(Of List(Of CameraBlink))
        Dim jeton As String = Await JetonValableAsync()
        If jeton = "" Then Throw New InvalidOperationException("Connexion expirée : reconnectez-vous avec votre mot de passe.")
        Dim liste As New List(Of CameraBlink)
        Using c As HttpClient = Client(True)
            ' L'ecran d'accueil en version 4 (le HTTP 426 « mise a niveau requise » de la version 3 : 26/10/2026) ; sans l'en-tete app-build, que blinkpy n'envoie pas non plus.
            Dim m As HttpRequestMessage = Requete(HttpMethod.Get, "https://rest-" & _niveau & ".immedia-semi.com/api/v4/accounts/" & _compte & "/homescreen", AgentApi)
            m.Headers.TryAddWithoutValidation("Authorization", "Bearer " & jeton)
            Dim r As HttpResponseMessage = Await c.SendAsync(m)
            If Not r.IsSuccessStatusCode Then
                Dim erreurCorps As String = Await r.Content.ReadAsStringAsync()
                DiagLog("Blink : liste des cameras refusee : HTTP " & CInt(r.StatusCode) & " : " & If(erreurCorps.Length > 200, erreurCorps.Substring(0, 200), erreurCorps))
                Throw New InvalidOperationException("Blink a refusé la liste des caméras (HTTP " & CInt(r.StatusCode) & ").")
            End If
            Using doc As JsonDocument = JsonDocument.Parse(Await r.Content.ReadAsStringAsync())
                For Each groupe As KeyValuePair(Of String, String) In {KV("cameras", "camera"), KV("owls", "owl"), KV("doorbells", "doorbell")}
                    Dim tableau As JsonElement
                    If Not doc.RootElement.TryGetProperty(groupe.Key, tableau) OrElse tableau.ValueKind <> JsonValueKind.Array Then Continue For
                    For Each e As JsonElement In tableau.EnumerateArray()
                        Dim cam As New CameraBlink With {.Id = Lire(e, "id"), .Nom = Lire(e, "name"), .ReseauId = Lire(e, "network_id"), .Type = groupe.Value, .Miniature = Lire(e, "thumbnail")}
                        EtatDeLaCamera(e, cam)
                        If cam.Id <> "" Then liste.Add(cam)
                    Next
                Next
            End Using
        End Using
        DiagLog("Blink : " & liste.Count & " camera(s) : " & String.Join(", ", liste.Select(Function(x) x.Nom & " (" & x.Type & If(x.Connectee, "", ", hors ligne") & ")")))
        DiagLog("Blink : etat des cameras : " & String.Join(" | ", liste.Select(Function(x) x.Nom & " : " & x.Detail)))
        Return liste
    End Function

    ' ------------------------------------------------------------------------------------------

    Private Function KV(cle As String, valeur As String) As KeyValuePair(Of String, String)
        Return New KeyValuePair(Of String, String)(cle, valeur)
    End Function

    Private Function Requete(methode As HttpMethod, adresse As String, agent As String) As HttpRequestMessage
        Dim m As New HttpRequestMessage(methode, adresse)
        m.Headers.TryAddWithoutValidation("User-Agent", agent)
        If agent = AgentNavigateur Then
            m.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
            m.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9")
        End If
        Return m
    End Function

    Private Function Echec(etape As String, r As HttpResponseMessage) As String
        DiagLog("Blink : echec a l'etape « " & etape & " » : HTTP " & CInt(r.StatusCode))
        Return "Blink a refusé l'étape « " & etape & " » (HTTP " & CInt(r.StatusCode) & ")."
    End Function

    ''' <summary>Le jeton anti-falsification : dans la page, une balise script d'identifiant « oauth-args » de type JSON, cle « csrf-token ».</summary>
    Private Function JetonCsrf(html As String) As String
        Try
            Dim m As Match = Regex.Match(html, "<script[^>]*id=""oauth-args""[^>]*>(.*?)</script>", RegexOptions.Singleline Or RegexOptions.IgnoreCase)
            If Not m.Success Then Return ""
            Using doc As JsonDocument = JsonDocument.Parse(m.Groups(1).Value.Trim())
                Return Lire(doc.RootElement, "csrf-token")
            End Using
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Vue en direct (etape 2) : demande de la session, puis surveillance de sa commande. Le flux lui-meme est dans BlinkDirect.
    ' ------------------------------------------------------------------------------------------

    Friend Class SessionBlink
        Friend Serveur As String = ""
        Friend CommandeId As String = ""
        Friend ReseauId As String = ""
        Friend IntervalleSecondes As Integer = 15
        Friend JetonDirect As String = ""
    End Class

    Private Function AdresseDeLApi() As String
        Return "https://rest-" & _niveau & ".immedia-semi.com"
    End Function

    Private Function EnteteApi(methode As HttpMethod, adresse As String, jeton As String) As HttpRequestMessage
        Dim m As HttpRequestMessage = Requete(methode, adresse, AgentApi)
        m.Headers.TryAddWithoutValidation("Authorization", "Bearer " & jeton)
        m.Headers.TryAddWithoutValidation("app-build", BuildApp)
        m.Headers.TryAddWithoutValidation("locale", "fr_FR")
        m.Headers.TryAddWithoutValidation("x-blink-time-zone", TimeZoneInfo.Local.Id)
        Return m
    End Function

    ''' <summary>Demande une session de vue en direct pour cette camera. Lance une exception avec un message lisible si Blink refuse.</summary>
    Friend Async Function DemanderLeDirectAsync(cam As CameraBlink, intention As String) As Task(Of SessionBlink)
        Dim jeton As String = Await JetonValableAsync()
        If jeton = "" Then Throw New InvalidOperationException("Connexion expirée : reconnectez-vous avec votre mot de passe.")
        ' Versions d'API essayees dans l'ordre : Blink repond 426 (« mise a jour requise ») a une version qu'il ne sert plus a ce type d'appli.
        Dim versions As String() = If(cam.Type = "camera" OrElse cam.Type = "", {"v6", "v5", "v4", "v3"}, {"v2"})
        Dim r As HttpResponseMessage = Nothing
        Dim corps As String = ""
        Using c As HttpClient = Client(True)
            For Each v As String In versions
                Dim chemin As String
                Select Case cam.Type
                    Case "owl" : chemin = "/api/" & v & "/accounts/" & _compte & "/networks/" & cam.ReseauId & "/owls/" & cam.Id & "/liveview"
                    Case "doorbell" : chemin = "/api/" & v & "/accounts/" & _compte & "/networks/" & cam.ReseauId & "/doorbells/" & cam.Id & "/liveview"
                    Case Else : chemin = "/api/" & v & "/accounts/" & _compte & "/networks/" & cam.ReseauId & "/cameras/" & cam.Id & "/liveview"
                End Select
                Dim m As HttpRequestMessage = EnteteApi(HttpMethod.Post, AdresseDeLApi() & chemin, jeton)
                m.Content = New StringContent("{""intent"":""" & intention & """,""motion_event_start_time"":null}", Encoding.UTF8, "application/json")
                r = Await c.SendAsync(m)
                corps = Await r.Content.ReadAsStringAsync()
                Dim apercu As String = Regex.Replace(corps, """(liveview_token|token)""\s*:\s*""[^""]*""", """$1"":""***""")
                DiagLog("Blink : demande de direct " & v & " (" & cam.Nom & ", " & cam.Type & ") : HTTP " & CInt(r.StatusCode) & " : " & If(apercu.Length > 300, apercu.Substring(0, 300), apercu))
                If r.IsSuccessStatusCode OrElse (CInt(r.StatusCode) <> 426 AndAlso CInt(r.StatusCode) <> 404) Then Exit For
            Next
            If Not r.IsSuccessStatusCode Then
                Dim raison As Match = Regex.Match(corps, """message""\s*:\s*""([^""]*)""")
                Throw New InvalidOperationException("Blink a refusé le direct (HTTP " & CInt(r.StatusCode) & If(raison.Success, " : " & raison.Groups(1).Value, "") & ").")
            End If
            Using doc As JsonDocument = JsonDocument.Parse(corps)
                Dim s As New SessionBlink With {.Serveur = Lire(doc.RootElement, "server"), .CommandeId = Lire(doc.RootElement, "command_id"), .ReseauId = cam.ReseauId,
                                                .JetonDirect = Lire(doc.RootElement, "liveview_token")}
                Dim n As Integer
                If Integer.TryParse(Lire(doc.RootElement, "polling_interval"), n) AndAlso n > 0 Then s.IntervalleSecondes = n
                If s.Serveur = "" Then Throw New InvalidOperationException("Blink n'a pas donné d'adresse de direct.")
                Return s
            End Using
        End Using
    End Function

    ''' <summary>Une interrogation de la commande du direct (Blink la ferme si on ne la surveille pas). Rend Faux si Blink dit qu'elle est finie ou refusee.</summary>
    Friend Async Function SurveillerLeDirectAsync(s As SessionBlink) As Task(Of Boolean)
        Try
            Dim jeton As String = Await JetonValableAsync()
            If jeton = "" Then Return False
            Using c As HttpClient = Client(True)
                Dim r As HttpResponseMessage = Await c.SendAsync(EnteteApi(HttpMethod.Get, AdresseDeLApi() & "/network/" & s.ReseauId & "/command/" & s.CommandeId, jeton))
                Return r.IsSuccessStatusCode
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function


    ' ------------------------------------------------------------------------------------------
    ' Enregistrements (clips) du stockage cloud de Blink (26/10/2026) : « media/changed » donne les clips recents de toutes les cameras (d'apres blinkpy).
    ' ------------------------------------------------------------------------------------------

    Friend Class ClipBlink
        Friend Id As String = ""
        Friend Camera As String = ""
        Friend CameraId As String = ""
        Friend ReseauId As String = ""
        Friend CreeLe As String = ""
        ''' <summary>Chemin du MP4 sur le serveur de l'API (a telecharger avec le jeton).</summary>
        Friend Media As String = ""
        Friend Miniature As String = ""
    End Class

    ''' <summary>Les clips des <paramref name="jours"/> derniers jours, du plus recent au plus ancien (lance une exception lisible si Blink refuse).</summary>
    Friend Async Function ListerLesClipsAsync(jours As Integer) As Task(Of List(Of ClipBlink))
        Return Await ListerLesClipsDepuisAsync(Date.UtcNow.AddDays(-jours), 3, True)
    End Function

    ''' <summary>Les clips crees depuis <paramref name="depuisUtc"/>, du plus recent au plus ancien. <paramref name="pagesMax"/> : pages de 200 lus ; <paramref name="journaliser"/> : trace de chaque reponse (a eviter pour une surveillance reguliere).</summary>
    Friend Async Function ListerLesClipsDepuisAsync(depuisUtc As Date, pagesMax As Integer, journaliser As Boolean) As Task(Of List(Of ClipBlink))
        Dim jeton As String = Await JetonValableAsync()
        If jeton = "" Then Throw New InvalidOperationException("Connexion expirée : reconnectez-vous avec votre mot de passe.")
        Dim liste As New List(Of ClipBlink)
        Dim depuis As String = depuisUtc.ToString("yyyy-MM-dd'T'HH:mm:ss", Globalization.CultureInfo.InvariantCulture) & "+0000"
        Using c As HttpClient = Client(True)
            For page As Integer = 1 To pagesMax
                Dim adresse As String = AdresseDeLApi() & "/api/v1/accounts/" & _compte & "/media/changed?since=" & Uri.EscapeDataString(depuis) & "&page=" & page
                Dim r As HttpResponseMessage = Await c.SendAsync(EnteteApi(HttpMethod.Get, adresse, jeton))
                Dim corps As String = Await r.Content.ReadAsStringAsync()
                If journaliser Then DiagLog("Blink : clips (page " & page & ") : HTTP " & CInt(r.StatusCode) & " : " & If(corps.Length > 200, corps.Substring(0, 200), corps).Replace(vbCr, " ").Replace(vbLf, " "))
                If Not r.IsSuccessStatusCode Then
                    Dim raison As Match = Regex.Match(corps, """message""\s*:\s*""([^""]*)""")
                    Throw New InvalidOperationException("Blink a refusé la liste des clips (HTTP " & CInt(r.StatusCode) & If(raison.Success, " : " & raison.Groups(1).Value, "") & ").")
                End If
                Dim trouves As Integer = 0
                Using doc As JsonDocument = JsonDocument.Parse(corps)
                    Dim media As JsonElement
                    If Not doc.RootElement.TryGetProperty("media", media) OrElse media.ValueKind <> JsonValueKind.Array Then Exit For
                    For Each m As JsonElement In media.EnumerateArray()
                        trouves += 1
                        If Lire(m, "deleted") = "True" Then Continue For
                        liste.Add(New ClipBlink With {.Id = Lire(m, "id"), .Camera = Lire(m, "device_name"), .CameraId = Lire(m, "device_id"), .ReseauId = Lire(m, "network_id"),
                                                      .CreeLe = Lire(m, "created_at"), .Media = Lire(m, "media"), .Miniature = Lire(m, "thumbnail")})
                    Next
                End Using
                If trouves = 0 Then Exit For
            Next
        End Using
        liste.Sort(Function(a, b) String.CompareOrdinal(b.CreeLe, a.CreeLe))
        Return liste
    End Function

    ''' <summary>Telecharge le MP4 du clip dans <paramref name="fichier"/> (jeton de l'API dans l'en-tete). Rend Vrai si le fichier est complet ; le journal dit la reponse de Blink sinon.</summary>
    Friend Async Function TelechargerLeClipAsync(clip As ClipBlink, fichier As String) As Task(Of Boolean)
        Dim jeton As String = Await JetonValableAsync()
        If jeton = "" OrElse clip.Media = "" Then Return False
        Dim adresse As String = If(clip.Media.StartsWith("http", StringComparison.OrdinalIgnoreCase), clip.Media, AdresseDeLApi() & clip.Media)
        Try
            Using c As HttpClient = Client(True)
                Using r As HttpResponseMessage = Await c.SendAsync(EnteteApi(HttpMethod.Get, adresse, jeton), HttpCompletionOption.ResponseHeadersRead)
                    If Not r.IsSuccessStatusCode Then
                        DiagLog("Blink : telechargement du clip " & clip.Id & " : HTTP " & CInt(r.StatusCode))
                        Return False
                    End If
                    Using flux As IO.Stream = Await r.Content.ReadAsStreamAsync()
                        Using sortie As New IO.FileStream(fichier, IO.FileMode.Create, IO.FileAccess.Write)
                            Await flux.CopyToAsync(sortie)
                        End Using
                    End Using
                End Using
            End Using
            Dim taille As Long = New IO.FileInfo(fichier).Length
            DiagLog("Blink : clip " & clip.Id & " (" & clip.Camera & ") telecharge : " & taille \ 1024 & " Ko")
            Return taille > 1024
        Catch ex As Exception
            DiagLog("Blink : telechargement du clip " & clip.Id & " impossible : " & ex.Message)
            Return False
        End Try
    End Function

End Module
