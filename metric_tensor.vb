Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Math

Public Class MetricTensorExplorer
    Inherits Form
    
    ' Physical constants and parameters
    Private Const G As Double = 6.67430e-11  ' Gravitational constant
    Private Const C As Double = 2.99792458e8 ' Speed of light
    Private Const M_SUN As Double = 1.989e30  ' Solar mass
    
    ' Metric tensor components (4x4 matrix)
    Private metricTensor(3, 3) As Double
    Private inverseMetric(3, 3) As Double
    
    ' Spacetime coordinates
    Private t As Double = 0    ' Time coordinate
    Private r As Double = 10   ' Radial coordinate (in units of GM/c²)
    Private theta As Double = PI / 4
    Private phi As Double = PI / 4
    
    ' Visualization parameters
    Private selectedMetric As MetricType = MetricType.Schwarzschild
    Private mass As Double = 1.0  ' Mass in solar masses
    Private showGeodesics As Boolean = True
    Private showLightCones As Boolean = True
    Private animationTimer As Timer
    Private isAnimating As Boolean = False
    
    ' UI Controls
    Private metricDisplay As RichTextBox
    Private visualizationPanel As Panel
    Private trackBarR As TrackBar
    Private trackBarTheta As TrackBar
    Private trackBarPhi As TrackBar
    Private trackBarMass As TrackBar
    Private comboMetric As ComboBox
    Private btnAnimate As Button
    Private btnReset As Button
    Private chkGeodesics As CheckBox
    Private chkLightCones As CheckBox
    Private lblInfo As Label
    Private lblCoordinates As Label
    
    ' Enum for metric types
    Private Enum MetricType
        Schwarzschild
        Kerr
        FLRW
        Minkowski
        SchwarzschildPainleve
    End Enum
    
    Public Sub New()
        InitializeComponent()
        SetupUI()
        CalculateMetric()
        UpdateDisplay()
    End Sub
    
    Private Sub InitializeComponent()
        Me.Text = "General Relativity - Metric Tensor Explorer"
        Me.Size = New Size(1400, 900)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(10, 10, 30)
        Me.DoubleBuffered = True
        
        ' Create main layout
        Dim mainPanel = New TableLayoutPanel()
        mainPanel.Dock = DockStyle.Fill
        mainPanel.ColumnCount = 2
        mainPanel.RowCount = 1
        mainPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))
        mainPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60))
        mainPanel.Controls.Add(CreateControlPanel(), 0, 0)
        mainPanel.Controls.Add(CreateVisualizationPanel(), 1, 0)
        Me.Controls.Add(mainPanel)
    End Sub
    
    Private Function CreateControlPanel() As Panel
        Dim panel = New Panel()
        panel.Dock = DockStyle.Fill
        panel.BackColor = Color.FromArgb(20, 20, 50)
        panel.Padding = New Padding(20)
        panel.AutoScroll = True
        
        ' Title
        Dim title = New Label()
        title.Text = "📐 Metric Tensor Explorer"
        title.Font = New Font("Segoe UI", 18, FontStyle.Bold)
        title.ForeColor = Color.White
        title.Dock = DockStyle.Top
        title.Height = 50
        panel.Controls.Add(title)
        
        ' Description
        Dim desc = New Label()
        desc.Text = "The metric tensor defines the geometry of spacetime." & vbCrLf &
                    "It determines distances, angles, and the curvature of space."
        desc.Font = New Font("Segoe UI", 10)
        desc.ForeColor = Color.FromArgb(200, 200, 255)
        desc.Dock = DockStyle.Top
        desc.Height = 60
        desc.Padding = New Padding(0, 10, 0, 0)
        panel.Controls.Add(desc)
        
        ' Metric selection
        Dim metricLabel = New Label()
        metricLabel.Text = "Select Metric Tensor:"
        metricLabel.ForeColor = Color.White
        metricLabel.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        metricLabel.Dock = DockStyle.Top
        metricLabel.Height = 30
        panel.Controls.Add(metricLabel)
        
        comboMetric = New ComboBox()
        comboMetric.Dock = DockStyle.Top
        comboMetric.Height = 30
        comboMetric.DropDownStyle = ComboBoxStyle.DropDownList
        comboMetric.Items.AddRange(New Object() {
            "Schwarzschild (Static Black Hole)",
            "Kerr (Rotating Black Hole)",
            "FLRW (Expanding Universe)",
            "Minkowski (Flat Spacetime)",
            "Schwarzschild-Painlevé"
        })
        comboMetric.SelectedIndex = 0
        AddHandler comboMetric.SelectedIndexChanged, AddressOf OnMetricChanged
        panel.Controls.Add(comboMetric)
        
        ' Parameters section
        Dim paramGroup = New GroupBox()
        paramGroup.Text = "Parameters"
        paramGroup.ForeColor = Color.White
        paramGroup.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        paramGroup.Dock = DockStyle.Top
        paramGroup.Height = 220
        paramGroup.Padding = New Padding(10)
        panel.Controls.Add(paramGroup)
        
        ' Mass control
        Dim massLabel = New Label()
        massLabel.Text = "Mass (M_solar): 1.0"
        massLabel.ForeColor = Color.FromArgb(200, 255, 200)
        massLabel.Font = New Font("Consolas", 9)
        massLabel.Dock = DockStyle.Top
        massLabel.Height = 25
        paramGroup.Controls.Add(massLabel)
        
        trackBarMass = New TrackBar()
        trackBarMass.Dock = DockStyle.Top
        trackBarMass.Height = 45
        trackBarMass.Minimum = 1
        trackBarMass.Maximum = 100
        trackBarMass.Value = 10
        trackBarMass.TickFrequency = 10
        AddHandler trackBarMass.Scroll, Sub(s, e)
                                          mass = trackBarMass.Value / 10.0
                                          massLabel.Text = $"Mass (M_solar): {mass:F1}"
                                          CalculateMetric()
                                          UpdateDisplay()
                                      End Sub
        paramGroup.Controls.Add(trackBarMass)
        
        ' Radial coordinate
        Dim rLabel = New Label()
        rLabel.Text = "r (radius): 10.0 GM/c²"
        rLabel.ForeColor = Color.FromArgb(200, 200, 255)
        rLabel.Font = New Font("Consolas", 9)
        rLabel.Dock = DockStyle.Top
        rLabel.Height = 25
        paramGroup.Controls.Add(rLabel)
        
        trackBarR = New TrackBar()
        trackBarR.Dock = DockStyle.Top
        trackBarR.Height = 45
        trackBarR.Minimum = 20
        trackBarR.Maximum = 200
        trackBarR.Value = 100
        AddHandler trackBarR.Scroll, Sub(s, e)
                                        r = trackBarR.Value / 10.0
                                        rLabel.Text = $"r (radius): {r:F1} GM/c²"
                                        CalculateMetric()
                                        UpdateDisplay()
                                    End Sub
        paramGroup.Controls.Add(trackBarR)
        
        ' Angular coordinate theta
        Dim thetaLabel = New Label()
        thetaLabel.Text = "θ (theta): π/4"
        thetaLabel.ForeColor = Color.FromArgb(255, 200, 200)
        thetaLabel.Font = New Font("Consolas", 9)
        thetaLabel.Dock = DockStyle.Top
        thetaLabel.Height = 25
        paramGroup.Controls.Add(thetaLabel)
        
        trackBarTheta = New TrackBar()
        trackBarTheta.Dock = DockStyle.Top
        trackBarTheta.Height = 45
        trackBarTheta.Minimum = 0
        trackBarTheta.Maximum = 180
        trackBarTheta.Value = 45
        AddHandler trackBarTheta.Scroll, Sub(s, e)
                                            theta = trackBarTheta.Value * PI / 180
                                            thetaLabel.Text = $"θ (theta): {theta:F2} rad"
                                            CalculateMetric()
                                            UpdateDisplay()
                                        End Sub
        paramGroup.Controls.Add(trackBarTheta)
        
        ' Options
        Dim optionsGroup = New GroupBox()
        optionsGroup.Text = "Visualization Options"
        optionsGroup.ForeColor = Color.White
        optionsGroup.Font = New Font("Segoe UI", 10, FontStyle.Bold)
        optionsGroup.Dock = DockStyle.Top
        optionsGroup.Height = 100
        optionsGroup.Padding = New Padding(10)
        panel.Controls.Add(optionsGroup)
        
        chkGeodesics = New CheckBox()
        chkGeodesics.Text = "Show Geodesics"
        chkGeodesics.ForeColor = Color.White
        chkGeodesics.Checked = True
        chkGeodesics.Dock = DockStyle.Top
        chkGeodesics.Height = 30
        AddHandler chkGeodesics.CheckedChanged, Sub(s, e) UpdateDisplay()
        optionsGroup.Controls.Add(chkGeodesics)
        
        chkLightCones = New CheckBox()
        chkLightCones.Text = "Show Light Cones"
        chkLightCones.ForeColor = Color.White
        chkLightCones.Checked = True
        chkLightCones.Dock = DockStyle.Top
        chkLightCones.Height = 30
        AddHandler chkLightCones.CheckedChanged, Sub(s, e) UpdateDisplay()
        optionsGroup.Controls.Add(chkLightCones)
        
        ' Control buttons
        Dim btnPanel = New FlowLayoutPanel()
        btnPanel.Dock = DockStyle.Top
        btnPanel.Height = 50
        btnPanel.FlowDirection = FlowDirection.LeftToRight
        panel.Controls.Add(btnPanel)
        
        btnAnimate = New Button()
        btnAnimate.Text = "▶ Animate"
        btnAnimate.Size = New Size(100, 35)
        btnAnimate.BackColor = Color.FromArgb(50, 100, 200)
        btnAnimate.ForeColor = Color.White
        btnAnimate.FlatStyle = FlatStyle.Flat
        AddHandler btnAnimate.Click, AddressOf ToggleAnimation
        btnPanel.Controls.Add(btnAnimate)
        
        btnReset = New Button()
        btnReset.Text = "⟲ Reset View"
        btnReset.Size = New Size(100, 35)
        btnReset.BackColor = Color.FromArgb(100, 50, 200)
        btnReset.ForeColor = Color.White
        btnReset.FlatStyle = FlatStyle.Flat
        AddHandler btnReset.Click, AddressOf ResetView
        btnPanel.Controls.Add(btnReset)
        
        ' Metric display
        Dim metricLabel2 = New Label()
        metricLabel2.Text = "Metric Tensor Components:"
        metricLabel2.ForeColor = Color.White
        metricLabel2.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        metricLabel2.Dock = DockStyle.Top
        metricLabel2.Height = 30
        panel.Controls.Add(metricLabel2)
        
        metricDisplay = New RichTextBox()
        metricDisplay.Dock = DockStyle.Fill
        metricDisplay.BackColor = Color.FromArgb(15, 15, 40)
        metricDisplay.ForeColor = Color.FromArgb(200, 255, 200)
        metricDisplay.Font = New Font("Consolas", 9)
        metricDisplay.ReadOnly = True
        metricDisplay.BorderStyle = BorderStyle.None
        panel.Controls.Add(metricDisplay)
        
        Return panel
    End Function
    
    Private Function CreateVisualizationPanel() As Panel
        visualizationPanel = New Panel()
        visualizationPanel.Dock = DockStyle.Fill
        visualizationPanel.BackColor = Color.Black
        visualizationPanel.Padding = New Padding(10)
        AddHandler visualizationPanel.Paint, AddressOf OnVisualizationPaint
        AddHandler visualizationPanel.Resize, Sub(s, e) visualizationPanel.Invalidate()
        Return visualizationPanel
    End Function
    
    Private Sub CalculateMetric()
        ' Reset metric tensor
        Array.Clear(metricTensor, 0, metricTensor.Length)
        
        Select Case selectedMetric
            Case MetricType.Schwarzschild
                CalculateSchwarzschildMetric()
            Case MetricType.Kerr
                CalculateKerrMetric()
            Case MetricType.FLRW
                CalculateFLRWMetric()
            Case MetricType.Minkowski
                CalculateMinkowskiMetric()
            Case MetricType.SchwarzschildPainleve
                CalculatePainleveMetric()
        End Select
        
        ' Calculate inverse metric
        CalculateInverseMetric()
    End Sub
    
    Private Sub CalculateSchwarzschildMetric()
        ' Schwarzschild metric for a non-rotating black hole
        ' ds² = -(1 - 2GM/r)dt² + (1 - 2GM/r)⁻¹dr² + r²(dθ² + sin²θ dφ²)
        
        Dim rs = 2 * mass * G / (C * C)  ' Schwarzschild radius
        Dim factor = 1 - rs / r
        
        ' Using units where G = c = 1
        Dim m = mass * G / (C * C)  ' Geometric units
        factor = 1 - 2 * m / r
        
        metricTensor(0, 0) = -factor          ' g_tt
        metricTensor(1, 1) = 1 / factor       ' g_rr
        metricTensor(2, 2) = r * r            ' g_θθ
        metricTensor(3, 3) = r * r * Sin(theta) * Sin(theta)  ' g_φφ
    End Sub
    
    Private Sub CalculateKerrMetric()
        ' Kerr metric for a rotating black hole
        ' Simplified version - only showing key components
        
        Dim a = 0.5  ' Spin parameter (0 to 1)
        Dim m = mass * G / (C * C)
        Dim rho2 = r * r + a * a * Cos(theta) * Cos(theta)
        Dim delta = r * r - 2 * m * r + a * a
        
        metricTensor(0, 0) = -(1 - 2 * m * r / rho2)
        metricTensor(0, 3) = -2 * m * r * a * Sin(theta) * Sin(theta) / rho2
        metricTensor(1, 1) = rho2 / delta
        metricTensor(2, 2) = rho2
        metricTensor(3, 3) = (r * r + a * a + 2 * m * r * a * a * Sin(theta) * Sin(theta) / rho2) * Sin(theta) * Sin(theta)
        metricTensor(3, 0) = metricTensor(0, 3)  ' Symmetric
    End Sub
    
    Private Sub CalculateFLRWMetric()
        ' Friedmann-Lemaître-Robertson-Walker metric
        ' ds² = -dt² + a(t)²[dr²/(1-kr²) + r²(dθ² + sin²θ dφ²)]
        
        Dim a_scale = 1.0  ' Scale factor
        Dim k = 0.0        ' Curvature (0 = flat)
        
        metricTensor(0, 0) = -1
        metricTensor(1, 1) = a_scale * a_scale / (1 - k * r * r)
        metricTensor(2, 2) = a_scale * a_scale * r * r
        metricTensor(3, 3) = a_scale * a_scale * r * r * Sin(theta) * Sin(theta)
    End Sub
    
    Private Sub CalculateMinkowskiMetric()
        ' Minkowski metric (flat spacetime)
        ' ds² = -dt² + dr² + r²dθ² + r²sin²θ dφ²
        
        metricTensor(0, 0) = -1
        metricTensor(1, 1) = 1
        metricTensor(2, 2) = r * r
        metricTensor(3, 3) = r * r * Sin(theta) * Sin(theta)
    End Sub
    
    Private Sub CalculatePainleveMetric()
        ' Schwarzschild metric in Painlevé-Gullstrand coordinates
        ' Shows the "river" model of spacetime flowing into a black hole
        
        Dim m = mass * G / (C * C)
        Dim beta = Sqrt(2 * m / r)
        
        metricTensor(0, 0) = -(1 - 2 * m / r)
        metricTensor(0, 1) = beta
        metricTensor(1, 0) = beta
        metricTensor(1, 1) = 1
        metricTensor(2, 2) = r * r
        metricTensor(3, 3) = r * r * Sin(theta) * Sin(theta)
    End Sub
    
    Private Sub CalculateInverseMetric()
        ' Calculate inverse of the metric tensor using Gaussian elimination
        ' For simplicity, we'll use a direct formula for diagonal metrics
        ' For full tensor, would need matrix inversion
        
        Dim det = 0.0
        For i As Integer = 0 To 3
            If metricTensor(i, i) <> 0 Then
                inverseMetric(i, i) = 1 / metricTensor(i, i)
            Else
                inverseMetric(i, i) = 0
            End If
        Next
        
        ' For Kerr metric with off-diagonal terms, handle g_tφ coupling
        If selectedMetric = MetricType.Kerr Then
            Dim gtt = metricTensor(0, 0)
            Dim gtphi = metricTensor(0, 3)
            Dim gphiphi = metricTensor(3, 3)
            Dim denom = gtt * gphiphi - gtphi * gtphi
            
            If denom <> 0 Then
                inverseMetric(0, 0) = gphiphi / denom
                inverseMetric(0, 3) = -gtphi / denom
                inverseMetric(3, 0) = inverseMetric(0, 3)
                inverseMetric(3, 3) = gtt / denom
            End If
        End If
    End Sub
    
    Private Sub UpdateDisplay()
        ' Update the metric display
        Dim sb As New System.Text.StringBuilder()
        
        sb.AppendLine("Metric Tensor Components (g_μν):")
        sb.AppendLine("".PadRight(40, "="c))
        sb.AppendLine()
        
        ' Header
        sb.Append("       ")
        For j As Integer = 0 To 3
            sb.Append($"   {GetCoordinateName(j)}")
        Next
        sb.AppendLine()
        
        ' Matrix values
        For i As Integer = 0 To 3
            sb.Append($" {GetCoordinateName(i)}")
            For j As Integer = 0 To 3
                Dim val = metricTensor(i, j)
                If Abs(val) < 1e-10 Then
                    sb.Append("     0   ")
                Else
                    sb.Append($" {val,7:F4}")
                End If
            Next
            sb.AppendLine()
        Next
        
        sb.AppendLine()
        sb.AppendLine("Inverse Metric Components (g^μν):")
        sb.AppendLine("".PadRight(40, "="c))
        sb.AppendLine()
        
        ' Header
        sb.Append("       ")
        For j As Integer = 0 To 3
            sb.Append($"   {GetCoordinateName(j)}")
        Next
        sb.AppendLine()
        
        For i As Integer = 0 To 3
            sb.Append($" {GetCoordinateName(i)}")
            For j As Integer = 0 To 3
                Dim val = inverseMetric(i, j)
                If Abs(val) < 1e-10 Then
                    sb.Append("     0   ")
                Else
                    sb.Append($" {val,7:F4}")
                End If
            Next
            sb.AppendLine()
        Next
        
        ' Add physical interpretation
        sb.AppendLine()
        sb.AppendLine("Physical Interpretation:")
        sb.AppendLine("".PadRight(40, "="c))
        sb.AppendLine()
        
        ' Calculate some invariants
        Dim ricciScalar = CalculateRicciScalar()
        sb.AppendLine($"Ricci Scalar: {ricciScalar:F4}")
        sb.AppendLine($"Energy Density (approx): {CalculateEnergyDensity():F4}")
        sb.AppendLine($"Spacetime Curvature: {CalculateCurvature():F2}")
        
        ' Add event horizon info for Schwarzschild
        If selectedMetric = MetricType.Schwarzschild Then
            Dim rs = 2 * mass * G / (C * C) / (G / (C * C)) ' in geometric units
            sb.AppendLine($"Event Horizon: r_s = {rs:F2} GM/c²")
            If r <= rs Then
                sb.AppendLine("*** INSIDE EVENT HORIZON ***")
            End If
        End If
        
        metricDisplay.Text = sb.ToString()
        
        ' Refresh visualization
        visualizationPanel.Invalidate()
        
        ' Update coordinate info
        If lblCoordinates IsNot Nothing Then
            lblCoordinates.Text = $"Coordinates: (t={t:F2}, r={r:F2}, θ={theta:F2}, φ={phi:F2})"
        End If
    End Sub
    
    Private Function GetCoordinateName(index As Integer) As String
        Dim names = {"t", "r", "θ", "φ"}
        Return names(index)
    End Function
    
    Private Function CalculateRicciScalar() As Double
        ' Simplified Ricci scalar calculation
        ' For Schwarzschild: R = 0 (vacuum)
        ' For FLRW: R = 6(ä/a + (å/a)² + k/a²)
        
        Select Case selectedMetric
            Case MetricType.Schwarzschild, MetricType.Kerr, MetricType.Minkowski
                Return 0.0
            Case MetricType.FLRW
                Dim a = 1.0
                Dim adot = 0.1
                Dim addot = 0.05
                Dim k = 0.0
                Return 6 * (addot / a + (adot / a) * (adot / a) + k / (a * a))
            Case Else
                Return 0.0
        End Select
    End Function
    
    Private Function CalculateEnergyDensity() As Double
        ' Simplified energy density from Einstein equations
        ' Using G_00 = 8πG T_00
        
        Select Case selectedMetric
            Case MetricType.Schwarzschild
                Return 0.0  ' Vacuum solution
            Case MetricType.FLRW
                Dim a = 1.0
                Dim adot = 0.1
                Return 3 * (adot / a) * (adot / a) / (8 * PI)
            Case Else
                Return 0.0
        End Select
    End Function
    
    Private Function CalculateCurvature() As Double
        ' Calculate Kretschmann scalar (R_μνρσ R^μνρσ) approximation
        ' For Schwarzschild: K = 48M²/r⁶
        
        Select Case selectedMetric
            Case MetricType.Schwarzschild
                Dim m = mass * G / (C * C)
                Return 48 * m * m / (r * r * r * r * r * r)
            Case Else
                Return 0.0
        End Select
    End Function
    
    Private Sub OnVisualizationPaint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        
        Dim panel = DirectCast(sender, Panel)
        Dim centerX = panel.Width / 2
        Dim centerY = panel.Height / 2
        Dim scale = Math.Min(panel.Width, panel.Height) / 6
        
        ' Draw spacetime grid
        DrawSpacetimeGrid(g, centerX, centerY, scale)
        
        ' Draw curvature visualization
        DrawCurvature(g, centerX, centerY, scale)
        
        ' Draw geodesics if enabled
        If chkGeodesics.Checked Then
            DrawGeodesics(g, centerX, centerY, scale)
        End If
        
        ' Draw light cones if enabled
        If chkLightCones.Checked Then
            DrawLightCones(g, centerX, centerY, scale)
        End If
        
        ' Draw event horizon for black holes
        DrawEventHorizon(g, centerX, centerY, scale)
        
        ' Draw coordinate labels
        DrawCoordinateLabels(g, panel)
    End Sub
    
    Private Sub DrawSpacetimeGrid(g As Graphics, cx As Integer, cy As Integer, scale As Double)
        ' Draw the fabric of spacetime
        Using pen As New Pen(Color.FromArgb(50, 100, 200, 255))
            pen.DashStyle = DashStyle.Dash
            
            ' Draw radial lines
            For i As Integer = 0 To 11
                Dim angle = i * PI / 6
                Dim x = cx + scale * 2.5 * Cos(angle)
                Dim y = cy + scale * 2.5 * Sin(angle)
                g.DrawLine(pen, cx, cy, x, y)
            Next
            
            ' Draw concentric circles
            For radius As Integer = 1 To 2
                Dim rect As New RectangleF(cx - scale * radius, cy - scale * radius, 
                                          scale * radius * 2, scale * radius * 2)
                g.DrawEllipse(pen, rect)
            Next
        End Using
    End Sub
    
    Private Sub DrawCurvature(g As Graphics, cx As Integer, cy As Integer, scale As Double)
        ' Visualize curvature using color gradients
        Dim gradSize = 200
        Dim bmp As New Bitmap(gradSize, gradSize)
        
        For x As Integer = 0 To gradSize - 1
            For y As Integer = 0 To gradSize - 1
                ' Map to coordinates
                Dim dx = (x - gradSize / 2) / gradSize * 3.0
                Dim dy = (y - gradSize / 2) / gradSize * 3.0
                Dim dist = Sqrt(dx * dx + dy * dy)
                
                ' Calculate curvature at this point (simplified)
                Dim curvature = 0.0
                If dist > 0.001 Then
                    Select Case selectedMetric
                        Case MetricType.Schwarzschild
                            Dim m = mass * G / (C * C)
                            curvature = 48 * m * m / (dist * dist * dist * dist * dist * dist + 0.01)
                        Case MetricType.Kerr
                            curvature = 10 / (dist * dist * dist * dist + 1)
                        Case Else
                            curvature = 0.0
                    End Select
                End If
                
                ' Limit curvature for visualization
                curvature = Math.Min(curvature, 1.0)
                
                ' Color based on curvature
                Dim r = CByte(50 + 200 * curvature)
                Dim gb = CByte(50 + 50 * (1 - curvature))
                bmp.SetPixel(x, y, Color.FromArgb(r, gb, 255 - r))
            Next
        Next
        
        ' Draw the curvature map
        g.DrawImage(bmp, cx - gradSize / 2, cy - gradSize / 2, gradSize, gradSize)
        bmp.Dispose()
        
        ' Overlay with semi-transparent gradient
        Using brush As New LinearGradientBrush(New Point(cx, cy), New Point(cx, cy - 100), 
                                              Color.FromArgb(50, 0, 0, 0), Color.Transparent)
            g.FillEllipse(brush, cx - 150, cy - 150, 300, 300)
        End Using
    End Sub
    
    Private Sub DrawGeodesics(g As Graphics, cx As Integer, cy As Integer, scale As Double)
        ' Draw geodesic paths (simplified)
        Using pen As New Pen(Color.FromArgb(150, 255, 200, 100), 2)
            pen.DashStyle = DashStyle.Dot
            
            For i As Integer = 0 To 7
                Dim angle = i * PI / 4 + 0.1
                
                ' For Schwarzschild, geodesics curve inward
                Dim curvature = 0.0
                If selectedMetric = MetricType.Schwarzschild Then
                    Dim m = mass * G / (C * C)
                    curvature = 2 * m / (r * r)
                End If
                
                Dim points As New List(Of PointF)()
                For t As Double = 0 To 1 Step 0.05
                    Dim radius = scale * (0.5 + t * 2)
                    Dim theta_cur = angle - curvature * t * t * 0.5
                    Dim x = cx + radius * Cos(theta_cur)
                    Dim y = cy + radius * Sin(theta_cur)
                    points.Add(New PointF(x, y))
                Next
                
                If points.Count > 1 Then
                    g.DrawCurve(pen, points.ToArray())
                End If
            Next
        End Using
    End Sub
    
    Private Sub DrawLightCones(g As Graphics, cx As Integer, cy As Integer, scale As Double)
        ' Draw light cones (future and past)
        Using pen As New Pen(Color.FromArgb(100, 100, 255, 255), 1)
            Dim coneSize = scale * 0.3
            
            For i As Integer = 0 To 3
                Dim angle = i * PI / 2 + 0.2
                For j As Integer = -1 To 1 Step 2
                    Dim x1 = cx + coneSize * Cos(angle + j * PI / 4)
                    Dim y1 = cy + coneSize * Sin(angle + j * PI / 4)
                    Dim x2 = cx + coneSize * 0.5 * Cos(angle)
                    Dim y2 = cy + coneSize * 0.5 * Sin(angle)
                    
                    g.DrawLine(pen, x2, y2, x1, y1)
                Next
            Next
        End Using
    End Sub
    
    Private Sub DrawEventHorizon(g As Graphics, cx As Integer, cy As Integer, scale As Double)
        If selectedMetric = MetricType.Schwarzschild Or selectedMetric = MetricType.Kerr Then
            Dim rs = 2 * mass * G / (C * C) / (G / (C * C))
            Dim horizonRadius = scale * rs / 10
            
            Using pen As New Pen(Color.FromArgb(150, 255, 0, 0), 3)
                pen.DashStyle = DashStyle.Dash
                g.DrawEllipse(pen, cx - horizonRadius, cy - horizonRadius, 
                             horizonRadius * 2, horizonRadius * 2)
                
                ' Label
                Using font As New Font("Segoe UI", 8)
                    Using brush As New SolidBrush(Color.FromArgb(150, 255, 100, 100))
                        g.DrawString("Event Horizon", font, brush, cx - 40, cy - horizonRadius - 20)
                    End Using
                End Using
            End Using
        End If
    End Sub
    
    Private Sub DrawCoordinateLabels(g As Graphics, panel As Panel)
        Using font As New Font("Consolas", 9)
            Using brush As New SolidBrush(Color.FromArgb(150, 255, 255, 255))
                g.DrawString($"r = {r:F1} GM/c²", font, brush, 10, 10)
                g.DrawString($"θ = {theta:F2} rad", font, brush, 10, 30)
                g.DrawString($"φ = {phi:F2} rad", font, brush, 10, 50)
                
                If selectedMetric = MetricType.Schwarzschild Then
                    Dim rs = 2 * mass * G / (C * C) / (G / (C * C))
                    g.DrawString($"r_s = {rs:F2} GM/c²", font, brush, 10, 70)
                End If
            End Using
        End Using
    End Sub
    
    Private Sub OnMetricChanged(sender As Object, e As EventArgs)
        selectedMetric = DirectCast(comboMetric.SelectedIndex, MetricType)
        CalculateMetric()
        UpdateDisplay()
    End Sub
    
    Private Sub ToggleAnimation(sender As Object, e As EventArgs)
        isAnimating = Not isAnimating
        
        If isAnimating Then
            btnAnimate.Text = "⏹ Stop"
            btnAnimate.BackColor = Color.FromArgb(200, 50, 50)
            
            animationTimer = New Timer()
            animationTimer.Interval = 100
            AddHandler animationTimer.Tick, AddressOf Animate
            animationTimer.Start()
        Else
            btnAnimate.Text = "▶ Animate"
            btnAnimate.BackColor = Color.FromArgb(50, 100, 200)
            
            If animationTimer IsNot Nothing Then
                animationTimer.Stop()
                animationTimer.Dispose()
                animationTimer = Nothing
            End If
        End If
    End Sub
    
    Private Sub Animate(sender As Object, e As EventArgs)
        ' Animate parameters to show dynamic behavior
        t += 0.05
        
        ' Oscillate r slightly
        r = 10 + 2 * Sin(t * 0.5)
        trackBarR.Value = CInt(r * 10)
        
        ' Oscillate theta
        theta = PI / 4 + 0.3 * Sin(t * 0.3)
        trackBarTheta.Value = CInt(theta * 180 / PI)
        
        CalculateMetric()
        UpdateDisplay()
    End Sub
    
    Private Sub ResetView(sender As Object, e As EventArgs)
        t = 0
        r = 10
        theta = PI / 4
        phi = PI / 4
        mass = 1.0
        
        trackBarR.Value = 100
        trackBarTheta.Value = 45
        trackBarMass.Value = 10
        
        CalculateMetric()
        UpdateDisplay()
    End Sub
    
    Public Shared Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New MetricTensorExplorer())
    End Sub
End Class
