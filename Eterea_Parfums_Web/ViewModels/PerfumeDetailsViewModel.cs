using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Eterea_Parfums_Web.Models;

namespace Eterea_Parfums_Web.ViewModels
{

    public class PerfumeDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Imagen { get; set; }
        public string Marca { get; set; }
        public double Precio { get; set; }
        public int Presentacion { get; set; }
        public int StockDisponibleParaWeb { get; set; }

        // Tamaño de los perfumes
        public List<PerfumeRelacionadoDto> Presentaciones { get; set; }

        // OPCIONAL: Podés eliminarlo o dejarlo como propiedad calculada más adelante
        public bool TienePromocion { get; set; }

        public int NotasComunes { get; set; }
        public int AromasComunes { get; set; }

    }

    public class PerfumeRelacionadoDto
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
        public int NotasComunes { get; set; }
        public int AromasComunes { get; set; }

    }

    public class PerfumeDetailsViewModel
    {
        public perfume Perfume { get; set; }
        public int StockDisponibleParaWeb { get; set; }
        public List<PerfumeDto> PerfumesRelacionados { get; set; }
        public List<perfume> perfumesIgualesEnPresentacioMl { get; set; }
    }
}