using System.Windows;

namespace MedHelp.Views
{
    public partial class AboutDeveloperWindow : Window
    {
        public AboutDeveloperWindow()
        {
            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}