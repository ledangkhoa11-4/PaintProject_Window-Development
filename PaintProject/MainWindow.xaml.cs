﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Linq;
using Telerik.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using Newtonsoft.Json;

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

        Dictionary<string, IShape> _abilities = new Dictionary<string, IShape>();

        private bool isDrawing = false;
        private Point startPoint;
        private Point endPoint;
        private IShape shape = null;  //Hình đang vẽ
        private UIElement lastDraw = null; //Hình preview cuối cùng (Không vẽ lại tất cả - Improve số 4)
        private List<IShape> listDrewShapes = new List<IShape>(); //Các hình đã vẽ

        private bool isBucketFillMode = false;
        private bool isSelectionMode = false;
     
        private IShape selectedShape = null;
        private UIElement sampleUI = null;
        private UIElement selectedUI = null;
        private Color selectedShapeColor;
        private int selectedShapeThickness = 0;
        private bool isFirstSelected = false;
        private Point startEditPoint;
        private Point originalStart;
        private Point originalEnd;
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
                return;
            }
            
            if (isSelectionMode && selectedShape == null)
            {
                UIElement clickedElement = null;
                HitTestResult hitTestResult = VisualTreeHelper.HitTest(mainPaper, mouseCoor);
                if (hitTestResult != null && hitTestResult.VisualHit != null)
                {
                    clickedElement = hitTestResult.VisualHit as UIElement;
                }

                // Check if an element was actually clicked
                if (clickedElement != null)
                {
                    int thickness = 0;
                    Color color;
                    if(clickedElement is System.Windows.Shapes.Line)
                    {
                        Debug.WriteLine("Line");
                        var line = (System.Windows.Shapes.Line)clickedElement;
                        thickness = (int)line.StrokeThickness;
                        var brush = line.Stroke as SolidColorBrush;
                        color = brush.Color;

                        var element = listDrewShapes.FirstOrDefault(shape =>
                        {
                            if (shape.name == "Line" && shape.Start == new Point(line.X1, line.Y1) && shape.End == new Point(line.X2, line.Y2))
                                return true;
                            return false;
                         });
                        if(element != null)
                        {
                            selectedUI = clickedElement;
                            selectedShapeColor = color;
                            selectedShapeThickness= thickness;
                            DashStyle dashStyle = new DashStyle(new double[] { 4, 2 }, 0);
                            var sampleLine = new System.Windows.Shapes.Line();
                            sampleLine.StrokeThickness = thickness;
                            sampleLine.Stroke = new SolidColorBrush(Colors.White);
                            sampleLine.X1 = line.X1;
                            sampleLine.Y1 = line.Y1;
                            sampleLine.X2 = line.X2;
                            sampleLine.Y2 = line.Y2;
                            sampleLine.StrokeDashArray = dashStyle.Dashes;
                            mainPaper.Children.Add(sampleLine);
                            selectedShape = element;
                            sampleUI = sampleLine;
                            isFirstSelected = true;
                            originalStart = selectedShape.Start;
                            originalEnd = selectedShape.End;
                            
                        }
                    }
                }
                return;
            }

            if (isSelectionMode && selectedShape != null)
            {
                if (selectedShape.name == "Line")
                {
                    var sampleline = (System.Windows.Shapes.Line)sampleUI;
                    mainPaper.Cursor = Cursors.Hand;
                    startEditPoint = mouseCoor;
                    Debug.WriteLine("Selected");
                    Debug.WriteLine($"{startEditPoint.X} - {startEditPoint.Y}");
                    isFirstSelected= false;
                }
                return;
                
            }
            isDrawing = true;
            startPoint = mouseCoor;
            shape.UpdateStart(startPoint);
            Debug.WriteLine("Start");
            mainPaper.CaptureMouse();
        }
        private void drawing(object sender, MouseEventArgs e)
        {
            Point mouseCoor = e.GetPosition(mainPaper);
            if (isDrawing)
            {
                endPoint = mouseCoor;
                shape.UpdateEnd(endPoint);
                UIElement drawShape = shape.Draw(Colors.Red, 2, isShiftKeyPressed);
                drawShape.MouseUp += stopDrawing;
                if (lastDraw == null) //first Drawing
                {
                    lastDraw = drawShape;
                    mainPaper.Children.Add(drawShape);
                }
                else
                {
                    mainPaper.Children.Remove(lastDraw);
                    mainPaper.Children.Add(drawShape);
                    lastDraw = drawShape;
                }
            }
            if(isSelectionMode == true && selectedShape!= null && !isFirstSelected)
            {
                var moveX = mouseCoor.X - startEditPoint.X;
                var moveY = mouseCoor.Y - startEditPoint.Y;

                selectedShape.UpdateStart(new Point(originalStart.X + moveX, originalStart.Y + moveY));
                selectedShape.UpdateEnd(new Point(originalEnd.X + moveX, originalEnd.Y + moveY));
                var newDraw = selectedShape.Draw(selectedShapeColor, selectedShapeThickness);
                newDraw.MouseUp += stopDrawing;
                mainPaper.Children.Remove(selectedUI);
                mainPaper.Children.Add(newDraw);
                selectedUI = newDraw;
            }
           
        }
        private void stopDrawing(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;
                Debug.WriteLine("Stop");
                mainPaper.ReleaseMouseCapture();
                listDrewShapes.Add((IShape)shape.Clone());
                lastDraw = null;
            }
            Debug.WriteLine("STop");
            if (isSelectionMode && selectedShape != null && !isFirstSelected) 
            {
                Debug.WriteLine("STop");
                mainPaper.Cursor = Cursors.Arrow;
                isFirstSelected = true;
                selectedShape = null;
                mainPaper.Children.Remove(sampleUI);
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
            //shape = _abilities["Line"];
            //line.IsChecked = true;
            bool ischecked = true;
            foreach (var ability in _abilities)
            {
                var button = new RadRibbonRadioButton()
                {
                    CollapseToMedium = CollapseThreshold.Never,
                    CollapseToSmall = CollapseThreshold.WhenGroupIsMedium,
                    IsAutoSize = true,
                    IsChecked= ischecked,
                    //LargeImage = new BitmapImage(new Uri(@"shapes_icon/{ability.Key.ToLower()}_32.png", UriKind.RelativeOrAbsolute)),
                    Size = Telerik.Windows.Controls.RibbonView.ButtonSize.Large,
                    Name = ability.Key.ToLower(),
                    //SmallImage = new BitmapImage(new Uri(@"shapes_icon/{ability.Key.ToLower()}_16.png", UriKind.RelativeOrAbsolute)),
                    Text = ability.Key
                };
                if(ischecked) ischecked= false;
                StyleManager.SetTheme(button, new MaterialTheme());

                var image32 = new BitmapImage();
                image32.BeginInit();
                image32.UriSource = new Uri(folder + $"shapes_icon/{ability.Key.ToLower()}_32.png", UriKind.Absolute);
                image32.EndInit();
                button.LargeImage = image32;

                var image16 = new BitmapImage();
                image16.BeginInit();
                image16.UriSource = new Uri(folder + $"shapes_icon/{ability.Key.ToLower()}_16.png", UriKind.Absolute);
                image16.EndInit();
                button.SmallImage = image16;

                button.Click += chooseShape;
                shapes.Items.Add(button);
            }
            shape = _abilities.FirstOrDefault().Value;
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
                isBucketFillMode = true;
                selectElementTg.IsChecked = false;
                mainPaper.Cursor = bucketCursor;
            }
            else
            {
                isBucketFillMode = false;
                mainPaper.Cursor = Cursors.Arrow;

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

        private void ScanLineFill(Point pt, Color targetColor, Color replacementColor)
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
                while (y1 >= 0 && GetColorAtPoint(getAbsolutePoint(new Point(temp.X, y1))) == targetColor)
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
                    else if (spanLeft && temp.X - 2 == 0 && GetColorAtPoint(getAbsolutePoint(new Point(temp.X - 2, y1))) != targetColor)
                    {
                        spanLeft = false;
                    }
                    if (!spanRight && temp.X < mainPaper.ActualWidth - 2 && GetColorAtPoint(getAbsolutePoint(new Point(temp.X + 2, y1))) == targetColor)
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
        private void chooseShape(object sender, RoutedEventArgs e)
        {
            var button = (RadRibbonRadioButton)sender;
            string name = (string)button.Text;
            shape = _abilities[name];
        }

        private void selectMode(object sender, RoutedEventArgs e)
        {
            if (selectElementTg.IsChecked == true)
            {
                isSelectionMode = true;
                bucketFill.IsChecked = false;
               
            }
            else
            {
                isSelectionMode = false;
                mainPaper.Cursor = Cursors.Arrow;

            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "paint";
            saveFileDialog.DefaultExt = ".bin";
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                WriteObjectListToFile(filePath, listDrewShapes);
            }
            ribbon.IsBackstageOpen= false;
        }
        void WriteObjectListToFile(string fileName, List<IShape> objectList)
        {
            string jsonString = JsonConvert.SerializeObject(listDrewShapes, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            File.WriteAllText(fileName, jsonString);
        }

        // Reading a list of objects from a .json file
        List<IShape> ReadObjectListFromFile(string fileName)
        {
            string jsonString = File.ReadAllText(fileName);
            List<IShape> objectList = JsonConvert.DeserializeObject<List<IShape>>(jsonString, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            return objectList;
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog= new OpenFileDialog();
            openFileDialog.Filter = "Files|*.bin";
            if(openFileDialog.ShowDialog() == true)
            {
                listDrewShapes= ReadObjectListFromFile(openFileDialog.FileName);
                foreach(var shape in listDrewShapes)
                {
                    UIElement drawshape= shape.Draw(Colors.Red, 2, isShiftKeyPressed);
                    mainPaper.Children.Add(drawshape);
                }
                
            }
        }
    }
}
