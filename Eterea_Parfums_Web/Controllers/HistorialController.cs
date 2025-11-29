using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using Eterea_Parfums_Web.Filters;

namespace Eterea_Parfums_Web.Controllers
{
    public class HistorialController : Controller
    {
        [ForzarPerfilCompleto]

        // GET: Historial
        public ActionResult Index(int page = 1)
        {
            int pageSize = 5;
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }
            int clienteId = (int)Session["clienteId"];

            using (var db = new etereaEntities7())
            {
                var usuario = db.cliente.Find(clienteId);

                if (usuario == null)
                {
                    return RedirectToAction("Login", "Cliente");
                }

                var query = db.factura
                      .Where(f => f.cliente_id == clienteId)
                      .OrderByDescending(f => f.fecha);

                int totalFacturas = query.Count();

                var facturasPaginadas = query
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize)
                                        .ToList();
                var model = new HistorialViewModel
                {
                    Nombre = usuario.nombre,
                    Apellido = usuario.apellido,
                    Dni = usuario.dni.ToString(),
                    Email = usuario.e_mail,
                    Facturas = facturasPaginadas,
                    PaginaActual = page,
                    TotalPaginas = (int)Math.Ceiling((double)totalFacturas / pageSize)
                };

                return View(model);
            }
        }



        // GET: Historial/DetalleFactura
        public ActionResult DetalleFactura(int? factura_id)
        {
            if (factura_id == null || factura_id == 0)
            {
                TempData["Error"] = "Página inválida o factura no especificada.";
                return RedirectToAction("Index", "Historial");
            }
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }

            using (var db = new etereaEntities7())
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

                    // Cargar promoción principal y secundaria (si existe)
                    var promo1 = db.promocion.FirstOrDefault(promo => promo.id == d.promocion_id);
                    var promo2 = db.promocion.FirstOrDefault(promo => promo.id == d.promocion2_id);

                    // Armar el texto a mostrar en "Promoción:"
                    string textoPromo;
                    var nombresPromo = new List<string>();

                    if (promo1 != null && promo1.id != 1) nombresPromo.Add(promo1.nombre);
                    if (promo2 != null && promo2.id != 1 && (promo2.id != promo1?.id)) nombresPromo.Add(promo2.nombre);

                    if (!nombresPromo.Any())
                        textoPromo = "Sin promoción";
                    else
                        textoPromo = string.Join(" + ", nombresPromo);

                    // Determinar si tiene promo del 10% y/o promo por cantidad
                    // ⚠️ ADAPTAR si tus promos se identifican de otra forma
                    int? d1 = promo1?.descuento;
                    int? d2 = promo2?.descuento;

                    bool tienePromo10 =
                        (d1.HasValue && d1.Value == 10) ||
                        (d2.HasValue && d2.Value == 10);

                    bool tienePromoCantidad =
                        (d1.HasValue && d1.Value > 10) ||
                        (d2.HasValue && d2.Value > 10);

                    int descuentoCantidad = 0;
                    if (tienePromoCantidad)
                    {
                        var descs = new List<int>();
                        if (d1.HasValue && d1.Value > 10) descs.Add(d1.Value);
                        if (d2.HasValue && d2.Value > 10) descs.Add(d2.Value);
                        descuentoCantidad = descs.Max(); // si hay más de una promo por cantidad, uso la de mayor descuento
                    }

                    decimal precioUnitario = (decimal)d.precio_unitario;
                    int cantidad = d.cantidad;

                    // Calcular total de la línea con promos aplicadas
                    decimal totalLinea = CalcularTotalLinea(
                        cantidad,
                        precioUnitario,
                        tienePromo10,
                        tienePromoCantidad,
                        descuentoCantidad
                    );

                    perfumes.Add(new DetallePerfumeViewModel
                    {
                        Id = perfumeData.id,
                        Imagen1 = perfumeData.imagen1,
                        Nombre = perfumeData.nombre,
                        Marca = marca?.nombre ?? "Sin marca",
                        Tamaño = perfumeData.presentacion_ml,
                        PrecioUnitario = precioUnitario,
                        Cantidad = cantidad,
                        Promocion = textoPromo,
                        PrecioTotal = totalLinea
                    });
                }

                // El total de la factura que mostrás abajo debería ser el que realmente se pagó:
                // dbo.factura.precio_total (incluye recargo por tarjeta si lo hubo)
                decimal totalPagado = (decimal)factura.precio_total;

                var viewModel = new DetalleFacturaViewModel
                {
                    NumeroFactura = factura.num_factura.ToString(),
                    Fecha = factura.fecha,
                    Perfumes = perfumes,
                    PrecioTotal = totalPagado,

                    //datos para mostrar el recargo
                    RecargoTarjeta = (decimal)factura.recargo_tarjeta,
                    FormaDePago = factura.forma_de_pago
                };

                return View(viewModel);
            }
        }

        private decimal CalcularTotalLinea(
        int cantidad,
        decimal precioUnitario,
        bool tienePromo10,
        bool tienePromoCantidad,
        int descuentoCantidad // porcentaje de descuento global sobre el PAR (columna promocion.descuento)
    )
            {
                // Sin promos
                if (!tienePromo10 && !tienePromoCantidad)
                    return cantidad * precioUnitario;

                decimal total = 0m;

                // Solo 10% OFF
                if (tienePromo10 && !tienePromoCantidad)
                {
                    var factor10 = 1m - (10m / 100m);
                    total = cantidad * precioUnitario * factor10;
                    return Math.Round(total, 2, MidpointRounding.AwayFromZero);
                }

                // Solo promo por cantidad (segunda unidad con x% de descuento, guardado como
                // descuento total sobre el par en promocion.descuento: ej. 40 => 80% off segunda unidad)
                if (!tienePromo10 && tienePromoCantidad)
                {
                    int pares = cantidad / 2;
                    int resto = cantidad % 2;

                    var factorPar = 1m - (descuentoCantidad / 100m); // descuento global sobre el par
                    total += pares * (2 * precioUnitario * factorPar);
                    total += resto * precioUnitario;

                    return Math.Round(total, 2, MidpointRounding.AwayFromZero);
                }

                // Tiene LAS DOS promos:
                // - en los pares usamos la promo por cantidad
                // - si hay una unidad sobrante (cantidad impar), le aplicamos 10% OFF
                if (tienePromo10 && tienePromoCantidad)
                {
                    int pares = cantidad / 2;
                    int resto = cantidad % 2;

                    var factorPar = 1m - (descuentoCantidad / 100m);
                    total += pares * (2 * precioUnitario * factorPar);

                    if (resto == 1)
                    {
                        var factor10 = 1m - (10m / 100m);
                        total += 1 * precioUnitario * factor10;
                    }

                    return Math.Round(total, 2, MidpointRounding.AwayFromZero);
                }

                return cantidad * precioUnitario;
            }



    }
}