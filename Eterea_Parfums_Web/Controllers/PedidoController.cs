using Eterea_Parfums_Web.Filters;
using Eterea_Parfums_Web.Helpers;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;



namespace Eterea_Parfums_Web.Controllers
{
    [ForzarPerfilCompleto]
    public class PedidoController : Controller
    {
        private etereaEntities7 db = new etereaEntities7();

        /*  [HttpPost]
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

              // ✅ Armar el texto del domicilio para mostrar
              string domicilioTexto = "";

              if (calle != null)
                  domicilioTexto += calle.nombre + " ";
              domicilioTexto += cliente.numeracion_calle;

              if (!string.IsNullOrEmpty(cliente.piso))
                  domicilioTexto += " Piso " + cliente.piso;
              if (!string.IsNullOrEmpty(cliente.departamento))
                  domicilioTexto += " Dpto. " + cliente.departamento;

              domicilioTexto += "\n";
              domicilioTexto += "C.P. " + cliente.codigo_postal + ", ";
              if (localidad != null)
                  domicilioTexto += localidad.nombre + ", ";
              if (provincia != null)
                  domicilioTexto += provincia.nombre;

              // ✅ Guardar en sesión
              Session["DomicilioDeEnvioTexto"] = domicilioTexto;

              // ✅ Cargar ítems del pedido
              var items = new List<ItemCarritoViewModel>();
              for (int i = 0; i < PerfumeIds.Count; i++)
              {
                  int perfumeId = PerfumeIds[i];
                  int cantidad = Cantidades[i];

                  var perfume = db.perfume
                      .Include(p => p.promocion)
                      .Include(p => p.tipo_de_perfume)
                      .Include(p => p.genero)
                      .Include(p => p.stock)
                      .FirstOrDefault(p => p.id == perfumeId);

                  if (perfume != null && cantidad > 0)
                  {
                      // Simular un objeto carrito para reusar el helper
                      var carritoFake = new carrito
                      {
                          perfume = perfume,
                          cantidad = cantidad
                      };

                      // Podés calcular stock real si querés, o dejarlo en 0
                      int stockDisponible = perfume.stock.Sum(s => s.cantidad) - 5;

                      var itemVM = CarritoHelper.BuildItemViewModel(carritoFake, stockDisponible);

                      items.Add(itemVM);
                  }
              }

              double subtotal = items.Sum(i => i.PrecioOriginal * i.Cantidad);
              double total = items.Sum(i => i.Total);
              double descuento = subtotal - total;
              bool envioGratis = total >= 50000;


              var model = new VistaPreviaPedidoViewModel
              {
                  Cliente = cliente,
                  Calle = calle,
                  Localidad = localidad,
                  Provincia = provincia,
                  Items = items,
                  Subtotal = subtotal,
                  Descuento = descuento,
                  Total = total,
                  EnvioGratis = envioGratis,
                  DomicilioDeEnvioTexto = domicilioTexto  // ✅ pasarlo al modelo también
              };

              return View(model);
          }*/
        [HttpGet]
        public ActionResult VistaPrevia()
        {
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];

            // Carga de domicilio en la vista
            if (Session["NuevoDomicilioEntrega"] != null)
            {
                Session["DomicilioDeEnvioTexto"] = Session["NuevoDomicilioEntrega"];
                
            }

            var carrito = db.carrito
                .Include(c => c.perfume)
                .Include(c => c.perfume.promocion)
                .Include(c => c.perfume.stock)
                .Where(c => c.cliente_id == clienteId)
                .ToList();

            var perfumeIds = carrito.Select(c => c.perfume_id).Distinct().ToList();

            var stockDict = db.stock
                .Where(s => perfumeIds.Contains(s.perfume_id) && s.sucursal_id == 1)
                .ToDictionary(
                    s => s.perfume_id,
                    s => Math.Max(0, s.cantidad - 5)
                );

            var items = carrito.Select(c =>
                CarritoHelper.BuildItemViewModel(
                    c,
                    stockDict.ContainsKey(c.perfume_id)
                        ? stockDict[c.perfume_id]
                        : 0)).ToList();

