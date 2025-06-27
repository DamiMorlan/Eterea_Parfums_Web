using Eterea_Parfums_Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class PerfumeConStockViewModel
    {
        public perfume Perfume { get; set; }
        public int StockDisponibleParaWeb { get; set; }
    }

}