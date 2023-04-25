using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using PaintProject;
using System.Windows.Shapes;
namespace RectangleAbility
{
    public class MyRectangle:IShape
    {
        public Point Start { get; set; }
        public Point End { get; set; }

        public string name { get => "Rectangle"; }

        public void UpdateStart(Point p)
        {
            Start = p;
        }
        public void UpdateEnd(Point p)
        {
            End = p;
        }
        public Color ColorDrew { get; set; }
        public int ThicknessDrew { get; set; }

        public UIElement Draw(Color color, int thickness)
        {
            ColorDrew = color;
            ThicknessDrew = thickness;
            double width = Math.Abs(End.X - Start.X);
            double height = Math.Abs(End.Y - Start.Y);

            var shape = new Rectangle()
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
