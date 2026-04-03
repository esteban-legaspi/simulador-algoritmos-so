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
    public partial class SjfProcesosWindow : Window
    {
        Random rnd = new Random();

        public SjfProcesosWindow()
        {
            InitializeComponent();
        }

        public class Proceso
        {
            public string? Nombre { get; set; }
            public int Rafaga { get; set; }
            public int OrdenEjecucion { get; set; }
            public int TiempoEspera { get; set; }
            public int TiempoRetorno { get; set; }
        }

        private void BtnAleatorio_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int n)) return;

            List<Proceso> lista = new List<Proceso>();
            for (int i = 0; i < n; i++)
                lista.Add(new Proceso { Nombre = "P" + i, Rafaga = rnd.Next(1, 10) });

            CalcularSJF(lista);
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            // Se implementa después con ManualProcesosDialog
        }

        private void CalcularSJF(List<Proceso> procesos)
        {
            var ordenados = procesos.OrderBy(p => p.Rafaga).ToList();
            int reloj = 0;
            int orden = 1;
            icGantt.Items.Clear();

            foreach (var p in ordenados)
            {
                p.OrdenEjecucion = orden++;
                p.TiempoEspera = reloj;
                DibujarBloqueGantt(p.Nombre, p.Rafaga);
                reloj += p.Rafaga;
                p.TiempoRetorno = reloj;
            }

            dgSjf.ItemsSource = ordenados;
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
