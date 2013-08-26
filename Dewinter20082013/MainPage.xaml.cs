using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;




namespace Metro_Paint
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        InkManager MyInkManager = new InkManager();
        string DrawingTool;
        double X1, X2, Y1, Y2, StrokeThickness = 1;
        Line NewLine;
        Ellipse NewEllipse;
        Point StartPoint, PreviousContactPoint, CurrentContactPoint;
        Polyline Pencil;
        Rectangle NewRectangle;
        Color BorderColor = Colors.Black;
        uint PenID, TouchID;
        WriteableBitmap wb;
       //right now i haven't added color function by default color has some value for what ever be the color of line 
        //we are drawing on canvas it will show only the default color only.
        //we can add them.
        Color clr = new Color();
        //checks the current pointer released is generated from line or rectangle or ellipse.
        //we don't require for pencil since we have added code in pointer moved only.
        // but for remaining check is necessary.so that actual line is displayed after pointer is released.
        bool itisline =false;
        bool itisrectangle = false;
        bool itisellipse = false;
        //below all are to draw on our writable bitmap.
        double rectX1, rectX2, rectY1, rectY2;
        double elipX1, elipX2, elipY1, elipY2;
        double lineX1, lineY1;
        double lineX2 = 0.0, lineY2 = 0.0;

        public MainPage()
        {
            this.InitializeComponent();
            Image img = new Image();
            img.Source = new BitmapImage(new Uri("ms-appx:///Pictures/2.jpg", UriKind.RelativeOrAbsolute));
            canvas.Children.Insert(0, img);
            canvas.Height = img.ActualHeight;
            canvas.Width = img.ActualWidth;
           
            clr.R = 212;

            LoadImage();
           
            canvas.PointerMoved += canvas_PointerMoved;
            canvas.PointerReleased += canvas_PointerReleased;
            canvas.PointerPressed += canvas_PointerPressed;
            canvas.PointerExited += canvas_PointerExited;
            
            for (int i = 1; i < 21; i++)
            {
                ComboBoxItem Items = new ComboBoxItem();
                Items.Content = i;
                cbStrokeThickness.Items.Add(Items);
            }
            cbStrokeThickness.SelectedIndex = 0;
            
            //var t = typeof(Colors);
            //var ti = t.GetTypeInfo();
            //var dp = ti.DeclaredProperties;

            var colors = typeof(Colors).GetTypeInfo().DeclaredProperties;
            foreach (var item in colors)
            {
                cbBorderColor.Items.Add(item);
               // cbFillColor.Items.Add(item);
            }
           
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        #region Pointer Events

        void canvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 1);
        }

        void  canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if(DrawingTool != "Eraser")
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Cross, 1);
            else
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.UniversalNo, 1);

            switch (DrawingTool)
            {
                    //all events are working on canvas.. we will be collecting points to draw on writablebitmap also.
                case "Line":
                    {
                        NewLine = new Line();
                        NewLine.X1 = e.GetCurrentPoint(canvas).Position.X;
                        NewLine.Y1 = e.GetCurrentPoint(canvas).Position.Y;
                        lineX1 = e.GetCurrentPoint(canvas).Position.X;
                        lineY1 = e.GetCurrentPoint(canvas).Position.Y;
                        NewLine.X2 = NewLine.X1 + 1;
                        NewLine.Y2 = NewLine.Y1 + 1;
                        NewLine.StrokeThickness = StrokeThickness;
                        NewLine.Stroke = new SolidColorBrush(BorderColor);
                        canvas.Children.Add(NewLine);
                       
                    }
                    break;

                case "Pencil":
                    {
                        /* Old Code
                        StartPoint = e.GetCurrentPoint(canvas).Position;
                        Pencil = new Polyline();
                        Pencil.Stroke = new SolidColorBrush(BorderColor);
                        Pencil.StrokeThickness = StrokeThickness;
                        canvas.Children.Add(Pencil);
                        */

                        var MyDrawingAttributes = new InkDrawingAttributes();
                        MyDrawingAttributes.Size = new Size(StrokeThickness, StrokeThickness);
                        MyDrawingAttributes.Color = BorderColor;
                        MyDrawingAttributes.FitToCurve = true;
                        MyInkManager.SetDefaultDrawingAttributes(MyDrawingAttributes);

                        PreviousContactPoint = e.GetCurrentPoint(canvas).Position;
                        //PointerDeviceType pointerDevType = e.Pointer.PointerDeviceType;  to identify the pointer device
                        if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
                        {
                            // Pass the pointer information to the InkManager.
                            MyInkManager.ProcessPointerDown(e.GetCurrentPoint(canvas));
                            PenID = e.GetCurrentPoint(canvas).PointerId;
                            e.Handled = true;
                        }
                    }
                    break;                

                case "Rectagle":
                    {
                        NewRectangle = new Rectangle();
                        X1 = e.GetCurrentPoint(canvas).Position.X;
                        Y1 = e.GetCurrentPoint(canvas).Position.Y;
                        X2 = X1;
                        Y2 = Y1;
                        NewRectangle.Width = X2 - X1;
                        NewRectangle.Height = Y2 - Y1;
                        rectX1 = X1;
                        rectY1 = Y1;
                        NewRectangle.StrokeThickness = StrokeThickness;
                        NewRectangle.Stroke = new SolidColorBrush(BorderColor);
                       // NewRectangle.Fill = new SolidColorBrush(FillColor);
                        canvas.Children.Add(NewRectangle);
                    }
                    break;

                case "Ellipse":
                    {
                        NewEllipse = new Ellipse();
                        X1 = e.GetCurrentPoint(canvas).Position.X;
                        Y1 = e.GetCurrentPoint(canvas).Position.Y;
                        X2 = X1;
                        Y2 = Y1;
                        elipX1 = X1;
                        elipY1 = Y1;
                        NewEllipse.Width = X2 - X1;
                        NewEllipse.Height = Y2 - Y1;
                        NewEllipse.StrokeThickness = StrokeThickness;
                        NewEllipse.Stroke = new SolidColorBrush(BorderColor);
                        //NewEllipse.Fill = new SolidColorBrush(FillColor);
                        canvas.Children.Add(NewEllipse);
                    }
                    break;

                case "Eraser":
                    {
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.UniversalNo, 1);
                        StartPoint = e.GetCurrentPoint(canvas).Position;
                        Pencil = new Polyline();
                        Pencil.Stroke = new SolidColorBrush(Colors.Wheat);
                        Pencil.StrokeThickness = 10;
                        canvas.Children.Add(Pencil);
                    }
                    break;

                default:
                    break;
            }
        }

        void canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (DrawingTool != "Eraser")
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Cross, 1);
            else
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.UniversalNo, 1);

            switch (DrawingTool)
            {
                    //leaving pencil remaining all will work on canvas.pencil will work on both.
                case "Pencil":
                    {
                      

                        if (e.Pointer.PointerId == PenID || e.Pointer.PointerId == TouchID)
                        {
                            // Distance() is an application-defined function that tests
                            // whether the pointer has moved far enough to justify 
                            // drawing a new line.
                            CurrentContactPoint = e.GetCurrentPoint(canvas).Position;
                            X1 = PreviousContactPoint.X;
                            Y1 = PreviousContactPoint.Y;
                            X2 = CurrentContactPoint.X;
                            Y2 = CurrentContactPoint.Y;
                            //drawing a pencil on writablebitmap.
                            wb.DrawLine((int)X1, (int)Y1, (int)X2, (int)Y2, clr);
                            img1.Source = wb;

                            if (Distance(X1, Y1, X2, Y2) > 2.0)
                            { 
                                Line line = new Line()
                                {
                                    X1 = X1,
                                    Y1 = Y1,
                                    X2 = X2,
                                    Y2 = Y2,
                                    StrokeThickness = StrokeThickness,
                                    Stroke = new SolidColorBrush(BorderColor)
                                };

                                PreviousContactPoint = CurrentContactPoint;
                                canvas.Children.Add(line);
                                MyInkManager.ProcessPointerUpdate(e.GetCurrentPoint(canvas));
                            }
                        }
                    }
                    break;

                    
                case "Line":
                    {
                        
                        if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed == true)
                        {
                            itisline = true;
                            NewLine.X2 = e.GetCurrentPoint(canvas).Position.X;
                            NewLine.Y2 = e.GetCurrentPoint(canvas).Position.Y;
                            lineX2 = NewLine.X2;
                            lineY2 = NewLine.Y2;
                            
                        }
                        
                       
                    }
                    break;

                case "Rectagle":
                    {
                        if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed == true)
                        {
                            itisrectangle = true;
                            X2 = e.GetCurrentPoint(canvas).Position.X;
                            Y2 = e.GetCurrentPoint(canvas).Position.Y;
                            rectX2 = X2;
                            rectY2 = Y2;
                            if (X2  > X1 && Y2  > Y1)
                                NewRectangle.Margin = new Thickness(X1, Y1, X2, Y2);
                            else if (X2 < X1 && Y2 <Y1)
                                NewRectangle.Margin = new Thickness(X2, Y2, X1, Y1);
                            else if (X2 > X1 && Y2 < Y1)
                                NewRectangle.Margin = new Thickness(X1, Y2, X2, Y1);
                            else if (X2  < X1 && Y2  > Y1)
                                NewRectangle.Margin = new Thickness(X2, Y1, X1, Y2);
                            NewRectangle.Width = Math.Abs(X2 - X1);
                            NewRectangle.Height = Math.Abs(Y2 - Y1);
                        }
                    }
                    break;

                case "Ellipse":
                    {
                        if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed == true)
                        {
                            itisellipse = true;
                            X2 = e.GetCurrentPoint(canvas).Position.X;
                            Y2 = e.GetCurrentPoint(canvas).Position.Y;
                            elipX2 = X2;
                            elipY2 = Y2;
                            if (X2 > X1 && Y2 > Y1)
                                NewEllipse.Margin = new Thickness(X1, Y1, X2, Y2);
                            else if (X2 < X1 && Y2 < Y1)
                                NewEllipse.Margin = new Thickness(X2, Y2, X1, Y1);
                            else if (X2 > X1 && Y2 < Y1)
                                NewEllipse.Margin = new Thickness(X1, Y2, X2, Y1);
                            else if (X2 < X1 && Y2 > Y1)
                                NewEllipse.Margin = new Thickness(X2, Y1, X1, Y2);
                            NewEllipse.Width = Math.Abs(X2 - X1);
                            NewEllipse.Height = Math.Abs(Y2 - Y1);
                        }
                    }
                    break;

                case "Eraser":
                    {
                        if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed == true)
                        {
                            if (StartPoint != e.GetCurrentPoint(canvas).Position)
                            {
                                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.UniversalNo, 1);
                                Pencil.Points.Add(e.GetCurrentPoint(canvas).Position);
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        //to calculate the minimum distance required to draw a line
        private double Distance(double x1, double y1, double x2, double y2)
        {
            double d = 0;
            d = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
            return d;
        }

        void canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerId == PenID || e.Pointer.PointerId == TouchID)
                MyInkManager.ProcessPointerUp(e.GetCurrentPoint(canvas));

            TouchID = 0;
            PenID = 0;
            e.Handled = true;
            Pencil = null;
            NewLine = null;
            NewRectangle = null;
            NewEllipse = null;
            

            //to draw line on our writable bitmap not on canvas.
            if (itisline)
            {
                wb.DrawLineDDA((int)lineX1, (int)lineY1, (int)lineX2, (int)lineY2, clr);
            }
            //to draw line on our actual rectangle.
            if (itisrectangle)
            {
                if(rectX2>rectX1 && rectY2>rectY1)
                wb.DrawRectangle((int)rectX1,(int) rectY1,(int) rectX2,(int) rectY2, clr);
                else if(rectX2<rectX1 && rectY2<rectY1)
                    wb.DrawRectangle((int)rectX2, (int)rectY2, (int)rectX1, (int)rectY1, clr);
                else if(rectX2>rectX1 && rectY2<rectY1)
                    wb.DrawRectangle((int)rectX1, (int)rectY2, (int)rectX2, (int)rectY1, clr);
                else if(rectX2<rectX1 && rectY2>rectY1)
                    wb.DrawRectangle((int)rectX2, (int)rectY1, (int)rectX1, (int)rectY2, clr);
            }
            // to draw a ellipse
            if (itisellipse)
            {
                if(elipX2>elipX1 && elipY2>elipY1)
                wb.DrawEllipse((int)elipX1,(int) elipY1, (int)elipX2,(int) elipY2, clr);
                else if(elipX2<elipX1 && elipY2<elipY1)
                    wb.DrawEllipse((int)elipX2, (int)elipY2, (int)elipX1, (int)elipY1, clr);
                else if (elipX2 > elipX1 && elipY2 < elipY1)
                    wb.DrawEllipse((int)elipX1, (int)elipY2, (int)elipX2, (int)elipY1, clr);
                else if (elipX2 < elipX1 && elipY2 > elipY1)
                    wb.DrawEllipse((int)elipX2, (int)elipY1, (int)elipX1, (int)elipY2, clr);
  
            }
        }

        #endregion

        #region Drawing Tools Click Events

        private void btnPencil_Click(object sender, RoutedEventArgs e)
        {
            DrawingTool = "Pencil";
        }

        private void btnLine_Click(object sender, RoutedEventArgs e)
        {
            DrawingTool = "Line";
        }

        private void btnEllipse_Click(object sender, RoutedEventArgs e)
        {
            DrawingTool = "Ellipse";
        }

        private void btnRectagle_Click(object sender, RoutedEventArgs e)
        {
            DrawingTool = "Rectagle";
        }


        #endregion

      

        #region Other events
        //thickness on canvas
        private void cbStrokeThickness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StrokeThickness = Convert.ToDouble(cbStrokeThickness.SelectedIndex + 1);
        }

   
        //to change the border color
        private void cbBorderColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbBorderColor.SelectedIndex != -1)
            {
                var pi = cbBorderColor.SelectedItem as PropertyInfo;
                BorderColor = (Color)pi.GetValue(null);
            }
        }

       

       
      

    //to save the image i will add code.
        private  void btnSaveAsImage_Click(object sender, RoutedEventArgs e)
        {
           
        }

        #endregion

   


        //this method is to load image on the writable bitmap it is been called in main.
        async void LoadImage()
        {

            // Open the file
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Pictures/2.jpg"));

            using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                // We have to load the file in a BitmapImage first, so we can check the width and height..
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(fileStream);
                // Load the picture in a WriteableBitmap
                 wb = new WriteableBitmap(bmp.PixelWidth, bmp.PixelHeight);
                wb.SetSource(await file.OpenAsync(Windows.Storage.FileAccessMode.Read));
                img1.Source = wb;

            }
        }
    



        
    }
}