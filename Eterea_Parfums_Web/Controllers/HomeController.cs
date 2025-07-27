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
        private etereaEntities7 db = new etereaEntities7();

        public ActionResult Index()
        {
            // 1. Obtener stock completo
            var stock = db.stock.Where(s => s.sucursal_id == 1).ToList();

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


            // 5.  Agrupar perfumes por nombre
            var perfumesAgrupados = perfumes
                .GroupBy(p => p.nombre)
                .Select(g =>
                {
                    var presentaciones = g
                        .Where(p => stockDisponiblePorPerfume.ContainsKey(p.id))
                        .Select(p =>
                        {
                            var promocionesActivas = p.promocion
                            .Where(pr => pr.activo && pr.id != 1 &&
                                         pr.fecha_inicio <= DateTime.Now &&
                                         pr.fecha_fin >= DateTime.Now)
                            .ToList();

                            var listaPromos = new List<PromocionDetalleViewModel>();

                            foreach (var promo in promocionesActivas)
                            {
                                string leyenda = null;
                                double? precioConDescuento = null;

                                if (promo.descuento * 2 == 100)
                                {
                                    leyenda = "Promoción <br> 2x1";
                                    precioConDescuento = Math.Round(p.precio_en_pesos * 0.5, 2);
                                }
                                else if (promo.descuento == 10)
                                {
                                    leyenda = "Promoción <br> 10% OFF";
                                    precioConDescuento = Math.Round(p.precio_en_pesos * 0.9, 2);
                                }
                                else if (promo.descuento > 10)
                                {
                                    var descuento = promo.descuento;
                                    var precio2daUnidad = p.precio_en_pesos * (1 - (descuento / 100.0));
                                    precioConDescuento = Math.Round((p.precio_en_pesos + precio2daUnidad) / 2, 2);
                                    leyenda = $"Promoción <br> {descuento * 2}% segunda unidad";
                                }

                                if (precioConDescuento.HasValue)
                                {
                                    listaPromos.Add(new PromocionDetalleViewModel
                                    {
                                        LeyendaPromocion = leyenda,
                                        PrecioConDescuento = precioConDescuento,
                                        PrecioOriginal = p.precio_en_pesos
                                    });
                                }
                            }

                            return new PresentacionViewModel
                            {
                                Id = p.id,
                                Ml = p.presentacion_ml,
                                Precio = p.precio_en_pesos,
                                StockDisponible = stockDisponiblePorPerfume[p.id],
                                Imagen = p.imagen1,
                                Marca = p.marca.nombre,
                                Promociones = listaPromos,
                                TienePromocion = listaPromos.Any() // si querés mantenerlo
                            };
                        })
                        .OrderBy(p => p.Ml)
                        .ToList();

                    var presentacionPrincipal = presentaciones.First(); // Por defecto la menor

                    return new PerfumeHomeViewModel
                    {
                        Id = presentacionPrincipal.Id,
                        Nombre = g.Key,
                        Imagen = presentacionPrincipal.Imagen,
                        Marca = presentacionPrincipal.Marca,
                        Precio = presentacionPrincipal.Precio,

                        TienePromocion = presentacionPrincipal.TienePromocion,

                        Presentacion = presentacionPrincipal.Ml,
                        StockDisponibleParaWeb = presentacionPrincipal.StockDisponible,
                        Presentaciones = presentaciones
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
            var perfumesMasVendidosPorNombre = db.detalle_factura
                .Join(db.perfume, df => df.perfume_id, p => p.id, (df, p) => new { df, p })
                .Where(x => x.p.activo)
                .GroupBy(x => x.p.nombre)
                .Select(g => new
                {
                    Nombre = g.Key,
                    TotalVendido = g.Sum(x => x.df.cantidad)
                })
                .OrderByDescending(g => g.TotalVendido)
                .Take(8)
                .ToList();

            //9. Convertir a ViewModel solo los perfumes activos y con stock
            var perfumesTop = perfumesMasVendidosPorNombre
                .Select(g =>
                {
                    var presentaciones = db.perfume
                        .Where(p => p.nombre == g.Nombre && p.activo)
                        .ToList()
                        .Where(p => stockDisponiblePorPerfume.ContainsKey(p.id))
                        .Select(p =>
                        {
                            var promocionesActivas = p.promocion
                            .Where(pr => pr.activo && pr.id != 1 &&
                                         pr.fecha_inicio <= DateTime.Now &&
                                         pr.fecha_fin >= DateTime.Now)
                            .ToList();

                            var listaPromos = new List<PromocionDetalleViewModel>();

                            foreach (var promo in promocionesActivas)
                            {
                                string leyenda = null;
                                double? precioConDescuento = null;

                                if (promo.descuento * 2 == 100)
                                {
                                    leyenda = "Promoción <br> 2x1";
                                    precioConDescuento = Math.Round(p.precio_en_pesos * 0.5, 2);
                                }
                                else if (promo.descuento == 10)
                                {
                                    leyenda = "Promoción <br> 10% OFF";
                                    precioConDescuento = Math.Round(p.precio_en_pesos * 0.9, 2);
                                }
                                else if (promo.descuento > 10)
                                {
                                    var descuento = promo.descuento;
                                    var precio2daUnidad = p.precio_en_pesos * (1 - (descuento / 100.0));
                                    precioConDescuento = Math.Round((p.precio_en_pesos + precio2daUnidad) / 2, 2);
                                    leyenda = $"Promoción <br> {descuento * 2}% segunda unidad";
                                }

                                if (precioConDescuento.HasValue)
                                {
                                    listaPromos.Add(new PromocionDetalleViewModel
                                    {
                                        LeyendaPromocion = leyenda,
                                        PrecioConDescuento = precioConDescuento,
                                        PrecioOriginal = p.precio_en_pesos
                                    });
                                }
                            }

                            return new PresentacionViewModel
                            {
                                Id = p.id,
                                Ml = p.presentacion_ml,
                                Precio = p.precio_en_pesos,
                                StockDisponible = stockDisponiblePorPerfume[p.id],
                                Imagen = p.imagen1,
                                Marca = p.marca.nombre,
                                Promociones = listaPromos,
                                TienePromocion = listaPromos.Any() // si querés mantenerlo
                            };
                        })
                        .OrderBy(p => p.Ml)
                        .ToList();


                    var presentacionPrincipal = presentaciones.First();

                    return new PerfumeHomeViewModel
                    {
                        Id = presentacionPrincipal.Id,
                        Nombre = g.Nombre,
                        Imagen = presentacionPrincipal.Imagen,
                        Marca = presentacionPrincipal.Marca,
                        Precio = presentacionPrincipal.Precio,

                        TienePromocion = presentacionPrincipal.TienePromocion,

                        Presentacion = presentacionPrincipal.Ml,
                        StockDisponibleParaWeb = presentacionPrincipal.StockDisponible,
                        Presentaciones = presentaciones
                    };
                })
                .ToList();

            // 10. ViewModel combinado
            var viewModel = new HomeViewModel
            {
                Perfumes = perfumesAgrupados,
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