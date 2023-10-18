using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DriverScanner
{
    /// <summary>
    /// Логика взаимодействия для MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window, INotifyPropertyChanged
    {
        public MessageWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private string _t;

        public string MessageText 
        {
            get => _t;
            
            set
            {
              _t = value;
              NotifyPropertyChanged();                
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
