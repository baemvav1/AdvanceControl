using System;
using System.Collections.Generic;

namespace Advance_Control.Models
{
    /// <summary>
    /// Fila de un timeline (panel Graficos de DetalleClientesView). Una fila puede representar
    /// un segmento (Inicio-Fin, ej. factura emitida-pagada) o una serie de puntos sueltos sobre
    /// la misma fila (ej. fechas de factura de un equipo), o ambas cosas.
    /// </summary>
    public class TimelineFila
    {
        public TimelineFila(string etiqueta, string colorHex, DateTime? inicio = null, DateTime? fin = null, List<DateTime>? puntos = null)
        {
            Etiqueta = etiqueta;
            ColorHex = colorHex;
            Inicio = inicio;
            Fin = fin;
            Puntos = puntos ?? new List<DateTime>();
        }

        public string Etiqueta { get; }

        public DateTime? Inicio { get; }

        public DateTime? Fin { get; }

        public List<DateTime> Puntos { get; }

        public string ColorHex { get; }
    }
}
