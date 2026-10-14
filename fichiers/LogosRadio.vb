Option Strict On
Option Explicit On

Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports System.Threading.Tasks

''' <summary>
''' Logo d'une station de radio pour la pochette de la carte album (20/10/2026). Il vient de l'annuaire radio-browser.info
''' (champ "favicon", cherche par adresse du flux puis par nom exact), est charge EN TACHE DE FOND et garde en PNG dans
''' data\settings\logos_radio\. Sans logo connu, un repere ".aucun" evite de redemander pendant 7 jours.
''' </summary>
Friend Module LogosRadio

    Private ReadOnly Serveurs As String() = {"de1", "nl1", "at1"}
    Private ReadOnly _http As New HttpClient With {.Timeout = TimeSpan.FromSeconds(12)}
    Private ReadOnly _enCours As New HashSet(Of String)
    Private ReadOnly _verrou As New Object()

    Private Function Dossier() As String
        Return Path.Combine(DossierReglages, "logos_radio")
    End Function

    Private Function Nom(url As String) As String
        Using sha As SHA1 = SHA1.Create()
            Dim h As Byte() = sha.ComputeHash(Encoding.UTF8.GetBytes(url.Trim().ToLowerInvariant()))
            Return BitConverter.ToString(h, 0, 8).Replace("-", "")
        End Using
    End Function

    ''' <summary>Le logo de la station (une copie a liberer), ou Nothing s'il n'est pas (encore) connu ; dans ce cas la recherche
    ''' est lancee en tache de fond et le logo sera disponible a un prochain appel.</summary>
    Friend Function Logo(url As String, nomStation As String) As Image
        Try
            If String.IsNullOrWhiteSpace(url) Then Return Nothing
            Dim base As String = Path.Combine(Dossier(), Nom(url))
            Dim png As String = base & ".png"
            If File.Exists(png) Then
                Try
                    Dim octets As Byte() = File.ReadAllBytes(png)
                    Using flux As New MemoryStream(octets)
                        Using lu As Image = Image.FromStream(flux)
                            Return New Bitmap(lu)
                        End Using
                    End Using
                Catch ex As Exception
                    Return Nothing
                End Try
            End If
            Dim aucun As String = base & ".aucun"
            If File.Exists(aucun) AndAlso (Now - File.GetLastWriteTime(aucun)).TotalDays < 7 Then Return Nothing
            SyncLock _verrou
                If Not _enCours.Add(url) Then Return Nothing
            End SyncLock
            Dim station As String = If(nomStation, "")
            Task.Run(Async Function() As Task
                         Try
                             Await Chercher(url, station, png, aucun)
                         Catch ex As Exception
                             DiagLog("Logo radio : " & ex.Message)
                         Finally
                             SyncLock _verrou
                                 _enCours.Remove(url)
                             End SyncLock
                         End Try
                     End Function)
        Catch ex As Exception
            ' pas de logo : la carte se contente du nom
        End Try
        Return Nothing
    End Function

    Private Async Function Chercher(url As String, station As String, png As String, aucun As String) As Task
        Directory.CreateDirectory(Dossier())
        Dim adresseLogo As String = Await AdresseDuLogo("/json/stations/byurl?url=" & Uri.EscapeDataString(url.Trim()), "")
        If adresseLogo = "" AndAlso station.Trim() <> "" AndAlso station <> "Radio" Then
            adresseLogo = Await AdresseDuLogo("/json/stations/search?hidebroken=true&order=clickcount&reverse=true&limit=10&name=" & Uri.EscapeDataString(station.Trim()), station.Trim())
        End If
        If adresseLogo <> "" Then
            Try
                Using demande As New HttpRequestMessage(HttpMethod.Get, adresseLogo)
                    demande.Headers.TryAddWithoutValidation("User-Agent", "Slide/8 (diaporama)")
                    Using reponse As HttpResponseMessage = Await _http.SendAsync(demande)
                        reponse.EnsureSuccessStatusCode()
                        Dim octets As Byte() = Await reponse.Content.ReadAsByteArrayAsync()
                        If octets.Length > 0 AndAlso octets.Length < 6000000 Then
                            Using lu As Image = LireLImage(octets)
                                Dim echelle As Double = Math.Min(1.0, 512.0 / Math.Max(lu.Width, lu.Height))
                                Using petit As New Bitmap(Math.Max(1, CInt(lu.Width * echelle)), Math.Max(1, CInt(lu.Height * echelle)), PixelFormat.Format32bppArgb)
                                    Using g As Graphics = Graphics.FromImage(petit)
                                        g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                                        g.DrawImage(lu, 0, 0, petit.Width, petit.Height)
                                    End Using
                                    Dim temporaire As String = png & ".tmp"
                                    petit.Save(temporaire, ImageFormat.Png)
                                    File.Move(temporaire, png, True)
                                End Using
                            End Using
                            Exit Function
                        End If
                    End Using
                End Using
            Catch ex As Exception
                ' logo illisible (format non gere, serveur muet) : repere "aucun" ci-dessous
            End Try
        End If
        File.WriteAllText(aucun, adresseLogo)
    End Function

    ''' <summary>Une image bitmap (PNG, JPEG, GIF, ICO...) ou un dessin SVG (rendu en 512 pixels de large au plus par Svg.NET).</summary>
    Private Function LireLImage(octets As Byte()) As Image
        Dim debut As String = Encoding.UTF8.GetString(octets, 0, Math.Min(octets.Length, 1024))
        If debut.Contains("<svg", StringComparison.OrdinalIgnoreCase) Then
            Using flux As New MemoryStream(octets)
                Dim dessin As Svg.SvgDocument = Svg.SvgDocument.Open(Of Svg.SvgDocument)(flux)
                Dim l As Single = dessin.Width.Value, h As Single = dessin.Height.Value
                If dessin.ViewBox.Width > 0 AndAlso dessin.ViewBox.Height > 0 AndAlso (dessin.Width.Type = Svg.SvgUnitType.Percentage OrElse l <= 0) Then
                    l = dessin.ViewBox.Width : h = dessin.ViewBox.Height
                End If
                If l <= 0 OrElse h <= 0 Then l = 512 : h = 512
                Dim echelle As Single = 512.0F / Math.Max(l, h)
                Return dessin.Draw(Math.Max(1, CInt(l * echelle)), Math.Max(1, CInt(h * echelle)))
            End Using
        End If
        Using flux As New MemoryStream(octets)
            Return New Bitmap(Image.FromStream(flux))
        End Using
    End Function

    ''' <summary>Adresse du logo (champ "favicon") de la premiere station de la reponse qui en a un ; avec
    ''' <paramref name="nomExact"/>, seule une station de ce nom compte. "" si aucun serveur n'en donne.</summary>
    Private Async Function AdresseDuLogo(requete As String, nomExact As String) As Task(Of String)
        For Each serveur As String In Serveurs
            Try
                Using demande As New HttpRequestMessage(HttpMethod.Get, "https://" & serveur & ".api.radio-browser.info" & requete)
                    demande.Headers.TryAddWithoutValidation("User-Agent", "Slide/8 (diaporama)")
                    Using reponse As HttpResponseMessage = Await _http.SendAsync(demande)
                        reponse.EnsureSuccessStatusCode()
                        Dim json As String = Await reponse.Content.ReadAsStringAsync()
                        Using doc As JsonDocument = JsonDocument.Parse(json)
                            For Each e As JsonElement In doc.RootElement.EnumerateArray()
                                Dim logo As String = Texte(e, "favicon")
                                If logo = "" OrElse Not logo.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then Continue For
                                If nomExact <> "" AndAlso Not Texte(e, "name").Trim().Equals(nomExact, StringComparison.OrdinalIgnoreCase) Then Continue For
                                Return logo
                            Next
                        End Using
                        Return ""
                    End Using
                End Using
            Catch ex As Exception
                ' serveur suivant
            End Try
        Next
        Return ""
    End Function

    Private Function Texte(e As JsonElement, champ As String) As String
        Dim v As JsonElement = Nothing
        If e.ValueKind = JsonValueKind.Object AndAlso e.TryGetProperty(champ, v) AndAlso v.ValueKind = JsonValueKind.String Then
            Return If(v.GetString(), "").Trim()
        End If
        Return ""
    End Function

End Module
