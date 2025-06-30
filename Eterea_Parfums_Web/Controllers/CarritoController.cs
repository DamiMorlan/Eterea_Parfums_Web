using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace Eterea_Parfums_Web.Controllers
{
    public class CarritoController : Controller
    {
        private etereaEntities1 db = new etereaEntities1();
        // GET: Carrito
        public ActionResult Index()
        {
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }

            int clienteId = Convert.ToInt32(Session["clienteId"]);

            // Paso 1: Obtener el carrito completo con perfumes
            var carrito = db.carrito
                .Include(c => c.perfume)
                .Where(c => c.cliente_id == clienteId)
                .OrderByDescending(c => c.id)
                .ToList();

            var perfumeIds = carrito.Select(c => c.perfume_id).ToList();

            // Paso 2: Calcular stock disponible para cada perfume
            var stockPorPerfume = db.stock
                .Where(s => perfumeIds.Contains(s.perfume_id))
                .ToList()
                .GroupBy(s => s.perfume_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => Math.Max(0, s.cantidad - 5)).Sum()
                );

            bool huboCambios = false;

            // Paso 3: Ajustar carrito en memoria y DB según el stock
            foreach (var item in carrito.ToList())
            {
                int stockDisponible = stockPorPerfume.ContainsKey(item.perfume_id) ? stockPorPerfume[item.perfume_id] : 0;

                if (stockDisponible <= 0)
                {
                    db.carrito.Remove(item); // eliminar si ya no hay stock
                    huboCambios = true;
                    continue;
                }

                if (item.cantidad > stockDisponible)
                {
                    item.cantidad = stockDisponible; // ajustar cantidad
                    huboCambios = true;
                }
            }

            if (huboCambios)
            {
                db.SaveChanges();
                ViewBag.MensajeStockActualizado = "Algunos productos del carrito fueron ajustados por cambios en el stock.";
            }

            // Paso 4: Volver a armar el carrito actualizado
            var perfumesEnCarrito = db.carrito
                .Where(c => c.cliente_id == clienteId)
                .OrderByDescending(c => c.id)
                .Select(c => new
                {
                    c.cantidad,
                    Perfume = c.perfume
                })
                .ToList();

            perfumeIds = perfumesEnCarrito.Select(p => p.Perfume.id).ToList();

            stockPorPerfume = db.stock
                .Where(s => perfumeIds.Contains(s.perfume_id))
                .ToList()
                .GroupBy(s => s.perfume_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => Math.Max(0, s.cantidad - 5)).Sum()
                );

            // Paso 5: Armar ViewModel
            var viewModel = perfumesEnCarrito.Select(p =>
            {
                var perfume = p.Perfume;

                int stockDisponible = stockPorPerfume.ContainsKey(perfume.id)
                    ? stockPorPerfume[perfume.id]
                    : 0;

                var promociones = perfume.promocion
                  .Where(pr => pr.id != 1 &&
                               pr.activo &&
                               pr.fecha_inicio <= DateTime.Now &&
                               pr.fecha_fin >= DateTime.Now)
                  .ToList();

                var promo10 = promociones.FirstOrDefault(pr => pr.descuento == 10);
                var promoPorCantidad = promociones.FirstOrDefault(pr => pr.descuento > 10);


                double precioOriginal = perfume.precio_en_pesos;
                double precioConDescuento = precioOriginal;
                double total = 0;
                string leyendaPromo = "";
                bool tienePromo = false;

                int cantidad = p.cantidad;

                if (promoPorCantidad != null && cantidad >= 2)
                {
                    tienePromo = true;

                    int cantidadConDescuento = (cantidad / 2) * 2;
                    int cantidadSinDescuento = cantidad % 2;

                    double porcentaje = (100 - promoPorCantidad.descuento) / 100.0;

                    // Si hay impar (cantidadSinDescuento == 1), puede aplicar el 10% si la promo está
                    if (cantidadSinDescuento == 1 && promo10 != null)
                    {
                        total = (cantidadConDescuento * precioOriginal * porcentaje) +
                                (1 * precioOriginal * 0.9);

                        precioConDescuento = precioOriginal * 0.9;

                        int cantidadPromoCantidad = cantidad - 1; // la cantidad par
                        int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;

                        leyendaPromo = $"<span style='color: black;'>1 unidad:</span> Promoción 10% OFF<br />" +
                                       $"<span style='color: black;'>{cantidadPromoCantidad} unidades:</span> Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad";
                    }
                    else
                    {
                        total = (cantidadConDescuento * precioOriginal * porcentaje) +
                                (cantidadSinDescuento * precioOriginal);

                        leyendaPromo = (promoPorCantidad.descuento * 2 == 100)
                            ? "Promoción 2 x 1"
                            : $"Promoción {promoPorCantidad.descuento * 2}% de descuento en la segunda unidad";
                    }
                }
                else if (promo10 != null)
                {
                    tienePromo = true;
                    precioConDescuento = precioOriginal * 0.9;
                    total = precioConDescuento * cantidad;
                    leyendaPromo = "Promoción 10% OFF";

                    if (promoPorCantidad != null && stockDisponible > cantidad) // 🔍 SOLO si se puede agregar otra unidad
                    {
                        int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;
                        leyendaPromo += $"<br /><strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
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
                    LeyendaPromo = leyendaPromo,
                    StockDisponibleParaVentaWeb = stockDisponible
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

            // Variables auxiliares
            double precioOriginal = item.perfume.precio_en_pesos;
            double precioUnitarioConDescuento = totalPerfume / item.cantidad;
            bool mostrarPrecioTachado = false;

            // Aplicar misma lógica que en ViewModel
            var promociones = item.perfume.promocion
                .Where(pr => pr.id != 1 && pr.activo && pr.fecha_inicio <= DateTime.Now && pr.fecha_fin >= DateTime.Now)
                .ToList();

            var promo10 = promociones.FirstOrDefault(pr => pr.descuento == 10);
            var promoCantidad = promociones.FirstOrDefault(pr => pr.descuento > 10);

            if (promo10 != null && promoCantidad == null && item.cantidad == 1)
            {
                // Solo 10% aplica
                mostrarPrecioTachado = true;
            }
            else if (promo10 != null && promoCantidad != null && item.cantidad == 1)
            {
                mostrarPrecioTachado = true; // Solo 10% aplica porque no alcanza para promo por cantidad
            }

            // Si hay más de 1 unidad, NUNCA se tacha
            // => mostrarPrecioTachado = false;

            return Json(new
            {
                success = true,
                subtotal = subtotal.ToString("N0"),
                total = total.ToString("N0"),
                descuento = (subtotal - total).ToString("N0"),
                envioGratis = total >= 50000,
                perfumeTotal = totalPerfume.ToString("N0"),
                perfumeId = perfumeId,
                precioOriginal = precioOriginal.ToString("N0"),
                precioConDescuento = (mostrarPrecioTachado ? precioUnitarioConDescuento.ToString("N0") : null),
                mostrarPrecioTachado = mostrarPrecioTachado,
                leyendaPromo = ObtenerLeyendaPromoSegunCantidad(item, perfumeId)
            });
        }


        [HttpPost]
        public JsonResult Agregar(int perfumeId)
        {
            if (Session["clienteId"] == null)
            {
                return Json(new { redirect = Url.Action("Login", "Cliente") });
            }

            int clienteId = Convert.ToInt32(Session["clienteId"]);

            // Obtener perfume
            var perfume = db.perfume.Find(perfumeId);
            if (perfume == null || !perfume.activo)
            {
                return Json(new { error = "El perfume no existe o está inactivo." });
            }

            // Obtener stock total disponible para la venta web
            var stockDisponible = db.stock
                .Where(s => s.perfume_id == perfumeId)
                .ToList()
                .Select(s => Math.Max(0, s.cantidad - 5))
                .Sum();

            // Ver cuántas unidades de este perfume ya tiene el cliente en su carrito
            var itemEnCarrito = db.carrito.FirstOrDefault(c => c.cliente_id == clienteId && c.perfume_id == perfumeId);
            int cantidadEnCarrito = itemEnCarrito?.cantidad ?? 0;

            if (cantidadEnCarrito >= stockDisponible)
            {
                return Json(new { error = "Ya agregaste todas las unidades disponibles de este perfume." });
            }

            // Agregar o incrementar
            if (itemEnCarrito != null)
            {
                itemEnCarrito.cantidad++;
            }
            else
            {
                int nuevoId = db.carrito.Any() ? db.carrito.Max(c => c.id) + 1 : 1;
                db.carrito.Add(new carrito
                {
                    id = nuevoId,
                    cliente_id = clienteId,
                    perfume_id = perfumeId,
                    cantidad = 1
                });
            }

            db.SaveChanges();

            // Promo
            var promo = perfume.promocion.FirstOrDefault(pr =>
                pr.id != 1 && pr.activo && pr.fecha_inicio <= DateTime.Now && pr.fecha_fin >= DateTime.Now);

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
                    int descuentoSegundaUnidad = promo.descuento * 2;
                    leyendaPromo = (descuentoSegundaUnidad == 100)
                        ? "Promoción 2 x 1"
                        : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad";
                          
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

        private string ObtenerLeyendaPromoSegunCantidad(carrito item, int perfumeId)
        {
            var perfume = item.perfume;
            int cantidad = item.cantidad;

            var promociones = perfume.promocion
                .Where(pr => pr.id != 1 &&
                             pr.activo &&
                             pr.fecha_inicio <= DateTime.Now &&
                             pr.fecha_fin >= DateTime.Now)
                .ToList();

            var promo10 = promociones.FirstOrDefault(pr => pr.descuento == 10);
            var promoPorCantidad = promociones.FirstOrDefault(pr => pr.descuento > 10);

            if (promoPorCantidad != null && cantidad >= 2)
            {
                int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;

                if (cantidad % 2 == 1 && promo10 != null)
                {
                    int cantidadPromoCantidad = cantidad - 1; // la parte par


                    return $"<span style='color: black;'>1 unidad:</span> Promoción 10% OFF<br />" +
                           $"<span style='color: black;'>{cantidadPromoCantidad} unidades:</span> Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad";
                }
                else
                {
                    return (descuentoSegundaUnidad == 100)
                        ? "Promoción 2 x 1"
                        : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad";
                }
            }
            else if (promo10 != null)
            {
                string leyenda = "Promoción 10% OFF";

                if (promoPorCantidad != null)
                {
                    int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;
                    leyenda += $"<br /><strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
                }

                return leyenda;
            }

            return ""; // Sin promociones activas
        }


    }
}