            double subtotal = items.Sum(x => x.TotalSinDescuento);
            double total = items.Sum(x => x.Total);
            double descuento = items.Sum(x => x.DescuentoAplicado);

            // Dirección de envío
            string domicilio;
            if (Session["DomicilioDeEnvioTexto"] != null)
            {
                domicilio = Session["DomicilioDeEnvioTexto"].ToString();
            }
            else
            {
                var cliente = db.cliente
                    .Include(c => c.calle)
                    .Include(c => c.localidad)
                    .Include(c => c.localidad.provincia)
                    .FirstOrDefault(c => c.id == clienteId);

                domicilio = cliente != null
                    ? DireccionHelper.ConstruirTextoCompleto(
                        cliente.calle?.nombre ?? "",
                        cliente.numeracion_calle,
                        cliente.piso,
                        cliente.departamento,
                        cliente.codigo_postal,
                        cliente.localidad?.nombre ?? "",
                        cliente.localidad?.provincia?.nombre ?? "")
                    : "Domicilio no disponible.";

                // ✅ Guardamos el domicilio en sesión para que esté disponible en SimularPago
                Session["NuevoDomicilioEntrega"] = domicilio;
                Session["DomicilioDeEnvioTexto"] = domicilio;
            }

            var model = new VistaPreviaPedidoViewModel
            {
                Items = items,
                Subtotal = subtotal,
                Total = total,
                Descuento = descuento,
                EnvioGratis = total >= 70000,
                DomicilioDeEnvioTexto = domicilio
            };

            foreach (var i in items)
            {
                System.Diagnostics.Debug.WriteLine($"PerfumeId: {i.PerfumeId} | PrecioOriginal: {i.PrecioOriginal} | Cantidad: {i.Cantidad} | Total: {i.Total} | PrecioConDescuento: {i.PrecioConDescuento} | Promo: {i.LeyendaPromo}");
            }

            return View(model);
        }



        //[HttpPost]
        /*public async Task<ActionResult> IrAPagar(List<ItemResumenPedidoViewModel> productos, string montoFinal)
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

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "TEST-5038567099517736-070123-746239afa62e81d9d67bce507d09076f-130528138");
                var content = new StringContent(JsonConvert.SerializeObject(preference), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.mercadopago.com/checkout/preferences", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    string initPoint = result.init_point;
                    return Redirect(initPoint);
                }
                else
                {
                    TempData["ErrorPago"] = "No se pudo iniciar el pago. Intente nuevamente.";
                    return RedirectToAction("Index", "Carrito");
                }
            }
        }*/




