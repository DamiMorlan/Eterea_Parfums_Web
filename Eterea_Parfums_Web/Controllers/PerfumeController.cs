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
        private etereaEntities1 db = new etereaEntities1();

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
                return RedirectToAction("Index");
            }

            var perfume = db.perfume.Find(id);
            if (perfume == null)
            {
                return HttpNotFound();
            }

            // Obtener stock disponible para web (sumar cantidad por perfume_id restando 5 por sucursal)
            var stockPorSucursales = db.stock
                .Where(s => s.perfume_id == perfume.id)
                .ToList();

            int stockDisponibleWeb = stockPorSucursales
                .Sum(s => Math.Max(0, s.cantidad - 5));

            // Pasar stock disponible como ViewBag
            ViewBag.StockDisponibleParaWeb = stockDisponibleWeb;

            return View(perfume);
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
