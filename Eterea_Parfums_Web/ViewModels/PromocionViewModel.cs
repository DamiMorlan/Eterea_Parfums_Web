using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class PromocionViewModel
    {
        public int id { get; set; }
        public string nombre { get; set; }

        public string descripcion { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime FechaFin { get; set; }
        public string banner { get; set; } // opcional
    }
}
