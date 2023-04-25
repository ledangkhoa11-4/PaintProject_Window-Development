using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

using System.Windows.Input;
using System.Windows.Media;


namespace PaintProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, IShape> _abilities =new Dictionary<string, IShape>();

        private bool isDrawing = false;
        private Point startPoint;
        private Point endPoint;
        private IShape shape = null;  //Hình đang vẽ
        private UIElement lastDraw = null; //Hình preview cuối cùng (Không vẽ lại tất cả - Improve số 4)
        private List<IShape> listDrewShapes = new List<IShape>(); //Các hình đã vẽ
        public MainWindow()
        {
            InitializeComponent();
        }

        private void startingDrawing(object sender, MouseButtonEventArgs e)
        {
            isDrawing = true;
            Point mouseCoor = e.GetPosition(mainPaper);
            startPoint = mouseCoor;
            shape.UpdateStart(startPoint);
            Debug.WriteLine("Start");
            mainPaper.CaptureMouse();
        }

        private void drawing(object sender, MouseEventArgs e)
        {
            if (!isDrawing)
                return;
            Point mouseCoor = e.GetPosition(mainPaper);
            endPoint= mouseCoor;
            shape.UpdateEnd(endPoint);
            UIElement drawShape = shape.Draw(Colors.Red, 1);
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
            isDrawing = false;
            Debug.WriteLine("Stop");
            mainPaper.ReleaseMouseCapture();
            listDrewShapes.Add((IShape)shape.Clone());
            lastDraw = null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ////// Tự scan chương trình nạp lên các khả năng của mình
            var domain = AppDomain.CurrentDomain;
            var folder = domain.BaseDirectory + "abilities";

            var folderInfo = new DirectoryInfo(folder);
            if (!folderInfo.Exists)
            {
                MessageBox.Show("Abilities folder not found", "Folder not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Debug.WriteLine($"{folderInfo}");
            var dllFiles = folderInfo.GetFiles("*.dll");
            Debug.WriteLine(dllFiles.Length);
            foreach (var dll in dllFiles)
            {
                var assembly = Assembly.LoadFrom(dll.FullName);
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsClass &&
                        typeof(IShape).IsAssignableFrom(type)){
                        var shape = Activator.CreateInstance(type) as IShape;
                        _abilities.Add(shape!.name, shape);
                    }
                }
            }
            shape = _abilities["Line"];
            foreach (var ability in _abilities)
            {
                //2
                
            }
        }
    }
}
