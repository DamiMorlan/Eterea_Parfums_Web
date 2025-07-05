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
                .Where(p => stockDisponiblePorPerfume.ContainsKey(p.id))
                .Select(p =>
                {
                    var promocionesActivas = p.promocion
                        .Where(pr => pr.activo && pr.id != 1 &&
                                     pr.fecha_inicio <= DateTime.Now &&
                                     pr.fecha_fin >= DateTime.Now)
                        .ToList();

                    var promo10 = promocionesActivas.FirstOrDefault(pr => pr.descuento == 10);
                    var promoPorCantidad = promocionesActivas.FirstOrDefault(pr => pr.descuento > 10);

                    string leyendaPromo = null;
                    double? precioDescuento = null;
                    bool tienePromo = false;

                    if (promoPorCantidad != null)
                    {
                        tienePromo = true;

                        if (promoPorCantidad.descuento * 2 == 100)
                        {
                            leyendaPromo = "Promoción 2x1";
                            precioDescuento = Math.Round(p.precio_en_pesos * 0.5, 2); // Mitad de precio como estimación
                        }
                        else
                        {
                            var descuento = promoPorCantidad.descuento; // ejemplo: 80
                            var precioConDescuentoEn2daUnidad = p.precio_en_pesos * (1 - (descuento / 100.0));
                            var precioPromedio = (p.precio_en_pesos + precioConDescuentoEn2daUnidad) / 2;

                            leyendaPromo = $"Promoción {descuento * 2}% en la segunda unidad";
                            precioDescuento = Math.Round(precioPromedio, 2); // Promedio por unidad
                        }
                    }
                    else if (promo10 != null)
                    {
                        tienePromo = true;
                        leyendaPromo = "Promoción 10% OFF";
                        precioDescuento = Math.Round(p.precio_en_pesos * 0.9, 2);
                    }

                    return new PerfumeHomeViewModel
                    {
                        Id = p.id,
                        Nombre = p.nombre,
                        Imagen = p.imagen1,
                        Marca = p.marca.nombre,
                        Precio = p.precio_en_pesos,
                        Presentacion = p.presentacion_ml,
                        StockDisponibleParaWeb = stockDisponiblePorPerfume[p.id],

                        Presentaciones = db.perfume
                            .Where(x => x.nombre == p.nombre && x.activo)
                            .Select(x => new PresentacionViewModel
                            {
                                Id = x.id,
                                Ml = x.presentacion_ml,
                                Precio = x.precio_en_pesos
                            })
                            .OrderBy(x => x.Ml)
                            .ToList(),

                        TienePromocion = tienePromo,
                        LeyendaPromocion = leyendaPromo,
                        PrecioConDescuento = precioDescuento
                    };
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

            //8. Obtener perfumes más vendidos (TOP 8)
            var perfumesMasVendidos = db.detalle_factura
                .GroupBy(df => df.perfume_id)
                .Select(g => new
                {
                    PerfumeId = g.Key,
                    TotalVendido = g.Sum(x => x.cantidad)
                })
                .OrderByDescending(g => g.TotalVendido)
                .Take(8)
                .ToList();

            //9. Convertir a ViewModel solo los perfumes activos y con stock
            var perfumesTop = perfumesMasVendidos
                .Join(db.perfume.Where(p => p.activo),
                      top => top.PerfumeId,
                      p => p.id,
                      (top, p) => new { Perfume = p, top.TotalVendido })
                .Where(p => stockDisponiblePorPerfume.ContainsKey(p.Perfume.id))
                .Select(p =>
                {
                    var promocionesActivas = p.Perfume.promocion
                        .Where(pr => pr.activo && pr.id != 1 &&
                                     pr.fecha_inicio <= DateTime.Now &&
                                     pr.fecha_fin >= DateTime.Now)
                        .ToList();

                    var promo10 = promocionesActivas.FirstOrDefault(pr => pr.descuento == 10);
                    var promoPorCantidad = promocionesActivas.FirstOrDefault(pr => pr.descuento > 10);

                    string leyendaPromo = null;
                    double? precioDescuento = null;
                    bool tienePromo = false;

                    if (promoPorCantidad != null)
                    {
                        tienePromo = true;

                        if (promoPorCantidad.descuento * 2 == 100)
                        {
                            leyendaPromo = "Promoción 2x1";
                            precioDescuento = Math.Round(p.Perfume.precio_en_pesos * 0.5, 2);
                        }
                        else
                        {
                            var descuento = promoPorCantidad.descuento;
                            var precio2daUnidad = p.Perfume.precio_en_pesos * (1 - (descuento / 100.0));
                            precioDescuento = Math.Round((p.Perfume.precio_en_pesos + precio2daUnidad) / 2, 2);
                            leyendaPromo = $"Promoción {descuento}% en la segunda unidad";
                        }
                    }
                    else if (promo10 != null)
                    {
                        tienePromo = true;
                        leyendaPromo = "Promoción 10% OFF";
                        precioDescuento = Math.Round(p.Perfume.precio_en_pesos * 0.9, 2);
                    }

                    return new PerfumeHomeViewModel
                    {
                        Id = p.Perfume.id,
                        Nombre = p.Perfume.nombre,
                        Imagen = p.Perfume.imagen1,
                        Marca = p.Perfume.marca.nombre,
                        Precio = p.Perfume.precio_en_pesos,
                        PrecioConDescuento = precioDescuento,
                        TienePromocion = tienePromo,
                        LeyendaPromocion = leyendaPromo,
                        Presentacion = p.Perfume.presentacion_ml,
                        StockDisponibleParaWeb = stockDisponiblePorPerfume[p.Perfume.id],

                        Presentaciones = db.perfume
                            .Where(x => x.nombre == p.Perfume.nombre && x.activo)
                            .Select(x => new PresentacionViewModel
                            {
                                Id = x.id,
                                Ml = x.presentacion_ml,
                                Precio = x.precio_en_pesos
                            })
                            .OrderBy(x => x.Ml)
                            .ToList()
                    };
                })
                .ToList();

            // 10. ViewModel combinado
            var viewModel = new HomeViewModel
            {
                Perfumes = perfumesConStock,
                Promociones = promociones,
                Marcas = marcas,
                MasVendidos = perfumesTop
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