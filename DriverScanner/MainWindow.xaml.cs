using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DriverScanner
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _textLoad;
        public string TextLoad { get => _textLoad; set { _textLoad = value; NotifyPropertyChanged(); } }
        public string Counter { get => $"Осталось попыток {_counter}"; }
        private bool _loaded = false;
        private MessageWindow Mess;
        private System.Timers.Timer _timer;
        private bool _mess;
        private ScanCore scanner;

        public List<string> _scans;
        private Button _button;
        private int _counter;
        private int _norm;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
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
            _timer.Elapsed += _timer_Elapsed;
            var norm = ConfigurationManager.AppSettings["Count"];
            if(!int.TryParse(norm, out _norm))
            {
                _norm = 5;
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
            sb.AppendLine("Если что-то пошло не так, позвоните по телефону 112");
            return sb.ToString();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CloseShow();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _button = sender as Button;
            if (_loaded)
            {
                _loaded = false;
                scanner.StopLoopScan();
                EndScann();
                SaveAll();
                _scans = new List<string>();
                list.Visibility = Visibility.Hidden;
                _button.Content = "Загрузить документы";                  
                TextLoad = GetTextLoad();
                coun.Visibility = Visibility.Hidden;
                return;
            }
            _loaded = true;
            _counter = _norm;
            TextLoad = "Вставляйте документы в сканер по одному. Нажмите кнопку, чтобы завершить процесс";
            coun.Visibility = Visibility.Visible;
            NotifyPropertyChanged("Counter");
            banner.Foreground = new SolidColorBrush(Colors.Red);
            _scans = new List<string>();
            Task.Run(() => Scann());
            _button.Content = "Сохранить сканирование";
        }

        private void SaveAll()
        {
            //
        }

        private void Scann()
        {                       
            scanner = new ScanCore();
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
        }

        private void ExitFromScan()
        {
            scanner.StopLoopScan();
            EndScann();
            _loaded = false;
            SaveAll();
            _scans = new List<string>();
            Dispatcher.Invoke(() =>
            {
                list.Visibility = Visibility.Hidden;
                coun.Visibility = Visibility.Hidden;
                _button.Content = "Загрузить документы";
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
                        _button.Content = "Загрузить документы";
                    });
                    EndScann();
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
            var t = Kernel.PowerSwitch();
            if(t != "")
            {
                Show(t);
                return;
            }
            Show("Кнопка питания была нажата");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (!Kernel.PaperSwitch(false, out var m))
            {
                Show("Ошибка связи с Кернел-чипом");
            }
            else
            {
                //Show(m);        
            }
        }
    }
}
