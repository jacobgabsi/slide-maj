Imports System.Globalization
Imports System.IO
Imports System.Net.Http
Imports Newtonsoft.Json.Linq

''' <summary>
''' « Ma position » : ajoute la ville ou l'on se trouve a la table des villes de Slide (utilisee par la meteo et les horaires de Chabbat).
'''  1. La position vient de la localisation de Windows (GeoCoordinateWatcher) ; si elle est coupee ou inconnue, de l'adresse IP (ipwho.is : ville, coordonnees, fuseau).
'''  2. Avec une position Windows (coordonnees seules) : le nom de la ville vient d'OpenStreetMap (Nominatim) et le fuseau d'Open-Meteo. Seules les coordonnees sont envoyees.
'''  3. La ville est rangee dans la premiere ligne libre de la table City et notee dans data\settings\VillesAjoutees.txt (une ligne « nom;lat;lon;fuseau »), relue a chaque lancement.
''' </summary>
Friend Module VillesAjoutees

    Friend Class Resultat
        Friend Ok As Boolean
        Friend Nom As String = ""
        Friend Message As String = ""
        Friend Origine As String = ""
        Friend Lat As Double
        Friend Lon As Double
        Friend Fuseau As String = ""
        ''' <summary>Region et pays, pour que l'on reconnaisse la ville (« Hauts-de-Seine, France »).</summary>
        Friend Detail As String = ""
    End Class

    ''' <summary>Cherche une ville par son nom (Open-Meteo, base de lieux ; seul le nom tape est envoye). Ne l'ajoute pas : voir Ajouter.</summary>
    Friend Async Function ChercherParNomAsync(texte As String) As Task(Of Resultat)
        Dim r As New Resultat() With {.Origine = "votre recherche"}
        Try
            Dim url As String = "https://geocoding-api.open-meteo.com/v1/search?count=1&language=fr&format=json&name=" & Uri.EscapeDataString(texte.Trim())
            Dim j As JObject = JObject.Parse(Await _http.GetStringAsync(url))
            Dim t As JToken = j("results")?(0)
            If t Is Nothing Then
                r.Message = "Aucune ville trouvée pour « " & texte.Trim() & " »."
                Return r
            End If
            r.Nom = Convert.ToString(t("name"))
            r.Lat = CDbl(t("latitude")) : r.Lon = CDbl(t("longitude"))
            r.Fuseau = Convert.ToString(t("timezone"))
            r.Detail = String.Join(", ", {Convert.ToString(t("admin2")), Convert.ToString(t("admin1")), Convert.ToString(t("country"))}.Where(Function(x) x <> "").Distinct())
            r.Ok = r.Nom <> "" AndAlso r.Fuseau <> ""
            If Not r.Ok Then r.Message = "Ville trouvée, mais sans fuseau horaire."
        Catch ex As Exception
            DiagLog("Villes ajoutees : recherche par nom impossible (" & ex.Message & ")")
            r.Message = "Recherche impossible : " & ex.Message
        End Try
        Return r
    End Function

    ''' <summary>Range la ville trouvee dans la table (premiere ligne libre) et la note dans le fichier. Le nom d'une ville deja presente est repris tel quel.</summary>
    Friend Function Ajouter(r As Resultat) As Boolean
        For u As Integer = 1 To 50
            If String.Equals(Convert.ToString(City(u, 0)), r.Nom, StringComparison.OrdinalIgnoreCase) Then r.Nom = Convert.ToString(City(u, 0)) : Return True
        Next
        If Not Placer(r.Nom, r.Lat, r.Lon, r.Fuseau) Then
            r.Message = "La table des villes est pleine."
            Return False
        End If
        Noter(r.Nom, r.Lat, r.Lon, r.Fuseau)
        DiagLog("Villes ajoutees : « " & r.Nom & " » (" & r.Lat.ToString("0.###", CultureInfo.InvariantCulture) & ", " & r.Lon.ToString("0.###", CultureInfo.InvariantCulture) & ", " & r.Fuseau & ") par " & r.Origine)
        Return True
    End Function

    Private ReadOnly _http As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(12)}

    Private Function Fichier() As String
        Return Path.Combine(DossierReglages, "VillesAjoutees.txt")
    End Function

    ''' <summary>Au lancement : remet dans la table les villes ajoutees avec « Ma position ».</summary>
    Friend Sub Charger()
        Try
            If Not File.Exists(Fichier()) Then Exit Sub
            For Each ligne As String In File.ReadAllLines(Fichier())
                Dim p As String() = ligne.Split(";"c)
                If p.Length < 4 Then Continue For
                Dim lat As Double, lon As Double
                If Not Double.TryParse(p(1), NumberStyles.Float, CultureInfo.InvariantCulture, lat) Then Continue For
                If Not Double.TryParse(p(2), NumberStyles.Float, CultureInfo.InvariantCulture, lon) Then Continue For
                If p(0).Trim() = "" OrElse p(3).Trim() = "" Then Continue For
                Placer(p(0).Trim(), lat, lon, p(3).Trim())
            Next
        Catch ex As Exception
            DiagLog("Villes ajoutees : lecture impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Range la ville dans la premiere ligne libre de la table (ou ne fait rien si le nom y est deja). Faux : table pleine.</summary>
    Private Function Placer(nom As String, lat As Double, lon As Double, fuseau As String) As Boolean
        For u As Integer = 1 To 50
            If String.Equals(Convert.ToString(City(u, 0)), nom, StringComparison.OrdinalIgnoreCase) Then Return True
        Next
        For u As Integer = 1 To 50
            If Convert.ToString(City(u, 0)) = "" Then
                City(u, 0) = nom
                City(u, 1) = lat.ToString("0.#####", CultureInfo.CurrentCulture)         ' (la table est lue avec la culture courante : voir HeureDeShabbat, MeteoSlide.CoordonneesDe)
                City(u, 2) = lon.ToString("0.#####", CultureInfo.CurrentCulture)
                City(u, 3) = fuseau
                Return True
            End If
        Next
        Return False
    End Function

    Private Sub Noter(nom As String, lat As Double, lon As Double, fuseau As String)
        Try
            Dim lignes As New List(Of String)
            If File.Exists(Fichier()) Then lignes.AddRange(File.ReadAllLines(Fichier()).Where(Function(l) Not l.StartsWith(nom & ";", StringComparison.OrdinalIgnoreCase)))
            lignes.Add(nom & ";" & lat.ToString("0.#####", CultureInfo.InvariantCulture) & ";" & lon.ToString("0.#####", CultureInfo.InvariantCulture) & ";" & fuseau)
            Directory.CreateDirectory(DossierReglages)
            File.WriteAllLines(Fichier(), lignes)
        Catch ex As Exception
            DiagLog("Villes ajoutees : ecriture impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Cherche la position, trouve la ville, l'ajoute a la table. Ne leve jamais d'exception : le resultat porte le message a montrer.</summary>
    Friend Async Function LocaliserAsync() As Task(Of Resultat)
        Dim r As New Resultat()
        Try
            Dim lat As Double, lon As Double
            Dim nom As String = "", fuseau As String = ""
            ' 1. La localisation de Windows (dans un autre fil : elle peut mettre plusieurs secondes).
            Dim trouveeParWindows As Boolean = Await Task.Run(Function() PositionWindows(lat, lon))
            If trouveeParWindows Then
                r.Origine = "la localisation de Windows"
                nom = Await NomDeLaVilleAsync(lat, lon)
                fuseau = Await FuseauAsync(lat, lon)
            End If
            ' 2. Sinon : l'adresse IP.
            If Not trouveeParWindows OrElse nom = "" OrElse fuseau = "" Then
                Dim j As JObject = JObject.Parse(Await _http.GetStringAsync("https://ipwho.is/"))
                If CBool(If(j("success"), True)) Then
                    If Not trouveeParWindows Then
                        lat = CDbl(j("latitude")) : lon = CDbl(j("longitude"))
                        r.Origine = "l'adresse IP"
                    End If
                    If nom = "" Then nom = Convert.ToString(j("city"))
                    If fuseau = "" Then fuseau = Convert.ToString(j("timezone")?("id"))
                ElseIf Not trouveeParWindows Then
                    r.Message = "Position introuvable : la localisation de Windows est coupee ou inconnue, et la recherche par adresse IP a echoue."
                    Return r
                End If
            End If
            If nom = "" OrElse fuseau = "" Then
                r.Message = "Position trouvee, mais ni le nom de la ville ni son fuseau horaire n'ont pu etre determines."
                Return r
            End If
            ' (La ville n'est PAS ajoutee ici : l'adresse IP donne souvent la grande ville voisine ; la fenetre demande confirmation, voir Ajouter.)
            r.Ok = True
            r.Nom = nom
            r.Lat = lat : r.Lon = lon : r.Fuseau = fuseau
        Catch ex As Exception
            DiagLog("Villes ajoutees : echec (" & ex.Message & ")")
            r.Message = "Localisation impossible : " & ex.Message
        End Try
        Return r
    End Function

    ''' <summary>La position donnee par Windows (Parametres > Confidentialite > Localisation). Faux si coupee, refusee ou inconnue au bout de 8 secondes.</summary>
    Private Function PositionWindows(ByRef lat As Double, ByRef lon As Double) As Boolean
        Try
            Dim script As String =
                "Add-Type -AssemblyName System.Device;" &
                "$w = New-Object System.Device.Location.GeoCoordinateWatcher([System.Device.Location.GeoPositionAccuracy]::Default);" &
                "[void]$w.TryStart($false,[TimeSpan]::FromSeconds(7));" &
                "$p = $w.Position.Location;" &
                "if ($p.IsUnknown) { 'inconnu' } else { ('{0};{1}' -f $p.Latitude.ToString([Globalization.CultureInfo]::InvariantCulture), $p.Longitude.ToString([Globalization.CultureInfo]::InvariantCulture)) }"
            Dim encode As String = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script))
            Dim info As New ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " & encode) With {
                .UseShellExecute = False, .CreateNoWindow = True, .RedirectStandardOutput = True, .RedirectStandardError = True}
            Using p As Process = Process.Start(info)
                If Not p.WaitForExit(15000) Then
                    Try
                        p.Kill()
                    Catch ex As Exception
                    End Try
                    Return False
                End If
                Dim sortie As String = p.StandardOutput.ReadToEnd().Trim()
                Dim t As String() = sortie.Split(";"c)
                If t.Length = 2 AndAlso Double.TryParse(t(0), NumberStyles.Float, CultureInfo.InvariantCulture, lat) AndAlso Double.TryParse(t(1), NumberStyles.Float, CultureInfo.InvariantCulture, lon) Then
                    Return Not (lat = 0 AndAlso lon = 0)
                End If
            End Using
        Catch ex As Exception
            DiagLog("Villes ajoutees : localisation Windows impossible (" & ex.Message & ")")
        End Try
        Return False
    End Function

    ''' <summary>Le nom de la ville a ces coordonnees (OpenStreetMap / Nominatim, en francais). Vide si introuvable.</summary>
    Private Async Function NomDeLaVilleAsync(lat As Double, lon As Double) As Task(Of String)
        Try
            Dim url As String = "https://nominatim.openstreetmap.org/reverse?format=jsonv2&zoom=10&accept-language=fr&lat=" & lat.ToString("0.#####", CultureInfo.InvariantCulture) & "&lon=" & lon.ToString("0.#####", CultureInfo.InvariantCulture)
            Using req As New HttpRequestMessage(HttpMethod.Get, url)
                req.Headers.UserAgent.ParseAdd("Slide")
                Using rep As HttpResponseMessage = Await _http.SendAsync(req)
                    Dim a As JToken = JObject.Parse(Await rep.Content.ReadAsStringAsync())("address")
                    If a Is Nothing Then Return ""
                    For Each cle As String In {"city", "town", "village", "municipality", "county"}
                        Dim v As String = Convert.ToString(a(cle))
                        If v <> "" Then Return v
                    Next
                End Using
            End Using
        Catch ex As Exception
            DiagLog("Villes ajoutees : nom de la ville introuvable (" & ex.Message & ")")
        End Try
        Return ""
    End Function

    ''' <summary>Le fuseau horaire (nom IANA) de ces coordonnees, donne par Open-Meteo. Vide si introuvable.</summary>
    Private Async Function FuseauAsync(lat As Double, lon As Double) As Task(Of String)
        Try
            Dim url As String = "https://api.open-meteo.com/v1/forecast?current=temperature_2m&timezone=auto&latitude=" & lat.ToString("0.#####", CultureInfo.InvariantCulture) & "&longitude=" & lon.ToString("0.#####", CultureInfo.InvariantCulture)
            Return Convert.ToString(JObject.Parse(Await _http.GetStringAsync(url))("timezone"))
        Catch ex As Exception
            DiagLog("Villes ajoutees : fuseau introuvable (" & ex.Message & ")")
        End Try
        Return ""
    End Function

End Module
