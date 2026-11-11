using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace ShichenClock {
    public sealed class ClockWindow : Window {
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; public int Width { get { return Right-Left; } } public int Height { get { return Bottom-Top; } } }
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out RECT r);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h,IntPtr a,int x,int y,int cx,int cy,uint flags);
        private ClockFace face = new ClockFace();
        private Grid root = new Grid();
        private StackPanel toolbar = new StackPanel();
        private DispatcherTimer tick = new DispatcherTimer();
        private DispatcherTimer dockTimer = new DispatcherTimer();
        private Forms.NotifyIcon tray;
        private string settingsPath;
        private string edge="";
        private string savedEdge="";
        private bool tucked,dragging,autoHide=true,menuOpen;
        private bool closing;
        private DateTime lastInside=DateTime.UtcNow;
        private RECT expanded;
        private double scale=1;
        private IntPtr hwnd;
        private Window chooser;
        private Button pinButton;
        private double savedLeft=Double.NaN,savedTop=Double.NaN;
        private bool verification;
        private bool moving;
        private readonly Stopwatch motionClock=new Stopwatch();
        private RECT motionFrom;
        private int motionX,motionY;
        private double motionDuration,blendFrom,blendTo;
        private Action motionDone;
        private System.Drawing.Rectangle dockArea;
        private int motionFrames;
        public ClockWindow(int theme,bool testMode=false) {
            verification=testMode;
            Title="拾辰 · 十二时辰"; Width=360; Height=340;
            WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize;
            AllowsTransparency=true; Background=Brushes.Transparent; Topmost=true; ShowInTaskbar=false;
            settingsPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.ini");
            if(!verification) ReadSettings(); if(theme>=0) face.Theme=theme;
            var view=new Viewbox {Stretch=Stretch.Uniform}; root.Width=360; root.Height=340;
            root.Children.Add(face); toolbar.Orientation=Orientation.Horizontal;
            toolbar.HorizontalAlignment=HorizontalAlignment.Right; toolbar.VerticalAlignment=VerticalAlignment.Top;
            toolbar.Margin=new Thickness(0,21,24,0);
            toolbar.Opacity=0;
            MouseEnter+=(s,e)=>FadeTools(true);
            MouseLeave+=(s,e)=>FadeTools(false);
            var themeBtn=ToolButton("◈","选择外观（共 16 套）"); themeBtn.Click+=(s,e)=>OpenChooser(); toolbar.Children.Add(themeBtn);
            pinButton=ToolButton("●","切换置顶"); pinButton.Click+=(s,e)=>{Topmost=!Topmost; RefreshButtons(); SaveSettings();}; toolbar.Children.Add(pinButton);
            var menuBtn=ToolButton("⋯","更多：吸附、大小、退出"); menuBtn.Click+=(s,e)=>ShowMenu(menuBtn); toolbar.Children.Add(menuBtn);
            root.Children.Add(toolbar); view.Child=root; Content=view;
            MouseLeftButtonDown+=StartDrag;
            MouseRightButtonUp+=(s,e)=>ShowMenu(null);
            PreviewKeyDown+=(s,e)=>{ if(e.Key==Key.Escape){ Reveal(true); edge=""; root.Clip=null; SaveSettings(); } if(e.Key==Key.T)OpenChooser(); if(e.Key==Key.Left)SetTheme((face.Theme+ThemeInfo.All.Length-1)%ThemeInfo.All.Length); if(e.Key==Key.Right)SetTheme((face.Theme+1)%ThemeInfo.All.Length); };
            SourceInitialized+=(s,e)=>{
                hwnd=new WindowInteropHelper(this).Handle;
                new WindowInteropHelper(this).EnsureHandle();
                if(!Double.IsNaN(savedLeft)) {Left=savedLeft;Top=savedTop;} else { var work=SystemParameters.WorkArea; Left=work.Right-Width-60;Top=work.Top+80; }
                KeepVisible();
                GetWindowRect(hwnd,out expanded);
            };
            Loaded+=(s,e)=>{RefreshButtons(); if(!verification) CreateTray(); Snap();};
            tick.Interval=TimeSpan.FromSeconds(1); tick.Tick+=(s,e)=>{if(!tucked) face.InvalidateVisual();}; tick.Start();
            dockTimer.Interval=TimeSpan.FromMilliseconds(100); dockTimer.Tick+=(s,e)=>DockTick(); if(!verification) dockTimer.Start();
            Closing+=(s,e)=>{Reveal(true);SaveSettings();};
            Closed+=(s,e)=>{closing=true;StopMotion();tick.Stop();dockTimer.Stop();if(chooser!=null)chooser.Close();if(tray!=null)tray.Dispose();};
        }
        private void FadeTools(bool show) {
            toolbar.IsHitTestVisible=show;
            toolbar.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(show?1:0,TimeSpan.FromMilliseconds(160)));
        }
        private Button ToolButton(string text,string tip) {
            var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(7));border.SetValue(Border.BackgroundProperty,Brushes.Transparent);border.Name="surface";
            var content=new FrameworkElementFactory(typeof(ContentPresenter));content.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);content.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(content);
            var template=new ControlTemplate(typeof(Button)){VisualTree=border};var hover=new Trigger{Property=UIElement.IsMouseOverProperty,Value=true};hover.Setters.Add(new Setter(Border.BackgroundProperty,ClockFace.Brush("#20808080"),"surface"));template.Triggers.Add(hover);
            return new Button {Content=text,ToolTip=tip,Width=27,Height=27,FontSize=14,Padding=new Thickness(0),Template=template,Cursor=Cursors.Hand,Focusable=false};
        }
        private void RefreshButtons() {
            foreach(Button b in toolbar.Children) b.Foreground=ClockFace.Brush(ThemeInfo.All[face.Theme].Muted);
            pinButton.Content=Topmost?"●":"○";
        }
        private void ReadSettings() {
            face.Theme=1;
            try {
                if(!File.Exists(settingsPath))return;
                var kv=new Dictionary<string,string>(); foreach(var line in File.ReadAllLines(settingsPath)) { var p=line.Split('='); if(p.Length==2)kv[p[0]]=p[1]; }
                int n; double v;
                if(kv.ContainsKey("theme")&&Int32.TryParse(kv["theme"],out n)&&n>=0&&n<ThemeInfo.All.Length)face.Theme=n;
                if(kv.ContainsKey("topmost"))Topmost=kv["topmost"]=="True";
                if(kv.ContainsKey("autoHide"))autoHide=kv["autoHide"]=="True";
                if(kv.ContainsKey("edge")&&new[]{"Left","Right","Top","Bottom"}.Contains(kv["edge"]))savedEdge=kv["edge"];
                if(kv.ContainsKey("scale")&&Double.TryParse(kv["scale"],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)&&v>=.75&&v<=1.4){scale=v;Width=360*scale;Height=340*scale;}
                if(kv.ContainsKey("left")&&Double.TryParse(kv["left"],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)&&!Double.IsInfinity(v)&&!Double.IsNaN(v))savedLeft=v;
                if(kv.ContainsKey("top")&&Double.TryParse(kv["top"],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v)&&!Double.IsInfinity(v)&&!Double.IsNaN(v))savedTop=v;
            } catch(IOException) {} catch(UnauthorizedAccessException) {}
        }
        private void SaveSettings() {
            if(tucked||moving||closing||verification)return;
            try {
                string data="theme="+face.Theme+"\r\ntopmost="+Topmost+"\r\nautoHide="+autoHide+"\r\nscale="+scale.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\r\nleft="+Left.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\r\ntop="+Top.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\r\n";
                data+="edge="+edge+"\r\n";
                File.WriteAllText(settingsPath+".tmp",data,Encoding.UTF8);
                if(File.Exists(settingsPath))File.Replace(settingsPath+".tmp",settingsPath,null); else File.Move(settingsPath+".tmp",settingsPath);
            }catch(IOException) {}catch(UnauthorizedAccessException) {}
        }
        private void CreateTray() {
            tray=new Forms.NotifyIcon(); tray.Text="拾辰 · 十二时辰（双击找回时钟）";
            tray.Icon=System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var cm=new Forms.ContextMenuStrip();
            cm.Items.Add("显示时钟",null,(s,e)=>Dispatcher.Invoke(new Action(BringBack)));
            cm.Items.Add("选择 16 套外观",null,(s,e)=>Dispatcher.Invoke(new Action(OpenChooser)));
            cm.Items.Add("退出",null,(s,e)=>Dispatcher.Invoke(new Action(Quit)));
            tray.ContextMenuStrip=cm; tray.DoubleClick+=(s,e)=>Dispatcher.Invoke(new Action(BringBack));tray.Visible=true;
        }
        private void StartDrag(object sender,MouseButtonEventArgs e) {
            DependencyObject p=e.OriginalSource as DependencyObject;
            while(p!=null) {if(p is Button)return; p=VisualTreeHelper.GetParent(p);}
            Reveal(true); dragging=true; edge="";root.Clip=null;
            try {DragMove();} catch(InvalidOperationException) {} finally {dragging=false; Snap(); SaveSettings();}
        }
        private void KeepVisible() {
            RECT r; GetWindowRect(hwnd,out r);
            var screen=Forms.Screen.FromHandle(hwnd);var w=screen.WorkingArea;
            int x=Math.Max(w.Left,Math.Min(r.Left,w.Right-r.Width));int y=Math.Max(w.Top,Math.Min(r.Top,w.Bottom-r.Height));
            SetWindowPos(hwnd,IntPtr.Zero,x,y,0,0,0x15);
        }
        private void Snap() {
            if(hwnd==IntPtr.Zero)return;
            RECT r;GetWindowRect(hwnd,out r);var w=Forms.Screen.FromHandle(hwnd).WorkingArea;
            int threshold=(int)(24*r.Width/360.0);
            int mx=DockGeometry.MarginX(ToRectangle(r)),my=DockGeometry.MarginY(ToRectangle(r));
            int[] dist={Math.Abs(r.Left+mx-w.Left),Math.Abs(w.Right-r.Right+mx),Math.Abs(r.Top+my-w.Top),Math.Abs(w.Bottom-r.Bottom+my)};
            int nearest=0;for(int i=1;i<4;i++)if(dist[i]<dist[nearest])nearest=i;
            int saved=Array.IndexOf(new[]{"Left","Right","Top","Bottom"},savedEdge);
            if(saved>=0&&dist[saved]<=threshold)nearest=saved;
            savedEdge="";
            edge="";
            if(dist[nearest]<=threshold) {
                edge=new[]{"Left","Right","Top","Bottom"}[nearest];
                var target=DockGeometry.Expanded(edge,ToRectangle(r),w);
                dockArea=w;
                MoveTo(target.X,target.Y,220,0,()=>{GetWindowRect(hwnd,out expanded);SaveSettings();});
                lastInside=DateTime.UtcNow;return;
            }
            root.Clip=null;GetWindowRect(hwnd,out expanded);lastInside=DateTime.UtcNow;
        }
        private static System.Drawing.Rectangle ToRectangle(RECT r) {return new System.Drawing.Rectangle(r.Left,r.Top,r.Width,r.Height);}
        private void DockTick() {
            if(hwnd==IntPtr.Zero||dragging||menuOpen||chooser!=null||String.IsNullOrEmpty(edge)||!autoHide)return;
            if(moving)return;
            POINT p;GetCursorPos(out p); EvaluatePointer(p);
        }
        private void EvaluatePointer(POINT p) {
            RECT r;GetWindowRect(hwnd,out r);
            bool inside;
            if(tucked) {
                double sx=r.Width/360.0,sy=r.Height/340.0;
                double x=edge=="Left"?344:edge=="Right"?8:141;
                double y=edge=="Top"?324:edge=="Bottom"?8:131;
                double w=(edge=="Left"||edge=="Right")?8:78;
                double h=(edge=="Left"||edge=="Right")?78:8;
                inside=dockArea.Contains(p.X,p.Y)&&p.X>=r.Left+x*sx&&p.X<=r.Left+(x+w)*sx&&p.Y>=r.Top+y*sy&&p.Y<=r.Top+(y+h)*sy;
            } else inside=p.X>=r.Left&&p.X<r.Right&&p.Y>=r.Top&&p.Y<r.Bottom;
            if(inside) {lastInside=DateTime.UtcNow;if(tucked)Reveal();}
            else if(!tucked&&(DateTime.UtcNow-lastInside).TotalMilliseconds>850)Tuck();
        }
        private void Tuck() {
            if(tucked)return;
            GetWindowRect(hwnd,out expanded); SaveSettings();
            dockArea=Forms.Screen.FromHandle(hwnd).WorkingArea;
            var target=DockGeometry.Hidden(edge,ToRectangle(expanded),dockArea);
            tucked=true;face.HiddenEdge=edge;FadeTools(false);
            MoveTo(target.X,target.Y,360,1,null);
        }
        private void Reveal(bool immediately=false) {
            if(!tucked) {if(immediately&&moving)FinishMotion();return;}
            tucked=false;
            MoveTo(expanded.Left,expanded.Top,300,0,()=>{face.HiddenEdge=null;lastInside=DateTime.UtcNow;SaveSettings();});
            if(immediately)FinishMotion();
            lastInside=DateTime.UtcNow;
        }
        private void StopMotion() {CompositionTarget.Rendering-=MotionFrame;motionClock.Stop();moving=false;motionDone=null;}
        private void MoveTo(int x,int y,double milliseconds,double blend,Action done) {
            // A replacement motion starts at the CURRENT frame, allowing a smooth reversal.
            StopMotion();GetWindowRect(hwnd,out motionFrom);motionX=x;motionY=y;blendFrom=face.DockBlend;blendTo=blend;motionDone=done;motionFrames=0;
            motionDuration=milliseconds;moving=true;motionClock.Restart();CompositionTarget.Rendering+=MotionFrame;
        }
        private void ClipToWorkArea(int x,int y) {
            if(String.IsNullOrEmpty(edge)){root.Clip=null;return;}
            double sx=360.0/motionFrom.Width,sy=340.0/motionFrom.Height;
            var visible=System.Drawing.Rectangle.Intersect(new System.Drawing.Rectangle(x,y,motionFrom.Width,motionFrom.Height),dockArea);
            root.Clip=new RectangleGeometry(new Rect((visible.Left-x)*sx,(visible.Top-y)*sy,Math.Max(0,visible.Width*sx),Math.Max(0,visible.Height*sy)));
        }
        private void MotionFrame(object sender,EventArgs e) {
            double p=Math.Min(1,motionClock.Elapsed.TotalMilliseconds/motionDuration);
            ApplyMotion(p);if(p>=1)CompleteMotion();
        }
        private void ApplyMotion(double p) {
            double eased=p*p*(3-2*p);
            int x=(int)Math.Round(motionFrom.Left+(motionX-motionFrom.Left)*eased),y=(int)Math.Round(motionFrom.Top+(motionY-motionFrom.Top)*eased);
            face.DockBlend=blendFrom+(blendTo-blendFrom)*eased;face.InvalidateVisual();ClipToWorkArea(x,y);
            SetWindowPos(hwnd,IntPtr.Zero,x,y,0,0,0x15);motionFrames++;
        }
        private void CompleteMotion() {
            var done=motionDone;StopMotion();lastInside=DateTime.UtcNow;if(done!=null)done();
        }
        private void FinishMotion() {if(!moving)return;ApplyMotion(1);CompleteMotion();}
        private void WaitForMotion(bool requireFrames=true) {
            if(!moving)return;
            var frame=new DispatcherFrame();var watch=Stopwatch.StartNew();var poll=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(15)};
            poll.Tick+=(s,e)=>{if(!moving||watch.ElapsedMilliseconds>2500)frame.Continue=false;};poll.Start();Dispatcher.PushFrame(frame);poll.Stop();
            if(moving)throw new Exception("Animation did not settle");
            if(requireFrames&&motionFrames<3)throw new Exception("Animation skipped intermediate frames");
        }
        private void BringBack() {
            Reveal(true);edge="";root.Clip=null;KeepVisible();Show();Activate();lastInside=DateTime.UtcNow;SaveSettings();
        }
        public void SetTheme(int index) {Reveal(true);face.Theme=index;face.InvalidateVisual();RefreshButtons();SaveSettings();}
        private MenuItem Item(string label,Action action) {var i=new MenuItem {Header=label};i.Click+=(s,e)=>action();return i;}
        private void ShowMenu(Button anchor) {
            Reveal(true);menuOpen=true;
            var cm=new ContextMenu();
            cm.Items.Add(Item("选择外观 · 16 套",OpenChooser));
            var always=Item("始终置顶",()=>{Topmost=!Topmost;RefreshButtons();SaveSettings();});always.IsCheckable=true;always.IsChecked=Topmost;cm.Items.Add(always);
            var hide=Item("贴边自动隐藏",()=>{autoHide=!autoHide;if(!autoHide)Reveal();SaveSettings();});hide.IsCheckable=true;hide.IsChecked=autoHide;cm.Items.Add(hide);
            var size=new MenuItem {Header="时钟大小"};
            foreach(double value in new[]{.8,1.0,1.2}) {double captured=value;var i=Item((value*100).ToString("0")+"%",()=>{Reveal(true);scale=captured;Width=360*scale;Height=340*scale;UpdateLayout();KeepVisible();Snap();SaveSettings();});i.IsChecked=Math.Abs(value-scale)<.01;size.Items.Add(i);}cm.Items.Add(size);
            var edges=new MenuItem {Header="吸附到屏幕边缘"};
            string[] labels={"左边","右边","上边","下边"};for(int n=0;n<4;n++){int side=n;edges.Items.Add(Item(labels[n],()=>DockAt(side)));}cm.Items.Add(edges);
            cm.Items.Add(Item("打开全部风格预览",()=>System.Diagnostics.Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"风格预览.html"))));
            cm.Items.Add(Item("八卦与时辰 · 看懂卦象",()=>System.Diagnostics.Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"易象预览.html"))));
            cm.Items.Add(new Separator());cm.Items.Add(Item("退出",Quit));
            cm.Closed+=(s,e)=>{menuOpen=false;lastInside=DateTime.UtcNow;};cm.PlacementTarget=anchor??(FrameworkElement)this;cm.IsOpen=true;
        }
        private void DockAt(int side) {
            Reveal(true);RECT r;GetWindowRect(hwnd,out r);var w=Forms.Screen.FromHandle(hwnd).WorkingArea;
            edge=new[]{"Left","Right","Top","Bottom"}[side];dockArea=w;
            var target=DockGeometry.Expanded(edge,ToRectangle(r),w);
            MoveTo(target.X,target.Y,260,0,()=>{GetWindowRect(hwnd,out expanded);SaveSettings();});
        }
        public void VerifyDesktop() {
            var report=new List<string>();
            for(int i=0;i<ThemeInfo.All.Length;i++){SetTheme(i);UpdateLayout();if(face.Theme!=i)throw new Exception("Theme selection failed");}
            report.Add("PASS: all 16 native themes switched.");
            foreach(double value in new[]{.8,1.0,1.2}) {
                scale=value;Width=360*scale;Height=340*scale;UpdateLayout();
                for(int side=0;side<4;side++) {
                    DockAt(side);WaitForMotion();string expected=new[]{"Left","Right","Top","Bottom"}[side];
                    if(edge!=expected)throw new Exception("Dock selection failed: "+expected);
                    RECT before;GetWindowRect(hwnd,out before);
                    int mx=DockGeometry.MarginX(ToRectangle(before)),my=DockGeometry.MarginY(ToRectangle(before));
                    int gap=side==0?before.Left+mx-dockArea.Left:side==1?dockArea.Right-before.Right+mx:side==2?before.Top+my-dockArea.Top:dockArea.Bottom-before.Bottom+my;
                    if(gap!=0)throw new Exception("Visible face is not flush: "+expected+" gap="+gap);
                    lastInside=DateTime.UtcNow.AddSeconds(-2);EvaluatePointer(new POINT {X=before.Left-1000,Y=before.Top-1000});
                    if(!tucked)throw new Exception("Auto-hide failed: "+expected);
                    WaitForMotion();if(Math.Abs(face.DockBlend-1)>.001)throw new Exception("Handle blend failed");
                    RECT hidden;GetWindowRect(hwnd,out hidden);
                    if(hidden.Left==before.Left&&hidden.Top==before.Top)throw new Exception("Native window did not hide");
                    // The outermost physical screen pixel must reveal the clock, without searching for an inset handle.
                    int x=side==0?dockArea.Left:side==1?dockArea.Right-1:hidden.Left+hidden.Width/2;
                    int y=side==2?dockArea.Top:side==3?dockArea.Bottom-1:hidden.Top+hidden.Height/2;
                    EvaluatePointer(new POINT {X=x,Y=y});
                    if(tucked)throw new Exception("Hover reveal failed: "+expected);
                    WaitForMotion();if(face.DockBlend!=0||root.Clip==null)throw new Exception("Reveal visual state failed");
                    RECT after;GetWindowRect(hwnd,out after);
                    if(before.Left!=after.Left||before.Top!=after.Top||before.Width!=after.Width||before.Height!=after.Height)throw new Exception("Restore position mismatch: "+expected);
                    report.Add("PASS: "+expected+" / scale "+value+" / animated snap, auto-hide, hover reveal ("+motionFrames+" frames), restored bounds "+after.Width+"x"+after.Height);
                }
            }
            DockAt(1);WaitForMotion();Tuck();
            // Reverse mid-flight and verify there is no position discontinuity at retarget time.
            ApplyMotion(.4);RECT midway;GetWindowRect(hwnd,out midway);double midBlend=face.DockBlend;
            Reveal();RECT reversed;GetWindowRect(hwnd,out reversed);
            if(midway.Left!=reversed.Left||midway.Top!=reversed.Top||face.DockBlend!=midBlend)throw new Exception("Animation reversal jumped");
            WaitForMotion();report.Add("PASS: mid-flight reversal preserves position and opacity; completes smoothly.");
            OpenChooser();if(chooser==null||!chooser.IsVisible)throw new Exception("Theme chooser failed");chooser.Close();report.Add("PASS: theme chooser opened and closed.");
            File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"desktop-test-results.txt"),report,Encoding.UTF8);
        }
        private void Quit() {Reveal(true);SaveSettings();Close();}
        private void OpenChooser() {
            Reveal(true);if(chooser!=null){chooser.Activate();return;}
            chooser=new Window {Title="拾辰 · 十六种时间的样子",Width=1000,Height=760,MinWidth=560,MinHeight=500,Background=ClockFace.Brush("#EEF1F4"),WindowStartupLocation=WindowStartupLocation.CenterScreen,Topmost=Topmost};
            var outer=new DockPanel {Margin=new Thickness(24)};
            var title=new TextBlock {Text="拾辰 / 十六种时间的样子",FontSize=27,Foreground=ClockFace.Brush("#263B4C"),Margin=new Thickness(4,0,0,8)};DockPanel.SetDock(title,Dock.Top);outer.Children.Add(title);
            var subtitle=new TextBlock {Text="点击一款，即刻换装。拖动时钟到屏幕边缘，移开鼠标后自动收起。",Foreground=ClockFace.Brush("#647584"),FontSize=13,Margin=new Thickness(4,0,0,20),TextWrapping=TextWrapping.Wrap};DockPanel.SetDock(subtitle,Dock.Top);outer.Children.Add(subtitle);
            var wrap=new WrapPanel();var scroller=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Content=wrap};outer.Children.Add(scroller);
            for(int n=0;n<ThemeInfo.All.Length;n++) {
                int id=n;var panel=new StackPanel {Width=276};
                var picture=new Viewbox {Width=270,Height=255,Child=new ClockFace {Theme=id,Sample=ClockMoment.Current()}};panel.Children.Add(picture);
                panel.Children.Add(new TextBlock {Text=ThemeInfo.All[n].Name,FontSize=17,Foreground=ClockFace.Brush("#263B4C"),Margin=new Thickness(13,2,0,6)});
                panel.Children.Add(new TextBlock {Text=ThemeInfo.All[n].Description,FontSize=11,Foreground=ClockFace.Brush("#647584"),Margin=new Thickness(13,0,0,14)});
                var b=new Button {Content=panel,Background=Brushes.Transparent,BorderThickness=new Thickness(1),BorderBrush=ClockFace.Brush(id==face.Theme?"#639AA2":"#D8E0E6"),Padding=new Thickness(3),Margin=new Thickness(5),Cursor=Cursors.Hand};
                b.Click+=(s,e)=>{SetTheme(id);chooser.Close();};wrap.Children.Add(b);
            }
            chooser.Content=outer;chooser.Closed+=(s,e)=>{chooser=null;lastInside=DateTime.UtcNow;};chooser.Show();
        }
    }

    public static class Program {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [STAThread] public static void Main(string[] args) {
            SetProcessDPIAware();
            if(args.Length>0&&args[0]=="--render") {
                DateTime sample=new DateTime(2026,11,11,17,38,24);
                if(args.Length>2)sample=sample.Date.Add(DateTime.ParseExact(args[2],"HH:mm",System.Globalization.CultureInfo.InvariantCulture).TimeOfDay);
                Render(args.Length>1?args[1]:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"previews"),sample);return;
            }
            if(args.Length>0&&args[0]=="--self-test") {SelfTest();return;}
            if(args.Length>0&&args[0]=="--smoke-test") {
                var testApp=new Application {ShutdownMode=ShutdownMode.OnMainWindowClose};var window=new ClockWindow(1,true);
                window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(new Action(()=>{
                    try {window.VerifyDesktop();window.Close();}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"desktop-test-results.txt"),"FAIL: "+ex);testApp.Shutdown(1);}
                }),DispatcherPriority.ApplicationIdle);
                testApp.Run(window);return;
            }
            bool owner;using(var mutex=new System.Threading.Mutex(true,"Local\\ShichenClock.Desktop.v1",out owner)) {
                if(!owner){MessageBox.Show("拾辰已经在运行。请双击系统托盘中的时钟图标找回窗口。","拾辰");return;}
                int theme=-1;if(args.Length==2&&args[0]=="--theme"){int n;if(Int32.TryParse(args[1],out n)&&n>=1&&n<=ThemeInfo.All.Length)theme=n-1;}
                var app=new Application {ShutdownMode=ShutdownMode.OnMainWindowClose};
                app.DispatcherUnhandledException+=(s,e)=>{try{File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"error.log"),DateTime.Now+" "+e.Exception+Environment.NewLine);}catch{}MessageBox.Show("运行遇到问题，详情已记录到 error.log。\n"+e.Exception.Message,"拾辰");app.Shutdown(1);e.Handled=true;};
                app.Run(new ClockWindow(theme));
            }
        }
        private static void Render(string folder,DateTime sample) {
            Directory.CreateDirectory(folder);
            for(int i=0;i<ThemeInfo.All.Length;i++) {
                var face=new ClockFace {Theme=i,Sample=ClockMoment.At(sample)};
                face.Measure(new Size(360,340));face.Arrange(new Rect(0,0,360,340));face.UpdateLayout();
                var bitmap=new RenderTargetBitmap(720,680,192,192,PixelFormats.Pbgra32);bitmap.Render(face);
                var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using(var stream=File.Create(Path.Combine(folder,(i+1).ToString("00")+".png")))encoder.Save(stream);
            }
        }
        private static void SelfTest() {
            var report=new List<string>();
            for(int hour=0;hour<24;hour++) {
                var c=ClockMoment.At(new DateTime(2026,1,1,hour,0,0));
                int expected=hour==23?0:(hour+1)/2;
                if(c.Index!=expected||Math.Abs(c.Progress-(hour%2==1?0:.5))>.00001)throw new Exception("Hour boundary "+hour);
                report.Add(hour.ToString("00")+":00 = "+c.Name+"时 / "+c.Detail+" / "+c.Range);
            }
            var late=ClockMoment.At(new DateTime(2026,1,1,22,59,59));if(late.Index!=11||late.Next!="子"||late.MinutesLeft!=1)throw new Exception("Midnight wrap");
            if(ClockMoment.At(new DateTime(2026,1,1,0,0,0)).Name!="子")throw new Exception("Early midnight");
            foreach(int minute in new[]{0,14,15,29,30,44,45,59}) {
                var c=ClockMoment.At(new DateTime(2026,1,1,17,minute,0));
                if(c.Quarter!=new[]{"初刻","一刻","二刻","三刻"}[minute/15])throw new Exception("Quarter boundary");
            }
            foreach(int hour in new[]{0,2,6,12,18,23}) {
                var c=ClockMoment.At(new DateTime(2026,1,1,hour,0,0));
                if(Math.Abs(c.HourHandAngle-(hour*15-90))>.00001||c.MinuteHandAngle!=-90)throw new Exception("24-hour hand alignment");
            }
            if(ClockMoment.At(new DateTime(2026,1,1,0,15,0)).MinuteHandAngle!=0)throw new Exception("Minute hand alignment");
            for(int i=0;i<12;i++) {
                int start=(i*2+23)%24;
                var begin=ClockMoment.At(new DateTime(2026,1,1,start,0,0));
                var end=ClockMoment.At(new DateTime(2026,1,1,(start+1)%24,59,59));
                var next=ClockMoment.At(new DateTime(2026,1,1,(start+2)%24,0,0));
                if(begin.Index!=i||begin.Progress!=0||end.Progress<.999||next.Index!=(i+1)%12||next.Progress!=0)throw new Exception("Ring segment boundary "+i);
            }
            report.Add("PASS: 24-hour hand angles, minute hand angles, 12 ring segment transitions including 23:00 reset.");
            string[] expectedNames={"复","临","泰","大壮","夬","乾","姤","遁","否","观","剥","坤"};
            int[] expectedNumbers={24,19,11,34,43,1,44,33,12,20,23,2};
            for(int i=0;i<12;i++) {
                int hour=(i*2+23)%24;var begin=ClockMoment.At(new DateTime(2026,1,1,hour,0,0));
                var hex=Yijing.At(begin);int count=0;
                for(int line=0;line<6;line++)if(hex.Yang(line))count++;
                if(hex.Name!=expectedNames[i]||hex.Number!=expectedNumbers[i]||count!=(i<6?i+1:11-i))throw new Exception("Message hexagram mapping "+i);
                int changed=Yijing.ChangingLine(i);
                if(changed!=(i+1)%6)throw new Exception("Changing line "+i);
                if(Math.Abs(Yijing.DayProgress(begin)-i/12.0)>.00001)throw new Exception("Yijing day boundary");
            }
            if(!Yijing.Cycle[0].Yang(0)||Yijing.Cycle[0].Yang(5)||!Yijing.Cycle[10].Yang(5)||Yijing.Cycle[10].Yang(0))throw new Exception("Hexagram bottom-to-top orientation");
            if(Yijing.EarlierHeaven[0]!=7||Yijing.EarlierHeaven[2]!=2||Yijing.EarlierHeaven[4]!=0||Yijing.EarlierHeaven[6]!=5)throw new Exception("Earlier Heaven cardinal placement");
            var trigrams=new HashSet<int>(Yijing.EarlierHeaven);if(trigrams.Count!=8)throw new Exception("Eight distinct trigrams");
            report.Add("PASS: 12 message hexagram names/numbers, yin-yang counts, bottom-to-top lines, single-line transitions, 23:00 cycle, Earlier Heaven placement.");
            report.Add("PASS: 24 hour boundaries, midnight wrap, 8 quarter boundaries. All time calculations use UTC+8.");
            File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),report,Encoding.UTF8);
        }
    }
}
