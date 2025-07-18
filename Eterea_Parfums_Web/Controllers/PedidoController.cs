using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;
using System.Data.Entity;
using Eterea_Parfums_Web.Helpers;


namespace Eterea_Parfums_Web.Controllers
{
    public class PedidoController : Controller
    {
        private etereaEntities7 db = new etereaEntities7();

        [HttpPost]
        public ActionResult VistaPrevia(List<int> PerfumeIds, List<int> Cantidades, double Subtotal, double Descuento, double Total)
        {
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cuenta");
            }

            if (PerfumeIds == null || Cantidades == null || PerfumeIds.Count != Cantidades.Count)
            {
                return RedirectToAction("Index", "Carrito");
            }

            int clienteId = (int)Session["clienteId"];

            var cliente = db.cliente.FirstOrDefault(c => c.id == clienteId);
            var calle = db.calle.FirstOrDefault(c => c.id == cliente.calle_id);
            var localidad = db.localidad.FirstOrDefault(l => l.id == cliente.localidad_id);
            var provincia = db.provincia.FirstOrDefault(p => p.id == cliente.provincia_id);

            var items = new List<ItemResumenPedidoViewModel>();

            for (int i = 0; i < PerfumeIds.Count; i++)
            {
                int perfumeId = PerfumeIds[i];
                int cantidad = Cantidades[i];

                var perfume = db.perfume.FirstOrDefault(p => p.id == perfumeId);
                if (perfume != null && cantidad > 0)
                {
                    items.Add(new ItemResumenPedidoViewModel
                    {
                        PerfumeId = perfume.id,
                        Nombre = perfume.nombre,
                        Imagen = perfume.imagen1,
                        Presentacion = perfume.presentacion_ml,
                        Cantidad = cantidad,
                        Precio = perfume.precio_en_pesos
                    });
                }
            }

            double subtotal = Subtotal;
            double descuento = Descuento;
            double total = Total;
            bool envioGratis = total >= 50_000;        // regla de negocio

        

            var model = new VistaPreviaPedidoViewModel
            {
                Cliente = cliente,
                Calle = calle,
                Localidad = localidad,
                Provincia = provincia,
                Items = items,           // <- ya compila, tipado correcto
                Subtotal = subtotal,
                Descuento = descuento,
                Total = total,
                EnvioGratis = envioGratis     // <- variable definida arriba
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult VistaPrevia()
        {
            /* ─────────────────────── Validación de sesión ────────────────────── */
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];

            /* ─────────── 1) Carrito con relaciones necesarias ──────────── */
            var carrito = db.carrito
                .Include(c => c.perfume)
                .Include(c => c.perfume.promocion)
                .Include(c => c.perfume.stock)
                .Where(c => c.cliente_id == clienteId)
                .ToList();

            /* ─────────── 2) Stock neto por perfume (regla “–5”) ─────────── */
            var perfumeIds = carrito
                .Select(c => c.perfume_id)
                .Distinct()
                .ToList();                   // materializamos como List<int>

            var stockDict = db.stock
                .Where(s => perfumeIds.Contains(s.perfume_id))
                .ToList()
                .GroupBy(s => s.perfume_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => Math.Max(0, s.cantidad - 5))
                );

            /* ─────────── 3) Ítems del carrito usando el helper ──────────── */
            var items = carrito
                .Select(c => CarritoHelper.BuildItemViewModel(
                    c,
                    stockDict.ContainsKey(c.perfume_id)
                        ? stockDict[c.perfume_id]
                        : 0))
                .ToList();

            /* ─────────── 4) Mapear a ViewModel para la vista ───────────── */
            var itemsVm = items.Select(i => new ItemResumenPedidoViewModel
            {
                PerfumeId = i.PerfumeId,
                Nombre = i.Nombre,
                Imagen = i.Imagen,
                Presentacion = i.Presentacion,
                Cantidad = i.Cantidad,

                Precio = i.PrecioConDescuento ?? i.PrecioOriginal,
                           
            }).ToList();

            /* ─────────── 5) Totales globales ───────────────────────────── */

            double subtotal = items.Sum(x => x.PrecioOriginal * x.Cantidad);
            double total = itemsVm.Sum(x => x.Total);   // 👈 usa el correcto
            double descuento = subtotal - total;

