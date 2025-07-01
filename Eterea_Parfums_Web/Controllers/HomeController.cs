using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;

namespace Eterea_Parfums_Web.Controllers
{
    public class HomeController : Controller
    {
        private etereaEntities1 db = new etereaEntities1();

        public ActionResult Index()
        {
            // 1. Obtener stock completo
            var stock = db.stock.ToList();

            // 2. Calcular stock disponible por perfume
            var stockDisponiblePorPerfume = stock
                .GroupBy(s => s.perfume_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => Math.Max(0, s.cantidad - 5)).Sum()
                );

            // 3. Obtener los IDs de perfumes que están en promoción (distinta de 1)
            var perfumesEnPromocionIds = db.Database.SqlQuery<int>(
                "SELECT DISTINCT perfume_id FROM perfumes_en_promo WHERE promocion_id != 1"
            ).ToList();

            // 4. Obtener los perfumes activos que están en una promoción válida
            var perfumes = db.perfume
                .Where(p => p.activo && perfumesEnPromocionIds.Contains(p.id))
                .ToList();

            // 5. Filtrar perfumes que tengan stock disponible
            var perfumesConStock = perfumes
                .Where(p => stockDisponiblePorPerfume.ContainsKey(p.id) && stockDisponiblePorPerfume[p.id] > 0)
                .Select(p => new PerfumeHomeViewModel
                {
                    Id = p.id,
                    Nombre = p.nombre,
                    Imagen = p.imagen1,
                    Marca = p.marca.nombre,
                    Precio = p.precio_en_pesos,
                    Presentacion = p.presentacion_ml,
                    StockDisponibleParaWeb = stockDisponiblePorPerfume[p.id]
                })
                .ToList();

            // 6. Obtener promociones activas con banner
            var promociones = db.promocion
                .Where(p => p.activo && p.id != 1 && !string.IsNullOrEmpty(p.banner))
                .Select(p => new PromocionViewModel
                {
                    id = p.id,
                    nombre = p.nombre,
                    banner = p.banner
                })
                .ToList();

            // 7. Obtener marcas
            var marcas = db.marca
                .Select(m => new MarcaViewModel
                {
                    Id = m.id,
                    Nombre = m.nombre
                })
                .ToList();

            // 8. ViewModel combinado
            var viewModel = new HomeViewModel
            {
                Perfumes = perfumesConStock,
                Promociones = promociones,
                Marcas = marcas
            };

            return View(viewModel);
        }




        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}