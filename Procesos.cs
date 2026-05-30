using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace simulador_algoritmos_so
{
    public class Proceso
    {
        public string? Nombre { get; set; }
        public int Llegada { get; set; }
        public int t { get; set; }
        public int Prioridad { get; set; }
        public int Inicio { get; set; }
        public int Fin { get; set; }
        public int T { get; set; }
        public int E { get; set; }
        public double P { get; set; }
        public SolidColorBrush? Color { get; set; }

        public int tRestante { get; set; }
    }
}
