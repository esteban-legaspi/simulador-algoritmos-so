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
    public partial class ProcesosWindow : Window
    {
        Random rnd = new Random();
        private List<Proceso> _procesosActuales = new();

        // Colores fijos por proceso para que tabla y Gantt coincidan
        private static readonly List<Color> Colores = new()
        {
            Colors.Red, Colors.OrangeRed, Colors.Gold, Colors.Yellow,
            Colors.YellowGreen, Colors.Green, Colors.Teal, Colors.CornflowerBlue,
            Colors.Blue, Colors.MediumPurple, Colors.Brown, Colors.Orange,
            Colors.Pink
        };

        public ProcesosWindow()
        {
            InitializeComponent();
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnAleatorio_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int n)) return;

            List<Proceso> lista = new();
            for (int i = 0; i < n; i++)
                lista.Add(new Proceso
                {
                    Nombre = ((char)('A' + i)).ToString(),
                    Llegada = rnd.Next(0, 10),
                    t = rnd.Next(1, 10),
                    Prioridad = rnd.Next(1, 6)
                });

            // Ordenar por llegada
            lista = lista.OrderBy(p => p.Llegada).ToList();
            Calcular(lista);
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int n)) return;

            var dialogo = new ManualProcesosDialog(n);
            if (dialogo.ShowDialog() == true)
                Calcular(dialogo.Procesos!);
        }

        private void Calcular(List<Proceso> procesos)
        {
            _procesosActuales = procesos.Select(p => new Proceso
            {
                Nombre = p.Nombre,
                Llegada = p.Llegada,
                t = p.t,
                Prioridad = p.Prioridad
            }).ToList();

            string algoritmo = (cmbAlgoritmo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "FIFO";

            int reloj = 0;
            List<Proceso> resultado = new();
            List<Proceso> pendientes = new(procesos);

            for (int i = 0; i < procesos.Count; i++)
            {
                List<Proceso> disponibles;

                if (algoritmo == "SJF")
                    disponibles = pendientes.Where(p => p.Llegada <= reloj).OrderBy(p => p.t).ToList();
                else
                    disponibles = pendientes.OrderBy(p => p.Llegada).ToList();

                // Si no hay procesos disponibles avanza el reloj al siguiente que llega
                if (disponibles.Count == 0)
                {
                    reloj = pendientes.Min(p => p.Llegada);
                    disponibles = pendientes.Where(p => p.Llegada <= reloj).OrderBy(p => p.t).ToList();
                }

                var siguiente = disponibles.First();
                pendientes.Remove(siguiente);

                siguiente.Color = new SolidColorBrush(Colores[resultado.Count % Colores.Count]);

                if (reloj < siguiente.Llegada)
                    reloj = siguiente.Llegada;

                siguiente.Inicio = reloj;
                reloj += siguiente.t;
                siguiente.Fin = reloj;
                siguiente.T = siguiente.Fin - siguiente.Llegada;
                siguiente.E = siguiente.T - siguiente.t;
                siguiente.P = resultado.Count + 1;

                resultado.Add(siguiente);
            }

            dgProcesos.ItemsSource = null;
            dgProcesos.ItemsSource = resultado;

            dgProcesos.LoadingRow += (s, e) =>
            {
                if (e.Row.DataContext is Proceso p)
                    e.Row.Background = p.Color;
            };

            var ganttOrdenado = resultado.OrderBy(p => p.Llegada).ThenBy(p => p.Inicio).ToList();
            DibujarGantt(ganttOrdenado);
        }

        private void BtnRecalcular_Click(object sender, RoutedEventArgs e)
        {
            if (_procesosActuales.Count == 0)
            {
                MessageBox.Show("Primero ingresa procesos.");
                return;
            }

            Calcular(_procesosActuales);
        }


        private void DibujarGantt(List<Proceso> procesos)
        {
            int tiempoTotal = procesos.Max(p => p.Fin);
            int cellSize = 30;

            var grid = new Grid();

            // Columnas: una por unidad de tiempo + 1 para nombres
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            for (int t = 0; t <= tiempoTotal; t++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellSize) });

            // Filas: una por proceso + 1 para encabezado de tiempo
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) });
            for (int i = 0; i < procesos.Count; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });

            // Encabezado de tiempo
            for (int t = 0; t <= tiempoTotal; t++)
            {
                var lbl = new TextBlock
                {
                    Text = t.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11
                };
                Grid.SetRow(lbl, 0);
                Grid.SetColumn(lbl, t + 1);
                grid.Children.Add(lbl);
            }

            // Filas de procesos
            for (int i = 0; i < procesos.Count; i++)
            {
                var p = procesos[i];

                // Nombre del proceso
                var nombre = new TextBlock
                {
                    Text = p.Nombre,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Foreground = p.Color
                };
                Grid.SetRow(nombre, i + 1);
                Grid.SetColumn(nombre, 0);
                grid.Children.Add(nombre);

                // Celdas de tiempo
                for (int t = 0; t < tiempoTotal; t++)
                {
                    var border = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0.5)
                    };

                    if (t >= p.Inicio && t < p.Fin)
                        border.Background = p.Color;                  // ejecutando
                    else if (t >= p.Llegada && t < p.Inicio)
                        border.Background = Brushes.Black;            // esperando
                    else
                        border.Background = Brushes.White;            // no ha llegado o ya terminó

                    Grid.SetRow(border, i + 1);
                    Grid.SetColumn(border, t + 1);
                    grid.Children.Add(border);
                }
            }

            icGantt.Content = grid;
        }
    }
}