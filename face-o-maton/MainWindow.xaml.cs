using System;
using System.Collections.Generic;
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
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VideoWindow videoWindow;
        PhotoWindow photoWindow;
        MixFacesWindow mixFacesWindow;

        public MainWindow()
        {
            InitializeComponent();

            videoWindow = new VideoWindow();
            photoWindow = new PhotoWindow();
            mixFacesWindow = new MixFacesWindow();
        }

        private void Video_Button_Click(object sender, RoutedEventArgs e)
        {
            videoWindow.Show();
        }

        private void Photo_Button_Click(object sender, RoutedEventArgs e)
        {
            photoWindow.Show();
        }

        private void Mix_Faces_Button_Click(object sender, RoutedEventArgs e)
        {
            mixFacesWindow.Show();
            mixFacesWindow.Start();
        }
    }
}
