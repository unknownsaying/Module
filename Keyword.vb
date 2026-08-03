Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Public Class KeywordMatrixForm
    Inherits Form
    
    ' List of all VB.NET keywords organized by category
    Private ReadOnly keywordCategories As New Dictionary(Of String, List(Of String)) From {
        {"Data Types", New List(Of String) From {"Boolean", "Byte", "Char", "Date", "Decimal", "Double", "Integer", "Long", "Object", "SByte", "Short", "Single", "String", "UInteger", "ULong", "UShort"}},
        {"Control Flow", New List(Of String) From {"If", "Then", "Else", "ElseIf", "Select", "Case", "For", "Each", "Next", "Do", "Loop", "While", "Until", "With", "End With"}},
        {"Exception Handling", New List(Of String) From {"Try", "Catch", "Finally", "Throw", "When"}},
        {"Declarations", New List(Of String) From {"Dim", "Public", "Private", "Protected", "Friend", "Shared", "Static", "ReadOnly", "WriteOnly", "Const", "As", "Of", "In", "Out"}},
        {"Operators", New List(Of String) From {"And", "Or", "Not", "Xor", "Mod", "Like", "Is", "IsNot", "TypeOf", "AddressOf", "GetType", "DirectCast", "TryCast"}},
        {"Procedures", New List(Of String) From {"Sub", "Function", "Property", "Event", "Delegate", "Operator", "Return", "Exit", "Continue", "GoTo"}},
        {"Classes & Objects", New List(Of String) From {"Class", "Module", "Interface", "Structure", "Enum", "Inherits", "Implements", "Overrides", "Overridable", "NotOverridable", "MustOverride", "Shadows", "New"}},
        {"Linq", New List(Of String) From {"From", "Where", "Select", "Group", "Order By", "Join", "Let", "Distinct", "Skip", "Take", "Aggregate"}},
        {"Async", New List(Of String) From {"Async", "Await", "Task"}},
        {"Other Keywords", New List(Of String) From {"Me", "MyBase", "MyClass", "Nothing", "True", "False", "Global", "Namespace", "Imports", "Option", "Comparison", "Equals", "Get", "Set", "AddHandler", "RemoveHandler", "RaiseEvent", "WithEvents", "Handles", "Implements", "MustInherit", "NotInheritable", "Partial"}}
    }
    
    Private matrixPanel As Panel
    Private searchBox As TextBox
    Private statusLabel As Label
    Private connectionLines As List(Of Line)
    Private keywordButtons As New Dictionary(Of String, Button)
    Private selectedKeyword As String = ""
    Private random As New Random()
    Private matrixTimer As Timer
    
    Public Sub New()
        InitializeComponent()
        SetupMatrix()
        StartMatrixAnimation()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "VB.NET Ultimate Keyword Matrix"
        Me.Size = New Size(1200, 800)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(20, 20, 40)
        Me.DoubleBuffered = True
        Me.KeyPreview = True
        
        ' Status bar
        statusLabel = New Label()
        statusLabel.Text = "Click any keyword to see its connections | Total: " & GetAllKeywords().Count & " keywords"
        statusLabel.ForeColor = Color.FromArgb(100, 200, 255)
        statusLabel.Font = New Font("Consolas", 10, FontStyle.Bold)
        statusLabel.Dock = DockStyle.Bottom
        statusLabel.TextAlign = ContentAlignment.MiddleLeft
        statusLabel.Padding = New Padding(10, 0, 0, 0)
        statusLabel.Height = 30
        Me.Controls.Add(statusLabel)
        
        ' Search box
        searchBox = New TextBox()
        searchBox.Location = New Point(10, 10)
        searchBox.Size = New Size(250, 25)
        searchBox.BackColor = Color.FromArgb(30, 30, 50)
        searchBox.ForeColor = Color.White
        searchBox.Font = New Font("Consolas", 10)
        searchBox.BorderStyle = BorderStyle.FixedSingle
        searchBox.Text = "Search keywords..."
        AddHandler searchBox.Enter, Sub(s, e) If searchBox.Text = "Search keywords..." Then searchBox.Text = ""
        AddHandler searchBox.Leave, Sub(s, e) If String.IsNullOrEmpty(searchBox.Text) Then searchBox.Text = "Search keywords..."
        AddHandler searchBox.TextChanged, AddressOf OnSearchTextChanged
        Me.Controls.Add(searchBox)
        
        ' Matrix Panel
        matrixPanel = New Panel()
        matrixPanel.Dock = DockStyle.Fill
        matrixPanel.BackColor = Color.Transparent
        matrixPanel.Padding = New Padding(0, 50, 0, 40)
        matrixPanel.AutoScroll = True
        Me.Controls.Add(matrixPanel)
        
        connectionLines = New List(Of Line)()
    End Sub
    
    Private Sub SetupMatrix()
        Dim allKeywords = GetAllKeywords()
        Dim keywordColors = GetKeywordColors()
        
        ' Calculate grid layout
        Dim totalKeywords = allKeywords.Count
        Dim cols = 8
        Dim rows = CInt(Math.Ceiling(totalKeywords / cols))
        Dim buttonWidth = 130
        Dim buttonHeight = 35
        Dim padding = 15
        
        ' Get all keywords and sort them
        Dim sortedKeywords = allKeywords.OrderBy(Function(k) k).ToList()
        
        For i As Integer = 0 To totalKeywords - 1
            Dim keyword = sortedKeywords(i)
            Dim row = i \ cols
            Dim col = i Mod cols
            
            Dim btn = New Button()
            btn.Text = keyword
            btn.Size = New Size(buttonWidth, buttonHeight)
            btn.Location = New Point(col * (buttonWidth + padding) + 20, row * (buttonHeight + padding) + 20)
            btn.BackColor = keywordColors(keyword)
            btn.ForeColor = Color.White
            btn.Font = New Font("Consolas", 9, FontStyle.Bold)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = Color.FromArgb(100, 200, 255)
            btn.FlatAppearance.BorderSize = 1
            btn.Tag = keyword
            
            AddHandler btn.Click, AddressOf KeywordButton_Click
            AddHandler btn.MouseEnter, AddressOf KeywordButton_MouseEnter
            AddHandler btn.MouseLeave, AddressOf KeywordButton_MouseLeave
            
            matrixPanel.Controls.Add(btn)
            keywordButtons(keyword) = btn
        Next
        
        ' Set panel size
        Dim totalWidth = cols * (buttonWidth + padding) + 40
        Dim totalHeight = rows * (buttonHeight + padding) + 40
        matrixPanel.AutoScrollMinSize = New Size(totalWidth, totalHeight)
    End Sub
    
    Private Function GetAllKeywords() As List(Of String)
        Dim result As New List(Of String)()
        For Each kvp In keywordCategories
            result.AddRange(kvp.Value)
        Next
        Return result
    End Function
    
    Private Function GetKeywordColors() As Dictionary(Of String, Color)
        Dim colors As New Dictionary(Of String, Color)()
        Dim colorList As New List(Of Color) From {
            Color.FromArgb(255, 100, 100), ' Red
            Color.FromArgb(100, 255, 100), ' Green
            Color.FromArgb(100, 100, 255), ' Blue
            Color.FromArgb(255, 255, 100), ' Yellow
            Color.FromArgb(255, 100, 255), ' Magenta
            Color.FromArgb(100, 255, 255), ' Cyan
            Color.FromArgb(255, 150, 50),  ' Orange
            Color.FromArgb(150, 50, 255),  ' Purple
            Color.FromArgb(50, 255, 150),  ' Mint
            Color.FromArgb(255, 50, 150)   ' Pink
        }
        
        Dim categoryIndex = 0
        For Each kvp In keywordCategories
            Dim color = colorList(categoryIndex Mod colorList.Count)
            For Each keyword In kvp.Value
                colors(keyword) = color
            Next
            categoryIndex += 1
        Next
        
        Return colors
    End Function
    
    Private Sub KeywordButton_Click(sender As Object, e As EventArgs)
        Dim btn = DirectCast(sender, Button)
        Dim keyword = btn.Text
        
        ' Toggle selection
        If selectedKeyword = keyword Then
            selectedKeyword = ""
            statusLabel.Text = "Click any keyword to see its connections | Total: " & GetAllKeywords().Count & " keywords"
        Else
            selectedKeyword = keyword
            ShowKeywordConnections(keyword)
            statusLabel.Text = "Showing connections for: " & keyword & " | Click again to clear"
        End If
        
        RefreshMatrix()
    End Sub
    
    Private Sub KeywordButton_MouseEnter(sender As Object, e As EventArgs)
        Dim btn = DirectCast(sender, Button)
        btn.FlatAppearance.BorderSize = 3
        btn.FlatAppearance.BorderColor = Color.White
    End Sub
    
    Private Sub KeywordButton_MouseLeave(sender As Object, e As EventArgs)
        Dim btn = DirectCast(sender, Button)
        btn.FlatAppearance.BorderSize = 1
        btn.FlatAppearance.BorderColor = Color.FromArgb(100, 200, 255)
    End Sub
    
    Private Sub ShowKeywordConnections(keyword As String)
        connectionLines.Clear()
        
        ' Find which category this keyword belongs to
        Dim category As String = ""
        Dim allKeywords = GetAllKeywords()
        
        For Each kvp In keywordCategories
            If kvp.Value.Contains(keyword) Then
                category = kvp.Key
                Exit For
            End If
        Next
        
        ' If not found, use all keywords
        If String.IsNullOrEmpty(category) Then
            category = "All"
        End If
        
        ' Get keywords to connect (all keywords in same category)
        Dim connectedKeywords As List(Of String)
        If category = "All" Then
            connectedKeywords = allKeywords
        Else
            connectedKeywords = keywordCategories(category)
        End If
        
        ' Get positions of keywords
        Dim keywordPositions As New Dictionary(Of String, Point)
        For Each btn In matrixPanel.Controls.OfType(Of Button)()
            keywordPositions(btn.Text) = New Point(btn.Left + btn.Width \ 2, btn.Top + btn.Height \ 2)
        Next
        
        ' Create connection lines
        Dim sourcePos = keywordPositions(keyword)
        
        For Each connectedKeyword In connectedKeywords
            If connectedKeyword <> keyword AndAlso keywordPositions.ContainsKey(connectedKeyword) Then
                Dim targetPos = keywordPositions(connectedKeyword)
                
                ' Calculate distance to determine if connection is close enough
                Dim distance = Math.Sqrt(Math.Pow(targetPos.X - sourcePos.X, 2) + Math.Pow(targetPos.Y - sourcePos.Y, 2))
                If distance < 500 Then ' Limit connections to reasonable distance
                    connectionLines.Add(New Line(sourcePos, targetPos, connectedKeyword))
                End If
            End If
        Next
    End Sub
    
    Private Sub RefreshMatrix()
        matrixPanel.Invalidate()
        matrixPanel.Refresh()
    End Sub
    
    Private Sub OnSearchTextChanged(sender As Object, e As EventArgs)
        Dim searchText = searchBox.Text.ToLower()
        
        For Each btn In matrixPanel.Controls.OfType(Of Button)()
            If String.IsNullOrEmpty(searchText) Or searchText = "search keywords..." Then
                btn.Visible = True
            Else
                btn.Visible = btn.Text.ToLower().Contains(searchText)
            End If
        Next
    End Sub
    
    Private Sub StartMatrixAnimation()
        matrixTimer = New Timer()
        matrixTimer.Interval = 3000
        AddHandler matrixTimer.Tick, AddressOf AnimateMatrix
        matrixTimer.Start()
    End Sub
    
    Private Sub AnimateMatrix(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(selectedKeyword) Then
            ' Randomly select a keyword to show connections
            Dim allKeywords = GetAllKeywords()
            Dim randomKeyword = allKeywords(random.Next(allKeywords.Count))
            ShowKeywordConnections(randomKeyword)
            selectedKeyword = randomKeyword
            statusLabel.Text = "Auto-discovering: " & randomKeyword & " | Click any keyword to explore"
            RefreshMatrix()
            
            ' Highlight the selected keyword
            For Each kvp In keywordButtons
                If kvp.Key = randomKeyword Then
                    kvp.Value.BackColor = Color.Gold
                    kvp.Value.ForeColor = Color.Black
                Else
                    kvp.Value.BackColor = GetKeywordColors()(kvp.Key)
                    kvp.Value.ForeColor = Color.White
                End If
            Next
        End If
    End Sub
    
    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        
        ' Draw connection lines
        Using g As Graphics = matrixPanel.CreateGraphics()
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            
            For Each line In connectionLines
                Using pen As New Pen(Color.FromArgb(100, 100, 255, 255), 2)
                    pen.DashStyle = Drawing2D.DashStyle.Dot
                    g.DrawLine(pen, line.Start, line.[End])
                    
                    ' Draw glow effect
                    Using glowPen As New Pen(Color.FromArgb(50, 100, 255, 255), 6)
                        glowPen.DashStyle = Drawing2D.DashStyle.Dot
                        g.DrawLine(glowPen, line.Start, line.[End])
                    End Using
                End Using
            Next
            
            ' Draw labels for connections (optional)
            If connectionLines.Count > 0 Then
                For Each line In connectionLines
                    Dim midX = (line.Start.X + line.[End].X) \ 2
                    Dim midY = (line.Start.Y + line.[End].Y) \ 2
                    Using font As New Font("Consolas", 7, FontStyle.Regular)
                        Using brush As New SolidBrush(Color.FromArgb(150, 200, 200, 255))
                            ' Only show a few labels to avoid clutter
                            If connectionLines.IndexOf(line) Mod 3 = 0 Then
                                g.DrawString("✦", font, brush, midX - 5, midY - 5)
                            End If
                        End Using
                    End Using
                Next
            End If
        End Using
    End Sub
    
    Private Class Line
        Public Property Start As Point
        Public Property [End] As Point
        Public Property Label As String
        
        Public Sub New(startPoint As Point, endPoint As Point, labelText As String)
            Start = startPoint
            [End] = endPoint
            Label = labelText
        End Sub
    End Class
    
    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        MyBase.OnFormClosed(e)
        If matrixTimer IsNot Nothing Then
            matrixTimer.Stop()
            matrixTimer.Dispose()
        End If
    End Sub
    
    Public Shared Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New KeywordMatrixForm())
    End Sub
End Class

' Entry point
Module Program
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New KeywordMatrixForm())
    End Sub
End Module