using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Eterea_Parfums_Web.Models;

namespace Eterea_Parfums_Web.ViewModels
{
        public class HistorialViewModel
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }
        public string Email { get; set; }
        public List<factura> Facturas { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
    }
}