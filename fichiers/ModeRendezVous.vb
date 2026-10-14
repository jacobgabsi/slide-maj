Option Strict On
Option Explicit On

''' <summary>
''' Mode RDV (reglages, onglet Divers ; ou tache "Mode RDV" de la minuterie) : le diaporama ne
''' montre que les photos et les videos prises entre une date de debut et une date de fin, dans
''' l'ordre chronologique, photos et videos melees, en boucle ou une seule fois.
'''
''' Reecrit le 06/10 ("il arrive souvent que ca bug", Jacob). L'ancien code avancait deux
''' compteurs, un dans la liste des photos et un dans celle des videos, et supposait les deux
''' listes triees par date croissante. Defauts releves :
'''   - listes triees autrement (tri decroissant) : "aucune photo a cette date" a tort ;
'''   - le compteur depassait la fin de la liste quand la periode allait jusqu'a la derniere
'''     photo : erreur ;
'''   - fin d'un RDV sans boucle pendant le tour des videos : la video n° 0 etait jouee ;
'''   - RDV lance par la minuterie : il reprenait la ou le precedent s'etait arrete.
'''
''' Ici : a l'activation (ou quand les dates, les listes ou les cases "que les photos / que les
''' videos" changent), on dresse UNE liste de ce qui tombe dans la periode, triee par date ; il
''' ne reste qu'a la parcourir. L'ordre des listes de Slide n'a plus d'importance.
''' </summary>
Module ModeRendezVous

    Private Structure Element
        Public Quand As Date
        Public EstVideo As Boolean
        Public Numero As Integer            ' ligne dans la liste des photos (ou des videos)
    End Structure

    Private ReadOnly _liste As New List(Of Element)
    Private _position As Integer
    Private _signature As String = ""

    ''' <summary>Fait repartir le RDV du debut de la periode au prochain element.</summary>
    Public Sub RdvReinitialiser()
        _signature = ""
    End Sub

    ''' <summary>Element suivant du RDV. Renvoie son numero de ligne et met ModeRdvType a "PHOTO"
    ''' ou "VIDEO" (et NumPhoto ou NumVideo) ; renvoie Nothing quand le RDV est termine ou
    ''' impossible : ModeRdv est alors remis a False et le diaporama normal reprend.</summary>
    Public Function RdvSuivant() As Object
        Try
            Dim debut As Date, fin As Date
            If Not LireDate(ModeRdvDateDebut, debut) OrElse Not LireDate(ModeRdvDateFin, fin) Then
                Return Terminer("Les dates du mode RDV ne sont pas valides.")
            End If
            debut = debut.Date : fin = fin.Date
            If fin < debut Then
                Dim t As Date = debut : debut = fin : fin = t
            End If

            ' L'ancien code signalait "repartir du debut" en remettant cette date a 1900 (Form4).
            Dim demande As Boolean = (ModeRdvLastDatePhoto = #1/1/1900#)
            Dim nbPhotos As Integer = Math.Min(CInt(NombreDePhoto), Form2.DataGridView1.Rows.Count)
            Dim nbVideos As Integer = Math.Min(CInt(NombreDeVideo), Form2.DataGridView2.Rows.Count)
            Dim signature As String = debut.ToString("yyyyMMdd") & "|" & fin.ToString("yyyyMMdd") & "|" & onlyphotos & "|" & onlyvideos &
                                      "|" & nbPhotos & "|" & nbVideos
            If demande OrElse signature <> _signature Then
                If Not Dresser(debut, fin, nbPhotos, nbVideos) Then Return Nothing
                _signature = signature
                ModeRdvLastDatePhoto = Date.Today
                ModeRdvLastDateVideo = Date.Today
            End If

            If _position >= _liste.Count Then
                If Not ModeRdvEnBoucle Then
                    DiagLog("Mode RDV : fin de la periode, retour au mode normal")
                    ModeRdv = False
                    RdvReinitialiser()
                    Return Nothing
                End If
                _position = 0
                CaseAcocherVide(1)
                CaseAcocherVide(2)
                ' Fin de la periode, en boucle : on le dit quelques secondes (demande de Jacob, 06/10).
                DiagLog("Mode RDV : fin de la periode, reprise au debut")
                Try
                    MessageEcran("Reprise au début de la sélection, " & PeriodeDuAu(debut, fin) & ".", DureeMessageReprise, "Mode RDV")
                Catch ex As Exception
                End Try
            End If

            Dim e As Element = _liste(_position)
            _position += 1
            If e.EstVideo Then
                ModeRdvType = "VIDEO"
                NumVideo = e.Numero
            Else
                ModeRdvType = "PHOTO"
                NumPhoto = e.Numero
            End If
            Return e.Numero
        Catch ex As Exception
            DiagLog("Mode RDV : " & ex.Message)
            Return Terminer("Le mode RDV a rencontré une erreur.")
        End Try
    End Function

    ''' <summary>Dresse la liste de la periode. Faux (et retour au mode normal) si elle est vide.</summary>
    Private Function Dresser(debut As Date, fin As Date, nbPhotos As Integer, nbVideos As Integer) As Boolean
        _liste.Clear()
        _position = 0
        Dim finExclue As Date = fin.AddDays(1)
        Dim photos As Integer = 0, videos As Integer = 0

        For i As Integer = 0 To nbPhotos - 1
            Dim d As Date
            If LireDate(Form2.DataGridView1.Rows(i).Cells(2).Value, d) AndAlso d >= debut AndAlso d < finExclue Then
                photos += 1
                If Not onlyvideos Then _liste.Add(New Element With {.Quand = d, .EstVideo = False, .Numero = i})
            End If
        Next
        For i As Integer = 0 To nbVideos - 1
            Dim d As Date
            If LireDate(Form2.DataGridView2.Rows(i).Cells(2).Value, d) AndAlso d >= debut AndAlso d < finExclue Then
                videos += 1
                If Not onlyphotos Then _liste.Add(New Element With {.Quand = d, .EstVideo = True, .Numero = i})
            End If
        Next
        RDV_AucunePhotoTrouvé = (photos = 0)
        RDV_AucuneVideoTrouvé = (videos = 0)

        ' Ordre chronologique ; a date egale la photo d'abord, puis l'ordre des listes.
        _liste.Sort(Function(a, b)
                        Dim c As Integer = a.Quand.CompareTo(b.Quand)
                        If c = 0 Then c = a.EstVideo.CompareTo(b.EstVideo)
                        If c = 0 Then c = a.Numero.CompareTo(b.Numero)
                        Return c
                    End Function)
        DiagLog("Mode RDV : du " & debut.ToString("dd/MM/yyyy") & " au " & fin.ToString("dd/MM/yyyy") & " -> " & photos & " photo(s), " &
                videos & " video(s), " & _liste.Count & " a montrer" & If(ModeRdvEnBoucle, ", en boucle", ", une seule fois"))

        If _liste.Count = 0 Then
            If photos = 0 AndAlso videos = 0 Then
                Terminer("Aucune photo ni vidéo " & Periode(debut, fin) & ".")
            ElseIf onlyphotos Then
                Terminer("Il n'y a que des vidéos " & Periode(debut, fin) & ", et « N'afficher que les photos » est coché.")
            Else
                Terminer("Il n'y a que des photos " & Periode(debut, fin) & ", et « N'afficher que les vidéos » est coché.")
            End If
            Return False
        End If
        CaseAcocherVide(1)
        CaseAcocherVide(2)
        Return True
    End Function

    ''' <summary>Dates de la photo ou de la video la plus ancienne et de la plus recente des
    ''' listes (les dates par defaut du mode RDV). Faux s'il n'y a aucune photo ni video datee.</summary>
    Public Function PeriodeComplete(ByRef plusAncienne As Date, ByRef plusRecente As Date) As Boolean
        Dim trouve As Boolean = False
        For Each grille As DataGridView In {Form2.DataGridView1, Form2.DataGridView2}
            For i As Integer = 0 To grille.Rows.Count - 1
                If grille.Rows(i).IsNewRow OrElse grille.Columns.Count < 3 Then Continue For
                Dim d As Date
                If Not LireDate(grille.Rows(i).Cells(2).Value, d) Then Continue For
                If Not trouve OrElse d < plusAncienne Then plusAncienne = d
                If Not trouve OrElse d > plusRecente Then plusRecente = d
                trouve = True
            Next
        Next
        Return trouve
    End Function

    ''' <summary>Secondes d'affichage du message "Reprise au début de la sélection".</summary>
    Private Const DureeMessageReprise As Integer = 3

    ''' <summary>"du 02/01/2020 au 06/01/2020" (ou "le 02/01/2020" pour un seul jour).</summary>
    Private Function PeriodeDuAu(debut As Date, fin As Date) As String
        If debut.Date = fin.Date Then Return "le " & debut.ToString("dd/MM/yyyy")
        Return "du " & debut.ToString("dd/MM/yyyy") & " au " & fin.ToString("dd/MM/yyyy")
    End Function

    ''' <summary>"le 21/03/2008" ou "entre le 01/06/2021 et le 30/06/2021".</summary>
    Private Function Periode(debut As Date, fin As Date) As String
        If debut.Date = fin.Date Then Return "le " & debut.ToString("dd/MM/yyyy")
        Return "entre le " & debut.ToString("dd/MM/yyyy") & " et le " & fin.ToString("dd/MM/yyyy")
    End Function

    ''' <summary>Quitte le mode RDV en l'annoncant a l'ecran ; renvoie Nothing.</summary>
    Private Function Terminer(message As String) As Object
        ModeRdv = False
        RdvReinitialiser()
        DiagLog("Mode RDV : " & message.Replace(vbCrLf, " - "))
        Try
            MessageEcran(message & vbCrLf & "Retour au diaporama normal.", 5, "Mode RDV")
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    ''' <summary>Date d'une case des listes (date, ou texte "21/03/2008 11:43") ou d'un reglage
    ''' ("21/03/2008", "21-03-2008"). Les textes sont lus en jour/mois/annee, quelle que soit la
    ''' langue de Windows.</summary>
    Friend Function LireDate(valeur As Object, ByRef resultat As Date) As Boolean
        If valeur Is Nothing OrElse valeur Is DBNull.Value Then Return False
        If TypeOf valeur Is Date Then
            resultat = CDate(valeur)
            Return resultat.Year > 1900
        End If
        Dim texte As String = valeur.ToString().Trim().Replace("-", "/")
        If texte = "" Then Return False
        Dim francais As Globalization.CultureInfo = Globalization.CultureInfo.GetCultureInfo("fr-FR")
        If Date.TryParse(texte, francais, Globalization.DateTimeStyles.None, resultat) Then Return resultat.Year > 1900
        If Date.TryParse(texte, Globalization.CultureInfo.CurrentCulture, Globalization.DateTimeStyles.None, resultat) Then Return resultat.Year > 1900
        Return False
    End Function

End Module
