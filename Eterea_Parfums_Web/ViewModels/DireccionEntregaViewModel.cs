using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class DireccionEntregaViewModel
    {
        public string Calle { get; set; }
        public string Numeracion { get; set; }
        public string Piso { get; set; }
        public string Departamento { get; set; }
        public string CodigoPostal { get; set; }
        public string Localidad { get; set; }
        public string Provincia { get; set; }

        public string ConstruirTextoCompleto()
        {
            string linea1 = $"{Calle}  {Numeracion}";
            if (!string.IsNullOrWhiteSpace(Piso))
                linea1 += $" Piso {Piso}";
            if (!string.IsNullOrWhiteSpace(Departamento))
                linea1 += $" Dpto. {Departamento}";

            string linea2 = $"C.P. {CodigoPostal}, {Localidad}, {Provincia}";

            return linea1 + "\n" + linea2;
        }
    }
}