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
    public partial class FifoProcesosWindow : Window
    {
        Random rnd = new Random();

        public FifoProcesosWindow()
        {
            InitializeComponent();
        }

        public class Proceso
        {
            public string? Nombre { get; set; }
            public int Rafaga { get; set; }
            public int TiempoEspera { get; set; }
            public int TiempoRetorno { get; set; }
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnAleatorio_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int n)) return;

            List<Proceso> lista = new List<Proceso>();
            for (int i = 0; i < n; i++)
                lista.Add(new Proceso { Nombre = "P" + i, Rafaga = rnd.Next(1, 10) });

            CalcularFIFO(lista);
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int n)) return;

            var dialogo = new ManualProcesosDialog(n);
            if (dialogo.ShowDialog() == true)
                CalcularFIFO(dialogo.Procesos);
        }

        private void CalcularFIFO(List<Proceso> procesos)
        {
            int reloj = 0;
            icGantt.Items.Clear();

            foreach (var p in procesos)
            {
                p.TiempoEspera = reloj;
                DibujarBloqueGantt(p.Nombre, p.Rafaga);
                reloj += p.Rafaga;
                p.TiempoRetorno = reloj;
            }

            dgFifo.ItemsSource = procesos;
        }

        private void DibujarBloqueGantt(string nombre, int ancho)
        {
            Border b = new Border
            {
                Width = ancho * 30,
                Height = 60,
                Background = new SolidColorBrush(Color.FromRgb(
                    (byte)rnd.Next(150, 255),
                    (byte)rnd.Next(150, 255),
                    (byte)rnd.Next(150, 255))),
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = nombre,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            icGantt.Items.Add(b);
        }
    }
}
