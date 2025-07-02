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
        public ActionResult DetalleFactura(int? factura_id)
        {
            if (factura_id == null || factura_id == 0)
            {
                // Podés mostrar una vista de error o redirigir
                TempData["Error"] = "Página inválida o factura no especificada.";
                return RedirectToAction("Index", "Historial");
            }
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }

            using (var db = new etereaEntities1())
            {
                var factura = db.factura.FirstOrDefault(f => f.id == factura_id);
                if (factura == null)
                {
                    return HttpNotFound("Factura no encontrada.");
                }

                var detalles = db.detalle_factura
                                 .Where(d => d.factura_id == factura.id)
                                 .ToList();

                var perfumes = new List<DetallePerfumeViewModel>();

                foreach (var d in detalles)
                {
                    var perfumeData = db.perfume.FirstOrDefault(p => p.id == d.perfume_id);
                    if (perfumeData == null) continue;

                    var marca = db.marca.FirstOrDefault(m => m.id == perfumeData.marca_id);
                    var promocion = d.promocion_id != null
                                    ? db.promocion.FirstOrDefault(promo => promo.id == d.promocion_id)
                                    : null;

                    perfumes.Add(new DetallePerfumeViewModel
                    {
                        Id = perfumeData.id,
                        Imagen1 = perfumeData.imagen1,
                        Nombre = perfumeData.nombre,
                        Marca = marca?.nombre ?? "Sin marca",
                        Tamaño = perfumeData.presentacion_ml,
                        PrecioUnitario = (decimal)d.precio_unitario,
                        Cantidad = d.cantidad,
                        Promocion = promocion?.nombre ?? "Sin promoción"
                    });
                }

                var viewModel = new DetalleFacturaViewModel
                {
                    NumeroFactura = factura.num_factura.ToString(),
                    Fecha = factura.fecha,
                    Perfumes = perfumes
                };

                return View(viewModel);
            }
        }




    }
}