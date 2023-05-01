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
        public Color ColorDrew { get; set; }
        public int ThicknessDrew { get; set; }
        public bool ShiftKey { get; set; }
        public string Text { get; set; } //do not use
        public UIElement Draw(Color color, int thickness,DoubleCollection stroke, bool isShiftKeyPressed=false, int angle = 0, string data = "")
        {
            ColorDrew = color;
            ThicknessDrew = thickness;
            ShiftKey = isShiftKeyPressed;
            if (stroke != null) { StrokeDashArray = stroke; }
            else { stroke = StrokeDashArray; }
            double width = Math.Abs(End.X - Start.X);
            double height;
            if (isShiftKeyPressed)
            {
                height = width;
                End = new Point(Start.X + width, Start.Y + width);
            }
            else
            {
                height = Math.Abs(End.Y - Start.Y);
            }


            var shape = new Rectangle()
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(color),
                StrokeDashArray = stroke,
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
            shape.RenderTransform = scaleTransform;
           
            Point center = new Point( shape.Width / 2, shape.Height/2);
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
