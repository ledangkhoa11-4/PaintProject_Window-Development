using System;
using System.Windows;
using System.Windows.Media;

namespace PaintProject
{
    public interface IShape:ICloneable
    {
        public string name { get; }
        void UpdateStart(Point p);
        void UpdateEnd(Point p);
        UIElement Draw(System.Windows.Media.Color color, int thickness);
    }
}
