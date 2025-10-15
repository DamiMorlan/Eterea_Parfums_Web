using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class PerfumeController : Controller
    {
        private etereaEntities7 db = new etereaEntities7();

        // GET: Perfume
        public ActionResult Index(string busqueda, List<string> marcasSeleccionadas, List<string> generosSeleccionados, List<string> tamaniosSeleccionados, List<string> tipoDePerfumeSeleccionados, List<string> tipoDeAromaSeleccionados, decimal? precioMin, decimal? precioMax, string orden, int pagina = 1, string filtrarSoloConPromocion = null, int? promocionId = null)
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
                 .Where(p => p.activo)
                 .ToList();

            // 4.1. Filtro por ID de promoción (si vino desde la pantalla de promociones)
            if (promocionId.HasValue)
            {
                perfumes = perfumes
                    .Where(p => p.promocion.Any(pr =>
                        pr.id == promocionId.Value &&
                        pr.activo &&
                        pr.fecha_inicio <= DateTime.Now &&
                        pr.fecha_fin >= DateTime.Now))
                    .ToList();
            }

            // 5. Obtener marcas
            var marcas = db.marca
                .Select(m => new MarcaViewModel
                {
                    Id = m.id,
                    Nombre = m.nombre
                })
                .ToList();

            // 6. Obtener marcas
            var generos = db.genero
                .Select(g => g.genero1)
                .Distinct()
                .ToList();

            // 7. Obtener todos los tamaños 
            var tamaniosDisponibles = db.perfume
                .Where(p => p.activo)
                .Select(p => p.presentacion_ml)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            // 8. Obtener tipos de perfumes
            var tipo_de_perfume = db.tipo_de_perfume
                .Select(t => t.tipo_de_perfume1)
                .Distinct()
                .ToList();

            // 9. Obtener tipos de aromas
            var tiposDeAroma = db.tipo_de_aroma
                .Select(a => a.nombre)
                .Distinct()
                .ToList();

            // 10. Filtro por marcas 
            if (marcasSeleccionadas != null && marcasSeleccionadas.Any())
            {
                perfumes = perfumes
                    .Where(p => marcasSeleccionadas.Contains(p.marca.nombre))
                    .ToList();
            }

            // 11. Filtro por género
            if (generosSeleccionados != null && generosSeleccionados.Any())
            {
                perfumes = perfumes
                    .Where(p => generosSeleccionados.Contains(p.genero.genero1))
                    .ToList();
            }

            // 12. Filtro por tamaño
            if (tamaniosSeleccionados != null && tamaniosSeleccionados.Any())
            {
                var tamaniosMl = tamaniosSeleccionados.Select(int.Parse).ToList();
                perfumes = perfumes.Where(p => tamaniosMl.Contains(p.presentacion_ml)).ToList();
            }

            // 13. Filtro por tipos de perfumes
            if (tipoDePerfumeSeleccionados != null && tipoDePerfumeSeleccionados.Any())
            {
                perfumes = perfumes
                    .Where(p => tipoDePerfumeSeleccionados.Contains(p.tipo_de_perfume.tipo_de_perfume1))
                    .ToList();
            }

            // 14. Filtro por tipos de aromas
            if (tipoDeAromaSeleccionados != null && tipoDeAromaSeleccionados.Any())
            {
                perfumes = perfumes
                    .Where(p => p.tipo_de_aroma.Any(a => tipoDeAromaSeleccionados.Contains(a.nombre)))
                    .ToList();
            }

            // 15. Filtro por precio Min
            if (precioMin.HasValue)
            {
                float min = (float)precioMin.Value;
                perfumes = perfumes.Where(p => p.precio_en_pesos >= min).ToList();
            }

            // 16. Filtro por precio Max
            if (precioMax.HasValue)
            {
                float max = (float)precioMax.Value;
                perfumes = perfumes.Where(p => p.precio_en_pesos <= max).ToList();
            }

            // 17. Filtro por nombre
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string nombreNormalizado = RemoverAcentos(busqueda);

                perfumes = perfumes
                    .Where(p => RemoverAcentos(p.nombre).Contains(nombreNormalizado))
                    .ToList();
            }

            // 18. Filtro por Promocion
            if (!string.IsNullOrEmpty(filtrarSoloConPromocion) && filtrarSoloConPromocion == "on")
            {
                perfumes = perfumes
                    .Where(p =>
                        p.promocion.Any(pr =>
                            pr.activo &&
                            pr.id != 1 &&
                            pr.fecha_inicio <= DateTime.Now &&
                            pr.fecha_fin >= DateTime.Now
                        )
                    )
                    .ToList();
            }

            // 19. Mostrar los perfumes
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
                                    var precio2daUnidad = p.precio_en_pesos * (1 - (descuento * 2 / 100.0));
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

                    var presentacionPrincipal = presentaciones.First(); // la menor presentación por defecto

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
                .Where(p => p != null)
                .ToList();

            // 20. Ordenar perfumes
            switch (orden)
            {
                case "nombreAsc":
                    perfumesAgrupados = perfumesAgrupados.OrderBy(p => p.Nombre).ToList();
                    break;
                case "nombreDesc":
                    perfumesAgrupados = perfumesAgrupados.OrderByDescending(p => p.Nombre).ToList();
                    break;

                case "precioAsc":
                    perfumesAgrupados = perfumesAgrupados.OrderBy(p => p.Precio).ToList();
                    break;
                case "precioDesc":
                    perfumesAgrupados = perfumesAgrupados.OrderByDescending(p => p.Precio).ToList();
                    break;


                /*case "precioAsc":
                    perfumesAgrupados = perfumesAgrupados.OrderBy(p => p.PrecioConDescuento ?? p.Precio).ToList();
                    break;
                case "precioDesc":
                    perfumesAgrupados = perfumesAgrupados.OrderByDescending(p => p.PrecioConDescuento ?? p.Precio).ToList();
                    break;*/


                case "masVendidos":
                    perfumesAgrupados = perfumesAgrupados
                        .OrderByDescending(p =>
                            db.detalle_factura
                                .Where(df => df.perfume_id == p.Id)
                                .Sum(df => (int?)df.cantidad) ?? 0
                        )
                        .ToList();
                    break;
                default:
                    // Orden por defecto (por nombre A-Z)
                    perfumesAgrupados = perfumesAgrupados.OrderBy(p => p.Nombre).ToList();
                    break;
            }

            // 20. Paginacion
            int perfumesPorPagina = 8;

            var perfumesPaginados = perfumesAgrupados
                .Skip((pagina - 1) * perfumesPorPagina)
                .Take(perfumesPorPagina)
                .ToList();

            // 21. ViewModel combinado
            var viewModel = new PerfumeViewModel
            {
                Perfumes = perfumesPaginados,
                Busqueda = busqueda,
                Marcas = marcas,
                Generos = generos,
                Tamanios = tamaniosDisponibles,
                TipoDePerfumes = tipo_de_perfume,
                TiposDeAroma = tiposDeAroma,
                PaginaActual = pagina,
                TotalPaginas = (int)Math.Ceiling((double)perfumesAgrupados.Count / perfumesPorPagina)
            };

            ViewBag.OrdenSeleccionado = orden;
            return View(viewModel);
        }


        // GET: Perfume/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            // Traer y procesar stock completo
            // 1. Obtener stock completo
            var stock = db.stock.Where(s => s.sucursal_id == 1).ToList();

            var stockDisponiblePorPerfume = stock
                .GroupBy(s => s.perfume_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => Math.Max(0, s.cantidad - 5)).Sum()
                );

            // Obtener perfume base
            var perfume = db.perfume.Find(id);
            if (perfume == null)
                return HttpNotFound();

            // Cargar relaciones necesarias
            db.Entry(perfume).Collection(p => p.nota_con_tipo_de_nota).Load();
            db.Entry(perfume).Collection(p => p.tipo_de_aroma).Load();
            db.Entry(perfume).Collection(p => p.stock).Load();

            int stockPerfumeActual = stockDisponiblePorPerfume.ContainsKey(perfume.id)
                ? stockDisponiblePorPerfume[perfume.id]
                : 0;

            // Obtener presentaciones iguales (mismo nombre y marca)
            var perfumesIgualesConDistintaPresentacion = db.perfume
                .Where(p => p.nombre == perfume.nombre && p.activo && p.marca.nombre == perfume.marca.nombre)
                .OrderBy(p => p.presentacion_ml)
                .ToList();

            // Notas y aromas a comparar
            var notasComparar = perfume.nota_con_tipo_de_nota.Select(n => n.nota_id).ToList();
            var aromasComparar = perfume.tipo_de_aroma.Select(a => a.id).ToList();

            // Perfumes relacionados base
            var perfumesRelacionadosQuery = db.perfume
                .Where(p => p.id != perfume.id &&
                            p.nombre != perfume.nombre && // Agregamos esta condición
                            p.activo &&
                            p.nota_con_tipo_de_nota.Any(n => notasComparar.Contains(n.nota_id)) &&
                            p.tipo_de_aroma.Any(a => aromasComparar.Contains(a.id)))
                .ToList();

            // Agrupar por nombre y traer TODAS las presentaciones del grupo
            var perfumesRelacionados = perfumesRelacionadosQuery
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
                                    var precio2daUnidad = p.precio_en_pesos * (1 - (descuento * 2 / 100.0));
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

                            return new PerfumeRelacionadoDto
                            {
                                Id = p.id,
                                Ml = p.presentacion_ml,
                                Precio = p.precio_en_pesos,
                                StockDisponible = stockDisponiblePorPerfume[p.id],
                                Imagen = p.imagen1,
                                Marca = p.marca.nombre,
                                Promociones = listaPromos,
                                TienePromocion = listaPromos.Any(),// si querés mantenerlo
                                NotasComunes = p.nota_con_tipo_de_nota
                                     .Where(n => (n.tipo_de_nota_id == 2 || n.tipo_de_nota_id == 3) &&
                                                 notasComparar.Contains(n.nota_id))
                                     .Select(n => n.nota_id)
                                     .Distinct()
                                     .Count(),
                                AromasComunes = p.tipo_de_aroma
                                     .Where(a => aromasComparar.Contains(a.id))
                                     .Select(a => a.id)
                                     .Distinct()
                                     .Count()
                            };
                        })
                        .OrderBy(p => p.Ml)
                        .ToList();

                    var presentacionPrincipal = presentaciones.First(); // Por defecto la menor

                    return new PerfumeDto
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

            // Armar ViewModel
            var viewModel = new PerfumeDetailsViewModel
            {
                Perfume = perfume,
                StockDisponibleParaWeb = stockPerfumeActual,
                PerfumesRelacionados = perfumesRelacionados,
                perfumesIgualesEnPresentacioMl = perfumesIgualesConDistintaPresentacion
            };

            return View(viewModel);
        }




        // GET: Perfume/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Perfume/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Perfume/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Perfume/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Perfume/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Perfume/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        private string RemoverAcentos(string texto)
        {
            if (texto == null) return null;
            return new string(texto
                .Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray())
                .ToLowerInvariant();
        }
    }
}
