
Imports System
Imports System.Windows.Forms

Public Class CalendarColumn
    Inherits DataGridViewColumn

    Public Sub New()
        MyBase.New(New CalendarCell())
    End Sub

    Public Overrides Property CellTemplate() As DataGridViewCell
        Get
            Return MyBase.CellTemplate
        End Get
        Set(ByVal value As DataGridViewCell)

            ' Ensure that the cell used for the template is a CalendarCell.
            If (value IsNot Nothing) AndAlso _
                Not value.GetType().IsAssignableFrom(GetType(CalendarCell)) _
                Then
                Throw New InvalidCastException("Must be a CalendarCell")
            End If
            MyBase.CellTemplate = value

        End Set
    End Property

End Class

Public Class CalendarCell
    Inherits DataGridViewTextBoxCell

    Public Sub New()
        ' Use the short date format.
        Me.Style.Format = "HH:mm"
    End Sub

    Public Overrides Sub InitializeEditingControl(ByVal rowIndex As Integer, _
        ByVal initialFormattedValue As Object, _
        ByVal dataGridViewCellStyle As DataGridViewCellStyle)

        ' Case de saisie (28/10/2026, avant : un selecteur d'heure) : on tape "18:12" ou une regle liee a Chabbat ("ShabbatDebut-10", voir RegleShabbat).
        ' La case montre toujours l'HEURE (la regle d'une heure liee a Chabbat se voit et se change dans la fenetre du double-clic).
        MyBase.InitializeEditingControl(rowIndex, initialFormattedValue, _
            dataGridViewCellStyle)
        RegleShabbat.ReglePendante = Nothing

        Dim ctl As DataGridViewTextBoxEditingControl = TryCast(DataGridView.EditingControl, DataGridViewTextBoxEditingControl)
        If ctl Is Nothing Then Exit Sub
        ctl.SelectAll()
    End Sub

    ''' <summary>Ce qui a ete tape : une regle de Chabbat (l'heure du jour est calculee, la regle attend la fin de l'edition dans RegleShabbat.ReglePendante), une heure
    ''' (18:12, 18h12, 1812...) ou, a defaut, ce que sait lire le DataGridView.</summary>
    Public Overrides Function ParseFormattedValue(formattedValue As Object, cellStyle As DataGridViewCellStyle,
            formattedValueTypeConverter As System.ComponentModel.TypeConverter,
            valueTypeConverter As System.ComponentModel.TypeConverter) As Object
        Dim texte As String = Convert.ToString(formattedValue).Trim()
        Dim regle As String = ""
        If RegleShabbat.Analyser(texte, regle) Then
            Dim minutes As Integer, jourBase As Integer
            If Not RegleShabbat.Calculer(regle, minutes, jourBase) Then
                MessageBox.Show("Horaire inconnu (aucune ville choisie dans l'onglet Shabbat ?)", "Heure", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Return Conservee()
            End If
            RegleShabbat.ReglePendante = regle
            Return DateTime.Today.AddMinutes(((minutes Mod 1440) + 1440) Mod 1440)
        End If
        Dim saisie As DateTime? = RegleShabbat.LireHeureSaisie(texte)
        If saisie IsNot Nothing Then
            RegleShabbat.ReglePendante = ""
            Return saisie.Value
        End If
        ' Texte vide (on a efface pour retaper puis quitte la case) : l'heure d'avant est gardee, rien n'est perdu (28/10/2026 : « pourquoi tu effaces l'heure »).
        If texte = "" Then Return Conservee()
        Try
            Return MyBase.ParseFormattedValue(formattedValue, cellStyle, formattedValueTypeConverter, valueTypeConverter)
        Catch ex As Exception
            MessageBox.Show("Heure non reconnue : l'heure d'avant est gardée." & vbCrLf & "Tapez par exemple 18:12, ou ShabbatDebut-10 (ShabbatPreDebut, ShabbatDebut, ShabbatSortie, SoleilLever ou SoleilCoucher, suivi de +n ou -n minutes).", "Heure", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return Conservee()
        End Try
    End Function

    ''' <summary>La valeur actuelle de la case (l'heure d'avant), pour ne rien effacer quand ce qui est tape n'est pas une heure.</summary>
    Private Function Conservee() As Object
        RegleShabbat.ReglePendante = Nothing
        Dim ancienne As Object = Me.Value
        If ancienne Is Nothing OrElse IsDBNull(ancienne) Then Return DateTime.Today.AddHours(18)
        Return ancienne
    End Function

    ''' <summary>L'heure contenue dans une valeur de la colonne : DateTime (base Access :
    ''' 30/12/1899 11:20), texte "11:20", ou texte "11:20 30/09/2026" (anciennes valeurs).
    ''' Nothing si ce n'est pas une heure.</summary>
    Friend Shared Function LireHeure(valeur As Object) As DateTime?
        If valeur Is Nothing OrElse IsDBNull(valeur) Then Return Nothing
        If TypeOf valeur Is DateTime Then Return CType(valeur, DateTime)
        Dim texte As String = CStr(valeur).Trim()
        If texte = "" Then Return Nothing
        Dim d As DateTime
        If DateTime.TryParse(texte, d) Then Return d
        ' "HH:mm" en tete, suivi d'autre chose
        If texte.Length >= 5 AndAlso DateTime.TryParseExact(texte.Substring(0, 5), "HH:mm",
                Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, d) Then Return d
        Return Nothing
    End Function

    ''' <summary>Affichage : toujours l'heure seule ("HH:mm"), quelle que soit la forme de la
    ''' valeur. Le format de la colonne ne s'appliquait qu'aux DateTime, pas au texte.</summary>
    Protected Overrides Function GetFormattedValue(value As Object, rowIndex As Integer,
            ByRef cellStyle As DataGridViewCellStyle,
            valueTypeConverter As System.ComponentModel.TypeConverter,
            formattedValueTypeConverter As System.ComponentModel.TypeConverter,
            context As DataGridViewDataErrorContexts) As Object
        Dim heure As DateTime? = LireHeure(value)
        If heure IsNot Nothing Then Return heure.Value.ToString("HH:mm")
        Return MyBase.GetFormattedValue(value, rowIndex, cellStyle, valueTypeConverter, formattedValueTypeConverter, context)
    End Function

    Public Overrides ReadOnly Property EditType() As Type
        Get
            ' Return the type of the editing control that CalendarCell uses.
            Return GetType(HeureEditingControl)
        End Get
    End Property

    Public Overrides ReadOnly Property ValueType() As Type
        Get
            '*' Return the type of the value that CalendarCell contains.
            Return GetType(DateTime)
        End Get
    End Property

    Public Overrides ReadOnly Property DefaultNewRowValue() As Object
        Get
            'Use the current date and time as the default value.

            Return DateTime.Now

        End Get
    End Property

End Class

''' <summary>Case de saisie de l'heure : on tape l'heure (ou une regle de Chabbat), et les fleches Haut / Bas la changent comme le faisait l'ancien selecteur
''' (28/10/2026) : +1 / -1 sur les HEURES quand le curseur est avant les deux-points, sur les MINUTES apres ; Gauche / Droite changent de partie.</summary>
Public Class HeureEditingControl
    Inherits DataGridViewTextBoxEditingControl

    Public Overrides Function EditingControlWantsInputKey(keyData As Keys, dataGridViewWantsInputKey As Boolean) As Boolean
        Select Case keyData And Keys.KeyCode
            Case Keys.Up, Keys.Down
                Return True                 ' (sinon le tableau change de ligne)
        End Select
        Return MyBase.EditingControlWantsInputKey(keyData, dataGridViewWantsInputKey)
    End Function

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If (e.KeyCode = Keys.Up OrElse e.KeyCode = Keys.Down) AndAlso Not e.Alt AndAlso Not e.Control Then
            e.Handled = True
            e.SuppressKeyPress = True
            Dim heure As DateTime? = RegleShabbat.LireHeureSaisie(Me.Text)
            If heure IsNot Nothing Then
                Dim sens As Integer = If(e.KeyCode = Keys.Up, 1, -1)
                Dim surLesHeures As Boolean = Me.SelectionStart < 2
                Dim nouvelle As DateTime = If(surLesHeures, heure.Value.AddHours(sens), heure.Value.AddMinutes(sens))
                Me.Text = nouvelle.ToString("HH:mm")
                If surLesHeures Then Me.Select(0, 2) Else Me.Select(3, 2)
            End If
            Return
        End If
        MyBase.OnKeyDown(e)
    End Sub

End Class

Class CalendarEditingControl
    Inherits DateTimePicker
    Implements IDataGridViewEditingControl
    Private dataGridViewControl As DataGridView
    Private valueIsChanged As Boolean = False
    Private rowIndexNum As Integer

    Public Sub New()
        'Me.Format = DateTimePickerFormat.Short
        Me.Format = DateTimePickerFormat.Custom
        Me.CustomFormat = "HH:mm"



    End Sub

    Public Property EditingControlFormattedValue() As Object _
        Implements IDataGridViewEditingControl.EditingControlFormattedValue

        Get
            Return Me.Value.ToString("HH:mm")
        End Get

        Set(ByVal value As Object)
            Try
                ' This will throw an exception of the string is 
                ' null, empty, or not in the format of a date.
                Me.Value = DateTime.Parse(CStr(value))
            Catch
                ' In the case of an exception, just use the default
                ' value so we're not left with a null value.
                Me.Value = DateTime.Now
            End Try
        End Set


    End Property

    Public Function GetEditingControlFormattedValue(ByVal context _
        As DataGridViewDataErrorContexts) As Object _
        Implements IDataGridViewEditingControl.GetEditingControlFormattedValue
        '''''''''*''''''''''''
        'Return Me.Value.ToShortDateString
        Return Me.Value.ToShortTimeString
        'Debug.Print(Me.Value.ToShortTimeString)

    End Function

    Public Sub ApplyCellStyleToEditingControl(ByVal dataGridViewCellStyle As  _
        DataGridViewCellStyle) _
        Implements IDataGridViewEditingControl.ApplyCellStyleToEditingControl

        Me.Font = dataGridViewCellStyle.Font
        Me.CalendarForeColor = dataGridViewCellStyle.ForeColor
        Me.CalendarMonthBackground = dataGridViewCellStyle.BackColor

    End Sub

    Public Property EditingControlRowIndex() As Integer _
        Implements IDataGridViewEditingControl.EditingControlRowIndex

        Get
            Return rowIndexNum
        End Get
        Set(ByVal value As Integer)
            rowIndexNum = value
        End Set


    End Property

    Public Function EditingControlWantsInputKey(ByVal key As Keys, _
        ByVal dataGridViewWantsInputKey As Boolean) As Boolean _
        Implements IDataGridViewEditingControl.EditingControlWantsInputKey

        ' Let the DateTimePicker handle the keys listed.
        Select Case key And Keys.KeyCode
            Case Keys.Left, Keys.Up, Keys.Down, Keys.Right, _
                Keys.Home, Keys.End, Keys.PageDown, Keys.PageUp

                Return True

            Case Else
                Return Not dataGridViewWantsInputKey
        End Select

    End Function

    Public Sub PrepareEditingControlForEdit(ByVal selectAll As Boolean) _
        Implements IDataGridViewEditingControl.PrepareEditingControlForEdit

        ' No preparation needs to be done.

    End Sub

    Public ReadOnly Property RepositionEditingControlOnValueChange() _
        As Boolean Implements _
        IDataGridViewEditingControl.RepositionEditingControlOnValueChange

        Get
            Return False
        End Get

    End Property

    Public Property EditingControlDataGridView() As DataGridView _
        Implements IDataGridViewEditingControl.EditingControlDataGridView

        Get
            Return dataGridViewControl
        End Get
        Set(ByVal value As DataGridView)
            dataGridViewControl = value
        End Set

    End Property

    Public Property EditingControlValueChanged() As Boolean _
        Implements IDataGridViewEditingControl.EditingControlValueChanged

        Get
            Return valueIsChanged
        End Get
        Set(ByVal value As Boolean)
            valueIsChanged = value
        End Set

    End Property

    Public ReadOnly Property EditingControlCursor() As Cursor _
        Implements IDataGridViewEditingControl.EditingPanelCursor

        Get
            Return MyBase.Cursor
        End Get

    End Property

    Protected Overrides Sub OnValueChanged(ByVal eventargs As EventArgs)

        ' Notify the DataGridView that the contents of the cell have changed.
        valueIsChanged = True
        Me.EditingControlDataGridView.NotifyCurrentCellDirty(True)
        MyBase.OnValueChanged(eventargs)

    End Sub

End Class


