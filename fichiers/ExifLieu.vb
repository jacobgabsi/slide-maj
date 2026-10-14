Option Strict On
Option Explicit On

Imports System.Data.OleDb
Imports System.Globalization
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Threading
Imports MetadataExtractor
Imports MetadataExtractor.Formats.Exif

''' <summary>
''' Legende affichee en bas a gauche des photos et des videos : "Lieu [mois an]".
'''
''' Remplace ExifWithCity (et Exif, CityByLatLong, ConvertDecimal, HaversineDistance), qui :
'''  - n'affichaient JAMAIS de lieu pour les videos .mov, pourtant geolocalisees par l'iPhone :
'''    la date etait lue en ouvrant le fichier comme une image, ce qui echoue sur une video, et la
'''    fonction abandonnait avant de lire le GPS. (Mesure sur 45 .mov : 41 avec GPS, 0 lieu affiche.)
'''    De plus leur position "+48.8177+002.4439/" etait convertie avec la virgule decimale
'''    francaise, donc fausse ;
'''  - abandonnaient aussi pour les photos sans le champ EXIF "DateTime" (0x132) ;
'''  - decodaient toute la photo pour lire la date (~50 ms), sans la liberer ;
'''  - relisaient les 60 000 villes de la base Access a chaque photo (~600 ms, interface figee).
'''
''' Ici : une seule lecture des en-tetes du fichier, vraie date de prise de vue, GPS des photos
''' (EXIF) et des videos (QuickTime), villes chargees une fois en memoire. Le nom affiche suit la
''' meme regle qu'avant : localite la plus proche de la table Cities.
'''
''' Option "Lieu cherche sur Internet" (Form4, onglet Photos, reglage LieuEnLigne) : le lieu est
''' demande a OpenStreetMap (Nominatim), avec repli automatique sur la table Cities.
''' </summary>
Module ExifLieu

    Private Structure Ville
        Public Lieu As LieuAffiche  ' "Marrakech" + "Maroc", "Levallois-Perret" + "" (France)
        Public Lat As Double
        Public Lon As Double
    End Structure

    ''' <summary>Lieu pret a afficher, ville et pays separes (le pays est vide pour la France).
    ''' La legende en bas a gauche les colle ("Marrakech Maroc") ; la bande noire les ecrit sur
    ''' des lignes distinctes (FormLieuBande).</summary>
    Public Structure LieuAffiche
        Public Localite As String
        Public Pays As String
        Public ReadOnly Property Texte As String
            Get
                If String.IsNullOrEmpty(Localite) Then Return ""
                Return If(String.IsNullOrEmpty(Pays), Localite, Localite & " " & Pays)
            End Get
        End Property
    End Structure

    ' ------------------------------------------------------------------------------------------
    ' Dernier fichier traite par LegendeLieuDate : lu par Module1 pour ecrire le lieu dans la
    ' bande noire de gauche (FormLieuBande).
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Lieu du dernier fichier (Localite vide si aucun).</summary>
    Public DernierLieu As LieuAffiche
    ''' <summary>Lieu deduit d'un fichier voisin (affiche avec "~").</summary>
    Public DernierLieuDeduit As Boolean
    ''' <summary>Rapport largeur/hauteur de l'image telle qu'affichee (rotation comprise) ; 0 si inconnu.</summary>
    Public DernierRatio As Double
    ''' <summary>Meme rapport SANS l'orientation EXIF (photo affichee telle quelle, mode 4:3).</summary>
    Public DernierRatioBrut As Double
    Public DernierEstVideo As Boolean
    ''' <summary>Date du dernier fichier (prise de vue, sinon date du fichier) ; Nothing si aucune.</summary>
    Public DerniereDate As Date?
    ''' <summary>Morceaux de la legende du bas ("~lieu" et "[mmm aa]") : retires de la legende
    ''' quand ils sont deja ecrits en grand dans les bandes noires.</summary>
    Public DerniereLegendeLieu As String = ""
    Public DerniereLegendeDate As String = ""

    ''' <summary>A appeler avant chaque nouvelle photo/video : evite de reprendre le lieu du fichier
    ''' precedent quand LegendeLieuDate n'est pas appelee (date non affichee).</summary>
    Public Sub OublierDernierLieu()
        DernierLieu = New LieuAffiche With {.Localite = "", .Pays = ""}
        DernierLieuDeduit = False
        DernierRatio = 0
        DernierRatioBrut = 0
        DernierEstVideo = False
        DerniereDate = Nothing
        DerniereLegendeLieu = ""
        DerniereLegendeDate = ""
    End Sub

    ' Chargees une seule fois ; PrechargerVilles lance le chargement en arriere-plan au demarrage
    ' pour que la premiere photo n'attende pas.
    Private ReadOnly _villes As New Lazy(Of Ville())(AddressOf ChargerVilles, LazyThreadSafetyMode.ExecutionAndPublication)

    ''' <summary>A appeler une fois DatabasePath connu (init).</summary>
    Public Sub PrechargerVilles()
        Task.Run(Function() _villes.Value)
    End Sub

    ''' <summary>"Lieu [MMM yy]", ou seulement "[MMM yy]" sans position GPS. Sans date de prise de
    ''' vue lisible, la date du fichier est utilisee (comme avant).</summary>
    Public Function LegendeLieuDate(fichier As String) As String
        Dim prise As Date? = Nothing
        Dim position As (Lat As Double, Lon As Double)? = Nothing
        OublierDernierLieu()
        DernierEstVideo = EstUneVideo(fichier)
        Try
            Dim dossiers As IReadOnlyList(Of MetadataExtractor.Directory) = ImageMetadataReader.ReadMetadata(fichier)
            prise = DatePriseDeVue(dossiers)
            position = PositionGps(dossiers)
            Dim r As (Tourne As Double, Brut As Double) = RatioAffiche(dossiers, DernierEstVideo)
            DernierRatio = r.Tourne
            DernierRatioBrut = r.Brut
        Catch
            ' Format inconnu ou fichier illisible : date du fichier, sans lieu.
        End Try

        Dim quand As Date
        If prise.HasValue Then
            quand = prise.Value
        Else
            Try
                quand = Global.System.IO.File.GetLastWriteTime(fichier)
            Catch
                quand = Date.Now
            End Try
        End If

        DerniereDate = quand
        DerniereLegendeDate = Format(quand, "MMMM yy")          ' mois en entier : "septembre 24"

        ' Option "Avec la localite" decochee (Form4, onglet Photos) : la date seule.
        If Not AfficheLieu Then Return DerniereLegendeDate

        ' Video sans position (les .mp4 exportes ou recus perdent leur GPS) : lieu deduit du fichier
        ' geolocalise le plus proche dans la numerotation, signale par "~".
        Dim deduit As Boolean = False
        If Not position.HasValue AndAlso EstUneVideo(fichier) Then
            position = PositionDuVoisin(fichier)
            deduit = position.HasValue
        End If

        If position.HasValue Then
            DernierLieu = PartiesDuLieu(position.Value.Lat, position.Value.Lon)
            DernierLieuDeduit = deduit AndAlso Not String.IsNullOrEmpty(DernierLieu.Localite)
        End If
        Dim lieu As String = DernierLieu.Texte
        If DernierLieuDeduit Then lieu = "~" & lieu
        DerniereLegendeLieu = lieu
        Return If(lieu = "", "", lieu & SeparateurLieuDate) & DerniereLegendeDate
    End Function

    ''' <summary>Entre le lieu et la date dans la legende du bas : "Marrakech Maroc · mars 23".
    ''' (Jusqu'a la version 4.8, la date etait entre crochets : "Marrakech Maroc [mars 23]".)</summary>
    Public Const SeparateurLieuDate As String = " · "

    ''' <summary>Rapport largeur/hauteur de l'image telle qu'elle apparait a l'ecran : photo tournee
    ''' selon son orientation EXIF, video selon la rotation de sa piste (les videos iPhone filmees
    ''' en hauteur sont enregistrees "couchees" avec une rotation de 90 degres). 0 si inconnu.</summary>
    Private Function RatioAffiche(dossiers As IReadOnlyList(Of MetadataExtractor.Directory), video As Boolean) As (Tourne As Double, Brut As Double)
        Dim largeur As Integer = 0, hauteur As Integer = 0
        Dim tourne As Boolean = False

        If video Then
            ' Premiere piste video (largeur non nulle) de l'en-tete QuickTime.
            For Each piste As MetadataExtractor.Directory In dossiers.Where(Function(x) x.Name = "QuickTime Track Header")
                Dim l, h As Integer
                If piste.TryGetInt32(&HA, l) AndAlso piste.TryGetInt32(&HB, h) AndAlso l > 0 AndAlso h > 0 Then
                    largeur = l : hauteur = h
                    Dim rotation As Integer
                    If piste.TryGetInt32(&HD, rotation) Then tourne = (Math.Abs(rotation) Mod 180) = 90
                    Exit For
                End If
            Next
        Else
            ' D'ABORD la taille reellement stockee dans le fichier (l'en-tete JPEG), et seulement
            ' ensuite celle annoncee par l'EXIF : une photo recadree ou pivotee puis re-enregistree
            ' garde souvent l'ancienne taille EXIF. Vu sur IMG_048327.jpg : EXIF 4032x3024 (paysage)
            ' alors que l'image fait 3024x4032 (portrait). On croyait donc la photo en largeur, et
            ' le lieu et la date n'allaient pas dans les bandes noires.
            For Each jpeg As MetadataExtractor.Formats.Jpeg.JpegDirectory In dossiers.OfType(Of MetadataExtractor.Formats.Jpeg.JpegDirectory)()
                If jpeg.TryGetInt32(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageWidth, largeur) AndAlso
                   jpeg.TryGetInt32(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight, hauteur) Then Exit For
            Next
            If largeur <= 0 OrElse hauteur <= 0 Then
                For Each exif As ExifSubIfdDirectory In dossiers.OfType(Of ExifSubIfdDirectory)()
                    If exif.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, largeur) AndAlso
                       exif.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, hauteur) Then Exit For
                Next
            End If
            If largeur <= 0 OrElse hauteur <= 0 Then
                For Each png As MetadataExtractor.Formats.Png.PngDirectory In dossiers.OfType(Of MetadataExtractor.Formats.Png.PngDirectory)()
                    If png.TryGetInt32(MetadataExtractor.Formats.Png.PngDirectory.TagImageWidth, largeur) AndAlso
                       png.TryGetInt32(MetadataExtractor.Formats.Png.PngDirectory.TagImageHeight, hauteur) Then Exit For
                Next
            End If
            ' Orientations EXIF 5, 6 et 8 = tournees de 90 degres par Slide (voir ImageOrientation
            ' dans AffichePhoto169 ; la 7 n'y est pas traitee).
            For Each ifd0 As ExifIfd0Directory In dossiers.OfType(Of ExifIfd0Directory)()
                Dim orientation As Integer
                If ifd0.TryGetInt32(ExifDirectoryBase.TagOrientation, orientation) Then tourne = orientation = 5 OrElse orientation = 6 OrElse orientation = 8
            Next
        End If

        If largeur <= 0 OrElse hauteur <= 0 Then Return (0, 0)
        Dim brut As Double = largeur / hauteur
        ' Une video est toujours tournee par VLC ; une photo ne l'est qu'en mode 16:9.
        Return (If(tourne, 1 / brut, brut), If(video AndAlso tourne, 1 / brut, brut))
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Lieu deduit des voisins (videos sans GPS)
    '
    ' L'iPhone numerote tout dans l'ordre de prise de vue (IMG_043819...) : une video sans position
    ' a ete prise, tres probablement, au meme endroit que la photo ou la video juste avant ou juste
    ' apres. On cherche le fichier geolocalise le plus proche jusqu'a EcartVoisins numeros, dans le
    ' meme dossier. Mesure sur les 210 .mp4 de 2023 : 66 retrouvent un lieu a +/- 3, dont 62 dont
    ' les voisins avant/apres concordent (< 5 km).
    ' ------------------------------------------------------------------------------------------

    Private Const EcartVoisins As Integer = 3

    ' Contenu des dossiers deja parcourus : nom sans extension (minuscules) -> chemins complets.
    Private ReadOnly _dossiersLus As New Dictionary(Of String, Dictionary(Of String, List(Of String)))(StringComparer.OrdinalIgnoreCase)
    ' Position deja lue pour un fichier voisin (Nothing = pas de GPS).
    Private ReadOnly _positionsLues As New Dictionary(Of String, (Lat As Double, Lon As Double)?)(StringComparer.OrdinalIgnoreCase)

    Private Function EstUneVideo(fichier As String) As Boolean
        Select Case Global.System.IO.Path.GetExtension(fichier).ToLowerInvariant()
            Case ".mp4", ".mov", ".m4v", ".3gp", ".avi", ".mkv", ".mpg", ".mpeg", ".wmv"
                Return True
        End Select
        Return False
    End Function

    Private Function PositionDuVoisin(fichier As String) As (Lat As Double, Lon As Double)?
        Try
            Dim nom As String = Global.System.IO.Path.GetFileNameWithoutExtension(fichier)
            Dim m As Match = Regex.Match(nom, "^(?<prefixe>.*?)(?<numero>\d+)$")
            If Not m.Success Then Return Nothing
            Dim prefixe As String = m.Groups("prefixe").Value
            Dim chiffres As String = m.Groups("numero").Value
            Dim numero As Long
            If Not Long.TryParse(chiffres, numero) Then Return Nothing

            Dim contenu As Dictionary(Of String, List(Of String)) = ContenuDuDossier(Global.System.IO.Path.GetDirectoryName(fichier))

            ' Du plus proche au plus lointain ; a egalite, celui d'avant.
            For ecart As Integer = 1 To EcartVoisins
                For Each voisin As Long In {numero - ecart, numero + ecart}
                    If voisin < 0 Then Continue For
                    Dim nomVoisin As String = (prefixe & voisin.ToString().PadLeft(chiffres.Length, "0"c)).ToLowerInvariant()
                    Dim chemins As List(Of String) = Nothing
                    If Not contenu.TryGetValue(nomVoisin, chemins) Then Continue For
                    For Each chemin As String In chemins
                        Dim p As (Lat As Double, Lon As Double)? = PositionDuFichier(chemin)
                        If p.HasValue Then Return p
                    Next
                Next
            Next
        Catch
            ' Dossier illisible : pas de lieu deduit.
        End Try
        Return Nothing
    End Function

    Private Function ContenuDuDossier(dossier As String) As Dictionary(Of String, List(Of String))
        Dim contenu As Dictionary(Of String, List(Of String)) = Nothing
        If _dossiersLus.TryGetValue(dossier, contenu) Then Return contenu
        contenu = New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)
        For Each f As String In Global.System.IO.Directory.GetFiles(dossier)
            Dim cle As String = Global.System.IO.Path.GetFileNameWithoutExtension(f).ToLowerInvariant()
            Dim liste As List(Of String) = Nothing
            If Not contenu.TryGetValue(cle, liste) Then
                liste = New List(Of String)
                contenu(cle) = liste
            End If
            liste.Add(f)
        Next
        _dossiersLus(dossier) = contenu
        Return contenu
    End Function

    Private Function PositionDuFichier(chemin As String) As (Lat As Double, Lon As Double)?
        Dim p As (Lat As Double, Lon As Double)? = Nothing
        If _positionsLues.TryGetValue(chemin, p) Then Return p
        Try
            p = PositionGps(ImageMetadataReader.ReadMetadata(chemin))
        Catch
            p = Nothing
        End Try
        _positionsLues(chemin) = p
        Return p
    End Function

    ''' <summary>Nom du lieu pour des coordonnees GPS : sur Internet si l'option est cochee (et que
    ''' "Hors ligne sans internet" ne l'est pas), sinon - ou en cas d'echec - table Cities locale.</summary>
    Public Function NomDuLieu(lat As Double, lon As Double) As String
        Return PartiesDuLieu(lat, lon).Texte
    End Function

    Public Function PartiesDuLieu(lat As Double, lon As Double) As LieuAffiche
        If LieuEnLigne AndAlso Not SansInternet Then
            Dim enLigne As LieuAffiche = LieuOpenStreetMap(lat, lon)
            If Not String.IsNullOrEmpty(enLigne.Localite) Then Return enLigne
        End If
        Return VillePlusProche(lat, lon)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Lieu en ligne : OpenStreetMap / Nominatim
    '
    ' Regles d'usage du service (operations.osmfoundation.org/policies/nominatim) : 1 requete par
    ' seconde au plus, application identifiee, resultats mis en cache. Les coordonnees sont
    ' arrondies a 0,01 degre (~1 km) AVANT l'envoi : moins de precision transmise, et une meme
    ' zone n'est demandee qu'une fois. Le cache data\LieuxEnLigne.txt est permanent : un lieu deja
    ' trouve reste disponible, meme hors ligne.
    ' Appele sur le fil de l'interface : requete synchrone courte (1,5 s max), et apres un echec on
    ' n'insiste pas pendant 2 minutes pour ne pas faire attendre le diaporama a chaque photo.
    ' ------------------------------------------------------------------------------------------

    Private ReadOnly _http As HttpClient = CreerClientHttp()
    Private ReadOnly _cache As New Lazy(Of Dictionary(Of String, LieuOsm))(AddressOf ChargerCache)
    Private _derniereRequete As Date = Date.MinValue
    Private _pasDeReseauJusqua As Date = Date.MinValue

    ''' <summary>Reponse OpenStreetMap memorisee : localite, code pays ISO ("fr", "us"...) et nom du
    ''' pays. Le nom affiche est calcule a la lecture (Formater), pas stocke : changer la regle
    ''' d'affichage s'applique donc aussi aux lieux deja en cache.</summary>
    Private Structure LieuOsm
        Public Localite As String
        Public CodePays As String
        Public Pays As String
    End Structure

    Private Function CreerClientHttp() As HttpClient
        Dim client As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(1.5)}
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Slide-diaporama/" & SlideVersion & " (usage personnel)")
        Return client
    End Function

    Private Function LieuOpenStreetMap(lat As Double, lon As Double) As LieuAffiche
        Dim latArrondie As Double = Math.Round(lat, 2)
        Dim lonArrondie As Double = Math.Round(lon, 2)
        Dim cle As String = latArrondie.ToString("0.00", CultureInfo.InvariantCulture) & ";" &
                            lonArrondie.ToString("0.00", CultureInfo.InvariantCulture)

        Dim lieu As LieuOsm = Nothing
        Dim enCache As Boolean = _cache.Value.TryGetValue(cle, lieu)
        ' Un nom memorise en alphabet grec ou cyrillique date de l'ancienne requete : on le
        ' redemande (nom francais ou anglais, voir plus bas) ; a defaut, il sera translittere.
        If enCache AndAlso Not AlphabetNonLatin(lieu.Localite) Then Return Formater(lieu)
        If Date.Now < _pasDeReseauJusqua Then Return If(enCache, Formater(lieu), New LieuAffiche())

        ' Au plus une requete par seconde.
        Dim attente As Double = 1100 - (Date.Now - _derniereRequete).TotalMilliseconds
        If attente > 0 Then Thread.Sleep(CInt(attente))
        _derniereRequete = Date.Now

        Try
            Dim url As String = String.Format(CultureInfo.InvariantCulture,
                "https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={0:0.00}&lon={1:0.00}&zoom=12&addressdetails=1&accept-language=fr,en",
                latArrondie, lonArrondie)
            Using requete As New HttpRequestMessage(HttpMethod.Get, url),
                  delai As New CancellationTokenSource(TimeSpan.FromSeconds(1.5)),
                  reponse As HttpResponseMessage = _http.Send(requete, delai.Token)
                reponse.EnsureSuccessStatusCode()
                Using flux As Global.System.IO.Stream = reponse.Content.ReadAsStream(delai.Token),
                      json As JsonDocument = JsonDocument.Parse(flux)
                    lieu = LireReponse(json.RootElement)
                End Using
            End Using
        Catch ex As Exception
            _pasDeReseauJusqua = Date.Now.AddMinutes(2)
            DiagLog("Lieu en ligne : echec (" & ex.Message & ") -> table locale pendant 2 min")
            Return If(enCache, Formater(lieu), New LieuAffiche())
        End Try

        ' Aussi memorise quand OpenStreetMap ne trouve rien (en mer...) : pas de nouvelle demande,
        ' la table locale prend le relais.
        MemoriserDansCache(cle, lieu)
        Return Formater(lieu)
    End Function

    ''' <summary>Localite la plus precise disponible (ville, bourg, village...) et pays.</summary>
    Private Function LireReponse(racine As JsonElement) As LieuOsm
        Dim lieu As New LieuOsm With {.Localite = "", .CodePays = "", .Pays = ""}
        Dim adresse As JsonElement
        If Not racine.TryGetProperty("address", adresse) Then Return lieu

        ' Le bourg ou le village d'abord : dans certains pays (Grece...), "city" est le nom de
        ' l'entite ADMINISTRATIVE ("Thira Municipal Unit") alors que "town" est la vraie localite
        ' ("Firá", Santorin). En France, un village n'a pas de champ "city" : rien ne change.
        For Each champ As String In {"town", "village", "city", "municipality", "hamlet", "suburb", "county", "state"}
            Dim valeur As JsonElement
            If adresse.TryGetProperty(champ, valeur) AndAlso valeur.ValueKind = JsonValueKind.String Then
                lieu.Localite = If(valeur.GetString(), "").Trim()
                If lieu.Localite <> "" Then Exit For
            End If
        Next
        Dim code As JsonElement
        If adresse.TryGetProperty("country_code", code) AndAlso code.ValueKind = JsonValueKind.String Then
            lieu.CodePays = If(code.GetString(), "").Trim().ToLowerInvariant()
        End If
        Dim pays As JsonElement
        If adresse.TryGetProperty("country", pays) AndAlso pays.ValueKind = JsonValueKind.String Then
            lieu.Pays = If(pays.GetString(), "").Trim()
        End If
        Return lieu
    End Function

    ''' <summary>"Localite Pays" ; pas de pays pour la France (comme la table locale), nom court sinon.</summary>
    Private Function Formater(lieu As LieuOsm) As LieuAffiche
        If String.IsNullOrEmpty(lieu.Localite) Then Return New LieuAffiche()
        ' Lisible dans tous les cas : sans les mots administratifs, et en lettres latines.
        Dim localite As String = EnLettresLatines(SansMotsAdministratifs(lieu.Localite))
        If lieu.CodePays = "fr" Then Return New LieuAffiche With {.Localite = localite, .Pays = ""}
        Return New LieuAffiche With {.Localite = localite,
                                     .Pays = EnLettresLatines(PaysCourt(If(lieu.CodePays, ""), If(lieu.Pays, "")))}
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Noms lisibles
    '
    ' OpenStreetMap donne le nom francais, sinon anglais (accept-language=fr,en), sinon celui du
    ' pays, dans son alphabet : grec en Grece, cyrillique en Bulgarie... illisible a l'ecran
    ' (demande de Jacob). On retire donc les mots administratifs ("Municipal Unit",
    ' "Δημοτική Ενότητα"...) puis on translittere le grec et le cyrillique en lettres latines.
    ' ------------------------------------------------------------------------------------------

    Private ReadOnly MotsAdministratifs As New Regex(
        "^(Municipal Unit of|Municipality of|Regional Unit of|Δημοτική Ενότητα|Δήμος|Κοινότητα|Περιφερειακή Ενότητα)\s+|" &
        "\s+(Municipal Unit|Municipality|Regional Unit|Community)$",
        RegexOptions.IgnoreCase Or RegexOptions.CultureInvariant)

    Private Function SansMotsAdministratifs(nom As String) As String
        Dim propre As String = MotsAdministratifs.Replace(If(nom, ""), "").Trim()
        Return If(propre = "", If(nom, "").Trim(), propre)
    End Function

    ''' <summary>Vrai si le texte contient des lettres grecques ou cyrilliques.</summary>
    Private Function AlphabetNonLatin(texte As String) As Boolean
        If String.IsNullOrEmpty(texte) Then Return False
        For Each c As Char In texte
            If (c >= ChrW(&H370) AndAlso c <= ChrW(&H3FF)) OrElse (c >= ChrW(&H1F00) AndAlso c <= ChrW(&H1FFF)) OrElse
               (c >= ChrW(&H400) AndAlso c <= ChrW(&H4FF)) Then Return True
        Next
        Return False
    End Function

    ' Grec moderne (proche de la norme ELOT 743) et cyrillique (russe, bulgare, ukrainien).
    Private ReadOnly Digrammes As String()() = {
        New String() {"ου", "ou"}, New String() {"αυ", "av"}, New String() {"ευ", "ev"},
        New String() {"γγ", "ng"}, New String() {"τσ", "ts"}, New String() {"τζ", "tz"}}
    Private ReadOnly Lettres As New Dictionary(Of Char, String) From {
        {"α"c, "a"}, {"β"c, "v"}, {"γ"c, "g"}, {"δ"c, "d"}, {"ε"c, "e"}, {"ζ"c, "z"}, {"η"c, "i"},
        {"θ"c, "th"}, {"ι"c, "i"}, {"κ"c, "k"}, {"λ"c, "l"}, {"μ"c, "m"}, {"ν"c, "n"}, {"ξ"c, "x"},
        {"ο"c, "o"}, {"π"c, "p"}, {"ρ"c, "r"}, {"σ"c, "s"}, {"ς"c, "s"}, {"τ"c, "t"}, {"υ"c, "y"},
        {"φ"c, "f"}, {"χ"c, "ch"}, {"ψ"c, "ps"}, {"ω"c, "o"},
        {"а"c, "a"}, {"б"c, "b"}, {"в"c, "v"}, {"г"c, "g"}, {"д"c, "d"}, {"е"c, "e"}, {"ё"c, "e"},
        {"ж"c, "zh"}, {"з"c, "z"}, {"и"c, "i"}, {"й"c, "y"}, {"к"c, "k"}, {"л"c, "l"}, {"м"c, "m"},
        {"н"c, "n"}, {"о"c, "o"}, {"п"c, "p"}, {"р"c, "r"}, {"с"c, "s"}, {"т"c, "t"}, {"у"c, "u"},
        {"ф"c, "f"}, {"х"c, "kh"}, {"ц"c, "ts"}, {"ч"c, "ch"}, {"ш"c, "sh"}, {"щ"c, "shch"},
        {"ъ"c, ""}, {"ы"c, "y"}, {"ь"c, ""}, {"э"c, "e"}, {"ю"c, "yu"}, {"я"c, "ya"},
        {"і"c, "i"}, {"ї"c, "yi"}, {"є"c, "ye"}, {"ґ"c, "g"}}

    ''' <summary>Translittere le grec et le cyrillique en lettres latines, mot par mot, en gardant
    ''' la majuscule initiale ("Θήρα" -> "Thira", "Пловдив" -> "Plovdiv"). Le reste est inchange.</summary>
    Public Function EnLettresLatines(texte As String) As String
        If Not AlphabetNonLatin(texte) Then Return If(texte, "")
        Dim mots As String() = texte.Split(" "c)
        For i As Integer = 0 To mots.Length - 1
            If Not AlphabetNonLatin(mots(i)) Then Continue For
            ' Sans accents (ά -> α, ё reste ё : on ne retire que les signes combinants).
            Dim decompose As String = mots(i).Normalize(NormalizationForm.FormD)
            Dim sansAccent As New StringBuilder()
            For Each c As Char In decompose
                If CharUnicodeInfo.GetUnicodeCategory(c) <> UnicodeCategory.NonSpacingMark Then sansAccent.Append(c)
            Next
            Dim minuscule As String = sansAccent.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant()
            For Each d As String() In Digrammes
                minuscule = minuscule.Replace(d(0), d(1))
            Next
            ' En debut de mot, μπ / ντ / γκ se lisent b / d / g ; ailleurs mb / nd / ng.
            If minuscule.StartsWith("μπ") Then minuscule = "b" & minuscule.Substring(2)
            If minuscule.StartsWith("ντ") Then minuscule = "d" & minuscule.Substring(2)
            If minuscule.StartsWith("γκ") Then minuscule = "g" & minuscule.Substring(2)
            minuscule = minuscule.Replace("μπ", "mb").Replace("ντ", "nd").Replace("γκ", "ng")
            Dim latin As New StringBuilder()
            For Each c As Char In minuscule
                Dim l As String = Nothing
                latin.Append(If(Lettres.TryGetValue(c, l), l, c.ToString()))
            Next
            Dim mot As String = latin.ToString()
            If mot.Length > 0 AndAlso Char.IsUpper(mots(i)(0)) Then mot = Char.ToUpperInvariant(mot(0)) & mot.Substring(1)
            mots(i) = mot
        Next
        Return String.Join(" ", mots)
    End Function

    ' Noms courts, par code pays ISO, quand le nom officiel donne par OpenStreetMap est long
    ' ("États-Unis d'Amérique" -> "USA"). Les autres pays gardent le nom OpenStreetMap.
    Private ReadOnly PaysCourts As New Dictionary(Of String, String) From {
        {"us", "USA"}, {"gb", "Angleterre"}, {"ae", "Émirats"}, {"do", "Rép. dominicaine"},
        {"cd", "RD Congo"}, {"cg", "Congo"}, {"cf", "Centrafrique"}, {"cz", "Tchéquie"},
        {"ba", "Bosnie"}, {"mk", "Macédoine"}, {"va", "Vatican"}, {"kr", "Corée"},
        {"kp", "Corée du Nord"}, {"ru", "Russie"}, {"sa", "Arabie"}, {"ps", "Palestine"},
        {"vn", "Vietnam"}, {"la", "Laos"}, {"mm", "Birmanie"}, {"tz", "Tanzanie"}, {"sy", "Syrie"},
        {"ir", "Iran"}, {"bo", "Bolivie"}, {"ve", "Venezuela"}, {"md", "Moldavie"}, {"tw", "Taïwan"},
        {"st", "Sao Tomé"}, {"kn", "Saint-Kitts"}, {"vc", "Saint-Vincent"}, {"tt", "Trinité"},
        {"ag", "Antigua"}, {"gq", "Guinée équat."}, {"pg", "Papouasie"}, {"sb", "Salomon"},
        {"mh", "Marshall"}, {"fm", "Micronésie"}, {"gw", "Guinée-Bissau"}, {"nl", "Pays-Bas"},
        {"ch", "Suisse"}, {"be", "Belgique"}, {"de", "Allemagne"}, {"es", "Espagne"}, {"it", "Italie"},
        {"pt", "Portugal"}, {"ma", "Maroc"}, {"il", "Israël"}, {"mv", "Maldives"}}

    Private Function PaysCourt(code As String, nomOsm As String) As String
        Dim court As String = Nothing
        If code <> "" AndAlso PaysCourts.TryGetValue(code, court) Then Return court
        ' Par securite : certains noms OSM ajoutent la langue locale ("Maroc / ..."), on garde la
        ' premiere partie.
        For Each separateur As String In {" / ", ";", " ("}
            Dim p As Integer = nomOsm.IndexOf(separateur, StringComparison.Ordinal)
            If p > 0 Then nomOsm = nomOsm.Substring(0, p)
        Next
        Return nomOsm.Trim()
    End Function

    Private Function FichierCache() As String
        Return AppPath & "\data\LieuxEnLigne.txt"
    End Function

    ''' <summary>Format : cle TAB localite TAB code pays TAB pays. Les lignes a l'ancien format (nom
    ''' deja formate, 2 champs) sont ignorees : ces lieux seront simplement redemandes une fois.</summary>
    Private Function ChargerCache() As Dictionary(Of String, LieuOsm)
        Dim cache As New Dictionary(Of String, LieuOsm)
        Try
            If Global.System.IO.File.Exists(FichierCache()) Then
                For Each ligne As String In Global.System.IO.File.ReadAllLines(FichierCache(), Encoding.UTF8)
                    Dim champs As String() = ligne.Split(ControlChars.Tab)
                    If champs.Length = 4 Then
                        cache(champs(0)) = New LieuOsm With {.Localite = champs(1), .CodePays = champs(2), .Pays = champs(3)}
                    End If
                Next
            End If
        Catch ex As Exception
            DiagLog("Lieu en ligne : cache illisible (" & ex.Message & ")")
        End Try
        Return cache
    End Function

    Private Sub MemoriserDansCache(cle As String, lieu As LieuOsm)
        _cache.Value(cle) = lieu
        Try
            Global.System.IO.File.AppendAllText(FichierCache(),
                String.Join(ControlChars.Tab, cle, lieu.Localite, lieu.CodePays, lieu.Pays) & ControlChars.CrLf,
                New UTF8Encoding(False))
        Catch
            ' Cache non ecrit : le lieu sera simplement redemande au prochain lancement.
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Metadonnees
    ' ------------------------------------------------------------------------------------------

    Private Function DatePriseDeVue(dossiers As IReadOnlyList(Of MetadataExtractor.Directory)) As Date?
        Dim d As Date

        ' Photo : date de prise de vue, sinon date EXIF generale.
        For Each exif As ExifSubIfdDirectory In dossiers.OfType(Of ExifSubIfdDirectory)()
            If exif.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, d) AndAlso Plausible(d) Then Return d
        Next
        For Each exif As ExifIfd0Directory In dossiers.OfType(Of ExifIfd0Directory)()
            If exif.TryGetDateTime(ExifDirectoryBase.TagDateTime, d) AndAlso Plausible(d) Then Return d
        Next

        ' Video QuickTime (.mov iPhone) : "Creation Date" (heure locale), sinon "Created" de l'en-tete.
        For Each q As MetadataExtractor.Directory In dossiers.Where(Function(x) x.Name = "QuickTime Metadata Header")
            If q.TryGetDateTime(&H7, d) AndAlso Plausible(d) Then Return d
        Next
        For Each q As MetadataExtractor.Directory In dossiers.Where(Function(x) x.Name = "QuickTime Movie Header")
            If q.TryGetDateTime(&H3, d) AndAlso Plausible(d) Then Return d
        Next
        Return Nothing
    End Function

    ''' <summary>Ecarte les dates vides : les MP4 telecharges portent "1904" (date non renseignee).</summary>
    Private Function Plausible(d As Date) As Boolean
        Return d.Year >= 1971 AndAlso d <= Date.Now.AddDays(2)
    End Function

    Private Function PositionGps(dossiers As IReadOnlyList(Of MetadataExtractor.Directory)) As (Lat As Double, Lon As Double)?
        ' Photo : GPS EXIF ; la bibliotheque applique elle-meme N/S et E/O.
        For Each gps As GpsDirectory In dossiers.OfType(Of GpsDirectory)()
            Dim p As GeoLocation = Nothing
            If gps.TryGetGeoLocation(p) AndAlso Valide(p.Latitude, p.Longitude) Then Return (p.Latitude, p.Longitude)
        Next

        ' Video QuickTime : chaine ISO 6709 "+48.8177+002.4439+027.647/", toujours avec un POINT
        ' decimal, quelle que soit la langue du PC.
        For Each q As MetadataExtractor.Directory In dossiers.Where(Function(x) x.Name = "QuickTime Metadata Header")
            Dim iso As String = q.GetDescription(&HE)
            If iso Is Nothing Then Continue For
            Dim m As Match = Regex.Match(iso, "^\s*([+-]\d+(?:\.\d+)?)([+-]\d+(?:\.\d+)?)")
            If Not m.Success Then Continue For
            Dim lat As Double = Double.Parse(m.Groups(1).Value, CultureInfo.InvariantCulture)
            Dim lon As Double = Double.Parse(m.Groups(2).Value, CultureInfo.InvariantCulture)
            If Valide(lat, lon) Then Return (lat, lon)
        Next
        Return Nothing
    End Function

    Private Function Valide(lat As Double, lon As Double) As Boolean
        Return Math.Abs(lat) <= 90 AndAlso Math.Abs(lon) <= 180 AndAlso Not (lat = 0 AndAlso lon = 0)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Villes (table Cities de la base Access)
    ' ------------------------------------------------------------------------------------------

    ''' <summary>
    ''' Les villes sont des donnees de reference livrees avec le programme : on les lit dans la
    ''' base LOCALE (data\slide.accdb), et seulement a defaut dans la base en service.
    '''
    ''' Avant, elles etaient lues dans DatabasePath. Avec "Minuterie en reseau", DatabasePath est
    ''' la base partagee (X:\SlideShare\slide.accdb), qui ne contient que la minuterie : la table
    ''' Cities semblait alors avoir disparu et aucune localite ne s'affichait.
    ''' </summary>
    Private Function ChargerVilles() As Ville()
        Dim essayees As New List(Of String)
        For Each chemin As String In {DatabaseLocalPath, DatabasePath}
            If String.IsNullOrEmpty(chemin) OrElse essayees.Contains(chemin, StringComparer.OrdinalIgnoreCase) Then Continue For
            essayees.Add(chemin)
            If Not System.IO.File.Exists(chemin) Then Continue For
            Dim villes As Ville() = LireVilles(chemin)
            If villes.Length > 0 Then Return villes
        Next
        DiagLog("Lieux : aucune table Cities utilisable (" & String.Join(" ; ", essayees) & ")")
        Return Array.Empty(Of Ville)()
    End Function

    Private Function LireVilles(chemin As String) As Ville()
        Dim liste As New List(Of Ville)(65000)
        Try
            Using cn As New OleDbConnection(OLEDBPROVIDER & chemin)
                cn.Open()
                Using cmd As New OleDbCommand("SELECT Ville, Pays, Latitude, Longitude FROM Cities", cn),
                      r As OleDbDataReader = cmd.ExecuteReader()
                    While r.Read()
                        If IsDBNull(r("Latitude")) OrElse IsDBNull(r("Longitude")) Then Continue While
                        liste.Add(New Ville With {
                            .Lieu = NomAffiche(r("Ville").ToString(), r("Pays").ToString()),
                            .Lat = Convert.ToDouble(r("Latitude")),
                            .Lon = Convert.ToDouble(r("Longitude"))})
                    End While
                End Using
            End Using
        Catch ex As Exception
            ' Sans base, on affiche seulement la date (jamais d'exception vers le diaporama).
            DiagLog("Lieux : villes non chargees depuis " & chemin & " (" & ex.Message & ")")
        End Try
        Return liste.ToArray()
    End Function

    ''' <summary>Meme regle d'affichage qu'avant : pas de pays pour la France ("Europe/Paris"),
    ''' sinon la partie apres "/" ("Africa/Maroc" -> "Marrakech Maroc").</summary>
    Private Function NomAffiche(ville As String, pays As String) As LieuAffiche
        If pays.Contains("Paris") Then Return New LieuAffiche With {.Localite = ville, .Pays = ""}
        Dim p As Integer = pays.IndexOf("/"c)
        If p >= 0 Then pays = pays.Substring(p + 1)
        Return New LieuAffiche With {.Localite = ville, .Pays = pays.Trim()}
    End Function

    Private Function VillePlusProche(lat As Double, lon As Double) As LieuAffiche
        Dim villes As Ville() = _villes.Value
        Dim meilleure As New LieuAffiche()
        Dim distanceMin As Double = Double.MaxValue
        For Each v As Ville In villes
            Dim d As Double = DistanceKm(lat, lon, v.Lat, v.Lon)
            If d < distanceMin Then
                distanceMin = d
                meilleure = v.Lieu
            End If
        Next
        Return meilleure
    End Function

    Private Function DistanceKm(lat1 As Double, lon1 As Double, lat2 As Double, lon2 As Double) As Double
        Const rad As Double = Math.PI / 180
        Dim dLat As Double = (lat2 - lat1) * rad
        Dim dLon As Double = (lon2 - lon1) * rad
        Dim a As Double = Math.Sin(dLat / 2) ^ 2 + Math.Cos(lat1 * rad) * Math.Cos(lat2 * rad) * Math.Sin(dLon / 2) ^ 2
        Return 6371 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a))
    End Function

End Module
