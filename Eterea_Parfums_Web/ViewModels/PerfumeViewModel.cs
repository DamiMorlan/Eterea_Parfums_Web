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
        
    }
}