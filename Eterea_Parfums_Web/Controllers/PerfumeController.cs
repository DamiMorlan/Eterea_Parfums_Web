using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class PerfumeController : Controller
    {
        private etereaEntities7 db = new etereaEntities7();

        // GET: Perfume
        public ActionResult Index(List<string> marcasSeleccionadas)
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
                 .Where(p => p.activo)
                 .ToList();

            // 🔸 NUEVO: Aplicar filtro por marcas seleccionadas (si hay)
            if (marcasSeleccionadas != null && marcasSeleccionadas.Any())
            {
                perfumes = perfumes
                    .Where(p => marcasSeleccionadas.Contains(p.marca.nombre))
                    .ToList();
            }

            // 5. Agrupar perfumes por nombre y marca
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
                                    precioDescuento = Math.Round(p.precio_en_pesos * 0.5, 2);
                                }
                                else
                                {
                                    var descuento = promoPorCantidad.descuento;
                                    var precio2daUnidad = p.precio_en_pesos * (1 - (descuento / 100.0));
                                    precioDescuento = Math.Round((p.precio_en_pesos + precio2daUnidad) / 2, 2);
                                    leyendaPromo = $"Promoción {descuento * 2}% en la segunda unidad";
                                }
                            }
                            else if (promo10 != null)
                            {
                                tienePromo = true;
                                leyendaPromo = "Promoción 10% OFF";
                                precioDescuento = Math.Round(p.precio_en_pesos * 0.9, 2);
                            }

                            return new PresentacionViewModel
                            {
                                Id = p.id,
                                Ml = p.presentacion_ml,
                                Precio = p.precio_en_pesos,
                                TienePromocion = tienePromo,
                                LeyendaPromocion = leyendaPromo,
                                PrecioConDescuento = precioDescuento,
                                StockDisponible = stockDisponiblePorPerfume[p.id],
                                Imagen = p.imagen1,
                                Marca = p.marca.nombre
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
                        PrecioConDescuento = presentacionPrincipal.PrecioConDescuento,
                        TienePromocion = presentacionPrincipal.TienePromocion,
                        LeyendaPromocion = presentacionPrincipal.LeyendaPromocion,
                        Presentacion = presentacionPrincipal.Ml,
                        StockDisponibleParaWeb = presentacionPrincipal.StockDisponible,
                        Presentaciones = presentaciones
                    };
                })
                .Where(p => p != null)
                .ToList();

            // 7. Obtener marcas
            var marcas = db.marca
                .Select(m => new MarcaViewModel
                {
                    Id = m.id,
                    Nombre = m.nombre
                })
                .ToList();

            // 10. ViewModel combinado
            var viewModel = new PerfumeViewModel
            {
                Perfumes = perfumesAgrupados,
                Marcas = marcas
            };

            return View(viewModel);
        }


        // GET: Perfume/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return RedirectToAction("Index");

            // Traer y procesar stock completo
            var stock = db.stock.ToList();
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
                            p.activo &&
                            p.nota_con_tipo_de_nota.Any(n => notasComparar.Contains(n.nota_id)) &&
                            p.tipo_de_aroma.Any(a => aromasComparar.Contains(a.id)))
                .ToList();

            // Agrupar por nombre y traer TODAS las presentaciones del grupo
            var perfumesRelacionados = perfumesRelacionadosQuery
                .GroupBy(p => p.nombre)
                .Select(g =>
                {
                    var presentacionesEnMemoria = db.perfume
                     .Where(x => x.nombre == g.Key && x.activo)
                     .ToList();
                    var presentaciones = presentacionesEnMemoria
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
                                    precioDescuento = Math.Round(p.precio_en_pesos * 0.5, 2);
                                }
                                else
                                {
                                    var descuento = promoPorCantidad.descuento;
                                    var precio2daUnidad = p.precio_en_pesos * (1 - (descuento / 100.0));
                                    precioDescuento = Math.Round((p.precio_en_pesos + precio2daUnidad) / 2, 2);
                                    leyendaPromo = $"Promoción {descuento * 2}% en la segunda unidad";
                                }
                            }
                            else if (promo10 != null)
                            {
                                tienePromo = true;
                                leyendaPromo = "Promoción 10% OFF";
                                precioDescuento = Math.Round(p.precio_en_pesos * 0.9, 2);
                            }

                            return new PerfumeDto
                            {
                                Id = p.id,
                                Ml = p.presentacion_ml,
                                Precio = p.precio_en_pesos,
                                TienePromocion = tienePromo,
                                LeyendaPromocion = leyendaPromo,
                                PrecioConDescuento = precioDescuento,
                                StockDisponible = stockDisponiblePorPerfume[p.id],
                                Imagen = p.imagen1,
                                Marca = p.marca.nombre,
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

                    var principal = presentaciones.FirstOrDefault();

                    return new PerfumeRelacionadoDto
                    {
                        Id = principal.Id,
                        Nombre = g.Key,
                        Imagen = principal?.Imagen,
                        Marca = principal?.Marca,
                        Precio = principal?.Precio ?? 0,
                        Presentacion = principal?.Ml ?? 0,
                        StockDisponibleParaWeb = principal?.StockDisponible ?? 0,
                        TienePromocion = principal?.TienePromocion ?? false,
                        LeyendaPromocion = principal?.LeyendaPromocion,
                        PrecioConDescuento = principal?.PrecioConDescuento,
                        Presentaciones = presentaciones
                    };
                })
                .OrderByDescending(p => p.Presentaciones.FirstOrDefault()?.NotasComunes ?? 0)
                .ThenByDescending(p => p.Presentaciones.FirstOrDefault()?.AromasComunes ?? 0)
                .ThenBy(p => p.Presentacion)
                .Take(10)
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
    }
}
