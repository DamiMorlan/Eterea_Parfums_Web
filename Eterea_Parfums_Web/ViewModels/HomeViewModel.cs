using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class HomeViewModel
    {
        public List<PerfumeHomeViewModel> Perfumes { get; set; }
        public List<PromocionViewModel> Promociones { get; set; }
        public List<MarcaViewModel> Marcas { get; set; }
        public List<PerfumeHomeViewModel> MasVendidos { get; set; }
    }
}