using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class ItemResumenPedidoViewModel
    {
        public int PerfumeId { get; set; }
        public string Nombre { get; set; }
        public string Imagen { get; set; }
        public int Presentacion { get; set; }
        public int Cantidad { get; set; }
        public double Precio { get; set; }   // ← no nullable
        public double Total => Precio * Cantidad;
    }

}