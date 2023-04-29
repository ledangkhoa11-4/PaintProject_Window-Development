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
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Win32;
using Path = System.IO.Path;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;
using Telerik.Windows.Controls.Map;

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
        private Color selectedColor = Colors.Black;
        private int thickness = 1;
        private DoubleCollection stroke = new DoubleCollection();

        private bool isBucketFillMode = false;
        private bool isSelectionMode = false;

        private IShape selectedShape = null;
        private UIElement sampleUI = null;
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
        public MainWindow()
        {
            InitializeComponent();
            var folderInfo = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "cursors");
            bucketCursor = new Cursor($"{folderInfo}\\bucket.cur");
            moveCursor = new Cursor($"{folderInfo}\\move.cur");
        }

        private void startingDrawing(object sender, MouseButtonEventArgs e)
        {
            Point mouseCoor = e.GetPosition(mainPaper);
            Point mouseInScreen = PointToScreen(e.GetPosition(this));
            if (isBucketFillMode)
            {
                Color color = GetColorAtPoint(mouseInScreen);
                string hex = "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
                Debug.WriteLine(hex);
                Debug.WriteLine(mouseCoor.X);
                Debug.WriteLine(mouseCoor.Y);
                haveImageOrFill = true;
                ScanLineFill(mouseCoor, color, selectedColor);
                
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
                IShape element = null;
                // Check if an element was actually clicked
                if (clickedElement != null)
                {
                    if (clickedElement is System.Windows.Shapes.Line)
                    {
                        var line = (System.Windows.Shapes.Line)clickedElement;
                        line.StrokeThickness = thickness;
                        var brush = line.Stroke as SolidColorBrush;
                        element = listDrewShapes.FirstOrDefault(shape =>
                        {
                            if (shape.name == "Line" && shape.Start == new Point(line.X1, line.Y1) && shape.End == new Point(line.X2, line.Y2)) return true;
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
                        element = listDrewShapes.FirstOrDefault(shape =>
                        {
                            if (shape.name == "Rectangle" && shape.Start == new Point(left, top)) return true;
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
                        element = listDrewShapes.FirstOrDefault(shape =>
                        {
                            if (shape.name == "Ellipse" && shape.Start == new Point(left, top)) return true;
                            return false;
                        });
                    }
                    if (element != null)
                    {
                        DashStyle dashStyle = new DashStyle(new double[] { 4, 2 }, 0);
                        var sampleShape = _abilities[element.name];
                        sampleShape.UpdateStart(element.Start);
                        sampleShape.UpdateEnd(element.End);
                        var sampleLine = sampleShape.Draw(Colors.White, 2, new DoubleCollection(new double[] { 4, 2 }));
                        mainPaper.Children.Add(sampleLine);
                        selectedShape = element;
                        selectedShapeColor = selectedShape.ColorDrew;
                        selectedShapeThickness = selectedShape.ThicknessDrew;
                        sampleUI = sampleLine;
                        isFirstSelected = true;
                        originalStart = selectedShape.Start;
                        originalEnd = selectedShape.End;
                        lastDraw = clickedElement;

                    }
                }
                return;
            }

            if (isSelectionMode && selectedShape != null)
            {
                mainPaper.Cursor = moveCursor;
                startEditPoint = mouseCoor;
                Debug.WriteLine("Selected");
                Debug.WriteLine($"{startEditPoint.X} - {startEditPoint.Y}");
                isFirstSelected = false;
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
                UIElement drawShape = shape.Draw(selectedColor, thickness, stroke, isShiftKeyPressed);
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
            if (isSelectionMode == true && selectedShape != null && !isFirstSelected)
            {
                var moveX = mouseCoor.X - startEditPoint.X;
                var moveY = mouseCoor.Y - startEditPoint.Y;

                selectedShape.UpdateStart(new Point(originalStart.X + moveX, originalStart.Y + moveY));
                selectedShape.UpdateEnd(new Point(originalEnd.X + moveX, originalEnd.Y + moveY));
                var newDraw = selectedShape.Draw(selectedShapeColor, selectedShapeThickness, null, isShiftKeyPressed);
                newDraw.MouseUp += stopDrawing;
                if(lastDraw!=null)
                {
                    mainPaper.Children.Remove(lastDraw);
                }
                mainPaper.Children.Add(newDraw);
                lastDraw = newDraw;
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
                isFileSave= false;
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
            weightInfo.Text = thickness.ToString() + "px";
            strokeSelect.Text = "Line";
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
                lastDraw = null;
                isSelectionMode = false;
                mainPaper.Cursor = Cursors.Arrow;
                isFirstSelected = true;
                selectedShape = null;
                mainPaper.Children.Remove(sampleUI);

            }
        }

        private void ColorPickerChanged(object sender, EventArgs e)
        {
            RadColorPicker colorPicker = sender as RadColorPicker;
            selectedColor = colorPicker.SelectedColor;

        }

        private void ChangeWeight(object sender, SelectionChangedEventArgs e)
        {
            int i = listWeight.SelectedIndex;
            if (i == 0) thickness = 1;
            else if (i == 1) thickness = 3;
            else if(i == 2) thickness = 5;
            else if(i == 3) thickness = 8;
            weightInfo.Text = thickness.ToString() + "px";
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
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (haveImageOrFill)
            {
                ExportImageFile(new PngBitmapEncoder());
            }
            else
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
                }
                else
                {
                    isFileSave = false;
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
                openFileDialog.Filter = "Files|*.bin;*.dat;*.dkpq;*.jpg;*.png;*.bmp";
                if (openFileDialog.ShowDialog() == true)
                {
                    string ext = Path.GetExtension(openFileDialog.FileName).ToLower().Replace(".","");
                    if (ext == "jpg" || ext == "png" || ext == "bmp")
                    {
                        mainPaper.Children.Clear();
                        
                        haveImageOrFill = true;
                        BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                        double width = bitmap.Width;
                        double height = bitmap.Height;
                        mainPaper.Width = width;
                        mainPaper.Height = height;
                        ImageBrush imageBrush = new ImageBrush();
                        imageBrush.ImageSource = bitmap;
                        mainPaper.Background = imageBrush;
                    }
                    else
                    {
                        try
                        {
                            mainPaper.Background = null;
                            mainPaper.Children.Clear();
                            listDrewShapes = ReadObjectListFromFile(openFileDialog.FileName);
                            foreach (var shape in listDrewShapes)
                            {
                                UIElement drawshape = shape.Draw(shape.ColorDrew, shape.ThicknessDrew, shape.StrokeDashArray, false);
                                mainPaper.Children.Add(drawshape);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }

                }
            }
        }
        private void ExportImageFile(BitmapEncoder encoder)
        {
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)mainPaper.ActualWidth, (int)mainPaper.ActualHeight, 96d, 96d, PixelFormats.Default);
            rtb.Render(mainPaper);
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = encoder.GetType().ToString().ToLower().Replace("bitmapencoder", "");
            saveFileDialog.FileName = "paint";
            saveFileDialog.OverwritePrompt = true;
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (FileStream fs = File.Create(saveFileDialog.FileName))
                    {
                        encoder.Save(fs);
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


        }

        private void ExportJpgFile(object sender, RoutedEventArgs e)
        {
            ExportImageFile(new JpegBitmapEncoder());
        }

        private void ExportBmpFile(object sender, RoutedEventArgs e)
        {
            ExportImageFile(new BmpBitmapEncoder());
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
            if (newScale >= 0.125)
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
        private System.Windows.Controls.Image selectedImage;
        private Point initialMousePos;
        private Point initialImagePos;

        private void ImportImageButton_Click(object sender, RoutedEventArgs e)
        {

            // Create a OpenFileDialog to allow the user to select an image file
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg;*.bmp)|*.png;*.jpeg;*.jpg;*.bmp|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                // Create a new BitmapImage from the selected file
                var bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));

                // Create a new Image control and set its Source and Stretch properties
                var image = new System.Windows.Controls.Image() { Source = bitmapImage, Stretch = Stretch.Fill };
              
                // Create a new Viewbox and set its Width, Height, and Child properties
                var viewbox = new Viewbox() { Width = 200, Height = 200, Child = image };

                // Add the Viewbox to the canvas
                mainPaper.Children.Add(viewbox);
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
                }
                else
                {
                    isFileSave = true;
                    NewFileBtn_Click(sender: this, e: e);

                }
            }
            else
            {
                mainPaper.Background= null;
                
            }

            }
        }
    }
}
