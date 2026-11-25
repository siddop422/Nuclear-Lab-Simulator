using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace NuclearLabSimulator;

public sealed class MainForm : Form
{
    private static readonly Color Background = Color.FromArgb(10, 16, 22);
    private static readonly Color Sidebar = Color.FromArgb(13, 21, 29);
    private static readonly Color Surface = Color.FromArgb(20, 30, 40);
    private static readonly Color SurfaceAlt = Color.FromArgb(25, 38, 50);
    private static readonly Color Border = Color.FromArgb(43, 59, 72);
    private static readonly Color Muted = Color.FromArgb(145, 162, 177);
    private static readonly Color Ink = Color.FromArgb(235, 241, 245);
    private static readonly Color Cyan = Color.FromArgb(62, 205, 220);
    private static readonly Color Orange = Color.FromArgb(255, 151, 74);

    private readonly Panel _workspace = new();
    private readonly Label _status = new();
    private readonly Label _elapsedLabel = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 40 };
    private readonly Random _random = new(73);
    private readonly List<double> _history = new();
    private LabVisualPanel? _visual;
    private ChartPanel? _chart;
    private Label? _metric1;
    private Label? _metric2;
    private Label? _metric3;
    private Label? _metric4;
    private Label? _readout1;
    private Label? _readout2;
    private Label? _readout3;
    private Label? _readout4;
    private TrackBar? _temperature;
    private TrackBar? _density;
    private TrackBar? _confinement;
    private TrackBar? _field;
    private TrackBar? _fuel;
    private TrackBar? _mass;
    private TrackBar? _spin;
    private TrackBar? _accretion;
    private Button? _runButton;
    private string _activeLab = "";
    private bool _running;
    private double _elapsed;
    private double _phase;

    private int Temperature => _temperature?.Value ?? 15;
    private int Density => _density?.Value ?? 65;
    private int Confinement => _confinement?.Value ?? 32;
    private int Field => _field?.Value ?? 60;
    private int Fuel => _fuel?.Value ?? 50;
    private int MassExponent => _mass?.Value ?? 5;
    private int Spin => _spin?.Value ?? 70;
    private int Accretion => _accretion?.Value ?? 60;
    private double BlackHoleMass => Math.Pow(10, MassExponent);

    private double FusionQ
    {
        get
        {
            var thermal = Math.Exp(-Math.Pow((Temperature - 15.0) / 9.5, 2));
            var density = Math.Pow(Density / 65.0, 1.65);
            var confinement = Math.Pow(Confinement / 32.0, 0.82);
            var magnetic = Math.Pow(Field / 60.0, 0.45);
            var fuelMix = 1.0 - Math.Pow((Fuel - 50.0) / 50.0, 2) * 0.18;
            var stability = Math.Max(0.08, 1.0 - Math.Max(0, Density - 82) * 0.72 / 100.0);
            return 1.35 * thermal * density * confinement * magnetic * fuelMix * stability;
        }
    }

    private double FusionPower => FusionQ * 50.0;
    private double PlasmaStability => Math.Clamp(100 - Math.Abs(Temperature - 15) * 1.9 - Math.Max(0, Density - 75) * 0.9 - Math.Max(0, Field - 72) * 0.4, 5, 99);
    private double SchwarzschildKm => 2.953 * BlackHoleMass;
    private double HawkingKelvin => 6.17e-8 / BlackHoleMass;
    private double OrbitalHours
    {
        get
        {
            const double G = 6.67430e-11;
            const double SolarMassKg = 1.98847e30;
            var massKg = BlackHoleMass * SolarMassKg;
            var radiusM = 6 * SchwarzschildKm * 1000;
            return 2 * Math.PI * Math.Sqrt(Math.Pow(radiusM, 3) / (G * massKg)) / 3600;
        }
    }

    public MainForm()
    {
        Text = "Nuclear Lab Simulator";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1420, 900);
        MinimumSize = new Size(1120, 720);
        BackColor = Background;
        ForeColor = Ink;
        Font = new Font("Segoe UI", 9F);
        DoubleBuffered = true;
        BuildShell();
        BuildOverview();
        _timer.Tick += OnTick;
    }

    private void BuildShell()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.FromArgb(13, 20, 27), Padding = new Padding(22, 0, 20, 0) };
        var brand = new Label { Text = "N / L", AutoSize = true, ForeColor = Cyan, Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold), Location = new Point(22, 15) };
        var brandText = new Label { Text = "NUCLEAR LAB  /  RESEARCH SUITE", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold), Location = new Point(88, 23) };
        _status.Text = "READY  /  SELECT A LAB";
        _status.AutoSize = true;
        _status.ForeColor = Muted;
        _status.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _status.Location = new Point(Width - 300, 25);
        header.Controls.Add(brand);
        header.Controls.Add(brandText);
        header.Controls.Add(_status);
        header.Resize += (_, _) => _status.Location = new Point(header.Width - _status.Width - 24, 25);

        var nav = new Panel { Dock = DockStyle.Left, Width = 218, BackColor = Sidebar, Padding = new Padding(15, 20, 15, 15) };
        nav.Controls.Add(new Label { Text = "WORKSPACE", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), Location = new Point(18, 12) });
        nav.Controls.Add(MakeNavButton("⌂   Overview", 50, true, () => { StopLab(); BuildOverview(); }));
        nav.Controls.Add(MakeNavButton("◉   Nuclear fusion", 96, false, OpenFusion));
        nav.Controls.Add(MakeNavButton("●   Black hole", 142, false, OpenBlackHole));
        nav.Controls.Add(new Label { Text = "LOCAL MODE\nNo account  •  No network\n\nModels are simplified for\nlearning and exploration.", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 8.2F), Location = new Point(18, 250) });

        _workspace.Dock = DockStyle.Fill;
        _workspace.BackColor = Background;
        Controls.Add(_workspace);
        Controls.Add(nav);
        Controls.Add(header);
    }

    private Button MakeNavButton(string caption, int y, bool selected, Action click)
    {
        var b = new Button
        {
            Text = caption, Location = new Point(8, y), Size = new Size(185, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = selected ? Color.FromArgb(27, 47, 57) : Sidebar,
            ForeColor = selected ? Cyan : Muted,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => click();
        return b;
    }

    private void BuildOverview()
    {
        _activeLab = "";
        ClearWorkspace();
        var page = new Panel { Dock = DockStyle.Fill, Padding = new Padding(34, 30, 34, 28), AutoScroll = true };
        page.Controls.Add(new Label { Text = "RESEARCH ENVIRONMENT  /  01", AutoSize = true, ForeColor = Cyan, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), Location = new Point(34, 30) });
        page.Controls.Add(new Label { Text = "Choose your laboratory", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI Semibold", 27F, FontStyle.Bold), Location = new Point(32, 58) });
        page.Controls.Add(new Label { Text = "Explore stellar-scale physics and the extreme state of matter.", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 10F), Location = new Point(35, 110) });
        page.Controls.Add(CreateLabCard("01", "NUCLEAR FUSION LAB", "Magnetic confinement  /  Plasma performance", "Tune a tokamak-inspired plasma, balance its operating window and watch the model respond.", Cyan, new Point(36, 180), OpenFusion));
        page.Controls.Add(CreateLabCard("02", "BLACK HOLE OBSERVATORY", "Gravity  /  Accretion physics", "Vary black-hole mass, spin and feeding activity while observing the event-horizon scale.", Orange, new Point(36, 395), OpenBlackHole));
        var note = new Label { Text = "EDUCATIONAL MODEL  •  NOT A SCIENTIFIC OR ENGINEERING SOLVER", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Location = new Point(38, 640) };
        page.Controls.Add(note);
        _workspace.Controls.Add(page);
        _status.Text = "READY  /  SELECT A LAB";
    }

    private Panel CreateLabCard(string number, string title, string sub, string description, Color accent, Point location, Action open)
    {
        var card = new Panel { Location = location, Size = new Size(780, 185), BackColor = Surface };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            using var edge = new SolidBrush(accent);
            e.Graphics.FillRectangle(edge, 0, 0, 4, card.Height);
        };
        card.Controls.Add(new Label { Text = number, AutoSize = true, ForeColor = accent, Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold), Location = new Point(24, 20) });
        card.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold), Location = new Point(76, 21) });
        card.Controls.Add(new Label { Text = sub, AutoSize = true, ForeColor = accent, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), Location = new Point(78, 52) });
        card.Controls.Add(new Label { Text = description, AutoSize = false, Size = new Size(570, 42), ForeColor = Muted, Font = new Font("Segoe UI", 9F), Location = new Point(78, 82) });
        card.Controls.Add(MakeButton("OPEN LAB  →", accent, 78, 137, 145, 32, open));
        return card;
    }

    private void OpenFusion()
    {
        StopLab();
        _activeLab = "fusion";
        BuildLabPage("NUCLEAR FUSION LAB", "TOKAMAK STUDY  /  DEUTERIUM–TRITIUM PLASMA", Cyan, "Adjust the operating conditions and observe an illustrative confinement-performance model.");
        var controls = CreateControlsPanel();
        _temperature = AddSlider(controls, "PLASMA TEMPERATURE", "keV", 1, 30, 15, v => UpdateFusion());
        _density = AddSlider(controls, "FUEL DENSITY", "×10²⁰ m⁻³", 10, 100, 65, v => UpdateFusion());
        _confinement = AddSlider(controls, "CONFINEMENT TIME", "×0.1 s", 5, 100, 32, v => UpdateFusion());
        _field = AddSlider(controls, "MAGNETIC FIELD", "×0.1 T", 10, 80, 60, v => UpdateFusion());
        _fuel = AddSlider(controls, "DEUTERIUM SHARE", "%", 10, 90, 50, v => UpdateFusion());
        FinishLabPage(controls, "fusion");
        UpdateFusion();
    }

    private void OpenBlackHole()
    {
        StopLab();
        _activeLab = "blackhole";
        BuildLabPage("BLACK HOLE OBSERVATORY", "RELATIVISTIC ASTROPHYSICS  /  ACCRETION STUDY", Orange, "Explore a schematic black hole and the scale of its event horizon.");
        var controls = CreateControlsPanel();
        _mass = AddSlider(controls, "BLACK HOLE MASS", "log₁₀ solar masses", 1, 9, 5, v => UpdateBlackHole());
        _spin = AddSlider(controls, "DIMENSIONLESS SPIN", "×0.01", 0, 99, 70, v => UpdateBlackHole());
        _accretion = AddSlider(controls, "ACCRETION ACTIVITY", "%", 0, 100, 60, v => UpdateBlackHole());
        FinishLabPage(controls, "blackhole");
        UpdateBlackHole();
    }

    private void BuildLabPage(string title, string subtitle, Color accent, string description)
    {
        ClearWorkspace();
        var page = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), AutoScroll = true };
        var back = MakeButton("←  OVERVIEW", SurfaceAlt, 0, 0, 132, 34, () => { StopLab(); BuildOverview(); });
        page.Controls.Add(back);
        page.Controls.Add(new Label { Text = title, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold), Location = new Point(0, 47) });
        page.Controls.Add(new Label { Text = subtitle, AutoSize = true, ForeColor = accent, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), Location = new Point(2, 82) });
        page.Controls.Add(new Label { Text = description, AutoSize = true, ForeColor = Muted, Location = new Point(2, 107) });
        var run = MakeButton("▶  START EXPERIMENT", accent, 900, 45, 190, 38, ToggleRun);
        run.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _runButton = run;
        page.Controls.Add(run);
        _elapsedLabel.Text = "ELAPSED  00:00";
        _elapsedLabel.AutoSize = true;
        _elapsedLabel.ForeColor = Muted;
        _elapsedLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _elapsedLabel.Location = new Point(900, 91);
        page.Controls.Add(_elapsedLabel);
        page.Resize += (_, _) =>
        {
            _runButton!.Location = new Point(page.ClientSize.Width - 220, 45);
            _elapsedLabel.Location = new Point(page.ClientSize.Width - 220, 91);
        };
        _workspace.Controls.Add(page);
    }

    private FlowLayoutPanel CreateControlsPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Location = new Point(0, 150), Size = new Size(380, 570), AutoScroll = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            BackColor = Surface, Padding = new Padding(18), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
        };
        panel.Controls.Add(new Label { Text = "EXPERIMENT PARAMETERS", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) });
        return panel;
    }

    private void FinishLabPage(FlowLayoutPanel controls, string lab)
    {
        var page = _workspace.Controls.OfType<Panel>().First();
        var visual = new LabVisualPanel { Location = new Point(405, 150), Size = new Size(690, 365), Accent = lab == "fusion" ? Cyan : Orange, Mode = lab, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        var metrics = new TableLayoutPanel { Location = new Point(405, 530), Size = new Size(690, 105), ColumnCount = 4, RowCount = 1, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        for (int i = 0; i < 4; i++) metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        _metric1 = AddMetric(metrics, 0);
        _metric2 = AddMetric(metrics, 1);
        _metric3 = AddMetric(metrics, 2);
        _metric4 = AddMetric(metrics, 3);
        var readouts = new Panel { Location = new Point(405, 645), Size = new Size(690, 74), BackColor = Surface, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _readout1 = AddReadout(readouts, "SYSTEM STATUS", 18);
        _readout2 = AddReadout(readouts, "MODEL OUTPUT", 185);
        _readout3 = AddReadout(readouts, "REFERENCE", 352);
        _readout4 = AddReadout(readouts, "MODE", 519);
        _chart = new ChartPanel { Location = new Point(405, 730), Size = new Size(690, 145), Accent = lab == "fusion" ? Cyan : Orange, Values = _history, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        page.Controls.Add(controls);
        page.Controls.Add(visual);
        page.Controls.Add(metrics);
        page.Controls.Add(readouts);
        page.Controls.Add(_chart);
        _visual = visual;
        page.Resize += (_, _) =>
        {
            int right = Math.Max(460, page.ClientSize.Width - 24);
            visual.Width = Math.Max(420, right - 405);
            metrics.Width = visual.Width;
            readouts.Width = visual.Width;
            _chart.Width = visual.Width;
        };
        _status.Text = lab == "fusion" ? "LAB OPEN  /  FUSION PLASMA" : "LAB OPEN  /  BLACK HOLE";
    }

    private Label AddMetric(TableLayoutPanel table, int column)
    {
        var box = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(10) };
        box.Controls.Add(new Label { Text = "—", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold), Location = new Point(10, 9), Name = "value" });
        box.Controls.Add(new Label { Text = "METRIC", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold), Location = new Point(11, 49) });
        table.Controls.Add(box, column, 0);
        return (Label)box.Controls["value"]!;
    }

    private Label AddReadout(Panel panel, string caption, int x)
    {
        var value = new Label { Text = "—", AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold), Location = new Point(x, 38) };
        panel.Controls.Add(new Label { Text = caption, AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold), Location = new Point(x, 14) });
        panel.Controls.Add(value);
        return value;
    }

    private TrackBar AddSlider(FlowLayoutPanel parent, string caption, string unit, int min, int max, int initial, Action<int> changed)
    {
        var block = new Panel { Size = new Size(320, 82), BackColor = Surface };
        var title = new Label { Text = caption, AutoSize = true, ForeColor = Ink, Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold), Location = new Point(0, 2) };
        var value = new Label { Text = $"{initial} {unit}", AutoSize = true, ForeColor = Cyan, Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold), Location = new Point(208, 2) };
        value.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        var bar = new TrackBar { Minimum = min, Maximum = max, Value = initial, TickStyle = TickStyle.None, SmallChange = 1, LargeChange = Math.Max(1, (max - min) / 10), Location = new Point(-5, 26), Size = new Size(310, 43), BackColor = Surface };
        bar.ValueChanged += (_, _) => { value.Text = $"{bar.Value} {unit}"; changed(bar.Value); };
        block.Controls.Add(title);
        block.Controls.Add(value);
        block.Controls.Add(bar);
        parent.Controls.Add(block);
        return bar;
    }

    private Button MakeButton(string text, Color color, int x, int y, int width, int height, Action click)
    {
        var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height), FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = color == SurfaceAlt ? Ink : Color.FromArgb(7, 16, 22), Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold), Cursor = Cursors.Hand };
        b.FlatAppearance.BorderSize = 0;
        b.Click += (_, _) => click();
        return b;
    }

    private void ToggleRun()
    {
        _running = !_running;
        if (_running)
        {
            _timer.Start();
            _runButton!.Text = "Ⅱ  PAUSE EXPERIMENT";
            _status.Text = "SIMULATION RUNNING";
        }
        else
        {
            _timer.Stop();
            _runButton!.Text = "▶  RESUME EXPERIMENT";
            _status.Text = "SIMULATION PAUSED";
        }
        _visual?.Invalidate();
    }

    private void StopLab()
    {
        _timer.Stop();
        _running = false;
        _elapsed = 0;
        _history.Clear();
        _visual = null;
        _chart = null;
        _runButton = null;
    }

    private void ClearWorkspace()
    {
        foreach (Control control in _workspace.Controls) control.Dispose();
        _workspace.Controls.Clear();
        _visual = null;
        _chart = null;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _phase += 0.045;
        _elapsed += _timer.Interval / 1000.0;
        if (_activeLab == "fusion") _history.Add(FusionPower * (0.97 + _random.NextDouble() * 0.06));
        if (_activeLab == "blackhole") _history.Add(Math.Max(0.1, Accretion * (0.96 + _random.NextDouble() * 0.08)));
        if (_history.Count > 100) _history.RemoveAt(0);
        _elapsedLabel.Text = $"ELAPSED  {TimeSpan.FromSeconds(_elapsed):mm\\:ss}";
        _visual?.Invalidate();
        _chart?.Invalidate();
        UpdateMetricsOnly();
    }

    private void UpdateFusion()
    {
        if (_activeLab != "fusion") return;
        UpdateMetricsOnly();
        if (_visual is not null) { _visual.Temperature = Temperature; _visual.FuelShare = Fuel; _visual.Invalidate(); }
        _chart?.Invalidate();
    }

    private void UpdateBlackHole()
    {
        if (_activeLab != "blackhole") return;
        UpdateMetricsOnly();
        if (_visual is not null) { _visual.Spin = Spin; _visual.MassExponent = MassExponent; _visual.Accretion = Accretion; _visual.Invalidate(); }
        _chart?.Invalidate();
    }

    private void UpdateMetricsOnly()
    {
        if (_activeLab == "fusion")
        {
            double q = FusionQ;
            _metric1!.Text = $"{q:0.00}";
            _metric2!.Text = $"{FusionPower:0.0} MW";
            _metric3!.Text = $"{PlasmaStability:0}%";
            _metric4!.Text = $"{_elapsed:0.0} s";
            _readout1!.Text = PlasmaStability >= 65 ? "STABLE WINDOW" : "OFF-NOMINAL";
            _readout2!.Text = $"Q = {q:0.00}";
            _readout3!.Text = $"{Temperature} keV";
            _readout4!.Text = _running ? "LIVE MODEL" : "READY";
        }
        else if (_activeLab == "blackhole")
        {
            _metric1!.Text = FormatDistance(SchwarzschildKm) + " km";
            _metric2!.Text = $"{BlackHoleMass:0.0E+0} M☉";
            _metric3!.Text = $"{HawkingKelvin:0.000E+0} K";
            _metric4!.Text = $"{OrbitalHours:0.00} h";
            _readout1!.Text = "HORIZON SCALE";
            _readout2!.Text = $"SPIN  {Spin / 100.0:0.00}";
            _readout3!.Text = $"ACCRETION  {Accretion}%";
            _readout4!.Text = _running ? "LIVE MODEL" : "READY";
        }
    }

    private static string FormatDistance(double km)
    {
        if (km < 1_000_000) return km.ToString("N0", CultureInfo.InvariantCulture);
        if (km < 1.0e9) return (km / 1.0e6).ToString("0.00", CultureInfo.InvariantCulture) + "M";
        return (km / 1.0e9).ToString("0.00", CultureInfo.InvariantCulture) + "B";
    }
}

