using Eterea_Parfums_Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Eterea_Parfums_Web.ViewModels;

namespace Eterea_Parfums_Web.Controllers
{
    public class PerfumeController : Controller
    {
        private etereaEntities1 db = new etereaEntities1();

        // GET: Perfume
        public ActionResult Index()
        {
            var perfumes = db.perfume.ToList(); // Obtiene todos los perfumes
            return View(perfumes);
        }

        // GET: Perfume/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index"); // O HttpNotFound
            }

            var perfume = db.perfume.Find(id);

            if (perfume == null)
            {
                return HttpNotFound();
            }

            // Obtener notas y aromas directamente del perfume cargado
            var notasComparar = perfume.nota_con_tipo_de_nota.Select(n => n.nota_id).ToList();
            var aromasComparar = perfume.tipo_de_aroma.Select(a => a.id).ToList();

            // Obtener perfumes relacionados
            var perfumesRelacionados = db.perfume
             .Where(p => p.id != perfume.id &&
                         p.activo &&
                         p.stock.Any(s => s.cantidad > 0) &&
                         p.nota_con_tipo_de_nota.Any(n => notasComparar.Contains(n.nota_id)) &&
                         p.tipo_de_aroma.Any(a => aromasComparar.Contains(a.id)))
             .Select(p => new PerfumeRelacionadoDto
             {
                 id = p.id,
                 codigo = p.codigo,
                 marca_id = p.marca_id,
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

                 TotalStock = p.stock.Sum(s => s.cantidad)
             })
             .OrderByDescending(p => p.NotasComunes)
             .ThenByDescending(p => p.AromasComunes)
             .Take(10)
             .ToList();

            var viewModel = new PerfumeDetailsViewModel
            {
                Perfume = perfume,
                PerfumesRelacionados = perfumesRelacionados
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
