using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FractalExplorerWPF.Infrastructure;
using FractalExplorerWPF.Models;
using Color = System.Windows.Media.Color;

namespace FractalExplorerWPF.Views;

public partial class DynamicPaletteWindow : Window
{
    private readonly DynamicPaletteStore _store;
    private readonly List<DynamicPalette> _palettes;
    public DynamicPalette? SelectedPalette => PaletteList.SelectedItem as DynamicPalette;
    public DynamicPaletteWindow(DynamicPaletteStore store,IEnumerable<DynamicPalette> palettes,DynamicPalette? selected)
    {
        _store=store;_palettes=palettes.ToList();InitializeComponent();ModeBox.ItemsSource=new[]{"LegacyBuiltIn","Diverging","Absolute","ZeroBandHighlight","HistogramEqualized","Cycle","Gradient"};PaletteList.ItemsSource=_palettes;PaletteList.SelectedItem=selected??_palettes.FirstOrDefault();
    }
    private void PaletteList_OnSelectionChanged(object sender,SelectionChangedEventArgs e){if(SelectedPalette is not{}p)return;NameBox.Text=p.Name;NameBox.IsReadOnly=p.IsBuiltIn;ModeBox.SelectedItem=p.Mode;ModeBox.IsEnabled=!p.IsBuiltIn;RangeBox.Text=p.ExponentRange.ToString("G",CultureInfo.InvariantCulture);ZeroBox.Text=p.ZeroBandWidth.ToString("G",CultureInfo.InvariantCulture);ColorsBox.Text=string.Join(", ",p.Colors.Select(c=>$"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}"));ColorsBox.IsReadOnly=p.IsBuiltIn;RefreshPreview(p.Colors);}
    private void Clone_OnClick(object sender,RoutedEventArgs e){if(SelectedPalette is not{}p)return;DynamicPalette copy=p.Clone(p.Name+" — копия");_palettes.Add(copy);RefreshList(copy);}
    private void Delete_OnClick(object sender,RoutedEventArgs e){if(SelectedPalette is not{IsBuiltIn:false}p)return;_palettes.Remove(p);RefreshList(_palettes.FirstOrDefault());_store.Save(_palettes);}
    private void Apply_OnClick(object sender,RoutedEventArgs e){SaveEditor();}
    private void Done_OnClick(object sender,RoutedEventArgs e){if(!SaveEditor())return;DialogResult=true;}
    private bool SaveEditor(){if(SelectedPalette is not{}p)return false;if(p.IsBuiltIn)return true;if(string.IsNullOrWhiteSpace(NameBox.Text)||!double.TryParse(RangeBox.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out double range)||!double.TryParse(ZeroBox.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out double zero)){MessageBox.Show(this,"Проверьте название и числовые параметры.");return false;}var colors=new List<Color>();foreach(string part in ColorsBox.Text.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)){if(!TryColor(part,out Color c)){MessageBox.Show(this,$"Некорректный цвет: {part}");return false;}colors.Add(c);}if(colors.Count<2){MessageBox.Show(this,"Нужно минимум два цвета.");return false;}p.Name=NameBox.Text.Trim();p.Mode=ModeBox.SelectedItem as string??"Diverging";p.ExponentRange=Math.Max(1e-9,range);p.ZeroBandWidth=Math.Max(1e-9,zero);p.Colors=colors;_store.Save(_palettes);RefreshList(p);RefreshPreview(colors);return true;}
    private void RefreshList(DynamicPalette? selected){PaletteList.ItemsSource=null;PaletteList.ItemsSource=_palettes;PaletteList.SelectedItem=selected;}
    private void RefreshPreview(IReadOnlyList<Color> colors){var gradient=new LinearGradientBrush{StartPoint=new(.0,.5),EndPoint=new(1,.5)};for(int i=0;i<colors.Count;i++)gradient.GradientStops.Add(new(colors[i],i/(double)Math.Max(1,colors.Count-1)));Preview.Background=gradient;}
    private static bool TryColor(string text,out Color color){color=default;string s=text.Trim().TrimStart('#');if(s.Length==6)s="FF"+s;return s.Length==8&&uint.TryParse(s,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out uint v)&&Assign(out color,Color.FromArgb((byte)(v>>24),(byte)(v>>16),(byte)(v>>8),(byte)v));}
    private static bool Assign(out Color target,Color value){target=value;return true;}
}
