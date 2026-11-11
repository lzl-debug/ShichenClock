using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ShichenClock {
    public sealed class ThemeInfo {
        public string Name, English, Description, Bg, Fg, Accent, Muted;
        public ThemeInfo(string n,string e,string d,string b,string f,string a,string m) {Name=n;English=e;Description=d;Bg=b;Fg=f;Accent=a;Muted=m;}
        public static readonly ThemeInfo[] All = {
            new ThemeInfo("临界", "ORBIT", "冷光科技 · 六爻扫描", "#101A23", "#EFFAFF", "#95DCEB", "#90A6B4"),
            new ThemeInfo("天青", "CELADON", "青瓷釉色 · 阴阳刻纹", "#DDECE6", "#294E4B", "#5E8D81", "#6E8C83"),
            new ThemeInfo("留白", "PAPER", "宣纸书卷 · 墨爻朱印", "#EEECE5", "#333732", "#9E4438", "#86867E"),
            new ThemeInfo("绛色", "LACQUER", "东方漆器 · 六爻金弧", "#421C2A", "#F4DEBD", "#C9A67B", "#C7A7A3"),
            new ThemeInfo("苔息", "GROVE", "林间叶序 · 六爻生息", "#213E35", "#E5EDCF", "#BDCF9F", "#A3B6A2"),
            new ThemeInfo("月背", "LUNAR", "银蓝月盘 · 阴阳镂影", "#141923", "#E3E7F1", "#B7C5E2", "#8E9CB2"),
            new ThemeInfo("慢频", "ANALOG", "机械翻页 · 六爻分屏", "#DFDFD4", "#333C36", "#DE6A38", "#7B8177"),
            new ThemeInfo("蓝调", "BLUE NOTE", "克莱因蓝 · 卦象构成", "#193CC5", "#F1F1DE", "#E3F39A", "#B4C4FF"),
            new ThemeInfo("点阵", "DOT MATRIX", "紫色像素 · 点阵六爻", "#201E2C", "#DDD3F6", "#C4AEF2", "#9A8FB1"),
            new ThemeInfo("无相", "PURE", "纯白浮雕 · 静默卦象", "#F0F1F2", "#363C41", "#535F68", "#8D969C"),
            new ThemeInfo("刻度", "CHRONO", "精密腕表 · 玫瑰金爻", "#202122", "#EFE5DA", "#D5A18A", "#A69E96"),
            new ThemeInfo("浮光", "AURA", "杏桃香芋 · 悬浮爻线", "#EFE4E4", "#675165", "#A77C9A", "#9B7F95"),
            new ThemeInfo("游针", "MERIDIAN", "地支指针 · 十二卦刻度", "#E8E5DD", "#34443F", "#B56F4F", "#8C9388"),
            new ThemeInfo("流转", "CYCLE", "环形时辰 · 十二卦流转", "#1A2233", "#E5EAF7", "#A9B9EF", "#98A6BF"),
            new ThemeInfo("消息", "TWELVE MESSAGES", "六爻时序 · 一时一卦", "#E3EDE4", "#34544B", "#A46845", "#718C7D"),
            new ThemeInfo("玄枢", "EARLIER HEAVEN", "先天八卦 · 环行十二辰", "#28273E", "#EDE7DE", "#D5B78E", "#AAA0BC")
        };
    }
    public sealed partial class ClockFace : FrameworkElement {
        public int Theme;
        public ClockMoment Sample;
        public string HiddenEdge;
        public double DockBlend;
        private DrawingContext d;
        private ThemeInfo t;
        private ClockMoment m;
        private DrawingGroup cached;
        private int cachedTheme=-1;
        private long cachedSecond=-1;
        private static readonly Dictionary<string,SolidColorBrush> brushes=new Dictionary<string,SolidColorBrush>();
        public ClockFace() {Width=360;Height=340;SnapsToDevicePixels=false;}
        public static SolidColorBrush Brush(string hex) {SolidColorBrush b;if(brushes.TryGetValue(hex,out b))return b;b=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));b.Freeze();brushes[hex]=b;return b;}
        private LinearGradientBrush Gradient(string a,string b,double angle=90) {var g=new LinearGradientBrush(Brush(a).Color,Brush(b).Color,angle);g.Freeze();return g;}
        private void Box(double x,double y,double w,double h,double r,string fill,string stroke=null,double sw=1) {d.DrawRoundedRectangle(fill==null?null:Brush(fill),stroke==null?null:new Pen(Brush(stroke),sw),new Rect(x,y,w,h),r,r);}
        private void Line(double x1,double y1,double x2,double y2,string c,double w=1) {d.DrawLine(new Pen(Brush(c),w),new Point(x1,y1),new Point(x2,y2));}
        private void Ellipse(double x,double y,double rx,double ry,string fill,string stroke=null,double sw=1) {d.DrawEllipse(fill==null?null:Brush(fill),stroke==null?null:new Pen(Brush(stroke),sw),new Point(x,y),rx,ry);}
        private void Txt(string s,double x,double y,double size,string color=null,string font="Microsoft YaHei UI",bool center=false,bool bold=false) {
            var f=new FormattedText(s,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(new FontFamily(font),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),size,Brush(color??t.Fg),VisualTreeHelper.GetDpi(this).PixelsPerDip);d.DrawText(f,new Point(center?x-f.Width/2:x,y));
        }
        private void Arc(double x,double y,double r,double start,double sweep,string color,double width=1) {
            if(Math.Abs(sweep)<.001)return;double a=start*Math.PI/180,b=(start+sweep)*Math.PI/180;var g=new StreamGeometry();
            using(var c=g.Open()){c.BeginFigure(new Point(x+r*Math.Cos(a),y+r*Math.Sin(a)),false,false);c.ArcTo(new Point(x+r*Math.Cos(b),y+r*Math.Sin(b)),new Size(r,r),0,Math.Abs(sweep)>180,SweepDirection.Clockwise,true,false);}
            d.DrawGeometry(null,new Pen(Brush(color),width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},g);
        }
        private void Orb(double x,double y,double r,string light,string shade,double opacity=1) {var b=new RadialGradientBrush(Brush(light).Color,Brush(shade).Color){GradientOrigin=new Point(.28,.2),Center=new Point(.45,.4),RadiusX=.68,RadiusY=.68};d.PushOpacity(opacity);d.DrawEllipse(b,null,new Point(x,y),r,r);d.Pop();}
        private void Glow(double x,double y,double rx,double ry,string c,double opacity) {var b=new RadialGradientBrush(Brush(c).Color,Colors.Transparent);d.PushOpacity(opacity);d.DrawEllipse(b,null,new Point(x,y),rx,ry);d.Pop();}
        private void Surface(string top,string bottom=null) {d.DrawRoundedRectangle(Gradient(top,bottom??top),null,new Rect(10,10,340,320),28,28);}
        private void Label(string english=null) {Txt(t.Name,33,30,12);Txt(english??t.English,33,49,8,t.Muted,"Segoe UI",false,true);}
        private void Footer(double y=277) {Txt(m.Now.ToString("HH:mm"),33,y,28,t.Fg,"Segoe UI Light");Txt("北京 · UTC+8",34,y+36,9,t.Muted);Txt(m.Detail,282,y+9,11,t.Fg,center:true);Txt(m.Range,282,y+33,9,t.Muted,"Segoe UI",true);}
        private void Character(double x,double y,double size=83,string font="DengXian Light",string color=null) {Txt(m.Name,x,y,size,color??t.Fg,font,true);}
        protected override void OnRender(DrawingContext context) {
            base.OnRender(context);m=Sample??ClockMoment.Current();t=ThemeInfo.All[Theme];long second=m.Now.Ticks/TimeSpan.TicksPerSecond;
            if(cached==null||cachedTheme!=Theme||cachedSecond!=second) {
                cached=new DrawingGroup();using(var c=cached.Open()) {d=c;Box(11,15,338,317,28,"#0A000000");Box(10,12,340,318,28,"#07000000");d.PushClip(new RectangleGeometry(new Rect(10,10,340,320),28,28));
                    switch(Theme){case 0:Orbit();break;case 1:Celadon();break;case 2:Paper();break;case 3:Lacquer();break;case 4:Grove();break;case 5:Lunar();break;case 6:Analog();break;case 7:Blue();break;case 8:Matrix();break;case 9:Pure();break;case 10:Chrono();break;case 11:Aura();break;case 12:Meridian();break;case 13:Cycle();break;case 14:Messages();break;case 15:BaguaDial();break;}
                    d.Pop();Box(10.5,10.5,339,319,27.5,null,"#16FFFFFF",.7);
                }cached.Freeze();cachedTheme=Theme;cachedSecond=second;
            }
            d=context;double blend=Math.Max(0,Math.Min(1,DockBlend));d.PushOpacity(1-blend);d.DrawDrawing(cached);d.Pop();
            if(blend>0&&!String.IsNullOrEmpty(HiddenEdge)) {d.PushOpacity(blend);
                if(HiddenEdge=="Left"||HiddenEdge=="Right") {double x=HiddenEdge=="Left"?346:10;Box(x,133,4,74,1.5,t.Accent);}
                else {double y=HiddenEdge=="Top"?326:10;Box(143,y,74,4,1.5,t.Accent);}d.Pop();
            }
        }
        private void Hand(double x,double y,double angle,double length,double tail,double width,string color) {
            double a=angle*Math.PI/180;var pen=new Pen(Brush(color),width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            d.DrawLine(pen,new Point(x-tail*Math.Cos(a),y-tail*Math.Sin(a)),new Point(x+length*Math.Cos(a),y+length*Math.Sin(a)));
        }
        private Point Polar(double cx,double cy,double r,double angle) {double a=angle*Math.PI/180;return new Point(cx+r*Math.Cos(a),cy+r*Math.Sin(a));}
        private void Sector(double start,double sweep,string color) {
            if(sweep<=.001)return;const double outer=97,inner=72;var geometry=new StreamGeometry();
            using(var c=geometry.Open()) {c.BeginFigure(Polar(180,166,outer,start),true,true);c.ArcTo(Polar(180,166,outer,start+sweep),new Size(outer,outer),0,sweep>180,SweepDirection.Clockwise,true,false);c.LineTo(Polar(180,166,inner,start+sweep),true,false);c.ArcTo(Polar(180,166,inner,start),new Size(inner,inner),0,sweep>180,SweepDirection.Counterclockwise,true,false);}
            d.DrawGeometry(Brush(color),null,geometry);
        }
    }
}
