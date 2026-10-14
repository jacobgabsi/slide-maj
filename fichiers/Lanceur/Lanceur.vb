Option Strict On
Option Explicit On

Imports System.IO
Imports System.Windows.Forms
Imports Microsoft.Win32

''' <summary>
''' Lanceur de Slide, a poser a la racine du dossier (Slide.exe).
'''
''' Il est compile pour .NET Framework 4.8, present d'origine sur Windows 10 et 11 : il demarre
''' donc meme sur un PC ou rien n'est installe, alors que le vrai programme
''' (bin\Debug\net10.0-windows\slide.exe) exige .NET 10.
'''
''' Il verifie les deux choses dont Slide a besoin :
'''   - .NET 10 Desktop, sans lequel le programme ne demarre pas du tout ;
'''   - le fournisseur Access ACE, sans lequel la base slide.accdb (minuterie, localite des
'''     photos) n'est pas lisible.
'''
''' - Tout est la            : il lance le programme, sans rien afficher.
''' - Il manque quelque chose : il PROPOSE de lancer Slide.bat, qui installe ce qui manque
'''                      (depuis le dossier Runtime, donc sans Internet) puis demarre Slide.
'''                      Rien ne se fait sans la reponse de l'utilisateur : le programme reste
'''                      intact s'il refuse.
'''
''' Il ecrit ce qu'il a fait dans %TEMP%\slide_lanceur.log. L'argument /test affiche la decision
''' sans rien lancer.
''' </summary>
Module Lanceur

    Sub Main(arguments As String())

        Dim test As Boolean = arguments.Any(Function(a) String.Equals(a, "/test", StringComparison.OrdinalIgnoreCase))
        Dim dossier As String = Path.GetDirectoryName(Application.ExecutablePath)
        Dim programme As String = Trouver(dossier)
        Dim script As String = Path.Combine(dossier, "Slide.bat")

        ' Ce qui manque sur ce PC, en clair : sert au journal et au texte de la question.
        Dim manquant As New List(Of String)
        If Not DotNet10Installe() Then manquant.Add(".NET 10")
        If Not AccessInstalle() Then manquant.Add("le fournisseur Access (ACE)")
        Dim bilan As String = String.Join(" et ", manquant.ToArray())

        Dim decision As String
        If programme = "" Then
            decision = "slide.exe introuvable sous " & dossier
        ElseIf manquant.Count = 0 Then
            decision = "NET 10 et ACE presents -> " & programme
        ElseIf File.Exists(script) Then
            decision = "manque " & bilan & " -> proposition de lancer " & script
        Else
            decision = "manque " & bilan & " et Slide.bat est introuvable"
        End If
        Journal(decision)

        If test Then
            MessageBox.Show(decision, "Lanceur Slide (test)", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If

        Try
            If programme = "" Then
                MessageBox.Show("slide.exe est introuvable dans " & dossier & ".", "Slide",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
            ElseIf manquant.Count = 0 Then
                Process.Start(New ProcessStartInfo(programme) With {.WorkingDirectory = Path.GetDirectoryName(programme)})
            ElseIf File.Exists(script) Then
                ' Rien n'est installe sans accord : on propose, l'utilisateur decide.
                Dim reponse As DialogResult =
                    MessageBox.Show("Il manque sur ce PC " & bilan & ", dont Slide a besoin." & vbCrLf & vbCrLf &
                                    "Voulez-vous lancer l'installation maintenant ?" & vbCrLf & vbCrLf &
                                    script & vbCrLf &
                                    "installe ce qui manque sans Internet (depuis le dossier Runtime)," & vbCrLf &
                                    "puis demarre Slide.",
                                    "Slide : installation necessaire",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1)
                If reponse = DialogResult.Yes Then
                    Journal("l'utilisateur a accepte : lancement de " & script)
                    Process.Start(New ProcessStartInfo(script) With {.WorkingDirectory = dossier})
                Else
                    Journal("l'utilisateur a refuse l'installation")
                End If
            Else
                MessageBox.Show("Il manque sur ce PC " & bilan & ", et le fichier Slide.bat," & vbCrLf &
                                "qui sert a l'installer, est absent du dossier " & dossier & ".", "Slide",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Catch ex As Exception
            Journal("ERREUR : " & ex.Message)
            MessageBox.Show(ex.Message, "Slide", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub

    ''' <summary>Le vrai programme : sous bin\Debug, sous bin\Release, ou a cote.</summary>
    Private Function Trouver(dossier As String) As String
        For Each chemin As String In {Path.Combine(dossier, "bin\Debug\net10.0-windows\slide.exe"),
                                      Path.Combine(dossier, "bin\Release\net10.0-windows\slide.exe"),
                                      Path.Combine(dossier, "slide.exe")}
            If File.Exists(chemin) Then Return chemin
        Next
        Return ""
    End Function

    ''' <summary>.NET 10 Desktop installe ? La presence du dossier du runtime suffit a le dire.</summary>
    Private Function DotNet10Installe() As Boolean
        For Each racine As String In {Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                      Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)}
            Try
                If racine = "" Then Continue For
                Dim partage As String = Path.Combine(racine, "dotnet\shared\Microsoft.WindowsDesktop.App")
                If Not Directory.Exists(partage) Then Continue For
                If Directory.GetDirectories(partage, "10.*").Length > 0 Then Return True
            Catch ex As Exception
                ' dossier illisible : on regarde le suivant
            End Try
        Next
        Return False
    End Function

    ''' <summary>
    ''' Fournisseur Access ACE installe ? On regarde le ProgID dans la base de registre, comme le
    ''' fait Slide.bat. La vue 64 bits est la seule qui compte : Slide est compile en x64.
    ''' </summary>
    Private Function AccessInstalle() As Boolean
        For Each progid As String In {"Microsoft.ACE.OLEDB.12.0", "Microsoft.ACE.OLEDB.16.0"}
            Try
                Using base As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64)
                    Using cle As RegistryKey = base.OpenSubKey(progid)
                        If cle IsNot Nothing Then Return True
                    End Using
                End Using
            Catch ex As Exception
                ' registre illisible : on regarde le suivant
            End Try
        Next
        Return False
    End Function

    Private Sub Journal(texte As String)
        Try
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "slide_lanceur.log"),
                               Date.Now.ToString("dd/MM/yyyy HH:mm:ss") & " ; " & texte & Environment.NewLine)
        Catch ex As Exception
            ' sans importance
        End Try
    End Sub

End Module
