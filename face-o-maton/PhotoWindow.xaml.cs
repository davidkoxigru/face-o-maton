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
using System.Windows.Shapes;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class PhotoWindow : Window
    {
        public PhotoWindow()
        {
            InitializeComponent();
        }

        private void Photo_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
