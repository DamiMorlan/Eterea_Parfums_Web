using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;

namespace Eterea_Parfums_Web.Controllers
{
    public class HistorialController : Controller
    {


        // GET: Historial
        public ActionResult Index()
        {
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }
            int clienteId = (int)Session["clienteId"];

            using (var db = new etereaEntities1())
            {
                var usuario = db.cliente.Find(clienteId);

                if (usuario == null)
                {
                    return RedirectToAction("Login", "Cliente");
                }

                var model = new HistorialViewModel
                {
                    Nombre = usuario.nombre,
                    Apellido = usuario.apellido,
                    Dni = usuario.dni.ToString(),
                    Email = usuario.e_mail,
                    Facturas = db.factura
                                 .Where(f => f.cliente_id == clienteId)
                                 .OrderByDescending(f => f.fecha)
                                 .ToList()
                };

                return View(model);
            }
        }



        // GET: Historial/DetalleFactura
        public ActionResult DetalleFactura()
        {
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }
            return View();
        }



    }
}