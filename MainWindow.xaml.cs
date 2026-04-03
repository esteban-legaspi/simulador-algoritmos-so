using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace simulador_algoritmos_so
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnFifoProcesos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new FifoProcesosWindow();
            ventana.Owner = this;
            this.Hide();
            ventana.ShowDialog();
            this.Show();
        }

        private void BtnSjf_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new SjfProcesosWindow();
            ventana.Owner = this;
            this.Hide();
            ventana.ShowDialog();
            this.Show();
        }

        private void BtnFifoPaginas_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new FifoPaginasWindow();
            ventana.Owner = this;
            this.Hide();
            ventana.ShowDialog();
            this.Show();
        }
    }
}