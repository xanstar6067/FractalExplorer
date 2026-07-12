using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FractalExplorerWPF.Infrastructure.ColorPicking;
using FractalExplorerWPF.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Views;

public partial class FlameTransformEditorWindow : Window
{
    private readonly List<FlameTransform> _transforms;
    private readonly Stack<Snapshot> _undo = new();
    private readonly ColorSelectionService _picker = ColorSelectionService.Default;
    private int _selectedIndex = -1; private bool _syncing;
    public event Action<IReadOnlyList<FlameTransform>>? TransformsApplied;
    public List<FlameTransform> ResultTransforms { get; private set; }

    public FlameTransformEditorWindow(IEnumerable<FlameTransform> transforms)
    {
        InitializeComponent(); _transforms = transforms.Select(t => t.Clone()).ToList(); ResultTransforms = _transforms;
        VariationBox.ItemsSource = Enum.GetValues<FlameVariation>(); Rebind();
        if (_transforms.Count > 0) TransformList.SelectedIndex = 0; else EnableEditor(false);
    }

    private void Rebind(int? selected = null)
    {
        int index = selected ?? _selectedIndex; TransformList.ItemsSource = null; TransformList.ItemsSource = _transforms;
        TransformList.SelectedIndex = _transforms.Count == 0 ? -1 : Math.Clamp(index, 0, _transforms.Count - 1); UpdateTotal();
    }
    private void RefreshCard() { TransformList.Items.Refresh(); UpdateTotal(); UpdateWeightLabels(WeightSlider.Value); UpdateMatrix(); }
    private void TransformList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TransformList.SelectedIndex < 0) return; _selectedIndex = TransformList.SelectedIndex; LoadEditor(_transforms[_selectedIndex]);
    }
    private void LoadEditor(FlameTransform t)
    {
        _syncing = true; try { VariationBox.SelectedItem = t.Variation; ColorPreview.Background = new SolidColorBrush(t.Color); WeightSlider.Value = Math.Clamp(t.Weight, 0, 10); ABox.Text = F(t.A); BBox.Text = F(t.B); CBox.Text = F(t.C); DBox.Text = F(t.D); EBox.Text = F(t.E); FBox.Text = F(t.F); EditorTitle.Text = $"Трансформация {_selectedIndex + 1}"; UpdateWeightLabels(t.Weight); UpdateMatrix(); EnableEditor(true); } finally { _syncing = false; }
    }
    private void EnableEditor(bool value) { EditorPanel.IsEnabled = value; if (!value) EditorTitle.Text = "Нет трансформаций — нажмите «Добавить»"; }
    private void Editor_OnChanged(object sender, EventArgs e)
    {
        if (_syncing || _selectedIndex < 0) return; FlameTransform t = _transforms[_selectedIndex];
        PushUndo(); if (sender == VariationBox && VariationBox.SelectedItem is FlameVariation v) t.Variation = v;
        else if (sender is TextBox box && TryRead(box.Text, out double value))
        { if (box == ABox) t.A=value; else if(box==BBox)t.B=value; else if(box==CBox)t.C=value; else if(box==DBox)t.D=value; else if(box==EBox)t.E=value; else if(box==FBox)t.F=value; }
        RefreshCard();
    }
    private void Weight_OnChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateWeightLabels(e.NewValue); if (_syncing || _selectedIndex < 0) return; PushUndo(); _transforms[_selectedIndex].Weight = e.NewValue; RefreshCard();
    }
    private void Color_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedIndex < 0) return; Color initial = _transforms[_selectedIndex].Color; if (!_picker.TrySelectColor(this, initial, out Color selected)) return;
        PushUndo(); _transforms[_selectedIndex].Color = selected; ColorPreview.Background = new SolidColorBrush(selected); RefreshCard();
    }
    private void Add_OnClick(object sender, RoutedEventArgs e)
    {
        PushUndo(); _transforms.Add(new FlameTransform { Weight=1, A=.5, E=.5, Color=Colors.White }); EnableEditor(true); Rebind(_transforms.Count - 1);
    }
    private void Delete_OnClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not FlameTransform t) return; int index = _transforms.IndexOf(t); if (index < 0) return;
        PushUndo(); _transforms.RemoveAt(index); _selectedIndex = _transforms.Count == 0 ? -1 : Math.Min(index, _transforms.Count - 1); Rebind(); if (_selectedIndex < 0) EnableEditor(false);
    }
    private void Randomize_OnClick(object sender, RoutedEventArgs e) { PushUndo(); int selected = _selectedIndex; _transforms.Clear(); _transforms.AddRange(CreateRandom()); Rebind(Math.Clamp(selected, 0, _transforms.Count - 1)); Commit(); }
    private void Undo_OnClick(object sender, RoutedEventArgs e)
    {
        if (_undo.Count == 0) return; Snapshot s = _undo.Pop(); _transforms.Clear(); _transforms.AddRange(s.Transforms.Select(t => t.Clone())); _selectedIndex = s.SelectedIndex; Rebind(); EnableEditor(_transforms.Count > 0); UndoButton.IsEnabled = _undo.Count > 0;
    }
    private void PushUndo()
    {
        var snapshot = new Snapshot(_transforms.Select(t => t.Clone()).ToList(), _selectedIndex); if (_undo.TryPeek(out Snapshot? last) && last.Same(snapshot)) return; _undo.Push(snapshot); UndoButton.IsEnabled = true;
    }
    private void Apply_OnClick(object sender, RoutedEventArgs e) => Commit();
    private void Done_OnClick(object sender, RoutedEventArgs e) { Commit(); DialogResult = true; }
    private void Commit() { ResultTransforms = _transforms.Where(t => t.Weight > 0).Select(t => t.Clone()).ToList(); TransformsApplied?.Invoke(ResultTransforms); }
    private void UpdateTotal() { double total = _transforms.Sum(t => t.Weight); TotalWeightText.Text = $"Σ {total:F2}"; TotalWeightText.Foreground = Math.Abs(total - 1) < .001 || _transforms.Count == 0 ? (Brush)FindResource("Theme.SecondaryTextBrush") : Brushes.OrangeRed; }
    private void UpdateWeightLabels(double weight) { WeightText.Text = weight.ToString("F3"); double total = _transforms.Sum(t => t.Weight); if (_selectedIndex >= 0) total = total - _transforms[_selectedIndex].Weight + weight; WeightPercentText.Text = total > 0 ? $"{Math.Round(weight / total * 100):0}%" : "—"; }
    private void UpdateMatrix() { if (_selectedIndex < 0) return; FlameTransform t = _transforms[_selectedIndex]; MatrixText.Text = $"┌ {t.A,10:F6}  {t.B,10:F6}  {t.C,10:F6} ┐\n└ {t.D,10:F6}  {t.E,10:F6}  {t.F,10:F6} ┘\n\n{t.Variation}"; }
    private static string F(double v) => v.ToString("0.########", CultureInfo.InvariantCulture);
    private static bool TryRead(string text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

    private static List<FlameTransform> CreateRandom()
    {
        Random random = Random.Shared; int count = random.Next(3, 7); double[] weights = new double[count]; double total = 0;
        for (int i=0;i<count;i++) total += weights[i] = .25 + random.NextDouble() * random.NextDouble() * 2.5;
        double hue = random.NextDouble() * 360; var result = new List<FlameTransform>();
        for (int i=0;i<count;i++) result.Add(RandomTransform(random, weights[i] / total, hue + i * (360d/count) + Range(random,-28,28))); return result;
    }
    private static FlameTransform RandomTransform(Random r,double weight,double hue)
    {
        double angle=Range(r,-Math.PI,Math.PI),sx=Range(r,.22,.82),sy=Range(r,.22,.82),shear=Range(r,-.28,.28),cos=Math.Cos(angle),sin=Math.Sin(angle); FlameVariation variation=Enum.GetValues<FlameVariation>()[r.Next(3)]; double radius=variation==FlameVariation.Spherical?Range(r,.04,.48):Range(r,.12,.92),ta=Range(r,-Math.PI,Math.PI);
        return new FlameTransform { Weight=weight,A=cos*sx+sin*shear,B=-sin*sy,C=Math.Cos(ta)*radius,D=sin*sx,E=cos*sy+cos*shear,F=Math.Sin(ta)*radius,Variation=variation,Color=Hsv(hue,Range(r,.62,.95),Range(r,.72,1)) };
    }
    private static double Range(Random r,double min,double max)=>min+r.NextDouble()*(max-min);
    private static Color Hsv(double hue,double saturation,double value) { hue=((hue%360)+360)%360;double c=value*saturation,x=c*(1-Math.Abs(hue/60%2-1)),m=value-c;(double r,double g,double b)=hue switch{<60=>(c,x,0d),<120=>(x,c,0d),<180=>(0d,c,x),<240=>(0d,x,c),<300=>(x,0d,c),_=>(c,0d,x)};return Color.FromRgb((byte)Math.Round((r+m)*255),(byte)Math.Round((g+m)*255),(byte)Math.Round((b+m)*255)); }
    private sealed record Snapshot(List<FlameTransform> Transforms,int SelectedIndex) { public bool Same(Snapshot other) => SelectedIndex==other.SelectedIndex && Transforms.Count==other.Transforms.Count && Transforms.Zip(other.Transforms).All(x=>Equal(x.First,x.Second)); private static bool Equal(FlameTransform a,FlameTransform b)=>a.Weight==b.Weight&&a.A==b.A&&a.B==b.B&&a.C==b.C&&a.D==b.D&&a.E==b.E&&a.F==b.F&&a.Variation==b.Variation&&a.Color==b.Color; }
}
