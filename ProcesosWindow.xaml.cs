using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace simulador_algoritmos_so
{
    public partial class ProcesosWindow : Window
    {
        Random rnd = new Random();
        private List<Proceso> _procesosActuales = new();

        private static readonly List<Color> Colores = new()
        {
            Colors.Red, Colors.OrangeRed, Colors.Gold, Colors.Yellow,
            Colors.YellowGreen, Colors.Green, Colors.Teal, Colors.CornflowerBlue,
            Colors.Blue, Colors.MediumPurple, Colors.Brown, Colors.Orange, Colors.Pink
        };

        public ProcesosWindow() => InitializeComponent();

        private void BtnRegresar_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnAleatorio_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int n)) return;

            var lista = new List<Proceso>();
            for (int i = 0; i < n; i++)
                lista.Add(new Proceso
                {
                    Nombre = ((char)('A' + i)).ToString(),
                    Llegada = rnd.Next(0, 10),
                    t = rnd.Next(1, 10),
                    Prioridad = rnd.Next(1, 6)
                });

            Calcular(lista.OrderBy(p => p.Llegada).ToList());
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtCantidad.Text, out int n)) return;
            var dialogo = new ManualProcesosDialog(n);
            if (dialogo.ShowDialog() == true) Calcular(dialogo.Procesos!);
        }

        private void BtnRecalcular_Click(object sender, RoutedEventArgs e)
        {
            if (_procesosActuales.Count == 0) { MessageBox.Show("Primero ingresa procesos."); return; }
            Calcular(_procesosActuales);
        }

        // ─── Punto de entrada principal ──────────────────────────────────────────

        private void Calcular(List<Proceso> procesos)
        {
            // Guardar copia original
            _procesosActuales = procesos.Select(p => new Proceso
            {
                Nombre = p.Nombre,
                Llegada = p.Llegada,
                t = p.t,
                Prioridad = p.Prioridad
            }).ToList();

            string algoritmo = (cmbAlgoritmo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "FIFO";
            int.TryParse(txtQuantum.Text, out int quantum);
            if (quantum <= 0) quantum = 2;

            // Copias de trabajo (Inicio = -1 para detectar primer arranque)
            var procs = procesos.Select(p => new Proceso
            {
                Nombre = p.Nombre,
                Llegada = p.Llegada,
                t = p.t,
                Prioridad = p.Prioridad,
                tRestante = p.t,
                Inicio = -1
            }).ToList();

            var resultado = new List<Proceso>();
            var segs = new List<(Proceso p, int ini, int fin)>();

            switch (algoritmo)
            {
                case "FIFO": EjecutarNoExpulsivo(procs, resultado, segs, "FIFO"); break;
                case "Round Robin": EjecutarRoundRobin(procs, resultado, segs, quantum); break;
                case "Prioridad": EjecutarNoExpulsivo(procs, resultado, segs, "Prioridad"); break;
                case "SRTF": EjecutarSRTF(procs, resultado, segs); break;
                case "SJF":
                    EjecutarNoExpulsivo(procs, resultado, segs, "SJF");
                    break;
            }

            // Asignar colores y posición de finalización
            for (int i = 0; i < resultado.Count; i++)
            {
                resultado[i].Color = new SolidColorBrush(Colores[i % Colores.Count]);
                resultado[i].P = resultado[i].t > 0 ? Math.Round((double)resultado[i].T / resultado[i].t, 2) : 1.0;
            }

            dgProcesos.ItemsSource = null;
            dgProcesos.ItemsSource = resultado;
            dgProcesos.LoadingRow += (s, e) =>
            {
                if (e.Row.DataContext is Proceso p) e.Row.Background = p.Color;
            };

            DibujarGantt(resultado.OrderBy(p => p.Llegada).ThenBy(p => p.Inicio).ToList(), segs);
            MostrarEstadisticas(resultado);
        }

        // ─── Algoritmos ──────────────────────────────────────────────────────────

        // Recibe procesos, lista resultado, segmentos gantt y el tipo de algoritmo
        private void EjecutarNoExpulsivo(List<Proceso> procs, List<Proceso> resultado,
            List<(Proceso p, int ini, int fin)> segs, string tipo)
        {
            // Copia de procesos pendientes por ejecutar
            var pendientes = new List<Proceso>(procs);
            // Reloj del simulador
            int reloj = 0;

            // Mientras haya procesos sin ejecutar
            while (pendientes.Count > 0)
            {
                // Filtrar los que ya llegaron al tiempo actual
                var disponibles = pendientes.Where(p => p.Llegada <= reloj).ToList();
                // Si ninguno ha llegado, avanzar el reloj al siguiente que llega
                if (!disponibles.Any()) { reloj = pendientes.Min(p => p.Llegada); continue; }

                // Elegir el siguiente según el algoritmo
                var siguiente = tipo switch
                {
                    "SJF" => disponibles.OrderBy(p => p.t).ThenBy(p => p.Llegada).First(),        // menor burst
                    "Prioridad" => disponibles.OrderBy(p => p.Prioridad).ThenBy(p => p.Llegada).First(),// menor número = mayor prioridad
                    _ => disponibles.OrderBy(p => p.Llegada).First()                          // FIFO: el que llegó primero
                };

                pendientes.Remove(siguiente);                          // ya no está pendiente
                if (reloj < siguiente.Llegada) reloj = siguiente.Llegada; // si llegó después, avanzar reloj

                siguiente.Inicio = reloj;                              // empieza ahora
                reloj += siguiente.t;                                  // ocupa la CPU su burst completo
                siguiente.Fin = reloj;                                 // termina aquí
                siguiente.T = siguiente.Fin - siguiente.Llegada;       // turnaround = fin - llegada
                siguiente.E = siguiente.T - siguiente.t;               // espera = turnaround - burst

                segs.Add((siguiente, siguiente.Inicio, siguiente.Fin)); // registrar en Gantt
                resultado.Add(siguiente);                               // agregar a resultados
            }
        }

        // Recibe la lista de procesos, lista resultado, segmentos gantt y el quantum
        private void EjecutarRoundRobin(List<Proceso> procs, List<Proceso> resultado,
            List<(Proceso p, int ini, int fin)> segs, int quantum)
        {
            // Copia de procesos ordenados por llegada
            var restantes = new List<Proceso>(procs.OrderBy(p => p.Llegada));
            // Cola de listos (procesos que ya llegaron y esperan CPU)
            var cola = new Queue<Proceso>();
            // Reloj del simulador
            int t = 0;

            // Mientras haya procesos sin terminar (en espera o en cola)
            while (restantes.Count > 0 || cola.Count > 0)
            {
                // Si la cola está vacía no hay nadie listo, saltar al siguiente que llega
                if (cola.Count == 0)
                {
                    t = restantes.Min(p => p.Llegada);        // avanzar reloj a esa llegada
                    EnqueueArrived(restantes, cola, t);        // encolar los que ya llegaron
                }

                var cur = cola.Dequeue();                      // sacar el primero de la cola
                if (cur.Inicio < 0) cur.Inicio = t;            // guardar cuándo corrió por primera vez

                int run = Math.Min(quantum, cur.tRestante);    // correr quantum o lo que le queda
                segs.Add((cur, t, t + run));                   // registrar segmento en el Gantt
                t += run;                                      // avanzar el reloj
                cur.tRestante -= run;                          // reducir tiempo restante

                // Encolar procesos que llegaron DURANTE este quantum (antes de reinsertar el actual)
                EnqueueArrived(restantes, cola, t);

                if (cur.tRestante > 0)
                    cola.Enqueue(cur);                         // no terminó, regresa al final de la cola
                else
                {
                    cur.Fin = t;                               // terminó: registrar fin
                    cur.T = cur.Fin - cur.Llegada;             // turnaround = fin - llegada
                    cur.E = cur.T - cur.t;                     // espera = turnaround - burst
                    resultado.Add(cur);                        // agregar a resultados finales
                }
            }
        }

        private static void EnqueueArrived(List<Proceso> restantes, Queue<Proceso> cola, int t)
        {
            foreach (var p in restantes.Where(p => p.Llegada <= t).OrderBy(p => p.Llegada))
                cola.Enqueue(p);
            restantes.RemoveAll(p => p.Llegada <= t);
        }

        // Recibe procesos, lista resultado y segmentos gantt
        private void EjecutarSRTF(List<Proceso> procs, List<Proceso> resultado,
            List<(Proceso p, int ini, int fin)> segs)
        {
            // Reloj del simulador
            int t = 0;
            // Proceso que corrió en el tick anterior (para detectar cambios)
            Proceso? prev = null;
            // Inicio del segmento actual en el Gantt
            int segIni = 0;

            // Mientras haya procesos con tiempo restante
            while (procs.Any(p => p.tRestante > 0))
            {
                // Filtrar los que ya llegaron y aún tienen tiempo, ordenar por menor tiempo restante
                var disponibles = procs
                    .Where(p => p.Llegada <= t && p.tRestante > 0)
                    .OrderBy(p => p.tRestante).ThenBy(p => p.Llegada).ToList();

                // Si ninguno ha llegado aún, avanzar un tick
                if (!disponibles.Any()) { t++; continue; }

                // Tomar el de menor tiempo restante
                var cur = disponibles.First();
                // Guardar cuándo corrió por primera vez
                if (cur.Inicio < 0) cur.Inicio = t;

                // Si cambió el proceso que está corriendo, cerrar el segmento anterior en el Gantt
                if (cur != prev)
                {
                    if (prev != null) segs.Add((prev, segIni, t)); // cerrar segmento del anterior
                    segIni = t;                                     // iniciar nuevo segmento
                    prev = cur;                                     // actualizar proceso actual
                }

                cur.tRestante--;  // ejecutar un tick
                t++;              // avanzar el reloj

                // Si terminó
                if (cur.tRestante == 0)
                {
                    segs.Add((cur, segIni, t));            // cerrar su último segmento en Gantt
                    prev = null;                           // no hay proceso previo
                    cur.Fin = t;                           // termina ahora
                    cur.T = cur.Fin - cur.Llegada;         // turnaround = fin - llegada
                    cur.E = cur.T - cur.t;                 // espera = turnaround - burst
                    resultado.Add(cur);                    // agregar a resultados
                }
            }
        }

        // ─── Gantt (ahora basado en segmentos para soportar expulsión) ────────────

        private void DibujarGantt(List<Proceso> procesos, List<(Proceso p, int ini, int fin)> segs)
        {
            if (!segs.Any()) return;

            int tiempoTotal = segs.Max(s => s.fin);
            int cellSize = 30;

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            for (int t = 0; t <= tiempoTotal; t++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellSize) });

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) });
            foreach (var _ in procesos)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellSize) });

            // Encabezado numérico
            for (int t = 0; t <= tiempoTotal; t++)
            {
                var lbl = new TextBlock
                {
                    Text = t.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 11
                };
                Grid.SetRow(lbl, 0); Grid.SetColumn(lbl, t + 1);
                grid.Children.Add(lbl);
            }

            // Filas de procesos
            for (int i = 0; i < procesos.Count; i++)
            {
                var p = procesos[i];

                var nombre = new TextBlock
                {
                    Text = p.Nombre,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    Foreground = p.Color
                };
                Grid.SetRow(nombre, i + 1); Grid.SetColumn(nombre, 0);
                grid.Children.Add(nombre);

                for (int t = 0; t < tiempoTotal; t++)
                {
                    bool ejecutando = segs.Any(s => s.p == p && t >= s.ini && t < s.fin);
                    bool esperando = !ejecutando && t >= p.Llegada && t < p.Fin;

                    var border = new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0.5),
                        Background = ejecutando ? p.Color
                                        : esperando ? Brushes.Black
                                        : Brushes.White
                    };
                    Grid.SetRow(border, i + 1); Grid.SetColumn(border, t + 1);
                    grid.Children.Add(border);
                }
            }

            icGantt.Content = grid;
        }

        // ─── Estadísticas ─────────────────────────────────────────────────────────

        private void MostrarEstadisticas(List<Proceso> resultado)
        {
            if (!resultado.Any()) return;

            double tRespProm = resultado.Average(p => p.T);
            double tEsperaProm = resultado.Average(p => p.E);
            double eficienciaProm = resultado.Average(p => p.P);

            txtStats.Text =
                $"T. Respuesta Prom: {tRespProm:F2}   |   " +
                $"T. Espera Prom: {tEsperaProm:F2}   |   " +
                $"Eficiencia Prom: {eficienciaProm:F2}";
        }
    }
}