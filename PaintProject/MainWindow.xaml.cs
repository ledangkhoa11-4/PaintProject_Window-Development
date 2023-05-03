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
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Linq;
using Telerik.Windows.Controls;
using Microsoft.Win32;
using Path = System.IO.Path;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;
using System.Text;

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
        private UIElement lastShape = null;
        private List<IShape> listDrewShapes = new List<IShape>(); //Các hình đã vẽ
        private Color selectedColor = Colors.Black;
        private int thickness = 3;
        private DoubleCollection stroke = new DoubleCollection();

        private bool isBucketFillMode = false;
        private bool isSelectionMode = false;
        private bool isEraserMode = false;
        private bool isRotate = false;

        private IShape selectedShape = null;
        private Canvas sampleUI = null;
        private Color selectedShapeColor;
        private int selectedShapeThickness = 0;
        private bool isFirstSelected = false;
        private Point startEditPoint;
        private Point originalStart;
        private Point originalEnd;
        private Cursor bucketCursor;
        private bool isFileSave=true;
        private string curFilePath = "";
        private bool haveImageOrFill=false;
        private Cursor moveCursor;
        private Cursor rotateCursor;
        private Cursor eraserCursor;
        private System.Windows.Controls.Image imageSelected = null;

        private Ellipse rotateCircle = null;
        private Point originalCoor;
        private Point anchorPoint;
        private Point startRotatePoint;
        private RecentFilesManager recentFilesManager;
        private List<IShape> listUndoShape = new List<IShape>();

        private bool erasering = false;
        public MainWindow()
        {
            InitializeComponent();
            var folderInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "cursors");
            bucketCursor = new Cursor($"{folderInfo}\\bucket.cur");
            moveCursor = new Cursor($"{folderInfo}\\move.cur");
            rotateCursor = new Cursor($"{folderInfo}\\rotate.cur");
            eraserCursor = new Cursor($"{folderInfo}\\eraser.cur");
            recentFilesManager=new RecentFilesManager();
            recentFilesManager.LoadRecentFiles();
        }

        private void startingDrawing(object sender, MouseButtonEventArgs e)
        {
            Point mouseCoor = e.GetPosition(mainPaper);
            Point mouseInScreen = PointToScreen(e.GetPosition(this));
            if (isBucketFillMode)
            {
                Color color = GetColorAtPoint(mouseInScreen);
                string hex = "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
                haveImageOrFill = true;
                ScanLineFill(mouseCoor, color, selectedColor);
                return;
            }
            if (isSelectionMode && selectedShape == null)
            {
                bool isOverrideCirclePoint = false;
                UIElement clickedElement = null;
                HitTestResult hitTestResult = VisualTreeHelper.HitTest(mainPaper, mouseCoor);
                if (hitTestResult != null && hitTestResult.VisualHit != null)
                    clickedElement = hitTestResult.VisualHit as UIElement;
                IShape element = null;
                int width = 0;
                int height = 0;
                if (clickedElement != null)
                {
                    if (clickedElement is System.Windows.Shapes.Line)
                    {
                        var line = (System.Windows.Shapes.Line)clickedElement;
                        line.StrokeThickness = thickness;
                        var brush = line.Stroke as SolidColorBrush;
                        element = listDrewShapes.FirstOrDefault(shape =>
                        {
                            if (shape.name == "Line" && shape.Start == new Point(line.X1, line.Y1) && shape.End == new Point(line.X2, line.Y2))
                            {
                                originalCoor = new Point((line.X1 + line.X2) / 2, (line.Y1 + line.Y2) / 2);
                                isOverrideCirclePoint = true;
                                return true;
                            }
                            return false;
                        });
                    }
                    if (clickedElement is System.Windows.Shapes.Rectangle)
                    {
                        var rect = (System.Windows.Shapes.Rectangle)clickedElement;
                        rect.StrokeThickness = thickness;
                        var brush = rect.Stroke as SolidColorBrush;
                        var left = Canvas.GetLeft(rect);
                        var top = Canvas.GetTop(rect);
                        width = (int)rect.ActualWidth;
                       
                        element = listDrewShapes.FirstOrDefault(shape =>
                        {
                            if (shape.name == "Rectangle" && shape.Start == new Point(left, top))
                            {
                                originalCoor = new Point(Canvas.GetLeft(rect) + rect.Width / 2, Canvas.GetTop(rect) + rect.Height / 2);
                                return true;
                            }
                            return false;
                        });
                    }
                    if (clickedElement is System.Windows.Shapes.Ellipse)
                    {
                        var ellip = (System.Windows.Shapes.Ellipse)clickedElement;
                        ellip.StrokeThickness = thickness;
                        var brush = ellip.Stroke as SolidColorBrush;
                        var left = Canvas.GetLeft(ellip);
                        var top = Canvas.GetTop(ellip);
                        width = (int)ellip.ActualWidth;
                        height = (int)ellip.ActualHeight/2;
                        element = listDrewShapes.FirstOrDefault(shape =>
                        {
                            if (shape.name == "Ellipse" && shape.Start == new Point(left, top))
                            {
                                originalCoor = new Point(Canvas.GetLeft(ellip) + ellip.Width / 2, Canvas.GetTop(ellip) + ellip.Height / 2);
                                return true;
                            }
                            return false;
                        });
                    }
                    if (clickedElement is System.Windows.Controls.Image)
                    {
                        var image = (System.Windows.Controls.Image)clickedElement;
                        element = _abilities["Rectangle"];
                        element.UpdateStart(new Point(Canvas.GetLeft(image), Canvas.GetTop(image)));
                        element.UpdateEnd(new Point(Canvas.GetLeft(image) + image.ActualWidth, Canvas.GetTop(image) + image.ActualHeight));
                        originalCoor = new Point(Canvas.GetLeft(image) + image.ActualWidth/2, Canvas.GetTop(image) + image.ActualHeight/2);
                        var imageTrans = image.RenderTransform as RotateTransform;
                        if (imageTrans != null)
                            element.rotateAngle = (int)imageTrans.Angle;
                        imageSelected = image;
                    }
                    if (element != null)
                    {
                        selectedShape = element;
                        originalStart = selectedShape.Start;
                        originalEnd = selectedShape.End;
                        selectedShapeColor = selectedShape.ColorDrew;
                        selectedShapeThickness = selectedShape.ThicknessDrew;

                        var sampleShape = _abilities[element.name];
                        var sampleCanvas = new Canvas();
                        Canvas.SetLeft(sampleCanvas, Math.Min(element.Start.X, element.End.X));
                        Canvas.SetTop(sampleCanvas, Math.Min(element.Start.Y, element.End.Y));
                        sampleCanvas.Width = Math.Abs(element.Start.X - element.End.X);
                        sampleCanvas.Height = Math.Abs(element.Start.Y - element.End.Y);
                        var topLeft = new Point(Canvas.GetLeft(sampleCanvas), Canvas.GetTop(sampleCanvas));
                        if (topLeft != element.Start && topLeft != element.End)
                        {
                            sampleShape.UpdateStart(new Point(0, sampleCanvas.Height));
                            sampleShape.UpdateEnd(new Point(sampleCanvas.Width, 0));
                        }
                        else
                        {
                            sampleShape.UpdateStart(new Point(0, 0));
                            sampleShape.UpdateEnd(new Point(sampleCanvas.Width, sampleCanvas.Height));
                        }
                        sampleCanvas.RenderTransform = new RotateTransform(element.rotateAngle, sampleCanvas.Width/2, sampleCanvas.Height/2);
                        var sampleLine = sampleShape.Draw(Colors.Thistle, 2, new DoubleCollection(new double[] { 4, 2 }),false);
                        sampleUI = sampleCanvas;
                        isFirstSelected = true;
                        lastDraw = clickedElement;
                        
                        rotateCircle = new Ellipse();
                        rotateCircle.Width = 8; rotateCircle.Height = 8;
                        rotateCircle.Stroke = new SolidColorBrush(Colors.Red);
                        rotateCircle.Fill = new SolidColorBrush(Colors.White);
                        rotateCircle.StrokeThickness = 1;
                        if(isOverrideCirclePoint)
                            anchorPoint = new Point(sampleShape.Start.X, sampleShape.Start.Y);
                        else
                            anchorPoint = new Point(sampleCanvas.Width - 4, height - 4);
                        Canvas.SetLeft(rotateCircle, anchorPoint.X);
                        Canvas.SetTop(rotateCircle, anchorPoint.Y);
                        rotateCircle.Cursor = rotateCursor;
                        sampleCanvas.Children.Add(rotateCircle);
                        sampleCanvas.Children.Add(sampleLine);
                        mainPaper.Children.Add(sampleCanvas);
                    }
                }
                return;
            }

            if (isSelectionMode && selectedShape != null)
            {
                HitTestResult hitTestResult = VisualTreeHelper.HitTest(mainPaper, mouseCoor);
                if (hitTestResult != null && hitTestResult.VisualHit != null)
                {
                    var clickedEle = hitTestResult.VisualHit as UIElement;
                    if (clickedEle == rotateCircle)
                    {
                        isRotate = true;
                        startRotatePoint = mouseCoor;
                        return;
                    }
                }
                mainPaper.Cursor = moveCursor;
                startEditPoint = mouseCoor;
                isFirstSelected = false;
                return;

            }
            if (isEraserMode == true)
            {
                erasering = true;
                return;
            }
            isDrawing = true;
            startPoint = mouseCoor;
            
            shape.UpdateStart(startPoint);
            mainPaper.CaptureMouse();
        }
        private void drawing(object sender, MouseEventArgs e)
        {
           
            Point mouseCoor = e.GetPosition(mainPaper);
            if (isDrawing)
            {
                endPoint = mouseCoor;
                shape.UpdateEnd(endPoint);
                UIElement drawShape = shape.Draw(selectedColor, thickness, stroke, isShiftKeyPressed, 0,"");
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
                    lastShape = drawShape;
                }
            }
            if (isSelectionMode == true && selectedShape != null && !isFirstSelected)
            {
                var moveX = mouseCoor.X - startEditPoint.X;
                var moveY = mouseCoor.Y - startEditPoint.Y;
                if (imageSelected == null)
                {
                    selectedShape.UpdateStart(new Point(originalStart.X + moveX, originalStart.Y + moveY));
                    selectedShape.UpdateEnd(new Point(originalEnd.X + moveX, originalEnd.Y + moveY));
                    var newDraw = selectedShape.Draw(selectedShapeColor, selectedShapeThickness, null, isShiftKeyPressed, selectedShape.rotateAngle);
                    newDraw.MouseUp += stopDrawing;
                    if(lastDraw!=null)
                    {
                        mainPaper.Children.Remove(lastDraw);
                    }
                    mainPaper.Children.Add(newDraw);
                    lastDraw = newDraw;
                    mainPaper.Children.Remove(rotateCircle);
                }
                else
                {
                    Canvas.SetLeft(imageSelected,originalStart.X + moveX);
                    Canvas.SetTop(imageSelected,originalStart.Y + moveY);
                }
            }
            if (isSelectionMode == true && isRotate)
            {
                var defaultRad = Math.Atan2(startRotatePoint.Y - originalCoor.Y, startRotatePoint.X - originalCoor.X );
                var defaultDegree = (int)(defaultRad * 180 / Math.PI);
                var angleRad = Math.Atan2(mouseCoor.Y-originalCoor.Y, mouseCoor.X-originalCoor.X);
                var angleDeg= (int)(angleRad * 180 / Math.PI);

                if (imageSelected == null)
                {
                    var newDraw = selectedShape.Draw(selectedShapeColor, selectedShapeThickness, null, isShiftKeyPressed, angleDeg - defaultDegree);
                    newDraw.MouseUp += stopDrawing;
                    if (lastDraw != null)
                    {
                        mainPaper.Children.Remove(lastDraw);
                    }
                    mainPaper.Children.Add(newDraw);
                    lastDraw = newDraw;
                    mainPaper.Children.Remove(rotateCircle);
                }
                else
                {
                    imageSelected.RenderTransform = new RotateTransform(angleDeg - defaultDegree,  imageSelected.ActualWidth/2,  imageSelected.ActualHeight/2);
                }
                mainPaper.Cursor = rotateCursor;
            }
            if (erasering)
            {
                HitTestResult hitTestResult = VisualTreeHelper.HitTest(mainPaper, mouseCoor);
                if (hitTestResult != null && hitTestResult.VisualHit != null)
                {
                    
                    var clickedEle = hitTestResult.VisualHit as UIElement;
                    
                    var top = Canvas.GetTop(clickedEle);
                    var left = Canvas.GetLeft(clickedEle);
                   
                    if (clickedEle is Line)
                    {
                        var line = (Line)clickedEle;
                        left = line.X1;
                        top = line.Y1;
                    }
                    if (clickedEle is Rectangle || clickedEle is Image)
                    {
                        mainPaper.Children.Remove(clickedEle);

                    }
                    var ishapeSelected = listDrewShapes.FirstOrDefault(shape => shape.Start == new Point(left, top));
                    if(ishapeSelected != null) { 
                        listDrewShapes.Remove(ishapeSelected);
                        mainPaper.Children.Remove(clickedEle);
                        listUndoShape.Add(ishapeSelected);

                    }
                }
            }
        }
        private void stopDrawing(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;
                mainPaper.ReleaseMouseCapture();
                if (mainPaper.Children.Count < listDrewShapes.Count)
                {
                    listDrewShapes.RemoveRange(mainPaper.Children.Count - 1, listDrewShapes.Count - mainPaper.Children.Count + 1);
                }
                listDrewShapes.Add((IShape)shape.Clone());
                isFileSave = false;
                lastDraw = null;
            }
           
            if (isSelectionMode && selectedShape != null && !isFirstSelected)
            {
           
                mainPaper.Cursor = Cursors.Arrow;
                isFirstSelected = true;
                selectedShape = null;
                mainPaper.Children.Remove(sampleUI);
                mainPaper.Children.Remove(rotateCircle);
                imageSelected = null;
            }
            if (isSelectionMode && isRotate)
            {
                mainPaper.Cursor = Cursors.Arrow;
                isFirstSelected = true;
                selectedShape = null;
                mainPaper.Children.Remove(sampleUI);
                mainPaper.Children.Remove(rotateCircle);
                imageSelected = null;
                isRotate = false;
            }
            if(isEraserMode == true)
            {
                erasering = false;
            }
            if(UndoButton.IsEnabled == false)
            {
                UndoButton.IsEnabled = true;
                Border border = UndoButton.FindChildByType<Border>();
                border.Background = new SolidColorBrush(Color.FromRgb(43, 196, 138));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewKeyDown += Window_PreviewKeyDown;


            this.PreviewKeyUp += Window_PreviewKeyUp;
            this.KeyDown += MainWindow_KeyDown;
            this.DataContext = recentFilesManager.RecentFiles;
            var domain = AppDomain.CurrentDomain;
            var folder = domain.BaseDirectory;

            var folderInfo = new DirectoryInfo(folder);

            var dllFiles = folderInfo.GetFiles("*Ability*.dll");
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
                        Debug.WriteLine(shape.name);
                        _abilities.Add(shape!.name, shape);
                    }
                }
            }
            if (_abilities.Count == 0)
            {
                var dialog = MessageBox.Show("No possibility drawings found. Please add more ability dll!!!", "No ability found",MessageBoxButton.OK,MessageBoxImage.Error);
                if(dialog == MessageBoxResult.OK)
                    System.Windows.Application.Current.Shutdown();
            }
            bool ischecked = true;
            foreach (var ability in _abilities)
            {
                var button = new RadRibbonRadioButton()
                {
                    CollapseToMedium = CollapseThreshold.Never,
                    CollapseToSmall = CollapseThreshold.WhenGroupIsMedium,
                    IsAutoSize = true,
                    IsChecked = ischecked,
                    //LargeImage = new BitmapImage(new Uri(@"shapes_icon/{ability.Key.ToLower()}_32.png", UriKind.RelativeOrAbsolute)),
                    Size = Telerik.Windows.Controls.RibbonView.ButtonSize.Large,
                    Name = ability.Key.ToLower(),
                    //SmallImage = new BitmapImage(new Uri(@"shapes_icon/{ability.Key.ToLower()}_16.png", UriKind.RelativeOrAbsolute)),
                    Text = ability.Key
                };
                if (ischecked) ischecked = false;
                StyleManager.SetTheme(button, new MaterialTheme());

                try
                {
                    var image32 = new BitmapImage();
                    image32.BeginInit();
                    image32.UriSource = new Uri(folder + $"shapes_icon/{ability.Key.ToLower()}_32.png", UriKind.Absolute);
                    image32.EndInit();
                    button.LargeImage = image32;
                    Debug.WriteLine(ability.Key.ToLower());

                    var image16 = new BitmapImage();
                    image16.BeginInit();
                    image16.UriSource = new Uri(folder + $"shapes_icon/{ability.Key.ToLower()}_16.png", UriKind.Absolute);
                    image16.EndInit();
                    button.SmallImage = image16;
                    Debug.WriteLine(ability.Key.ToLower());
                }
                catch
                {
                    var image32 = new BitmapImage();
                    image32.BeginInit();
                    image32.UriSource = new Uri(folder + $"shapes_icon/default_32.png", UriKind.Absolute);
                    image32.EndInit();
                    button.LargeImage = image32;

                    var image16 = new BitmapImage();
                    image16.BeginInit();
                    image16.UriSource = new Uri(folder + $"shapes_icon/default_16.png", UriKind.Absolute);
                    image16.EndInit();
                    button.SmallImage = image16;
                }


                button.Click += chooseShape;
                shapes.Items.Add(button);
            }
            shape = _abilities.FirstOrDefault().Value;
            weightInfo.Text = thickness.ToString() + "px";
            strokeSelect.Text = "Line";
            UndoButton.MouseEnter += OnMouseEnterButton1;
            UndoButton.MouseLeave += OnMouseLeaveButton1;
            RedoButton.MouseLeave += OnMouseLeaveButton2;
            RedoButton.MouseEnter += OnMouseEnterButton2;
        }
        private Point getAbsolutePoint(Point pointInApp)
        {
            try
            {
                var x = PointToScreen(pointInApp).X;
                var y = PointToScreen(pointInApp).Y + ribbon.ActualHeight + header.ActualHeight;
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
                eraserElementTg.IsChecked = false;
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
            bucketFill.IsChecked = false;
            selectElementTg.IsChecked = false;
            eraserElementTg.IsChecked = false;
        }

        private void selectMode(object sender, RoutedEventArgs e)
        {
            if (selectElementTg.IsChecked == true)
            {
                isSelectionMode = true;
                bucketFill.IsChecked = false;
                eraserElementTg.IsChecked = false;

            }
            else
            {
                lastDraw = null;
                isSelectionMode = false;
                mainPaper.Cursor = Cursors.Arrow;
                isFirstSelected = true;
                selectedShape = null;
                mainPaper.Children.Remove(sampleUI);
                mainPaper.Children.Remove(rotateCircle);

            }
        }

        private void ColorPickerChanged(object sender, EventArgs e)
        {
            RadColorPicker colorPicker = sender as RadColorPicker;
            selectedColor = colorPicker.SelectedColor;
            if(isSelectionMode && selectedShape != null) {
                mainPaper.Children.Remove(lastDraw);
                mainPaper.Children.Remove(sampleUI);
                var newDrawColor = selectedShape.Draw(selectedColor, selectedShapeThickness, selectedShape.StrokeDashArray, false, selectedShape.rotateAngle);
                selectedShapeColor = selectedColor;
                mainPaper.Children.Add(newDrawColor);
                mainPaper.Children.Add(sampleUI);
                lastDraw = newDrawColor;
            }

        }

        private void ChangeWeight(object sender, SelectionChangedEventArgs e)
        {
            int i = listWeight.SelectedIndex;
            if (i == 0) thickness = 1;
            else if (i == 1) thickness = 3;
            else if(i == 2) thickness = 5;
            else if(i == 3) thickness = 8;
            weightInfo.Text = thickness.ToString() + "px";

            if (isSelectionMode && selectedShape != null)
            {
                mainPaper.Children.Remove(sampleUI);
                mainPaper.Children.Remove(lastDraw);
                var newDrawColor = selectedShape.Draw(selectedShapeColor, thickness, selectedShape.StrokeDashArray, false, selectedShape.rotateAngle);
                selectedShapeThickness = thickness;
                mainPaper.Children.Add(newDrawColor);
                mainPaper.Children.Add(sampleUI);
                lastDraw = newDrawColor;
            }
        }

        private void ChangeStroke(object sender, SelectionChangedEventArgs e)
        {
            int i = listStroke.SelectedIndex;
            if (i == 0)
            {
                strokeSelect.Text = "Line";
                stroke = new DoubleCollection() { };
            }
            else if (i == 1)
            {
                strokeSelect.Text = "Dot";
                stroke = new DoubleCollection() { 1 };
            }
            else if (i == 2)
            {
                strokeSelect.Text = "Dash";
                stroke = new DoubleCollection() { 4, 1 };
            }
            else if (i == 3)
            {
                strokeSelect.Text = "Dash Dot Dot";
                stroke = new DoubleCollection() { 4, 1, 1, 1, 1, 1 };
            }

            if (isSelectionMode && selectedShape != null)
            {
                mainPaper.Children.Remove(sampleUI);
                mainPaper.Children.Remove(lastDraw);
                var newDrawColor = selectedShape.Draw(selectedShapeColor, thickness, stroke, false, selectedShape.rotateAngle);
                mainPaper.Children.Add(newDrawColor);
                mainPaper.Children.Add(sampleUI);
                lastDraw = newDrawColor;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (isFileSave) { return; }
            if (haveImageOrFill)
            {
                if (curFilePath == "")
                {
                    ExportImageFile(new PngBitmapEncoder());
                }
                else
                {
                    var encoder= new PngBitmapEncoder();
                    RenderTargetBitmap rtb = new RenderTargetBitmap((int)mainPaper.ActualWidth, (int)mainPaper.ActualHeight, 96d, 96d, PixelFormats.Default);
                    rtb.Render(mainPaper);
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    try
                    {
                       
                        using (FileStream stream = new FileStream(curFilePath, FileMode.Create, FileAccess.Write))
                        {
                            // Save the encoder to the stream
                            encoder.Save(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error saving file: " + ex.Message);
                    }
                    
                    
                }
                isFileSave = true;
            }
            else
            {
                if (curFilePath == "")
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = "paint";
                    saveFileDialog.DefaultExt = ".bin";
                    saveFileDialog.Filter = "Files|*.bin;*.dat;*.dkpq";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        curFilePath = saveFileDialog.FileName;

                        WriteObjectListToFile(curFilePath, listDrewShapes);
                        isFileSave = true;
                        title.Text = Path.GetFileName(saveFileDialog.FileName) + "- Paint";
                        recentFilesManager.AddRecentFile(Path.GetFileName(saveFileDialog.FileName), saveFileDialog.FileName);
                    }
                    else
                    {
                        isFileSave = false;
                    }
                }
                else
                {
                    WriteObjectListToFile(curFilePath, listDrewShapes);
                }
                ribbon.IsBackstageOpen = false;
            } 
            
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
            ribbon.IsBackstageOpen = false;
            if (!isFileSave && listDrewShapes.Count>0)
            {
                string messageBoxText = "Do you want to save changes?";
                string caption = "Save file";
                MessageBoxButton button = MessageBoxButton.YesNoCancel;
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBoxResult result;

                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                if (result == MessageBoxResult.Yes)
                {
                    SaveBtn_Click(sender, e);
                }
                else
                {
                    isFileSave = true;
                    OpenFileBtn_Click(sender: this, e: e);
                    
                }
            }
            else
            {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Files|*.bin;*.dat;*.dkpq;*.jpeg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == true)
                {
                    curFilePath= openFileDialog.FileName;
                    title.Text = Path.GetFileName(openFileDialog.FileName) + "- Paint";
                    string ext = Path.GetExtension(openFileDialog.FileName).ToLower().Replace(".","");
                    if (ext == "jpeg" || ext == "png" || ext == "bmp")
                    {
                        haveImageOrFill= true;
                        mainPaper.Children.Clear();
                        using (FileStream fileStream = new FileStream(curFilePath, FileMode.Open, FileAccess.Read))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = fileStream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            double width = bitmap.Width;
                            double height = bitmap.Height;
                            mainPaper.Width = width;
                            mainPaper.Height = height;
                            ImageBrush imageBrush = new ImageBrush();
                            imageBrush.ImageSource = bitmap;
                            mainPaper.Background = imageBrush;
                        }
                       
                    }
                    else
                    {
                        try
                        {
                            mainPaper.Background = new SolidColorBrush(Colors.White);
                            mainPaper.Children.Clear();
                            listDrewShapes = ReadObjectListFromFile(openFileDialog.FileName);

                            haveImageOrFill = false;
                            foreach (var shape in listDrewShapes)
                            {
                                UIElement drawshape = shape.Draw(shape.ColorDrew, shape.ThicknessDrew, shape.StrokeDashArray, false, shape.rotateAngle);
                                mainPaper.Children.Add(drawshape);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    recentFilesManager.AddRecentFile(Path.GetFileName(openFileDialog.FileName), openFileDialog.FileName);


                }
            }
        }
        private void ExportImageFile(BitmapEncoder encoder)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)mainPaper.ActualWidth, (int)mainPaper.ActualHeight, 96d, 96d, PixelFormats.Default);
            rtb.Render(mainPaper);
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = encoder.GetType().ToString().ToLower().Replace("bitmapencoder", "").Replace("system.windows.media.imaging", "");
            saveFileDialog.FileName = "paint";
            
            saveFileDialog.OverwritePrompt = true;
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    curFilePath= saveFileDialog.FileName;
                    title.Text= Path.GetFileName(curFilePath)+"- Paint";
                    using (FileStream fs = File.Create(saveFileDialog.FileName))
                    {
                        encoder.Save(fs);
                        recentFilesManager.AddRecentFile(Path.GetFileName(curFilePath), curFilePath);
                            
                    }
                }catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void ExportPngFile(object sender, RoutedEventArgs e)
        {
            ExportImageFile(new PngBitmapEncoder());
            isFileSave = true;


        }

        private void ExportJpgFile(object sender, RoutedEventArgs e)
        {
            ExportImageFile(new JpegBitmapEncoder());
            isFileSave = true;
        }

        private void ExportBmpFile(object sender, RoutedEventArgs e)
        {
            ExportImageFile(new BmpBitmapEncoder());
            isFileSave = true;
        }
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            double newScale = scaleTransform.ScaleX * 1.1;
            if (newScale <= 8.0)
            {
                scaleTransform.ScaleX = newScale;
                scaleTransform.ScaleY = newScale;
                ZoomSlider.Value = newScale;
                ZoomPercentage.Text = string.Format("{0}%", (int)(newScale * 100));
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            double newScale = scaleTransform.ScaleX / 1.1;
            if (newScale >= 0.1)
            {
                scaleTransform.ScaleX = newScale;
                scaleTransform.ScaleY = newScale;
                ZoomSlider.Value = newScale;
                ZoomPercentage.Text = string.Format("{0}%", (int)(newScale * 100));
            }
        }

        private void mainPaper_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    ZoomIn_Click(null, null);
                }
                else if (e.Delta < 0)
                {
                    ZoomOut_Click(null, null);
                }
                e.Handled = true;
            }
        }
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (scaleTransform != null)
            {
                double newScale = ZoomSlider.Value;
                scaleTransform.ScaleX = newScale;
                scaleTransform.ScaleY = newScale;
                ZoomPercentage.Text = string.Format("{0}%", (int)(newScale * 100));
            }
        }
        

        private void ImportImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a OpenFileDialog to allow the user to select an image file
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                var bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                var image = new System.Windows.Controls.Image() { Source = bitmapImage, Stretch = Stretch.Fill };
                image.Width = 200;
                
                Canvas.SetLeft(image, 0);
                Canvas.SetTop(image, 0);
                mainPaper.Children.Add(image);
                haveImageOrFill = true;
            }
        }

        private void NewFileBtn_Click(object sender, RoutedEventArgs e)
        {
            ribbon.IsBackstageOpen = false;
            if (!isFileSave && listDrewShapes.Count > 0)
            {
                string messageBoxText = "Do you want to save changes?";
                string caption = "Save file";
                MessageBoxButton button = MessageBoxButton.YesNoCancel;
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBoxResult result;

                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                if (result == MessageBoxResult.Yes)
                {
                    SaveBtn_Click(sender, e);
                    mainPaper.Children.Clear();
                    listDrewShapes.Clear();
                    isFileSave= false;
                    haveImageOrFill= false;
                    mainPaper.Background = new SolidColorBrush(Colors.White);
                    curFilePath = "";
                }
                else
                {
                    isFileSave = true;
                    NewFileBtn_Click(sender: this, e: e);

                }
            }
            else
            {
                mainPaper.Children.Clear();
                listDrewShapes.Clear();
                mainPaper.Background = new SolidColorBrush(Colors.White);
                isFileSave = false;
                haveImageOrFill = false;
                curFilePath = "";

            }

        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void eraserMode(object sender, RoutedEventArgs e)
        {
            if (eraserElementTg.IsChecked == true)
            {
                isEraserMode = true;
                bucketFill.IsChecked = false;
                selectElementTg.IsChecked = false;
                mainPaper.Cursor = eraserCursor;

            }
            else
            {
                isEraserMode = false;
                mainPaper.Cursor = Cursors.Arrow;
            }
        }
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    if (mainPaper.Children.Count > 0)
                    {
                        if (RedoButton.IsEnabled == false)
                        {
                            RedoButton.IsEnabled = true;
                            Border border = RedoButton.FindChildByType<Border>();
                            border.Background = new SolidColorBrush(Color.FromRgb(43, 196, 138));
                        }
                        mainPaper.Children.RemoveAt(mainPaper.Children.Count - 1);
                        IShape removeShape = listDrewShapes[listDrewShapes.Count - 1];
                        listDrewShapes.Remove(removeShape);
                        listUndoShape.Add(removeShape);
                    }
                    if (mainPaper.Children.Count == 0 && UndoButton.IsEnabled)
                    {
                        UndoButton.IsEnabled = false;
                        Border border = UndoButton.FindChildByType<Border>();
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    }
                }
                else if (e.Key == Key.Y)
                {
                    if (listUndoShape.Count > 0)
                    {
                        if (UndoButton.IsEnabled == false)
                        {
                            UndoButton.IsEnabled = true;
                            Border border = UndoButton.FindChildByType<Border>();
                            border.Background = new SolidColorBrush(Color.FromRgb(43, 196, 138));
                        }
                        IShape shapeRedo = listUndoShape[listUndoShape.Count-1];
                        UIElement temp = shapeRedo.Draw(shapeRedo.ColorDrew, shapeRedo.ThicknessDrew, shapeRedo.StrokeDashArray, shapeRedo.ShiftKey);
                        listDrewShapes.Add(shapeRedo);
                        listUndoShape.Remove(shapeRedo);
                        temp.MouseUp += stopDrawing;
                        mainPaper.Children.Add(temp);
                    }
                    if (listUndoShape.Count > 0 && RedoButton.IsEnabled)
                    {
                        RedoButton.IsEnabled = false;
                        Border border = RedoButton.FindChildByType<Border>();
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    }
                }
            }
        }
        private void UndoClick(object sender, RoutedEventArgs e)
        {
            if (mainPaper.Children.Count > 0)
            {
                if (RedoButton.IsEnabled == false)
                {
                    RedoButton.IsEnabled = true;
                    Border border = RedoButton.FindChildByType<Border>();
                    border.Background = new SolidColorBrush(Color.FromRgb(43, 196, 138));
                }
                mainPaper.Children.RemoveAt(mainPaper.Children.Count - 1);
                IShape removeShape = listDrewShapes[listDrewShapes.Count - 1];
                listDrewShapes.Remove(removeShape);
                listUndoShape.Add(removeShape);
            }
            if (mainPaper.Children.Count == 0 && UndoButton.IsEnabled)
            {
                UndoButton.IsEnabled = false;
                Border border = UndoButton.FindChildByType<Border>();
                border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
        }

        private void RedoClick(object sender, RoutedEventArgs e)
        {
            if (listUndoShape.Count > 0)
            {
                if (UndoButton.IsEnabled == false)
                {
                    UndoButton.IsEnabled = true;
                    Border border = UndoButton.FindChildByType<Border>();
                    border.Background = new SolidColorBrush(Color.FromRgb(43, 196, 138));
                }
                IShape shapeRedo = listUndoShape[listUndoShape.Count - 1];
                UIElement temp = shapeRedo.Draw(shapeRedo.ColorDrew, shapeRedo.ThicknessDrew, shapeRedo.StrokeDashArray, shapeRedo.ShiftKey);
                listDrewShapes.Add(shapeRedo);
                listUndoShape.Remove(shapeRedo);
                temp.MouseUp += stopDrawing;
                mainPaper.Children.Add(temp);
            }
            if (listUndoShape.Count == 0 && RedoButton.IsEnabled)
            {
                RedoButton.IsEnabled = false;
                Border border = RedoButton.FindChildByType<Border>();
                border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
        }
        private void OnMouseEnterButton1(object sender, EventArgs e)
        {
            Border border = UndoButton.FindChildByType<Border>();
            border.Background = new SolidColorBrush(Color.FromRgb(40, 152, 172));
        }
        private void OnMouseLeaveButton1(object sender, EventArgs e)
        {
            if (UndoButton.IsEnabled)
            {
                Border border = UndoButton.FindChildByType<Border>();
                border.Background = new SolidColorBrush(Color.FromRgb(43, 196, 138));
            }
            else
            {
                Border border = UndoButton.FindChildByType<Border>();
                border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
        }
        private void OnMouseEnterButton2(object sender, EventArgs e)
        {
            Border border = RedoButton.FindChildByType<Border>();
            border.Background = new SolidColorBrush(Color.FromRgb(40, 152, 172));
        }
        private void OnMouseLeaveButton2(object sender, EventArgs e)
        {
            if (RedoButton.IsEnabled)
            {
                Border border = RedoButton.FindChildByType<Border>();
                border.Background = new SolidColorBrush(Color.FromRgb(43, 196, 138));
            }
            else
            {
                Border border = RedoButton.FindChildByType<Border>();
                border.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            recentFilesManager.SaveRecentFiles();
            

        }
        private void OnRecentFileClicked(object sender, RoutedEventArgs e)
        {
            var buttonSend = (RadRibbonButton)sender;
            var recentFile = (RecentFile)buttonSend.DataContext;
            var filePath = recentFile.FilePath;
            if (!isFileSave && listDrewShapes.Count > 0)
            {
                string messageBoxText = "Do you want to save changes?";
                string caption = "Save file";
                MessageBoxButton button = MessageBoxButton.YesNoCancel;
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBoxResult result;

                result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                if (result == MessageBoxResult.Yes)
                {
                    SaveBtn_Click(sender, e);
                }
                else
                {

                    isFileSave = true;
                    OnRecentFileClicked(sender, e);

                }
            }
            else
            {
                string ext = Path.GetExtension(filePath).ToLower().Replace(".", "");
                curFilePath= filePath;
                if (ext == "jpeg" || ext == "png" || ext == "bmp")
                {
                    haveImageOrFill = true;
                    mainPaper.Children.Clear();
                    using (FileStream fileStream = new FileStream(curFilePath, FileMode.Open, FileAccess.Read))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = fileStream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        double width = bitmap.Width;
                        double height = bitmap.Height;
                        mainPaper.Width = width;
                        mainPaper.Height = height;
                        ImageBrush imageBrush = new ImageBrush();
                        imageBrush.ImageSource = bitmap;
                        mainPaper.Background = imageBrush;
                    }
                }
                else
                {
                    try
                    {
                        mainPaper.Background = new SolidColorBrush(Colors.White);
                        mainPaper.Children.Clear();
                        listDrewShapes = ReadObjectListFromFile(filePath);
                        haveImageOrFill = false;
                        foreach (var shape in listDrewShapes)
                        {
                            Debug.WriteLine(shape.Text);
                            UIElement drawshape = shape.Draw(shape.ColorDrew, shape.ThicknessDrew, shape.StrokeDashArray, false, shape.rotateAngle);
                            mainPaper.Children.Add(drawshape);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            ribbon.IsBackstageOpen = false;
        }
    }
}
  

