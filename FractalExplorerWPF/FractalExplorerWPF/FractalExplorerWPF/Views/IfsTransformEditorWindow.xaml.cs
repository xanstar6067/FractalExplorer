using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FractalExplorerWPF.Models;
using Brush=System.Windows.Media.Brush;
using Brushes=System.Windows.Media.Brushes;

namespace FractalExplorerWPF.Views;

public partial class IfsTransformEditorWindow:Window
{
    private readonly List<IfsAffineTransform> _transforms;private readonly Stack<Snapshot> _undo=new();private int _selectedIndex=-1;private bool _syncing;
    public List<IfsAffineTransform> ResultTransforms{get;private set;}public event Action<IReadOnlyList<IfsAffineTransform>>? TransformsApplied;
    public IfsTransformEditorWindow(IEnumerable<IfsAffineTransform> source){InitializeComponent();_transforms=source.Select(t=>t.Clone()).ToList();ResultTransforms=_transforms;Rebind();if(_transforms.Count>0)TransformList.SelectedIndex=0;else EnableEditor(false);}
    private void Rebind(int? selected=null){int index=selected??_selectedIndex;TransformList.ItemsSource=null;TransformList.ItemsSource=_transforms;TransformList.SelectedIndex=_transforms.Count==0?-1:Math.Clamp(index,0,_transforms.Count-1);UpdateTotal();}
    private void TransformList_OnSelectionChanged(object sender,SelectionChangedEventArgs e){if(TransformList.SelectedIndex<0)return;_selectedIndex=TransformList.SelectedIndex;LoadEditor(_transforms[_selectedIndex]);}
    private void LoadEditor(IfsAffineTransform t){_syncing=true;try{ABox.Text=F(t.A);BBox.Text=F(t.B);CBox.Text=F(t.C);DBox.Text=F(t.D);EBox.Text=F(t.E);FBox.Text=F(t.F);ProbabilitySlider.Value=Math.Clamp(t.Probability,0,1);EditorTitle.Text=$"Преобразование {_selectedIndex+1}";UpdateProbability(t.Probability);UpdateMatrix();EnableEditor(true);}finally{_syncing=false;}}
    private void EnableEditor(bool value){EditorPanel.IsEnabled=value;if(!value)EditorTitle.Text="Нет преобразований — нажмите «Добавить»";}
    private void Editor_OnChanged(object sender,EventArgs e){if(_syncing||_selectedIndex<0||sender is not TextBox box||!Read(box.Text,out double value))return;PushUndo();IfsAffineTransform t=_transforms[_selectedIndex];if(box==ABox)t.A=value;else if(box==BBox)t.B=value;else if(box==CBox)t.C=value;else if(box==DBox)t.D=value;else if(box==EBox)t.E=value;else if(box==FBox)t.F=value;Refresh();}
    private void Probability_OnChanged(object sender,RoutedPropertyChangedEventArgs<double> e){UpdateProbability(e.NewValue);if(_syncing||_selectedIndex<0)return;PushUndo();_transforms[_selectedIndex].Probability=e.NewValue;Refresh();}
    private void Refresh(){TransformList.Items.Refresh();UpdateTotal();UpdateProbability(ProbabilitySlider.Value);UpdateMatrix();}
    private void UpdateProbability(double p){ProbabilityText.Text=p.ToString("F3");double total=_transforms.Sum(t=>Math.Max(0,t.Probability));if(_selectedIndex>=0)total=total-Math.Max(0,_transforms[_selectedIndex].Probability)+p;ProbabilityPercentText.Text=total>0?$"{Math.Round(p/total*100):0}%":"—";}
    private void UpdateTotal(){double total=_transforms.Sum(t=>Math.Max(0,t.Probability));TotalText.Text=$"Σ {total:F4}";TotalText.Foreground=Math.Abs(total-1)<.0001||_transforms.Count==0?(Brush)FindResource("Theme.SecondaryTextBrush"):Brushes.OrangeRed;}
    private void UpdateMatrix(){if(_selectedIndex<0)return;IfsAffineTransform t=_transforms[_selectedIndex];MatrixText.Text=$"┌ {t.A,10:F6}  {t.B,10:F6}  {t.E,10:F6} ┐\n└ {t.C,10:F6}  {t.D,10:F6}  {t.F,10:F6} ┘";}
    private void Add_OnClick(object sender,RoutedEventArgs e){PushUndo();_transforms.Add(new IfsAffineTransform{A=.5,D=.5,Probability=.5});EnableEditor(true);Rebind(_transforms.Count-1);}
    private void Delete_OnClick(object sender,RoutedEventArgs e){if((sender as FrameworkElement)?.DataContext is not IfsAffineTransform t)return;int index=_transforms.IndexOf(t);if(index<0)return;PushUndo();_transforms.RemoveAt(index);_selectedIndex=_transforms.Count==0?-1:Math.Min(index,_transforms.Count-1);Rebind();if(_selectedIndex<0)EnableEditor(false);}
    private void Normalize_OnClick(object sender,RoutedEventArgs e){double total=_transforms.Sum(t=>Math.Max(0,t.Probability));if(total<=0)return;PushUndo();foreach(IfsAffineTransform t in _transforms)t.Probability=Math.Max(0,t.Probability)/total;Rebind(_selectedIndex);}
    private void Randomize_OnClick(object sender,RoutedEventArgs e){PushUndo();int selected=_selectedIndex;_transforms.Clear();_transforms.AddRange(CreateRandom());Rebind(Math.Clamp(selected,0,_transforms.Count-1));Commit();}
    private void Undo_OnClick(object sender,RoutedEventArgs e){if(_undo.Count==0)return;Snapshot s=_undo.Pop();_transforms.Clear();_transforms.AddRange(s.Transforms.Select(t=>t.Clone()));_selectedIndex=s.SelectedIndex;Rebind();EnableEditor(_transforms.Count>0);UndoButton.IsEnabled=_undo.Count>0;}
    private void PushUndo(){var snapshot=new Snapshot(_transforms.Select(t=>t.Clone()).ToList(),_selectedIndex);if(_undo.TryPeek(out Snapshot? old)&&old.Same(snapshot))return;_undo.Push(snapshot);UndoButton.IsEnabled=true;}
    private void Apply_OnClick(object sender,RoutedEventArgs e)=>Commit();private void Done_OnClick(object sender,RoutedEventArgs e){if(_transforms.Count==0){MessageBox.Show(this,"Добавьте хотя бы одно преобразование.","IFS",MessageBoxButton.OK,MessageBoxImage.Warning);return;}Commit();DialogResult=true;}
    private void Commit(){if(_transforms.Count==0)return;ResultTransforms=_transforms.Select(t=>t.Clone()).ToList();TransformsApplied?.Invoke(ResultTransforms);}
    private static List<IfsAffineTransform> CreateRandom(){Random r=Random.Shared;int count=r.Next(3,7);var result=new List<IfsAffineTransform>(count+1);double[] raw=new double[count];double total=0,baseAngle=Range(r,-Math.PI,Math.PI);for(int i=0;i<count;i++){double anchor=baseAngle+i*Math.PI*2/count+Range(r,-.35,.35),radius=Range(r,.25,.9),rotation=anchor+Range(r,-.9,.9),sx=Range(r,.28,.68),sy=Range(r,.24,.68),shear=Range(r,-.18,.18);result.Add(Create(rotation,sx,sy,shear,Math.Cos(anchor)*radius,Math.Sin(anchor)*radius,0));total+=raw[i]=Math.Max(.03,sx*sy)*Range(r,.8,1.35);}if(r.NextDouble()<.35)result.Add(Create(Range(r,-.2,.2),Range(r,.03,.16),Range(r,.32,.62),Range(r,-.05,.05),Range(r,-.08,.08),Range(r,-.85,-.35),Range(r,.02,.08)));double fixedP=result.Skip(count).Sum(t=>t.Probability),remaining=Math.Max(.1,1-fixedP);for(int i=0;i<count;i++)result[i].Probability=raw[i]/Math.Max(total,1e-12)*remaining;Normalize(result);return result;}
    private static IfsAffineTransform Create(double rotation,double sx,double sy,double shear,double tx,double ty,double p){double cos=Math.Cos(rotation),sin=Math.Sin(rotation);return new(){A=cos*sx+sin*shear,B=-sin*sy,C=sin*sx,D=cos*sy+cos*shear,E=tx,F=ty,Probability=p};}
    private static void Normalize(List<IfsAffineTransform> items){double total=items.Sum(t=>Math.Max(0,t.Probability));if(total<=0){foreach(IfsAffineTransform t in items)t.Probability=1d/items.Count;}else foreach(IfsAffineTransform t in items)t.Probability=Math.Max(0,t.Probability)/total;}
    private static double Range(Random r,double min,double max)=>min+r.NextDouble()*(max-min);private static string F(double v)=>v.ToString("0.########",CultureInfo.InvariantCulture);private static bool Read(string text,out double v)=>double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out v)||double.TryParse(text,NumberStyles.Float,CultureInfo.CurrentCulture,out v);
    private sealed record Snapshot(List<IfsAffineTransform> Transforms,int SelectedIndex){public bool Same(Snapshot other)=>SelectedIndex==other.SelectedIndex&&Transforms.Count==other.Transforms.Count&&Transforms.Zip(other.Transforms).All(x=>Equal(x.First,x.Second));private static bool Equal(IfsAffineTransform a,IfsAffineTransform b)=>a.A==b.A&&a.B==b.B&&a.C==b.C&&a.D==b.D&&a.E==b.E&&a.F==b.F&&a.Probability==b.Probability;}
}
