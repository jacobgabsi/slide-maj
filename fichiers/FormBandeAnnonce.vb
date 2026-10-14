Imports Microsoft.Web.WebView2.Core

''' <summary>
''' Bande-annonce d'un film de Nflix, jouee DANS une fenetre de Slide (20/11/2026) : ouverte dans le navigateur par defaut, elle passait derriere la fenetre Nflix
''' (et derriere le diaporama) et on ne la voyait pas. Un navigateur integre (WebView2, comme le lecteur Deezer) la montre devant, au-dessus de Nflix ; fermer la
''' fenetre arrete la video. Si WebView2 est absent du PC, la bande-annonce s'ouvre dans le navigateur comme avant.
''' </summary>
Public Class FormBandeAnnonce

    Private _adresse As String = ""

    Public Sub New()
        InitializeComponent()          ' (constructeur sans parametre : requis par le Concepteur)
    End Sub

    ''' <summary>Joue la bande-annonce <paramref name="adresse"/> (page YouTube) dans une fenetre au-dessus de <paramref name="proprietaire"/>. Rend Faux si le navigateur integre est indisponible.</summary>
    Friend Shared Function Jouer(proprietaire As IWin32Window, titre As String, adresse As String) As Boolean
        Using f As New FormBandeAnnonce()
            f._adresse = adresse
            f.Text = "Bande-annonce - " & titre
            f.ShowDialog(proprietaire)
            Return Not f._indisponible
        End Using
    End Function

    Private _indisponible As Boolean

    ' WebView2 refuse un dossier de donnees sur le reseau : dossier local, a part de celui de Deezer (options differentes).
    Private Shared Function DossierDuNavigateur() As String
        Return IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Slide", "WebView2Trailer")
    End Function

    ' 20/11/2026 : la fenetre (modale) pouvait rester INVISIBLE derriere les photos : plus rien ne se cliquait, des bips. Elle se met donc devant toutes les fenetres de Slide,
    ' se ferme avec Echap, et se referme toute seule (navigateur par defaut a la place) si la page n'est pas arrivee au bout de 20 secondes.
    Private _delai As System.Windows.Forms.Timer
    Private _chargee As Boolean

    Private Sub AmenerDevant()
        Try
            If IsDisposed Then Exit Sub
            If WindowState = FormWindowState.Minimized Then WindowState = FormWindowState.Normal
            TopMost = True
            BringToFront()
            Activate()
        Catch ex As Exception
        End Try
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then Close() : Return True
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Async Sub FormBandeAnnonce_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        AmenerDevant()
        _delai = New System.Windows.Forms.Timer With {.Interval = 20000}
        AddHandler _delai.Tick, Sub()
                                    _delai.Stop()
                                    If Not _chargee AndAlso Not IsDisposed Then
                                        DiagLog("Bande-annonce : la page n'est pas arrivee en 20 s -> navigateur par defaut")
                                        _indisponible = True
                                        Close()
                                    End If
                                End Sub
        _delai.Start()
        Dim relance As New System.Windows.Forms.Timer With {.Interval = 700}
        AddHandler relance.Tick, Sub()
                                     relance.Stop()
                                     relance.Dispose()
                                     AmenerDevant()
                                 End Sub
        relance.Start()
        Try
            IO.Directory.CreateDirectory(DossierDuNavigateur())
            Dim options As New CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required")
            Dim milieu As CoreWebView2Environment = Await CoreWebView2Environment.CreateAsync(Nothing, DossierDuNavigateur(), options)
            Await WebView21.EnsureCoreWebView2Async(milieu)
            If IsDisposed Then Exit Sub
            AddHandler WebView21.CoreWebView2.NavigationCompleted, Sub(s As Object, a As CoreWebView2NavigationCompletedEventArgs)
                                                                       If a.IsSuccess Then _chargee = True
                                                                   End Sub
            WebView21.CoreWebView2.Navigate(_adresse)
            DiagLog("Bande-annonce : " & _adresse)
        Catch ex As Exception
            DiagLog("Bande-annonce : navigateur integre indisponible (" & ex.Message & ") -> navigateur par defaut")
            _indisponible = True
            Close()
        End Try
    End Sub

    Private Sub FormBandeAnnonce_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            If _delai IsNot Nothing Then _delai.Stop() : _delai.Dispose() : _delai = Nothing
        Catch ex As Exception
        End Try
        Try
            ' La video s'arrete avec la fenetre.
            If WebView21.CoreWebView2 IsNot Nothing Then WebView21.CoreWebView2.Navigate("about:blank")
        Catch ex As Exception
        End Try
    End Sub

End Class
