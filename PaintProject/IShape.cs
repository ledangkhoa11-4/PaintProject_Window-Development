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
        public bool ShiftKey { get; set; }
        public DoubleCollection StrokeDashArray { get; set; }
        public string Text { get; set; }
        public int rotateAngle { get; set; }
        void UpdateStart(Point p);
        void UpdateEnd(Point p);
        UIElement Draw(System.Windows.Media.Color color, int thickness,DoubleCollection stroke = null,bool isShiftKeyPressed=false, int angle = 0, string data = "");
    }
}
