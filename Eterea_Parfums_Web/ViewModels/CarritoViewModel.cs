using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class CarritoViewModel
    {
        public List<ItemCarritoViewModel> Items { get; set; }

        public decimal Subtotal => Items.Sum(i => i.SubtotalSinDesc);
        public decimal Total => Items.Sum(i => i.TotalConDesc);
        public decimal Descuento => Subtotal - Total;
    }

}