using System;
using System.Windows;
using System.Windows.Media;

namespace ShichenClock {
    public sealed partial class ClockFace {
        private int YangCount(TimeHexagram hex) {
            int count=0;for(int i=0;i<6;i++)if(hex.Yang(i))count++;return count;
        }
        private void TimeTrack(double x,double y,double width,string rest,string passed,string active) {
            double step=width/12,cell=step-4;
            for(int i=0;i<12;i++) {
                Box(x+i*step,y,cell,2,1,i<m.Index?passed:rest);
                if(i==m.Index&&m.Progress>0)Box(x+i*step,y,cell*m.Progress,2,1,active);
            }
        }
        private void Orbit() {
            Surface("#122C38","#0C1826");Label("YIN / YANG TELEMETRY");var hex=Yijing.At(m);
            // The six scan channels are the current six lines, with the bottom channel = 初爻.
            for(int row=0;row<6;row++) {
                double y=96+row*24;bool yang=hex.Yang(5-row);
                Txt((6-row).ToString("00"),34,y-1,8,"#587B8C","Consolas");
                Yao(59,y,124,7,yang,yang?"#A0E5E8":"#4F8496");
                Line(59,y+11,183,y+11,"#244557",.6);
            }
            Ellipse(263,156,54,54,null,"#32576B",.7);Arc(263,156,62,-90,Math.Max(.01,m.Progress*359.9),t.Accent,1.7);
            Character(263,110,49);Txt("时",263,170,11,t.Muted,center:true);
            Txt(hex.Name+" / "+hex.Number.ToString("00"),264,218,14,t.Accent,"DengXian",true);
            TimeTrack(34,258,296,"#233C4F","#4C7183",t.Accent);Footer();
        }
        private void Celadon() {
            Surface("#E7F1E8","#BCD7CE");Glow(109,76,168,145,"#FAFFF1",.65);Label("GLAZED CHANGES");var hex=Yijing.At(m);
            Ellipse(209,165,98,98,"#AECDC1");Orb(205,160,98,"#FAFFF2","#B4D1C4");
            Ellipse(205,160,87,87,null,"#A8C6B7",.6);
            // A second, pale impression gives the lines the depth of a shallow ceramic engraving.
            Hexagram(hex,163,106,84,4,11,"#FAFFF2","#FAFFF2");
            Hexagram(hex,163,105,84,4,11,"#547F73","#82A897");
            Txt(hex.Name,205,206,22,t.Fg,"KaiTi",true);Character(62,104,43,"KaiTi");
            Txt("时",62,160,12,t.Muted,"SimSun",true);Txt("卦",62,198,11,t.Muted,"SimSun",true);
            Txt("序",62,219,11,t.Muted,"SimSun",true);Footer();
        }
        private void Paper() {
            Surface("#F2F0E8","#E5E3D8");Label("INK / SIX LINES");var hex=Yijing.At(m);
            Line(199,92,199,229,"#CAC9BD",.7);Character(105,78,105,"KaiTi");Txt("时",149,190,16,t.Muted,"KaiTi");
            Hexagram(hex,224,102,65,3,16,t.Fg,"#777B70");
            string caption=hex.Image;for(int i=0;i<caption.Length;i++)Txt(caption[i].ToString(),316,104+i*22,11,t.Muted,"SimSun",true);
            Box(44,222,29,29,2,t.Accent);Txt(hex.Name,58.5,hex.Name.Length==1?225:229,hex.Name.Length==1?19:12,"#F3EADD","KaiTi",true);
            Txt("第 "+hex.Number+" 卦 · "+m.Range,85,233,10,t.Muted,"SimSun");
            Line(34,266,326,266,"#C7C8BC",.6);Footer();
        }
        private void Lacquer() {
            Surface("#642D40","#321C30");Label("GOLD / LACQUER");var hex=Yijing.At(m);
            Glow(246,143,111,119,"#B6746C",.13);
            // Curved inlay: outermost = 上爻, innermost = 初爻. Each broken line keeps one central gap.
            for(int row=0;row<6;row++) {
                double r=91-row*12;bool yang=hex.Yang(5-row);string color=yang?"#E1BF91":"#A87973";
                if(yang)Arc(224,164,r,-146,230,color,2.7);
                else {Arc(224,164,r,-146,102,color,2.7);Arc(224,164,r,-18,102,color,2.7);}
            }
            Character(84,108,62,"KaiTi");Txt(hex.Image,42,220,11,t.Accent);
            Txt(hex.Name,270,237,20,t.Fg,"KaiTi",true);Line(36,268,324,268,"#76475A",.6);Footer();
        }
        private void Leaf(double x,double y,double width,double height,string color) {
            var geometry=new StreamGeometry();using(var c=geometry.Open()) {
                c.BeginFigure(new Point(x,y+height/2),true,true);
                c.BezierTo(new Point(x+width*.28,y-height*.25),new Point(x+width*.72,y-height*.25),new Point(x+width,y+height/2),true,false);
                c.BezierTo(new Point(x+width*.72,y+height*1.25),new Point(x+width*.28,y+height*1.25),new Point(x,y+height/2),true,false);
            }d.DrawGeometry(Brush(color),null,geometry);
        }
        private void Grove() {
            Surface("#344F41","#182E2B");Label("GROWTH / RETURN");var hex=Yijing.At(m);
            Glow(296,124,133,151,"#BDCF9F",.14);
            for(int row=0;row<6;row++) {
                double y=92+row*25;bool yang=hex.Yang(5-row);string color=yang?"#D3DDB1":"#83A38A";
                if(yang)Leaf(199,y,101,8,color);
                else {Leaf(199,y,43,8,color);Leaf(257,y,43,8,color);}
            }
            Character(98,94,78,"KaiTi");Txt(hex.Image,47,194,13,t.Accent);
            Txt(YangCount(hex)+" 阳 / "+(6-YangCount(hex))+" 阴",47,224,10,t.Muted);
            TimeTrack(35,258,294,"#355445","#6C8E73",t.Accent);Footer();
        }
        private void Lunar() {
            Surface("#202D43","#101823");Label("SILVER / SHADOW");var hex=Yijing.At(m);
            Glow(248,153,97,97,"#9EAFD3",.17);Orb(248,153,78,"#EEF1E8","#8394B4");
            Ellipse(248,153,69,69,null,"#B8C6D3",.5);
            // Shadow-cut hexagram, rather than an invented lunar-phase calculation.
            Hexagram(hex,215,112,66,4,10,"#36465E","#687C99");
            Character(83,100,64);Txt(hex.Name+" · "+hex.Number.ToString("00"),84,187,18,t.Fg,"KaiTi",true);
            Txt("阴 阳 消 长",84,220,9,t.Muted,center:true);
            Arc(248,153,88,-90,Math.Max(.01,m.Progress*359.9),t.Accent,1);
            Line(35,264,325,264,"#35435B",.7);Footer();
        }
        private void Analog() {
            Surface("#E3E4D8","#C7CDBC");Label("MECHANICAL CHANGES");var hex=Yijing.At(m);
            Box(32,88,296,151,15,"#A9B4A2");
            foreach(double x in new[]{35.0,183.0})d.DrawRoundedRectangle(Gradient("#3F4E42","#27392F"),null,new Rect(x,91,142,145),11,11);
            Character(106,102,65,"DengXian Light","#EBEFDA");Txt(hex.Name+"卦",106,193,17,"#BCC9B2","KaiTi",true);
            Hexagram(hex,205,107,97,6,16,"#E5EDD5","#839A83");
            Line(35,163,325,163,"#18291F",2);Line(36,165,324,165,"#63775E",.6);
            foreach(double x in new[]{35.0,174,183,322})Box(x,156,4,15,1,"#8A9D80");
            Box(37,253,24,3,1,t.Accent);Txt(hex.Image+" · 第 "+hex.Number+" 卦",72,247,10,t.Muted);Footer();
        }
        private void Blue() {
            Surface("#244BD5","#1937B4");Label("SIX LINES / BLUE");var hex=Yijing.At(m);
            d.PushOpacity(.15);Ellipse(303,155,96,96,"#DDE4FF");d.Pop();
            // The hexagram becomes the graphic composition, rather than an extra badge.
            Hexagram(hex,40,95,158,12,12,"#EFF0D8","#99B8F0");
            Character(268,95,64,"DengXian Light");Txt(hex.Name,267,176,25,t.Accent,"KaiTi",true);
            Txt(hex.Number.ToString("00"),267,222,11,t.Fg,"Consolas",true);
            TimeTrack(36,260,292,"#4766D9","#98ACE7",t.Accent);Footer();
        }
        private void Matrix() {
            Surface("#302840","#1D1D30");Label("DOTS / CHANGES");var hex=Yijing.At(m);
            for(int row=0;row<6;row++)for(int col=0;col<22;col++)for(int dy=0;dy<2;dy++) {
                bool lit=hex.Yang(5-row)||col<9||col>12;
                Ellipse(43+col*7,101+row*24+dy*4,1.5,1.5,lit?(hex.Yang(5-row)?"#E6D4FA":"#AD8AC6"):"#493C5A");
            }
            Txt(m.Name,272,107,44,t.Fg,"DengXian Light",true);Txt(hex.Name,272,170,23,t.Accent,"KaiTi",true);
            Txt("第 "+hex.Number+" 卦",272,211,10,t.Muted,center:true);
            for(int i=0;i<12;i++)Box(37+i*24,258,18,3,.5,i==m.Index?t.Accent:i<m.Index?"#8D76A7":"#50405F");Footer();
        }
        private void Pure() {
            Surface("#F7F9F9","#E7EDEE");Label("RELIEF / SILENCE");var hex=Yijing.At(m);
            Ellipse(183,168,99,99,"#D6DFE0");Orb(180,163,99,"#FFFFFF","#EEF3F2");
            for(int i=0;i<12;i++){var p=Polar(180,163,87,i*30-90);Ellipse(p.X,p.Y,1,1,i==m.Index?t.Accent:"#C1CED0");}
            Hexagram(hex,138,108,84,3,11,"#D0DCDB","#DCE5E3");
            Hexagram(hex,138,106,84,3,11,"#526B68","#9DB1AE");
            Txt(m.Name+"时 · "+hex.Name,180,209,17,t.Fg,"DengXian Light",true);
            Txt(m.Now.ToString("HH:mm"),180,277,28,t.Fg,"Segoe UI Light",true);Txt("北京 / "+m.Range,180,313,9,t.Muted,"Segoe UI",true);
        }
        private void Chrono() {
            Surface("#343234","#1C2023");Label("CALIBRE / SIX LINES");var hex=Yijing.At(m);
            Ellipse(180,166,101,101,"#222629","#665B56",.8);Ellipse(180,166,93,93,null,"#494643",.7);
            for(int i=0;i<60;i++){var a=Polar(180,166,i%5==0?82:87,i*6-90);var b=Polar(180,166,91,i*6-90);Line(a.X,a.Y,b.X,b.Y,i%5==0?"#C8AA95":"#62574F",i%5==0?1.3:.7);}
            Hexagram(hex,148,111,64,3,11,"#E7C4A6","#8A786B");
            Txt(m.Name+"时 · "+hex.Name,180,205,15,t.Fg,"DengXian",true);
            Txt(hex.Number.ToString("00"),180,238,8,t.Muted,"Segoe UI",true);
            Arc(180,166,100,-90,Math.Max(.01,m.Progress*359.9),t.Accent,1.7);
            Txt(m.Now.ToString("HH:mm"),180,279,27,t.Fg,"Segoe UI Light",true);Txt("北京 / "+m.Range,180,314,9,t.Muted,"Segoe UI",true);
        }
        private void SoftYao(double x,double y,double width,double height,bool yang) {
            double piece=yang?width:width*.41;int count=yang?1:2;
            for(int i=0;i<count;i++) {double left=x+i*(width-piece);
                Box(left+1,y+2,piece,height,height/2,"#22A581A2");
                d.DrawRoundedRectangle(Gradient("#FFF5DE","#DBBCD3"),null,new Rect(left,y,piece,height),height/2,height/2);
                Line(left+height/2,y+2,left+piece-height/2,y+2,"#DDFFF9EB",.6);
            }
        }
        private void Aura() {
            Surface("#EFDED9","#CECAE7");Label("FLOATING / CHANGES");var hex=Yijing.At(m);
            Glow(50,94,140,160,"#FFF6D8",.6);Glow(305,240,130,141,"#A6A2D4",.3);
            for(int row=0;row<6;row++)SoftYao(88,89+row*25,184,12,hex.Yang(5-row));
            Txt(m.Name+"时 · "+hex.Name,180,247,17,t.Fg,"KaiTi",true);Footer();
        }
        private void Meridian() {
            Surface("#EBEEE5","#D3DBD0");Label("TWELVE / MERIDIAN");var hex=Yijing.At(m);
            Ellipse(182,169,99,99,"#B8C4B6");Orb(180,166,99,"#FFFDF1","#E3E8DA");
            for(int i=0;i<60;i++){var a=Polar(180,166,i%5==0?88:92,i*6-90);var b=Polar(180,166,96,i*6-90);Line(a.X,a.Y,b.X,b.Y,i%5==0?"#718775":"#BBC7B6",i%5==0?1:.6);}
            // All twelve hour markers are the matching message hexagrams; the hand still follows 24-hour time.
            for(int i=0;i<12;i++) {
                Point p=Polar(180,166,76,i*30-90);var marker=Yijing.Cycle[i];
                Hexagram(marker,p.X-8,p.Y-7,16,1.2,1.4,i==m.Index?t.Accent:"#607964",i==m.Index?t.Accent:"#A0B19B");
                Point label=Polar(180,166,115,i*30-90);Txt(ClockMoment.Names[i],label.X,label.Y-7,10,i==m.Index?t.Accent:t.Muted,"DengXian",true);
            }
            Txt(m.Name+"时",180,126,15,t.Fg,"DengXian",true);Txt(hex.Name+"卦",180,196,12,t.Muted,"KaiTi",true);
            Hand(180,166,m.HourHandAngle,45,8,4,t.Fg);Hand(180,166,m.MinuteHandAngle,64,11,1.8,t.Accent);
            Ellipse(180,166,4.5,4.5,t.Fg);Ellipse(180,166,1.4,1.4,"#F6EDD2");
            Txt(m.Now.ToString("HH:mm"),34,285,26,t.Fg,"Segoe UI Light");Txt("北京 · UTC+8",34,316,8,t.Muted);
            Txt(hex.Image,271,291,12,t.Fg,"KaiTi",true);Txt("短针一天 · 长针一小时",270,314,8,t.Muted,center:true);
        }
        private void Cycle() {
            Surface("#29334A","#172033");Label("TWELVE / CYCLE");var hex=Yijing.At(m);
            string[] colors={"#AB9FE0","#A29CDE","#9BA9E8","#92B6DF","#8DC6D1","#92C9BD","#B0CCA8","#D0C4A2","#D2B09C","#CEA2B4","#BC9BD1","#B29DDB"};
            for(int i=0;i<12;i++) {
                double start=i*30-103;Sector(start,26,"#35415A");
                if(i<m.Index)Sector(start,26,"#85"+colors[i].Substring(1));if(i==m.Index)Sector(start,26*m.Progress,colors[i]);
                Point p=Polar(180,166,84.5,i*30-90);d.PushTransform(new RotateTransform(i*30,p.X,p.Y));
                Hexagram(Yijing.Cycle[i],p.X-8,p.Y-7,16,1,1.4,i==m.Index?"#FCF3DD":"#C1CBE0",i==m.Index?"#F3DECD":"#9DAAC7");d.Pop();
                Point label=Polar(180,166,113,i*30-90);Txt(ClockMoment.Names[i],label.X,label.Y-7,10,i==m.Index?t.Fg:t.Muted,"DengXian",true);
            }
            var cursor=Polar(180,166,101,m.Index*30-103+26*m.Progress);Ellipse(cursor.X,cursor.Y,2.5,2.5,t.Fg);
            Character(180,111,44);Txt(hex.Name+"卦",180,169,18,t.Accent,"KaiTi",true);Txt(m.Detail,180,201,9,t.Muted,center:true);
            Txt(((int)(m.Progress*100))+"%",180,222,8,t.Accent,"Segoe UI",true);
            Txt(m.Now.ToString("HH:mm"),34,282,27,t.Fg,"Segoe UI Light");Txt("北京 · UTC+8",34,315,9,t.Muted);
            Txt("下一卦 · "+Yijing.Cycle[(m.Index+1)%12].Name,270,289,11,t.Fg,center:true);Txt(m.MinutesLeft+" 分钟后",270,311,10,t.Muted,center:true);
        }
    }
}
