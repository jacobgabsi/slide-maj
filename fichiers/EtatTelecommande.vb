Imports System.IO

''' <summary>
''' L'etat de Slide montre sur la telecommande (04/11/2026, demande de Jacob : « une barre de progression des volumes »). La telecommande n'a aucun autre canal que le dossier
''' partage : Slide y ecrit le volume general (0 a 100) dans <c>ControleAdistanceEtat\Volume.txt</c> a chaque changement, et la telecommande le relit chaque seconde.
''' Le fichier est dans un dossier a part : « ControleAdistance » ne contient que des commandes (tout fichier qui y arrive est execute).
''' </summary>
Friend Module EtatTelecommande

    ''' <summary>Ecrit le volume general (en tache de fond : le dossier peut etre sur un lecteur reseau lent). Sans effet si la telecommande n'est pas activee.</summary>
    Friend Sub EcrireVolume(valeur As Object)
        Try
            If Not ControlAdistance OrElse String.IsNullOrEmpty(DatabaseNetPath) Then Exit Sub
            Dim v As Integer = Math.Max(0, Math.Min(100, CInt(Val(valeur))))
            Dim dossier As String = DatabaseNetPath & "\ControleAdistanceEtat"
            Task.Run(Sub()
                         Try
                             Directory.CreateDirectory(dossier)
                             File.WriteAllText(Path.Combine(dossier, "Volume.txt"), v.ToString())
                         Catch ex As Exception
                         End Try
                     End Sub)
        Catch ex As Exception
        End Try
    End Sub

    Private _dernierEtat As String = ""
    Private _dernierEcritLe As Long = 0

    ''' <summary>L'etat des options montre sur la telecommande (boutons en vert, 12/11/2026) : <c>ControleAdistanceEtat\Etat.txt</c> = « VolumeOFF=1/0 », « FullScreen=1/0 », « Discret=1/0 », « Musique=1/0 » (la musique joue).
    ''' Appelee chaque seconde par le rattrapage de la telecommande ; n'ecrit (en tache de fond) que si quelque chose a change.</summary>
    Friend Sub EcrireEtatSiChange()
        Try
            If Not ControlAdistance OrElse String.IsNullOrEmpty(DatabaseNetPath) Then Exit Sub
            Dim texte As String = "VolumeOFF=" & If(VolumeOFF, "1", "0") & vbLf & "FullScreen=" & If(FullScreen, "1", "0") & vbLf & "Discret=" & If(ModeDiscret.Actif, "1", "0") & vbLf & "Musique=" & If(MusiqueVLC.MusiqueEnCours(), "1", "0")
            If texte = _dernierEtat AndAlso Environment.TickCount64 - _dernierEcritLe < 5000 Then Exit Sub          ' (reecrit aussi toutes les 5 s : la telecommande voit que Slide est vivant, ligne « Maj »)
            _dernierEtat = texte : _dernierEcritLe = Environment.TickCount64
            Dim maj As Long = _dernierEcritLe
            Dim dossier As String = DatabaseNetPath & "\ControleAdistanceEtat"
            Task.Run(Sub()
                         Try
                             Directory.CreateDirectory(dossier)
                             File.WriteAllText(Path.Combine(dossier, "Etat.txt"), texte & vbLf & "Maj=" & maj)
                         Catch ex As Exception
                             _dernierEtat = ""          ' (ecriture impossible : on reessaiera)
                         End Try
                     End Sub)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Le morceau qui joue (titre, artiste, pochette) pour la fenetre « Pochette » de la telecommande (04/11/2026) : Pochette.jpg (600 px au plus), puis Pochette.txt, ecrit EN DERNIER
    ''' (premiere ligne = numero du morceau : la telecommande ne recharge l'image que s'il change). Appelee dans le fil de l'interface (l'image est copiee ici, l'ecriture se fait en tache de fond).</summary>
    Friend Sub EcrirePochette(titre As String, artiste As String, image As Image)
        Try
            If Not ControlAdistance OrElse String.IsNullOrEmpty(DatabaseNetPath) Then Exit Sub
            Dim octets As Byte() = Nothing
            If image IsNot Nothing Then
                Dim k As Double = Math.Min(1.0, 600.0 / Math.Max(1, Math.Max(image.Width, image.Height)))
                Using petite As New Bitmap(Math.Max(1, CInt(image.Width * k)), Math.Max(1, CInt(image.Height * k)))
                    Using g As Graphics = Graphics.FromImage(petite)
                        g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                        g.DrawImage(image, 0, 0, petite.Width, petite.Height)
                    End Using
                    Using flux As New MemoryStream()
                        petite.Save(flux, Imaging.ImageFormat.Jpeg)
                        octets = flux.ToArray()
                    End Using
                End Using
            End If
            Dim marqueur As String = Environment.TickCount64.ToString()
            Dim texte As String = marqueur & vbLf & If(titre, "").Replace(vbCr, " ").Replace(vbLf, " ") & vbLf & If(artiste, "").Replace(vbCr, " ").Replace(vbLf, " ")
            Dim dossier As String = DatabaseNetPath & "\ControleAdistanceEtat"
            Task.Run(Sub()
                         Try
                             Directory.CreateDirectory(dossier)
                             Dim jpg As String = Path.Combine(dossier, "Pochette.jpg")
                             If octets IsNot Nothing Then File.WriteAllBytes(jpg, octets) Else If File.Exists(jpg) Then File.Delete(jpg)
                             File.WriteAllText(Path.Combine(dossier, "Pochette.txt"), texte)
                         Catch ex As Exception
                         End Try
                     End Sub)
        Catch ex As Exception
        End Try
    End Sub

End Module
