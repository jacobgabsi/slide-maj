Option Strict On
Option Explicit On

Imports System.IO

''' <summary>
''' Garde ouverte la connexion au partage du recepteur satellite (\\VUDUO2\Harddisk) pendant que
''' les reglages sont ouverts.
'''
''' Pourquoi (mesures du 09/10/2026, "le preview est tres lent a ouvrir la source \\VUDUO2") : le
''' recepteur ferme la connexion au partage des qu'elle reste une vingtaine de secondes sans
''' servir, et la rouvrir prend 12 a 18 secondes, quel que soit l'acces (File.Exists, liste du
''' dossier, ouverture du film par VLC). Chaque apercu d'un film du recepteur commencait donc par
''' cette attente, alors que le film lui-meme s'ouvre en moins d'une seconde. Tant qu'un fichier
''' du partage est tenu ouvert, la connexion n'est pas fermee (mesure : 62 ms apres 60 s).
'''
''' A l'ouverture des reglages, la connexion est donc etablie en tache de fond (l'attente n'est
''' plus subie au premier apercu) et un fichier est tenu ouvert, en lecture et sans rien lire,
''' jusqu'a leur fermeture. Rien n'est fait si la minuterie n'utilise pas le recepteur.
''' </summary>
Friend Module ConnexionRecepteur

    Private ReadOnly _verrou As New Object
    Private _fichierTenu As FileStream
    Private _voulue As Boolean

    ''' <summary>A l'ouverture des reglages (thread de l'interface) : rend la main tout de suite.</summary>
    Sub Garder()
        Dim utilise As Boolean = False
        Try
            utilise = RecepteurUtilise()
        Catch ex As Exception
        End Try
        If Not utilise Then Exit Sub
        SyncLock _verrou
            If _voulue Then Exit Sub
            _voulue = True
        End SyncLock
        Dim dossier As String = CStr(recepteurSat) & "Harddisk\movie"
        System.Threading.Tasks.Task.Run(Sub() Etablir(dossier))
    End Sub

    ''' <summary>A la fermeture des reglages : le fichier est rendu, le recepteur refermera la connexion.</summary>
    Sub Rendre()
        Dim f As FileStream
        SyncLock _verrou
            _voulue = False
            f = _fichierTenu
            _fichierTenu = Nothing
        End SyncLock
        Try
            If f IsNot Nothing Then f.Dispose()
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Le recepteur sert-il ? Son dossier par defaut est regle, ou une tache de la
    ''' minuterie joue un de ses films.</summary>
    Private Function RecepteurUtilise() As Boolean
        Dim recepteur As String = CStr(recepteurSat)
        If CStr(DossierDreamboxParDefault).IndexOf(recepteur, StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
        For Each ligne As DataGridViewRow In Form4.DataGridView1.Rows.Cast(Of DataGridViewRow)().Concat(Form4.DataGridViewTuya.Rows.Cast(Of DataGridViewRow)()).ToList()
            If Convert.ToString(ligne.Cells("Action").Value).IndexOf(recepteur, StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
        Next
        Return False
    End Function

    ''' <summary>En tache de fond : c'est ici que se passe l'attente de 12 a 18 s.</summary>
    Private Sub Etablir(dossier As String)
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Try
            Dim fichier As String = Directory.EnumerateFiles(dossier).FirstOrDefault()
            If fichier Is Nothing Then
                DiagLog("Recepteur : dossier " & dossier & " vide, connexion non gardee")
                Exit Sub
            End If
            ' Lecture seule, tout partage : le recepteur et les autres programmes restent libres
            ' de lire, d'ecrire ou de supprimer ce fichier.
            Dim f As New FileStream(fichier, FileMode.Open, FileAccess.Read, FileShare.ReadWrite Or FileShare.Delete)
            Dim garde As Boolean = False
            SyncLock _verrou
                If _voulue AndAlso _fichierTenu Is Nothing Then _fichierTenu = f : garde = True
            End SyncLock
            If Not garde Then f.Dispose()                  ' reglages deja refermes entre-temps
            DiagLog("Recepteur : connexion au partage etablie en " & chrono.ElapsedMilliseconds & " ms" &
                    If(garde, ", gardee ouverte tant que les reglages le sont", ""))
        Catch ex As Exception
            DiagLog("Recepteur : connexion au partage impossible apres " & chrono.ElapsedMilliseconds & " ms (" & ex.Message & ")")
        End Try
    End Sub

End Module
