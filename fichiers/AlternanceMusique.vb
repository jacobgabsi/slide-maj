Option Strict On
Option Explicit On

''' <summary>
''' Plusieurs sources de musique cochees a la fois (demande de Jacob, 14/10/2026 : "il faut aussi la multiselection
''' dans l'onglet Audio") : Fichiers MP3, Radio Internet, Deezer. Les boutons ronds exclusifs de l'onglet Audio sont
''' devenus des cases ; avec plusieurs cases cochees, Slide ALTERNE : chaque source joue pendant le temps choisi
''' ("Alterner toutes les N minutes"), puis la suivante prend le relais, et ainsi de suite.
'''
''' Une seule source est jouee a la fois : les trois reglages Music_Mp3 / Music_Radio / Music_Deezer restent donc
''' exclusifs (ils designent la source EN COURS, comme les taches "Radio to Mp3" de la minuterie) et tout le reste de
''' Slide est inchange. Les sources cochees sont gardees a part (reglage Music_Sources).
'''
''' Le changement se fait seulement quand la musique peut jouer (photos, ou video que l'on n'entend pas : voir
''' LaMusiquePeutJouerMaintenant) ; sinon il attend, sans perdre son tour. Une source inutilisable (radio sans
''' adresse, Deezer externe, aucun morceau MP3) est sautee.
''' </summary>
Friend Module AlternanceMusique

    Private ReadOnly Ordre As String() = {"mp3", "radio", "deezer"}

    Private _minuteur As System.Windows.Forms.Timer
    Private ReadOnly _depuis As Stopwatch = Stopwatch.StartNew()

    ' Le changement programme attend la FIN du morceau en cours (15/10/2026 : "permute que quand la chanson est terminee, sinon
    ' ca fait des coupures pas jolies, avec un maximum de 5 mn") : un morceau coupe en plein milieu n'est pas agreable.
    Private Const AttenteMaxMinutes As Integer = 5
    Private _attenteDepuis As Date = Date.MinValue
    Private _titreAttendu As String = ""

    ''' <summary>Les sources cochees ("mp3", "radio", "deezer"). A defaut de reglage : la source en cours.</summary>
    Friend Function Sources() As List(Of String)
        Dim liste As New List(Of String)
        For Each s As String In If(My.Settings.Music_Sources, "").Split(";"c)
            Dim code As String = s.Trim().ToLowerInvariant()
            If Ordre.Contains(code) AndAlso Not liste.Contains(code) Then liste.Add(code)
        Next
        If liste.Count = 0 Then
            If Music_Mp3 Then liste.Add("mp3")
            If Music_Radio Then liste.Add("radio")
            If Music_Deezer Then liste.Add("deezer")
        End If
        Return liste
    End Function

    ''' <summary>La source qui joue en ce moment (les reglages exclusifs), "" si aucune.</summary>
    Friend Function SourceEnCours() As String
        If Music_Deezer Then Return "deezer"
        If Music_Radio Then Return "radio"
        If Music_Mp3 Then Return "mp3"
        Return ""
    End Function

    ''' <summary>La source qui suit <paramref name="courante"/> dans l'ordre Fichiers MP3, Radio, Deezer, parmi les
    ''' <paramref name="cochees"/> (la premiere si la courante n'en fait pas partie). "" si aucune source.</summary>
    Friend Function SuivanteApres(courante As String, cochees As IEnumerable(Of String)) As String
        Dim parmi As List(Of String) = Ordre.Where(Function(o) cochees.Contains(o)).ToList()
        If parmi.Count = 0 Then Return ""
        Dim i As Integer = parmi.IndexOf(If(courante, ""))
        Return parmi((i + 1) Mod parmi.Count)
    End Function

    ''' <summary>Garde les sources cochees et la duree (appele par l'OK des reglages).</summary>
    Friend Sub Enregistrer(cochees As IEnumerable(Of String), minutes As Integer)
        My.Settings.Music_Sources = String.Join(";", Ordre.Where(Function(o) cochees.Contains(o)))
        My.Settings.Music_AlternanceMinutes = Math.Max(1, Math.Min(240, minutes))
        _depuis.Restart()
        _attenteDepuis = Date.MinValue
    End Sub

    Private Function Utilisable(source As String) As Boolean
        Select Case source
            Case "mp3" : Return MusiqueVLC.MusiqueNombre() > 0
            Case "radio" : Return If(RadioUrl, "").Trim() <> ""
            Case "deezer" : Return MusiqueDeezer.Interne
        End Select
        Return False
    End Function

    ''' <summary>A appeler une fois, depuis le fil de l'interface, au demarrage.</summary>
    Friend Sub Demarrer()
        If _minuteur IsNot Nothing Then Exit Sub
        _minuteur = New System.Windows.Forms.Timer With {.Interval = 500}
        AddHandler _minuteur.Tick, Sub() Verifier()
        _minuteur.Start()
        ' Deezer fait partie des sources cochees mais ne joue pas : il est charge en silence a l'avance (voir MusiqueDeezer.Prechauffer).
        Dim avance As New System.Windows.Forms.Timer With {.Interval = 40000}
        AddHandler avance.Tick,
            Sub()
                avance.Stop()
                Try
                    If Music AndAlso Sources().Contains("deezer") AndAlso SourceEnCours() <> "deezer" Then MusiqueDeezer.Prechauffer()
                Catch ex As Exception
                End Try
            End Sub
        avance.Start()
    End Sub

    Private _doubleSonVu As Integer

    ''' <summary>
    ''' Garde-fou : MP3 (VLC) et Deezer ne jouent jamais ensemble une fois Deezer en lecture (15/10/2026 : "a un moment deezer et mp3
    ''' jouent en meme temps, c'est pas normal"). Six constats de suite (3 s) pour ne pas gener le court passage de relais ; VLC est
    ''' alors mis en pause. L'evenement est ecrit dans le journal.
    ''' </summary>
    Private Sub GarderUneSeuleSource()
        Try
            If Music AndAlso SourceEnCours() = "deezer" AndAlso MusiqueDeezer.EnLecture AndAlso MusiqueVLC.VlcJoue() Then
                _doubleSonVu += 1
                If _doubleSonVu >= 6 Then
                    DiagLog("Musique : Deezer et un fichier MP3/radio jouaient EN MEME TEMPS -> VLC mis en pause")
                    MusiqueVLC.PauserCarDeezerJoue()
                    _doubleSonVu = 0
                End If
            Else
                _doubleSonVu = 0
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub Verifier()
        Try
            GarderUneSeuleSource()
            If Not Music Then Exit Sub
            Dim cochees As List(Of String) = Sources()
            If cochees.Count < 2 Then Exit Sub
            If _depuis.Elapsed.TotalMinutes < Math.Max(1, My.Settings.Music_AlternanceMinutes) Then Exit Sub
            If Not LaMusiquePeutJouerMaintenant() Then Exit Sub          ' son de video, film, TV... : on attend

            Dim source As String = SourceEnCours()
            ' Radio (pas de fin de morceau) ou rien qui joue (rien a couper) : on change tout de suite.
            Dim joueUnMorceau As Boolean = (source = "mp3" AndAlso MusiqueVLC.MusiqueEnCours()) OrElse (source = "deezer" AndAlso MusiqueDeezer.EnLecture)
            If Not joueUnMorceau Then
                PasserALaSourceSuivante("alternance")
                Exit Sub
            End If
            If _attenteDepuis = Date.MinValue Then
                _attenteDepuis = Now
                _titreAttendu = If(source = "deezer", MusiqueDeezer.MorceauEnCours(), "")
                DiagLog("Musique : alternance due, attend la fin du morceau (" & AttenteMaxMinutes & " mn au plus)" &
                        If(source = "deezer" AndAlso MusiqueDeezer.Etat IsNot Nothing, " ; Deezer : duree " & MusiqueDeezer.Etat.Duree.ToString("0") & " s, position " & MusiqueDeezer.Etat.Position.ToString("0") & " s", ""))
            End If
            If (Now - _attenteDepuis).TotalMinutes >= AttenteMaxMinutes Then
                PasserALaSourceSuivante("alternance (" & AttenteMaxMinutes & " mn d'attente depassees)")
            ElseIf source = "deezer" Then
                MusiqueDeezer.SurveillerLaFin(True)
                Dim etat As MusiqueDeezer.EtatDuLecteur = MusiqueDeezer.Etat
                If etat IsNot Nothing AndAlso etat.Duree > 5 AndAlso etat.Position <= etat.Duree + 1 Then
                    ' La duree est connue : la source suivante prend le relais JUSTE AVANT la fin, sans que le morceau suivant
                    ' de Deezer ne commence (15/10/2026 : "l'alternance doit etre chanson prochaine").
                    If etat.Duree - etat.Position <= 2.5 Then PasserALaSourceSuivante("alternance (fin du morceau)")
                ElseIf MusiqueDeezer.MorceauEnCours() <> _titreAttendu Then
                    ' Duree inconnue : Deezer a deja enchaine, le morceau a change (l'ancien est donc termine).
                    PasserALaSourceSuivante("alternance (morceau suivant, duree inconnue)")
                End If
            End If
            ' (mp3 : MusiqueVLC appelle ChangerALaFinDuMorceau au moment ou le fichier se termine.)
        Catch ex As Exception
            DiagLog("Musique : alternance impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Appele par MusiqueVLC quand un fichier MP3 vient de se terminer : si l'alternance attend cette fin, la source change
    ''' maintenant au lieu de lancer le morceau suivant. Rend Vrai si c'est fait (le morceau suivant ne doit pas etre lance).</summary>
    Friend Function ChangerALaFinDuMorceau() As Boolean
        If _attenteDepuis = Date.MinValue Then Return False
        Return PasserALaSourceSuivante("alternance (fin du morceau)")
    End Function

    ''' <summary>
    ''' Passe a la source suivante parmi celles cochees (Fichiers MP3, Radio, Deezer), en sautant celles qui ne sont pas
    ''' utilisables, et la joue. Sert a l'alternance programmee et a la touche N (15/10/2026 : "le N doit aussi changer de
    ''' source de musique, par exemple de Deezer a mp3, si selectionne"). Rend Vrai si la source a change ; Faux s'il n'y
    ''' a qu'une source cochee ou qu'aucune autre n'est utilisable (la musique n'est alors pas touchee).
    ''' </summary>
    Friend Function PasserALaSourceSuivante(origine As String) As Boolean
        Try
            Dim cochees As List(Of String) = Sources()
            If cochees.Count < 2 Then Return False
            Dim courante As String = SourceEnCours()
            Dim candidate As String = courante
            Dim choisie As String = ""
            For essai As Integer = 1 To cochees.Count
                candidate = SuivanteApres(candidate, cochees)
                If candidate = courante Then Exit For
                If Utilisable(candidate) Then
                    choisie = candidate
                    Exit For
                End If
            Next
            _depuis.Restart()                                         ' (la prochaine echeance part d'ici dans tous les cas)
            _attenteDepuis = Date.MinValue
            MusiqueDeezer.SurveillerLaFin(False)
            If choisie = "" Then Return False
            Music_Mp3 = (choisie = "mp3")
            Music_Radio = (choisie = "radio")
            Music_Deezer = (choisie = "deezer")
            DiagLog("Musique : " & origine & " " & courante & " -> " & choisie & " (" & String.Join(", ", cochees) & ")")
            Music_SWITCH_ON(True)
            If choisie = "deezer" Then MusiqueDeezer.ReprendreAuMorceauSuivant()
            If origine.StartsWith("touche S", StringComparison.OrdinalIgnoreCase) Then CarteDeezer.AnnoncerLaSource(If(choisie = "deezer", "Deezer", If(choisie = "radio", "Radio", "Fichiers MP3")))
            Return True
        Catch ex As Exception
            DiagLog("Musique : changement de source impossible (" & ex.Message & ")")
            Return False
        End Try
    End Function

End Module
