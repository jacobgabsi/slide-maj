Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Globalization
Imports System.IO
Imports System.Net.Http
Imports Newtonsoft.Json.Linq

''' <summary>
''' Meteo de la journee, heure par heure, en petite carte posee sur l'image de Slide (demande de Jacob, 12/11/2026 : « la meteo de la journee par heure avec temperature
''' sous forme d'icone deplacable, petit et tres joli »). OPTION, decochee au depart (menu du clic droit > Meteo...).
'''
''' - Donnees : Open-Meteo (open-meteo.com), gratuit et SANS cle : seule la latitude / longitude de la ville choisie est envoyee. Les heures sont celles de la ville.
''' - La ville vient de la table des villes de Slide (la meme que les heures de Chabbat). Mise a jour toutes les 30 minutes (5 minutes apres un echec : la derniere meteo reste affichee).
''' - Dessin : une fenetre calque (FormCalqueImage), comme l'icone de la source de musique. Tout est trace en GDI+ (icones vectorielles : soleil, lune, nuages, pluie, neige, orage, brouillard).
''' - Souris : cliquer-glisser = deplace ; molette = taille (40 a 200 %) ; double-clic = les reglages ; clic droit = le menu de Slide.
''' - Reglages dans data\settings\Meteo.txt (cle=valeur), un fichier par poste (voir TransfertParametres.FichiersDeReglages).
''' </summary>
Friend Module MeteoSlide

    ' ------------------------------------------------------------------------------------------
    ' Reglages
    ' ------------------------------------------------------------------------------------------

    Friend Class ReglagesMeteo
        Friend Actif As Boolean = False
        ''' <summary>Les villes choisies (au moins une) : la carte les montre l'une apres l'autre (Rotation), ou au clic.</summary>
        Friend Villes As New List(Of String) From {"Paris"}
        ''' <summary>Vrai (par defaut) : la meteo est celle de la VILLE DE CHABBAT des options (Shabbat.Ville) et suit ses changements ; faux : les villes cochees ci-dessus.</summary>
        Friend SuivreChabbat As Boolean = True
        ''' <summary>Cases « jour » apres les heures (0 a 7, a partir de demain : icone, maxi, mini).</summary>
        Friend Jours As Integer = 3
        ''' <summary>Montrer les cases d'heures / les cases de jours (au moins un des deux).</summary>
        Friend AfficherHeures As Boolean = True
        Friend AfficherJours As Boolean = True
        ''' <summary>Transparence du fond de la carte en % (0 = opaque comme avant, 99 maximum) ; les textes et icones restent nets.</summary>
        Friend Transparence As Integer = 0
        ''' <summary>Vrai : la carte n'apparait que SecondesTemporaire secondes a chaque passage photo/video (ou video/photo). Si le diaporama ne montre que des photos ou que des videos, elle reste fixe.</summary>
        Friend Temporaire As Boolean = False
        Friend SecondesTemporaire As Integer = 10
        ''' <summary>Secondes entre deux villes quand il y en a plusieurs (3 a 60).</summary>
        Friend Rotation As Integer = 8
        ''' <summary>Heures entre deux colonnes (1, 2 ou 3).</summary>
        Friend Pas As Integer = 2
        ''' <summary>Nombre de cases d'heures (3 a 24).</summary>
        Friend Nombre As Integer = 6
        ''' <summary>Disposition de la carte : 0 horizontale (defaut), 1 verticale, 2 rectangulaire (grille sur deux rangees).</summary>
        Friend Disposition As Integer = 0
        ''' <summary>Taille en % (40 a 200).</summary>
        Friend Pourcent As Integer = 100
        ''' <summary>Place : fraction de la course horizontale / verticale dans l'image de Slide (0,0 = haut gauche).</summary>
        Friend X As Double = 0.0
        Friend Y As Double = 0.0
    End Class

    Friend Const PourcentMini As Integer = 15
    Friend Const PourcentMaxi As Integer = 200

    Private _reglages As ReglagesMeteo
    Private _pourcentALaSouris As Integer?

    Private Function FichierReglages() As String
        Return Path.Combine(DossierReglages, "Meteo.txt")
    End Function

    Friend Function Reglages() As ReglagesMeteo
        If _reglages Is Nothing Then _reglages = Lire()
        Return _reglages
    End Function

    Private Function Lire() As ReglagesMeteo
        Dim r As New ReglagesMeteo()
        Dim cles As New HashSet(Of String)
        Try
            If File.Exists(FichierReglages()) Then
                For Each ligne As String In File.ReadAllLines(FichierReglages())
                    Dim i As Integer = ligne.IndexOf("="c)
                    If i <= 0 Then Continue For
                    Dim cle As String = ligne.Substring(0, i).Trim().ToLowerInvariant()
                    cles.Add(cle)
                    Dim v As String = ligne.Substring(i + 1).Trim()
                    Dim n As Integer, d As Double
                    Select Case cle
                        Case "actif" : r.Actif = (v = "1")
                        Case "ville" : If v <> "" Then r.Villes = New List(Of String) From {v}         ' (ancien reglage : une seule ville)
                        Case "villes"
                            Dim liste As List(Of String) = v.Split(";"c).Select(Function(x) x.Trim()).Where(Function(x) x <> "").Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                            If liste.Count > 0 Then r.Villes = liste
                        Case "rotation" : If Integer.TryParse(v, n) Then r.Rotation = Math.Max(3, Math.Min(60, n))
                        Case "suivrechabbat" : r.SuivreChabbat = (v = "1")
                        Case "jours" : If Integer.TryParse(v, n) Then r.Jours = Math.Max(0, Math.Min(7, n))
                        Case "afficherheures" : r.AfficherHeures = (v <> "0")
                        Case "afficherjours" : r.AfficherJours = (v <> "0")
                        Case "transparence" : If Integer.TryParse(v, n) Then r.Transparence = Math.Max(0, Math.Min(99, n))
                        Case "temporaire" : r.Temporaire = (v = "1")
                        Case "secondestemporaire" : If Integer.TryParse(v, n) Then r.SecondesTemporaire = Math.Max(2, Math.Min(600, n))
                        Case "pas" : If Integer.TryParse(v, n) Then r.Pas = Math.Max(1, Math.Min(3, n))
                        Case "nombre" : If Integer.TryParse(v, n) Then r.Nombre = Math.Max(3, Math.Min(24, n))
                        Case "disposition" : If Integer.TryParse(v, n) Then r.Disposition = Math.Max(0, Math.Min(2, n))
                        Case "pourcent" : If Integer.TryParse(v, n) Then r.Pourcent = Math.Max(PourcentMini, Math.Min(PourcentMaxi, n))
                        Case "x" : If Double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, d) Then r.X = Math.Max(0, Math.Min(1, d))
                        Case "y" : If Double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, d) Then r.Y = Math.Max(0, Math.Min(1, d))
                    End Select
                Next
            End If
        Catch ex As Exception
            DiagLog("Meteo : reglages illisibles (" & ex.Message & ")")
        End Try
        ' Reglage absent : la ville de Chabbat par defaut, sauf si des villes avaient deja ete choisies a la main (reglages d'avant : on les garde).
        If Not cles.Contains("suivrechabbat") Then r.SuivreChabbat = Not (cles.Contains("villes") OrElse cles.Contains("ville"))
        Return r
    End Function

    ''' <summary>Les villes dont la meteo est montree : la ville de Chabbat (qui suit ses changements) ou, sinon, les villes cochees.</summary>
    Friend Function ListeDesVilles(r As ReglagesMeteo) As List(Of String)
        If Not r.SuivreChabbat Then Return r.Villes
        Dim v As String = ""
        Try
            v = Convert.ToString(Shabbat.Ville)
        Catch ex As Exception
            v = ""
        End Try
        Dim lat As Double, lon As Double
        If v = "" OrElse Not CoordonneesDe(v, lat, lon) Then v = "Paris"
        Return New List(Of String) From {v}
    End Function

    Friend Sub Enregistrer(r As ReglagesMeteo)
        If r.Temporaire AndAlso (_reglages Is Nothing OrElse Not _reglages.Temporaire) Then
            _dernierChangementMode = DateTime.Now          ' (option cochee a l'instant : la carte se montre le temps choisi)
            _signature = ""
        End If
        _reglages = r
        Try
            Directory.CreateDirectory(DossierReglages)
            File.WriteAllLines(FichierReglages(), {
                "actif=" & If(r.Actif, "1", "0"),
                "suivrechabbat=" & If(r.SuivreChabbat, "1", "0"),
                "villes=" & String.Join(";", r.Villes),
                "rotation=" & r.Rotation,
                "jours=" & r.Jours,
                "afficherheures=" & If(r.AfficherHeures, "1", "0"),
                "afficherjours=" & If(r.AfficherJours, "1", "0"),
                "transparence=" & r.Transparence,
                "temporaire=" & If(r.Temporaire, "1", "0"),
                "secondestemporaire=" & r.SecondesTemporaire,
                "pas=" & r.Pas,
                "nombre=" & r.Nombre,
                "disposition=" & r.Disposition,
                "pourcent=" & r.Pourcent,
                "x=" & r.X.ToString("0.####", CultureInfo.InvariantCulture),
                "y=" & r.Y.ToString("0.####", CultureInfo.InvariantCulture)})
        Catch ex As Exception
            DiagLog("Meteo : reglages non enregistres (" & ex.Message & ")")
        End Try
        _signature = ""
        ' La taille, la place, le pas et le nombre de cases se tirent des donnees deja la (48 heures) ; une ville nouvelle est lue au prochain passage (Rafraichir).
        If r.Villes.Count = 0 Then r.Villes.Add("Paris")
        If _indexVille >= r.Villes.Count Then _indexVille = 0
        _dernierChangement = DateTime.Now
        Rafraichir()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Donnees (Open-Meteo)
    ' ------------------------------------------------------------------------------------------

    Private Class Donnees
        Friend Ville As String
        Friend Maintenant As DateTime                ' heure locale de la ville a la lecture
        Friend LueLe As DateTime = DateTime.MinValue ' heure du PC a la lecture
        ''' <summary>L'heure locale de la ville MAINTENANT : celle de la lecture, avancee du temps ecoule depuis (la meteo n'est relue que toutes les 30 minutes : sans cela les cases restaient sur l'heure de la lecture, 12/11/2026).</summary>
        Friend Function MaintenantDeLaVille() As DateTime
            If LueLe = DateTime.MinValue Then Return Maintenant
            Return Maintenant + (DateTime.Now - LueLe)
        End Function
        Friend TempActuelle As Double
        Friend CodeActuel As Integer
        Friend JourActuel As Boolean
        Friend Heures As New List(Of PointHoraire)
        ''' <summary>Une prevision par jour (Heure = la date ; Temp = maxi ; Min = mini).</summary>
        Friend Jours As New List(Of PointHoraire)
    End Class

    Private Class PointHoraire
        Friend Heure As DateTime
        Friend Temp As Double
        Friend Code As Integer
        Friend Jour As Boolean
        Friend Pluie As Integer
        ''' <summary>Pour une case JOUR : l'etiquette (« Dem. », « Mer »...) et la temperature mini (Temp est alors la maxi).</summary>
        Friend Etiquette As String = ""
        Friend Min As Double
        Friend AMin As Boolean
    End Class

    ' Une meteo par ville choisie, gardee en memoire (mise a jour toutes les 30 minutes, 5 minutes apres un echec).
    Private ReadOnly _cache As New Dictionary(Of String, Donnees)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _prochaines As New Dictionary(Of String, DateTime)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _enCours As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Private _indexVille As Integer
    Private _dernierChangement As DateTime = DateTime.Now
    Private ReadOnly _http As New HttpClient With {.Timeout = TimeSpan.FromSeconds(15)}

    ''' <summary>Latitude / longitude de la ville dans la table de Slide (la meme que les heures de Chabbat).</summary>
    Friend Function CoordonneesDe(ville As String, ByRef lat As Double, ByRef lon As Double) As Boolean
        For u As Integer = 1 To 50
            If String.Equals(Convert.ToString(City(u, 0)), ville, StringComparison.OrdinalIgnoreCase) Then
                Dim a As Double, b As Double
                If Double.TryParse(Convert.ToString(City(u, 1)), NumberStyles.Float, CultureInfo.CurrentCulture, a) AndAlso
                   Double.TryParse(Convert.ToString(City(u, 2)), NumberStyles.Float, CultureInfo.CurrentCulture, b) Then
                    lat = a : lon = b
                    Return True
                End If
            End If
        Next
        Return False
    End Function

    ''' <summary>Les villes proposees (celles de la table de Slide, sans doublon).</summary>
    Friend Function Villes() As List(Of String)
        Dim l As New List(Of String)
        Dim lat As Double, lon As Double
        For u As Integer = 1 To 50
            Dim nom As String = Convert.ToString(City(u, 0))
            If nom <> "" AndAlso Not l.Contains(nom) AndAlso CoordonneesDe(nom, lat, lon) Then l.Add(nom)       ' (une ligne de la table sans coordonnees n'est pas proposee ; « Netanya2 » reste proposee, a la demande de Jacob)
        Next
        Return l
    End Function

    Private Async Sub MettreAJour(ville As String)
        If _enCours.Contains(ville) Then Exit Sub
        _enCours.Add(ville)
        Try
            Dim lat As Double, lon As Double
            If Not CoordonneesDe(ville, lat, lon) Then
                DiagLog("Meteo : ville « " & ville & " » inconnue dans la table des villes")
                _prochaines(ville) = DateTime.Now.AddMinutes(5)
                Exit Sub
            End If
            Dim url As String = "https://api.open-meteo.com/v1/forecast?latitude=" & lat.ToString("0.####", CultureInfo.InvariantCulture) &
                                "&longitude=" & lon.ToString("0.####", CultureInfo.InvariantCulture) &
                                "&current=temperature_2m,weather_code,is_day&hourly=temperature_2m,weather_code,is_day,precipitation_probability&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=auto&forecast_days=8"
            Dim texte As String = Await _http.GetStringAsync(url)
            Dim d As Donnees = Await Task.Run(Function() Analyser(texte, ville))
            _cache(ville) = d
            _prochaines(ville) = DateTime.Now.AddMinutes(30)
            _signature = ""
            DiagLog("Meteo : " & ville & " " & d.TempActuelle.ToString("0.#", CultureInfo.InvariantCulture) & " C, " & d.Heures.Count & " heures lues")
            Rafraichir()
        Catch ex As Exception
            DiagLog("Meteo : lecture impossible pour " & ville & " (" & ex.Message & ")")
            _prochaines(ville) = DateTime.Now.AddMinutes(5)
        Finally
            _enCours.Remove(ville)
        End Try
    End Sub

    Private Function Analyser(json As String, ville As String) As Donnees
        Dim racine As JObject = JObject.Parse(json)
        Dim d As New Donnees With {.Ville = ville}
        Dim cur As JToken = racine("current")
        d.Maintenant = DateTime.Parse(cur("time").ToString(), CultureInfo.InvariantCulture)
        d.LueLe = DateTime.Now
        d.TempActuelle = cur("temperature_2m").Value(Of Double)()
        d.CodeActuel = cur("weather_code").Value(Of Integer)()
        d.JourActuel = cur("is_day").Value(Of Integer)() = 1
        Dim h As JToken = racine("hourly")
        Dim temps As JArray = CType(h("time"), JArray)
        Dim t2 As JArray = CType(h("temperature_2m"), JArray)
        Dim codes As JArray = CType(h("weather_code"), JArray)
        Dim jours As JArray = CType(h("is_day"), JArray)
        Dim pluies As JArray = TryCast(h("precipitation_probability"), JArray)
        For i As Integer = 0 To temps.Count - 1
            Dim p As New PointHoraire With {.Heure = DateTime.Parse(temps(i).ToString(), CultureInfo.InvariantCulture)}
            If t2(i).Type = JTokenType.Null Then Continue For
            p.Temp = t2(i).Value(Of Double)()
            p.Code = If(codes(i).Type = JTokenType.Null, 0, codes(i).Value(Of Integer)())
            p.Jour = (jours(i).Type <> JTokenType.Null AndAlso jours(i).Value(Of Integer)() = 1)
            p.Pluie = If(pluies IsNot Nothing AndAlso pluies(i).Type <> JTokenType.Null, pluies(i).Value(Of Integer)(), 0)
            d.Heures.Add(p)
        Next
        ' Les previsions par jour (aujourd'hui et les 7 suivants).
        Dim jr As JToken = racine("daily")
        If jr IsNot Nothing Then
            Dim dates As JArray = TryCast(jr("time"), JArray)
            Dim codesJ As JArray = TryCast(jr("weather_code"), JArray)
            Dim maxis As JArray = TryCast(jr("temperature_2m_max"), JArray)
            Dim minis As JArray = TryCast(jr("temperature_2m_min"), JArray)
            Dim pluiesJ As JArray = TryCast(jr("precipitation_probability_max"), JArray)
            If dates IsNot Nothing AndAlso codesJ IsNot Nothing AndAlso maxis IsNot Nothing AndAlso minis IsNot Nothing Then
                For i As Integer = 0 To dates.Count - 1
                    If maxis(i).Type = JTokenType.Null OrElse minis(i).Type = JTokenType.Null Then Continue For
                    d.Jours.Add(New PointHoraire With {
                        .Heure = DateTime.Parse(dates(i).ToString(), CultureInfo.InvariantCulture),
                        .Temp = maxis(i).Value(Of Double)(),
                        .Min = minis(i).Value(Of Double)(),
                        .AMin = True,
                        .Code = If(codesJ(i).Type = JTokenType.Null, 0, codesJ(i).Value(Of Integer)()),
                        .Jour = True,
                        .Pluie = If(pluiesJ IsNot Nothing AndAlso pluiesJ(i).Type <> JTokenType.Null, pluiesJ(i).Value(Of Integer)(), 0)})
                Next
            End If
        End If
        Return d
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Affichage (fenetre calque)
    ' ------------------------------------------------------------------------------------------

    Private _calque As FormCalqueImage
    Private _signature As String = ""
    Private _zone As Rectangle
    Private _carte As Size
    Private _marge As Integer
    Private WithEvents _minuteur As New System.Windows.Forms.Timer() With {.Interval = 1000}
    Private WithEvents _minuteurTaille As New System.Windows.Forms.Timer() With {.Interval = 600}
    Private _demarre As Boolean

    ''' <summary>A appeler une fois, depuis le fil de l'interface (chargement de la fenetre de Slide).</summary>
    Friend Sub Demarrer()
        If _demarre Then Exit Sub
        _demarre = True
        _dernierChangementMode = DateTime.Now          ' (« uniquement au changement » : la carte se montre aussi au lancement, le temps choisi)
        _minuteur.Start()
    End Sub

    Private Sub Minuteur_Tick(sender As Object, e As EventArgs) Handles _minuteur.Tick
        Rafraichir()
    End Sub

    Private Function PourcentActuel() As Integer
        Return Math.Max(PourcentMini, Math.Min(PourcentMaxi, If(_pourcentALaSouris, Reglages().Pourcent)))
    End Function

    ''' <summary>Vrai quand une fenetre de travail de Slide (options, Nflix, listes...) est la fenetre active : la carte s'efface, comme l'icone de la source de musique.</summary>
    Private Function UneFenetreDeTravailEstActive(principale As Form) As Boolean
        Try
            Dim f As Form = Form.ActiveForm
            If f Is Nothing OrElse f.IsDisposed OrElse ReferenceEquals(f, principale) OrElse TypeOf f Is FormCalqueImage Then Return False
            Return f.Modal OrElse f.ShowInTaskbar
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' --- Affichage temporaire au changement photo / video ---------------------------------------
    Private _dernierModeVideo As Boolean?
    Private _dernierChangementMode As DateTime = DateTime.MinValue
    ''' <summary>La duree maximale de l'affichage temporaire : la plus courte des durees d'une photo et d'une video (reglages Duree photo / Duree video), au moins 2 s.</summary>
    Friend Function SecondesTemporaireMax() As Integer
        Dim p As Integer = CInt(Val(DurréePhoto)), v As Integer = CInt(Val(DurréeVideo))
        If p <= 0 Then p = 600
        If v <= 0 Then v = 600
        Return Math.Max(2, Math.Min(600, Math.Min(p, v)))
    End Function

    ''' <summary>A appeler a chaque image montree (photo : estVideo = False ; video : True) : un changement de mode relance l'affichage temporaire de la carte.</summary>
    Friend Sub NoterLeMode(estVideo As Boolean)
        If _dernierModeVideo.HasValue AndAlso _dernierModeVideo.Value <> estVideo Then
            _dernierChangementMode = DateTime.Now
            _signature = ""
            If Reglages().Temporaire Then DiagLog("Meteo : passage " & If(estVideo, "photo -> video", "video -> photo") & ", carte montree " & Math.Min(Reglages().SecondesTemporaire, SecondesTemporaireMax()) & " s")
        End If
        _dernierModeVideo = estVideo
    End Sub

    ''' <summary>Vrai : la carte doit etre cachee maintenant (option « au changement photo/video » cochee, diaporama mixte, delai ecoule).</summary>
    Private Function CacheeEntreLesChangements(r As ReglagesMeteo) As Boolean
        If Not r.Temporaire Then Return False
        If onlyphotos OrElse onlyvideos Then Return False                    ' (reglages « Seulement photos » / « Seulement videos » : la carte reste fixe)
        If _dernierChangementMode = DateTime.MinValue Then Return True        ' (« uniquement au changement » : cachee tant qu'aucun passage photo/video n'a eu lieu)
        Dim ecoule As Double = (DateTime.Now - _dernierChangementMode).TotalSeconds
        Return ecoule > Math.Min(r.SecondesTemporaire, SecondesTemporaireMax())
    End Function

    Private Sub Cacher()
        _signature = ""
        If _calque IsNot Nothing AndAlso Not _calque.IsDisposed AndAlso _calque.Visible Then _calque.Hide()
    End Sub

    ''' <summary>La meteo de la ville a montrer maintenant : la ville de la rotation si on l'a lue, sinon la suivante qui l'est (Nothing : aucune lue pour l'instant).</summary>
    Private Function DonneesCourantes(r As ReglagesMeteo) As Donnees
        Dim villes As List(Of String) = ListeDesVilles(r)
        If villes.Count = 0 Then Return Nothing
        If _indexVille >= villes.Count Then _indexVille = 0
        For k As Integer = 0 To villes.Count - 1
            Dim i As Integer = (_indexVille + k) Mod villes.Count
            Dim d As Donnees = Nothing
            If _cache.TryGetValue(villes(i), d) Then
                _indexVille = i
                Return d
            End If
        Next
        Return Nothing
    End Function

    ''' <summary>Passe a la ville suivante (rotation, ou clic sur la carte).</summary>
    Private Sub VilleSuivante()
        Dim n As Integer = ListeDesVilles(Reglages()).Count
        If n <= 1 Then Exit Sub
        _indexVille = (_indexVille + 1) Mod n
        _dernierChangement = DateTime.Now
        _signature = ""
    End Sub

    ''' <summary>Montre, met a jour ou cache la carte selon les reglages et l'etat de Slide.</summary>
    Friend Sub Rafraichir()
        Try
            Dim r As ReglagesMeteo = Reglages()
            Dim principale As Form = FenetrePrincipale()
            If Not r.Actif OrElse principale Is Nothing OrElse principale.IsDisposed OrElse Not principale.Visible OrElse principale.WindowState = FormWindowState.Minimized Then
                Cacher()
                Exit Sub
            End If
            ' Une meteo par ville choisie : celles qui n'ont pas encore ete lues, ou qui datent de plus de 30 minutes, sont (re)lues.
            Dim villesActives As List(Of String) = ListeDesVilles(r).ToList()
            For Each v As String In villesActives
                Dim echeance As DateTime
                If (Not _prochaines.TryGetValue(v, echeance) OrElse DateTime.Now >= echeance) AndAlso Not _enCours.Contains(v) Then MettreAJour(v)
            Next
            ' Plusieurs villes : la carte passe de l'une a l'autre toutes les r.Rotation secondes (jamais pendant un geste a la souris).
            If villesActives.Count > 1 AndAlso Not _calquePris AndAlso (DateTime.Now - _dernierChangement).TotalSeconds >= r.Rotation Then VilleSuivante()
            Dim donnees As Donnees = DonneesCourantes(r)
            If donnees Is Nothing Then Exit Sub
            If UneFenetreDeTravailEstActive(principale) OrElse (Not _calquePris AndAlso CacheeEntreLesChangements(r)) Then
                Cacher()
                Exit Sub
            End If
            Dim zone As Rectangle = principale.RectangleToScreen(principale.ClientRectangle)
            If zone.Width < 200 OrElse zone.Height < 150 Then
                Cacher()
                Exit Sub
            End If
            If _calquePris AndAlso Not _enRedimension Then Exit Sub         ' (pendant un deplacement a la souris, on ne touche a rien)
            Dim pct As Integer = PourcentActuel()
            Dim heureLue As String = donnees.MaintenantDeLaVille().ToString("yyyyMMddHH")
            Dim signature As String = String.Join("|", zone.ToString(), pct, r.Pas, r.Nombre, r.Jours, r.AfficherHeures, r.AfficherJours, r.Transparence, r.Temporaire, r.Disposition, donnees.Ville, heureLue, donnees.TempActuelle.ToString("0.0", CultureInfo.InvariantCulture),
                                                  r.X.ToString("0.###", CultureInfo.InvariantCulture), r.Y.ToString("0.###", CultureInfo.InvariantCulture), FullScreen, Date.Now.Hour)
            If _calque IsNot Nothing AndAlso Not _calque.IsDisposed AndAlso _calque.Visible AndAlso signature = _signature Then Exit Sub

            Dim u As Single = Math.Max(3.0F, CSng(zone.Height / 48.0 * pct / 100.0))
            Dim carteDessinee As Bitmap = Dessiner(donnees, r, u)
            ' La carte ne doit jamais depasser la fenetre : trop grande (beaucoup de cases, grande taille), elle est redessinee plus petite.
            For essai As Integer = 1 To 4
                Dim m As Single = -u * 0.35F          ' (pas de marge : la carte peut remplir la fenetre, seule son ombre transparente deborde)
                Dim placeL As Single = zone.Width - 2 * m
                Dim placeH As Single = zone.Height - 2 * m
                If u <= 3.0F OrElse (carteDessinee.Width <= placeL AndAlso carteDessinee.Height <= placeH) Then Exit For
                Dim facteur As Single = Math.Min(placeL / carteDessinee.Width, placeH / carteDessinee.Height) * 0.98F
                u = Math.Max(3.0F, u * facteur)
                carteDessinee.Dispose()
                carteDessinee = Dessiner(donnees, r, u)
            Next
            Using image As Bitmap = carteDessinee
                If _calque Is Nothing OrElse _calque.IsDisposed Then
                    _calque = New FormCalqueImage()
                    _calque.Cursor = Cursors.SizeAll
                    AddHandler _calque.MouseDown, AddressOf Souris_Down
                    AddHandler _calque.MouseMove, AddressOf Souris_Move
                    AddHandler _calque.MouseUp, AddressOf Souris_Up
                    AddHandler _calque.MouseWheel, AddressOf Souris_Molette
                    AddHandler _calque.MouseDoubleClick, AddressOf Souris_DoubleClic
                End If
                _zone = zone
                _carte = image.Size
                _u = u
                _marge = -CInt(Math.Ceiling(u * 0.35F))          ' (aucune marge : la carte peut toucher le bord ; l'image deborde de l'ombre transparente, 0,35 u)
                Dim x As Integer = zone.Left + _marge + CInt(r.X * Math.Max(0, zone.Width - image.Width - 2 * _marge))
                Dim y As Integer = zone.Top + _marge + CInt(r.Y * Math.Max(0, zone.Height - image.Height - 2 * _marge))
                If _enRedimension Then x = _ancre.X : y = _ancre.Y            ' (taille changee a la souris : le coin haut gauche reste en place)
                _calque.Montrer(image, New Point(x, y), principale, FullScreen)
            End Using
            _signature = signature
        Catch ex As Exception
            DiagLog("Meteo : affichage impossible (" & ex.Message & ")")
        End Try
    End Sub

    ' --- Souris -------------------------------------------------------------------------------

    ' Comme l'icone de la source de musique : cliquer-glisser au CENTRE = deplacer ; Ctrl + glisser, ou cliquer-glisser dans le COIN BAS DROIT (poignee) = la taille
    ' (tirer vers la droite agrandit, vers la gauche reduit ; le coin haut gauche reste en place). La molette change aussi la taille.
    Private _calquePris As Boolean
    Private _enRedimension As Boolean
    Private _ancre As Point
    Private _pointeurDepart As Point
    Private _placeDepart As Point
    Private _departPourcent As Integer
    Private _departLargeur As Integer
    Private _u As Single

    ''' <summary>Vrai si le pointeur est dans la poignee de taille (coin bas droit de la carte) ou si Ctrl est enfonce.</summary>
    Private Function GesteDeTaille(local As Point) As Boolean
        If (Control.ModifierKeys And Keys.Control) = Keys.Control Then Return True
        If _carte.Width <= 0 Then Return False
        Return local.X >= _carte.Width - CInt(_u * 3.2F) AndAlso local.Y >= _carte.Height - CInt(_u * 2.2F)
    End Function

    Private Sub Souris_Down(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Exit Sub
        _calquePris = True
        _pointeurDepart = Cursor.Position
        _placeDepart = _calque.Location
        If GesteDeTaille(e.Location) Then
            _enRedimension = True
            _ancre = _calque.Location
            _departPourcent = PourcentActuel()
            _departLargeur = Math.Max(1, _carte.Width)
        End If
    End Sub

    Private Sub Souris_Move(sender As Object, e As MouseEventArgs)
        If Not _calquePris Then
            ' Le pointeur annonce le geste : double fleche en diagonale dans la poignee (ou avec Ctrl), croix de deplacement ailleurs.
            Dim voulu As Cursor = If(GesteDeTaille(e.Location), Cursors.SizeNWSE, Cursors.SizeAll)
            If _calque IsNot Nothing AndAlso _calque.Cursor IsNot voulu Then _calque.Cursor = voulu
            Exit Sub
        End If
        If _enRedimension Then
            Dim dx As Integer = Cursor.Position.X - _pointeurDepart.X
            Dim pct As Integer = CInt(Math.Round(_departPourcent * (_departLargeur + dx) / CDbl(_departLargeur)))
            pct = Math.Max(PourcentMini, Math.Min(PourcentMaxi, pct))
            If pct <> PourcentActuel() Then
                _pourcentALaSouris = pct
                _signature = ""
                Rafraichir()
            End If
            Exit Sub
        End If
        Dim p As Point = Cursor.Position
        Dim x As Integer = _placeDepart.X + p.X - _pointeurDepart.X
        Dim y As Integer = _placeDepart.Y + p.Y - _pointeurDepart.Y
        x = Math.Max(_zone.Left + _marge, Math.Min(x, _zone.Right - _carte.Width - _marge))
        y = Math.Max(_zone.Top + _marge, Math.Min(y, _zone.Bottom - _carte.Height - _marge))
        _calque.Location = New Point(x, y)
    End Sub

    Private Sub Souris_Up(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Right Then
            Dim principale As Form = FenetrePrincipale()
            If principale IsNot Nothing Then principale.ContextMenuStrip?.Show(Cursor.Position)
            Exit Sub
        End If
        If Not _calquePris Then Exit Sub
        _calquePris = False
        ' Un simple clic (le pointeur n'a pas bouge) : ville suivante, quand plusieurs villes sont choisies.
        If Not _enRedimension AndAlso Math.Abs(Cursor.Position.X - _pointeurDepart.X) + Math.Abs(Cursor.Position.Y - _pointeurDepart.Y) < 4 Then
            VilleSuivante()
            Rafraichir()
            Exit Sub
        End If
        If _enRedimension Then
            _enRedimension = False
            If _pourcentALaSouris.HasValue Then Reglages().Pourcent = _pourcentALaSouris.Value
            _pourcentALaSouris = Nothing
        End If
        ' Place enregistree en fraction de la course : elle suit l'image de Slide si elle change de taille (plein ecran...).
        Dim largeur As Integer = Math.Max(1, _zone.Width - _carte.Width - 2 * _marge)
        Dim hauteur As Integer = Math.Max(1, _zone.Height - _carte.Height - 2 * _marge)
        Dim r As ReglagesMeteo = Reglages()
        r.X = Math.Max(0.0, Math.Min(1.0, (_calque.Left - _zone.Left - _marge) / CDbl(largeur)))
        r.Y = Math.Max(0.0, Math.Min(1.0, (_calque.Top - _zone.Top - _marge) / CDbl(hauteur)))
        Enregistrer(r)
    End Sub

    Private Sub Souris_Molette(sender As Object, e As MouseEventArgs)
        _pourcentALaSouris = Math.Max(PourcentMini, Math.Min(PourcentMaxi, PourcentActuel() + Math.Sign(e.Delta) * 5))
        _signature = ""
        Rafraichir()
        _minuteurTaille.Stop()
        _minuteurTaille.Start()
    End Sub

    Private Sub MinuteurTaille_Tick(sender As Object, e As EventArgs) Handles _minuteurTaille.Tick
        _minuteurTaille.Stop()
        If _pourcentALaSouris.HasValue Then
            Dim r As ReglagesMeteo = Reglages()
            r.Pourcent = _pourcentALaSouris.Value
            _pourcentALaSouris = Nothing
            Enregistrer(r)
        End If
    End Sub

    Private Sub Souris_DoubleClic(sender As Object, e As MouseEventArgs)
        OuvrirLesReglages()
    End Sub

    ''' <summary>Les reglages de la meteo : fenetre Options, onglet Divers, cadre « Meteo du jour sur l'image » (double-clic sur la carte).</summary>
    Friend Sub OuvrirLesReglages()
        Try
            Form4.TabControl1.SelectedTab = Form4.TabPage6
            Form4.StartPosition = FormStartPosition.CenterScreen
            Form4.ShowDialog()
        Catch ex As Exception
            DiagLog("Meteo : fenetre des options impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Menu du clic droit > Meteo : affiche ou cache la carte (les autres reglages sont dans Options > Divers).</summary>
    Friend Sub Basculer()
        Dim r As ReglagesMeteo = Reglages()
        r.Actif = Not r.Actif
        Enregistrer(r)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Dessin : une carte de verre sombre, a gauche le temps actuel, puis une colonne par heure
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Dessin de la carte avec des donnees FICTIVES (tous les types de temps) : sert a verifier le rendu sans reseau ni fenetre.</summary>
    Friend Function ApercuDeTest(u As Single, nuit As Boolean, nombre As Integer, disposition As Integer, jours As Integer) As Bitmap
        Dim d As New Donnees With {.Ville = "Paris", .Maintenant = New DateTime(2026, 11, 12, 14, 20, 0), .TempActuelle = 13.4, .CodeActuel = 2, .JourActuel = Not nuit}
        Dim codes As Integer() = {2, 3, 61, 95, 71, 45, 0, 1, 80}
        Dim temps As Double() = {13.4, 12, 9.6, 6.1, 2.4, -1.2, 21.5, 29.8, 14}
        For k As Integer = 0 To 20
            d.Heures.Add(New PointHoraire With {.Heure = New DateTime(2026, 11, 12, 14, 0, 0).AddHours(k), .Temp = temps(k Mod temps.Length), .Code = codes(k Mod codes.Length), .Jour = Not nuit,
                                                .Pluie = If(codes(k Mod codes.Length) >= 61, 40 + 10 * (k Mod 4), 0)})
        Next
        Dim maxis As Double() = {12, 14, 9, 7, 16, 18, 8, 11}
        For k As Integer = 0 To 7
            d.Jours.Add(New PointHoraire With {.Heure = New DateTime(2026, 11, 12).AddDays(k), .Temp = maxis(k), .Min = maxis(k) - 6 - (k Mod 3), .AMin = True, .Code = codes((k + 1) Mod codes.Length), .Jour = True,
                                               .Pluie = If(codes((k + 1) Mod codes.Length) >= 61, 55, 0)})
        Next
        Return Dessiner(d, New ReglagesMeteo With {.Pas = 1, .Nombre = nombre, .Disposition = disposition, .Jours = jours}, u)
    End Function

    Private Function PoliceDe(taille As Single, Optional style As FontStyle = FontStyle.Regular) As Font
        Return New Font("Segoe UI", Math.Max(5.0F, taille), style, GraphicsUnit.Pixel)
    End Function

    ''' <summary>La plus grande police (au plus <paramref name="maxi"/>, au moins <paramref name="mini"/>) qui fait tenir <paramref name="texte"/> dans <paramref name="largeur"/> : un nom court est grand, un nom long se reduit.</summary>
    Private Function PoliceAjustee(g As Graphics, texte As String, maxi As Single, mini As Single, largeur As Single, style As FontStyle) As Font
        Dim taille As Single = maxi
        Do
            Dim f As Font = PoliceDe(taille, style)
            If taille <= mini + 0.01F OrElse g.MeasureString(texte, f, 10000, StringFormat.GenericTypographic).Width <= largeur Then Return f
            f.Dispose()
            taille -= Math.Max(0.5F, maxi * 0.04F)
        Loop
    End Function

    Private Function CouleurTemperature(t As Double) As Color
        ' froid : bleu glace ; doux : blanc ; chaud : orange (degrade continu)
        Dim froid As Color = Color.FromArgb(140, 200, 255)
        Dim neutre As Color = Color.FromArgb(250, 250, 252)
        Dim chaud As Color = Color.FromArgb(255, 168, 84)
        If t <= 12 Then Return Melanger(froid, neutre, Math.Max(0.0, Math.Min(1.0, (t - 0) / 12.0)))
        If t >= 22 Then Return Melanger(neutre, chaud, Math.Max(0.0, Math.Min(1.0, (t - 22) / 10.0)))
        Return neutre
    End Function

    Private Function Melanger(a As Color, b As Color, f As Double) As Color
        Return Color.FromArgb(CInt(CInt(a.R) + (CInt(b.R) - CInt(a.R)) * f), CInt(CInt(a.G) + (CInt(b.G) - CInt(a.G)) * f), CInt(CInt(a.B) + (CInt(b.B) - CInt(a.B)) * f))
    End Function

    Private Function CheminRond(r As RectangleF, rayon As Single) As GraphicsPath
        Dim p As New GraphicsPath()
        Dim d As Single = rayon * 2
        p.AddArc(r.X, r.Y, d, d, 180, 90)
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function

    ''' <summary>Les trois dispositions : 0 horizontale (le temps actuel puis une colonne par heure), 1 verticale (le temps actuel en haut puis une ligne par heure),
    ''' 2 rectangulaire (le temps actuel a gauche puis les heures en grille sur deux rangees). Meme fond, memes icones, memes couleurs. Apres les heures, une case PAR JOUR
    ''' (r.Jours cases, a partir de demain : icone, maxi, mini) separee par un filet.</summary>
    Private Function Dessiner(d As Donnees, r As ReglagesMeteo, u As Single) As Bitmap
        ' Les cases : a partir de l'heure en cours, toutes les r.Pas heures, r.Nombre cases (sans depasser les donnees).
        Dim ici As DateTime = d.MaintenantDeLaVille()
        Dim debut As DateTime = New DateTime(ici.Year, ici.Month, ici.Day, ici.Hour, 0, 0)
        Dim cases As New List(Of PointHoraire)
        _opaciteFond = 1.0F - Math.Max(0, Math.Min(99, r.Transparence)) / 100.0F
        Dim voirHeures As Boolean = r.AfficherHeures OrElse Not r.AfficherJours OrElse r.Jours = 0
        For k As Integer = 0 To If(voirHeures, r.Nombre, 0) - 1
            Dim voulu As DateTime = debut.AddHours(k * r.Pas)
            Dim p As PointHoraire = d.Heures.FirstOrDefault(Function(x) x.Heure = voulu)
            If p Is Nothing Then Exit For
            cases.Add(p)
        Next
        ' Les jours : a partir de DEMAIN (aujourd'hui est deja dans les heures).
        Dim jours As New List(Of PointHoraire)
        If r.Jours > 0 AndAlso r.AfficherJours Then jours = d.Jours.Where(Function(x) x.Heure.Date > ici.Date).Take(r.Jours).ToList()
        For Each j As PointHoraire In jours
            j.Etiquette = EtiquetteDuJour(j.Heure, ici)
        Next
        Dim nJ As Integer = jours.Count

        Dim marge As Single = u * 0.55F
        Dim largeurActuel As Single = u * 5.5F
        Dim largeurCol As Single = u * 2.15F
        Dim hauteurCol As Single = u * 4.1F                 ' (une colonne seule, disposition horizontale)
        Dim hauteurCase As Single = u * 4.1F                ' (une case de la grille rectangulaire)
        ' Disposition verticale : la colonne « % de pluie » n'existe que s'il y a de la pluie a annoncer (sinon plus d'espace vide a droite) ; les lignes des jours ont besoin de la place de la mini.
        Dim aDeLaPluie As Boolean = cases.Any(Function(c) c.Pluie >= 30) OrElse jours.Any(Function(c) c.Pluie >= 30)
        Dim largeurLigne As Single = If(nJ > 0, If(aDeLaPluie, u * 9.6F, u * 8.0F), If(aDeLaPluie, u * 8.4F, u * 6.9F))
        Dim largeurActuelRect As Single = u * 4.3F          ' (rectangulaire : le temps actuel est empile, icone puis temperature : bloc plus etroit)
        Dim hauteurLigne As Single = u * 1.75F
        Dim hauteurEntete As Single = u * 4.0F
        Dim padDroit As Single = u * 0.5F                   ' (la poignee de taille du coin bas droit ne doit rien toucher)
        Dim ombre As Single = u * 0.35F
        Dim ecartJours As Single = u * 0.6F                 ' (horizontale : espace entre les heures et les jours)
        Dim ecartJoursV As Single = u * 0.5F                ' (verticale : espace avant les lignes des jours)

        Dim largeur As Single, hauteur As Single
        Dim colonnesGrille As Integer = Math.Max(1, CInt(Math.Ceiling(cases.Count / 2.0)))
        Dim colonnesRect As Integer = Math.Max(colonnesGrille, nJ)
        Dim rangeesRect As Integer = If(nJ > 0, If(cases.Count > 0, 3, 1), 2)
        Select Case r.Disposition
            Case 1      ' verticale
                largeur = marge * 2 + padDroit + largeurLigne
                hauteur = marge * 0.7F + hauteurEntete + u * 0.35F + hauteurLigne * cases.Count + If(nJ > 0, If(cases.Count > 0, ecartJoursV, 0.0F) + hauteurLigne * nJ, 0.0F) + marge * 0.9F
            Case 2      ' rectangulaire
                largeur = marge * 2 + padDroit + largeurActuelRect + largeurCol * colonnesRect
                hauteur = marge * 0.5F + hauteurCase * rangeesRect + marge * 0.5F
            Case Else   ' horizontale
                largeur = marge * 2 + padDroit + largeurActuel + largeurCol * cases.Count + If(nJ > 0, If(cases.Count > 0, ecartJours, 0.0F) + largeurCol * nJ, 0.0F)
                hauteur = hauteurCol
        End Select

        Dim bmp As New Bitmap(CInt(Math.Ceiling(largeur + ombre * 2)), CInt(Math.Ceiling(hauteur + ombre * 2)), Imaging.PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.Clear(Color.FromArgb(0, 0, 0, 0))
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.HighQuality

            Dim carte As New RectangleF(ombre, ombre, largeur, hauteur)
            DessinerLeFond(g, carte, u)

            Select Case r.Disposition
                Case 1
                    Dim x0 As Single = carte.X + marge
                    DessinerLeTempsActuel(g, d, New RectangleF(x0, carte.Y + marge * 0.7F, largeurLigne, hauteurEntete), u, 1.0F, False, True)
                    Dim yFilet As Single = carte.Y + marge * 0.7F + hauteurEntete
                    TracerUnFilet(g, x0, yFilet, x0 + largeurLigne, yFilet, u)
                    Dim y0 As Single = yFilet + u * 0.3F
                    For i As Integer = 0 To cases.Count - 1
                        DessinerUneLigne(g, cases(i), i, New RectangleF(x0, y0 + hauteurLigne * i, largeurLigne, hauteurLigne), u)
                    Next
                    If nJ > 0 Then
                        Dim yJours As Single = y0 + hauteurLigne * cases.Count
                        Dim ecV As Single = If(cases.Count > 0, ecartJoursV, 0.0F)
                        If cases.Count > 0 Then TracerUnFilet(g, x0, yJours + ecV * 0.3F, x0 + largeurLigne, yJours + ecV * 0.3F, u)
                        For i As Integer = 0 To nJ - 1
                            DessinerUneLigne(g, jours(i), 100 + i, New RectangleF(x0, yJours + ecV + hauteurLigne * i, largeurLigne, hauteurLigne), u)
                        Next
                    End If
                Case 2
                    Dim yHaut As Single = carte.Y + marge * 0.5F
                    Dim xGrille As Single = carte.X + marge + largeurActuelRect
                    DessinerLeTempsActuel(g, d, New RectangleF(carte.X + marge, yHaut, largeurActuelRect, hauteurCase * rangeesRect), u, 1.0F, True)
                    Dim xSep As Single = xGrille - u * 0.1F
                    TracerUnFilet(g, xSep, yHaut + hauteurCase * 0.2F, xSep, yHaut + hauteurCase * (rangeesRect - 0.2F), u)
                    For i As Integer = 0 To cases.Count - 1
                        Dim rangee As Integer = i \ colonnesGrille
                        Dim col As Integer = i Mod colonnesGrille
                        DessinerUneColonne(g, cases(i), i, New RectangleF(xGrille + largeurCol * col, yHaut + hauteurCase * rangee, largeurCol, hauteurCase), u)
                    Next
                    If nJ > 0 Then
                        Dim yFiletJours As Single = yHaut + If(cases.Count > 0, hauteurCase * 2, 0.0F)
                        If cases.Count > 0 Then TracerUnFilet(g, xGrille + u * 0.2F, yFiletJours, xGrille + largeurCol * colonnesRect - u * 0.2F, yFiletJours, u)
                        For i As Integer = 0 To nJ - 1
                            DessinerUneColonne(g, jours(i), 100 + i, New RectangleF(xGrille + largeurCol * i, yFiletJours, largeurCol, hauteurCase), u)
                        Next
                    End If
                Case Else
                    DessinerLeTempsActuel(g, d, New RectangleF(carte.X + marge, carte.Y, largeurActuel, hauteur), u, 1.0F, False, True)
                    Dim xSep As Single = carte.X + marge + largeurActuel - u * 0.1F
                    TracerUnFilet(g, xSep, carte.Y + hauteur * 0.2F, xSep, carte.Bottom - hauteur * 0.2F, u)
                    For i As Integer = 0 To cases.Count - 1
                        DessinerUneColonne(g, cases(i), i, New RectangleF(carte.X + marge + largeurActuel + largeurCol * i, carte.Y, largeurCol, hauteur), u)
                    Next
                    If nJ > 0 Then
                        Dim ecJ As Single = If(cases.Count > 0, ecartJours, 0.0F)
                        Dim xJours As Single = carte.X + marge + largeurActuel + largeurCol * cases.Count + ecJ
                        If cases.Count > 0 Then TracerUnFilet(g, xJours - ecJ / 2.0F, carte.Y + hauteur * 0.2F, xJours - ecJ / 2.0F, carte.Bottom - hauteur * 0.2F, u)
                        For i As Integer = 0 To nJ - 1
                            DessinerUneColonne(g, jours(i), 100 + i, New RectangleF(xJours + largeurCol * i, carte.Y, largeurCol, hauteur), u)
                        Next
                    End If
            End Select

            ' --- Poignee de taille, coin bas droit (trois petits traits en diagonale, comme le coin d'une fenetre) : tirer vers la droite agrandit, vers la gauche reduit.
            Dim bx As Single = carte.Right - u * 0.3F
            Dim by As Single = carte.Bottom - u * 0.3F
            Using stylo As New Pen(Color.FromArgb(120, 205, 220, 245), Math.Max(1.2F, u * 0.075F)) With {.StartCap = LineCap.Round, .EndCap = LineCap.Round}
                For Each o As Single In {0.3F, 0.56F, 0.82F}
                    g.DrawLine(stylo, bx - u * o, by, bx, by - u * o)
                Next
            End Using
        End Using
        Return bmp
    End Function

    ''' <summary>Verre sombre : ombre douce, degrade bleu nuit translucide, reflet en haut, fin liseré.</summary>
    Private _opaciteFond As Single = 1.0F

    Private Function AlphaFond(a As Integer) As Integer
        Return Math.Max(0, Math.Min(255, CInt(a * _opaciteFond)))
    End Function

    Private Sub DessinerLeFond(g As Graphics, carte As RectangleF, u As Single)
        Dim rayon As Single = u * 1.0F
        For i As Integer = 4 To 1 Step -1
            Using chemin As GraphicsPath = CheminRond(RectangleF.Inflate(carte, i * u * 0.07F, i * u * 0.07F), rayon + i * u * 0.07F)
                Using pinceau As New SolidBrush(Color.FromArgb(AlphaFond(14), 0, 0, 0))
                    g.FillPath(pinceau, chemin)
                End Using
            End Using
        Next
        Using chemin As GraphicsPath = CheminRond(carte, rayon)
            Using degrade As New LinearGradientBrush(carte, Color.FromArgb(AlphaFond(196), 30, 38, 58), Color.FromArgb(AlphaFond(206), 14, 18, 30), 90.0F)
                g.FillPath(degrade, chemin)
            End Using
            g.SetClip(chemin)
            ' (le pinceau est plus haut que la zone remplie : le degrade finit transparent, sans filet visible a la jonction)
            Using reflet As New LinearGradientBrush(New RectangleF(carte.X, carte.Y - 1, carte.Width, carte.Height * 0.55F + 2), Color.FromArgb(40, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), 90.0F)
                g.FillRectangle(reflet, carte.X, carte.Y, carte.Width, carte.Height * 0.5F)
            End Using
            g.ResetClip()
            Using stylo As New Pen(Color.FromArgb(70, 255, 255, 255), Math.Max(1.0F, u * 0.05F))
                g.DrawPath(stylo, chemin)
            End Using
        End Using
    End Sub

    Private Sub TracerUnFilet(g As Graphics, x1 As Single, y1 As Single, x2 As Single, y2 As Single, u As Single)
        Using stylo As New Pen(Color.FromArgb(48, 255, 255, 255), Math.Max(1.0F, u * 0.04F))
            g.DrawLine(stylo, x1, y1, x2, y2)
        End Using
    End Sub

    Private ReadOnly _gris As Color = Color.FromArgb(170, 190, 205, 230)

    ''' <summary>Le temps actuel : icone, grande temperature, nom de la ville dessous. <paramref name="echelleVerticale"/> : part de la hauteur de la zone ou se place le bloc (0,8 = zone haute, rangees de la grille).</summary>
    Private Sub DessinerLeTempsActuel(g As Graphics, d As Donnees, zone As RectangleF, u As Single, echelleVerticale As Single, Optional empile As Boolean = False, Optional villeCentree As Boolean = False)
        If empile Then
            ' Disposition rectangulaire : l'icone en haut, la grande temperature SOUS l'icone, la ville ALIGNEE EN BAS du bloc (sur la derniere ligne des cases).
            Dim hz As Single = zone.Height
            Dim cx As Single = zone.X + zone.Width / 2.0F
            Dim fmtEmpile As New StringFormat(StringFormatFlags.NoWrap) With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center, .Trimming = StringTrimming.EllipsisCharacter}
            DessinerIcone(g, d.CodeActuel, d.JourActuel, cx, zone.Y + hz * 0.3F, u * 2.5F)
            Using f As Font = PoliceDe(u * 1.7F, FontStyle.Bold), p As New SolidBrush(CouleurTemperature(d.TempActuelle))
                g.DrawString(Math.Round(d.TempActuelle).ToString("0", CultureInfo.InvariantCulture) & "°", f, p, New RectangleF(zone.X, zone.Y + hz * 0.43F, zone.Width, hz * 0.17F), fmtEmpile)
            End Using
            Using f As Font = PoliceAjustee(g, d.Ville, u * 1.1F, u * 0.6F, zone.Width - u * 0.4F, FontStyle.Regular), p As New SolidBrush(Color.FromArgb(205, 205, 218, 240))
                g.DrawString(d.Ville, f, p, New RectangleF(zone.X, zone.Y + hz * 0.82F, zone.Width, hz * 0.15F), fmtEmpile)
            End Using
            Exit Sub
        End If
        Dim h As Single = zone.Height
        Dim centre As Single = zone.Y + h * 0.5F                  ' (milieu de la zone)
        Dim haut As Single = centre - h * 0.5F * echelleVerticale
        Dim h2 As Single = h * echelleVerticale
        Dim texteTemp As String = Math.Round(d.TempActuelle).ToString("0", CultureInfo.InvariantCulture) & "°"
        Dim decalage As Single = 0.0F                              ' (verticale : l'icone et la temperature forment un groupe centre dans la largeur)
        If villeCentree Then
            Using fm As Font = PoliceDe(u * 1.45F, FontStyle.Bold)
                Dim largeurTexte As Single = g.MeasureString(texteTemp, fm, 10000, StringFormat.GenericTypographic).Width
                decalage = Math.Max(0.0F, (zone.Width - (u * 2.25F + largeurTexte)) / 2.0F)
            End Using
        End If
        DessinerIcone(g, d.CodeActuel, d.JourActuel, zone.X + decalage + u * 1.15F, haut + h2 * 0.43F, u * 2.1F)
        Using f As Font = PoliceDe(u * 1.45F, FontStyle.Bold), p As New SolidBrush(CouleurTemperature(d.TempActuelle))
            g.DrawString(texteTemp, f, p, New RectangleF(zone.X + decalage + u * 2.25F, haut + h2 * 0.14F, u * 3.2F, h2 * 0.5F),
                         New StringFormat(StringFormatFlags.NoWrap) With {.Alignment = StringAlignment.Near, .LineAlignment = StringAlignment.Center})
        End Using
        Using f As Font = PoliceAjustee(g, d.Ville, u * 0.95F, u * 0.6F, zone.Width - u * 0.5F, FontStyle.Regular), p As New SolidBrush(Color.FromArgb(205, 205, 218, 240))
            g.DrawString(d.Ville, f, p, New RectangleF(zone.X, haut + h2 * 0.73F, zone.Width, h2 * 0.22F),
                         New StringFormat With {.Alignment = If(villeCentree, StringAlignment.Center, StringAlignment.Near), .LineAlignment = StringAlignment.Center, .Trimming = StringTrimming.EllipsisCharacter, .FormatFlags = StringFormatFlags.NoWrap})
        End Using
    End Sub

    ''' <summary>Une case en colonne : l'heure (ou le jour) en haut, l'icone, la temperature, puis la probabilite de pluie (si 30 % ou plus) ; une case JOUR montre la mini en gris a la place de la pluie.</summary>
    Private Sub DessinerUneColonne(g As Graphics, p As PointHoraire, i As Integer, zone As RectangleF, u As Single)
        Dim cx As Single = zone.X + zone.Width / 2.0F
        Dim h As Single = zone.Height
        Dim fmt As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
        Dim etiquette As String = If(p.Etiquette <> "", p.Etiquette, If(i = 0, "Maint.", p.Heure.Hour.ToString("0", CultureInfo.InvariantCulture) & "h"))
        Using f As Font = PoliceDe(u * 0.66F, FontStyle.Regular), pinceau As New SolidBrush(_gris)
            g.DrawString(etiquette, f, pinceau, New RectangleF(zone.X, zone.Y + h * 0.07F, zone.Width, h * 0.2F), fmt)
        End Using
        DessinerIcone(g, p.Code, p.Jour, cx, zone.Y + h * 0.46F, u * 1.45F)
        Using f As Font = PoliceDe(u * 0.86F, FontStyle.Bold), pinceau As New SolidBrush(CouleurTemperature(p.Temp))
            g.DrawString(Math.Round(p.Temp).ToString("0", CultureInfo.InvariantCulture) & "°", f, pinceau, New RectangleF(zone.X, zone.Y + h * 0.65F, zone.Width, h * 0.2F), fmt)
        End Using
        If p.AMin Then
            Using f As Font = PoliceDe(u * 0.7F, FontStyle.Regular), pinceau As New SolidBrush(Melanger(Color.FromArgb(150, 175, 205), CouleurTemperature(p.Min), 0.35))
                g.DrawString(Math.Round(p.Min).ToString("0", CultureInfo.InvariantCulture) & "°", f, pinceau, New RectangleF(zone.X, zone.Y + h * 0.82F, zone.Width, h * 0.15F), fmt)
            End Using
        ElseIf p.Pluie >= 30 Then
            Using f As Font = PoliceDe(u * 0.56F, FontStyle.Regular), pinceau As New SolidBrush(Color.FromArgb(235, 120, 190, 255))
                g.DrawString(p.Pluie.ToString("0", CultureInfo.InvariantCulture) & "%", f, pinceau, New RectangleF(zone.X, zone.Y + h * 0.82F, zone.Width, h * 0.15F), fmt)
            End Using
        End If
    End Sub

    ''' <summary>Une case en ligne (disposition verticale) : heure (ou jour) a gauche, icone, temperature (maxi, puis la mini en gris pour un jour), probabilite de pluie a droite.</summary>
    Private Sub DessinerUneLigne(g As Graphics, p As PointHoraire, i As Integer, zone As RectangleF, u As Single)
        Dim cy As Single = zone.Y + zone.Height / 2.0F
        Dim gauche As New StringFormat(StringFormatFlags.NoWrap) With {.Alignment = StringAlignment.Near, .LineAlignment = StringAlignment.Center}
        Dim droite As New StringFormat(StringFormatFlags.NoWrap) With {.Alignment = StringAlignment.Far, .LineAlignment = StringAlignment.Center}
        Dim etiquette As String = If(p.Etiquette <> "", p.Etiquette, If(i = 0, "Maint.", p.Heure.Hour.ToString("0", CultureInfo.InvariantCulture) & "h"))
        Using f As Font = PoliceDe(u * 0.7F, FontStyle.Regular), pinceau As New SolidBrush(_gris)
            g.DrawString(etiquette, f, pinceau, New RectangleF(zone.X, zone.Y, u * 2.6F, zone.Height), gauche)
        End Using
        DessinerIcone(g, p.Code, p.Jour, zone.X + u * 3.5F, cy, u * 1.4F)
        Using f As Font = PoliceDe(u * 0.95F, FontStyle.Bold), pinceau As New SolidBrush(CouleurTemperature(p.Temp))
            g.DrawString(Math.Round(p.Temp).ToString("0", CultureInfo.InvariantCulture) & "°", f, pinceau, New RectangleF(zone.X + u * 4.5F, zone.Y, If(p.AMin, u * 2.1F, u * 2.4F), zone.Height), gauche)
        End Using
        If p.AMin Then
            Using f As Font = PoliceDe(u * 0.78F, FontStyle.Regular), pinceau As New SolidBrush(Melanger(Color.FromArgb(150, 175, 205), CouleurTemperature(p.Min), 0.35))
                g.DrawString(Math.Round(p.Min).ToString("0", CultureInfo.InvariantCulture) & "°", f, pinceau, New RectangleF(zone.X + u * 6.6F, zone.Y, u * 1.5F, zone.Height), gauche)
            End Using
        End If
        If p.Pluie >= 30 Then
            Using f As Font = PoliceDe(u * 0.62F, FontStyle.Regular), pinceau As New SolidBrush(Color.FromArgb(235, 120, 190, 255))
                g.DrawString(p.Pluie.ToString("0", CultureInfo.InvariantCulture) & "%", f, pinceau, New RectangleF(zone.X + u * 6.4F, zone.Y, zone.Width - u * 6.4F, zone.Height), droite)
            End Using
        End If
    End Sub

    ''' <summary>« Auj. », « Dem. », puis le jour de la semaine abrege (« Mer », « Jeu »...).</summary>
    Private Function EtiquetteDuJour(jour As DateTime, aujourdhui As DateTime) As String
        Dim ecart As Integer = CInt((jour.Date - aujourdhui.Date).TotalDays)
        If ecart = 0 Then Return "Auj."
        If ecart = 1 Then Return "Dem."
        Dim court As String = jour.ToString("ddd", New CultureInfo("fr-FR")).TrimEnd("."c)
        Return If(court.Length > 0, Char.ToUpper(court(0)) & court.Substring(1), court)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Icones vectorielles (code meteo WMO d'Open-Meteo) : tracees en GDI+, centrees en (cx, cy), tenant dans un carre de cote s
    ' ------------------------------------------------------------------------------------------

    Private Sub DessinerIcone(g As Graphics, code As Integer, jour As Boolean, cx As Single, cy As Single, s As Single)
        Select Case code
            Case 0, 1
                If jour Then Soleil(g, cx, cy, s * 0.5F) Else Lune(g, cx, cy, s * 0.5F)
            Case 2
                If jour Then Soleil(g, cx - s * 0.16F, cy - s * 0.16F, s * 0.36F) Else Lune(g, cx - s * 0.16F, cy - s * 0.16F, s * 0.36F)
                Nuage(g, cx + s * 0.07F, cy + s * 0.1F, s * 0.86F, False)
            Case 3
                Nuage(g, cx, cy, s * 0.95F, True)
            Case 45, 48
                Brouillard(g, cx, cy, s)
            Case 51, 53, 55, 56, 57
                Nuage(g, cx, cy - s * 0.1F, s * 0.86F, True)
                Gouttes(g, cx, cy + s * 0.27F, s, 2)
            Case 61, 63, 65, 66, 67, 80, 81, 82
                Nuage(g, cx, cy - s * 0.1F, s * 0.86F, True)
                Gouttes(g, cx, cy + s * 0.27F, s, 3)
            Case 71, 73, 75, 77, 85, 86
                Nuage(g, cx, cy - s * 0.1F, s * 0.86F, True)
                Flocons(g, cx, cy + s * 0.27F, s)
            Case 95, 96, 99
                Nuage(g, cx, cy - s * 0.12F, s * 0.86F, True, True)
                Eclair(g, cx, cy + s * 0.2F, s)
            Case Else
                Nuage(g, cx, cy, s * 0.9F, True)
        End Select
    End Sub

    Private Sub Soleil(g As Graphics, cx As Single, cy As Single, rayonTotal As Single)
        Dim r As Single = rayonTotal * 0.52F
        ' Halo discret.
        Using halo As New GraphicsPath()
            halo.AddEllipse(cx - rayonTotal, cy - rayonTotal, rayonTotal * 2, rayonTotal * 2)
            Using pg As New PathGradientBrush(halo)
                pg.CenterColor = Color.FromArgb(70, 255, 200, 70)
                pg.SurroundColors = {Color.FromArgb(0, 255, 200, 70)}
                g.FillPath(pg, halo)
            End Using
        End Using
        Using stylo As New Pen(Color.FromArgb(255, 255, 202, 64), Math.Max(1.2F, rayonTotal * 0.11F)) With {.StartCap = LineCap.Round, .EndCap = LineCap.Round}
            For i As Integer = 0 To 7
                Dim a As Double = i * Math.PI / 4
                g.DrawLine(stylo, cx + CSng(Math.Cos(a)) * r * 1.4F, cy + CSng(Math.Sin(a)) * r * 1.4F, cx + CSng(Math.Cos(a)) * rayonTotal * 0.97F, cy + CSng(Math.Sin(a)) * rayonTotal * 0.97F)
            Next
        End Using
        Using disque As New LinearGradientBrush(New RectangleF(cx - r, cy - r, r * 2, r * 2), Color.FromArgb(255, 255, 224, 96), Color.FromArgb(255, 255, 150, 28), 60.0F)
            g.FillEllipse(disque, cx - r, cy - r, r * 2, r * 2)
        End Using
    End Sub

    Private Sub Lune(g As Graphics, cx As Single, cy As Single, rayonTotal As Single)
        Dim r As Single = rayonTotal * 0.82F
        Using p1 As New GraphicsPath(), p2 As New GraphicsPath()
            p1.AddEllipse(cx - r, cy - r, r * 2, r * 2)
            p2.AddEllipse(cx - r + r * 0.62F, cy - r - r * 0.12F, r * 1.7F, r * 1.7F)
            Using zone As New System.Drawing.Region(p1)
                zone.Exclude(p2)
                Using pinceau As New LinearGradientBrush(New RectangleF(cx - r, cy - r, r * 2, r * 2), Color.FromArgb(255, 246, 248, 255), Color.FromArgb(255, 176, 190, 228), 45.0F)
                    g.FillRegion(pinceau, zone)
                End Using
            End Using
        End Using
        ' Une etoile.
        Using pinceau As New SolidBrush(Color.FromArgb(220, 255, 250, 220))
            g.FillEllipse(pinceau, cx + r * 0.55F, cy - r * 0.78F, r * 0.2F, r * 0.2F)
        End Using
    End Sub

    Private Sub Nuage(g As Graphics, cx As Single, cy As Single, largeur As Single, Optional gris As Boolean = False, Optional sombre As Boolean = False)
        Dim w As Single = largeur
        Dim h As Single = w * 0.6F
        Dim x As Single = cx - w / 2
        Dim y As Single = cy - h / 2
        Using p As New GraphicsPath(FillMode.Winding)
            p.AddEllipse(x + w * 0.04F, y + h * 0.40F, w * 0.40F, h * 0.58F)      ' bosse gauche
            p.AddEllipse(x + w * 0.26F, y + h * 0.05F, w * 0.46F, h * 0.82F)      ' grosse bosse
            p.AddEllipse(x + w * 0.52F, y + h * 0.30F, w * 0.44F, h * 0.68F)      ' bosse droite
            p.AddRectangle(New RectangleF(x + w * 0.22F, y + h * 0.5F, w * 0.52F, h * 0.48F))
            Dim haut As Color = If(sombre, Color.FromArgb(255, 150, 160, 186), If(gris, Color.FromArgb(255, 238, 243, 252), Color.FromArgb(255, 250, 252, 255)))
            Dim bas As Color = If(sombre, Color.FromArgb(255, 84, 92, 120), If(gris, Color.FromArgb(255, 160, 176, 206), Color.FromArgb(255, 188, 204, 232)))
            Using pinceau As New LinearGradientBrush(New RectangleF(x, y, w, h), haut, bas, 90.0F)
                g.FillPath(pinceau, p)
            End Using
        End Using
    End Sub

    Private Sub Gouttes(g As Graphics, cx As Single, cy As Single, s As Single, nombre As Integer)
        Using stylo As New Pen(Color.FromArgb(255, 92, 168, 255), Math.Max(1.2F, s * 0.075F)) With {.StartCap = LineCap.Round, .EndCap = LineCap.Round}
            For i As Integer = 0 To nombre - 1
                Dim dx As Single = (i - (nombre - 1) / 2.0F) * s * 0.26F
                g.DrawLine(stylo, cx + dx + s * 0.04F, cy - s * 0.08F, cx + dx - s * 0.04F, cy + s * 0.14F)
            Next
        End Using
    End Sub

    Private Sub Flocons(g As Graphics, cx As Single, cy As Single, s As Single)
        Using pinceau As New SolidBrush(Color.FromArgb(250, 255, 255, 255))
            For i As Integer = 0 To 2
                Dim dx As Single = (i - 1) * s * 0.27F
                Dim dy As Single = If(i = 1, s * 0.07F, 0.0F)
                Dim r As Single = s * 0.065F
                g.FillEllipse(pinceau, cx + dx - r, cy + dy - r, r * 2, r * 2)
            Next
        End Using
    End Sub

    Private Sub Eclair(g As Graphics, cx As Single, cy As Single, s As Single)
        Dim pts As PointF() = {New PointF(cx + s * 0.05F, cy - s * 0.18F), New PointF(cx - s * 0.12F, cy + s * 0.06F), New PointF(cx - s * 0.01F, cy + s * 0.06F),
                               New PointF(cx - s * 0.08F, cy + s * 0.28F), New PointF(cx + s * 0.14F, cy - s * 0.02F), New PointF(cx + s * 0.03F, cy - s * 0.02F)}
        Using pinceau As New LinearGradientBrush(New RectangleF(cx - s * 0.15F, cy - s * 0.2F, s * 0.3F, s * 0.5F), Color.FromArgb(255, 255, 236, 110), Color.FromArgb(255, 255, 170, 30), 90.0F)
            g.FillPolygon(pinceau, pts)
        End Using
    End Sub

    Private Sub Brouillard(g As Graphics, cx As Single, cy As Single, s As Single)
        Using stylo As New Pen(Color.FromArgb(235, 200, 210, 228), Math.Max(1.4F, s * 0.09F)) With {.StartCap = LineCap.Round, .EndCap = LineCap.Round}
            g.DrawLine(stylo, cx - s * 0.34F, cy - s * 0.2F, cx + s * 0.3F, cy - s * 0.2F)
            g.DrawLine(stylo, cx - s * 0.4F, cy, cx + s * 0.4F, cy)
            g.DrawLine(stylo, cx - s * 0.28F, cy + s * 0.2F, cx + s * 0.34F, cy + s * 0.2F)
        End Using
    End Sub

End Module
