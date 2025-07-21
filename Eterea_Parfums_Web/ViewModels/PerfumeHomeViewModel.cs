using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class PerfumeHomeViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Imagen { get; set; }
        public string Marca { get; set; }
        public double Precio { get; set; }
        public int Presentacion { get; set; }
        public int StockDisponibleParaWeb { get; set; }

        // Tamaño de los perfumes
        public List<PresentacionViewModel> Presentaciones { get; set; }

        // OPCIONAL: Podés eliminarlo o dejarlo como propiedad calculada más adelante
        public bool TienePromocion { get; set; }      
    }
    public class PresentacionViewModel
    {
        public int Id { get; set; }
        public int Ml { get; set; }
        public double Precio { get; set; }
        public string Imagen { get; set; }
        public string Marca { get; set; }
        public int StockDisponible { get; set; }
       
        // Cambiar a lista para manejar múltiples promos por tamaño
        public List<PromocionDetalleViewModel> Promociones { get; set; }

        // OPCIONAL: Podés eliminarlo o dejarlo como propiedad calculada más adelante
        public bool TienePromocion { get; set; }
    }

    public class PromocionDetalleViewModel
    {
        public string LeyendaPromocion { get; set; }
        public double? PrecioConDescuento { get; set; }
        public double PrecioOriginal { get; set; }
    }
}