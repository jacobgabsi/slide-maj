Option Strict On
Option Explicit On

''' <summary>
''' Ouverture des reglages (Form4) : tous les sons qui jouent A CET INSTANT sont arretes (demande
''' de Jacob, 09/10/2026) : musique (MP3, radio, Deezer dans Slide), video du diaporama, film, TV,
''' PIP. Ce qui est lance ensuite depuis les reglages (apercu TV, test d'une radio...) s'entend
''' normalement : seuls les lecteurs coupes ici sont retenus.
'''
''' A la fermeture des reglages, chaque son coupe reprend comme il etait. (Reglages valides avec
''' un changement de sources : Slide se relance, il n'y a rien a rendre.)
'''
''' Les lecteurs video ne sont pas mis en pause mais rendus muets, piste audio desactivee : une
''' pause fausserait les boucles qui attendent la fin de la video, et l'interrupteur "muet" seul
''' est partage entre les lecteurs (voir CouperLeLecteur, Module2).
''' </summary>
Friend Module SilenceDesReglages

    Private Class LecteurCoupe
        Public Lecteur As Vlc.DotNet.Forms.VlcControl
        Public Piste As Integer = -1           ' piste audio a remettre
        Public Volume As Integer = -1          ' volume VLC a remettre
    End Class

    Private ReadOnly _coupes As New List(Of LecteurCoupe)
    Private _actif As Boolean
    Private _musiqueJouait As Boolean
    Private _modeALOuverture As String = ""

    ''' <summary>Vrai tant que les reglages sont ouverts et que leur silence est en place. Ce qui rend
    ''' automatiquement le son de Slide (fermeture d'un apercu, "retablir les volumes"...) ne doit alors
    ''' rien rendre : le son revient a la fermeture des reglages (Rendre).</summary>
    Friend ReadOnly Property Actif As Boolean
        Get
            Return _actif
        End Get
    End Property

    ''' <summary>A l'ouverture des reglages : coupe tout ce qui s'entend.</summary>
    Sub Couper()
        If _actif Then Exit Sub
        _actif = True
        _modeALOuverture = If(Diffusion.Mode, "")

        ' La musique : en pause (MusiquePause s'occupe aussi du Deezer integre).
        _musiqueJouait = False
        Try
            _musiqueJouait = MusiqueVLC.MusiqueEnCours()
            MusiqueVLC.MusiquePause()
        Catch ex As Exception
            DiagLog("Reglages (silence, musique) : " & ex.Message)
        End Try

        ' Les lecteurs video de toutes les fenetres ouvertes, PIP compris.
        Dim noms As New List(Of String)
        SyncLock _coupes
            _coupes.Clear()
            For Each lecteur As Vlc.DotNet.Forms.VlcControl In LecteursOuverts()
                Try
                    If lecteur.Name = "VlcMusique" Then Continue For
                    If lecteur.Audio Is Nothing OrElse Not lecteur.IsPlaying Then Continue For
                    If lecteur.Audio.IsMute Then Continue For                       ' deja muet : rien a rendre
                    Dim actuelle = lecteur.Audio.Tracks.Current
                    If actuelle Is Nothing OrElse actuelle.ID = -1 Then Continue For ' deja sans piste audio
                    Dim c As New LecteurCoupe With {.Lecteur = lecteur, .Piste = actuelle.ID, .Volume = lecteur.Audio.Volume}
                    Taire(lecteur)
                    _coupes.Add(c)
                    noms.Add(If(lecteur.Parent Is Nothing, "", lecteur.Parent.Name & ".") & lecteur.Name)
                Catch ex As Exception
                    DiagLog("Reglages (silence, " & lecteur.Name & ") : " & ex.Message)
                End Try
            Next
        End SyncLock

        DiagLog("Reglages ouverts : sons arretes (musique " & If(_musiqueJouait, "en pause", "deja arretee") &
                ", lecteurs coupes : " & If(noms.Count = 0, "aucun", String.Join(", ", noms)) & ")")
    End Sub

    ''' <summary>A la fermeture des reglages : rend le son a ce qui a ete coupe par Couper.</summary>
    Sub Rendre()
        If Not _actif Then Exit Sub
        _actif = False

        Dim aRendre As List(Of LecteurCoupe)
        SyncLock _coupes
            aRendre = _coupes.ToList()
            _coupes.Clear()
        End SyncLock

        Dim rendus As Integer = 0
        For Each c As LecteurCoupe In aRendre
            Try
                Dim lecteur As Vlc.DotNet.Forms.VlcControl = c.Lecteur
                If lecteur Is Nothing OrElse lecteur.IsDisposed OrElse lecteur.Audio Is Nothing OrElse Not lecteur.IsPlaying Then Continue For
                Dim actuelle = lecteur.Audio.Tracks.Current
                If actuelle Is Nothing OrElse actuelle.ID = -1 Then
                    Dim piste = lecteur.Audio.Tracks.All.FirstOrDefault(Function(p) p.ID = c.Piste)
                    If piste Is Nothing Then piste = lecteur.Audio.Tracks.All.FirstOrDefault(Function(p) p.ID <> -1)
                    If piste IsNot Nothing Then lecteur.Audio.Tracks.Current = piste
                End If
                lecteur.Audio.IsMute = False
                If c.Volume >= 0 Then lecteur.Audio.Volume = c.Volume
                rendus += 1
            Catch ex As Exception
                DiagLog("Reglages (son rendu) : " & ex.Message)
            End Try
        Next

        ' La musique ne reprend que si rien n'a change entre-temps : meme diffusion (une tache de
        ' la minuterie a pu lancer un film pendant les reglages), musique toujours voulue, pas de
        ' PIP avec le son.
        Dim musiqueReprise As Boolean = _musiqueJouait AndAlso CBool(Music) AndAlso CInt(PIPvolume) = 0 AndAlso
                                        If(Diffusion.Mode, "") = _modeALOuverture
        If musiqueReprise Then
            Try
                MusiqueVLC.MusiqueJouer()
            Catch ex As Exception
                DiagLog("Reglages (musique reprise) : " & ex.Message)
            End Try
        End If
        DiagLog("Reglages fermes : sons rendus (" & rendus & " lecteur(s) sur " & aRendre.Count &
                ", musique " & If(musiqueReprise, "reprise", "non reprise") & ")")
    End Sub

    ''' <summary>
    ''' Appelee par RendreLeSonAuLecteur (Module2). Pendant que les reglages sont ouverts, un lecteur
    ''' coupe ici ne reprend pas le son (film dont le son est "retabli", PIP relance apres un flux
    ''' gele...) : il reste muet et le volume demande est retenu pour la fermeture des reglages.
    ''' Renvoie True si le lecteur est retenu (l'appelant ne lui rend pas le son).
    ''' </summary>
    Function Retient(lecteur As Vlc.DotNet.Forms.VlcControl, volumeVlc As Integer) As Boolean
        If Not _actif OrElse lecteur Is Nothing Then Return False
        SyncLock _coupes
            Dim c As LecteurCoupe = _coupes.FirstOrDefault(Function(x) x.Lecteur Is lecteur)
            If c Is Nothing Then Return False
            c.Volume = volumeVlc
            Try
                ' Flux relance : il a une nouvelle piste audio, active. On la retient et on la coupe.
                Dim actuelle = lecteur.Audio.Tracks.Current
                If actuelle IsNot Nothing AndAlso actuelle.ID <> -1 Then c.Piste = actuelle.ID
                Taire(lecteur)
            Catch ex As Exception
            End Try
            Return True
        End SyncLock
    End Function

    ''' <summary>
    ''' Un APERCU (film, TV, satellite, IPTV) s'ouvre pendant que les reglages sont ouverts : il s'entend.
    ''' (Demande de Jacob, 13/10/2026 : "quand Form4 s'ouvre le son s'arrete, mais si j'ouvre le preview
    ''' le son doit reprendre dans tous les preview".)
    '''
    ''' Le silence des reglages ne concerne que ce qui jouait quand ils se sont ouverts. Mais l'interrupteur
    ''' "muet" de VLC est partage entre les lecteurs (mesure du 30/09, voir CouperLeLecteur) : celui laisse
    ''' par Couper pouvait garder l'aperçu muet, surtout quand sa sortie audio se cree apres le premier
    ''' reglage de volume (flux reseau, tuner). Ici :
    '''  - le lecteur de l'apercu n'est jamais retenu (retire de la liste des lecteurs coupes) ;
    '''  - son volume est redonne a 1,5, 4, 8 et 12 secondes, tant que l'apercu est ouvert et que le lecteur
    '''    joue. SetVolume coupe les autres lecteurs et regle celui-ci ; les lecteurs coupes a l'ouverture
    '''    des reglages ont leur piste audio desactivee et restent donc muets.
    ''' Au-dela de 12 secondes plus rien n'est redonne : un son coupe a la main dans l'apercu reste coupe.
    ''' Sans effet si les reglages ne sont pas ouverts.
    ''' </summary>
    Sub ApercuOuvert(fenetre As Form, lecteur As Vlc.DotNet.Forms.VlcControl, volume As Func(Of Integer))
        If fenetre Is Nothing OrElse lecteur Is Nothing OrElse volume Is Nothing Then Exit Sub
        If Not _actif Then Exit Sub
        SyncLock _coupes
            _coupes.RemoveAll(Function(c) ReferenceEquals(c.Lecteur, lecteur))
        End SyncLock
        Dim passage As Integer = 0
        Dim rappel As New System.Windows.Forms.Timer With {.Interval = 1500}
        AddHandler rappel.Tick,
            Sub()
                passage += 1
                Dim fin As Boolean = passage >= 4 OrElse fenetre.IsDisposed OrElse Not fenetre.Visible
                If fin Then
                    rappel.Stop()
                    rappel.Dispose()
                End If
                If fenetre.IsDisposed OrElse Not fenetre.Visible Then Exit Sub
                rappel.Interval = If(passage = 1, 2500, 4000)
                Try
                    Dim v As Integer = volume()
                    If v > 0 AndAlso lecteur.IsPlaying Then SetVolume(v, lecteur)
                Catch ex As Exception
                    DiagLog("Apercu (son redonne) : " & ex.Message)
                End Try
            End Sub
        rappel.Start()
        DiagLog("Apercu ouvert pendant les reglages : " & fenetre.Name & ", son redonne a l'apercu seul")
    End Sub

    ''' <summary>Muet et piste audio desactivee (comme CouperLeLecteur, sans son carnet de pistes).</summary>
    Private Sub Taire(lecteur As Vlc.DotNet.Forms.VlcControl)
        lecteur.Audio.IsMute = True
        Dim aucune = lecteur.Audio.Tracks.All.FirstOrDefault(Function(p) p.ID = -1)
        If aucune IsNot Nothing Then lecteur.Audio.Tracks.Current = aucune
    End Sub

    ''' <summary>Tous les lecteurs VLC des fenetres ouvertes (celles des PIP comprises).</summary>
    Private Function LecteursOuverts() As List(Of Vlc.DotNet.Forms.VlcControl)
        Dim lecteurs As New List(Of Vlc.DotNet.Forms.VlcControl)
        Try
            For Each f As Form In My.Application.OpenForms.Cast(Of Form)().ToList()
                Ajouter(f, lecteurs)
            Next
        Catch ex As Exception
            ' liste des fenetres modifiee pendant la lecture : on garde ceux deja trouves
        End Try
        Return lecteurs
    End Function

    Private Sub Ajouter(parent As Control, lecteurs As List(Of Vlc.DotNet.Forms.VlcControl))
        For Each c As Control In parent.Controls.Cast(Of Control)().ToList()
            Dim lecteur As Vlc.DotNet.Forms.VlcControl = TryCast(c, Vlc.DotNet.Forms.VlcControl)
            If lecteur IsNot Nothing Then lecteurs.Add(lecteur) Else Ajouter(c, lecteurs)
        Next
    End Sub

End Module
