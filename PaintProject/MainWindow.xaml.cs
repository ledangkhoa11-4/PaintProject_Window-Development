using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace PaintProject
{
    public partial class MainWindow : Window
    {
        bool isShiftKeyPressed = false;
        [DllImport("gdi32")]
        private static extern int GetPixel(int hdc, int nXPos, int nYPos);
        [DllImport("user32")]
        private static extern int GetWindowDC(int hwnd);
        [DllImport("user32")]
        private static extern int ReleaseDC(int hWnd, int hDC);

        Dictionary<string, IShape> _abilities =new Dictionary<string, IShape>();

        private bool isDrawing = false;
        private Point startPoint;
        private Point endPoint;
        private IShape shape = null;  //Hình đang vẽ
        private UIElement lastDraw = null; //Hình preview cuối cùng (Không vẽ lại tất cả - Improve số 4)
        private List<IShape> listDrewShapes = new List<IShape>(); //Các hình đã vẽ

        private bool isBucketFillMode = false;
        private Cursor bucketCursor;
        public MainWindow()
        {
            InitializeComponent();
            var folderInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "cursors");
            bucketCursor = new Cursor($"{folderInfo}\\bucket.cur");
        }

        private void startingDrawing(object sender, MouseButtonEventArgs e)
        {
            Point mouseCoor = e.GetPosition(mainPaper);
            Point mouseInScreen =  PointToScreen(e.GetPosition(this));
            if (isBucketFillMode)
            {
                Color color = GetColorAtPoint(mouseInScreen);
                string hex = "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
                Debug.WriteLine(hex);
                Debug.WriteLine(mouseCoor.X);
                Debug.WriteLine(mouseCoor.Y);
                ScanLineFill(mouseCoor, color, Colors.Red);
            }
            else
            {
                isDrawing = true;
                
                startPoint = mouseCoor;
                shape.UpdateStart(startPoint);
                Debug.WriteLine("Start");
                mainPaper.CaptureMouse();
            }
        }
        private void drawing(object sender, MouseEventArgs e)
        {
            if (!isDrawing)
                return;
            Point mouseCoor = e.GetPosition(mainPaper);
            endPoint= mouseCoor;
            shape.UpdateEnd(endPoint);
            UIElement drawShape = shape.Draw(Colors.Red, 1,isShiftKeyPressed);
            drawShape.MouseUp += stopDrawing;
            if(lastDraw == null) //first Drawing
            {
                lastDraw = drawShape;
                mainPaper.Children.Add(drawShape);
            }
            else
            {
                mainPaper.Children.Remove(lastDraw);
                mainPaper.Children.Add(drawShape);
                lastDraw= drawShape;
            }
        }
        private void stopDrawing(object sender, MouseButtonEventArgs e)
        {
            if(isDrawing)
            {
                isDrawing = false;
                Debug.WriteLine("Stop");
                mainPaper.ReleaseMouseCapture();
                listDrewShapes.Add((IShape)shape.Clone());
                lastDraw = null;
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += Window_PreviewKeyDown;

            
            this.PreviewKeyUp += Window_PreviewKeyUp;
            var domain = AppDomain.CurrentDomain;
            var folder = domain.BaseDirectory;

            var folderInfo = new DirectoryInfo(folder);

            Debug.WriteLine($"{folderInfo}");
            var dllFiles = folderInfo.GetFiles("*Ability*.dll");
            Debug.WriteLine(dllFiles.Length);
            foreach (var dll in dllFiles)
            {
                var assembly = Assembly.LoadFrom(dll.FullName);
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsClass &&
                        typeof(IShape).IsAssignableFrom(type))
                    {
                        var shape = Activator.CreateInstance(type) as IShape;
                        _abilities.Add(shape!.name, shape);
                    }
                }
            }
            shape = _abilities["Ellipse"];
            foreach (var ability in _abilities)
            {
                //2
            }
        }
        private Point getAbsolutePoint(Point pointInApp)
        {
            try
            {
                var x = PointToScreen(pointInApp).X;
                var y = PointToScreen(pointInApp).Y + ribbon.ActualHeight;
                return new Point(x, y);
            }
            catch
            {
                return new Point(0, 0);
            }
        }
        private void buckerFillChange(object sender, RoutedEventArgs e)
        {
            if (bucketFill.IsChecked == true)
            {
                isBucketFillMode= true;
                mainPaper.Cursor = bucketCursor;
            }
            else
            {
                isBucketFillMode = false;
                mainPaper.Cursor = Cursors.Arrow;

            }

            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check if Shift key is pressed
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                isShiftKeyPressed = true;
                // Handle "Shift" key hold event
                Console.WriteLine("Shift key is held down.");
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            // Check if Shift key is released
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                isShiftKeyPressed = false;
                // Handle "Shift" key release event
                Console.WriteLine("Shift key is released.");
            }
        }
        private Color GetColorAtPoint(Point point)
        {
            int lDC = GetWindowDC(0);
            int intColor = GetPixel(lDC, (int)point.X, (int)point.Y);
            ReleaseDC(0, lDC);
            byte a = (byte)((intColor >> 0x18) & 0xffL);
            byte b = (byte)((intColor >> 0x10) & 0xffL);
            byte g = (byte)((intColor >> 8) & 0xffL);
            byte r = (byte)(intColor & 0xffL);
            Color color = Color.FromRgb(r, g, b);
            return color;
        }

        void ScanLineFill(Point pt, Color targetColor, Color replacementColor)
        {
            targetColor = GetColorAtPoint(getAbsolutePoint(pt));
            if (targetColor == replacementColor)
            {
                return;
            }
            HashSet<Point> processedPixels = new HashSet<Point>();
            Stack<Point> pixels = new Stack<Point>();
            object lockObject = new object();
            pixels.Push(pt);
            ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            while (pixels.Count != 0 && isBucketFillMode)
            {

                Point temp = pixels.Pop();
                int y1 = (int)temp.Y;
                while (y1 >= 0 && GetColorAtPoint(getAbsolutePoint(new Point(temp.X, y1)))  == targetColor)
                {
                    y1--;
                }
                y1++;
                bool spanLeft = false;
                bool spanRight = false;
                while (y1 < mainPaper.ActualHeight && GetColorAtPoint(getAbsolutePoint(new Point(temp.X, y1))) == targetColor)
                {
                    //processedPixels.Add(new Point(temp.X, y1));
                    this.Dispatcher.Invoke(() =>
                    {
                        Rectangle rect = new Rectangle();
                        rect.Width = 2;
                        rect.Height = 2;
                        rect.Fill = new SolidColorBrush(replacementColor);
                        Canvas.SetLeft(rect, temp.X);
                        Canvas.SetTop(rect, y1);
                        mainPaper.Children.Add(rect);

                    }, DispatcherPriority.Background);

                    if (!spanLeft && temp.X > 0 && GetColorAtPoint(getAbsolutePoint(new Point(temp.X - 2, y1))) == targetColor)
                    {
                        pixels.Push(new Point(temp.X - 2, y1));
                        spanLeft = true;
                    }
                    else if (spanLeft && temp.X - 2 == 0  && GetColorAtPoint(getAbsolutePoint(new Point(temp.X - 2, y1))) != targetColor)
                    {
                        spanLeft = false;
                    }
                    if (!spanRight && temp.X < mainPaper.ActualWidth - 2 &&  GetColorAtPoint(getAbsolutePoint(new Point(temp.X + 2, y1))) == targetColor)
                    {
                        pixels.Push(new Point(temp.X + 2, y1));
                        spanRight = true;
                    }
                    else if (spanRight && temp.X < mainPaper.ActualWidth - 2 && GetColorAtPoint(getAbsolutePoint(new Point(temp.X + 2, y1))) != targetColor)
                    {
                        spanRight = false;
                    }
                    y1++;
                }

            }
            Debug.WriteLine("Finish");
        }
    }
}
