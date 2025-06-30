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
            var perfumes = db.perfume
                .Where(p => p.activo)
                .ToList();

            var stock = db.stock.ToList();

            var stockDisponiblePorPerfume = stock
                .GroupBy(s => s.perfume_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => Math.Max(0, s.cantidad - 5)).Sum()
                );

            var promociones = db.promocion
                .Where(p => p.activo)
                .Select(p => new PromocionViewModel
                {
                    id = p.id,
                    nombre = p.nombre,  // opcional
                    banner = p.banner // asegurate de que el campo se llame así                  
                })
                .ToList();

            // Proyectar a ViewModel solo perfumes con stock > 0
            var perfumesDisponibles = perfumes
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

            var viewModel = new HomeViewModel
            {
                Perfumes = perfumesDisponibles,
                Promociones = promociones
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