using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class CarritoController : Controller
    {
        private etereaEntities1 db = new etereaEntities1();
        // GET: Carrito
        public ActionResult Index()
        {
            int clienteId = 2; // Simulación de cliente logueado

            //int clienteId = Convert.ToInt32(Session["clienteId"]);

            var perfumesEnCarrito = db.carrito
                .Where(c => c.cliente_id == clienteId)
                .Select(c => new
                {
                    c.cantidad,
                    Perfume = c.perfume
                })
                .ToList();

            var viewModel = perfumesEnCarrito.Select(p =>
            {
                var perfume = p.Perfume;

                var promo = perfume.promocion
                    .Where(pr => pr.id != 1 &&
                                 pr.activo &&
                                 pr.fecha_inicio <= DateTime.Now &&
                                 pr.fecha_fin >= DateTime.Now)
                    .FirstOrDefault();

                double precioOriginal = perfume.precio_en_pesos;
                double precioConDescuento = precioOriginal;
                double total = 0;
                string leyendaPromo = "";
                bool tienePromo = false;
                int cantidad = p.cantidad;

                if (promo != null)
                {
                    tienePromo = true;

                    if (promo.descuento == 10)
                    {
                        precioConDescuento = precioOriginal * 0.9;
                        total = precioConDescuento * cantidad;
                        leyendaPromo = "Promoción 10% OFF";
                    }
                    else
                    {
                        int cantidadConDescuento = (cantidad / 2) * 2;
                        int cantidadSinDescuento = cantidad % 2;

                        double porcentaje = (100 - promo.descuento) / 100.0;
                        total = (cantidadConDescuento * precioOriginal * porcentaje) +
                                (cantidadSinDescuento * precioOriginal);

                        leyendaPromo = (promo.descuento * 2 == 100)
                            ? "Promoción 2 x 1"
                            : $"Promoción segunda unidad al {promo.descuento}%";
                    }
                }
                else
                {
                    total = precioOriginal * cantidad;
                }

                return new ItemCarritoViewModel
                {
                    PerfumeId = perfume.id,
                    Nombre = perfume.nombre,
                    TipoDePerfume = perfume.tipo_de_perfume.tipo_de_perfume1,
                    Presentacion = perfume.presentacion_ml,
                    Genero = perfume.genero.genero1,
                    Imagen = perfume.imagen1,
                    PrecioOriginal = precioOriginal,
                    PrecioConDescuento = precioConDescuento,
                    Cantidad = cantidad,
                    Total = total,
                    TienePromo = tienePromo,
                    LeyendaPromo = leyendaPromo
                };
            }).ToList();

            return View(viewModel);
        }


        [HttpPost]
        public ActionResult ActualizarCantidad(int perfumeId, int cantidad)
        {
            int clienteId = 2; // simulado

            var item = db.carrito.FirstOrDefault(c => c.perfume_id == perfumeId && c.cliente_id == clienteId);
            if (item != null)
            {
                if (cantidad == 0)
                {
                    db.carrito.Remove(item);
                }
                else
                {
                    item.cantidad = cantidad;
                }
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }



        // GET: Carrito/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Carrito/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Carrito/Create
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

        // GET: Carrito/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Carrito/Edit/5
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

        // GET: Carrito/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Carrito/Delete/5
        [HttpPost]
        public ActionResult Eliminar(int perfumeId)
        {
            int clienteId = 2; // simulado

            var item = db.carrito.FirstOrDefault(c => c.cliente_id == clienteId && c.perfume_id == perfumeId);
            if (item != null)
            {
                db.carrito.Remove(item);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}

