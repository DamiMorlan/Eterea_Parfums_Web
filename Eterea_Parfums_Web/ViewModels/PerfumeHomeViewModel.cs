using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class PerfumeHomeViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Imagen { get; set; }
        public string Marca { get; set; }
        public double Precio { get; set; }
        public int Presentacion { get; set; }
        public int StockDisponibleParaWeb { get; set; }
    }

}