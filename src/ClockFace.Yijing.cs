using System;
using System.Windows;
using System.Windows.Media;

namespace ShichenClock {
    public sealed partial class ClockFace {
        private void Yao(double x,double y,double width,double height,bool yang,string color) {
            if(yang)Box(x,y,width,height,height/3,color);
            else {double piece=(width-width*.18)/2;Box(x,y,piece,height,height/3,color);Box(x+width-piece,y,piece,height,height/3,color);}
        }
        private void Hexagram(TimeHexagram hex,double x,double y,double width,double height,double gap,string yang,string yin) {
            for(int row=0;row<6;row++)Yao(x,y+row*(height+gap),width,height,hex.Yang(5-row),hex.Yang(5-row)?yang:yin);
        }
        private void Messages() {
            Surface("#EDF3ED","#D7E3DB");Label("TWELVE MESSAGES");var hex=Yijing.At(m);
            Txt("十二消息卦",280,49,9,t.Muted,center:true);
            // Six horizontal strokes are the actual current hexagram, read from the bottom.
            Hexagram(hex,43,102,116,9,13,t.Fg,"#98AFA3");
            int row=5-Yijing.ChangingLine(m.Index);double y=102+row*22+4.5;
            Ellipse(29,y,2,2,t.Accent);
            Line(181,96,181,234,"#B9CBBF",.8);
            Txt(hex.Name,241,87,hex.Name.Length==1?52:35,t.Fg,"KaiTi",true);
            Txt(hex.Image+" · "+hex.Number.ToString("00"),242,151,10,t.Muted,center:true);
            Txt(m.Name+"时",242,181,23,t.Fg,"DengXian",true);
            Txt(m.Range,242,214,10,t.Muted,"Segoe UI",true);
            // Each cell is one double-hour; its coloured fill represents elapsed time in that cell.
            for(int i=0;i<12;i++) {
                double x=35+i*24.4;Box(x,254,20,3,1.5,"#BDCEC2");
                if(i<m.Index)Box(x,254,20,3,1.5,"#7E9A89");
                if(i==m.Index) {Box(x,254,Math.Max(.1,20*m.Progress),3,1.5,t.Accent);Txt(ClockMoment.Names[i],x+10,263,9,t.Accent,center:true);}
            }
            Txt(m.Now.ToString("HH:mm"),34,284,24,t.Fg,"Segoe UI Light");
            Txt("北京 · "+m.Detail,272,293,10,t.Muted,center:true);
        }
        private void BaguaDial() {
            Surface("#2C2942","#1D2034");Label("EARLIER HEAVEN");var hex=Yijing.At(m);
            const double cx=180,cy=170;
            Ellipse(cx,cy,104,104,null,"#514E69",.6);Ellipse(cx,cy,73,73,null,"#5E576F",.6);
            int upper=hex.Bits>>3,lower=hex.Bits&7;
            for(int i=0;i<8;i++) {
                int bits=Yijing.EarlierHeaven[i];double angle=i*45-90;
                Point pos=Polar(cx,cy,92,angle);
                // Trigram top line faces outward; the bottom line faces the dial centre.
                d.PushTransform(new RotateTransform(i*45,pos.X,pos.Y));
                for(int row=0;row<3;row++)Yao(pos.X-13,pos.Y-7+row*5,26,2.4,(bits&(1<<(2-row)))!=0,bits==upper||bits==lower?t.Accent:"#89849B");
                d.Pop();Point name=Polar(cx,cy,118,angle);Txt(Yijing.TrigramNames[bits],name.X,name.Y-7,10,bits==upper||bits==lower?t.Accent:t.Muted,"Microsoft YaHei UI",true);
            }
            // Inner ring starts at 子时 23:00; no trigram is falsely assigned a three-hour interval.
            for(int i=0;i<12;i++)Arc(cx,cy,66,-90+i*30+3,24,"#45445F",1.5);
            Arc(cx,cy,66,-90,Math.Max(.01,Yijing.DayProgress(m)*359.99),t.Accent,2);
            Point pointer=Polar(cx,cy,66,-90+Yijing.DayProgress(m)*360);Ellipse(pointer.X,pointer.Y,3,3,"#F6DEB6");
            Hexagram(hex,159,132,42,3,4,t.Fg,"#968CA8");
            Txt(m.Name+"时 · "+hex.Name,180,185,17,t.Fg,"DengXian",true);
            Txt(m.Detail+" / "+(m.Progress*100).ToString("0")+"%",180,213,9,t.Muted,center:true);
            Txt(m.Now.ToString("HH:mm"),34,289,25,t.Fg,"Segoe UI Light");
            Txt("先天八卦 · 十二消息",257,296,10,t.Muted,center:true);
        }
    }
}
