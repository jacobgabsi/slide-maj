Option Strict On
Option Explicit On

Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Core

''' <summary>
''' La fenetre du lecteur Deezer integre a Slide : un navigateur (WebView2) ouvert sur
''' www.deezer.com, et une barre en bas (etat, retour a l'accueil, masquer). Voir MusiqueDeezer,
''' qui la cree et la commande.
'''
''' "Masquee", elle n'est pas cachee mais posee HORS DE L'ECRAN, sans jamais prendre le clavier.
''' Mesure du 07/10/2026 sur le site : dans une fenetre vraiment cachee (jamais montree), la page
''' se dit "hidden" et le lecteur de Deezer ne DEMARRE pas la lecture ; affichee hors de l'ecran,
''' elle se dit "visible" et tout marche (lecture, morceau suivant...).
''' Elle appartient a la fenetre de Slide (Owner) : elle n'apparait donc ni dans la barre des
''' taches ni dans Alt+Tab, et, montree, elle reste devant Slide.
'''
''' On la montre pour choisir une musique ou finir une connexion ; la croix la masque seulement.
''' Les controles sont dans le Concepteur.
''' </summary>
Public Class FormDeezer

    Private Shared ReadOnly HorsEcran As New Point(-32000, -32000)

    Private _preparation As Task
    Private _pourDeBon As Boolean
    Private _masquee As Boolean = True
    Private _place As Rectangle = Rectangle.Empty       ' ou elle etait quand on l'a masquee
    Private _message As String = ""

    ''' <summary>Vrai quand le navigateur est en service (les commandes peuvent partir).</summary>
    Friend Property NavigateurPret As Boolean
    ''' <summary>Pourquoi le navigateur n'a pas pu demarrer ("" si tout va bien).</summary>
    Friend Property Erreur As String = ""
    Friend Property ErreurDite As Boolean

    ''' <summary>Vrai si la fenetre est a l'ecran (et non posee hors de l'ecran).</summary>
    Friend ReadOnly Property EstMontree As Boolean
        Get
            Return Not _masquee
        End Get
    End Property

    ''' <summary>Affichee sans prendre le clavier (elle est posee hors de l'ecran au depart).</summary>
    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    ''' <summary>Dossier ou le navigateur garde la connexion au compte Deezer : sur CE PC (un
    ''' dossier du reseau n'est pas accepte par WebView2, et Slide est parfois lance du reseau).</summary>
    Friend Shared Function DossierDuNavigateur() As String
        Return IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Slide", "Deezer")
    End Function

    ''' <summary>Pose la fenetre hors de l'ecran, met le navigateur en service et ouvre Deezer.
    ''' Peut etre appelee plusieurs fois.</summary>
    Friend Sub Preparer()
        If _preparation IsNot Nothing Then Exit Sub
        RelireLaPlace()
        Me.StartPosition = FormStartPosition.Manual
        Me.Location = HorsEcran
        Try
            Dim principale As Form = FenetrePrincipale()
            If principale IsNot Nothing AndAlso principale IsNot Me AndAlso Not principale.IsDisposed Then Me.Owner = principale
        Catch ex As Exception
        End Try
        Me.Show()
        _preparation = Initialiser()
    End Sub

    Private Async Function Initialiser() As Task
        Try
            ' Lecture lancee par Slide, sans clic dans la page : le navigateur doit l'accepter.
            Dim options As New CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required")
            Dim milieu As CoreWebView2Environment = Await CoreWebView2Environment.CreateAsync(Nothing, DossierDuNavigateur(), options)
            Await WebView21.EnsureCoreWebView2Async(milieu)
            Dim page As CoreWebView2 = WebView21.CoreWebView2
            ' Le navigateur est un programme a part : lie a Slide, il s'arrete avec lui, quelle que soit
            ' la facon dont Slide se termine (voir ProcessusLie). Sans cela, la musique continuait un
            ' bon moment apres la fermeture de Slide.
            Dim lie As Boolean = ProcessusLie.Lier(CInt(page.BrowserProcessId), "Deezer")
            page.IsMuted = MusiqueDeezer.Muet
            page.Settings.IsStatusBarEnabled = False
            ' Slide remplit lui-meme la page de connexion : le navigateur n'a pas a proposer
            ' d'enregistrer le mot de passe de son cote.
            page.Settings.IsPasswordAutosaveEnabled = False
            Await page.AddScriptToExecuteOnDocumentCreatedAsync(MusiqueDeezer.ScriptDAide)
            AddHandler page.ProcessFailed, AddressOf NavigateurEnPanne
            page.Navigate(MusiqueDeezer.PremiereAdresse())      ' la playlist voulue, sinon l'accueil
            NavigateurPret = True
            DiagLog("Deezer : navigateur en service (WebView2 " & milieu.BrowserVersionString & ", dossier " & DossierDuNavigateur() & If(lie, ", lie a Slide", ", NON lie a Slide") & ")")
        Catch ex As Exception
            ' WebView2 absent de ce PC (il est livre avec Windows 11 et avec Edge), dossier refuse...
            Erreur = ex.Message
        End Try
    End Function

    ''' <summary>Fait executer un petit programme par la page et renvoie sa reponse (en JSON).</summary>
    Friend Async Function Executer(script As String) As Task(Of String)
        If Not NavigateurPret OrElse Me.IsDisposed OrElse WebView21.CoreWebView2 Is Nothing Then Return ""
        Return Await WebView21.CoreWebView2.ExecuteScriptAsync(script)
    End Function

    ''' <summary>Ouvre une page de Deezer (une playlist, un album...) dans le lecteur.</summary>
    Friend Sub Aller(adresse As String)
        Try
            If NavigateurPret Then WebView21.CoreWebView2.Navigate(adresse)
        Catch ex As Exception
            DiagLog("Deezer : adresse refusee (" & adresse & ") : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Recharge le lecteur apres une panne (page restee sans lecteur, lecture figee) :
    ''' sur la page de la playlist voulue, sinon l'accueil. MusiqueDeezer relance la musique.</summary>
    Friend Sub RechargerLaMusique()
        Try
            If NavigateurPret Then WebView21.CoreWebView2.Navigate(MusiqueDeezer.PremiereAdresse())
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub Recharger()
        Try
            If NavigateurPret Then WebView21.CoreWebView2.Navigate(MusiqueDeezer.Accueil)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Le navigateur (ou la page) s'est arrete : MusiqueDeezer le rouvre.</summary>
    Private Sub NavigateurEnPanne(sender As Object, e As CoreWebView2ProcessFailedEventArgs)
        Try
            Select Case e.ProcessFailedKind
                Case CoreWebView2ProcessFailedKind.BrowserProcessExited, CoreWebView2ProcessFailedKind.RenderProcessExited,
                     CoreWebView2ProcessFailedKind.RenderProcessUnresponsive
                    If _pourDeBon Then Exit Sub
                    NavigateurPret = False
                    Dim sorte As String = e.ProcessFailedKind.ToString()
                    Me.BeginInvoke(New Action(Sub() MusiqueDeezer.Rouvrir("navigateur arrete (" & sorte & ")")))
                Case Else
                    DiagLog("Deezer : incident du navigateur (" & e.ProcessFailedKind.ToString() & ")")
            End Select
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Montrer / masquer
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Amene la fenetre a l'ecran, devant Slide, avec <paramref name="message"/> dans
    ''' la barre du bas.</summary>
    Friend Sub Montrer(message As String)
        Dire(message)
        If _masquee Then
            Dim ecran As Rectangle = Screen.PrimaryScreen.WorkingArea
            Try
                If Me.Owner IsNot Nothing Then ecran = Screen.FromControl(Me.Owner).WorkingArea
            Catch ex As Exception
            End Try
            Dim taille As Size = If(_place.IsEmpty, Me.Size, _place.Size)
            taille = New Size(Math.Min(taille.Width, ecran.Width), Math.Min(taille.Height, ecran.Height))
            Dim place As New Rectangle(ecran.Left + (ecran.Width - taille.Width) \ 2, ecran.Top + (ecran.Height - taille.Height) \ 2, taille.Width, taille.Height)
            ' La ou elle etait la derniere fois, si c'est encore un endroit visible.
            If Not _place.IsEmpty AndAlso Screen.AllScreens.Any(Function(s) s.WorkingArea.Contains(New Rectangle(_place.Left, _place.Top, Math.Min(200, _place.Width), 40))) Then
                place = New Rectangle(_place.Location, taille)
            End If
            _masquee = False
            Me.Bounds = place
            If _agrandie Then Me.WindowState = FormWindowState.Maximized
        End If
        If Not Me.Visible Then Me.Show()
        ResterUtilisable()
        Me.Activate()
    End Sub

    ''' <summary>
    ''' La fenetre des reglages de Slide est ouverte "en modal" (Form4.ShowDialog) : Windows
    ''' desactive alors toutes les autres fenetres de Slide, dont celle-ci, qui existe deja hors
    ''' de l'ecran. Montree par le bouton "Ouvrir Deezer..." des reglages, elle ne repondait ni a
    ''' la souris ni au clavier (Jacob, 08/10). On la reactive : c'est justement depuis les
    ''' reglages qu'on vient y choisir sa musique. (Appelee aussi toutes les deux secondes, pour le
    ''' cas ou les reglages sont ouverts APRES elle.)
    ''' </summary>
    Private Sub ResterUtilisable()
        Try
            If Not _masquee AndAlso Me.IsHandleCreated AndAlso Not IsWindowEnabled(Me.Handle) Then EnableWindow(Me.Handle, True)
        Catch ex As Exception
        End Try
    End Sub

    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function IsWindowEnabled(fenetre As IntPtr) As Boolean
    End Function

    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function EnableWindow(fenetre As IntPtr, active As Boolean) As Boolean
    End Function

    ''' <summary>Repose la fenetre hors de l'ecran : la musique continue.</summary>
    Friend Sub Masquer()
        If _masquee Then Exit Sub
        _agrandie = (Me.WindowState = FormWindowState.Maximized)
        If Me.WindowState <> FormWindowState.Normal Then Me.WindowState = FormWindowState.Normal
        _place = Me.Bounds
        RetenirLaPlace()
        _masquee = True
        _message = ""
        Me.Location = HorsEcran
        ' Le clavier revient a la fenetre d'ou l'on venait : les reglages s'ils sont ouverts (en
        ' modal, ils sont alors la seule fenetre de Slide qui reponde), sinon l'image de Slide.
        Try
            Dim retour As Form = Application.OpenForms.Cast(Of Form)().LastOrDefault(Function(x) x IsNot Me AndAlso x.Modal AndAlso x.Visible)
            If retour Is Nothing Then retour = Me.Owner
            If retour IsNot Nothing AndAlso retour.Visible AndAlso IsWindowEnabled(retour.Handle) Then retour.Activate()
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Taille et place de la fenetre, retenues d'un lancement de Slide a l'autre (demande de
    ' Jacob, 08/10 : "retiens en memoire la taille de la fenetre Deezer"). Reglage
    ' My.Settings.DeezerFenetre : "gauche;haut;largeur;hauteur;agrandie". Enregistre quand on la
    ' masque et quand on finit de la deplacer ou de la redimensionner ; relu a l'ouverture du
    ' lecteur. Montrer() la remet la, si c'est encore un endroit visible (ecran debranche...).
    ' ------------------------------------------------------------------------------------------

    Private _agrandie As Boolean                        ' elle etait en plein ecran (agrandie) quand on l'a masquee

    Private Sub RelireLaPlace()
        Try
            Dim parties As String() = If(My.Settings.DeezerFenetre, "").Split(";"c)
            If parties.Length < 4 Then Exit Sub
            Dim n(3) As Integer
            For i As Integer = 0 To 3
                If Not Integer.TryParse(parties(i), n(i)) Then Exit Sub
            Next
            If n(2) < Me.MinimumSize.Width OrElse n(3) < Me.MinimumSize.Height Then Exit Sub
            _place = New Rectangle(n(0), n(1), n(2), n(3))
            _agrandie = parties.Length > 4 AndAlso parties(4) = "1"
        Catch ex As Exception
        End Try
    End Sub

    Private Sub RetenirLaPlace()
        Try
            If _place.IsEmpty Then Exit Sub
            Dim texte As String = _place.Left & ";" & _place.Top & ";" & _place.Width & ";" & _place.Height & ";" & If(_agrandie, "1", "0")
            If My.Settings.DeezerFenetre = texte Then Exit Sub
            My.Settings.DeezerFenetre = texte
            My.Settings.Save()
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>L'utilisateur vient de deplacer ou de redimensionner la fenetre : sa place est
    ''' retenue tout de suite (Slide peut etre ferme alors qu'elle est encore a l'ecran).</summary>
    Private Sub FormDeezer_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        If _masquee OrElse Me.WindowState <> FormWindowState.Normal Then Exit Sub
        _place = Me.Bounds
        _agrandie = False
        RetenirLaPlace()
    End Sub

    ''' <summary>Appelee toutes les deux secondes : Windows ramene parfois a l'ecran les fenetres
    ''' qui en sont sorties (ecran debranche, TV eteinte puis rallumee) ; masquee, elle y retourne.</summary>
    Friend Sub TenirEnPlace()
        ResterUtilisable()
        Try
            If _masquee AndAlso Me.Visible AndAlso Me.WindowState = FormWindowState.Normal AndAlso Me.Location <> HorsEcran Then Me.Location = HorsEcran
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Message de la barre du bas, garde jusqu'a ce que la musique joue.</summary>
    Friend Sub Dire(message As String)
        If message = "" Then Exit Sub
        _message = message
        LabelEtat.Text = message
    End Sub

    ''' <summary>Barre du bas : ce que joue Deezer, ou ce qu'on attend de l'utilisateur.</summary>
    Friend Sub Afficher(etat As MusiqueDeezer.EtatDuLecteur)
        If _masquee Then Exit Sub
        Dim texte As String
        If _message <> "" AndAlso (Now < _messageJusqua OrElse Not (etat.Pret AndAlso etat.Connecte AndAlso etat.Joue)) Then
            texte = _message
        ElseIf Not etat.Pret Then
            texte = If(etat.Formulaire, "Connexion à Deezer.", "Chargement de Deezer...")
        ElseIf Not etat.Connecte Then
            texte = "Aucun compte Deezer n'est connecté."
        ElseIf etat.Joue Then
            _message = ""
            texte = "Lecture : " & MusiqueDeezer.MorceauEnCours() & ". Cette fenêtre peut être masquée, la musique continue."
        Else
            texte = "En pause" & If(MusiqueDeezer.MorceauEnCours() = "", "", " : " & MusiqueDeezer.MorceauEnCours()) & ". Cette fenêtre peut être masquée."
        End If
        If LabelEtat.Text <> texte Then LabelEtat.Text = texte
    End Sub

    Private Sub ButtonMasquer_Click(sender As Object, e As EventArgs) Handles ButtonMasquer.Click
        Masquer()
    End Sub

    ''' <summary>"Jouer ceci au demarrage" : ce que le lecteur joue en ce moment (playlist, album,
    ''' Flow ou une de ses variantes) devient la playlist des reglages de Slide. Voir
    ''' MusiqueDeezer.RetenirLaMusiqueEnCours.</summary>
    Private Sub ButtonRetenir_Click(sender As Object, e As EventArgs) Handles ButtonRetenir.Click
        ' (Affiche douze secondes : sinon "Lecture : ..." le remplacerait aussitot.)
        _messageJusqua = Now.AddSeconds(12)
        Dire(MusiqueDeezer.RetenirLaMusiqueEnCours())
    End Sub

    Private _messageJusqua As Date                      ' le message de la barre reste au moins jusque-la

    Private Sub ButtonAccueil_Click(sender As Object, e As EventArgs) Handles ButtonAccueil.Click
        MusiqueDeezer.RechargementVoulu()       ' (page rechargee expres : pas de relance de la playlist)
        Recharger()
    End Sub

    ''' <summary>Oublie le compte connecte dans le navigateur de Slide (ses temoins de connexion - cookies -
    ''' et ce que le site a garde) puis rouvre l'accueil de Deezer, deconnecte. <paramref name="suite"/>
    ''' est appelee ensuite avec True si c'est fait, TOUJOURS dans le fil de la fenetre (le seul qui ait
    ''' le droit de toucher au navigateur et aux minuteurs : apres une attente, on n'y est pas forcement
    ''' revenu - vu au banc du 08/10/2026).</summary>
    Friend Async Sub OublierLeCompte(suite As Action(Of Boolean))
        Dim fait As Boolean = False
        Try
            If NavigateurPret AndAlso Not Me.IsDisposed AndAlso WebView21.CoreWebView2 IsNot Nothing Then
                Await WebView21.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.Cookies Or CoreWebView2BrowsingDataKinds.AllDomStorage)
                fait = True
            End If
        Catch ex As Exception
            DiagLog("Deezer : le compte en place n'a pas pu etre oublie (" & ex.Message & ")")
        End Try
        Try
            Dim apres As New Action(Sub()
                                        Try
                                            If fait AndAlso Not Me.IsDisposed AndAlso WebView21.CoreWebView2 IsNot Nothing Then WebView21.CoreWebView2.Navigate(MusiqueDeezer.Accueil)
                                        Catch ex As Exception
                                            DiagLog("Deezer : accueil non rouvert apres l'oubli du compte (" & ex.Message & ")")
                                        End Try
                                        suite(fait)
                                    End Sub)
            If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Exit Sub
            If Me.InvokeRequired Then Me.BeginInvoke(apres) Else apres()
        Catch ex As Exception
            DiagLog("Deezer (oubli du compte) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>"Changer de compte" : le compte en place est deconnecte, et Slide se connecte avec
    ''' l'identifiant des reglages (onglet Audio). Sans identifiant dans les reglages, la page de
    ''' connexion de Deezer reste affichee pour se connecter a la main.</summary>
    Private Sub ButtonCompte_Click(sender As Object, e As EventArgs) Handles ButtonCompte.Click
        _messageJusqua = Now.AddSeconds(12)
        Dire(If(DeezerLogin = "", "Déconnexion du compte en place…", "Déconnexion du compte en place, puis connexion avec " & DeezerLogin & "…"))
        MusiqueDeezer.ChangerDeCompte()
    End Sub

    ''' <summary>La croix masque la fenetre : la musique continue. Seul MusiqueDeezer.Fermer la
    ''' ferme vraiment (et arrete Deezer) ; elle se ferme aussi avec Slide.</summary>
    Private Sub FormDeezer_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If Not _pourDeBon AndAlso e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Masquer()
        ElseIf Not _masquee Then
            ' Fermee avec Slide alors qu'elle est a l'ecran : sa place est retenue quand meme.
            _agrandie = (Me.WindowState = FormWindowState.Maximized)
            _place = If(Me.WindowState = FormWindowState.Normal, Me.Bounds, Me.RestoreBounds)
            RetenirLaPlace()
        End If
    End Sub

    ''' <summary>Coupe le son du lecteur a l'instant (Slide se ferme).</summary>
    Friend Sub Taire()
        Try
            If NavigateurPret AndAlso Not Me.IsDisposed AndAlso WebView21.CoreWebView2 IsNot Nothing Then WebView21.CoreWebView2.IsMuted = True
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub FermerPourDeBon()
        _pourDeBon = True
        NavigateurPret = False
        Me.Close()
        Me.Dispose()
    End Sub

    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub FormDeezer_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
