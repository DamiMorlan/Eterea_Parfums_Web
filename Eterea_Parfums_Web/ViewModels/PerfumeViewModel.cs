using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class PerfumeViewModel
    {
        public List<PerfumeHomeViewModel> Perfumes { get; set; }
        public List<MarcaViewModel> Marcas { get; set; }
        public List<string> Generos { get; set; }
        public List<int> Tamanios { get; set; }
        public List<string> TipoDePerfumes { get; set; }
        public List<string> TiposDeAroma { get; set; }

        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
    }
}