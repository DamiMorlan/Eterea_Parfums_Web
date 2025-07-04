using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.IO;
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

            // Paso 3: Ajustar cantidades por stock
            foreach (var item in carrito.ToList())
            {
                int stockDisponible = stockPorPerfume.ContainsKey(item.perfume_id) ? stockPorPerfume[item.perfume_id] : 0;

                if (stockDisponible <= 0)
                {
                    db.carrito.Remove(item);
                    huboCambios = true;
                    continue;
                }

                if (item.cantidad > stockDisponible)
                {
                    item.cantidad = stockDisponible;
                    huboCambios = true;
                }
            }

            if (huboCambios)
            {
                db.SaveChanges();
                ViewBag.MensajeStockActualizado = "Algunos productos del carrito fueron ajustados por cambios en el stock.";
            }

            // Paso 4: Armar ViewModel usando carrito ya corregido
            var viewModel = carrito.Select(item =>
            {
                var perfume = item.perfume;
                int cantidad = item.cantidad;

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
                bool tienePromo = false;

                // Lógica de total
               
                string leyendaPromo = "";

                if (promoPorCantidad != null && cantidad >= 2)
                {
                    tienePromo = true;

                    int cantidadConDescuento = (cantidad / 2) * 2;
                    int cantidadSinDescuento = cantidad % 2;
                    double porcentajeDescuento = promoPorCantidad.descuento / 100.0;
                    double porcentaje10 = 0.9;

                    total = (cantidadConDescuento * precioOriginal * porcentajeDescuento) +
                            (cantidadSinDescuento * precioOriginal * (promo10 != null ? porcentaje10 : 1.0));

                    if (cantidadSinDescuento == 1 && promo10 != null && stockDisponible > cantidad)
                    {
                        precioConDescuento = precioOriginal * porcentaje10;
                        // Solo hay una unidad → invitar a llevar otra
                        leyendaPromo = $"<strong>Si llevás 2 iguales, el segundo tiene {porcentajeDescuento}% de descuento</strong>";
                    }
                    else if (cantidad % 2 == 1 && stockDisponible > cantidad)
                    {
                        // Cantidad impar > 1 y hay stock → sugerir agregar uno más
                        leyendaPromo += $"<br /><strong>¡Si agregás uno más lo llevás con el {porcentajeDescuento}% de descuento!</strong>";
                    }
                    else
                    {
                        precioConDescuento = precioOriginal * porcentajeDescuento;
                    }

                    int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;

                    // Mensaje principal con cantidad con descuento
                    leyendaPromo = $"<span style='color: black;'>{cantidadConDescuento} unidades:</span> " +
                                   $"{(descuentoSegundaUnidad == 100 ? "Promoción 2 x 1" : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad")}";

                    // Agregá mensaje para la unidad restante con promo 10%
                    if (cantidadSinDescuento == 1 && promo10 != null)
                    {
                        leyendaPromo += $"<br /><span style='color: black;'>1 unidad:</span> Promoción 10% OFF";
                    }

                    // Agregá mensaje de "Si agregás uno más..." solo si no hay promo10
                    if (cantidadSinDescuento == 1 && promo10 == null && stockDisponible > cantidad)
                    {
                        leyendaPromo += $"<br /><strong>¡Si agregás uno más lo llevás con el {descuentoSegundaUnidad}% de descuento!</strong>";
                    }

                }
                else if (promo10 != null)
                {
                    tienePromo = true;
                    precioConDescuento = precioOriginal * 0.9;
                    total = precioConDescuento * cantidad;
                    leyendaPromo = "Promoción 10% OFF";

                    if (promoPorCantidad != null && cantidad < 2 && stockDisponible > cantidad)
                    {
                        int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;
                        leyendaPromo += $"<br /><strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
                    }
                }
                else if (promoPorCantidad != null)
                {
                    int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;
                    double porcentajePorCantidad = (100 - promoPorCantidad.descuento) / 100.0;

                    if (cantidad == 1)
                    {
                        tienePromo = false;
                        precioConDescuento = precioOriginal;
                        total = precioOriginal;

                        leyendaPromo = $"<strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
                    }
                    else
                    {
                        tienePromo = true;

                        int cantidadConDescuento = (cantidad / 2) * 2;
                        int cantidadSinDescuento = cantidad % 2;

                        total = (cantidadConDescuento * precioOriginal * porcentajePorCantidad) +
                                (cantidadSinDescuento * precioOriginal);

                        precioConDescuento = (cantidadSinDescuento > 0) ? precioOriginal : precioOriginal * porcentajePorCantidad;

                        leyendaPromo = $"<span style='color: black;'>{cantidadConDescuento} unidades:</span> " +
                                       $"{(descuentoSegundaUnidad == 100 ? "Promoción 2 x 1" : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad")}";

                        if (cantidadSinDescuento == 1 && stockDisponible > cantidad)
                        {
                            leyendaPromo += $"<br /><strong>¡Si agregás uno más lo llevás con el {descuentoSegundaUnidad}% de descuento!</strong>";
                        }
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
                    StockDisponibleParaVentaWeb = stockDisponible,
                    MostrarPrecioTachado = promo10 != null && (promoPorCantidad == null || cantidad < 2)

                };
            }).ToList();

            return View(viewModel);
        }


        [HttpPost]
        public JsonResult ActualizarCantidad(int perfumeId, int cantidad)
        {
            if (Session["clienteId"] == null)
            {
                return Json(new { redirect = Url.Action("Login", "Cliente") });
            }

            int clienteId = Convert.ToInt32(Session["clienteId"]);

            var item = db.carrito
                .Include(c => c.perfume)
                .Include(c => c.perfume.promocion)
                .Include(c => c.perfume.tipo_de_perfume)
                .Include(c => c.perfume.genero)
                .Include(c => c.perfume.stock)
                .FirstOrDefault(c => c.perfume_id == perfumeId && c.cliente_id == clienteId);

            if (item == null)
            {
                return Json(new { success = false, error = "Item no encontrado" });
            }



            bool seElimino = false;

            if (cantidad == 0)
            {
                db.carrito.Remove(item);
                db.SaveChanges();
                seElimino = true;
            }
            else
            {
                item.cantidad = cantidad;
                db.SaveChanges();
            }

            var perfumesEnCarrito = db.carrito
                .Where(c => c.cliente_id == clienteId)
                .Include(c => c.perfume)
                .Include(c => c.perfume.promocion)
                .ToList();

            double subtotal = perfumesEnCarrito.Sum(p => p.perfume.precio_en_pesos * p.cantidad);
            double total = 0;
            double totalPerfume = 0;

            foreach (var c in perfumesEnCarrito)
            {
                var perfume = c.perfume;
                var promo = perfume.promocion.FirstOrDefault(pr =>
                    pr.id != 1 && pr.activo && pr.fecha_inicio <= DateTime.Now && pr.fecha_fin >= DateTime.Now);

                double precioAplicado;

                if (promo != null)
                {
                    if (promo.descuento == 10)
                    {
                        precioAplicado = perfume.precio_en_pesos * 0.9 * c.cantidad;
                    }
                    else
                    {
                        int pares = (c.cantidad / 2) * 2;
                        int impares = c.cantidad % 2;
                        double porcentaje = (100 - promo.descuento) / 100.0;
                        precioAplicado = (pares * perfume.precio_en_pesos * porcentaje) + (impares * perfume.precio_en_pesos);
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

            if (seElimino)
            {
                return Json(new
                {
                    success = true,
                    eliminado = true,
                    subtotal = subtotal.ToString("N0"),
                    total = total.ToString("N0"),
                    descuento = (subtotal - total).ToString("N0"),
                    envioGratis = total >= 50000,
                    perfumeId = perfumeId
                });
            }

            // PROMOCIÓN lógica detallada para la fila actual
            double precioOriginal = item.perfume.precio_en_pesos;
            double precioConDescuentoUnitario = precioOriginal;
            bool mostrarPrecioTachado = false;

            var promociones = item.perfume.promocion
                .Where(pr => pr.id != 1 && pr.activo && pr.fecha_inicio <= DateTime.Now && pr.fecha_fin >= DateTime.Now)
                .ToList();

            var promo10 = promociones.FirstOrDefault(pr => pr.descuento == 10);
            var promoCantidad = promociones.Where(p => p.descuento > 10).OrderByDescending(p => p.descuento).FirstOrDefault();

            int cantidadActual = item.cantidad;
            int paresActuales = (cantidadActual / 2) * 2;
            int imparesActuales = cantidadActual % 2;

            if (promoCantidad != null && paresActuales >= 2)
            {
                mostrarPrecioTachado = false;
                precioConDescuentoUnitario = precioOriginal;

                if (imparesActuales == 1 && promo10 != null)
                {
                    mostrarPrecioTachado = false;
                }
            }
            else if (promo10 != null)
            {
                mostrarPrecioTachado = true;
                precioConDescuentoUnitario = precioOriginal * 0.9;
            }

            // ViewModel
            int stockDisponible = item.perfume.stock.Sum(s => Math.Max(0, s.cantidad - 5));
            var model = ConstruirItemViewModel(item, stockDisponible);


            string filaHtml = RenderPartialViewToString("_FilaCarrito", model);

            return Json(new
            {
                success = true,
                eliminado = false,
                perfumeId = perfumeId,
                subtotal = subtotal.ToString("N0"),
                total = total.ToString("N0"),
                descuento = (subtotal - total).ToString("N0"),
                envioGratis = total >= 50000,
                perfumeTotal = totalPerfume.ToString("N0"),
                filaHtml = filaHtml
            });
        }

        protected string RenderPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;

            using (var sw = new System.IO.StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
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

        private string ObtenerLeyendaPromoSegunCantidad(carrito item, int stockDisponible)
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
            var promoPorCantidad = promociones
                .Where(pr => pr.descuento > 10)
                .OrderByDescending(pr => pr.descuento)
                .FirstOrDefault();

            if (promo10 != null && promoPorCantidad != null)
            {
                int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;

                if (cantidad == 1)
                {
                    return $"Promoción 10% OFF<br /><strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
                }
                else
                {
                    return (descuentoSegundaUnidad == 100)
                        ? "Promoción 2 x 1"
                        : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad";
                }
            }

            if (promo10 != null)
            {
                return "Promoción 10% OFF";
            }

            if (promoPorCantidad != null)
            {
                int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;

                if (cantidad == 1)
                {
                    return $"<strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
                }
                else if (cantidad % 2 == 1 && cantidad > 1 && stockDisponible >= cantidad + 1)
                {
                    return $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad<br /><strong>¡Si agregás uno más lo llevás con el {descuentoSegundaUnidad}% de descuento!</strong>";
                }
                else
                {
                    return (descuentoSegundaUnidad == 100)
                        ? "Promoción 2 x 1"
                        : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad";
                }
            }

            return "";
        }

        private ItemCarritoViewModel ConstruirItemViewModel(carrito item, int stockDisponible)
        {
            var perfume = item.perfume;
            int cantidad = item.cantidad;

            var promociones = perfume.promocion
                .Where(pr => pr.id != 1 && pr.activo && pr.fecha_inicio <= DateTime.Now && pr.fecha_fin >= DateTime.Now)
                .ToList();

            var promo10 = promociones.FirstOrDefault(pr => pr.descuento == 10);
            var promoPorCantidad = promociones.FirstOrDefault(pr => pr.descuento > 10);

            double precioOriginal = perfume.precio_en_pesos;
            double precioConDescuento = precioOriginal;
            double total = 0;
            bool tienePromo = false;
            string leyendaPromo = "";

            if (promoPorCantidad != null && cantidad >= 2)
            {
                tienePromo = true;
                int cantidadConDescuento = (cantidad / 2) * 2;
                int cantidadSinDescuento = cantidad % 2;
                double porcentajePorCantidad = (100 - promoPorCantidad.descuento) / 100.0;
                double porcentaje10 = 0.9;

                total = (cantidadConDescuento * precioOriginal * porcentajePorCantidad) +
                        (cantidadSinDescuento * precioOriginal * (promo10 != null ? porcentaje10 : 1.0));

                if (cantidadSinDescuento == 1 && promo10 != null && stockDisponible > cantidad)
                    precioConDescuento = precioOriginal * porcentaje10;
                else
                    precioConDescuento = precioOriginal * porcentajePorCantidad;

                int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;

                leyendaPromo = $"<span style='color: black;'>{cantidadConDescuento} unidades:</span> " +
                               $"{(descuentoSegundaUnidad == 100 ? "Promoción 2 x 1" : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad")}";

                if (cantidadSinDescuento == 1 && promo10 != null && stockDisponible > cantidad)
                    leyendaPromo += $"<br /><span style='color: black;'>1 unidad:</span> Promoción 10% OFF";
            }
            else if (promo10 != null)
            {
                tienePromo = true;
                precioConDescuento = precioOriginal * 0.9;
                total = precioConDescuento * cantidad;
                leyendaPromo = "Promoción 10% OFF";

                if (promoPorCantidad != null && cantidad < 2 && stockDisponible > cantidad)
                {
                    int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;
                    leyendaPromo += $"<br /><strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
                }
            }
            else if (promoPorCantidad != null && cantidad == 1)
            {
                tienePromo = false;
                precioConDescuento = precioOriginal;
                total = precioOriginal;
                int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;
                leyendaPromo = $"<strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
            }
            else if (promoPorCantidad != null && cantidad > 1 && cantidad % 2 == 1 && stockDisponible > cantidad)
            {
                tienePromo = false;
                precioConDescuento = precioOriginal;
                total = precioOriginal * cantidad;
                int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;
                leyendaPromo = $"<strong>¡Si agregás uno más lo llevás con el {descuentoSegundaUnidad}% de descuento!</strong>";
            }
            else
            {
                total = precioOriginal * cantidad;
            }

            return new ItemCarritoViewModel
            {
                PerfumeId = perfume.id,
                Nombre = perfume.nombre,
                TipoDePerfume = perfume.tipo_de_perfume?.tipo_de_perfume1,
                Presentacion = perfume.presentacion_ml,
                Genero = perfume.genero?.genero1,
                Imagen = perfume.imagen1,
                PrecioOriginal = precioOriginal,
                PrecioConDescuento = precioConDescuento,
                Cantidad = cantidad,
                Total = total,
                TienePromo = tienePromo,
                LeyendaPromo = leyendaPromo,
                StockDisponibleParaVentaWeb = stockDisponible,
                MostrarPrecioTachado = promo10 != null && (promoPorCantidad == null || cantidad < 2)
            };
        }




    }
}