internal sealed class LabVisualPanel : Panel
{
    public string Mode { get; set; } = "fusion";
    public Color Accent { get; set; } = Color.Cyan;
    public int Temperature { get; set; } = 15;
    public int FuelShare { get; set; } = 50;
    public int MassExponent { get; set; } = 5;
    public int Spin { get; set; } = 70;
    public int Accretion { get; set; } = 60;
    private double _phase;

    public LabVisualPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(16, 25, 34);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var bg = new LinearGradientBrush(ClientRectangle, Color.FromArgb(15, 28, 39), Color.FromArgb(9, 15, 22), 90);
        g.FillRectangle(bg, ClientRectangle);
        using var grid = new Pen(Color.FromArgb(28, 91, 111, 130), 1);
        for (int x = 0; x < Width; x += 34) g.DrawLine(grid, x, 0, x, Height);
        for (int y = 0; y < Height; y += 34) g.DrawLine(grid, 0, y, Width, y);
        if (Mode == "fusion") DrawFusion(g); else DrawBlackHole(g);
        using var border = new Pen(Color.FromArgb(55, 75, 90));
        g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    private void DrawFusion(Graphics g)
    {
        float cx = Width * .53f, cy = Height * .51f;
        float scale = Math.Min(Width / 690f, Height / 365f);
        float rx = 170 * scale, ry = 86 * scale;
        using var title = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        using var sub = new SolidBrush(Color.FromArgb(145, 162, 177));
        using var accent = new Pen(Color.FromArgb(110, Accent), 2.2f * scale);
        using var tube = new Pen(Color.FromArgb(100, 83, 175, 202), 15 * scale);
        using var core = new SolidBrush(Color.FromArgb(30, 115, 202, 226));
        g.DrawString("MAGNETIC CONFINEMENT CHAMBER", title, new SolidBrush(Accent), 22, 18);
        g.DrawString("SCHEMATIC PLASMA VIEW  /  NOT TO SCALE", new Font("Segoe UI", 8F), sub, 22, 38);
        g.DrawEllipse(tube, cx - rx, cy - ry * .42f, rx * 2, ry * .9f);
        g.DrawEllipse(accent, cx - rx, cy - ry * .42f, rx * 2, ry * .9f);
        using var plasma = new SolidBrush(Color.FromArgb(65, 255, 136, 64));
        using var plasmaGlow = new Pen(Color.FromArgb(80, 255, 157, 86), 24 * scale);
        float heat = Math.Clamp(Temperature / 30f, .3f, 1f);
        g.DrawEllipse(plasmaGlow, cx - rx * .6f, cy - ry * .28f, rx * 1.2f, ry * .54f);
        g.FillEllipse(plasma, cx - rx * .55f, cy - ry * .23f, rx * 1.1f, ry * .46f);
        using var orbit = new Pen(Color.FromArgb(155, 109, 199, 217), 1.6f * scale);
        for (int i = 0; i < 7; i++)
        {
            double a = _phase + i * Math.PI * 2 / 7;
            float x = cx + (float)Math.Cos(a) * rx * .78f;
            float y = cy + (float)Math.Sin(a) * ry * .29f;
            float size = (4 + heat * 3) * scale;
            using var particle = new SolidBrush(Color.FromArgb(180, 190, 231, 245));
            g.FillEllipse(particle, x - size / 2, y - size / 2, size, size);
        }
        using var foot = new Font("Segoe UI", 9F);
        g.DrawString($"PLASMA TEMPERATURE  {Temperature} keV", foot, sub, 22, Height - 52);
        g.DrawString($"FUEL MIX  D {FuelShare}%  /  T {100 - FuelShare}%", foot, sub, 22, Height - 32);
        g.DrawString("FIELD COILS  •  D–T PLASMA", foot, new SolidBrush(Accent), Width - 235, Height - 32);
    }

