using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace simulador_algoritmos_so
{
    public partial class ManualProcesosDialog : Window
    {
        public List<FifoProcesosWindow.Proceso> Procesos { get; private set; } = new();

        private int _cantidad;

        public ManualProcesosDialog(int cantidad)
        {
            InitializeComponent();
            _cantidad = cantidad;
            CargarFilasVacias();
        }

        private void CargarFilasVacias()
        {
            var lista = new List<FifoProcesosWindow.Proceso>();
            for (int i = 0; i < _cantidad; i++)
                lista.Add(new FifoProcesosWindow.Proceso { Nombre = "P" + i, Rafaga = 1 });

            dgProcesos.ItemsSource = lista;
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            // Forzar commit de la celda activa
            dgProcesos.CommitEdit(DataGridEditingUnit.Row, true);

            var lista = dgProcesos.ItemsSource as List<FifoProcesosWindow.Proceso>;

            if (lista == null || lista.Any(p => p.Rafaga <= 0))
            {
                MessageBox.Show("Todos los procesos deben tener una ráfaga mayor a 0.");
                return;
            }

            Procesos = lista;
            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
