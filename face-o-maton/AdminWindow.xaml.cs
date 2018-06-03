using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private Action<int> _callback;
        private int _nbMaxColorPictures;
        private int _nbColorPictures;

        public AdminWindow(int nbMaxColorPictures, Action<int> callback)
        {
            InitializeComponent();

#if !DEBUG
            Topmost = true;
#endif
            _nbMaxColorPictures  = nbMaxColorPictures;
            _callback = callback;
        }

        public void Open(int nbColorPictures)
        {
            // reset counter 
            _nbColorPictures = nbColorPictures;
            DisplayNbColorPictures();
            Show();
        }
        
        private void Max_Button_Click(object sender, RoutedEventArgs e)
        {
            _nbColorPictures = _nbMaxColorPictures;
            DisplayNbColorPictures();
        }

        private void Plus_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_nbColorPictures < _nbMaxColorPictures)
            {
                _nbColorPictures++;
            }
            DisplayNbColorPictures();
        }

        private void Minus_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_nbColorPictures > 0)
            {
                _nbColorPictures--;
            }
            DisplayNbColorPictures();
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            _callback(_nbColorPictures);
        }

        private void DisplayNbColorPictures()
        {
            NbColorPicturesTextBox.Text = _nbColorPictures.ToString();
        }
    }
}
