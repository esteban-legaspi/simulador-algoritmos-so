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
    public partial class FifoPaginasWindow : Window
    {
        Random rnd = new Random();

        public FifoPaginasWindow()
        {
            InitializeComponent();
        }

        public class EstadoPagina
        {
            public int Referencia { get; set; }
            public string? EstadoFrames { get; set; }
            public string? Resultado { get; set; }
        }

        private void BtnAleatorio_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtFrames.Text, out int frames) || frames < 1) return;

            // Generar cadena aleatoria
            int[] refs = new int[12];
            for (int i = 0; i < refs.Length; i++)
                refs[i] = rnd.Next(1, 8);

            txtReferencias.Text = string.Join(" ", refs);
            CalcularFIFO(frames, refs);
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtFrames.Text, out int frames) || frames < 1)
            {
                MessageBox.Show("Ingresa un número de marcos válido.");
                return;
            }

            string input = txtReferencias.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Ingresa la cadena de referencias.");
                return;
            }

            // Parsear cadena
            var partes = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<int> refs = new List<int>();
            foreach (var p in partes)
            {
                if (int.TryParse(p, out int val))
                    refs.Add(val);
            }

            if (refs.Count == 0)
            {
                MessageBox.Show("La cadena no contiene números válidos.");
                return;
            }

            CalcularFIFO(frames, refs.ToArray());
        }

        private void CalcularFIFO(int numFrames, int[] referencias)
        {
            Queue<int> cola = new Queue<int>();
            HashSet<int> enMemoria = new HashSet<int>();
            List<EstadoPagina> estados = new List<EstadoPagina>();
            int fallos = 0, aciertos = 0;

            foreach (int ref_ in referencias)
            {
                string resultado;

                if (enMemoria.Contains(ref_))
                {
                    resultado = "✔ Hit";
                    aciertos++;
                }
                else
                {
                    resultado = "✘ Page Fault";
                    fallos++;

                    if (cola.Count >= numFrames)
                    {
                        int victima = cola.Dequeue();
                        enMemoria.Remove(victima);
                    }

                    cola.Enqueue(ref_);
                    enMemoria.Add(ref_);
                }

                estados.Add(new EstadoPagina
                {
                    Referencia = ref_,
                    EstadoFrames = string.Join(" | ", cola),
                    Resultado = resultado
                });
            }

            dgPaginas.ItemsSource = estados;

            int total = fallos + aciertos;
            txtFallos.Text = $"Page Faults: {fallos}";
            txtAciertos.Text = $"Hits: {aciertos}";
            txtTasa.Text = $"Tasa de fallos: {(fallos * 100.0 / total):F1}%";
        }
    }
}
