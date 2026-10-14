Option Strict On
Option Explicit On

Imports System.IO
Imports System.Net
Imports System.Net.Security
Imports System.Net.Sockets
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading

''' <summary>
''' Cameras Blink, etape 2 : la vue en direct d'une camera, relayee vers VLC (Jacob, 26/10/2026 : "un PIP avec les cameras Blink, comme un film").
'''
''' Blink n'envoie pas le direct par une adresse que VLC sait lire : la session demandee a Blink donne une adresse « immis:// » (une connexion TLS vers le serveur de Blink, avec une trame
''' d'identification, des trames de maintien toutes les secondes, puis des trames dont certaines portent de la video MPEG-TS). Ce protocole n'est pas public ; il est reconstitue d'apres
''' l'application mobile par des projets communautaires (voir BlinkApi). Ici, Slide ouvre cette connexion lui-meme et republie la video sur une adresse LOCALE du PC
''' (http://127.0.0.1:port/blink.ts) que VLC lit comme n'importe quelle chaine : le PIP, le plein ecran, le son, la duree sont alors ceux d'un film.
''' Le certificat TLS du serveur est verifie normalement par Windows ; s'il est refuse, la cause exacte est ecrite au journal (aucune verification n'est desactivee).
''' Une session Blink dure peu de minutes : tant que le lecteur reste connecte, une nouvelle session est demandee a la fin de la precedente et la video continue.
''' </summary>
Friend Class DirectBlink

    Private ReadOnly _camera As BlinkApi.CameraBlink
    Private _ecouteur As TcpListener
    Private ReadOnly _fin As New CancellationTokenSource()
    Private _octetsRelayes As Long

    ''' <summary>Octets de video deja relayes a VLC par ce relais : 0 tant que la camera n'a rien envoye (ecran noir). Sert a ranger a la fin les cameras sans image d'un ecran de plusieurs cameras.</summary>
    Friend ReadOnly Property OctetsRelayes As Long
        Get
            Return Interlocked.Read(_octetsRelayes)
        End Get
    End Property

    ''' <summary>Les relais en cours (un par camera). Plusieurs cameras peuvent jouer en meme temps si Blink les accepte (essai du 26/10/2026) ; la meme camera n'a qu'un relais.</summary>
    Private Shared ReadOnly _actifs As New List(Of DirectBlink)

    ''' <summary>Le relais ouvert par la fenetre d'apercu des reglages (arrete a sa fermeture).</summary>
    Friend Shared RelaisDeLApercu As DirectBlink

    ''' <summary>Les cameras du compte, telles qu'elles ont ete lues la derniere fois (listes de la minuterie) ; Nothing tant qu'elles n'ont pas ete lues.</summary>
    Friend Shared CamerasEnCache As List(Of BlinkApi.CameraBlink)

    ''' <summary>Vrai quand ce relais a ete arrete (par sa fin, ou parce qu'un autre direct a pris la place).</summary>
    Friend ReadOnly Property Arrete As Boolean
        Get
            Return _fin.IsCancellationRequested
        End Get
    End Property

    ''' <summary>Empreinte SHA-256 du certificat auto-signe du serveur de direct de Blink (valide jusqu'en 2039).</summary>
    Private Const EmpreinteBlink As String = "B1BFA71BA445F0DE14172A1382DB19E8848F93B850EB78BD13FDABC1BE7E2481"

    ''' <summary>Adresse locale a donner a VLC.</summary>
    Friend Adresse As String = ""

    Private Sub New(camera As BlinkApi.CameraBlink)
        _camera = camera
    End Sub

    ''' <summary>Demarre le relais pour cette camera ; l'eventuel relais precedent de la MEME camera est arrete (les autres cameras continuent). Rend le relais, dont Adresse est prete.</summary>
    Friend Shared Function Demarrer(camera As BlinkApi.CameraBlink) As DirectBlink
        Dim memeCamera As List(Of DirectBlink)
        SyncLock _actifs
            memeCamera = _actifs.Where(Function(x) x._camera.Id = camera.Id AndAlso x._camera.ReseauId = camera.ReseauId).ToList()
        End SyncLock
        For Each ancien As DirectBlink In memeCamera
            ancien.Arreter()
        Next
        Dim d As New DirectBlink(camera)
        d._ecouteur = New TcpListener(IPAddress.Loopback, 0)
        d._ecouteur.Start()
        d.Adresse = "http://127.0.0.1:" & CType(d._ecouteur.LocalEndpoint, IPEndPoint).Port & "/blink.ts"
        SyncLock _actifs
            _actifs.Add(d)
        End SyncLock
        Task.Run(Function() d.ServirAsync())
        DiagLog("Blink : relais du direct de « " & camera.Nom & " » pret sur " & d.Adresse)
        Return d
    End Function

    ''' <summary>L'adresse « blink://type/reseau/id/nom » d'une camera, telle qu'elle est ecrite dans les reglages d'une ligne de la minuterie (Url).</summary>
    Friend Shared Function AdresseDeLaCamera(c As BlinkApi.CameraBlink) As String
        Return "blink://" & c.Type & "/" & c.ReseauId & "/" & c.Id & "/" & Uri.EscapeDataString(c.Nom)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Ecran de plusieurs cameras Blink (29/10/2026, Jacob : « la multiselection de camera a Blink egalement ») : « blink://multi/type/reseau/id/nom;type/reseau/id/nom;... » (nom encode).
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Dernier choix de la liste de la case Action d'une ligne « Caméra Blink » : ouvre la liste a cocher des cameras.</summary>
    Friend Const TitreChoixPlusieurs As String = "Choisir plusieurs caméras Blink..."

    Friend Shared Function AdresseDeLEcran(cameras As IEnumerable(Of BlinkApi.CameraBlink)) As String
        Return "blink://multi/" & String.Join(";", cameras.Select(Function(c) c.Type & "/" & c.ReseauId & "/" & c.Id & "/" & Uri.EscapeDataString(c.Nom)))
    End Function

    ''' <summary>Les cameras d'une adresse « blink://multi/... » ; Nothing si ce n'en est pas une (ou s'il y a moins de deux cameras).</summary>
    Friend Shared Function CamerasDeLEcran(adresse As String) As List(Of BlinkApi.CameraBlink)
        If Not If(adresse, "").StartsWith("blink://multi/", StringComparison.OrdinalIgnoreCase) Then Return Nothing
        Dim l As New List(Of BlinkApi.CameraBlink)
        For Each morceau As String In adresse.Substring("blink://multi/".Length).Split(";"c)
            Dim m As Match = Regex.Match(morceau, "^([^/]*)/([^/]*)/([^/]*)/(.*)$")
            If m.Success Then l.Add(New BlinkApi.CameraBlink With {.Type = m.Groups(1).Value, .ReseauId = m.Groups(2).Value, .Id = m.Groups(3).Value, .Nom = Uri.UnescapeDataString(m.Groups(4).Value)})
        Next
        Return If(l.Count >= 2, l, Nothing)
    End Function

    Friend Shared Function TitreDeLEcran(cameras As IList(Of BlinkApi.CameraBlink)) As String
        Dim noms As String = String.Join(", ", cameras.Take(6).Select(Function(c) c.Nom))
        If cameras.Count > 6 Then noms &= ", ..."
        Return "Écran Blink : " & noms & " (" & cameras.Count & " caméras)"
    End Function

    ''' <summary>Le nom lisible d'une adresse « blink://... » ("" si ce n'en est pas une).</summary>
    Friend Shared Function NomDeLAdresse(adresse As String) As String
        If If(adresse, "").StartsWith("blink://multi/", StringComparison.OrdinalIgnoreCase) Then Return ""          ' (un ecran de plusieurs cameras n'a pas un nom)
        Dim m As Match = Regex.Match(If(adresse, ""), "^blink://[^/]*/[^/]*/[^/]*/(.*)$", RegexOptions.IgnoreCase)
        Return If(m.Success, Uri.UnescapeDataString(m.Groups(1).Value), "")
    End Function

    ''' <summary>Si <paramref name="adresse"/> est une adresse « blink://... » : demarre le relais de cette camera et rend l'adresse locale que VLC doit lire (le relais est rendu dans <paramref name="relais"/>
    ''' pour qu'on l'arrete a la fin). Sinon rend l'adresse telle quelle.</summary>
    Friend Shared Function Resoudre(adresse As String, ByRef relais As DirectBlink) As String
        If If(adresse, "").StartsWith("blink://multi/", StringComparison.OrdinalIgnoreCase) Then Return adresse          ' (un ecran de plusieurs cameras : les relais sont lances par EcranDeCameras)
        Dim m As Match = Regex.Match(If(adresse, ""), "^blink://([^/]*)/([^/]*)/([^/]*)/(.*)$", RegexOptions.IgnoreCase)
        If Not m.Success Then Return adresse
        Dim c As New BlinkApi.CameraBlink With {.Type = m.Groups(1).Value, .ReseauId = m.Groups(2).Value, .Id = m.Groups(3).Value, .Nom = Uri.UnescapeDataString(m.Groups(4).Value)}
        relais = Demarrer(c)
        ' Aucune attente ici (26/10/2026 : avec 4 cameras, les demandes de session faites sur le fil de l'interface bloquaient OK et Annuler de Form4) : la session est demandee par le relais
        ' lui-meme, quand le lecteur s'y connecte.
        Return relais.Adresse
    End Function

    Private _sessionPrete As BlinkApi.SessionBlink

    ' ------------------------------------------------------------------------------------------
    ' Cameras recentes : adresse « rtsps:// ». VLC 3 ne sait pas la lire, et Blink n'y suit pas le RTSP usuel : apres DESCRIBE (reponse presque vide : « a=range:npt=now- ») il envoie tout de
    ' suite des paquets RTP entrelaces (« $ », canal, longueur). Slide fait le dialogue lui-meme (TLS, OPTIONS, DESCRIBE, maintien), en retire la video MPEG-TS et la republie comme pour « immis ».
    ' ------------------------------------------------------------------------------------------

    Private Async Function LireExactementAsync(flux As Stream, n As Integer) As Task(Of Byte())
        Dim r(n - 1) As Byte
        Dim lu As Integer = 0
        Do While lu < n
            Dim k As Integer = Await flux.ReadAsync(r, lu, n - lu, _fin.Token)
            If k <= 0 Then Return Nothing
            lu += k
        Loop
        Return r
    End Function

    ''' <summary>Lit une reponse RTSP en texte (jusqu'a la ligne vide), puis son contenu s'il y en a un. <paramref name="premier"/> : un octet deja lu (-1 : aucun).</summary>
    Private Async Function LireUneReponseRtspAsync(flux As Stream, premier As Integer) As Task(Of String)
        Dim texte As New StringBuilder()
        If premier >= 0 Then texte.Append(ChrW(premier))
        Do While texte.Length < 16384 AndAlso Not texte.ToString().EndsWith(vbCrLf & vbCrLf)
            Dim b As Byte() = Await LireExactementAsync(flux, 1)
            If b Is Nothing Then Return Nothing
            texte.Append(ChrW(b(0)))
        Loop
        Dim m As Match = Regex.Match(texte.ToString(), "Content-Length:\s*(\d+)", RegexOptions.IgnoreCase)
        If m.Success AndAlso CInt(m.Groups(1).Value) > 0 Then
            Dim corps As Byte() = Await LireExactementAsync(flux, Math.Min(CInt(m.Groups(1).Value), 65536))
            If corps Is Nothing Then Return Nothing
            texte.Append(Encoding.Latin1.GetString(corps))
        End If
        Return texte.ToString()
    End Function

    ''' <summary>Attend la reponse RTSP a une requete, en laissant de cote les paquets entrelaces (« $ ... ») qui peuvent arriver avant elle.</summary>
    Private Async Function LireUneReponseEnIgnorantLesPaquetsAsync(flux As Stream) As Task(Of String)
        For essai As Integer = 1 To 200
            Dim b As Byte() = Await LireExactementAsync(flux, 1)
            If b Is Nothing Then Return Nothing
            If b(0) = &H24 Then
                Dim h As Byte() = Await LireExactementAsync(flux, 3)
                If h Is Nothing Then Return Nothing
                Dim longueur As Integer = (CInt(h(1)) << 8) Or CInt(h(2))
                If longueur > 0 AndAlso (Await LireExactementAsync(flux, longueur)) Is Nothing Then Return Nothing
            ElseIf b(0) = &H52 Then
                Return Await LireUneReponseRtspAsync(flux, b(0))
            End If
        Next
        Return Nothing
    End Function

    ''' <summary>Premiere ligne et en-tetes d'une reponse RTSP, sans identifiants longs, pour le journal.</summary>
    Private Shared Function ResumeDeLaReponse(reponse As String) As String
        If reponse Is Nothing Then Return "(connexion fermee)"
        Return Regex.Replace(reponse.Substring(0, Math.Min(300, reponse.Length)), "[0-9A-Za-z_\-]{24,}", "***").Replace(vbCr, "").Replace(vbLf, " | ")
    End Function

    Private Shared Async Function EnvoyerRtspAsync(flux As Stream, methode As String, adresse As String, numero As Integer, extra As String, jeton As CancellationToken) As Task
        Dim demande As String = methode & " " & adresse & " RTSP/1.0" & vbCrLf & "CSeq: " & numero & vbCrLf & "User-Agent: LibVLC/3.0.24 (LIVE555 Streaming Media v2026.06.24)" & vbCrLf & extra & vbCrLf
        Dim b As Byte() = Encoding.ASCII.GetBytes(demande)
        Await flux.WriteAsync(b, 0, b.Length, jeton)
        Await flux.FlushAsync(jeton)
    End Function

    ''' <summary>Meme retour que RelayerUneSessionAsync : Vrai si c'est le LECTEUR qui est parti.</summary>
    Private Async Function RelayerUneSessionRtspsAsync(session As BlinkApi.SessionBlink, sortie As Stream) As Task(Of Boolean)
        Dim m As Match = Regex.Match(session.Serveur, "^rtsps?://([^/:]+)(?::(\d+))?(/.*)?$", RegexOptions.IgnoreCase)
        If Not m.Success Then
            DiagLog("Blink : adresse de direct inattendue : " & Regex.Replace(session.Serveur, "[0-9A-Fa-f]{12,}", "***"))
            Return False
        End If
        Dim hote As String = m.Groups(1).Value
        Dim port As Integer = If(m.Groups(2).Success, CInt(m.Groups(2).Value), 443)
        DiagLog("Blink : RTSPS, connexion a " & hote)
        DemarrerLaSurveillance(session)
        Using tcp As New TcpClient()
            Await tcp.ConnectAsync(hote, port, _fin.Token)
            DiagLog("Blink : RTSPS, connecte (TCP)")
            Dim validation As RemoteCertificateValidationCallback =
                Function(expediteur, certificat, chaine, erreurs)
                    If erreurs = SslPolicyErrors.None Then Return True
                    ' Meme certificat auto-signe de Blink que pour « immis » : seule son empreinte epinglee est acceptee (accord de Jacob, 26/10/2026).
                    If certificat IsNot Nothing Then
                        Dim empreinte As String = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(certificat.GetRawCertData()))
                        If empreinte = EmpreinteBlink Then Return True
                        DiagLog("Blink : certificat du serveur RTSPS refuse (" & hote & ") : " & erreurs.ToString() & " ; empreinte " & empreinte & " ; sujet " & certificat.Subject)
                    End If
                    Return False
                End Function
            Using tls As New SslStream(tcp.GetStream(), False, validation)
                Await tls.AuthenticateAsClientAsync(hote)
                DiagLog("Blink : RTSPS, TLS etabli")
                Dim numero As Integer = 1
                Await EnvoyerRtspAsync(tls, "OPTIONS", session.Serveur, numero, "", _fin.Token)
                Dim reponse As String = Await LireUneReponseRtspAsync(tls, -1)
                If reponse Is Nothing OrElse Not reponse.StartsWith("RTSP/1.0 200") Then
                    DiagLog("Blink : RTSPS, OPTIONS refuse : " & If(reponse, "(connexion fermee)").Split(ChrW(13))(0))
                    Return False
                End If
                DiagLog("Blink : RTSPS, OPTIONS accepte")
                numero += 1
                Await EnvoyerRtspAsync(tls, "DESCRIBE", session.Serveur, numero, "Accept: application/sdp" & vbCrLf, _fin.Token)
                reponse = Await LireUneReponseRtspAsync(tls, -1)
                If reponse Is Nothing OrElse Not reponse.StartsWith("RTSP/1.0 200") Then
                    DiagLog("Blink : RTSPS, DESCRIBE refuse : " & If(reponse, "(connexion fermee)").Split(ChrW(13))(0))
                    Return False
                End If

                DiagLog("Blink : RTSPS, DESCRIBE accepte")
                ' La reponse a DESCRIBE ne decrit aucun flux (« a=range:npt=now- » seulement) et rien n'arrive ensuite : on demande la video comme un lecteur RTSP, SETUP puis PLAY, en entrelace sur la meme connexion.
                numero += 1
                Await EnvoyerRtspAsync(tls, "SETUP", session.Serveur, numero, "Transport: RTP/AVP/TCP;unicast;interleaved=0-1" & vbCrLf, _fin.Token)
                reponse = Await LireUneReponseEnIgnorantLesPaquetsAsync(tls)
                DiagLog("Blink : RTSPS, SETUP : " & ResumeDeLaReponse(reponse))
                Dim idSession As String = ""
                If reponse IsNot Nothing Then
                    Dim ms As Match = Regex.Match(reponse, "Session:\s*([^;\r\n]+)", RegexOptions.IgnoreCase)
                    If ms.Success Then idSession = ms.Groups(1).Value.Trim()
                End If
                numero += 1
                Await EnvoyerRtspAsync(tls, "PLAY", session.Serveur, numero, If(idSession <> "", "Session: " & idSession & vbCrLf, "") & "Range: npt=0.000-" & vbCrLf, _fin.Token)
                reponse = Await LireUneReponseEnIgnorantLesPaquetsAsync(tls)
                DiagLog("Blink : RTSPS, PLAY : " & ResumeDeLaReponse(reponse))
                ' Maintien du dialogue RTSP (une requete OPTIONS de temps en temps ; ses reponses sont lues par la boucle ci-dessous).
                Using arret As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_fin.Token)
                    Dim maintien As Task = Task.Run(Async Function()
                                                        Try
                                                            While Not arret.IsCancellationRequested
                                                                Await Task.Delay(20000, arret.Token)
                                                                numero += 1
                                                                Await EnvoyerRtspAsync(tls, "OPTIONS", session.Serveur, numero, "", arret.Token)
                                                            End While
                                                        Catch ex As OperationCanceledException
                                                        Catch ex As ObjectDisposedException
                                                        Catch ex As Exception
                                                            DiagLog("Blink : maintien RTSPS interrompu : " & ex.GetType().Name & " : " & ex.Message)
                                                        End Try
                                                    End Function)
                    Dim lecteurPart As Boolean = False
                    Try
                        Await LireLesPaquetsRtpAsync(tls, sortie)
                    Catch ex As ErreurLecteur
                        lecteurPart = True
                    Catch ex As OperationCanceledException
                        lecteurPart = _fin.IsCancellationRequested
                    Catch ex As Exception
                        DiagLog("Blink : lecture RTSPS : " & ex.GetType().Name & " : " & ex.Message)
                    End Try
                    arret.Cancel()
                    Try
                        numero += 1
                        Await EnvoyerRtspAsync(tls, "TEARDOWN", session.Serveur, numero, "", CancellationToken.None)          ' (arret propre de la session)
                    Catch ex As Exception
                    End Try
                    Return lecteurPart OrElse _fin.IsCancellationRequested
                End Using
            End Using
        End Using
    End Function

    ''' <summary>Boucle de lecture : des paquets entrelaces « $ canal longueur(2) contenu » (RTP ; les canaux impairs sont du RTCP) et, de temps en temps, une reponse RTSP en texte.
    ''' La video est la charge utile MPEG-TS des paquets RTP (type 33, ou tout contenu qui commence par 0x47). Les premiers paquets sont decrits au journal.</summary>
    Private Async Function LireLesPaquetsRtpAsync(tls As Stream, sortie As Stream) As Task
        Dim decrits As Integer = 0
        Dim sansVideo As Integer = 0
        Dim premiereImage As Boolean = True
        Dim dernierRapport As Date = Date.Now
        Do
            Dim b As Byte() = Await LireExactementAsync(tls, 1)
            If b Is Nothing Then
                DiagLog("Blink : le serveur RTSPS a ferme la connexion (" & _octetsRelayes & " octets de video relayes)")
                Exit Do
            End If
            If b(0) = &H52 Then                                                  ' « R » : une reponse RTSP (a un maintien)
                Await LireUneReponseRtspAsync(tls, b(0))
                Continue Do
            End If
            If b(0) <> &H24 Then Continue Do                                     ' (octet inattendu : ignore)
            Dim h As Byte() = Await LireExactementAsync(tls, 3)
            If h Is Nothing Then Exit Do
            Dim canal As Integer = h(0)
            Dim longueur As Integer = (CInt(h(1)) << 8) Or CInt(h(2))
            Dim p As Byte() = If(longueur > 0, Await LireExactementAsync(tls, longueur), New Byte() {})
            If p Is Nothing Then Exit Do
            If decrits < 6 Then
                decrits += 1
                DiagLog("Blink : paquet RTSPS : canal " & canal & ", " & longueur & " octets : " & BitConverter.ToString(p, 0, Math.Min(p.Length, 24)))
            End If
            If canal Mod 2 = 1 OrElse p.Length < 2 Then Continue Do                  ' RTCP
            Dim debut As Integer = 0
            Dim fin As Integer = p.Length
            If (p(0) >> 6) = 2 AndAlso p.Length > 12 Then                         ' en-tete RTP : 12 octets, + sources, + extension
                debut = 12 + 4 * (p(0) And &HF)
                If (p(0) And &H10) <> 0 AndAlso p.Length >= debut + 4 Then debut += 4 + 4 * ((CInt(p(debut + 2)) << 8) Or CInt(p(debut + 3)))
                If (p(0) And &H20) <> 0 Then fin -= p(p.Length - 1)
            End If
            If debut < fin AndAlso p(debut) = &H47 Then
                Await Ecrire(sortie, p, debut, fin - debut)
                _octetsRelayes += fin - debut
                sansVideo = 0
                If premiereImage Then
                    premiereImage = False
                    DiagLog("Blink : premiere video RTSPS relayee (" & (fin - debut) & " octets)")
                End If
            Else
                sansVideo += 1
                If sansVideo = 20 Then DiagLog("Blink : paquets RTSPS qui ne sont pas du MPEG-TS (type " & (p(1) And &H7F) & ") : rien a relayer")
            End If
            If (Date.Now - dernierRapport).TotalSeconds >= 30 Then
                dernierRapport = Date.Now
                DiagLog("Blink : direct RTSPS en cours : " & _octetsRelayes & " octets de video relayes")
            End If
        Loop
    End Function

    ''' <summary>Demande une session de direct ; « occupe » (409) : la precedente se ferme encore chez Blink, on reessaie (6 fois, 4 s). Tout autre refus est rendu tel quel.</summary>
    Private Shared Async Function DemanderAvecPatienceAsync(camera As BlinkApi.CameraBlink, jeton As CancellationToken) As Task(Of BlinkApi.SessionBlink)
        Dim derniere As Exception = Nothing
        For essai As Integer = 1 To 6
            Try
                Return Await BlinkApi.DemanderLeDirectAsync(camera, "liveview")
            Catch ex As Exception
                derniere = ex
                If Not ex.Message.Contains("busy") Then Throw          ' (« System is busy » : on reessaie ; « Sync Module Offline » ou autre refus : inutile d'insister)
            End Try
            Await Task.Delay(4000, jeton)
        Next
        Throw derniere
    End Function

    ''' <summary>Arrete tous les relais en cours.</summary>
    Friend Shared Sub ArreterLeDirect()
        Dim tous As List(Of DirectBlink)
        SyncLock _actifs
            tous = _actifs.ToList()
        End SyncLock
        For Each d As DirectBlink In tous
            d.Arreter()
        Next
    End Sub

    Friend Sub Arreter()
        Try
            _fin.Cancel()
            _surveillance?.Cancel()
            _ecouteur?.Stop()
        Catch ex As Exception
        End Try
        SyncLock _actifs
            _actifs.Remove(Me)
        End SyncLock
        DiagLog("Blink : relais du direct arrete (" & _octetsRelayes & " octets de video relayes)")
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Cote lecteur (VLC) : une petite reponse HTTP qui ne finit jamais, tant que le direct dure.
    ' ------------------------------------------------------------------------------------------

    Private Async Function ServirAsync() As Task
        Try
            While Not _fin.IsCancellationRequested
                Using lecteur As TcpClient = Await _ecouteur.AcceptTcpClientAsync(_fin.Token)
                    Dim sortie As NetworkStream = lecteur.GetStream()
                    Dim tampon(2047) As Byte
                    Dim recu As New StringBuilder()
                    Do While Not recu.ToString().Contains(vbCrLf & vbCrLf) AndAlso recu.Length < 8192
                        Dim n As Integer = Await sortie.ReadAsync(tampon, 0, tampon.Length, _fin.Token)
                        If n <= 0 Then Exit Do
                        recu.Append(Encoding.ASCII.GetString(tampon, 0, n))
                    Loop
                    ' La session est demandee a Blink a chaque connexion du lecteur : le type de serveur qu'il donne decide de la suite.
                    Dim session As BlinkApi.SessionBlink = Nothing
                    Dim erreurSession As String = ""
                    If _sessionPrete IsNot Nothing Then          ' (deja demandee par Resoudre : ne pas en ouvrir une seconde)
                        session = _sessionPrete
                        _sessionPrete = Nothing
                    Else
                        Try
                            session = Await DemanderAvecPatienceAsync(_camera, _fin.Token)
                        Catch ex As Exception
                            erreurSession = ex.GetType().Name & " : " & ex.Message
                        End Try
                    End If
                    If session Is Nothing Then
                        DiagLog("Blink : session de direct : " & erreurSession)
                        Dim refus As Byte() = Encoding.ASCII.GetBytes("HTTP/1.1 503 Service Unavailable" & vbCrLf & "Connection: close" & vbCrLf & vbCrLf)
                        Await sortie.WriteAsync(refus, 0, refus.Length, _fin.Token)
                        Continue While
                    End If
                    Dim entete As Byte() = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK" & vbCrLf & "Content-Type: video/mp2t" & vbCrLf & "Cache-Control: no-cache" & vbCrLf & "Connection: close" & vbCrLf & vbCrLf)
                    Await sortie.WriteAsync(entete, 0, entete.Length, _fin.Token)
                    Await PomperAsync(sortie, session)
                End Using
            End While
        Catch ex As OperationCanceledException
        Catch ex As ObjectDisposedException
        Catch ex As Exception
            DiagLog("Blink : relais du direct : " & ex.GetType().Name & " : " & ex.Message)
        End Try
    End Function

    Private _surveillance As CancellationTokenSource

    ''' <summary>Direct RTSPS : Blink veut qu'on interroge sa commande tant que le direct dure (sinon il le ferme). La surveillance precedente est arretee.</summary>
    Private Sub DemarrerLaSurveillance(session As BlinkApi.SessionBlink)
        Try
            _surveillance?.Cancel()
        Catch ex As Exception
        End Try
        Dim cts As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_fin.Token)
        _surveillance = cts
        Task.Run(Async Function()
                     Try
                         While Not cts.IsCancellationRequested
                             Dim ok As Boolean = Await BlinkApi.SurveillerLeDirectAsync(session)
                             If Not ok Then DiagLog("Blink : interrogation de la commande de direct refusee")
                             Await Task.Delay(Math.Max(5, session.IntervalleSecondes) * 1000, cts.Token)
                         End While
                     Catch ex As OperationCanceledException
                     Catch ex As ObjectDisposedException
                     End Try
                 End Function)
    End Sub

    ''' <summary>Tant que le lecteur est la : une session Blink apres l'autre, leur video recopiee vers lui.</summary>
    Private Async Function PomperAsync(sortie As Stream, premiere As BlinkApi.SessionBlink) As Task
        Dim echecsDeSuite As Integer = 0
        While Not _fin.IsCancellationRequested
            Dim debut As Date = Date.Now
            Try
                Dim session As BlinkApi.SessionBlink = premiere          ' (la 1re session est celle deja demandee a la connexion du lecteur)
                premiere = Nothing
                If session Is Nothing Then session = Await BlinkApi.DemanderLeDirectAsync(_camera, "liveview")
                ' Cameras recentes : adresse RTSPS ; les autres : « immis ».
                Dim lecteurPart As Boolean = If(session.Serveur.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase),
                                                Await RelayerUneSessionRtspsAsync(session, sortie), Await RelayerUneSessionAsync(session, sortie))
                If lecteurPart Then Exit While
            Catch ex As OperationCanceledException
                Exit While
            Catch ex As Exception
                DiagLog("Blink : session de direct : " & ex.GetType().Name & " : " & ex.Message)
            End Try
            If (Date.Now - debut).TotalSeconds < 20 Then echecsDeSuite += 1 Else echecsDeSuite = 0
            If echecsDeSuite >= 3 Then
                DiagLog("Blink : trois sessions de direct de suite sans video : abandon")
                Exit While
            End If
            Try
                Await Task.Delay(8000, _fin.Token)                                       ' (Blink repond « occupe » (409) un moment apres une session)
            Catch ex As OperationCanceledException
                Exit While
            End Try
        End While
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Cote Blink : la connexion « immis ».
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Rend Vrai si c'est le LECTEUR qui est parti (le relais s'arrete), Faux si la session de Blink est finie (une autre est demandee).</summary>
    Private Async Function RelayerUneSessionAsync(session As BlinkApi.SessionBlink, sortie As Stream) As Task(Of Boolean)
        Dim hote As Match = Regex.Match(session.Serveur, "^immis://([^/:]+)")
        Dim client As Match = Regex.Match(session.Serveur, "client_id=(\d+)")
        Dim chemin As Match = Regex.Match(session.Serveur, "^immis://[^/]+/([^?]+?)__")
        Dim serie As Match = Regex.Match(session.Serveur, "__[^?]*?_([^_?]+)\?")          ' (« __IMDS_G8T1940133450933 » : le numero de serie de l'appareil)
        If Not hote.Success OrElse Not client.Success OrElse Not chemin.Success OrElse Not serie.Success Then
            DiagLog("Blink : adresse de direct inattendue : " & Regex.Replace(session.Serveur, "[0-9A-Fa-f]{12,}", "***"))
            Return False
        End If
        Dim serveur As String = hote.Groups(1).Value
        Dim identifiant As String = chemin.Groups(1).Value
        Dim numeroSerie As String = serie.Groups(1).Value
        Dim numeroClient As UInteger = UInteger.Parse(client.Groups(1).Value)

        Using tcp As New TcpClient()
            Await tcp.ConnectAsync(serveur, 443, _fin.Token)
            ' Verification normale du certificat par Windows ; en cas de refus, la cause exacte est notee pour qu'on decide en connaissance de cause.
            Dim validation As RemoteCertificateValidationCallback =
                Function(expediteur, certificat, chaine, erreurs)
                    If erreurs = SslPolicyErrors.None Then Return True
                    ' Le serveur de direct de Blink presente un certificat auto-signe (*.immedia-semi.com) et on s'y connecte par adresse IP : Windows ne peut pas le valider.
                    ' Seul CE certificat, epingle par son empreinte SHA-256 (accord de Jacob, 26/10/2026), est accepte ; tout autre reste refuse.
                    If certificat IsNot Nothing Then
                        Dim empreinte As String = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(certificat.GetRawCertData()))
                        If empreinte = EmpreinteBlink Then Return True
                        DiagLog("Blink : certificat du serveur de direct refuse (" & serveur & ") : " & erreurs.ToString() & " ; empreinte " & empreinte & " ; sujet " & certificat.Subject)
                    End If
                    Return False
                End Function
            Using tls As New SslStream(tcp.GetStream(), False, validation)
                Await tls.AuthenticateAsClientAsync(serveur)
                Dim entete As Byte() = EnteteDeConnexion(numeroClient, numeroSerie, identifiant)
                Await tls.WriteAsync(entete, 0, entete.Length, _fin.Token)
                DiagLog("Blink : connecte au serveur de direct (" & serveur & ", client " & numeroClient & ", identifiant de " & identifiant.Length & " caracteres, " & entete.Length & " octets d'identification envoyes)")

                Using arret As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_fin.Token)
                    Dim verrou As New SemaphoreSlim(1, 1)
                    Dim maintien As Task = Task.Run(Async Function()
                                                        Dim derniereInterrogation As Date = Date.MinValue      ' (l'application interroge la commande tout de suite, pas apres un delai)
                                                        Dim tour As Integer = 0
                                                        Dim numeroMaintien As UInteger = 0
                                                        Try
                                                            While Not arret.IsCancellationRequested
                                                                If derniereInterrogation = Date.MinValue OrElse (Date.Now - derniereInterrogation).TotalSeconds >= session.IntervalleSecondes Then
                                                                    derniereInterrogation = Date.Now
                                                                    Dim ok As Boolean = Await BlinkApi.SurveillerLeDirectAsync(session)
                                                                    If Not ok Then DiagLog("Blink : interrogation de la commande de direct refusee")
                                                                End If
                                                                Await Task.Delay(1000, arret.Token)
                                                                Await verrou.WaitAsync(arret.Token)
                                                                Try
                                                                    ' Comme blinkpy : un maintien (type 0x0A, sans contenu) au premier tour puis tous les 10 tours, des statistiques de latence (0x12) a chaque seconde.
                                                                    If tour Mod 10 = 0 Then
                                                                        numeroMaintien += 1UI
                                                                        Dim k As Byte() = {&HA, CByte((numeroMaintien >> 24) And &HFFUI), CByte((numeroMaintien >> 16) And &HFFUI), CByte((numeroMaintien >> 8) And &HFFUI), CByte(numeroMaintien And &HFFUI), 0, 0, 0, 0}
                                                                        Await tls.WriteAsync(k, 0, k.Length, arret.Token)
                                                                    End If
                                                                    tour += 1
                                                                    Await tls.WriteAsync(TrameDeMaintien, 0, TrameDeMaintien.Length, arret.Token)
                                                                Finally
                                                                    verrou.Release()
                                                                End Try
                                                            End While
                                                        Catch ex As OperationCanceledException
                                                        Catch ex As ObjectDisposedException
                                                        Catch ex As Exception
                                                            DiagLog("Blink : maintien du direct interrompu : " & ex.GetType().Name & " : " & ex.Message)
                                                        End Try
                                                    End Function)
                    Dim lecteurPart As Boolean = False
                    Try
                        Await LireLesTramesAsync(tls, sortie)
                    Catch ex As ErreurLecteur
                        lecteurPart = True
                    Catch ex As OperationCanceledException
                        lecteurPart = _fin.IsCancellationRequested
                    Catch ex As Exception
                        DiagLog("Blink : lecture du direct : " & ex.GetType().Name & " : " & ex.Message)
                    End Try
                    arret.Cancel()
                    Try
                        Await verrou.WaitAsync(2000)
                        Dim arretPropre As Byte() = {7, 0, 0, 0, 1, 0, 0, 0, 0}           ' (sinon la camera reste « occupee » jusqu'au delai de Blink)
                        Await tls.WriteAsync(arretPropre, 0, arretPropre.Length)
                    Catch ex As Exception
                    End Try
                    Return lecteurPart OrElse _fin.IsCancellationRequested
                End Using
            End Using
        End Using
    End Function

    Private Class ErreurLecteur
        Inherits IOException
    End Class

    ''' <summary>Lit les trames de Blink (type 1 octet, numero 4 octets, longueur 4 octets, puis le contenu) et recopie la video (type 0, du MPEG-TS) vers le lecteur. Si le debut du flux ressemble
    ''' deja a du MPEG-TS (octet 0x47), tout est recopie tel quel. Les premiers octets sont notes au journal : c'est ce qui permet de corriger si Blink a change son format.</summary>
    Private Async Function LireLesTramesAsync(tls As Stream, sortie As Stream) As Task
        Dim lecture(65535) As Byte
        Dim tampon As New MemoryStream()
        Dim brut As Boolean = False
        Dim premier As Boolean = True
        Dim premiereImage As Boolean = True
        Dim dernierRapport As Date = Date.Now
        Do
            Dim n As Integer = Await tls.ReadAsync(lecture, 0, lecture.Length, _fin.Token)
            If n <= 0 Then
                DiagLog("Blink : le serveur de direct a ferme la connexion (" & _octetsRelayes & " octets de video relayes" & If(premier, ", rien recu", "") & ")")
                Exit Do
            End If
            If premier Then
                premier = False
                DiagLog("Blink : premiers octets recus du serveur de direct : " & BitConverter.ToString(lecture, 0, Math.Min(n, 40)))
                brut = (lecture(0) = &H47)
            End If
            If brut Then
                Await Ecrire(sortie, lecture, 0, n)
                _octetsRelayes += n
                Continue Do
            End If
            tampon.Write(lecture, 0, n)
            Dim donnees As Byte() = tampon.ToArray()
            Dim position As Integer = 0
            While donnees.Length - position >= 9
                Dim type As Byte = donnees(position)
                Dim longueur As Integer = (CInt(donnees(position + 5)) << 24) Or (CInt(donnees(position + 6)) << 16) Or (CInt(donnees(position + 7)) << 8) Or CInt(donnees(position + 8))
                If longueur < 0 OrElse longueur > 4000000 Then
                    DiagLog("Blink : trame de direct incomprise (type " & type & ", longueur " & longueur & ") : le flux est recopie tel quel")
                    brut = True
                    Await Ecrire(sortie, donnees, position, donnees.Length - position)
                    _octetsRelayes += donnees.Length - position
                    position = donnees.Length
                    Exit While
                End If
                If donnees.Length - position - 9 < longueur Then Exit While
                If type = 0 AndAlso longueur > 0 Then
                    Await Ecrire(sortie, donnees, position + 9, longueur)
                    _octetsRelayes += longueur
                    If premiereImage Then
                        premiereImage = False
                        DiagLog("Blink : premiere video du direct relayee (" & longueur & " octets)")
                    End If
                End If
                position += 9 + longueur
            End While
            tampon.SetLength(0)
            If position < donnees.Length Then tampon.Write(donnees, position, donnees.Length - position)
            If (Date.Now - dernierRapport).TotalSeconds >= 30 Then
                dernierRapport = Date.Now
                DiagLog("Blink : direct en cours : " & _octetsRelayes & " octets de video relayes")
            End If
        Loop
    End Function

    ''' <summary>Ecrit vers le lecteur ; un echec ici veut dire que le lecteur est parti.</summary>
    Private Async Function Ecrire(sortie As Stream, donnees As Byte(), debut As Integer, longueur As Integer) As Task
        Try
            Await sortie.WriteAsync(donnees, debut, longueur, _fin.Token)
        Catch ex As OperationCanceledException
            Throw
        Catch ex As Exception
            Throw New ErreurLecteur()
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Trames fixes du protocole (relevees dans le script d'essai amattu2/blinkpoc.py et dans ha-blink-plus).
    ' ------------------------------------------------------------------------------------------

    Private Shared ReadOnly TrameDeMaintien As Byte() = BuildMaintien()

    Private Shared Function BuildMaintien() As Byte()
        Dim t(32) As Byte                                                        ' type 0x12, numero 1000, 24 octets a zero (latences et images)
        t(0) = &H12 : t(3) = &H3 : t(4) = &HE8 : t(8) = &H18
        Return t
    End Function

    ''' <summary>Trame d'identification (122 octets, format actuel de blinkpy) : 00 00 00 28 ; longueur 16 + numero de serie ; identifiant de client (4) ; 01 08 ; longueur 64 + 64 zeros (pas de jeton) ;
    ''' longueur 16 + identifiant de connexion ; 00 00 00 01.</summary>
    Private Shared Function EnteteDeConnexion(numeroClient As UInteger, numeroSerie As String, identifiant As String) As Byte()
        Dim e As New List(Of Byte)
        e.AddRange({CByte(0), CByte(0), CByte(0), CByte(&H28)})
        e.AddRange({CByte(0), CByte(0), CByte(0), CByte(&H10)})
        e.AddRange(Sur16(numeroSerie))
        e.AddRange({CByte((numeroClient >> 24) And &HFFUI), CByte((numeroClient >> 16) And &HFFUI), CByte((numeroClient >> 8) And &HFFUI), CByte(numeroClient And &HFFUI)})
        e.AddRange({CByte(1), CByte(8)})
        e.AddRange({CByte(0), CByte(0), CByte(0), CByte(&H40)})
        e.AddRange(New Byte(63) {})
        e.AddRange({CByte(0), CByte(0), CByte(0), CByte(&H10)})
        e.AddRange(Sur16(identifiant))
        e.AddRange({CByte(0), CByte(0), CByte(0), CByte(1)})
        Return e.ToArray()
    End Function

    ''' <summary>Le texte sur exactement 16 octets (coupe, ou complete par des zeros).</summary>
    Private Shared Function Sur16(texte As String) As Byte()
        Dim r(15) As Byte
        Dim b As Byte() = Encoding.ASCII.GetBytes(texte)
        Array.Copy(b, r, Math.Min(16, b.Length))
        Return r
    End Function

End Class
