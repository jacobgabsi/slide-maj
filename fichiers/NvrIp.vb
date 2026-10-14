Option Strict On
Option Explicit On

Imports System.Text.Json

''' <summary>
''' Enregistreur de cameras IP (NVR) : type « Caméra IP » de la minuterie (Jacob, 29/10/2026 : Herospeed, 64 canaux, RTSP sur TCP). Une ligne garde seulement « nvr://canal/flux » dans !/Url ;
''' l'adresse RTSP complete (avec le compte) n'est fabriquee qu'au moment de lire, pour que le mot de passe ne passe ni par la minuterie, ni par les journaux, ni par l'export des minuteries.
''' Le compte est dans data\settings\nvr.dat, chiffre pour l'utilisateur Windows de ce PC (comme blink.dat) : propre au poste, jamais publie.
''' </summary>
Friend Module NvrIp

    Friend Const NombreDeCanaux As Integer = 64

    ''' <summary>Dernier choix de la case Action d'une ligne « Caméra IP » : ouvre le compte de l'enregistreur (ce n'est pas un canal).</summary>
    Friend Const TitreReglageDuCompte As String = "Régler le compte de l'enregistreur..."

    Private _charge As Boolean
    Private _hote As String = ""
    Private _port As Integer = 554
    Private _identifiant As String = ""
    Private _motDePasse As String = ""
    Private _hoteDistant As String = ""

    Private Function Fichier() As String
        Return DossierReglages & "\nvr.dat"
    End Function

    Private Sub Charger()
        If _charge Then Exit Sub
        _charge = True
        Try
            If Not IO.File.Exists(Fichier()) Then Exit Sub
            Dim json As String = MusiqueDeezer.Deproteger(IO.File.ReadAllText(Fichier()).Trim())
            If json = "" Then Exit Sub
            Using doc As JsonDocument = JsonDocument.Parse(json)
                Dim r As JsonElement = doc.RootElement
                _hote = Lire(r, "hote") : _identifiant = Lire(r, "identifiant") : _motDePasse = Lire(r, "motdepasse") : _hoteDistant = Lire(r, "hotedistant")
                Dim p As Integer
                If Integer.TryParse(Lire(r, "port"), p) AndAlso p > 0 AndAlso p < 65536 Then _port = p
            End Using
        Catch ex As Exception
            DiagLog("Camera IP : fichier de connexion illisible : " & ex.Message)
        End Try
    End Sub

    Private Function Lire(r As JsonElement, nom As String) As String
        Dim v As JsonElement
        If r.TryGetProperty(nom, v) AndAlso v.ValueKind = JsonValueKind.String Then Return If(v.GetString(), "")
        Return ""
    End Function

    Friend ReadOnly Property EstConfigure As Boolean
        Get
            Charger()
            Return _hote <> "" AndAlso _identifiant <> ""
        End Get
    End Property

    Friend ReadOnly Property Hote As String
        Get
            Charger()
            Return _hote
        End Get
    End Property

    Friend ReadOnly Property Port As Integer
        Get
            Charger()
            Return _port
        End Get
    End Property

    Friend ReadOnly Property Identifiant As String
        Get
            Charger()
            Return _identifiant
        End Get
    End Property

    Friend ReadOnly Property MotDePasse As String
        Get
            Charger()
            Return _motDePasse
        End Get
    End Property

    ''' <summary>L'adresse a utiliser quand l'enregistreur n'est pas joignable sur le reseau local (Tailscale, 29/10/2026) ; vide = aucune.</summary>
    Friend ReadOnly Property HoteDistant As String
        Get
            Charger()
            Return _hoteDistant
        End Get
    End Property

    Friend Sub Enregistrer(hote As String, port As Integer, identifiant As String, motDePasse As String, Optional hoteDistant As String = Nothing)
        Charger()
        _hote = hote.Trim() : _port = port : _identifiant = identifiant.Trim() : _motDePasse = motDePasse
        If hoteDistant IsNot Nothing Then _hoteDistant = hoteDistant.Trim()          ' (Nothing : l'adresse a distance deja enregistree est gardee)
        _charge = True
        _dernierTestLe = 0
        Try
            Dim json As String = JsonSerializer.Serialize(New Dictionary(Of String, String) From {
                {"hote", _hote}, {"port", CStr(_port)}, {"identifiant", _identifiant}, {"motdepasse", _motDePasse}, {"hotedistant", _hoteDistant}})
            IO.File.WriteAllText(Fichier(), MusiqueDeezer.Proteger(json))
        Catch ex As Exception
            DiagLog("Camera IP : connexion non enregistree : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La connexion en clair pour l'export des parametres ("" s'il n'y en a pas), comme Blink.</summary>
    Friend Function ExporterLaConnexion() As String
        Try
            If Not IO.File.Exists(Fichier()) Then Return ""
            Return MusiqueDeezer.Deproteger(IO.File.ReadAllText(Fichier()).Trim())
        Catch ex As Exception
            DiagLog("Camera IP : connexion non exportee : " & ex.Message)
            Return ""
        End Try
    End Function

    Friend Sub ImporterLaConnexion(json As String)
        If json Is Nothing OrElse json.Trim() = "" Then Exit Sub
        IO.File.WriteAllText(Fichier(), MusiqueDeezer.Proteger(json))
        _charge = False
        Charger()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Adresses : « nvr://canal/flux » (flux 1 = principal, 2 = leger, a preferer pour un PIP).
    ' ------------------------------------------------------------------------------------------

    Friend Function AdresseDuCanal(canal As Integer, flux As Integer) As String
        Return "nvr://" & canal & "/" & flux
    End Function

    ''' <summary>Le titre de la case Action d'une ligne : « Caméra IP 5 » (flux principal) ou « Caméra IP 5 (léger) ».</summary>
    Friend Function TitreDuCanal(canal As Integer, flux As Integer) As String
        Return "Caméra IP " & canal & If(flux = 2, " (léger)", "")
    End Function

    ''' <summary>Les titres de tous les canaux, puis des ecrans de plusieurs cameras, pour la liste de la case Action.</summary>
    Friend Function TousLesTitres() As List(Of String)
        Dim l As New List(Of String)
        For c As Integer = 1 To NombreDeCanaux
            l.Add(TitreDuCanal(c, 1))
            l.Add(TitreDuCanal(c, 2))
        Next
        For Each nb As Integer In EcransPossibles
            If nb = 1 Then Continue For
            For debut As Integer = 1 To NombreDeCanaux Step nb
                l.Add(TitreMosaique(nb, debut))
            Next
        Next
        Return l
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Ecran de plusieurs cameras (29/10/2026, Jacob : « Ecran 4, 8, 16, 32, 64 cameras ») : « nvr://mosaic/nombre/premiere ». Chaque case joue le flux leger de sa camera (FormMosaiqueCameras).
    ' ------------------------------------------------------------------------------------------

    Friend ReadOnly EcransPossibles As Integer() = {1, 4, 8, 16, 32, 64}

    ''' <summary>Colonnes et lignes de la grille pour <paramref name="nombre"/> cameras (4 : 2x2, 8 : 4x2, 16 : 4x4, 32 : 8x4, 64 : 8x8).</summary>
    Friend Sub GrilleDe(nombre As Integer, ByRef colonnes As Integer, ByRef lignes As Integer)
        Select Case nombre
            Case 4 : colonnes = 2 : lignes = 2
            Case 8 : colonnes = 4 : lignes = 2
            Case 16 : colonnes = 4 : lignes = 4
            Case 32 : colonnes = 8 : lignes = 4
            Case 64 : colonnes = 8 : lignes = 8
            Case Else : colonnes = 1 : lignes = 1
        End Select
    End Sub

    Friend Function AdresseMosaique(nombre As Integer, premiere As Integer) As String
        Return "nvr://mosaic/" & nombre & "/" & premiere
    End Function

    Friend Function TitreMosaique(nombre As Integer, premiere As Integer) As String
        Return "Écran " & nombre & " caméras : " & premiere & " à " & (premiere + nombre - 1)
    End Function

    Friend Function LireLaMosaique(adresse As String, ByRef nombre As Integer, ByRef premiere As Integer) As Boolean
        Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(If(adresse, ""), "^nvr://mosaic/(\d{1,2})/(\d{1,2})$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        If Not m.Success Then Return False
        nombre = CInt(m.Groups(1).Value) : premiere = CInt(m.Groups(2).Value)
        Return Array.IndexOf(EcransPossibles, nombre) > 0 AndAlso premiere >= 1 AndAlso premiere + nombre - 1 <= NombreDeCanaux
    End Function

    Friend Function LireLeTitreMosaique(titre As String, ByRef nombre As Integer, ByRef premiere As Integer) As Boolean
        Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(If(titre, ""), "^Écran (\d{1,2}) caméras : (\d{1,2}) à \d{1,2}$")
        If Not m.Success Then Return False
        nombre = CInt(m.Groups(1).Value) : premiere = CInt(m.Groups(2).Value)
        Return Array.IndexOf(EcransPossibles, nombre) > 0 AndAlso premiere >= 1 AndAlso premiere + nombre - 1 <= NombreDeCanaux
    End Function

    ' Ecran de cameras CHOISIES une a une (29/10/2026, Jacob : « la possibilite de choisir les cameras dans la minuterie ») : « nvr://mosaic/sel/3,5,9,12 ».

    ''' <summary>Dernier choix de la liste de la case Action d'une ligne « Caméra IP » avant le reglage du compte : ouvre la liste a cocher des cameras de l'ecran.</summary>
    Friend Const TitreChoixDesCameras As String = "Choisir les caméras de l'écran..."

    Friend Function AdresseSelection(canaux As IEnumerable(Of Integer)) As String
        Return "nvr://mosaic/sel/" & String.Join(",", canaux)
    End Function

    Friend Function LireLaSelection(adresse As String, ByRef canaux As List(Of Integer)) As Boolean
        Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(If(adresse, ""), "^nvr://mosaic/sel/(\d{1,2}(?:,\d{1,2})*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        If Not m.Success Then Return False
        Dim l As New List(Of Integer)
        For Each s As String In m.Groups(1).Value.Split(","c)
            Dim n As Integer = CInt(s)
            If n >= 1 AndAlso n <= NombreDeCanaux AndAlso Not l.Contains(n) Then l.Add(n)
        Next
        If l.Count < 2 Then Return False
        canaux = l
        Return True
    End Function

    ''' <summary>Titre d'une selection : « Écran sélection : 3, 5, 9 (3 caméras) » (les huit premieres, puis « ... »).</summary>
    Friend Function TitreSelection(canaux As IList(Of Integer)) As String
        Dim montre As String = String.Join(", ", canaux.Take(8))
        If canaux.Count > 8 Then montre &= ", ..."
        Return "Écran sélection : " & montre & " (" & canaux.Count & " caméras)"
    End Function

    ''' <summary>Les cameras d'une adresse d'ecran : suite de cameras (« nvr://mosaic/N/premiere ») ou selection (« nvr://mosaic/sel/... ») ; Nothing si ce n'est pas un ecran de plusieurs cameras.</summary>
    Friend Function CanauxDeLEcran(adresse As String) As List(Of Integer)
        Dim nb, premiere As Integer
        If LireLaMosaique(adresse, nb, premiere) Then Return Enumerable.Range(premiere, nb).ToList()
        Dim l As List(Of Integer) = Nothing
        If LireLaSelection(adresse, l) Then Return l
        Return Nothing
    End Function

    ''' <summary>Vrai pour toute adresse « nvr://... » (une camera ou un ecran de cameras).</summary>
    Friend Function EstUneAdresseNvr(adresse As String) As Boolean
        Return If(adresse, "").StartsWith("nvr://", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>L'adresse RTSP (avec le compte) du flux leger d'une camera, pour une case de l'ecran.</summary>
    Friend Function AdresseDeLaCase(canal As Integer) As String
        Return Resoudre(AdresseDuCanal(canal, 2))
    End Function

    ''' <summary>Canal et flux d'un titre « Caméra IP 5 (léger) » ; faux si ce n'en est pas un.</summary>
    Friend Function LireLeTitre(titre As String, ByRef canal As Integer, ByRef flux As Integer) As Boolean
        Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(If(titre, ""), "^Caméra IP (\d{1,2})( \(léger\))?$")
        If Not m.Success Then Return False
        canal = CInt(m.Groups(1).Value) : flux = If(m.Groups(2).Success, 2, 1)
        Return canal >= 1 AndAlso canal <= NombreDeCanaux
    End Function

    Friend Function LireLAdresse(adresse As String, ByRef canal As Integer, ByRef flux As Integer) As Boolean
        Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(If(adresse, ""), "^nvr://(\d{1,2})/([12])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        If Not m.Success Then Return False
        canal = CInt(m.Groups(1).Value) : flux = CInt(m.Groups(2).Value)
        Return canal >= 1 AndAlso canal <= NombreDeCanaux
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Adresse locale ou a distance (29/10/2026, Jacob : « une version pour moi avec Tailscale ») : l'adresse locale de l'enregistreur est essayee d'abord (0,4 s) ; si elle ne repond pas et qu'une adresse
    ' a distance est reglee (celle du PC du bureau dans Tailscale), c'est elle qui sert. Le resultat est garde 30 s. A distance, le flux leger est toujours utilise (debit).
    ' ------------------------------------------------------------------------------------------

    Private _distantActif As Boolean
    Private _dernierTestLe As Long

    Private Function Joignable(hote As String, port As Integer, delaiMs As Integer) As Boolean
        Try
            Using c As New Net.Sockets.TcpClient()
                Return c.ConnectAsync(hote, port).Wait(delaiMs) AndAlso c.Connected
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>L'adresse de l'enregistreur a utiliser maintenant (locale, ou a distance si la locale ne repond pas).</summary>
    Friend Function HoteActif() As String
        Charger()
        If _hoteDistant = "" Then
            _distantActif = False
            Return _hote
        End If
        If _dernierTestLe = 0 OrElse Environment.TickCount64 - _dernierTestLe > 30000 Then
            Dim avant As Boolean = _distantActif
            _distantActif = Not Joignable(_hote, _port, 400)
            _dernierTestLe = Math.Max(1, Environment.TickCount64)
            If _distantActif <> avant OrElse _dernierTestLe = 1 Then DiagLog("Camera IP : enregistreur " & If(_distantActif, "injoignable en local : adresse a distance utilisee", "joignable en local"))
        End If
        Return If(_distantActif, _hoteDistant, _hote)
    End Function

    ''' <summary>Vrai quand l'adresse a distance est celle qui sert en ce moment.</summary>
    Friend ReadOnly Property ADistance As Boolean
        Get
            HoteActif()
            Return _distantActif
        End Get
    End Property

    ''' <summary>Si <paramref name="adresse"/> est « nvr://canal/flux » : l'adresse RTSP complete a donner a VLC (avec le compte ; a ne jamais ecrire dans un journal). Sinon l'adresse telle quelle.
    ''' Rend "" si l'enregistreur n'est pas configure.</summary>
    Friend Function Resoudre(adresse As String) As String
        Dim canal, flux As Integer
        If Not LireLAdresse(adresse, canal, flux) Then Return adresse
        If Not EstConfigure Then
            DiagLog("Camera IP : enregistreur non configure (Options, onglet Camera, cadre « Caméra IP »)")
            Return ""
        End If
        Dim hoteVoulu As String = HoteActif()
        If _distantActif Then flux = 2          ' (a distance : le flux leger, sinon le debit montant du bureau ne suit pas)
        ' Herospeed : le chemin est « /numero » (29/10/2026 : « /Streaming/Channels/... » est ignore, l'enregistreur renvoie toujours la meme camera). Constate avec des captures :
        ' /0 /1 /2 = trois flux de la camera 1 (1280x720, 720x480, 352x288), /10 /11 /12 = ceux de la camera 2, /20 /21 = camera 3... : numero = (canal - 1) * 10 + flux (0 principal, 1 leger).
        Return "rtsp://" & Uri.EscapeDataString(_identifiant) & ":" & Uri.EscapeDataString(_motDePasse) & "@" & hoteVoulu & ":" & _port & "/" & ((canal - 1) * 10 + (flux - 1))
    End Function

    ''' <summary>Essai de connexion (sans rien enregistrer) : demande la description du canal 1 a l'enregistreur avec le compte donne (authentification Digest) et dit ce qui s'est passe.</summary>
    Friend Function Tester(hote As String, port As Integer, identifiant As String, motDePasse As String) As String
        Try
            Using c As New Net.Sockets.TcpClient()
                If Not c.ConnectAsync(hote.Trim(), port).Wait(4000) Then Return "Pas de réponse de " & hote.Trim() & ":" & port & " (adresse ou port incorrect ?)."
                c.ReceiveTimeout = 4000
                Dim flux As Net.Sockets.NetworkStream = c.GetStream()
                Dim uri As String = "rtsp://" & hote.Trim() & ":" & port & "/0"
                Dim r1 As String = DemandeRtsp(flux, "DESCRIBE " & uri & " RTSP/1.0" & vbCrLf & "CSeq: 1" & vbCrLf & "Accept: application/sdp" & vbCrLf & vbCrLf)
                If r1.Contains(" 200 ") Then Return "Connexion réussie (l'enregistreur n'a pas demandé de compte)."
                Dim realm As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(r1, "realm=""([^""]*)""")
                Dim nonce As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(r1, "nonce=""([^""]*)""")
                If Not r1.Contains(" 401 ") OrElse Not realm.Success OrElse Not nonce.Success Then Return "Réponse inattendue : " & r1.Split(vbCr(0))(0).Trim()
                Dim ha1 As String = Md5(identifiant & ":" & realm.Groups(1).Value & ":" & motDePasse)
                Dim ha2 As String = Md5("DESCRIBE:" & uri)
                Dim reponse As String = Md5(ha1 & ":" & nonce.Groups(1).Value & ":" & ha2)
                Dim r2 As String = DemandeRtsp(flux, "DESCRIBE " & uri & " RTSP/1.0" & vbCrLf & "CSeq: 2" & vbCrLf & "Accept: application/sdp" & vbCrLf &
                    "Authorization: Digest username=""" & identifiant & """, realm=""" & realm.Groups(1).Value & """, nonce=""" & nonce.Groups(1).Value & """, uri=""" & uri & """, response=""" & reponse & """" & vbCrLf & vbCrLf)
                If r2.Contains(" 200 ") Then Return "Connexion réussie : le compte « " & identifiant & " » voit la caméra 1."
                If r2.Contains(" 401 ") Then Return "Identifiant ou mot de passe refusé par l'enregistreur."
                If r2.Contains(" 404 ") Then Return "Compte accepté, mais le canal 1 est introuvable (autre modèle d'enregistreur ?)."
                Return "Réponse inattendue : " & r2.Split(vbCr(0))(0).Trim()
            End Using
        Catch ex As Exception
            Return "Essai impossible : " & ex.Message
        End Try
    End Function

    Private Function DemandeRtsp(flux As IO.Stream, requete As String) As String
        Dim b As Byte() = System.Text.Encoding.ASCII.GetBytes(requete)
        flux.Write(b, 0, b.Length)
        Dim tampon(4095) As Byte
        Dim n As Integer = flux.Read(tampon, 0, tampon.Length)
        Return System.Text.Encoding.ASCII.GetString(tampon, 0, Math.Max(0, n))
    End Function

    Private Function Md5(texte As String) As String
        Using h As System.Security.Cryptography.MD5 = System.Security.Cryptography.MD5.Create()
            Return Convert.ToHexString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(texte))).ToLowerInvariant()
        End Using
    End Function

    ''' <summary>Les options VLC d'une adresse RTSP : le flux passe par TCP (en UDP l'enregistreur coupe la lecture : « PLAY failed »).</summary>
    Friend Function OptionsVlc(adresseLue As String) As String()
        If adresseLue IsNot Nothing AndAlso adresseLue.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) Then Return {":rtsp-tcp"}
        Return Array.Empty(Of String)()
    End Function

End Module
