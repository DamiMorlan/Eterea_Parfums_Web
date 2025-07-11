using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;

namespace Eterea_Parfums_Web.Controllers
{
    public class PromocionController : Controller
    {

        private etereaEntities7 db = new etereaEntities7();
        // GET: Promocion
        public ActionResult Index()
        {

            var promociones = db.promocion
           .Where(p => p.activo && p.id != 1 && !string.IsNullOrEmpty(p.banner))
           .Select(p => new PromocionViewModel
           {
               id = p.id,
               nombre = p.nombre,
               descripcion = p.descripcion,
               FechaInicio = p.fecha_inicio,
               FechaFin = p.fecha_fin,
               banner = p.banner
           })
           .ToList();

            return View(promociones);
        }

        // GET: Promocion/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Promocion/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Promocion/Create
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

        // GET: Promocion/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Promocion/Edit/5
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

        // GET: Promocion/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Promocion/Delete/5
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
