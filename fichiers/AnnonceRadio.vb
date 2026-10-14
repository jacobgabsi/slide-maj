Option Strict On
Option Explicit On

Imports System.Net.Http
Imports System.Text
Imports System.Threading.Tasks

''' <summary>
''' Ce qu'une radio sur Internet annonce : son nom et le morceau en cours ("Artiste - Titre"), pour
''' la carte affichee sur l'image de Slide (CarteDeezer, demande de Jacob du 08/10/2026).
'''
''' VLC 3 ne donne pas ces annonces pour une adresse en https (mesure le 08/10/2026 sur la radio
''' des reglages : rien en 20 secondes, alors que la station les envoie). Slide les lit donc lui-meme :
''' une courte connexion a la station toutes les quinze secondes (quelques kilo-octets), qui demande
''' les annonces ("Icy-MetaData: 1"), lit l'en-tete "icy-name" puis le premier bloc d'annonce, et
''' raccroche. Rien n'est joue par cette connexion : le son reste celui de VLC.
''' </summary>
Friend Module AnnonceRadio

    Private Const IntervalleSecondes As Double = 15
    Private Const AttenteMaxiSecondes As Double = 8

    Private ReadOnly _client As New HttpClient(New HttpClientHandler With {.AllowAutoRedirect = True}) With {.Timeout = TimeSpan.FromSeconds(AttenteMaxiSecondes)}
    Private ReadOnly _verrou As New Object
    Private _adresse As String = ""
    Private _nom As String = ""
    Private _titre As String = ""
    Private _luLe As Date = Date.MinValue
    Private _enCours As Boolean

    ''' <summary>Nom de la station (en-tete "icy-name"), "" tant qu'il n'est pas connu.</summary>
    Friend Function NomDeLaStation(adresse As String) As String
        SyncLock _verrou
            Return If(adresse = _adresse, _nom, "")
        End SyncLock
    End Function

    ''' <summary>Morceau annonce par la station, "" si elle n'annonce rien (ou pas encore lu).</summary>
    Friend Function TitreAnnonce(adresse As String) As String
        SyncLock _verrou
            Return If(adresse = _adresse, _titre, "")
        End SyncLock
    End Function

    ''' <summary>A appeler regulierement pendant que la radio joue : relance une lecture des annonces
    ''' quand la derniere a plus de quinze secondes. Ne fait jamais attendre.</summary>
    Friend Sub SuivreLaStation(adresse As String)
        If String.IsNullOrWhiteSpace(adresse) Then Exit Sub
        SyncLock _verrou
            If adresse <> _adresse Then
                _adresse = adresse : _nom = "" : _titre = "" : _luLe = Date.MinValue
            End If
            If _enCours OrElse (Now - _luLe).TotalSeconds < IntervalleSecondes Then Exit Sub
            _enCours = True
        End SyncLock
        Task.Run(Async Function()
                     Dim nomLu As String = "", titreLu As String = ""
                     Dim reussi As Boolean = False
                     Try
                         Dim lu As (Nom As String, Titre As String) = Await LireLaStation(adresse)
                         nomLu = lu.Nom : titreLu = lu.Titre
                         reussi = True
                     Catch ex As Exception
                         ' station injoignable, ou qui n'annonce rien de cette facon : pas d'annonce
                     End Try
                     SyncLock _verrou
                         If adresse = _adresse Then
                             If reussi Then
                                 If nomLu <> "" Then _nom = nomLu
                                 _titre = titreLu
                             End If
                             _luLe = Now
                         End If
                         _enCours = False
                     End SyncLock
                 End Function)
    End Sub

    ''' <summary>Une connexion a la station : son nom, et son premier bloc d'annonce.</summary>
    Friend Async Function LireLaStation(adresse As String) As Task(Of (Nom As String, Titre As String))
        Using demande As New HttpRequestMessage(HttpMethod.Get, adresse)
            demande.Headers.TryAddWithoutValidation("Icy-MetaData", "1")
            demande.Headers.TryAddWithoutValidation("User-Agent", "Slide")
            Using limite As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(AttenteMaxiSecondes)),
                  reponse As HttpResponseMessage = Await _client.SendAsync(demande, HttpCompletionOption.ResponseHeadersRead, limite.Token)
                reponse.EnsureSuccessStatusCode()
                Dim nom As String = EnTete(reponse, "icy-name")
                Dim pas As Integer
                If Not Integer.TryParse(EnTete(reponse, "icy-metaint"), pas) OrElse pas <= 0 OrElse pas > 1000000 Then Return (nom, "")
                Using flux As IO.Stream = Await reponse.Content.ReadAsStreamAsync(limite.Token)
                    ' "pas" octets de son, puis un octet qui donne la longueur de l'annonce (x 16).
                    Dim son(pas - 1) As Byte
                    Await LireTout(flux, son, pas, limite.Token)
                    Dim un(0) As Byte
                    Await LireTout(flux, un, 1, limite.Token)
                    Dim longueur As Integer = un(0) * 16
                    If longueur = 0 Then Return (nom, "")
                    Dim bloc(longueur - 1) As Byte
                    Await LireTout(flux, bloc, longueur, limite.Token)
                    Return (nom, TitreDuBloc(Decoder(bloc)))
                End Using
            End Using
        End Using
    End Function

    Private Function EnTete(reponse As HttpResponseMessage, nom As String) As String
        Dim valeurs As IEnumerable(Of String) = Nothing
        If reponse.Headers.TryGetValues(nom, valeurs) OrElse reponse.Content.Headers.TryGetValues(nom, valeurs) Then
            ' (Les en-tetes arrivent lus en Latin-1 : un nom en UTF-8 est remis d'aplomb.)
            Return Decoder(Encoding.Latin1.GetBytes(If(valeurs.FirstOrDefault(), ""))).Trim()
        End If
        Return ""
    End Function

    Private Async Function LireTout(flux As IO.Stream, tampon As Byte(), combien As Integer, limite As Threading.CancellationToken) As Task
        Dim lus As Integer = 0
        Do While lus < combien
            Dim n As Integer = Await flux.ReadAsync(tampon.AsMemory(lus, combien - lus), limite)
            If n <= 0 Then Throw New IO.EndOfStreamException()
            lus += n
        Loop
    End Function

    ''' <summary>UTF-8 si le texte en est, sinon Latin-1 (les deux se rencontrent).</summary>
    Private Function Decoder(octets As Byte()) As String
        Try
            Return New UTF8Encoding(False, True).GetString(octets)
        Catch ex As Exception
            Return Encoding.Latin1.GetString(octets)
        End Try
    End Function

    ''' <summary>Le titre dans un bloc "StreamTitle='Artiste - Titre';StreamUrl='';".</summary>
    Friend Function TitreDuBloc(bloc As String) As String
        Dim debut As Integer = bloc.IndexOf("StreamTitle='", StringComparison.OrdinalIgnoreCase)
        If debut < 0 Then Return ""
        debut += "StreamTitle='".Length
        Dim fin As Integer = bloc.IndexOf("';", debut, StringComparison.Ordinal)
        If fin < 0 Then fin = bloc.IndexOf(ChrW(0), debut)
        If fin < 0 Then fin = bloc.Length
        Return bloc.Substring(debut, fin - debut).Trim(" "c, ChrW(0))
    End Function

End Module
