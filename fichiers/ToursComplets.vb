Imports System.Globalization
Imports System.IO

''' <summary>
''' Compteurs « tours complets » (22/11/2026) : combien de fois TOUTES les photos, et TOUTES les videos, ont ete vues (la liste est alors remise a zero par CaseAcocherVide).
''' Affiches dans l'onglet Videos, cadre « Deja vu ». Propres a ce poste : data\settings\ToursComplets.txt (« photos;videos;date des derniers tours »).
''' </summary>
Friend Module ToursComplets

    Private _photos As Integer
    Private _videos As Integer
    Private _dernierPhotos As String = ""
    Private _dernierVideos As String = ""
    Private _charge As Boolean
    ' La remise a zero automatique arrive des qu'aucune photo n'est plus trouvable (parfois 99 %) : un tour compte a partir de 97 % vues.
    Private Const SeuilDuTour As Double = 0.97

    Private Function Fichier() As String
        Return Path.Combine(DossierReglages, "ToursComplets.txt")
    End Function

    Private Sub Charger()
        If _charge Then Exit Sub
        _charge = True
        Try
            If Not File.Exists(Fichier()) Then Exit Sub
            Dim p As String() = File.ReadAllText(Fichier()).Trim().Split(";"c)
            If p.Length >= 2 Then
                _photos = CInt(Val(p(0)))
                _videos = CInt(Val(p(1)))
            End If
            If p.Length >= 4 Then _dernierPhotos = p(2) : _dernierVideos = p(3)
        Catch ex As Exception
            DiagLog("Tours complets : lecture impossible (" & ex.Message & ")")
        End Try
    End Sub

    Private Sub Enregistrer()
        Try
            File.WriteAllText(Fichier(), _photos & ";" & _videos & ";" & _dernierPhotos & ";" & _dernierVideos)
        Catch ex As Exception
            DiagLog("Tours complets : ecriture impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Vrai si TOUTES les lignes de la liste sont cochees (« Check »), donc si la remise a zero qui suit termine un tour. Une remise a zero voulue en cours de tour ne compte pas.</summary>
    Private Function ToutEstVu(grille As DataGridView) As Boolean
        Try
            Dim table As DataTable = Nothing
            If TypeOf grille.DataSource Is DataTable Then
                table = DirectCast(grille.DataSource, DataTable)
            ElseIf TypeOf grille.DataSource Is DataView Then
                table = DirectCast(grille.DataSource, DataView).Table
            End If
            If table Is Nothing Then
                ' Liste non liee (apres un scan complet) : case a cocher = 1re colonne.
                If grille.Columns.Count = 0 Then Return False
                Dim n As Integer = 0, vus As Integer = 0
                For Each l As DataGridViewRow In grille.Rows
                    If l.IsNewRow Then Continue For
                    n += 1
                    Dim v As Object = l.Cells(0).Value
                    If TypeOf v Is Boolean AndAlso CBool(v) Then vus += 1
                Next
                Return n > 0 AndAlso vus >= n * SeuilDuTour
            End If
            If Not table.Columns.Contains("Check") OrElse table.Rows.Count = 0 Then Return False
            Dim total As Integer = 0, vusT As Integer = 0
            For Each ligne As DataRow In table.Rows
                If ligne.RowState = DataRowState.Deleted Then Continue For
                total += 1
                If Not ligne.IsNull("Check") AndAlso CBool(ligne("Check")) Then vusT += 1
            Next
            Return total > 0 AndAlso vusT >= total * SeuilDuTour
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>Appelee par CaseAcocherVide AVANT la remise a zero de la liste <paramref name="x"/> (1 = photos, 2 = videos).</summary>
    Friend Sub AvantLaRemiseAZero(x As Integer, grille As DataGridView, Optional tourTermine As Boolean = False)
        If x <> 1 AndAlso x <> 2 Then Exit Sub
        If Not tourTermine AndAlso Not ToutEstVu(grille) Then Exit Sub
        Charger()
        Dim jour As String = Date.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
        If x = 1 Then _photos += 1 : _dernierPhotos = jour Else _videos += 1 : _dernierVideos = jour
        Enregistrer()
        DiagLog("Tours complets : " & If(x = 1, "toutes les photos vues", "toutes les videos vues") & " -> " & If(x = 1, _photos, _videos) & " tour(s)")
    End Sub

    ''' <summary>« Tours complets : 3 (dernier : 12/11/2026) » ou « Tours complets : aucun ».</summary>
    Friend Function Texte(x As Integer) As String
        Charger()
        Dim n As Integer = If(x = 1, _photos, _videos)
        Dim dernier As String = If(x = 1, _dernierPhotos, _dernierVideos)
        If n = 0 Then Return "Tours complets : aucun"
        Return "Tours complets : " & n & If(dernier <> "", " (dernier : " & dernier & ")", "")
    End Function

End Module
