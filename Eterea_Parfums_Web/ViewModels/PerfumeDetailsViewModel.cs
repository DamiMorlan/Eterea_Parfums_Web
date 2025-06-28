using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Eterea_Parfums_Web.Models;

namespace Eterea_Parfums_Web.ViewModels
{

    public class PerfumeRelacionadoDto
    {
        public int id { get; set; }
        public string codigo { get; set; }
        public string marca { get; set; }
        public string nombre { get; set; }
        public int tipo_de_perfume_id { get; set; }
        public int genero_id { get; set; }
        public int presentacion_ml { get; set; }
        public int pais_id { get; set; }
        public bool spray { get; set; }
        public bool recargable { get; set; }
        public string descripcion { get; set; }
        public int anio_de_lanzamiento { get; set; }
        public double precio_en_pesos { get; set; }
        public bool activo { get; set; }
        public string imagen1 { get; set; }
        public string imagen2 { get; set; }
        public System.DateTime? fecha_baja { get; set; }

        public int NotasComunes { get; set; }
        public int AromasComunes { get; set; }
        public int TotalStock { get; set; }
    }

    public class PerfumeDetailsViewModel
    {
        public perfume Perfume { get; set; }
        public int StockDisponibleParaWeb { get; set; }
        public List<PerfumeRelacionadoDto> PerfumesRelacionados { get; set; }
    }
}