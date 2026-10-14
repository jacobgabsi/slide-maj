Imports System.Threading

''' <summary>
''' Un seul Slide a la fois (04/11/2026, Jacob : « que se passe-t-il si j'ouvre Slide 2 fois ? »). Un verrou nomme du Windows de l'utilisateur est pris au demarrage.
''' Si un autre Slide le tient : le second previent le premier (evenement nomme) puis s'arrete sans rien ouvrir. Le premier se montre : au premier plan,
''' ou, en mode discret, un message propose de reafficher Slide.
''' Exception : un Slide qui se relance lui-meme (mise a jour, relance de nuit : fichier RelanceAutomatique.txt recent) attend jusqu'a 25 s que l'ancien ait fini de se fermer.
''' </summary>
Friend Module InstanceUnique

    Private Const NomVerrou As String = "Local\SlideInstanceUnique"
    Private Const NomSignal As String = "Local\SlideInstanceUniqueSignal"
    Private _verrou As Mutex
    Private _signal As EventWaitHandle

    ''' <summary>Vrai si ce processus est le seul Slide (il tient alors le verrou jusqu'a sa fin). Faux : un autre tourne, ce processus doit s'arreter.</summary>
    Friend Function Prendre() As Boolean
        Try
            _verrou = New Mutex(False, NomVerrou)
            Dim delai As Integer = If(RelanceRecente(), 25000, 0)
            Dim obtenu As Boolean
            Try
                obtenu = _verrou.WaitOne(delai)
            Catch ex As AbandonedMutexException
                obtenu = True         ' l'ancien Slide est tombe sans liberer : le verrou est a nous
            End Try
            If Not obtenu Then
                Try
                    Using s As EventWaitHandle = EventWaitHandle.OpenExisting(NomSignal)
                        s.Set()
                    End Using
                Catch ex As Exception
                End Try
                Return False
            End If
            _signal = New EventWaitHandle(False, EventResetMode.AutoReset, NomSignal)
            Dim fil As New Thread(AddressOf Ecouter) With {.IsBackground = True, .Name = "InstanceUnique"}
            fil.Start()
            Return True
        Catch ex As Exception
            Return True               ' en cas de doute, ne jamais empecher Slide de demarrer
        End Try
    End Function

    Private Function RelanceRecente() As Boolean
        Try
            Dim f As String = IO.Path.Combine(Application.StartupPath.TrimEnd(IO.Path.DirectorySeparatorChar), "data", "settings", "RelanceAutomatique.txt")
            Return IO.File.Exists(f) AndAlso (Now - IO.File.GetLastWriteTime(f)).TotalSeconds < 120
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub Ecouter()
        Do
            Try
                _signal.WaitOne()
                Try
                    DiagLog("Deuxieme lancement de Slide refuse : Slide tourne deja")
                    ' (Form3 ici serait une AUTRE instance : l'instance par defaut de VB est propre a chaque fil. On passe par une fenetre ouverte du fil principal.)
                    Dim hote As Form = Nothing
                    For Each f As Form In Application.OpenForms.Cast(Of Form)().ToList()
                        If f.IsHandleCreated AndAlso TypeOf f Is Form3 Then hote = f : Exit For
                    Next
                    If hote IsNot Nothing Then
                        hote.BeginInvoke(New Action(AddressOf Montrer))
                    Else
                        DiagLog("Deuxieme lancement : aucune fenetre pour afficher le message")
                    End If
                Catch ex As Exception
                End Try
            Catch ex As Exception
                Exit Do
            End Try
        Loop
    End Sub

    Private Sub Montrer()
        Try
            If ModeDiscret.Actif Then
                DiagLog("Deuxieme lancement : message du mode discret")
                ' Proprietaire invisible, au premier plan : sans lui le message s'ouvre derriere les autres fenetres (Slide est cache, il n'a pas le focus).
                Using proprietaire As New Form With {.TopMost = True, .ShowInTaskbar = False, .FormBorderStyle = FormBorderStyle.None,
                                                     .StartPosition = FormStartPosition.Manual, .Location = New Point(-32000, -32000), .Size = New Size(1, 1)}
                    proprietaire.Show()
                    If MessageBox.Show(proprietaire, "Slide est déjà lancé, en mode discret : il n'affiche rien à l'écran et reste à côté de l'horloge (icône de Slide)." & vbCrLf & vbCrLf &
                                       "Voulez-vous réafficher Slide ?", "Slide", MessageBoxButtons.YesNo, MessageBoxIcon.Information,
                                       MessageBoxDefaultButton.Button2) = DialogResult.Yes Then ModeDiscret.Definir(False)
                End Using
            ElseIf Not ModeDiscret.Cache Then
                If Form3.WindowState = FormWindowState.Minimized Then Form3.WindowState = FormWindowState.Normal
                Form3.Activate()
            End If
        Catch ex As Exception
            DiagLog("Deuxieme lancement : message impossible : " & ex.Message)
        End Try
    End Sub

End Module
