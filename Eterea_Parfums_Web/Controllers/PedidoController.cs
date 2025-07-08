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

namespace Eterea_Parfums_Web.Controllers
{
    public class PedidoController : Controller
    {
        private etereaEntities1 db = new etereaEntities1();

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
            return RedirectToAction("Index", "Carrito");
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

        // GET: Pedido/SimularPago
        public ActionResult SimularPago(double monto)
        {
            var model = new SimularPagoViewModel
            {
                Monto = monto,
                Usuario = "AdriCamp"   // o leés el nombre de la sesión
            };
            return View(model);
        }

        [HttpPost]
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
        }



    }
}
