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
                Session.Remove("NuevoDomicilioEntrega"); // ya se usó
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
            }

            var model = new VistaPreviaPedidoViewModel
            {
                Items = items,
                Subtotal = subtotal,
                Total = total,
                Descuento = descuento,
                EnvioGratis = total >= 50000,
                DomicilioDeEnvioTexto = domicilio
            };

            foreach (var i in items)
            {
                System.Diagnostics.Debug.WriteLine($"PerfumeId: {i.PerfumeId} | PrecioOriginal: {i.PrecioOriginal} | Cantidad: {i.Cantidad} | Total: {i.Total} | PrecioConDescuento: {i.PrecioConDescuento} | Promo: {i.LeyendaPromo}");
            }

            return View(model);
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
                        var promosVigentes = item.perfume.promocion
                         .Where(p => p.activo && p.fecha_inicio <= DateTime.Today && p.fecha_fin >= DateTime.Today)
                         .ToList();

                        // Clasificamos por tipo de promo
                        var promoDiez = promosVigentes.FirstOrDefault(p => p.descuento == 10);
                        var promoMayor = promosVigentes.FirstOrDefault(p => p.descuento > 10);

                        int cantidad = item.cantidad;
                        double precioUnitario = item.perfume.precio_en_pesos;
                        double descItem = 0;
                        int? p1 = null;
                        int? p2 = null;

                        // Aplicamos promo mayor a 10% por cada par
                        if (promoMayor != null && cantidad >= 2)
                        {
                            int pares = cantidad / 2;
                            descItem += pares * (promoMayor.descuento / 100.0) * precioUnitario * 2;
                            p2 = promoMayor.id;
                            cantidad -= pares * 2; // Reducimos lo que queda por aplicar
                        }

                        // Aplicamos promo del 10% por unidad restante
                        if (promoDiez != null && cantidad > 0)
                        {
                            descItem += cantidad * (promoDiez.descuento / 100.0) * precioUnitario;
                            p1 = promoDiez.id;
                        }

                        descuentoTotal += descItem;

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

                    if (Math.Round(totalCalculado, 2) != Math.Round(totalFinal, 2))  //VER ESTE IF, LAS PROMOCIONES SE ESTAN APLICANDO MAL, SI COMPRAS 2 PERFUMES CON UNA PROMO
                        //DE 40% Y TIENE UN DESCUENTO DEL 10% TAMBIEN, SE APLICAN AMBOS POR ESO EL totalCalculado NO DA IGUAL QUE EL totalFinal
                        //Math.Round(totalCalculado, 2) != Math.Round(totalFinal, 2)
                        throw new InvalidOperationException("Los totales no coinciden");

                    /* 4) Tipo y numeración de factura */
                    string tipoFactura = cliente.condicion_frente_al_iva == "Responsable Inscripto" ? "A" : "B";
                    string numFactura = GenerarNumeroFactura(db, tipoFactura);

                    int nuevoIdFactura = db.factura.Any()
                     ? db.factura.Max(f => f.id) + 1   // último + 1
                     : 1;                              // tabla vacía → 1

                    /* 5) FACTURA */
                    var fac = new factura
                    {
                        id = nuevoIdFactura,
                        fecha = DateTime.Now,
                        sucursal_id = 1,
                        empleado_id = 1,
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
                    System.Diagnostics.Debug.WriteLine($"Factura creada: {fac.id}");

                    /* 6) DETALLE_FACTURA */
                    foreach (var d in detallesTmp)
                    {
                        int cantidadRestante = d.CarritoItem.cantidad;

                        // Trae una tabla con los datos del stock donde el id del perfume sea igual al perfume del carrito
                        var stock = db.stock
                        .FirstOrDefault(s => s.perfume_id == d.CarritoItem.perfume_id
                                          && s.cantidad > 5
                                          && s.sucursal_id == 1);


                        if (stock != null)
                        {
                            int stockDisponibleWeb = stock.cantidad - 5; // solo lo que excede el mínimo
                            if (stockDisponibleWeb >= cantidadRestante)
                            {
                                stock.cantidad -= cantidadRestante;
                            }
                            else // Si aún queda cantidad comprada sin descontar del stock, hace un roll back
                            {
                                throw new InvalidOperationException($"Stock insuficiente para el perfume ID {d.CarritoItem.perfume_id}");
                            }
                        }


                        // Si promocion_id NO es nullable en BD, reemplazá null con 0 o un valor dummy
                        db.detalle_factura.Add(new detalle_factura
                        {
                            factura_id = fac.id,
                            perfume_id = d.CarritoItem.perfume_id,
                            cantidad = d.CarritoItem.cantidad,
                            precio_unitario = d.CarritoItem.perfume.precio_en_pesos,
                            promocion_id = d.Promo1Id ?? 1,      //  1 si no hay promo1
                            promocion2_id = d.Promo2Id
                        });
                    }
                    db.SaveChanges();

                    int nuevoIdOrden = db.orden.Any()
                     ? db.orden.Max(o => o.numero_de_orden) + 1
                     : 1;
                    /* 7) ORDEN */
                    db.orden.Add(new orden
                    {
                        numero_de_orden = nuevoIdOrden,
                        factura_id = fac.id,
                        nombre_cliente = cliente.nombre,
                        apellido_cliente = cliente.apellido,
                        dni = cliente.dni,
                        e_mail_cliente = cliente.e_mail,
                        domicilio_de_envio = ConstruirDireccionEnvio(db, cliente),
                        estado = true,
                        codigo_despacho = null,
                        fecha_creacion = DateTime.Now
                    });
                    var ordenAgregada = db.orden.Local.Last();
                    Console.WriteLine($"Orden→  Factura:{ordenAgregada.factura_id}, Cliente:{ordenAgregada.nombre_cliente}, DNI:{ordenAgregada.dni}, Email:{ordenAgregada.e_mail_cliente}, Envío:{ordenAgregada.domicilio_de_envio}, Estado:{(ordenAgregada.estado ? "Activa" : "Inactiva")}, Fecha:{ordenAgregada.fecha_creacion:dd/MM/yyyy HH:mm:ss}");


                    db.SaveChanges();

                    /* 8) Limpiar carrito y commit */
                    db.carrito.RemoveRange(carrito);
                    db.SaveChanges();

                    tx.Commit();
                    Task.Run(() =>
                    {
                        CorreoHelper.EnviarCorreoConfirmacionPedido(
                            emailDestino: cliente.e_mail,
                            nombreCliente: cliente.nombre,
                            numeroFactura: numFactura,
                            total: totalCalculado
                        );
                    });

                    return RedirectToAction("PagoExitoso", "Pedido", new { numOrden = nuevoIdOrden, totalFibal = totalFinal });


                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    TempData["ErrorPago"] = "Ocurrió un problema al procesar la venta.";
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