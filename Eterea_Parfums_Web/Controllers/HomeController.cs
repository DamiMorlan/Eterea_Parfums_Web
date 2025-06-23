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
            var perfumes = db.perfume
                             .Where(p => p.activo == true)
                             .Select(p => new PerfumeViewModel
                             {
                                 Nombre = p.nombre,
                                 Imagen1 = p.imagen1
                             }).ToList();

            return View(perfumes);
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