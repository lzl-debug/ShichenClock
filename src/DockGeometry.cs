using System;
using System.Drawing;

namespace ShichenClock {
    // Window coordinates include a 10-unit transparent margin around the clock face.
    // Align the visible face (and the hidden handle), not the native window rectangle.
    internal static class DockGeometry {
        internal static int MarginX(Rectangle window) { return (int)Math.Round(window.Width * 10.0 / 360); }
        internal static int MarginY(Rectangle window) { return (int)Math.Round(window.Height * 10.0 / 340); }
        internal static int Clamp(int value,int min,int max) { return Math.Max(min,Math.Min(value,Math.Max(min,max))); }
        internal static Point Expanded(string edge,Rectangle window,Rectangle work) {
            int mx=MarginX(window),my=MarginY(window);
            int x=Clamp(window.X,work.Left-mx,work.Right-window.Width+mx);
            int y=Clamp(window.Y,work.Top-my,work.Bottom-window.Height+my);
            if(edge=="Left")x=work.Left-mx;
            if(edge=="Right")x=work.Right-window.Width+mx;
            if(edge=="Top")y=work.Top-my;
            if(edge=="Bottom")y=work.Bottom-window.Height+my;
            return new Point(x,y);
        }
        internal static Point Hidden(string edge,Rectangle window,Rectangle work) {
            int mx=MarginX(window),my=MarginY(window);
            int px=Math.Max(3,(int)Math.Round(window.Width*4.0/360));
            int py=Math.Max(3,(int)Math.Round(window.Height*4.0/340));
            int x=window.X,y=window.Y;
            if(edge=="Left")x=work.Left+px-window.Width+mx;
            if(edge=="Right")x=work.Right-px-mx;
            if(edge=="Top")y=work.Top+py-window.Height+my;
            if(edge=="Bottom")y=work.Bottom-py-my;
            return new Point(x,y);
        }
    }
}
