
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

        ' Set the value of the editing control to the current cell value.
        MyBase.InitializeEditingControl(rowIndex, initialFormattedValue, _
            dataGridViewCellStyle)

        Dim ctl As CalendarEditingControl = _
            CType(DataGridView.EditingControl, CalendarEditingControl)

        ' L'heure de la cellule dans le selecteur, a la date du jour (un DateTimePicker refuse
        ' l'an 1). Avant, cette date etait AJOUTEE A LA VALEUR DE LA CELLULE : "11:20" devenait
        ' "11:20 30/09/2026", affiche tel quel dans la colonne.
        Dim heure As DateTime? = LireHeure(Me.Value)
        If heure Is Nothing Then
            ctl.Value = CType(Me.DefaultNewRowValue, DateTime)
        Else
            ctl.Value = DateTime.Today.Add(heure.Value.TimeOfDay)
        End If
    End Sub

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
            Return GetType(CalendarEditingControl)
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


