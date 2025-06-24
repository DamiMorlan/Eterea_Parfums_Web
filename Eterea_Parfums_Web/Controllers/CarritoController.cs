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
            //int clienteId = 2; // Simulación de cliente logueado

            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }

            int clienteId = Convert.ToInt32(Session["clienteId"]);


            var perfumesEnCarrito = db.carrito
                .Where(c => c.cliente_id == clienteId)
                .OrderByDescending(c => c.id)
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

                        int descuentoSegundaUnidad = promo.descuento * 2;

                        leyendaPromo = (descuentoSegundaUnidad == 100)
                            ? "Promoción 2 x 1"
                            : $"Promoción segunda unidad al {descuentoSegundaUnidad}%";
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
        public JsonResult ActualizarCantidad(int perfumeId, int cantidad)
        {
            //int clienteId = 2; // Simulado
            if (Session["clienteId"] == null)
            {
                return Json(new { redirect = Url.Action("Login", "Cliente") });
            }

            int clienteId = Convert.ToInt32(Session["clienteId"]);


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

            // Recalcular totales
            var perfumesEnCarrito = db.carrito
                .Where(c => c.cliente_id == clienteId)
                .ToList();

            double subtotal = perfumesEnCarrito.Sum(p => p.perfume.precio_en_pesos * p.cantidad);
            double total = 0;
            double totalPerfume = 0;

            foreach (var c in perfumesEnCarrito)
            {
                var perfume = c.perfume;
                var promo = perfume.promocion.FirstOrDefault(pr =>
                    pr.id != 1 && pr.activo && pr.fecha_inicio <= DateTime.Now && pr.fecha_fin >= DateTime.Now);

                double precioAplicado = 0;

                if (promo != null)
                {
                    if (promo.descuento == 10)
                        precioAplicado = perfume.precio_en_pesos * 0.9 * c.cantidad;
                    else
                    {
                        int conDesc = (c.cantidad / 2) * 2;
                        int sinDesc = c.cantidad % 2;
                        double porcentaje = (100 - promo.descuento) / 100.0;
                        precioAplicado = (conDesc * perfume.precio_en_pesos * porcentaje) + (sinDesc * perfume.precio_en_pesos);
                    }
                }
                else
                {
                    precioAplicado = perfume.precio_en_pesos * c.cantidad;
                }

                total += precioAplicado;

                if (c.perfume_id == perfumeId)
                {
                    totalPerfume = precioAplicado;
                }
            }

            return Json(new
            {
                success = true,
                subtotal = subtotal.ToString("N0"),
                total = total.ToString("N0"),
                descuento = (subtotal - total).ToString("N0"),
                envioGratis = total >= 50000,
                perfumeTotal = totalPerfume.ToString("N0"),
                perfumeId = perfumeId
            });
        }


        [HttpPost]
        public JsonResult Agregar(int perfumeId)
        {
            //int clienteId = 2; // simulado

            if (Session["clienteId"] == null)
            {
                return Json(new { redirect = Url.Action("Login", "Cliente") });
            }

            int clienteId = Convert.ToInt32(Session["clienteId"]);


            var item = db.carrito.FirstOrDefault(c => c.cliente_id == clienteId && c.perfume_id == perfumeId);
            if (item != null)
            {
                item.cantidad++;
            }
            else
            {
                int nuevoId = 1;
                if (db.carrito.Any())
                {
                    nuevoId = db.carrito.Max(c => c.id) + 1;
                }

                db.carrito.Add(new carrito
                {
                    id = nuevoId,
                    cliente_id = clienteId,
                    perfume_id = perfumeId,
                    cantidad = 1
                });
            }
            db.SaveChanges();

            var perfume = db.perfume.Find(perfumeId);
            int cantidad = item != null ? item.cantidad : 1;

            var promo = perfume.promocion.FirstOrDefault(pr =>
                pr.id != 1 &&
                pr.activo &&
                pr.fecha_inicio <= DateTime.Now &&
                pr.fecha_fin >= DateTime.Now);

            double precioOriginal = perfume.precio_en_pesos;
            double precioConDescuento = precioOriginal;
            string leyendaPromo = "";
            bool tienePromo = false;

            if (promo != null)
            {
                tienePromo = true;

                if (promo.descuento == 10)
                {
                    precioConDescuento = precioOriginal * 0.9;
                    leyendaPromo = "Promoción 10% OFF";
                }
                else
                {
                    // Para promociones tipo 2x1 o 2x60%
                    int descuentoSegundaUnidad = promo.descuento * 2;

                    leyendaPromo = (descuentoSegundaUnidad == 100)
                        ? "Promoción 2 x 1"
                        : $"Promoción segunda unidad al {descuentoSegundaUnidad}%";
                }
            }

            return Json(new
            {
                nombre = perfume.nombre,
                imagen = perfume.imagen1,
                tipo = perfume.tipo_de_perfume.tipo_de_perfume1,
                presentacion = perfume.presentacion_ml,
                genero = perfume.genero.genero1,
                precioOriginal = precioOriginal.ToString("N0"),
                precioDescuento = (tienePromo && promo.descuento == 10)
                    ? precioConDescuento.ToString("N0")
                    : null,
                tienePromo = tienePromo,
                leyenda = tienePromo ? leyendaPromo : null
            });
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

