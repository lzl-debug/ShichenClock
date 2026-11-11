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
            new ThemeInfo("临界", "ORBIT", "冷光科技 · 悬浮轨道", "#101A23", "#EFFAFF", "#95DCEB", "#90A6B4"),
            new ThemeInfo("天青", "CELADON", "当代东方 · 青瓷釉色", "#DDECE6", "#294E4B", "#5E8D81", "#6E8C83"),
            new ThemeInfo("留白", "PAPER", "宣纸书卷 · 墨字与朱印", "#EEECE5", "#333732", "#9E4438", "#86867E"),
            new ThemeInfo("绛色", "LACQUER", "东方漆器 · 酒红与淡金", "#421C2A", "#F4DEBD", "#C9A67B", "#C7A7A3"),
            new ThemeInfo("苔息", "GROVE", "自然静谧 · 林间光影", "#213E35", "#E5EDCF", "#BDCF9F", "#A3B6A2"),
            new ThemeInfo("月背", "LUNAR", "深空月相 · 银蓝光晕", "#141923", "#E3E7F1", "#B7C5E2", "#8E9CB2"),
            new ThemeInfo("慢频", "ANALOG", "复古工业 · 翻页计时器", "#DFDFD4", "#333C36", "#DE6A38", "#7B8177"),
            new ThemeInfo("蓝调", "BLUE NOTE", "平面构成 · 克莱因蓝", "#193CC5", "#F1F1DE", "#E3F39A", "#B4C4FF"),
            new ThemeInfo("点阵", "DOT MATRIX", "数字像素 · 紫色微光", "#201E2C", "#DDD3F6", "#C4AEF2", "#9A8FB1"),
            new ThemeInfo("无相", "PURE", "纯白极简 · 悬浮圆盘", "#F0F1F2", "#363C41", "#535F68", "#8D969C"),
            new ThemeInfo("刻度", "CHRONO", "精密腕表 · 石墨与玫瑰金", "#202122", "#EFE5DA", "#D5A18A", "#A69E96"),
            new ThemeInfo("浮光", "AURA", "柔和流体 · 杏桃与香芋", "#EFE4E4", "#675165", "#A77C9A", "#9B7F95"),
            new ThemeInfo("游针", "MERIDIAN", "地支指针 · 一日一周", "#E8E5DD", "#34443F", "#B56F4F", "#8C9388"),
            new ThemeInfo("流转", "CYCLE", "环形时辰 · 逐格流动", "#1A2233", "#E5EAF7", "#A9B9EF", "#98A6BF")
        };
    }
    public sealed class ClockFace : FrameworkElement {
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
        private static readonly Dictionary<string,List<Point>> dotGlyphs=new Dictionary<string,List<Point>>();
        public ClockFace() {Width=360;Height=340;SnapsToDevicePixels=false;}
        public static SolidColorBrush Brush(string hex) {SolidColorBrush b;if(brushes.TryGetValue(hex,out b))return b;b=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));b.Freeze();brushes[hex]=b;return b;}
        private LinearGradientBrush Gradient(string a,string b,double angle=90) {var g=new LinearGradientBrush(Brush(a).Color,Brush(b).Color,angle);g.Freeze();return g;}
        private void Box(double x,double y,double w,double h,double r,string fill,string stroke=null,double sw=1) {d.DrawRoundedRectangle(fill==null?null:Brush(fill),stroke==null?null:new Pen(Brush(stroke),sw),new Rect(x,y,w,h),r,r);}
        private void Line(double x1,double y1,double x2,double y2,string c,double w=1) {d.DrawLine(new Pen(Brush(c),w),new Point(x1,y1),new Point(x2,y2));}
        private void Ellipse(double x,double y,double rx,double ry,string fill,string stroke=null,double sw=1) {d.DrawEllipse(fill==null?null:Brush(fill),stroke==null?null:new Pen(Brush(stroke),sw),new Point(x,y),rx,ry);}
        private void Txt(string s,double x,double y,double size,string color=null,string font="Microsoft YaHei UI",bool center=false,bool bold=false) {
            var f=new FormattedText(s,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(new FontFamily(font),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),size,Brush(color??t.Fg),VisualTreeHelper.GetDpi(this).PixelsPerDip);d.DrawText(f,new Point(center?x-f.Width/2:x,y));
        }
        private void Path(string data,string fill,string stroke=null,double w=1) {d.DrawGeometry(fill==null?null:Brush(fill),stroke==null?null:new Pen(Brush(stroke),w),Geometry.Parse(data));}
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
                    switch(Theme){case 0:Orbit();break;case 1:Celadon();break;case 2:Paper();break;case 3:Lacquer();break;case 4:Grove();break;case 5:Lunar();break;case 6:Analog();break;case 7:Blue();break;case 8:Matrix();break;case 9:Pure();break;case 10:Chrono();break;case 11:Aura();break;case 12:Meridian();break;case 13:Cycle();break;}
                    d.Pop();Box(10.5,10.5,339,319,27.5,null,"#16FFFFFF",.7);
                }cached.Freeze();cachedTheme=Theme;cachedSecond=second;
            }
            d=context;double blend=Math.Max(0,Math.Min(1,DockBlend));d.PushOpacity(1-blend);d.DrawDrawing(cached);d.Pop();
            if(blend>0&&!String.IsNullOrEmpty(HiddenEdge)) {d.PushOpacity(blend);
                if(HiddenEdge=="Left"||HiddenEdge=="Right") {double x=HiddenEdge=="Left"?346:10;Box(x,133,4,74,1.5,t.Accent);}
                else {double y=HiddenEdge=="Top"?326:10;Box(143,y,74,4,1.5,t.Accent);}d.Pop();
            }
        }
        private void Orbit() {
            Surface("#1B2E3B","#0B111B");Glow(295,125,145,145,"#5895BD",.24);Label();
            Ellipse(263,151,107,107,null,"#2C4254",.7);Ellipse(263,151,86,86,null,"#304657",.6);Arc(263,151,107,-116,106,"#8FC4DC",1.5);Arc(263,151,86,35,87,"#4B6A83",1);
            double a=(-116+106*m.Progress)*Math.PI/180;Ellipse(263+107*Math.Cos(a),151+107*Math.Sin(a),3,3,"#DBF8FF");
            Character(94,94,99);Txt("时",161,168,17,t.Muted,"DengXian Light");Txt(m.Poem,36,222,11,t.Muted);Line(34,259,326,259,"#2C3C4B",.7);Footer();Txt("+",274,141,17,"#637F93","Segoe UI Light",true);
        }
        private void Celadon() {
            Surface("#EAF2E9","#C4DBD4");Glow(120,88,174,152,"#FFFFFF",.8);Label();Orb(204,164,97,"#FBFFF6","#94BAB2");Ellipse(204,164,97,97,null,"#B8D2C5",.5);Arc(204,164,86,30,125,"#F6FFF2",1);Arc(204,164,97,-80,80*m.Progress,"#527E75",1.4);
            Character(202,100,88);Txt(m.Detail,204,203,10,t.Muted,center:true);Txt("十",44,106,11,t.Muted,"SimSun");Txt("二",44,127,11,t.Muted,"SimSun");Txt("时",44,148,11,t.Muted,"SimSun");Txt("序",44,169,11,t.Muted,"SimSun");Footer();
        }
        private void Paper() {
            Surface("#F4F2EC","#EAE7DF");Label("A STUDY OF TIME");var random=new Random(19);for(int i=0;i<320;i++){double x=15+random.Next(330),y=15+random.Next(310);Line(x,y,x+random.Next(2,6),y,"#068A8377",.5);}
            Line(268,87,268,244,"#C9C6BB",.7);Character(135,80,119,"KaiTi");Txt("时",211,180,17,t.Muted,"KaiTi");string word=m.Poem.Split('·')[1].Trim();for(int i=0;i<word.Length;i++)Txt(word[i].ToString(),287,95+i*23,12,t.Fg,"SimSun");
            Box(43,221,18,18,1,t.Accent);Txt("辰",52,222,12,"#F6E9DB","KaiTi",true);Txt(m.Range,74,226,10,t.Muted,"Segoe UI");Line(34,265,326,265,"#D8D4CA",.7);Footer();
        }
        private void Lacquer() {
            Surface("#602B3C","#2E1724");Glow(290,130,150,200,"#AC5367",.16);Label();d.PushOpacity(.35);for(int i=0;i<7;i++)Arc(268,160,75+i*12,100,190,"#AA7481",.5);d.Pop();Arc(268,160,99,-90,165*m.Progress,"#D7B18B",1.2);
            Character(119,88,110);Txt("时",194,185,15,t.Accent,"DengXian Light");Txt(m.Poem,40,225,11,t.Muted);Line(36,263,324,263,"#704351",.6);Footer();
        }
        private void Grove() {
            Surface("#344E3F","#182F29");Glow(277,84,210,160,"#BDCB8E",.19);Path("M 225,10 C 200,78 240,150 197,220 C 178,251 182,305 180,340 L 360,340 360,10 Z","#15372D");Path("M 317,10 C 257,71 326,156 268,224 C 234,265 260,301 242,340 L 360,340 360,10 Z","#102D26");
            d.PushOpacity(.65);Path("M 279,229 C 276,175 290,120 306,77 M 285,171 C 254,157 253,137 256,118 C 278,130 286,150 285,171 M 293,138 C 314,126 324,107 320,90 C 299,101 295,119 293,138",null,"#77977B",.8);d.Pop();Label();Character(114,97,95);Txt(m.Poem,42,219,11,t.Muted);Txt("随 时 而 息",42,241,9,t.Muted);Footer();
        }
        private void Lunar() {
            Surface("#1E2738","#10141E");Label();Glow(254,141,100,100,"#8398C3",.17);Orb(252,137,61,"#DEE6F0","#647491");var r=new Random(12);d.PushClip(new EllipseGeometry(new Point(252,137),60,60));for(int i=0;i<38;i++){double x=193+r.Next(120),y=78+r.Next(120),size=1+r.Next(7);Ellipse(x,y,size,size*.8,"#0F2D405D");Arc(x,y,size,20,115,"#17F2F7FF",.6);}d.Pop();
            var shade=new LinearGradientBrush();shade.StartPoint=new Point(0,0);shade.EndPoint=new Point(1,0);shade.GradientStops.Add(new GradientStop(Color.FromArgb(0,16,20,30),0));shade.GradientStops.Add(new GradientStop(Color.FromArgb(220,16,20,30),1));d.DrawEllipse(shade,null,new Point(252,137),61,61);
            Character(91,102,86);Txt(m.Detail,56,207,11,t.Muted);Line(36,252,324,252,"#333C4D",.6);Footer();
        }
        private void Analog() {
            Surface("#E8E8DF","#D3D4C9");Label();Box(32,89,296,147,15,"#B7BCAF");Box(35,92,143,141,12,"#29312C");Box(182,92,143,141,12,"#29312C");d.DrawRoundedRectangle(Gradient("#3A453D","#242D29"),null,new Rect(36,93,141,139),11,11);d.DrawRoundedRectangle(Gradient("#3A453D","#242D29"),null,new Rect(183,93,141,139),11,11);
            Character(106,103,84,"DengXian Light","#ECEEDC");Txt("时",253,103,84,"#ECEEDC","DengXian Light",true);Line(35,163,325,163,"#171E19",2);Line(36,165,324,165,"#627064",.5);foreach(double x in new[]{35.0,174,183,322})Box(x,156,4,15,1,"#8A9686");Box(37,252,24,4,2,t.Accent);Txt(m.Detail,72,247,10,t.Muted);Footer();
        }
        private void Blue() {
            Surface("#2549D3","#1231B1");Label();d.PushOpacity(.12);Ellipse(313,151,124,124,null,"#D3E1FF",1);Ellipse(313,151,104,104,null,"#D3E1FF",1);d.Pop();Txt(m.Name,36,75,133,t.Fg,"Microsoft YaHei UI",false,true);Box(219,92,3,142,1,"#6E8AF1");Txt("时",269,138,42,t.Fg,"DengXian Light",true);Ellipse(271,224,18,18,t.Accent);double a=m.Progress*360*Math.PI/180;Line(271,224,271+12*Math.Sin(a),224-12*Math.Cos(a),t.Bg,1.5);Txt(m.Poem,40,236,10,t.Muted);Footer();
        }
        private void Matrix() {
            Surface("#292536","#1A1825");Label();for(int row=0;row<24;row++)for(int col=0;col<41;col++)Ellipse(39+col*7,84+row*7,.65,.65,"#433B52");List<Point> points;
            if(!dotGlyphs.TryGetValue(m.Name,out points)) {points=new List<Point>();var text=new FormattedText(m.Name,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface("Microsoft YaHei"),132,Brush(t.Fg),1);var geo=text.BuildGeometry(new Point(0,0));for(int y=0;y<160;y+=4)for(int x=0;x<140;x+=4)if(geo.FillContains(new Point(x+2,y+2)))points.Add(new Point(x,y));dotGlyphs[m.Name]=points;}
            foreach(var p in points)Box(51+p.X,83+p.Y,2.5,2.5,.5,t.Fg);Txt("时",265,136,25,t.Muted,"DengXian Light",true);Txt(m.Half+" · "+m.Quarter,267,182,10,t.Muted,center:true);Line(35,264,325,264,"#4C405D",.6);Footer();
        }
        private void Pure() {
            Surface("#F6F7F8","#E9ECEE");Label();Ellipse(183,166,98,98,"#DDE1E4");Orb(180,160,98,"#FFFFFF","#F0F2F3");for(int i=0;i<12;i++){double a=(i*30-90)*Math.PI/180;Ellipse(180+84*Math.Cos(a),160+84*Math.Sin(a),1,1,i==m.Index?t.Accent:"#CFD5D9");}Character(180,100,83);Txt(m.Detail,180,196,10,t.Muted,center:true);Txt(m.Now.ToString("HH:mm"),180,272,29,t.Fg,"Segoe UI Light",true);Txt("北京  /  "+m.Range,180,312,9,t.Muted,"Segoe UI",true);
        }
        private void Chrono() {
            Surface("#303032","#1B1B1D");Label();Ellipse(180,165,100,100,"#202022","#4B4441",.7);Ellipse(180,165,94,94,null,"#5B4C47",.6);for(int i=0;i<60;i++){double a=(i*6-90)*Math.PI/180;double r=i%5==0?81:86;Line(180+r*Math.Cos(a),165+r*Math.Sin(a),180+90*Math.Cos(a),165+90*Math.Sin(a),i%5==0?"#C3A08D":"#5E514B",i%5==0?1.3:.7);}Arc(180,165,99,-90,m.Progress*359.9,t.Accent,1.8);Character(180,111,65);Txt(m.Detail,180,190,10,t.Muted,center:true);Txt(ClockMoment.Names[(m.Index+11)%12],114,158,10,t.Muted);Txt(m.Next,238,158,10,t.Muted);Txt(m.Now.ToString("HH:mm"),180,278,28,t.Fg,"Segoe UI Light",true);Txt("北京  /  "+m.Range,180,313,9,t.Muted,"Segoe UI",true);
        }
        private void Aura() {
            Surface("#F4E6DA","#D7CBE7");Glow(69,82,180,160,"#FFF9E3",.8);Glow(299,237,160,160,"#B4A6D8",.55);Label();Orb(249,132,78,"#FFF1DA","#D7B1C4");Orb(289,202,48,"#E7DCF6","#AF9CBF");d.PushOpacity(.65);Arc(249,132,78,-140,140,"#FFF9EB",1);d.Pop();Character(112,93,104);Txt(m.Poem,43,227,11,t.Fg);Footer();
        }
        private void Hand(double x,double y,double angle,double length,double tail,double width,string color) {
            double a=angle*Math.PI/180;var pen=new Pen(Brush(color),width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            d.DrawLine(pen,new Point(x-tail*Math.Cos(a),y-tail*Math.Sin(a)),new Point(x+length*Math.Cos(a),y+length*Math.Sin(a)));
        }
        private void Meridian() {
            Surface("#F1F0E9","#DFE0D6");Label();
            Ellipse(182,169,102,102,"#CFD2C7");Orb(180,166,102,"#FFFEF6","#ECEDE2");
            for(int i=0;i<60;i++){double a=(i*6-90)*Math.PI/180;double r=i%5==0?90:94;Line(180+r*Math.Cos(a),166+r*Math.Sin(a),180+97*Math.Cos(a),166+97*Math.Sin(a),i%5==0?"#859184":"#C5CBBD",i%5==0?1:.7);}
            for(int i=0;i<12;i++){double a=(i*30-90)*Math.PI/180;Txt(ClockMoment.Names[i],180+78*Math.Cos(a),159+78*Math.Sin(a),12,i==m.Index?t.Accent:t.Fg,"DengXian",true,i==m.Index);}
            Txt(m.Name+"时",180,124,17,t.Fg,"DengXian",true);Txt("24 H",180,197,8,t.Muted,"Segoe UI",true);
            Hand(180,166,m.HourHandAngle,48,8,4.5,t.Fg);Hand(180,166,m.MinuteHandAngle,69,12,2,t.Accent);
            Ellipse(180,166,5,5,t.Fg);Ellipse(180,166,1.7,1.7,"#F8F2DE");
            Txt(m.Now.ToString("HH:mm"),34,280,27,t.Fg,"Segoe UI Light");Txt("北京 · UTC+8",34,313,9,t.Muted);
            Txt("短针 · 时辰",261,286,10,t.Fg,center:true);Txt("长针 · 分钟",261,308,10,t.Muted,center:true);
        }
        private Point Polar(double cx,double cy,double r,double angle) {double a=angle*Math.PI/180;return new Point(cx+r*Math.Cos(a),cy+r*Math.Sin(a));}
        private void Sector(double start,double sweep,string color) {
            if(sweep<=.001)return;const double outer=97,inner=72;var geometry=new StreamGeometry();
            using(var c=geometry.Open()) {c.BeginFigure(Polar(180,166,outer,start),true,true);c.ArcTo(Polar(180,166,outer,start+sweep),new Size(outer,outer),0,sweep>180,SweepDirection.Clockwise,true,false);c.LineTo(Polar(180,166,inner,start+sweep),true,false);c.ArcTo(Polar(180,166,inner,start),new Size(inner,inner),0,sweep>180,SweepDirection.Counterclockwise,true,false);}
            d.DrawGeometry(Brush(color),null,geometry);
        }
        private void Cycle() {
            Surface("#263148","#161D2D");Glow(180,166,138,138,"#5D70B7",.13);Label();
            string[] colors={"#AB9FE0","#A29CDE","#9BA9E8","#92B6DF","#8DC6D1","#92C9BD","#B0CCA8","#D0C4A2","#D2B09C","#CEA2B4","#BC9BD1","#B29DDB"};
            for(int i=0;i<12;i++) {
                double start=i*30-103;
                Sector(start,26,"#323E56");
                if(i<m.Index)Sector(start,26,"#85"+colors[i].Substring(1));
                if(i==m.Index)Sector(start,26*m.Progress,colors[i]);
                var p=Polar(180,166,113,i*30-90);Txt(ClockMoment.Names[i],p.X,p.Y-7,10,i==m.Index?t.Fg:"#8F9EB9","DengXian",true,i==m.Index);
            }
            var cursor=Polar(180,166,84.5,m.Index*30-103+26*m.Progress);Ellipse(cursor.X,cursor.Y,4,4,t.Fg);Ellipse(cursor.X,cursor.Y,1.8,1.8,"#7486B1");
            Character(180,111,56);Txt(m.Detail,180,181,10,t.Muted,center:true);Txt(((int)(m.Progress*100)).ToString()+"%",180,204,10,t.Accent,"Segoe UI",true);
            Txt(m.Now.ToString("HH:mm"),34,282,27,t.Fg,"Segoe UI Light");Txt("北京 · UTC+8",34,315,9,t.Muted);
            Txt("下一格 · "+m.Next+"时",270,289,10,t.Fg,center:true);Txt(m.MinutesLeft+" 分钟后",270,311,10,t.Muted,center:true);
        }
    }
}
