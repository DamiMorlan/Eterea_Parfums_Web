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
        public ActionResult Index()
        {
            // Traer todos los perfumes activos
            var perfumes = db.perfume
                .Where(p => p.activo) // 👈 solo perfumes activos
                .ToList();

            // Traer el stock completo en memoria
            var stock = db.stock.ToList();

            // Calcular stock disponible para venta web por perfume
            var stockDisponiblePorPerfume = stock
                .GroupBy(s => s.perfume_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => Math.Max(0, s.cantidad - 5)).Sum()
                );

            // Generar el ViewModel con perfumes activos que tienen stock > 0
            var viewModel = perfumes
                .Where(p => stockDisponiblePorPerfume.ContainsKey(p.id) && stockDisponiblePorPerfume[p.id] > 0)
                .Select(p => new PerfumeConStockViewModel
                {
                    Perfume = p,
                    StockDisponibleParaWeb = stockDisponiblePorPerfume[p.id]
                })
                .ToList();

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