    private void DrawBlackHole(Graphics g)
    {
        float cx = Width * .53f, cy = Height * .53f;
        float massScale = .78f + (MassExponent - 1) / 8f * .22f;
        float baseR = Math.Min(Width * .28f, Height * .38f) * massScale;
        float r = baseR * (0.65f + Math.Min(Spin, 99) / 99f * .12f);
        using var title = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        using var muted = new SolidBrush(Color.FromArgb(145, 162, 177));
        g.DrawString("ACCRETION DISK  /  EVENT HORIZON", title, new SolidBrush(Accent), 22, 18);
        g.DrawString("LENSING-INSPIRED SCHEMATIC VIEW", new Font("Segoe UI", 8F), muted, 22, 38);
        for (int i = 9; i >= 0; i--)
        {
            float grow = i * 9f * (0.4f + Accretion / 100f);
            using var disk = new Pen(Color.FromArgb(22 + i * 13, 255, 175 - i * 9, 80 - i * 4), (8 + i * .8f));
            g.DrawEllipse(disk, cx - r - grow, cy - r * .43f - grow * .22f, (r + grow) * 2, r * .86f + grow * .44f);
        }
        using var inner = new Pen(Color.FromArgb(180, 255, 204, 135), 2.2f);
        g.DrawEllipse(inner, cx - r, cy - r * .4f, r * 2, r * .8f);
        using var halo = new Pen(Color.FromArgb(40, 135, 167, 191), 18);
        g.DrawEllipse(halo, cx - r * .92f, cy - r * .92f, r * 1.84f, r * 1.84f);
        using var horizon = new SolidBrush(Color.Black);
        g.FillEllipse(horizon, cx - r * .72f, cy - r * .72f, r * 1.44f, r * 1.44f);
        using var rim = new Pen(Color.FromArgb(215, 231, 222, 211), 1.4f);
        g.DrawEllipse(rim, cx - r * .72f, cy - r * .72f, r * 1.44f, r * 1.44f);
        using var foot = new Font("Segoe UI", 9F);
        g.DrawString($"MASS  10^{MassExponent}  M☉", foot, muted, 22, Height - 52);
        g.DrawString($"SPIN  {Spin / 100.0:0.00}  /  ACCRETION  {Accretion}%", foot, muted, 22, Height - 32);
        g.DrawString("EVENT HORIZON", new Font("Segoe UI Semibold", 8F, FontStyle.Bold), new SolidBrush(Accent), Width - 165, Height - 32);
        _phase += .012;
    }

}

