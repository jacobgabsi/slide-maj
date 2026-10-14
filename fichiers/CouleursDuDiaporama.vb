Option Strict On
Option Explicit On

Imports System.Drawing.Imaging
Imports Vlc.DotNet.Forms

''' <summary>
''' Photos et videos du diaporama en NOIR ET BLANC (demande de Jacob, 13/10/2026 : "dans animation
''' photos et video, rajoute l'option Noir et Blanc").
'''
''' Une liste "Couleurs" dans chacun des deux cadres d'animation des reglages (onglets Photos et
''' Videos) : Normales, Noir et blanc, Au hasard (une photo ou une video sur quatre). Comme les autres
''' reglages de ces cadres, elle n'agit que si "Animer les photos" / "Animer les videos" est coche.
'''
''' Photos : l'image est mise en gris au moment ou elle est preparee pour l'animation
''' (AnimationPhoto.Preparer), par les poids habituels de la luminosite.
'''
''' Videos : c'est VLC qui dessine. Mesures du 13/10 avec le VLC de Slide :
'''  - des options posees sur la video elle-meme (":video-filter=adjust") sont ignorees ;
'''  - le reglage d'image du LECTEUR (saturation a zero) marche, est fait par la carte graphique
'''    (aucune image perdue, decodage materiel garde), et le fond flou comme le mouvement de la video
'''    (copies faites par Windows) suivent tout seuls.
''' Deux pieges de ce reglage, mesures aussi :
'''  - il appartient au lecteur, pas a la video : il resterait pour ce qu'on y jouerait ensuite (film,
'''    TV, annonce...) ;
'''  - VLC garde sa sortie video d'une lecture a l'autre, AVEC le reglage qu'elle avait, et un ordre
'''    donne entre deux lectures (avant que la nouvelle ait sa sortie video) ne l'atteint pas : la
'''    video suivante gardait les couleurs, ou le gris, de la precedente.
''' Le reglage voulu est donc retenu pour chaque lecteur, pose avant l'ouverture de chaque video du
''' diaporama (PourLaVideo), et REPOSE des que VLC annonce que la lecture a sa sortie video. Tout autre
''' contenu ouvert sur le meme lecteur le remet aux couleurs, de la meme facon.
''' </summary>
Friend Module CouleursDuDiaporama

    ''' <summary>Valeurs des listes "Couleurs" (My.Settings.AnimationPhotoCouleurs / AnimationVideoCouleurs).</summary>
    Friend Enum Couleurs
        Normales = 0
        NoirEtBlanc = 1
        AuHasard = 2
        Sepia = 3          ' (photos seulement, 26/10/2026 : "rajoute une version noir et blanc sepia" ; la liste des reglages la montre en 3e ligne)
    End Enum

    ''' <summary>Vrai si les photos animees sont en sepia : liste « Couleurs » sur « Sépia » (reglage 3).</summary>
    Friend Function PhotoEnSepia() As Boolean
        If Not AnimerLesPhotos Then Return False
        If AnimationPhotoCouleurs = Couleurs.Sepia Then Return True
        If AnimationMouvement <> 0 Then Return False          ' (le tirage au hasard ne joue que si le mouvement est « Au hasard »)
        SyncLock _hasard
            Return TirageAuHasard.SepiaTire(HasardPhotoMouvement, _hasard)
        End SyncLock
    End Function

    ''' <summary>Teinte sepia d'une image dessinee par GDI+ (matrice classique : brun chaud, transparence gardee).</summary>
    Friend Function MatriceDeSepia() As ColorMatrix
        Return New ColorMatrix(New Single()() {
            New Single() {0.393F, 0.349F, 0.272F, 0, 0},
            New Single() {0.769F, 0.686F, 0.534F, 0, 0},
            New Single() {0.189F, 0.168F, 0.131F, 0, 0},
            New Single() {0, 0, 0, 1, 0},
            New Single() {0, 0, 0, 0, 1}})
    End Function

    ''' <summary>"Au hasard" : part des photos et des videos montrees en noir et blanc.</summary>
    Private Const PartAuHasard As Double = 0.25

    Private ReadOnly _hasard As New Random()

    ''' <summary>Vrai si la prochaine photo animee doit etre en noir et blanc : liste « Couleurs » sur « Noir et blanc », ou
    ''' mouvement « Au hasard » et ligne « Noir et blanc » tiree dans le tableau des mouvements du hasard.</summary>
    Friend Function PhotoEnNoirEtBlanc() As Boolean
        If Not AnimerLesPhotos Then Return False
        If AnimationPhotoCouleurs = Couleurs.NoirEtBlanc Then Return True
        If AnimationMouvement <> 0 Then Return False
        SyncLock _hasard
            Return TirageAuHasard.NoirEtBlancTire(HasardPhotoMouvement, _hasard)
        End SyncLock
    End Function

    ''' <summary>Vrai si la prochaine video du diaporama doit etre en noir et blanc (meme regle que pour les photos).</summary>
    Friend Function VideoEnNoirEtBlanc() As Boolean
        If Not AnimerLesVideos Then Return False
        If AnimationVideoCouleurs = Couleurs.NoirEtBlanc Then Return True
        If AnimationVideoMouvement <> 0 Then Return False
        SyncLock _hasard
            Return TirageAuHasard.NoirEtBlancTire(HasardVideoMouvement, _hasard)
        End SyncLock
    End Function

    ''' <summary>Mise en gris d'une image dessinee par GDI+ : luminosite = 30 % du rouge, 59 % du vert,
    ''' 11 % du bleu ; la transparence est gardee.</summary>
    Friend Function MatriceDeGris() As ColorMatrix
        Return New ColorMatrix(New Single()() {
            New Single() {0.299F, 0.299F, 0.299F, 0, 0},
            New Single() {0.587F, 0.587F, 0.587F, 0, 0},
            New Single() {0.114F, 0.114F, 0.114F, 0, 0},
            New Single() {0, 0, 0, 1, 0},
            New Single() {0, 0, 0, 0, 1}})
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Videos : reglage d'image du lecteur VLC
    ' ------------------------------------------------------------------------------------------

    Private Class Suivi
        Public Voulu As Boolean                ' noir et blanc voulu pour ce qui joue (ou va jouer) sur ce lecteur
        Public Demande As Long                 ' heure (ms) de la derniere preparation : le changement de video qui suit est le notre
    End Class

    Private ReadOnly _suivis As New Dictionary(Of VlcControl, Suivi)
    Private _dejaDit As Boolean

    ''' <summary>A appeler JUSTE AVANT d'ouvrir une video du diaporama sur <paramref name="lecteur"/>
    ''' (SetMedia) : decide si elle sera en noir et blanc et regle le lecteur en consequence.</summary>
    Friend Sub PourLaVideo(lecteur As VlcControl)
        If lecteur Is Nothing Then Exit Sub
        Try
            Dim noirEtBlanc As Boolean = VideoEnNoirEtBlanc()
            Dim s As Suivi = Nothing
            SyncLock _suivis
                If Not _suivis.TryGetValue(lecteur, s) Then
                    ' Premier passage sur ce lecteur. Rien a suivre tant qu'il n'a jamais ete en noir et blanc.
                    If Not noirEtBlanc Then Exit Sub
                    s = New Suivi()
                    _suivis(lecteur) = s
                    ' (L'emetteur des evenements n'est pas le lecteur de Slide mais son moteur : le lecteur est retenu ici.)
                    Dim celuiCi As VlcControl = lecteur
                    AddHandler lecteur.MediaChanged, Sub(emetteur As Object, e As EventArgs) MediaChange(celuiCi)
                    AddHandler lecteur.VideoOutChanged, Sub(emetteur As Object, e As EventArgs) SortieVideoChangee(celuiCi)
                End If
                s.Voulu = noirEtBlanc
                s.Demande = Environment.TickCount64
            End SyncLock
            Poser(lecteur, noirEtBlanc)
            If noirEtBlanc AndAlso Not _dejaDit Then
                _dejaDit = True
                DiagLog("Videos en noir et blanc : en service (reglage d'image de VLC, saturation a zero)")
            End If
        Catch ex As Exception
            DiagLog("Couleurs de la video : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Donne l'ordre a VLC (fil de l'interface). Sans effet durable s'il arrive entre deux
    ''' lectures : il est redonne par SortieVideoChangee.</summary>
    Private Sub Poser(lecteur As VlcControl, noirEtBlanc As Boolean)
        Try
            If lecteur.IsDisposed Then Exit Sub
            If noirEtBlanc Then
                lecteur.Video.Adjustments.Enabled = True
                lecteur.Video.Adjustments.Saturation = 0
            Else
                ' Les deux : saturation normale, puis reglage retire.
                lecteur.Video.Adjustments.Saturation = 1
                lecteur.Video.Adjustments.Enabled = False
            End If
        Catch ex As Exception
            DiagLog("Couleurs de la video (reglage de VLC) : " & ex.Message)
        End Try
    End Sub

    Private Sub PoserDepuisVlc(lecteur As VlcControl, noirEtBlanc As Boolean)
        Try
            If lecteur.IsDisposed OrElse Not lecteur.IsHandleCreated Then Exit Sub
            lecteur.BeginInvoke(New Action(Sub() Poser(lecteur, noirEtBlanc)))
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Un contenu est ouvert sur un lecteur du diaporama (appele par VLC, dans un de ses fils).
    ''' Si ce n'est pas la video qu'on vient de preparer, c'est autre chose - film, TV, annonce... - qui
    ''' ne doit jamais heriter du noir et blanc : couleurs.</summary>
    Private Sub MediaChange(lecteur As VlcControl)
        Try
            Dim s As Suivi = Nothing
            SyncLock _suivis
                If Not _suivis.TryGetValue(lecteur, s) Then Exit Sub
                If s.Demande <> 0 AndAlso Environment.TickCount64 - s.Demande < 3000 Then
                    s.Demande = 0                              ' c'est la video du diaporama preparee a l'instant
                    Exit Sub
                End If
                If Not s.Voulu Then Exit Sub
                s.Voulu = False
            End SyncLock
            PoserDepuisVlc(lecteur, False)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>La lecture vient d'obtenir (ou de rendre) sa sortie video (appele par VLC) : c'est le
    ''' moment ou un ordre l'atteint a coup sur. Le reglage voulu est redonne.</summary>
    Private Sub SortieVideoChangee(lecteur As VlcControl)
        Try
            Dim voulu As Boolean
            SyncLock _suivis
                Dim s As Suivi = Nothing
                If Not _suivis.TryGetValue(lecteur, s) Then Exit Sub
                voulu = s.Voulu
            End SyncLock
            PoserDepuisVlc(lecteur, voulu)
        Catch ex As Exception
        End Try
    End Sub

End Module
