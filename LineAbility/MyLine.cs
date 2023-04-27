using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using PaintProject;
namespace LineAbility
{
    public class MyLine : IShape
    {
        public string name { get => "Line"; }

        public Point Start { get; set; }
        public Point End { get; set; }


        public Color ColorDrew { get; set; }
        public int ThicknessDrew { get; set; }
        public DoubleCollection StrokeDashArray { get ; set; } = new DoubleCollection();

        public void UpdateStart(Point p)
        {
            Start = p;
        }
        public void UpdateEnd(Point p)
        {
            End = p;
        }

        public UIElement Draw(Color color, int thickness,DoubleCollection stroke,bool isShiftKeyPressed)
        {
            ColorDrew = color;
            ThicknessDrew = thickness;
            return new Line()
            {
                X1 = Start.X,
                Y1 = Start.Y,
                X2 = End.X,
                Y2 = End.Y,
                Stroke = new SolidColorBrush(color),
                StrokeDashArray = stroke,
                StrokeThickness = thickness
            };
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
