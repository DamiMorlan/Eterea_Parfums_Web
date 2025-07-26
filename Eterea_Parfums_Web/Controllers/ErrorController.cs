using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Eterea_Parfums_Web.Controllers
{
    public class ErrorController:Controller
    {
        // Para errores generales (500, excepciones no controladas)
        public ActionResult Index()
        {
            var exception = RouteData.Values["exception"] as Exception;
            ViewBag.ErrorMessage = exception?.Message;
            Response.StatusCode = 500;
            return View("Error"); // Ubicada en Views/Shared/Error.cshtml o Views/Error/Error.cshtml
        }
    
        // Para errores 404
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View("~/Views/Shared/NotFound.cshtml"); // O cambialo si lo tenés en Views/Error
        }
    }
}
