using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using PaintProject;
namespace TextAbility
{
    public class MyText : IShape
    {
        public string name { get => "Text"; }

        public Point Start { get; set; }
        public Point End { get; set; }

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

        public UIElement Draw(Color color, int thickness, DoubleCollection stroke = null, bool isShiftKeyPressed = false, int angle = 0)
        {
            ColorDrew = color;
            //ThicknessDrew = thickness;
            ShiftKey = isShiftKeyPressed;
            if (stroke != null) { StrokeDashArray = stroke; }
            else { stroke = StrokeDashArray; }

            var text = new TextBox()
            {
                Text = "",
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(thickness),
                Background = Brushes.Transparent
            };

            Canvas.SetLeft(text, Start.X);
            Canvas.SetTop(text, Start.Y);

            text.Width = Math.Abs(End.X - Start.X);
            text.Height = Math.Abs(End.Y - Start.Y);

            text.TextAlignment = TextAlignment.Left;
            text.VerticalContentAlignment = VerticalAlignment.Top;

            return text;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
