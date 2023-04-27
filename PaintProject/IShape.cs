using System;
using System.Windows;
using System.Windows.Media;

namespace PaintProject
{
    public interface IShape:ICloneable
    {
        public string name { get; }
        public Point Start { get; set; }
        public Point End { get; set; }
        public Color ColorDrew { get; set; }
        public int ThicknessDrew { get; set; }
        public DoubleCollection StrokeDashArray { get; set; }
        void UpdateStart(Point p);
        void UpdateEnd(Point p);
        UIElement Draw(System.Windows.Media.Color color, int thickness,bool isShiftKeyPressed=false, DoubleCollection dash = null);
    }
}
