using PdfSharp.Drawing;
using PdfSharp.Pdf;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DriverScanner
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _textLoad;
        public string TextLoad { get => _textLoad; set { _textLoad = value; NotifyPropertyChanged(); } }
        private int _brd;
        public int Brd { get => _brd; set { _brd = value; NotifyPropertyChanged(); } }
        public string Counter { get => $"Осталось попыток {_counter}"; }
        private bool _loaded = false;
        private MessageWindow Mess;
        private System.Timers.Timer _timer;
        private bool _mess;
        private ScanCore scanner;
        private DateTime _last; // последнее нажате на кнопку
        private string _supprtPhone;

        public List<string> _scans;
        private bool _hardButton;
        private int _counter;
        private int _norm;
        private Sender _sender;
        private ButtonChecker _checker;
        private readonly KernelConnectorWeb _kernel;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            Logger.Log("***** Start *****");
            ImageBrush myBrush = new ImageBrush
            {
                ImageSource =
                new BitmapImage(new Uri($"file:///{Environment.CurrentDirectory}/main_img.jpg", UriKind.Absolute))
            };
            this.Background = myBrush;
            banner.Foreground = new SolidColorBrush(Colors.Red);
            TextLoad = GetTextLoad();
            DataContext = this;    
            _timer = new System.Timers.Timer { AutoReset = false, Enabled = false, Interval = 5000 };
            _timer.Elapsed += Timer_Elapsed;
            _supprtPhone = ConfigurationManager.AppSettings["SupportPhone"];
            var norm = ConfigurationManager.AppSettings["Count"];
            if(!int.TryParse(norm, out _norm))
            {
                _norm = 5;
            }
            _sender = new Sender();
            _kernel = new KernelConnectorWeb();
            _checker = new ButtonChecker(_kernel);
            _checker.Click += Checker_Click;
            _last = DateTime.Now;
        }

        private void Checker_Click(object sender, bool e)
        {
            try
            {
                if (e && !_hardButton)
                {
                    //Show("Кнопка нажата");
                    Dispatcher.Invoke(() =>
                    {
                        Brd = 20;
                    });
                    _hardButton = true;
                    return;
                }
                if (!e && _hardButton)
                {
                    if ((DateTime.Now - _last).TotalSeconds < 2) return; // время между нажатиями не меньше 2 сек. Иначе игнор.
                    _last = DateTime.Now;
                    Dispatcher.Invoke(() =>
                    {
                        Brd = 0;
                        Button_Click(this, null);
                    });
                    //Show("Кнопка отпущена");
                    _hardButton = false;

                }
            }catch(Exception ex)
            {
                Show(ex.Message);
                Logger.Error($"Ошибка при чтении кнопки: {ex.Message}");
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string GetTextLoad()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Чтобы сдать документы сделайте следующее:");
            sb.AppendLine("1. Разделите все листы, снимите все посторонние предементы - скобки, скрепки и т.п.");
            sb.AppendLine("2. Нажмите кнопку и вставляйте листы по одному.");
            sb.AppendLine("");
            sb.AppendLine($"Если что-то пошло не так, позвоните по телефону {_supprtPhone}");
            return sb.ToString();
        }
        
        /// <summary>
        /// Таймер закрытия окна сообщений
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CloseShow();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_loaded)
                {
                    Logger.Log("Остановка сканирования по кнопке");
                    _loaded = false;
                    scanner.StopLoopScan();
                    _scans = new List<string>();
                    list.Visibility = Visibility.Hidden;
                    mainbtn.Content = "Загрузить документы";
                    TextLoad = GetTextLoad();
                    coun.Visibility = Visibility.Hidden;
                    return;
                }
                Logger.Log("Запуск сканирования");
                ClearScanFolder();
                _loaded = true;
                _counter = _norm;
                TextLoad = "Вставляйте документы в сканер по одному. Нажмите кнопку, чтобы завершить процесс";
                coun.Visibility = Visibility.Visible;
                NotifyPropertyChanged("Counter");
                banner.Foreground = new SolidColorBrush(Colors.Red);
                _scans = new List<string>();
                Task.Run(() => Scann());
                mainbtn.Content = "Сохранить сканирование";
            }
            catch(Exception ex)
            {
                Logger.Error(ex.Message);
            }
             
        }

        private void Scann()
        {                       
            scanner = new ScanCore(_kernel);
            scanner.ScanEvent += Scanner_ScanEvent;
            scanner.NewScan += Scanner_NewScan;
            scanner.GoAuto();           
        }

        private void Scanner_NewScan(object sender, string image)
        {
            if (_loaded)
            {
                var dir = Environment.CurrentDirectory + "\\";
                _scans.Add(dir + image);
                ShowImages();
                _counter = _norm;
                NotifyPropertyChanged("Counter");
            }
        }

        private void ShowImages()
        {
            Dispatcher.Invoke(() =>
            {
                list.Items.Clear();
                list.Visibility = Visibility.Visible;
                var dir = Environment.CurrentDirectory;
                foreach (var image in _scans)
                {
                    list.Items.Add(image);                    
                }
            });
        }

        private void EndScann()
        {
            scanner.ScanEvent -= Scanner_ScanEvent;
            scanner = null;
            SavePdf();            
        }

        /// <summary>
        /// Вытащить все файлы с картинками из папки сканирования и собрать их в один ПДФ-файл
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void SavePdf()
        {
            string PathToFolder = @"scans\";
            string[] allfiles = Directory.GetFiles(PathToFolder, "*.jpg");
           
            if (allfiles.Count() > 0)
            {
                var dir = PathToFolder + @"\" + DateTime.Today.ToString("dd.MM.yyyy");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var file = dir + @"\filename_" + DateTime.Now.ToString("HH-mm-ss") + ".pdf";

                var document = new PdfDocument();
                document.Info.Title = "scan" + DateTime.Now;
                foreach (var fl in allfiles)
                {
                    // Create an empty page
                    var page = document.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    // Get an XGraphics object for drawing
                    var gfx = XGraphics.FromPdfPage(page);
                    var memoryStream = new MemoryStream();
                    var image = new Bitmap(fl);
                    image.Save(memoryStream, ImageFormat.Jpeg);
                    var img = XImage.FromStream(memoryStream);
                    gfx.DrawImage(img, 0, 0, gfx.PdfPage.Width, gfx.PdfPage.Height);
                }
                // Save the document...
                document.Save(file);
                _sender.AddTask(file);
                document.Close();
                Logger.Log($"Cохранен сканированный документ {file}");                               
            }
        }

        private void ClearScanFolder()
        {
            try
            {
                string PathToFolder = @"scans\";
                string[] allfiles = Directory.GetFiles(PathToFolder, "*.jpg");
                foreach (string filename in allfiles)
                {
                    File.Delete(filename);
                }
                Logger.Log("Папка со сканами очищена от черновиков сканов");
            }
            catch(Exception e)
            {
                Logger.Error($"Ошибка при удалении файла: {e.Message}");
            }
        }

        private void ExitFromScan()
        {
            scanner.StopLoopScan();
            EndScann();
            _loaded = false;
            _scans = new List<string>();
            Dispatcher.Invoke(() =>
            {
                list.Visibility = Visibility.Hidden;
                coun.Visibility = Visibility.Hidden;
                mainbtn.Content = "Загрузить документы";
                TextLoad = GetTextLoad();
            });
        }

        private void ContinueScan()
        {
            Show("Продолжаем сканирование");
            scanner.ContinueScann();
        }

        private void Scanner_ScanEvent(object sender, int e)
        {
            switch(e)
            {
                case 0:
                    Show("Подготовка сканера", false);
                    break;
                case 1:
                    CloseShow();
                    break;
                case 2:
                    //Show("Документы не были загружены. Если произошло замятие, обратитесь к сотруднику по указанному телефону");
                    Logger.Log("Выход из сканирования через ошибку");
                    if(--_counter == 0)
                    {
                        ExitFromScan();
                        break;
                    }
                    ContinueScan();
                    NotifyPropertyChanged("Counter");
                    break;
                case 4:
                    if (!_mess)
                    {
                        Show("Документ загружен.");                       
                    }
                    break;
                case 3:
                    //_loaded = false;
                    //Show("Документы успешно загружены.");
                    break;
                case 5:
                case 6:
                    Show($"Процесс завершился с кодом {e}");
                    break;
                case 10:
                    Show("Включение сканера. Похоже, что он не был включен.", false);
                    break;
                case 11:
                    //_loaded = false;
                    break;
                case 12:
                    Show("Сканер включен, начните сканировать еще раз.");
                    _loaded = false;
                    Dispatcher.Invoke(() =>
                    {
                        _scans = new List<string>();
                        list.Visibility = Visibility.Hidden;
                        mainbtn.Content = "Загрузить документы";
                        TextLoad = GetTextLoad();
                        coun.Visibility = Visibility.Hidden;
                    });

                    break;
                case 13:                    
                    Show("Сканирование завершено");
                    EndScann();
                    break;
            }
        }

        private void CloseShow()
        {
            Dispatcher.Invoke(() =>
            {
                Mess.Close();
            });
            _mess = false;
        }

        private void Show(string v, bool auto = true)
        {
            if (_mess)
            {
                CloseShow();
                Thread.Sleep(100);
                _timer.Stop();
            }
            Dispatcher.Invoke(() =>
            {
                Mess = new MessageWindow
                {
                    MessageText = v
                };
                Mess.Show();
            });
            if (auto)
            {
                _timer.Start();
            }
            _mess = true;
        }      

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {          
            var t = _kernel.PowerSwitch();
            if(t != "")
            {
                Show(t);
                return;
            }
            Show("Кнопка питания была нажата");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (!_kernel.PaperSwitch(false, out var m))
            {
                Show("Ошибка связи с Кернел-чипом");
            }
            else
            {
                //Show(m);        
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_mess)
            {
                CloseShow();
                Thread.Sleep(100);
                _timer.Stop();
            }
        }

        private void Window_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var url = "http://localhost:5000/CheckButton";
            url.PostToUrl("");
        }
    }
}
