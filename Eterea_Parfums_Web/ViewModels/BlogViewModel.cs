using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Eterea_Parfums_Web.Models;

namespace Eterea_Parfums_Web.ViewModels
{
    public class BlogViewModel
    {
        public List<perfume> TipoUno { get; set; }
        public List<perfume> TipoDos { get; set; }
        public List<perfume> TipoTres { get; set; }
        public List<perfume> Recomendados { get; set; }
        public List<perfume> Recomendados2 { get; set; }
    }
}