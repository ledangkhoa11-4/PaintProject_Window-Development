using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using PaintProject;
using System.Security.Policy;

namespace EllipseAbility
{
    public class MyEllipse:IShape
    {
        public Point Start { get; set; }
        public Point End { get; set; }

        public string name { get => "Ellipse"; }
        public Color ColorDrew { get; set; }
        public int ThicknessDrew { get; set; }
        public bool ShiftKey { get; set; }
        public DoubleCollection StrokeDashArray { get; set; } = new DoubleCollection();
        public int rotateAngle { get; set; } = 0;
        public void UpdateStart(Point p)
        {
            Start = p;
        }
        public void UpdateEnd(Point p)
        {
            End = p;
        }
        public string Text { get; set; } //do not use
        public UIElement Draw(Color color, int thickness,DoubleCollection stroke = null, bool isShiftKeyPressed=false, int angle = 0, string data = "")
        {
            ColorDrew = color;
            ThicknessDrew = thickness;
            ShiftKey = isShiftKeyPressed;
            if (stroke != null) { StrokeDashArray = stroke; }
            else { stroke = StrokeDashArray; }
            double height = Math.Abs(End.Y - Start.Y);
            double width;
            if(!isShiftKeyPressed)
            {
                width= Math.Abs(End.X - Start.X);
            }
            else
            {
                width = height;
                End = new Point(Start.X + width, Start.Y + width);
            }

            var shape = new Ellipse()
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(color),
                StrokeDashArray= stroke,
                StrokeThickness = thickness
            };

            Canvas.SetLeft(shape, Start.X);
            Canvas.SetTop(shape, Start.Y);
            TransformGroup transformGroup = new TransformGroup();
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

            Point center = new Point(shape.Width / 2, shape.Height / 2);
            rotateAngle = angle;
            RotateTransform rotateTransform = new RotateTransform(rotateAngle, center.X, center.Y);    
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(scaleTransform);
            shape.RenderTransform = transformGroup;

            return shape;

        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
