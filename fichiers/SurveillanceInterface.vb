Option Strict On
Option Explicit On

''' <summary>
''' Surveillance de l'interface (08/11/2026, « le clic droit met parfois 5 a 10 secondes avant que le menu apparaisse », autre PC en 4K, TNT en PIP) : un minuteur de l'interface
''' bat toutes les 100 ms ; un fil de surveillance regarde si le battement s'arrete. Quand l'interface ne repond plus depuis 1,5 s, une ligne « Interface figee » est ecrite (avec le dernier
''' repere pose par le code et le mode de diffusion), puis « Interface liberee apres N ms ». Aucun effet sur l'affichage : seulement des lignes du journal.
''' </summary>
Friend Module SurveillanceInterface

    Private Const SeuilMs As Long = 1500
    Private _dernierBattement As Long = Environment.TickCount64
    Private _lance As Boolean
    Private _fige As Boolean
    Private _debutFigee As Long
    Private _repere As String = ""
    Private _repereLe As Long
    Private _minuteur As System.Windows.Forms.Timer

    ''' <summary>Pose un repere (« UpdateLayeredWindow FormHorloge ») : s'il y a un gel, le journal dit ce que l'interface faisait en dernier. Texte vide : fin de l'operation.</summary>
    Friend Sub Repere(texte As String)
        _repere = texte
        _repereLe = Environment.TickCount64
    End Sub

    ''' <summary>A appeler une fois, depuis le fil de l'interface.</summary>
    Friend Sub Demarrer()
        If _lance Then Exit Sub
        _lance = True
        Try
            _dernierBattement = Environment.TickCount64
            _minuteur = New System.Windows.Forms.Timer With {.Interval = 100}
            AddHandler _minuteur.Tick, AddressOf Battement
            _minuteur.Start()
            Dim fil As New Threading.Thread(AddressOf Surveiller) With {.IsBackground = True, .Name = "SurveillanceInterface", .Priority = Threading.ThreadPriority.AboveNormal}
            fil.Start()
        Catch ex As Exception
            DiagLog("Surveillance interface : demarrage impossible : " & ex.Message)
        End Try
    End Sub

    Private Sub Battement(sender As Object, e As EventArgs)
        Dim maintenant As Long = Environment.TickCount64
        If _fige Then
            _fige = False
            DiagLog("Interface liberee apres " & (maintenant - _debutFigee) & " ms")
        End If
        _dernierBattement = maintenant
    End Sub

    Private Sub Surveiller()
        While True
            Try
                Threading.Thread.Sleep(250)
                Dim maintenant As Long = Environment.TickCount64
                If Not _fige AndAlso maintenant - _dernierBattement > SeuilMs Then
                    _fige = True
                    _debutFigee = _dernierBattement
                    Dim repere As String = _repere
                    Dim ancien As String = If(repere = "", "(aucun repere en cours)", repere & " depuis " & (maintenant - _repereLe) & " ms")
                    DiagLog("Interface figee depuis " & (maintenant - _dernierBattement) & " ms ; repere : " & ancien & " ; mode " & Diffusion.Mode)
                End If
            Catch ex As Exception
            End Try
        End While
    End Sub

End Module
