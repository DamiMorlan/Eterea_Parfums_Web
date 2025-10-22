using Eterea_Parfums_Web.Helpers;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Eterea_Parfums_Web.Controllers
{
    public class CarritoController : Controller
    {
        private etereaEntities7 db = new etereaEntities7();
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
                .Include(c => c.perfume.promocion)   // 👈 necesarias para el helper
                .Include(c => c.perfume.stock)
                .Where(c => c.cliente_id == clienteId)
                .OrderByDescending(c => c.id)
                .ToList();


            var perfumeIds = carrito.Select(c => c.perfume_id).ToList();

            // Paso 2: Calcular stock disponible para cada perfume
            var stockPorPerfume = db.stock
              .Where(s => perfumeIds.Contains(s.perfume_id) && s.sucursal_id == 1)
              .ToList()
              .ToDictionary(
                  s => s.perfume_id,
                  s => Math.Max(0, s.cantidad - 5)
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


          

            // 2) ViewModel usando el mismo método que usa ActualizarCantidad
            var viewModel = carrito.Select(c =>
                    CarritoHelper.BuildItemViewModel(
                        c,
                        stockPorPerfume.ContainsKey(c.perfume_id)
                            ? stockPorPerfume[c.perfume_id]
                            : 0))
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        public JsonResult ActualizarCantidad(int perfumeId, int cantidad)
        {
            if (Session["clienteId"] == null)
                return Json(new { redirect = Url.Action("Login", "Cliente") });

            int clienteId = (int)Session["clienteId"];

            var item = db.carrito
                .Include(c => c.perfume)
                .Include(c => c.perfume.promocion)
                .Include(c => c.perfume.stock)
                .FirstOrDefault(c => c.cliente_id == clienteId && c.perfume_id == perfumeId);

            if (item == null)
                return Json(new { success = false, error = "Item no encontrado" });

            int stockDisponible = db.stock
                .Where(s => s.perfume_id == perfumeId && s.sucursal_id == 1)
                .ToList()
                .Select(s => Math.Max(0, s.cantidad - 5))
                .FirstOrDefault();

            bool eliminado;
            if (cantidad == 0)
            {
                db.carrito.Remove(item);
                eliminado = true;
            }
            else
            {
                item.cantidad = Math.Min(cantidad, stockDisponible);
                eliminado = false;
            }

            db.SaveChanges();

            // Recalcular carrito
            var carrito = db.carrito
                .Where(c => c.cliente_id == clienteId)
                .Include(c => c.perfume)
                .Include(c => c.perfume.promocion)
                .Include(c => c.perfume.stock)
                .ToList();

            // Subtotal y total en DECIMAL (sin redondeos)
            decimal subtotalDec = 0m;
            decimal totalDec = 0m;

            foreach (var c in carrito)
            {
                decimal p = ToDec(c.perfume.precio_en_pesos);
                int cant = c.cantidad;

                subtotalDec += p * cant;

                var promos = c.perfume.promocion
                    .Where(pr => pr.id != 1 && pr.activo && pr.fecha_inicio <= DateTime.Now && pr.fecha_fin >= DateTime.Now)
                    .ToList();

                bool tiene10 = promos.Any(pr => pr.descuento == 10);
                var promoCant = promos.Where(pr => pr.descuento > 10).OrderByDescending(pr => pr.descuento).FirstOrDefault();

                if (promoCant != null && cant >= 2)
                {
                    int pares = cant / 2;
                    int resto = cant % 2;

                    decimal descPar = (2m * p) * ((decimal)promoCant.descuento / 100m);
                    decimal descResto = (resto == 1 && tiene10) ? 0.10m * p : 0m;

                    decimal descTotal = pares * descPar + descResto;
                    totalDec += (p * cant) - descTotal;
                }
                else if (tiene10)
                {
                    totalDec += (p * cant * 0.90m);
                }
                else
                {
                    totalDec += (p * cant);
                }
            }

            decimal descuentoDec = subtotalDec - totalDec;

            // Preparar fila HTML si no fue eliminado
            string filaHtml = "";
            if (!eliminado)
            {
                // stock por perfume para el helper
                var perfumeIds = carrito.Select(c => c.perfume_id).Distinct().ToList();
                var stockDict = db.stock
                  .Where(s => perfumeIds.Contains(s.perfume_id) && s.sucursal_id == 1)
                  .ToList()
                  .GroupBy(s => s.perfume_id)
                  .ToDictionary(g => g.Key, g => g.Sum(s => Math.Max(0, s.cantidad - 5)));

                var vmFila = CarritoHelper.BuildItemViewModel(
                    carrito.First(c => c.perfume_id == perfumeId),
                    stockDict.ContainsKey(perfumeId) ? stockDict[perfumeId] : 0
                );

                filaHtml = RenderPartialViewToString("_FilaCarrito", vmFila);
            }

            // DEVOLVER EN CENTAVOS (enteros), sin strings formateadas
            return Json(new
            {
                success = true,
                eliminado,
                perfumeId,
                filaHtml,

                subtotal_cents = ToCents(subtotalDec),
                descuento_cents = ToCents(descuentoDec),
                total_cents = ToCents(totalDec),

                envioGratis = totalDec >= 50000m
            });
        }

        private string RenderPartialViewToString(string viewName, object model)
        {
            // Si olvidás pasar viewName, usamos la acción actual
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            // Asignamos el modelo para la vista parcial
            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                // 1) Localizamos la vista parcial
                ViewEngineResult viewResult =
                    ViewEngines.Engines.FindPartialView(ControllerContext, viewName);

                if (viewResult.View == null)
                    throw new FileNotFoundException($"No se encontró la vista parcial '{viewName}'.");

                // 2) Renderizamos en el StringWriter
                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    sw
                );

                viewResult.View.Render(viewContext, sw);

                // 3) Liberamos la vista y devolvemos el HTML
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }




        [HttpPost]
        public JsonResult Agregar(int perfumeId)
        {
            if (Session["clienteId"] == null)
                return Json(new { redirect = Url.Action("Login", "Cliente") });

            int clienteId = (int)Session["clienteId"];

            // 1) Perfume válido
            var perfume = db.perfume.Find(perfumeId);
            if (perfume == null || !perfume.activo)
                return Json(new { error = "El perfume no existe o está inactivo." });

            // 2) Stock neto disponible para venta web
            int stockDisponible = db.stock
             .Where(s => s.perfume_id == perfumeId && s.sucursal_id == 1)
             .ToList() // 👈 importante
             .Select(s => Math.Max(0, s.cantidad - 5))
             .FirstOrDefault();

            // 3) Ítem actual en carrito (si existe)
            var itemEnCarrito = db.carrito
                .FirstOrDefault(c => c.cliente_id == clienteId && c.perfume_id == perfumeId);

            int cantidadEnCarrito = itemEnCarrito?.cantidad ?? 0;
            if (cantidadEnCarrito >= stockDisponible)
                return Json(new { error = "Ya agregaste todas las unidades disponibles de este perfume." });

            /*-------------------------------------------------
             * 4) Agregar o incrementar
             *------------------------------------------------*/
            carrito nuevoItem = null;          // capturamos referencia si es nuevo

            if (itemEnCarrito != null)
            {
                itemEnCarrito.cantidad++;
            }
            else
            {
                int nuevoId = db.carrito.Any() ? db.carrito.Max(c => c.id) + 1 : 1;

                nuevoItem = new carrito
                {
                    id = nuevoId,
                    cliente_id = clienteId,
                    perfume_id = perfumeId,
                    cantidad = 1
                };
                db.carrito.Add(nuevoItem);
            }

            db.SaveChanges();

            /*-------------------------------------------------
             * 5) ViewModel con las reglas centrales
             *------------------------------------------------*/
            var itemActual = itemEnCarrito ?? nuevoItem;        // seguro ≠ null
            var vm = CarritoHelper.BuildItemViewModel(itemActual, stockDisponible);

            /*-------------------------------------------------
             * 6) Respuesta JSON
             *------------------------------------------------*/
            return Json(new
            {
                nombre = vm.Nombre,
                imagen = vm.Imagen,
                tipo = vm.TipoDePerfume,
                presentacion = vm.Presentacion,
                genero = vm.Genero,
                precioOriginal = vm.PrecioOriginal.ToString("N0"),

                // vm.PrecioConDescuento es double?  ⇒  usar ?.Value o el operador ?.
                precioDescuento = vm.MostrarPrecioTachado
                                    ? vm.PrecioConDescuento?.ToString("N0")
                                    : null,

                tienePromo = vm.TienePromo,
                leyenda = vm.LeyendaPromo
            });
        }


        [HttpPost]
        public JsonResult Agregar2(int perfumeId, int cantidad = 1)
        {
            if (Session["clienteId"] == null)
                return Json(new { redirect = Url.Action("Login", "Cliente") });

            int clienteId = (int)Session["clienteId"];

            var perfume = db.perfume.Find(perfumeId);
            if (perfume == null || !perfume.activo)
                return Json(new { error = "El perfume no existe o está inactivo." });

            // ✅ Stock excedente solo del local 1
            int stockDisponible = db.stock
                .Where(s => s.perfume_id == perfumeId && s.sucursal_id == 1)
                .ToList()
                .Select(s => Math.Max(0, s.cantidad - 5))
                .Sum(); // por si hay más de una fila

            var itemEnCarrito = db.carrito
                .FirstOrDefault(c => c.cliente_id == clienteId && c.perfume_id == perfumeId);

            int cantidadEnCarrito = itemEnCarrito?.cantidad ?? 0;
            int cantidadMaximaParaAgregar = stockDisponible - cantidadEnCarrito;

            if (cantidadMaximaParaAgregar <= 0)
            {
                return Json(new { error = "Ya tenés todas las unidades disponibles de este perfume." });
            }

            int cantidadFinal = Math.Min(cantidad, cantidadMaximaParaAgregar);
            string mensaje = null;

            if (cantidadFinal < cantidad)
            {
                mensaje = $"Solo se agregaron {cantidadFinal} unidad/es disponibles (total permitido: {stockDisponible}).";
            }

            carrito nuevoItem = null;

            if (itemEnCarrito != null)
            {
                itemEnCarrito.cantidad += cantidadFinal;
            }
            else
            {
                int nuevoId = db.carrito.Any() ? db.carrito.Max(c => c.id) + 1 : 1;

                nuevoItem = new carrito
                {
                    id = nuevoId,
                    cliente_id = clienteId,
                    perfume_id = perfumeId,
                    cantidad = cantidadFinal
                };
                db.carrito.Add(nuevoItem);
            }

            db.SaveChanges();

            var itemActual = itemEnCarrito ?? nuevoItem;
            var vm = CarritoHelper.BuildItemViewModel(itemActual, stockDisponible);

            return Json(new
            {
                nombre = vm.Nombre,
                imagen = vm.Imagen,
                tipo = vm.TipoDePerfume,
                presentacion = vm.Presentacion,
                genero = vm.Genero,
                precioOriginal = vm.PrecioOriginal.ToString("N0"),
                precioDescuento = vm.MostrarPrecioTachado
                                    ? vm.PrecioConDescuento?.ToString("N0")
                                    : null,
                tienePromo = vm.TienePromo,
                leyenda = vm.LeyendaPromo,
                mensaje = mensaje // 👈 mensaje opcional para mostrar en view
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

        [HttpPost]
        public JsonResult EliminarPerfume(int perfumeId)
        {
            if (Session["clienteId"] == null)
                return Json(new { success = false });

            int clienteId = (int)Session["clienteId"];

            var item = db.carrito.FirstOrDefault(c => c.cliente_id == clienteId && c.perfume_id == perfumeId);
            if (item != null)
            {
                db.carrito.Remove(item);
                db.SaveChanges();
            }

            var carrito = db.carrito
                .Where(c => c.cliente_id == clienteId)
                .Include(c => c.perfume)
                .Include(c => c.perfume.promocion)
                .ToList();

            decimal subtotalDec = 0m;
            decimal totalDec = 0m;

            foreach (var c in carrito)
            {
                decimal p = ToDec(c.perfume.precio_en_pesos);
                int cant = c.cantidad;

                subtotalDec += p * cant;

                var promos = c.perfume.promocion
                    .Where(pr => pr.id != 1 && pr.activo && pr.fecha_inicio <= DateTime.Now && pr.fecha_fin >= DateTime.Now)
                    .ToList();

                bool tiene10 = promos.Any(pr => pr.descuento == 10);
                var promoCant = promos.Where(pr => pr.descuento > 10).OrderByDescending(pr => pr.descuento).FirstOrDefault();

                if (promoCant != null && cant >= 2)
                {
                    int pares = cant / 2;
                    int resto = cant % 2;

                    decimal descPar = (2m * p) * ((decimal)promoCant.descuento / 100m);
                    decimal descResto = (resto == 1 && tiene10) ? 0.10m * p : 0m;

                    decimal descTotal = pares * descPar + descResto;
                    totalDec += (p * cant) - descTotal;
                }
                else if (tiene10)
                {
                    totalDec += (p * cant * 0.90m);
                }
                else
                {
                    totalDec += (p * cant);
                }
            }

            decimal descuentoDec = subtotalDec - totalDec;

            return Json(new
            {
                success = true,
                perfumeId,

                subtotal_cents = ToCents(subtotalDec),
                total_cents = ToCents(totalDec),
                descuento_cents = ToCents(descuentoDec),

                envioGratis = totalDec >= 70000m
            });
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

       


       /* private ItemCarritoViewModel BuildItemViewModel(carrito item, int stockDisponible)
        {
            var perfume = item.perfume;
            int cantidad = item.cantidad;

            //-- Promos vigentes
            var promos = perfume.promocion
                .Where(p => p.id != 1 && p.activo &&
                            p.fecha_inicio <= DateTime.Now && p.fecha_fin >= DateTime.Now)
                .ToList();

            var promo10 = promos.FirstOrDefault(p => p.descuento == 10);
            var promoCantidad = promos.Where(p => p.descuento > 10)
                                       .OrderByDescending(p => p.descuento)
                                       .FirstOrDefault();

            double precioOriginal = perfume.precio_en_pesos;
            double precioConDescuento = precioOriginal;
            double total = 0;
            bool tienePromo = false;
            string leyendaPromo = "";
            bool mostrarPrecioTachado = false;

             //----------  REGLAS  ---------- 
            if (promoCantidad != null && cantidad >= 2)
            {
                tienePromo = true;

                int pares = (cantidad / 2) * 2;
                int impares = cantidad % 2;

                double pctCant = (100 - promoCantidad.descuento) / 100.0;
                double pct10 = 0.9;

                total = (pares * precioOriginal * pctCant) +
                        (impares * precioOriginal *
                                  (promo10 != null ? pct10 : 1.0));

                precioConDescuento = (impares == 0)
                    ? precioOriginal * pctCant                     // todas en promo-cantidad
                    : (promo10 != null ? precioOriginal * pct10    // impares caen en 10 %
                                       : precioOriginal);          // sin 10 %

                int desc2daUnidad = promoCantidad.descuento * 2;

                leyendaPromo =
                    $"<span style='color:black;'>{pares} unidades:</span> " +
                    (desc2daUnidad == 100
                        ? "Promoción 2 x 1"
                        : $"Promoción {desc2daUnidad}% de descuento en la segunda unidad");

                if (impares == 1 && promo10 != null)
                    leyendaPromo += "<br /><span style='color:black;'>1 unidad:</span> Promoción 10% OFF";

                if (impares == 1 && promo10 == null && stockDisponible > cantidad)
                    leyendaPromo += $"<br /><strong>¡Si agregás uno más lo llevás con el {desc2daUnidad}% de descuento!</strong>";
            }
            else if (promo10 != null)
            {
                tienePromo = true;
                precioConDescuento = precioOriginal * 0.9;
                total = precioConDescuento * cantidad;
                mostrarPrecioTachado = promo10 != null && (promoCantidad == null || cantidad < 2);
                leyendaPromo = "Promoción 10% OFF";

                if (promoCantidad != null && cantidad < 2 && stockDisponible > cantidad)
                {
                    int desc2da = promoCantidad.descuento * 2;
                    leyendaPromo += $"<br /><strong>Si llevás 2 iguales, el segundo tiene {desc2da}% de descuento</strong>";
                }
            }
            else if (promoCantidad != null)          // cantidad == 1 o impar sin 10 %
            {
                int desc2da = promoCantidad.descuento * 2;
                tienePromo = cantidad > 1;          // solo es promo cuando hay al menos 2

                if (cantidad == 1)
                {
                    total = precioOriginal;
                    leyendaPromo = $"<strong>Si llevás 2 iguales, el segundo tiene {desc2da}% de descuento</strong>";
                }
                else
                {
                    int pares = (cantidad / 2) * 2;
                    int impares = cantidad % 2;

                    double pctCant = (100 - promoCantidad.descuento) / 100.0;

                    total = (pares * precioOriginal * pctCant) +
                            (impares * precioOriginal);

                    // solo se aplica precio promo si TODAS las unidades entran en promo
                    precioConDescuento = (impares == 0)
                        ? precioOriginal * pctCant
                        : precioOriginal;

                    leyendaPromo =
                        $"<span style='color:black;'>{pares} unidades:</span> " +
                        (desc2da == 100
                            ? "Promoción 2 x 1"
                            : $"Promoción {desc2da}% de descuento en la segunda unidad");

                    if (impares == 1 && stockDisponible > cantidad)
                        leyendaPromo += $"<br /><strong>¡Si agregás uno más lo llevás con el {desc2da}% de descuento!</strong>";
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
                MostrarPrecioTachado = mostrarPrecioTachado
            };
        }*/

        // CarritoController.cs
        public ActionResult VistaPrevia()
        {
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];

            /* 1) Carrito actualizado desde BD */
            var carrito = db.carrito
                .Where(c => c.cliente_id == clienteId)
                .Include(c => c.perfume)
                .Include(c => c.perfume.promocion)
                .Include(c => c.perfume.stock)
                .ToList();

            /* 2) Stock neto x perfume (regla “-5”) */
            var stockDict = db.stock
                .Where(s => carrito.Select(c => c.perfume_id).Contains(s.perfume_id))
                .ToList()
                .GroupBy(s => s.perfume_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => Math.Max(0, s.cantidad - 5))
                );

            /* 3) Reutilizá TU helper central */
            var itemsVm = carrito.Select(c =>
                CarritoHelper.BuildItemViewModel(
                    c,
                    stockDict.ContainsKey(c.perfume_id) ? stockDict[c.perfume_id] : 0
                )
            ).ToList();





            // 4) Calculá totales con la lista resumida
            decimal subtotal = itemsVm.Sum(i => ToDec(i.PrecioOriginal) * i.Cantidad);
            decimal total = itemsVm.Sum(i => ToDec(i.Total));
            decimal descuento = subtotal - total;

            var vistaPreviaVm = new VistaPreviaPedidoViewModel
            {
                Items = itemsVm,
                Subtotal = (double)Trunc2(subtotal),
                Descuento = (double)Trunc2(descuento),
                Total = (double)Trunc2(total),
                EnvioGratis = total >= 70000m
            };

            return View(vistaPreviaVm);
        }


        private static decimal ToDec(double v) => (decimal)v;

        // TRUNCA a 2 decimales (para cuando necesites mostrar en el servidor)
        private static decimal Trunc2(decimal v) => decimal.Truncate(v * 100m) / 100m;

        // Convierte un decimal a centavos TRUNCANDO (sin redondeo)
        private static long ToCents(decimal v) => (long)decimal.Truncate(v * 100m);


    }
}
