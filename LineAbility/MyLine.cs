using System.Diagnostics;
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
        public bool ShiftKey { get; set; }
        public int rotateAngle { get; set; } = 0;
        public DoubleCollection StrokeDashArray { get ; set; } = new DoubleCollection();

        public void UpdateStart(Point p)
        {
            Start = p;
        }
        public void UpdateEnd(Point p)
        {
            End = p;
        }

        public UIElement Draw(Color color, int thickness,DoubleCollection stroke = null,bool isShiftKeyPressed = false, int angle = 0)
        {
            ColorDrew = color;
            ThicknessDrew = thickness;
            ShiftKey = isShiftKeyPressed;
            if (stroke != null) { StrokeDashArray = stroke; }
            else { stroke = StrokeDashArray; }
            var UI = new Line()
            {
                X1 = Start.X,
                Y1 = Start.Y,
                X2 = End.X,
                Y2 = End.Y,
                Stroke = new SolidColorBrush(color),
                StrokeDashArray = stroke,
                StrokeThickness = thickness
            };

            Point center = new Point((UI.X1 + UI.X2) / 2, (UI.Y1 + UI.Y2) / 2);
            rotateAngle = angle;
            RotateTransform rotateTransform = new RotateTransform(rotateAngle, center.X, center.Y);
            UI.RenderTransform = rotateTransform;
            return UI;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
