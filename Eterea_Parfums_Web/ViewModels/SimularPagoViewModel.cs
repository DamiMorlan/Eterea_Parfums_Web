using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class SimularPagoViewModel
    {
        public double Monto { get; set; }   // total sin recargo
        public double SaldoCuenta { get; set; }   // random
        public string Usuario { get; set; }
    }

}