internal sealed class ChartPanel : Panel
{
    public Color Accent { get; set; } = Color.Cyan;
    public List<double> Values { get; set; } = new();

    public ChartPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(20, 30, 40);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var titleFont = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
        using var muted = new SolidBrush(Color.FromArgb(145, 162, 177));
        g.DrawString("LIVE OUTPUT TRACE", titleFont, muted, 16, 10);
        int left = 44, right = Width - 18, top = 36, bottom = Height - 22;
        using var grid = new Pen(Color.FromArgb(40, 100, 120, 135));
        for (int i = 0; i <= 3; i++)
        {
            float y = top + (bottom - top) * i / 3f;
            g.DrawLine(grid, left, y, right, y);
        }
        if (Values.Count > 1)
        {
            double max = Math.Max(1, Values.Max() * 1.15);
            var points = Values.Select((v, i) => new PointF(left + (right - left) * i / Math.Max(1f, Values.Count - 1), bottom - (float)(Math.Clamp(v / max, 0, 1) * (bottom - top)))).ToArray();
            using var line = new Pen(Accent, 2.1f);
            line.LineJoin = LineJoin.Round;
            g.DrawLines(line, points);
            using var dot = new SolidBrush(Accent);
            g.FillEllipse(dot, points[^1].X - 3, points[^1].Y - 3, 6, 6);
        }
        else
        {
            g.DrawString("Start the experiment to record a live trace.", new Font("Segoe UI", 8.5F), muted, left + 8, top + 22);
        }
    }
}
