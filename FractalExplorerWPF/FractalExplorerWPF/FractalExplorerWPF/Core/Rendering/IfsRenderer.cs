using FractalExplorerWPF.Models;

namespace FractalExplorerWPF.Core.Rendering;

public sealed class IfsRenderer
{
    private readonly IfsState _state; private readonly int _width,_height; private readonly float[] _x,_y; private readonly byte[] _pixels; private readonly Random _random=new(12345);
    private double _currentX,_currentY; private bool _burnedIn,_boundsReady; private float _minX,_maxX,_minY,_maxY;
    public int GeneratedPoints{get;private set;} public int PlottedPoints{get;private set;}
    public IfsRenderer(IfsState state,int width,int height){_state=state.Clone();_width=width;_height=height;int count=Math.Max(1000,state.Iterations);_x=new float[count];_y=new float[count];_pixels=new byte[checked(width*height*4)];FillBackground();}
    public void Generate(int count,CancellationToken token)
    {
        if(_state.Transforms.Count==0){GeneratedPoints=_x.Length;return;}if(!_burnedIn){int burn=Math.Min(100,_x.Length/10);for(int i=0;i<burn;i++)Step();_burnedIn=true;}
        int end=Math.Min(_x.Length,GeneratedPoints+Math.Max(1,count));for(;GeneratedPoints<end;GeneratedPoints++){if((GeneratedPoints&4095)==0)token.ThrowIfCancellationRequested();Step();(_x[GeneratedPoints],_y[GeneratedPoints])=((float)_currentX,(float)_currentY);}
        if(GeneratedPoints==_x.Length&&!_boundsReady)PrepareBounds();
    }
    public void Plot(int count,CancellationToken token)
    {
        if(!_boundsReady)throw new InvalidOperationException("Сначала необходимо построить орбиту IFS.");double viewportWidth=Math.Clamp(Math.Abs(_state.Scale),.05,40),viewportHeight=viewportWidth*_height/(double)_width,left=_state.CenterX-viewportWidth/2,top=_state.CenterY+viewportHeight/2;float dx=Math.Max(1e-6f,_maxX-_minX),dy=Math.Max(1e-6f,_maxY-_minY);int end=Math.Min(_x.Length,PlottedPoints+Math.Max(1,count));
        for(;PlottedPoints<end;PlottedPoints++){if((PlottedPoints&4095)==0)token.ThrowIfCancellationRequested();double nx=(_x[PlottedPoints]-_minX)/dx,ny=(_y[PlottedPoints]-_minY)/dy,worldX=(nx-.5)*2,worldY=(ny-.5)*2;int px=(int)((worldX-left)/viewportWidth*_width),py=(int)((top-worldY)/viewportHeight*_height);if((uint)px>=(uint)_width||(uint)py>=(uint)_height)continue;int p=(py*_width+px)*4;_pixels[p]=_state.FractalColor.B;_pixels[p+1]=_state.FractalColor.G;_pixels[p+2]=_state.FractalColor.R;_pixels[p+3]=255;}
    }
    public byte[] CreateFrame()=>(byte[])_pixels.Clone();
    private void Step(){IfsAffineTransform t=Pick(_random.NextDouble());double nx=t.A*_currentX+t.B*_currentY+t.E,ny=t.C*_currentX+t.D*_currentY+t.F;_currentX=(float)nx;_currentY=(float)ny;}
    private IfsAffineTransform Pick(double value){double total=_state.Transforms.Sum(t=>Math.Max(0,t.Probability));if(total<=0)return _state.Transforms[^1];double sum=0;foreach(IfsAffineTransform t in _state.Transforms){sum+=Math.Max(0,t.Probability)/total;if(value<=sum)return t;}return _state.Transforms[^1];}
    private void PrepareBounds(){_minX=_x.Min();_maxX=_x.Max();_minY=_y.Min();_maxY=_y.Max();_boundsReady=true;}
    private void FillBackground(){for(int i=0;i<_width*_height;i++){int p=i*4;_pixels[p]=_state.BackgroundColor.B;_pixels[p+1]=_state.BackgroundColor.G;_pixels[p+2]=_state.BackgroundColor.R;_pixels[p+3]=_state.BackgroundColor.A;}}
}
