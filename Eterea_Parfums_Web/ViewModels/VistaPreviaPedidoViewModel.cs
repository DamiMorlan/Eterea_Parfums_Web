using Eterea_Parfums_Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class VistaPreviaPedidoViewModel
    {
        public cliente Cliente { get; set; }
        public calle Calle { get; set; }
        public localidad Localidad { get; set; }
        public provincia Provincia { get; set; }
        public string DomicilioDeEnvioTexto { get; set; }


        // ← tipo corregido
        public List<ItemResumenPedidoViewModel> Items { get; set; }

        // Alias retro-compatible para vistas antiguas
        public List<ItemResumenPedidoViewModel> ItemsCarrito
        {
            get => Items;
            set => Items = value;
        }

        public double Subtotal { get; set; }
        public double Descuento { get; set; }
        public double Total { get; set; }
        public bool EnvioGratis { get; set; }   // si querés mostrarlo en la vista
    }
    
}