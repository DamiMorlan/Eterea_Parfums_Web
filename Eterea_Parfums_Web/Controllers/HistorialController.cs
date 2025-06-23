using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Eterea_Parfums_Web.Models;

namespace Eterea_Parfums_Web.Controllers
{
    public class HistorialController : Controller
    {


        // GET: Historial
        public ActionResult Index()
        {
            return View();
        }

        // GET: Historial/DetalleFactura
        public ActionResult DetalleFactura()
        {
            return View();
        }



    }
}