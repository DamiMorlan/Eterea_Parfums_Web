using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class ItemCarritoViewModel
    {
        public int PerfumeId { get; set; }
        public string Nombre { get; set; }
        public string TipoDePerfume { get; set; }
        public int Presentacion { get; set; }
        public string Genero { get; set; }
        public string Imagen { get; set; }
        public decimal PrecioOriginal { get; set; }
        public decimal PrecioConDescuento { get; set; }
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
        public bool TienePromo { get; set; }
    }
}