Option Strict On
Option Explicit On

''' <summary>
''' Prechargement de la video suivante du diaporama, pour enchainer les videos SANS attente.
'''
''' Pendant qu'une video joue, la suivante (celle que choisira ProchaineVideoSuivante) est ouverte
''' sur le lecteur cache de Form3, demarree en pause (":start-paused" : VLC ouvre le fichier et
''' decode la premiere image, sans son). Quand le diaporama la demande, AfficheVideo bascule de
''' lecteur au lieu d'ouvrir le fichier : plus d'ouverture, plus d'attente du demarrage.
'''
''' Seulement pour une video courte qui part du debut (les rushes du mariage : 4 a 14 s) ; une
''' longue video decoupee en extraits demarre a une position tiree au hasard : chemin normal.
''' </summary>
Public Module PrechargementVideo

    Private _nom As String = ""

    ''' <summary>Vrai si une video est prechargee sur le lecteur cache.</summary>
    Public ReadOnly Property SuivantePrete As Boolean
        Get
            Return _nom <> ""
        End Get
    End Property

    ' Apres un basculement, le volume de la video est verifie pendant 2 s (voir
    ' NiveauSonore.ReaffirmerLeVolumeVideo).
    Private WithEvents _minuteurVolume As New Timer With {.Interval = 100}
    Private _toursVolume As Integer

    Private Sub SurveillerLeVolume()
        _toursVolume = 0
        _minuteurVolume.Start()
    End Sub

    Private Sub _minuteurVolume_Tick(sender As Object, e As EventArgs) Handles _minuteurVolume.Tick
        _toursVolume += 1
        If _toursVolume > 20 Then _minuteurVolume.Stop()
        NiveauSonore.ReaffirmerLeVolumeVideo()
    End Sub

    ''' <summary>Video de la ligne <paramref name="numVideo"/> de la grille, si elle peut etre
    ''' prechargee (courte, pas encore jouee) ; sinon "".</summary>
    Private Function VideoPrechargeable(numVideo As Integer) As String
        Try
            If numVideo < 0 OrElse numVideo >= Form2.DataGridView2.Rows.Count Then Return ""
            Dim ligne As DataGridViewRow = Form2.DataGridView2.Rows(numVideo)
            If CStr(If(ligne.Cells(0).Value, "")).Trim() = "1" Then Return ""          ' deja jouee
            Dim nom As String = CStr(If(ligne.Cells(3).Value, ""))
            If nom = "" OrElse nom.StartsWith("http", StringComparison.OrdinalIgnoreCase) OrElse Not IO.File.Exists(nom) Then Return ""
            Dim duree As String = GetDuration(nom)                  ' memorisee : instantane la 2e fois
            If duree = "" OrElse duree = "00:00:00" Then Return ""
            Dim secondes As Double = TimeSpan.ParseExact(duree, "c", Nothing).TotalSeconds
            If secondes < MinimumDurréeVideoAcceptable OrElse secondes > DurréeVideo Then Return ""  ' extraits : chemin normal
            Return nom
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>Video qui suivra <paramref name="numVideo"/> dans l'enchainement (meme regle que
    ''' ProchaineVideoSuivante, sans rien modifier).</summary>
    Private Function VideoSuivante(numVideo As Integer) As Integer
        Dim n As Integer = Form2.DataGridView2.Rows.Count
        If n = 0 Then Return -1
        Dim nonJouee As Func(Of Integer, Boolean) = Function(i) CStr(If(Form2.DataGridView2.Rows(i).Cells(0).Value, "")).Trim() <> "1"
        For k As Integer = 1 To n
            Dim i As Integer = (numVideo + k) Mod n
            If nonJouee(i) Then Return i
        Next
        Return -1
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Mesure du 01/10 (ecran filme pendant 4 changements de video, lecteur cache puis affiche) :
    ' ancienne video figee 350-400 ms, ECRAN NOIR 60-500 ms, 1re image figee 100-200 ms. Causes :
    '  - un lecteur CACHE (ou couvert par l'autre) n'est pas dessine par Windows : au moment ou il
    '    passe devant, il est noir jusqu'a sa prochaine image ;
    '  - une video ouverte en pause met ~300 ms a repartir.
    ' D'ou la methode : le lecteur de la video suivante reste VISIBLE mais reduit a un point de
    ' 2 x 2 pixels dans le coin (son image est donc vraiment dessinee) ; il est LANCE un peu avant
    ' la fin de la video en cours (LancerEnAvance) ; au changement, il n'y a plus qu'a l'agrandir.
    ' ------------------------------------------------------------------------------------------

    Private _lancee As Boolean

    ' Chronometrage du changement de video (du moment ou Form1 quitte la video en cours jusqu'a
    ' ce que la suivante soit a l'ecran) : chaque etape est notee dans le journal avec la bascule.
    Private ReadOnly _chrono As New Stopwatch()
    Private _etapes As String = ""

    ''' <summary>Debut du changement (Form1 quitte la video en cours).</summary>
    Public Sub DebutDuChangement()
        _etapes = ""
        _chrono.Restart()
    End Sub

    ''' <summary>Note le temps ecoule depuis le debut du changement.</summary>
    Public Sub Etape(nom As String)
        If _chrono.IsRunning Then _etapes &= " " & nom & "=" & _chrono.ElapsedMilliseconds
    End Sub

    ' La video d'apres n'est ouverte qu'une seconde apres le changement : ouverte tout de suite,
    ' elle provoquait un a-coup (~0,1 s) juste apres le debut de la nouvelle video.
    Private WithEvents _minuteurSuivante As New Timer With {.Interval = 1000}
    Private _numVideoEnCours As Integer = -1

    Public Sub PreparerLaSuivanteDansUnInstant(numVideo As Integer)
        _numVideoEnCours = numVideo
        _minuteurSuivante.Stop()
        _minuteurSuivante.Start()
    End Sub

    Private Sub _minuteurSuivante_Tick(sender As Object, e As EventArgs) Handles _minuteurSuivante.Tick
        _minuteurSuivante.Stop()
        If _numVideoEnCours >= 0 Then PreparerLaSuivante(_numVideoEnCours)
    End Sub

    ''' <summary>Place du lecteur de la video suivante pendant qu'il attend : taille de l'image,
    ''' decale pour ne laisser depasser qu'un coin de 2 x 2 pixels en bas a droite.</summary>
    Private Function ZoneEnAttente() As Rectangle
        Dim l As Integer = Form3.ClientSize.Width, h As Integer = Form3.ClientSize.Height
        Return New Rectangle(l - 2, h - 2, l, h)
    End Function

    ''' <summary>A appeler quand une video vient de demarrer : prepare la suivante.</summary>
    Public Sub PreparerLaSuivante(numVideo As Integer)
        Try
            If ModeRdv OrElse Diffusion.Mode <> "VIDEO" Then Exit Sub
            Dim suivante As Integer = VideoSuivante(numVideo)
            If suivante < 0 OrElse suivante = numVideo Then Exit Sub
            Dim nom As String = VideoPrechargeable(suivante)
            If nom = "" OrElse String.Equals(nom, _nom, StringComparison.OrdinalIgnoreCase) Then Exit Sub
            ' Meme regle que Form1 : le diaporama n'enchaine une autre video que si elle tient dans
            ' le temps video restant (sinon il repasse aux photos, et ouvrir la video serait inutile).
            Dim cumul As Double = CDbl(CumSecondeVideoPlay) + CDbl(DurationLastVideo)
            Dim dureeSuivante As Double = TimeSpan.ParseExact(GetDuration(nom), "c", Nothing).TotalSeconds
            If cumul >= DurréeVideo OrElse cumul + dureeSuivante >= DurréeVideo Then Exit Sub
            ' La video suivante est seulement RETENUE ici ; elle est ouverte et lancee d'un seul
            ' coup un peu avant la fin de celle-ci (LancerEnAvance). Mesure du 01/10 : sur ce PC
            ' (puce Intel UHD 620, rushes en 4K 50 images/s), chaque operation sur le second
            ' lecteur fige l'ecran 0,1 a 0,2 s ; l'ouvrir a +1 s puis la lancer avant la fin
            ' faisait DEUX a-coups par video. Il n'en reste qu'un, colle a la coupure.
            _nom = nom
            _lancee = False
        Catch ex As Exception
            _nom = ""
            DiagLog("Prechargement video : " & ex.Message)
        End Try
    End Sub

    ''' <summary>A appeler un peu avant la fin de la video en cours : la suivante commence a jouer
    ''' (encore reduite a son point), pour etre en mouvement au moment du changement.</summary>
    Public Sub LancerEnAvance()
        If _nom = "" OrElse _lancee Then Exit Sub
        _lancee = True
        Try
            Dim lecteur As Vlc.DotNet.Forms.VlcControl = Form3.VlcLecteurCache
            ' Visible, a sa TAILLE FINALE, mais decale presque entierement hors de l'image : seul
            ' un coin de 2 x 2 pixels depasse, en bas a droite. Au changement il suffit de le
            ' DEPLACER (l'agrandir coutait une image noire : VLC refait sa surface de dessin).
            lecteur.Bounds = ZoneEnAttente()
            lecteur.Visible = True
            lecteur.BringToFront()
            ' Son : au volume du diaporama si le son des videos est actif, muet sinon (chaque
            ' lecteur a sa sortie son : cela ne touche ni la musique ni le PIP).
            Dim avecSon As Boolean = CBool(VideoWithAudio) AndAlso PIPvolume <= 0
            If lecteur.Audio IsNot Nothing Then
                lecteur.Audio.IsMute = Not avecSon
                ' (volume deja corrige par le lissage : la video part au bon niveau des son lancement)
                If avecSon Then lecteur.Audio.Volume = NiveauSonore.VolumeVlcPourLaVideo(_nom, 0, CDbl(DurréeVideo))
            End If
            _imagesAuLancement = 0
            _signalDeLecture = False
            _signauxRecus = 0
            _lecteurLance = lecteur
            _depuisLeLancement.Restart()
            Form3.AttendreLeLecteur(lecteur)       ' fin de son arret en cours, s'il y en a un
            ' Ouverture ET lecture d'un seul coup.
            CouleursDuDiaporama.PourLaVideo(lecteur)     ' noir et blanc ou couleurs, selon les reglages
            lecteur.SetMedia(New Uri("file:///" & _nom))
            lecteur.Play()
        Catch ex As Exception
            DiagLog("Prechargement video (lancement) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La video suivante, lancee en avance, affiche-t-elle deja des images ? (Sa position
    ''' n'est mise a jour par VLC que toutes les ~300 ms : on se fie aussi au temps ecoule depuis
    ''' son lancement, une video prechargee mettant ~300 ms a repartir.)</summary>
    Public ReadOnly Property SuivanteEnMouvement As Boolean
        Get
            Try
                If _nom = "" OrElse Not _lancee Then Return False
                Dim lecteur As Vlc.DotNet.Forms.VlcControl = Form3.VlcLecteurCache
                ' Compteur de VLC : images REELLEMENT affichees par ce lecteur. Deux de plus qu'au
                ' lancement = la video bouge. (Idee de Jacob : mesurer plutot que deviner. Garde-fou
                ' si le compteur ne repond pas : 600 ms apres le lancement.)
                ' Signal de VLC (demande de Jacob : une fonction de VLC plutot qu'un chronometre) :
                ' le lecteur a annonce que sa position de lecture AVANCE depuis le lancement.
                If _signalDeLecture Then Return True
                ' Le compteur d'images affichees, lui, n'est rafraichi par VLC que de loin en loin.
                Dim affichees As Integer = ImagesAffichees(lecteur)
                If affichees >= _imagesAuLancement + 2 Then Return True
                ' (5e mesure : le compteur reste a 0 pendant le changement - VLC ne le rafraichit que
                '  de loin en loin ; et la video est en fait a 160-210 ms de son debut 310-430 ms
                '  apres le lancement : elle part en ~200 ms. On la tient pour lancee a 260 ms.)
                ' Garde-fou si le signal n'arrive pas : 700 ms apres le lancement.
                Return lecteur.IsPlaying AndAlso _depuisLeLancement.ElapsedMilliseconds >= 700
            Catch ex As Exception
                Return False
            End Try
        End Get
    End Property

    Private ReadOnly _depuisLeLancement As New Stopwatch()
    Private _imagesAuLancement As Integer

    ' Signal "TimeChanged" de VLC, recu sur un thread de VLC : la position du lecteur avance.
    Private _lecteurLance As Vlc.DotNet.Forms.VlcControl
    Private _signalDeLecture As Boolean
    Private _signalApresMs As Long

    ''' <summary>Appele par les deux lecteurs du diaporama a chaque avancee de leur position.</summary>
    ''' <remarks>6e mesure : le PREMIER signal arrive ~180 ms apres le lancement, mais les images
    ''' ne sont pas encore a l'ecran (image noire, puis 0,2 s fige apres le changement). Le
    ''' DEUXIEME signal, ~250 ms plus tard, correspond a une video qui tourne vraiment.</remarks>
    Public Sub LectureAvance(lecteur As Vlc.DotNet.Forms.VlcControl, position As Long)
        If _signalDeLecture OrElse Not _lancee OrElse Not ReferenceEquals(lecteur, _lecteurLance) OrElse position <= 0 Then Exit Sub
        _signauxRecus += 1
        If _signauxRecus = 1 Then _premierSignalMs = _depuisLeLancement.ElapsedMilliseconds
        ' (Un seul signal : la video est ouverte ET lancee d'un coup, le signal arrive quand elle joue.)
        _signalApresMs = _depuisLeLancement.ElapsedMilliseconds
        _signalDeLecture = True
    End Sub

    Private _signauxRecus As Integer
    Private _premierSignalMs As Long

    ''' <summary>Nombre d'images affichees par le lecteur depuis l'ouverture de sa video (-1 si
    ''' VLC ne le donne pas).</summary>
    Private Function ImagesAffichees(lecteur As Vlc.DotNet.Forms.VlcControl) As Integer
        Try
            Dim media As Vlc.DotNet.Core.VlcMedia = lecteur.GetCurrentMedia()
            If media Is Nothing Then Return -1
            Return media.Statistics.DisplayedPictures
        Catch ex As Exception
            Return -1
        End Try
    End Function

    ''' <summary>Images affichees par la video suivante depuis son lancement en avance (journal).</summary>
    Public Function ImagesDepuisLeLancement() As Integer
        Dim n As Integer = ImagesAffichees(Form3.VlcLecteurCache)
        Return If(n < 0, -1, n - _imagesAuLancement)
    End Function

    ' Position de la video en cours : VLC ne l'actualise que par a-coups (~250-300 ms). Entre deux
    ' actualisations, on l'extrapole avec un chronometre, pour declencher le lancement de la
    ' suivante et le changement au bon moment (mesure du 01/10 : avec la position brute, la video
    ' arrivait a sa fin avant que le changement soit decide).
    Private _dernierePosition As Long = -1
    Private ReadOnly _depuisLaPosition As New Stopwatch()

    ''' <summary>Temps restant (ms) avant la fin de la video en cours, extrapole.</summary>
    Public Function ResteDeLaVideoEnCours() As Long
        Dim lecteur As Vlc.DotNet.Forms.VlcControl = Form3.VlcControl1
        Dim position As Long = lecteur.Time
        If position <> _dernierePosition Then
            _dernierePosition = position
            _depuisLaPosition.Restart()
        End If
        Dim extrapolee As Long = position + If(lecteur.IsPlaying, _depuisLaPosition.ElapsedMilliseconds, 0L)
        Return lecteur.Length - extrapolee
    End Function

    ''' <summary>Le diaporama repasse aux photos (ou change de mode) : le point du lecteur de
    ''' prechargement ne doit pas rester a l'ecran.</summary>
    Public Sub Abandonner()
        _minuteurSuivante.Stop()
        If _nom = "" Then Exit Sub
        _nom = ""
        _lancee = False
        Try
            Dim lecteur As Vlc.DotNet.Forms.VlcControl = Form3.VlcLecteurCache
            lecteur.Visible = False
            lecteur.SetPause(True)
            Form3.LibererLeLecteur(lecteur)        ' video suivante abandonnee : sa memoire est rendue
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Si <paramref name="nom"/> est la video prechargee, son lecteur devient celui du
    ''' diaporama (Form3.VlcControl1), agrandi a toute l'image, et la fonction renvoie True ;
    ''' l'appelant n'a plus qu'a la lancer (Play, sans effet si elle joue deja). Sinon False :
    ''' ouverture normale.</summary>
    Public Function Prendre(nom As String, pos As Double) As Boolean
        Etape("prendre")
        Dim attendu As String = _nom
        Dim etaitLancee As Boolean = _lancee
        If attendu = "" Then Return False
        If pos <> 0 OrElse Not String.Equals(attendu, nom, StringComparison.OrdinalIgnoreCase) Then
            Abandonner()
            Return False
        End If
        _nom = ""
        _lancee = False
        Try
            Dim lecteur As Vlc.DotNet.Forms.VlcControl = Form3.VlcLecteurCache
            ' Le fichier doit etre ouvert (en pause, ou deja en lecture) ; encore en ouverture
            ' (disque lent), on lui laisse un court instant plutot que de tout recommencer.
            Dim chrono As Stopwatch = Stopwatch.StartNew()
            Dim pret As Func(Of Boolean) = Function() etaitLancee AndAlso lecteur.State = Vlc.DotNet.Core.Interops.Signatures.MediaStates.Playing
            Do While Not pret() AndAlso chrono.ElapsedMilliseconds < 400
                Application.DoEvents()
                Threading.Thread.Sleep(10)
            Loop
            If Not pret() Then
                DiagLog("Prechargement video : " & IO.Path.GetFileName(nom) & " pas prete (" & lecteur.State.ToString() & "), ouverture normale")
                lecteur.Visible = False
                Return False
            End If
            Dim position As Long = lecteur.Time
            Dim imagesJouees As Integer = If(etaitLancee, ImagesDepuisLeLancement(), 0)
            Etape("pret")
            ' Une photographie couvre-t-elle deja l'image (FormTransitionVideo) ? Alors le nouveau lecteur passe devant tout de suite, a l'abri. Sinon l'ANCIENNE image reste devant,
            ' vivante, jusqu'a ce que la nouvelle video joue (AfficheVideo prend alors la photographie puis appelle FinirLaBascule) : pas de saut avant la poussee.
            Form3.BasculerLeLecteur(FormTransitionVideo.Couvre)                   ' deja visible : il est deplace ; devant ou derriere selon ce qui precede
            Etape("bascule")
            _chrono.Stop()
            SurveillerLeVolume()
            DiagLog("Prechargement video : bascule sur " & IO.Path.GetFileName(nom) & " (" &
                    If(etaitLancee, "lancee depuis " & _depuisLeLancement.ElapsedMilliseconds & " ms, signal VLC " &
                       If(_signalDeLecture, "recu a " & _signalApresMs & " ms", "NON recu") & ", position " & position & " ms",
                       "en pause") & ") | etapes en ms :" & _etapes)
            Return True
        Catch ex As Exception
            DiagLog("Prechargement video : " & ex.Message)
            Return False
        End Try
    End Function
End Module
