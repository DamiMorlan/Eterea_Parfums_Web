using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class DireccionFormateadaViewModel
    {
        public string Linea1 { get; set; }
        public string Linea2 { get; set; }
        public string TextoCompleto => Linea1 + "\n" + Linea2;
    }

}