        public ActionResult PagoExitoso(int numOrden, double totalFinal)
        {
            ViewBag.NumOrden = numOrden;
            ViewBag.TotalFinal = totalFinal;
            return View();
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
            // Opción para crear preferencia de MercadoPago o iniciar el proceso de pago
            
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



        public ActionResult SimularPago(string monto)
        {
            System.Diagnostics.Debug.WriteLine($"[SimularPago] raw query monto='{monto}'");

            // 1) Parse del monto (Invariant primero)
            decimal m;
            if (!decimal.TryParse(monto, NumberStyles.Any, CultureInfo.InvariantCulture, out m))
                decimal.TryParse(monto, NumberStyles.Any, CultureInfo.GetCultureInfo("es-AR"), out m);

            // 2) Saldo random por sesión (100.000 a 800.000)
            if (Session["SaldoCuentaDemo"] == null)
            {
                // Semilla estable por sesión para no cambiar en cada refresh
                var seed = unchecked(Environment.TickCount + (Session.SessionID?.GetHashCode() ?? 0));
                var rnd = new Random(seed);
                // Next(min, maxExclusive) → usamos 800001 para incluir 800000
                var saldo = rnd.Next(100000, 800001);
                Session["SaldoCuentaDemo"] = (double)saldo;
            }
            double saldoCuenta = (double)Session["SaldoCuentaDemo"];

            var vm = new SimularPagoViewModel
            {
                Monto = (double)m,
                SaldoCuenta = saldoCuenta
            };

            System.Diagnostics.Debug.WriteLine($"[SimularPago] parsed m={m} | saldo={saldoCuenta}");
            return View(vm);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarPago(string medio, int cuotas, string totalFinal)
        {
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];

            // Parse robusto del total final recibido del form (hdTotal), que viene en INVARIANT (punto)
            decimal totalFinalDec;
            if (!decimal.TryParse(totalFinal, NumberStyles.Any, CultureInfo.InvariantCulture, out totalFinalDec))
            {
                // fallback es-AR por las dudas
                if (!decimal.TryParse(totalFinal, NumberStyles.Any, CultureInfo.GetCultureInfo("es-AR"), out totalFinalDec))
                {
                    TempData["ErrorPago"] = "No se pudo interpretar el total a pagar.";
                    return RedirectToAction("Index", "Carrito");
                }
            }

            using (var db = new etereaEntities7())
            using (var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    /* 1) Cliente y carrito */
                    var cliente = db.cliente
                                    .Include(c => c.calle)
                                    .Include(c => c.localidad)
                                    .FirstOrDefault(c => c.id == clienteId);

                    var carrito = db.carrito
                                    .Include(c => c.perfume)
                                    .Include(c => c.perfume.promocion)
                                    .Where(c => c.cliente_id == clienteId)
                                    .ToList();

                    if (!carrito.Any())
                        throw new InvalidOperationException("El carrito está vacío");

                    /* 2) Promos y descuentos (quedan en double, pero al final casteamos a decimal para cuentas finas) */
                    var detallesTmp = new List<DetalleTmp>();
                    double descuentoTotalDouble = 0;

                    foreach (var item in carrito)
                    {
                        var promosVigentes = item.perfume.promocion
                            .Where(p => p.activo && p.fecha_inicio <= DateTime.Today && p.fecha_fin >= DateTime.Today)
                            .ToList();

                        var promoDiez = promosVigentes.FirstOrDefault(p => p.descuento == 10);
                        var promoMayor = promosVigentes.FirstOrDefault(p => p.descuento > 10);

                        int cantidad = item.cantidad;
                        double precioUnitario = item.perfume.precio_en_pesos;
                        double descItem = 0;
                        int? p1 = null;
                        int? p2 = null;

                        if (promoMayor != null && cantidad >= 2)
                        {
                            int pares = cantidad / 2;
                            descItem += pares * (promoMayor.descuento / 100.0) * precioUnitario * 2;
                            p2 = promoMayor.id;
                            cantidad -= pares * 2;
                        }

                        if (promoDiez != null && cantidad > 0)
                        {
                            descItem += cantidad * (promoDiez.descuento / 100.0) * precioUnitario;
                            p1 = promoDiez.id;
                        }

                        descuentoTotalDouble += descItem;

                        detallesTmp.Add(new DetalleTmp
                        {
                            CarritoItem = item,
                            Promo1Id = p1,
                            Promo2Id = p2
                        });
                    }

                    /* 3) Cálculos finales en DECIMAL */
                    decimal subtotalOriginal = carrito
                        .Select(i => (decimal)i.perfume.precio_en_pesos * i.cantidad)
                        .Sum();

                    decimal descuentoTotal = (decimal)descuentoTotalDouble;
                    decimal recargoTotal = (decimal)GetRecargo(medio, cuotas,
                                                (double)subtotalOriginal, (double)descuentoTotal);

                    decimal totalCalculado = subtotalOriginal - descuentoTotal + recargoTotal;

                    // Comparación en DECIMAL
                    if (Math.Abs(totalCalculado - totalFinalDec) > 0.01m)
                        throw new InvalidOperationException("Los totales no coinciden");

                    /* 4) Tipo y numeración de factura */
                    string tipoFactura = cliente.condicion_frente_al_iva == "Responsable Inscripto" ? "A" : "B";

                    // Para la WEB, el punto de venta será 0004
                    string puntoVenta = "0004";

                    string numFactura = GenerarNumeroFactura(db, tipoFactura, puntoVenta);

                    int nuevoIdFactura = db.factura.Any()
                        ? db.factura.Max(f => f.id) + 1
                        : 1;

                    /* 5) FACTURA */
                    var formaPagoTexto = MapearFormaDePago(medio);

                    var fac = new factura
                    {
                        id = nuevoIdFactura,
                        fecha = DateTime.Now,
                        sucursal_id = 0,
                        empleado_id = 1,
                        cliente_id = clienteId,
                        forma_de_pago = formaPagoTexto,
                        precio_total = (double)totalCalculado, 
                        recargo_tarjeta = (double)recargoTotal,
                        descuento = (double)descuentoTotal,
                        numero_de_caja = 10,
                        tipo_de_consumidor = cliente.condicion_frente_al_iva,
                        origen = "web",
                        factura_pdf = null,
                        num_factura = numFactura,
                        tipo_de_factura = tipoFactura
                    };
                    db.factura.Add(fac);
                    db.SaveChanges();

                    /* 6) DETALLE_FACTURA */
                    foreach (var d in detallesTmp)
                    {
                        int cantidadRestante = d.CarritoItem.cantidad;

                        var stock = db.stock.FirstOrDefault(s =>
                            s.perfume_id == d.CarritoItem.perfume_id &&
                            s.cantidad > 5 &&
                            s.sucursal_id == 1);

                        if (stock != null)
                        {
                            int stockDisponibleWeb = stock.cantidad - 5;
                            if (stockDisponibleWeb >= cantidadRestante)
                            {
                                stock.cantidad -= cantidadRestante;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Stock insuficiente para el perfume ID {d.CarritoItem.perfume_id}");
                            }
                        }

                        db.detalle_factura.Add(new detalle_factura
                        {
                            factura_id = fac.id,
                            perfume_id = d.CarritoItem.perfume_id,
                            cantidad = d.CarritoItem.cantidad,
                            precio_unitario = d.CarritoItem.perfume.precio_en_pesos,
                            promocion_id = d.Promo1Id ?? 1,
                            promocion2_id = d.Promo2Id
                        });
                    }
                    db.SaveChanges();

                    int nuevoIdOrden = db.orden.Any()
                        ? db.orden.Max(o => o.numero_de_orden) + 1
                        : 1;

                    string domicilioEnvio = Session["NuevoDomicilioEntrega"]?.ToString();
                    if (string.IsNullOrWhiteSpace(domicilioEnvio))
                    {
                        TempData["ErrorPago"] = "No se pudo determinar el domicilio de envío. Por favor, seleccioná o cargá uno.";
                        return RedirectToAction("PagoFallido", "Pedido");
                    }

                    /* 7) ORDEN */
                    db.orden.Add(new orden
                    {
                        numero_de_orden = nuevoIdOrden,
                        factura_id = fac.id,
                        nombre_cliente = cliente.nombre,
                        apellido_cliente = cliente.apellido,
                        dni = cliente.dni,
                        e_mail_cliente = cliente.e_mail,
                        domicilio_de_envio = domicilioEnvio,
                        estado = true,
                        codigo_despacho = null,
                        fecha_creacion = DateTime.Now
                    });
                    db.SaveChanges();

                    /* 8) Limpiar carrito y commit */
                    db.carrito.RemoveRange(carrito);
                    db.SaveChanges();

                    // Generación PDF + correo (tal cual lo tenías)...
                    string htmlFactura = (tipoFactura == "A")
                        ? FacturaHelper.GenerarHtmlFacturaA(db, fac, cliente,cuotas)
                        : FacturaHelper.GenerarHtmlFacturaB(db, fac, cliente,cuotas);

                    byte[] pdfBytes = FacturaHelper.GenerarFacturaPdf(htmlFactura);

                    string nombreArchivo = $"Factura_{numFactura}.pdf";
                    string rutaRelativa = $"~/Facturas/{nombreArchivo}";
                    string rutaDelPdfGenerado = Server.MapPath(rutaRelativa);
                    Directory.CreateDirectory(Path.GetDirectoryName(rutaDelPdfGenerado));
                    System.IO.File.WriteAllBytes(rutaDelPdfGenerado, pdfBytes);

                    Task.Run(() =>
                    {
                        CorreoHelper.EnviarCorreoConfirmacionPedido(
                            emailDestino: cliente.e_mail,
                            nombreCliente: cliente.nombre,
                            numeroFactura: numFactura,
                            total: (double)totalCalculado,
                            rutaPdf: rutaDelPdfGenerado
                        );
                    });

                    tx.Commit();

                    return RedirectToAction("PagoExitoso", "Pedido", new
                    {
                        numOrden = nuevoIdOrden,
                        totalFinal = totalFinalDec.ToString(CultureInfo.InvariantCulture)
                    });
                }
                catch (Exception ex)
                {
                    try { tx.Rollback(); } catch { }
                    System.Diagnostics.Debug.WriteLine("Error al procesar la venta: " + ex.Message);
                    TempData["ErrorPago"] = "Ocurrió un problema al procesar la venta ";
                    return RedirectToAction("Index", "Carrito");
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

        private double GetRecargo(string medio, int cuotas, double baseTotal, double descuentoTotal)
        {
            // Total sobre el que se calcula interés = total con descuento aplicado
            double baseConDescuento = baseTotal - descuentoTotal;

            if (cuotas <= 1 || baseConDescuento <= 0)
                return 0;

            // Tablas de tasas por medio y cuotas
            var tablaVisaMc = new Dictionary<int, double>
    {
        { 1, 0.00 }, // sin interés
        { 3, 0.00 }, // sin interés
        { 6, 0.10 }, // 10%
        { 9, 0.15 }, // 15%
        { 12, 0.20 } // 20%
    };

            var tablaAmex = new Dictionary<int, double>
    {
        { 1, 0.00 }, // sin interés
        { 6, 0.10 }, // 10%
        { 12, 0.20 } // 20%
    };

            Dictionary<int, double> tabla = null;

            // Medios con cuotas
            if (medio == "MC" || medio == "VISC")
                tabla = tablaVisaMc;
            else if (medio == "AMEX")
                tabla = tablaAmex;
            else
                return 0; // Mercado Pago, Visa Débito y otros → sin recargo

            if (!tabla.TryGetValue(cuotas, out double tasa))
                return 0;

            return baseConDescuento * tasa;
        }

        private string GenerarNumeroFactura(etereaEntities7 db, string tipo, string puntoVenta)
        {
            // puntoVenta debe venir con 4 dígitos: "0001", "0002", "0003", "0004"
            string prefijo = puntoVenta;

            // Traemos el último num_factura del mismo tipo y mismo punto de venta
            string ultimo = db.factura
                              .Where(f => f.tipo_de_factura == tipo
                                       && f.num_factura.StartsWith(prefijo))
                              .OrderByDescending(f => f.id)   // o OrderByDescending(f => f.num_factura)
                              .Select(f => f.num_factura)
                              .FirstOrDefault();

            int correlativo = 0;

            if (!string.IsNullOrEmpty(ultimo) && ultimo.Length >= 12)
            {
                // Ej.: “000400000123”  →  “00000123” (últimos 8 dígitos)
                string parteNumerica = ultimo.Substring(4);   // desde la posición 4, 8 dígitos
                int.TryParse(parteNumerica, out correlativo);
            }

            correlativo += 1;

            // Devuelve algo como "0004" + "00000001"
            return $"{puntoVenta}{correlativo:D8}";
        }

        private string ConstruirDireccionEnvio(etereaEntities7 db, cliente cli)
        {
            if (Session["NuevoDomicilioEntrega"] != null)
            {
                return Session["NuevoDomicilioEntrega"].ToString();
            }

            // Si no hay dirección en sesión, lanzamos una excepción controlada
            throw new InvalidOperationException("No se ha definido un domicilio de envío en la sesión.");
        }

        private string MapearFormaDePago(string medio)
        {
            switch (medio)
            {
                case "MP":
                    return "Mercado Pago";

                case "VSD":
                    return "Visa Débito";

                case "VISC":
                    return "Visa Crédito";

                case "MC":
                    return "Mastercard";

                case "AMEX":
                    return "Amex";

                default:
                    return "Otro";
            }
        }


    }
}