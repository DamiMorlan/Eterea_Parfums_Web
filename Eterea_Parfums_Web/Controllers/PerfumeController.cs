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
        public ActionResult Index(List<string> marcasSeleccionadas, List<string> generosSeleccionados, List<string> tamaniosSeleccionados, List<string> tipoDePerfumeSeleccionados, List<string> tipoDeAromaSeleccionados, decimal? precioMin, decimal? precioMax, string orden)
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

            // 13. Filtro por tipos de aromas
            if (tipoDeAromaSeleccionados != null && tipoDeAromaSeleccionados.Any())
            {
                perfumes = perfumes
                    .Where(p => p.tipo_de_aroma.Any(a => tipoDeAromaSeleccionados.Contains(a.nombre)))
                    .ToList();
            }

            // 14. Filtro por precio dinámico (rango libre)
            if (precioMin.HasValue)
            {
                float min = (float)precioMin.Value;
                perfumes = perfumes.Where(p => p.precio_en_pesos >= min).ToList();
            }

            if (precioMax.HasValue)
            {
                float max = (float)precioMax.Value;
                perfumes = perfumes.Where(p => p.precio_en_pesos <= max).ToList();
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

            // Ordenar perfumes agrupados
            switch (orden)
            {
                case "nombreAsc":
                    perfumesAgrupados = perfumesAgrupados.OrderBy(p => p.Nombre).ToList();
                    break;
                case "nombreDesc":
                    perfumesAgrupados = perfumesAgrupados.OrderByDescending(p => p.Nombre).ToList();
                    break;
                case "precioAsc":
                    perfumesAgrupados = perfumesAgrupados.OrderBy(p => p.PrecioConDescuento ?? p.Precio).ToList();
                    break;
                case "precioDesc":
                    perfumesAgrupados = perfumesAgrupados.OrderByDescending(p => p.PrecioConDescuento ?? p.Precio).ToList();
                    break;
            }

            // 10. ViewModel combinado
            var viewModel = new PerfumeViewModel
            {
                Perfumes = perfumesAgrupados,
                Marcas = marcas,
                Generos = generos,
                Tamanios = tamaniosDisponibles,
                TipoDePerfumes = tipo_de_perfume,
                TiposDeAroma = tiposDeAroma
            };

            ViewBag.OrdenSeleccionado = orden;
            return View(viewModel);
        }


        // GET: Perfume/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index"); // O HttpNotFound
            }


            // Traer stock completo y calcular stock ajustado por perfume
            var stock = db.stock.ToList();
            var stockDisponiblePorPerfume = stock
               .GroupBy(s => s.perfume_id)
               .ToDictionary(
                g => g.Key,
                g => g.Select(s => Math.Max(0, s.cantidad - 5)).Sum()
                );

            //Traemos el perfumes
            var perfume = db.perfume.Find(id);

            if (perfume == null)
            {
                return HttpNotFound();
            }

            // Cargar relaciones manualmente
            db.Entry(perfume).Collection(p => p.nota_con_tipo_de_nota).Load();
            db.Entry(perfume).Collection(p => p.tipo_de_aroma).Load();
            db.Entry(perfume).Collection(p => p.stock).Load();

            // obtené el stock ajustado del perfume actual:
            int stockPerfumeActual = stockDisponiblePorPerfume.ContainsKey(perfume.id)
                ? stockDisponiblePorPerfume[perfume.id]
                : 0;


            //Obtener los perfumes iguales al perfume con distinta presentacion en ml

            var perfumesIgualesConDistintaPresentacion = db.perfume
                .Where(p => p.nombre == perfume.nombre
                      && p.marca.nombre == perfume.marca.nombre)
                .OrderBy(p => p.presentacion_ml)
                .ToList();


            // Obtener notas y aromas directamente del perfume cargado
            var notasComparar = perfume.nota_con_tipo_de_nota.Select(n => n.nota_id).ToList();
            var aromasComparar = perfume.tipo_de_aroma.Select(a => a.id).ToList();


            // Obtener perfumes relacionados
            var perfumesRelacionados = db.perfume
                .Where(p => p.id != perfume.id &&
                   p.activo &&
                   p.nota_con_tipo_de_nota.Any(n => notasComparar.Contains(n.nota_id)) &&
                   p.tipo_de_aroma.Any(a => aromasComparar.Contains(a.id)))
                .ToList()
                .Where(p => stockDisponiblePorPerfume.ContainsKey(p.id) && stockDisponiblePorPerfume[p.id] > 0)
                .Select(p => new PerfumeRelacionadoDto
                {
                    id = p.id,
                    codigo = p.codigo,
                    marca = p.marca.nombre,
                    nombre = p.nombre,
                    tipo_de_perfume_id = p.tipo_de_perfume_id,
                    genero_id = p.genero_id,
                    presentacion_ml = p.presentacion_ml,
                    pais_id = p.pais_id,
                    spray = p.spray,
                    recargable = p.recargable,
                    descripcion = p.descripcion,
                    anio_de_lanzamiento = p.anio_de_lanzamiento,
                    precio_en_pesos = p.precio_en_pesos,
                    activo = p.activo,
                    imagen1 = p.imagen1,
                    imagen2 = p.imagen2,
                    fecha_baja = p.fecha_baja,

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
                       .Count(),

                    TotalStock = stockDisponiblePorPerfume[p.id] // ← usamos el stock ajustado
                })
                   .OrderByDescending(p => p.NotasComunes)
                   .ThenByDescending(p => p.AromasComunes)
                   .Take(10)
                   .ToList();

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
