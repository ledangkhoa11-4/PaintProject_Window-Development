using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using PaintProject;

namespace EllipseAbility
{
    public class MyEllipse:IShape
    {
        public Point Start { get; set; }
        public Point End { get; set; }

        public string name { get => "Ellipse"; }
        public Color ColorDrew { get; set; }
        public int ThicknessDrew { get; set; }
        public void UpdateStart(Point p)
        {
            Start = p;
        }
        public void UpdateEnd(Point p)
        {
            End = p;
        }

        public UIElement Draw(Color color, int thickness)
        {
            ColorDrew = color;
            ThicknessDrew = thickness;
            double height = Math.Abs(End.Y - Start.Y);
            double width = Math.Abs(End.X - Start.X);

            var shape = new Ellipse()
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness
            };

            Canvas.SetLeft(shape, Start.X);
            Canvas.SetTop(shape, Start.Y);
            ScaleTransform scaleTransform = new ScaleTransform();

            if (Start.X <= End.X && Start.Y <= End.Y)
            {
                scaleTransform.ScaleY = 1;
                scaleTransform.ScaleX = 1;
            }
            if (Start.X <= End.X && Start.Y > End.Y)
            {
                scaleTransform.ScaleY = -1;
                scaleTransform.ScaleX = 1;
            }
            if (Start.X > End.X && Start.Y < End.Y)
            {
                scaleTransform.ScaleY = 1;
                scaleTransform.ScaleX = -1;
            }
            if (Start.X > End.X && Start.Y > End.Y)
            {
                scaleTransform.ScaleY = -1;
                scaleTransform.ScaleX = -1;
            }
            shape.RenderTransform = scaleTransform;
            return shape;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
