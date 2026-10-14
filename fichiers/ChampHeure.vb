Option Strict On
Option Explicit On

''' <summary>
''' Champ d'heure « HH:mm » aux couleurs de Slide (le selecteur d'heure de Windows restait blanc sur le fond sombre, 28/10/2026) : on tape l'heure, ou les fleches Haut / Bas
''' et la molette la changent (+1 / -1 sur les heures avant les deux-points, sur les minutes apres), comme la case Heure de la minuterie.
''' Meme interface que le selecteur remplace : <see cref="Value"/> et <see cref="ValueChanged"/>.
''' </summary>
Public Class ChampHeure
    Inherits TextBox

    Private _derniere As Date = Date.Today.AddHours(18)
    Private _sansEvenement As Boolean

    Public Event ValueChanged As EventHandler

    Public Sub New()
        MyBase.New()
        TextAlign = HorizontalAlignment.Center
        MaxLength = 5
        Text = _derniere.ToString("HH:mm")
    End Sub

    ''' <summary>L'heure du champ (la date n'a pas de sens : aujourd'hui) ; la derniere heure valide si le texte n'en est plus une.</summary>
    Public Property Value As Date
        Get
            Dim h As Date? = RegleShabbat.LireHeureSaisie(Text)
            If h IsNot Nothing Then _derniere = h.Value
            Return _derniere
        End Get
        Set(valeur As Date)
            _derniere = Date.Today.Add(valeur.TimeOfDay)
            Dim t As String = _derniere.ToString("HH:mm")
            If Text <> t Then
                _sansEvenement = True
                Text = t
                _sansEvenement = False
                RaiseEvent ValueChanged(Me, EventArgs.Empty)
            End If
        End Set
    End Property

    Private Sub Changer(sens As Integer)
        Dim sur As Boolean = SelectionStart < 2
        Dim n As Date = If(sur, Value.AddHours(sens), Value.AddMinutes(sens))
        Value = n
        If sur Then [Select](0, 2) Else [Select](3, 2)
    End Sub

    Protected Overrides Function IsInputKey(keyData As Keys) As Boolean
        Select Case keyData And Keys.KeyCode
            Case Keys.Up, Keys.Down
                Return True
        End Select
        Return MyBase.IsInputKey(keyData)
    End Function

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If (e.KeyCode = Keys.Up OrElse e.KeyCode = Keys.Down) AndAlso Not e.Alt AndAlso Not e.Control Then
            e.Handled = True
            e.SuppressKeyPress = True
            Changer(If(e.KeyCode = Keys.Up, 1, -1))
            Return
        End If
        MyBase.OnKeyDown(e)
    End Sub

    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        If e.Delta <> 0 Then Changer(If(e.Delta > 0, 1, -1))
        If TypeOf e Is HandledMouseEventArgs Then DirectCast(e, HandledMouseEventArgs).Handled = True
    End Sub

    Protected Overrides Sub OnGotFocus(e As EventArgs)
        MyBase.OnGotFocus(e)
        SelectAll()
    End Sub

    Protected Overrides Sub OnTextChanged(e As EventArgs)
        MyBase.OnTextChanged(e)
        If _sansEvenement Then Exit Sub
        If RegleShabbat.LireHeureSaisie(Text) IsNot Nothing Then RaiseEvent ValueChanged(Me, EventArgs.Empty)
    End Sub

    Protected Overrides Sub OnLeave(e As EventArgs)
        MyBase.OnLeave(e)
        Dim t As String = Value.ToString("HH:mm")          ' (texte illisible : la derniere heure valide est remise)
        If Text <> t Then
            _sansEvenement = True
            Text = t
            _sansEvenement = False
        End If
    End Sub

End Class
