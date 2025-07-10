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
        public List<PerfumeHomeViewModel> MasVendidos { get; set; }


        // Propiedad calculada para usar en los filtros (checkboxes)
        public List<string> MarcasDisponibles => Marcas?
            .Select(m => m.Nombre)
            .Distinct()
            .OrderBy(m => m)
            .ToList();
    }
}