            /* ─────────── 6) Datos de dirección del cliente ─────────────── */
            var cliente = db.cliente
                .Include(cl => cl.calle)
                .Include(cl => cl.localidad)
                .Include(cl => cl.localidad.provincia)
                .First(cl => cl.id == clienteId);

            var vm = new VistaPreviaPedidoViewModel
            {
                Cliente = cliente,
                Calle = cliente.calle,
                Localidad = cliente.localidad,
                Provincia = cliente.localidad?.provincia,

                Items = itemsVm,      // ItemsCarrito alias funciona igual
                Subtotal = subtotal,
                Descuento = descuento,
                Total = total,
                EnvioGratis = total >= 50_000
            };

            return View(vm);
        }
    

    [HttpPost]
        public async Task<ActionResult> IrAPagar(List<ItemResumenPedidoViewModel> productos, string montoFinal)
        {
            if (string.IsNullOrWhiteSpace(montoFinal))
            {
                TempData["ErrorPago"] = "No se pudo iniciar el pago. montoFinal vacío.";
                return RedirectToAction("Index", "Carrito");
            }

            decimal montoDecimal;
            bool ok = decimal.TryParse(
                montoFinal.Replace(",", "."), // Fuerza punto decimal
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out montoDecimal
            );

            if (!ok)
            {
                TempData["ErrorPago"] = "No se pudo interpretar el monto.";
                return RedirectToAction("Index", "Carrito");
            }

            // 👇 AQUÍ va esta parte (una vez que montoDecimal está listo)
            var items = new[]
            {
        new
        {
            title = "Compra en Etérea Parfums",
            quantity = 1,
            unit_price = montoDecimal,
            currency_id = "ARS"
        }
    };

            var preference = new
            {
                items = items,
                back_urls = new
                {
                    success = Url.Action("PagoExitoso", "Pedido", null, Request.Url.Scheme),
                    failure = Url.Action("PagoFallido", "Pedido", null, Request.Url.Scheme),
                    pending = Url.Action("PagoPendiente", "Pedido", null, Request.Url.Scheme)
                },
                auto_return = "approved"
            };

            Console.WriteLine(preference.back_urls.success);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "TEST-5038567099517736-070123-746239afa62e81d9d67bce507d09076f-130528138");
                var content = new StringContent(JsonConvert.SerializeObject(preference), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.mercadopago.com/checkout/preferences", content);

                Console.WriteLine(response.StatusCode);


                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    string initPoint = result.init_point;

                    Console.WriteLine(initPoint);

                    return Redirect(initPoint);
                }
                else
                {
                    TempData["ErrorPago"] = "No se pudo iniciar el pago. Intente nuevamente.";
                    return RedirectToAction("Index", "Carrito");
                }
            }
        }




        public ActionResult PagoExitoso()
        {
            return Content("¡Pago exitoso! Gracias por tu compra.");
        }

        public ActionResult PagoFallido()
        {
            return View(); // ASP.NET buscará automáticamente 'Views/Pedido/PagoFallido.cshtml'
        }

        public ActionResult PagoPendiente()
        {
            return View(); // Vista con mensaje de pago pendiente
        }
    


    public ActionResult IrAPagar()
        {
            // Lógica para crear preferencia de MercadoPago o iniciar el proceso de pago
            // Por ahora, podés redirigir a MercadoPago directamente para testeo
            return Redirect("https://www.mercadopago.com.ar/checkout");
        }


        // GET: Pedido
        public ActionResult Index()
        {
            return View();
        }

        // GET: Pedido/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Pedido/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Pedido/Create
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

        // GET: Pedido/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Pedido/Edit/5
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

        // GET: Pedido/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Pedido/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Pedido/SimularPago
        /*public ActionResult RealizarPago(double monto)
        {
            var model = new SimularPagoViewModel
            {
                Monto = monto,
                Usuario = "AdriCamp"   // o leés el nombre de la sesión
            };
            return View(model);
        }*/

        public ActionResult SimularPago(string monto)
        {
            double total = double.Parse(monto, CultureInfo.InvariantCulture);

            var rand = new Random();
            double saldo = rand.Next(50_000, 300_001);

            var model = new SimularPagoViewModel
            {
                Monto = total,
                SaldoCuenta = saldo,
                Usuario = Session["nombreUsuario"]?.ToString() ?? "Invitado"
            };
            return View(model);
        }


      /*  [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarPago()
        {
            var items = Session["PedidoItems"] as List<PedidoItemVM>;
            if (items == null || !items.Any())
            {
                TempData["ErrorPago"] = "No se encontró el pedido en sesión.";
                return RedirectToAction("Index", "Carrito");
            }

            int clienteId = (int)Session["clienteId"];   // ← id del cliente logueado

            using (var tx = db.Database.BeginTransaction())
            {
                try
                {
                    // ---------- 1. Descontar stock -----------------------------
                    foreach (var it in items)
                    {
                        int restante = it.Cantidad;
                        while (restante > 0)
                        {
                            var st1 = db.stock.FirstOrDefault(s => s.perfume_id == it.PerfumeId &&
                                                                   s.sucursal_id == 1);
                            if (st1 != null && st1.cantidad > 5)
                            {
                                st1.cantidad--;
                                restante--;
                                continue;
                            }

                            var st2 = db.stock.FirstOrDefault(s => s.perfume_id == it.PerfumeId &&
                                                                   s.sucursal_id == 2);
                            if (st2 != null && st2.cantidad > 5)
                            {
                                st2.cantidad--;
                                restante--;
                                continue;
                            }

                            throw new InvalidOperationException(
                                $"No hay stock disponible para el perfume {it.PerfumeId}.");
                        }
                    }

                    // ---------- 2. Vaciar el carrito en la BD ------------------
                    var lineasCarrito = db.carrito.Where(c => c.cliente_id == clienteId).ToList();
                    db.carrito.RemoveRange(lineasCarrito);

                    // ---------- 3. Guardar y confirmar -------------------------
                    db.SaveChanges();
                    tx.Commit();

                    Session.Remove("PedidoItems");
                    TempData["PagoOK"] = "¡Pago aprobado, stock actualizado y carrito vaciado!";
                    return RedirectToAction("PagoExitoso");
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["ErrorPago"] = $"Error al procesar el pago: {ex.Message}";
                    return RedirectToAction("Index", "Carrito");
                }
            }
        }*/

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarPago(string medio, int cuotas, double totalFinal)
        {
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];

            using (var db = new etereaEntities7())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))

            {
                try
                {
                    /* 1) Cliente y carrito */
                    var cliente = db.cliente.Find(clienteId);

                    var carrito = db.carrito
                                    .Include(c => c.perfume)
                                    .Include(c => c.perfume.promocion)
                                    .Where(c => c.cliente_id == clienteId)
                                    .ToList();

                    if (!carrito.Any())
                        throw new InvalidOperationException("El carrito está vacío");

                    /* 2) Para cada ítem calculo promos y descuento */
                    var detallesTmp = new List<DetalleTmp>();   // buffer local
                    double descuentoTotal = 0;

                    foreach (var item in carrito)
                    {
                        // Promos vigentes
                        var promosVigentes = item.perfume.promocion
                                                       .Where(p => p.activo
                                                                 && p.fecha_inicio <= DateTime.Today
                                                                 && p.fecha_fin >= DateTime.Today)
                                                       .OrderByDescending(p => p.descuento)  // prioridad mayor % primero
                                                       .Take(2)                              // máx 2
                                                       .ToList();

                        int? p1 = promosVigentes.ElementAtOrDefault(0)?.id;
                        int? p2 = promosVigentes.ElementAtOrDefault(1)?.id;

                        // Descuento aplicado a este ítem
                        double descItem = 0;
                        foreach (var prm in promosVigentes)
                        {
                            // Ejemplo: descuento % sobre cada unidad
                            descItem += (prm.descuento / 100.0) * (item.perfume.precio_en_pesos * item.cantidad);
                        }
                        descuentoTotal += descItem;

                        // Guardo info temporal para la inserción posterior
                        detallesTmp.Add(new DetalleTmp
                        {
                            CarritoItem = item,
                            Promo1Id = p1,
                            Promo2Id = p2
                        });
                    }

                    /* 3) Cálculos finales */
                    double subtotalOriginal = carrito.Sum(i => i.perfume.precio_en_pesos * i.cantidad);
                    double recargoTotal = GetRecargo(medio, cuotas, subtotalOriginal);
                    double totalCalculado = subtotalOriginal - descuentoTotal + recargoTotal;

                    Console.Write("Resultado: " + Math.Round(totalFinal, 2));
                    Console.Write(Math.Round(Math.Round(totalCalculado, 2)));


                    if (Math.Round(totalCalculado, 2) != Math.Round(totalFinal, 2))
                        throw new InvalidOperationException("Los totales no coinciden");

                    /* 4) Tipo y numeración de factura */
                    string tipoFactura = cliente.condicion_frente_al_iva == "Responsable Inscripto" ? "A" : "B";
                    string numFactura = GenerarNumeroFactura(db, tipoFactura);

                    /* 5) FACTURA */
                    var fac = new factura
                    {
                        fecha = DateTime.Now,
                        sucursal_id = 0,
                        empleado_id = 0,
                        cliente_id = clienteId,
                        forma_de_pago = medio,
                        precio_total = totalCalculado,
                        descuento = descuentoTotal,
                        numero_de_caja = 10,
                        tipo_de_consumidor = cliente.condicion_frente_al_iva,
                        origen = "web",
                        factura_pdf = null,
                        num_factura = numFactura,
                        tipo_de_factura = tipoFactura
                    };
                    db.factura.Add(fac);
                    db.SaveChanges();   // fac.id listo

                    /* 6) DETALLE_FACTURA */
                    foreach (var d in detallesTmp)
                    {
                        // Si promocion_id NO es nullable en BD, reemplazá null con 0 o un valor dummy
                        db.detalle_factura.Add(new detalle_factura
                        {
                            factura_id = fac.id,
                            perfume_id = d.CarritoItem.perfume_id,
                            cantidad = d.CarritoItem.cantidad,
                            precio_unitario = d.CarritoItem.perfume.precio_en_pesos,
                            promocion_id = d.Promo1Id ?? 0,      // ← 0 si no hay promo1
                            promocion2_id = d.Promo2Id            // nullable
                        });
                    }
                    db.SaveChanges();

                    /* 7) ORDEN */
                    db.orden.Add(new orden
                    {
                        factura_id = fac.id,
                        nombre_cliente = $"{cliente.nombre} {cliente.apellido}",
                        dni = cliente.dni,
                        e_mail_cliente = cliente.e_mail,
                        domicilio_de_envio = ConstruirDireccionEnvio(db, cliente),
                        estado = true,
                        codigo_despacho = null,
                        fecha_creacion = DateTime.Now
                    });
                    db.SaveChanges();

                    /* 8) Limpiar carrito y commit */
                    db.carrito.RemoveRange(carrito);
                    db.SaveChanges();

                    tx.Commit();
                    return RedirectToAction("Exito", new { id = fac.id });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["ErrorPago"] = "Ocurrió un problema al procesar la venta.";
                    return RedirectToAction("Index","Carrito");
                }
            }
        }

        /* ---- helper temporal interno ----------------------------------- */
        private sealed class DetalleTmp
        {
            public carrito CarritoItem { get; set; }
            public int? Promo1Id { get; set; }
            public int? Promo2Id { get; set; }
        }

        /* === helpers ======================================================= */

        private double GetRecargo(string medio, int cuotas, double baseTotal)
        {
            if (medio != "MC" || cuotas == 1) return 0;

            var tabla = new Dictionary<int, double> { { 3, 0.10 }, { 6, 0.15 }, { 9, 0.18 }, { 12, 0.20 } };
            return baseTotal * (tabla.ContainsKey(cuotas) ? tabla[cuotas] : 0);
        }

        private string GenerarNumeroFactura(etereaEntities7 db, string tipo)
        {
            // Traemos el último num_factura del mismo tipo (‘A’ o ‘B’)
            string ultimo = db.factura
                              .Where(f => f.tipo_de_factura == tipo)
                              .OrderByDescending(f => f.id)          // o por fecha
                              .Select(f => f.num_factura)
                              .FirstOrDefault();

            int correlativo = 0;

            if (!string.IsNullOrEmpty(ultimo) && ultimo.Length > 1)
            {
                // Ej.: “A00000123”  →  “00000123”
                int.TryParse(ultimo.Substring(1), out correlativo);
            }

            correlativo += 1;

            // Devuelve “A00000124” ó “B00000001”
            return $"{tipo}{correlativo:D8}";
        }

        private string ConstruirDireccionEnvio(etereaEntities7 db, cliente cli)
        {
            var calle = db.calle.Find(cli.calle_id)?.nombre;
            var loc = db.localidad.Find(cli.localidad_id)?.nombre;
            var prov = db.provincia.Find(cli.provincia_id)?.nombre;

            return $"{calle} {cli.piso ?? ""} {cli.departamento ?? ""}, CP {cli.codigo_postal}, {loc}, {prov}";
        }


    }
}
