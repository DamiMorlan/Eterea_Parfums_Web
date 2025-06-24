using Eterea_Parfums_Web.Models;
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
            var perfumes = db.perfume.ToList(); // Obtiene todos los perfumes
            return View(perfumes);
        }

        // GET: Perfume/Details/5
        public ActionResult Details(int id)
        {
            return View();
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
