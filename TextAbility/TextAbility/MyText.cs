using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using PaintProject;
using System.Diagnostics;
using System.Windows.Shapes;

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
        public string Text { get; set; } = "";
        public void UpdateStart(Point p)
        {
            Start = p;
        }
        public void UpdateEnd(Point p)
        {
            End = p;
        }

        public UIElement Draw(Color color, int thickness, DoubleCollection stroke = null, bool isShiftKeyPressed = false, int angle = 0, string data = "")
        {
            ColorDrew = color;
            //ThicknessDrew = thickness;
            ShiftKey = isShiftKeyPressed;
            if (stroke != null) { StrokeDashArray = stroke; }
            else { stroke = StrokeDashArray; }
            var text = new TextBox()
            {
                Text = data,
                BorderBrush = new SolidColorBrush(ColorDrew),
                BorderThickness = new Thickness(thickness),
                Background = Brushes.Transparent,
                TextWrapping= TextWrapping.Wrap,
            };
            
            Canvas.SetLeft(text, Start.X);
            Canvas.SetTop(text, Start.Y);
            text.TextChanged += textChange;
            text.Width = Math.Abs(End.X - Start.X);
            text.Height = Math.Abs(End.Y - Start.Y);

            text.TextAlignment = TextAlignment.Left;
            text.VerticalContentAlignment = VerticalAlignment.Top;

            return text;
        }

        private void textChange(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text;
            this.Text = text;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
