using Eterea_Parfums_Web.Models;
using System.Globalization;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System;
using System.IO;
using System.Text;
using System.Data.Entity;
using System.Linq;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Security.Policy;


namespace Eterea_Parfums_Web.Helpers
{
    public static class FacturaHelper
    {
        public static string GenerarHtmlFacturaB(etereaEntities7 db, factura factura, cliente cliente, int cuotas, decimal costoEnvio)
        {
            string templatePath = System.Web.HttpContext.Current.Server.MapPath("~/Templates/FacturaB.html");
            string html = File.ReadAllText(templatePath);

            // ============================
            // 1) Datos de la sucursal
            // ============================
            // Si sucursal_id = 0 (web), usamos 1 para obtener el domicilio del local 1 
            //que es desde donde se despachan las ventas online
            int sucursalIdParaDireccion = (factura.sucursal_id == 0) ? 1 : factura.sucursal_id;

            var sucursal = db.sucursal
                             .Include("calle")
                             .Include("localidad")
                             .Include("pais")
                             .FirstOrDefault(s => s.id == sucursalIdParaDireccion);

            string dirSucursal = "";
            string locPaisSucursal = "";

            if (sucursal != null)
            {
                string calleNombre = sucursal.calle != null ? sucursal.calle.nombre : "";

                string numero = sucursal.numeracion_calle.ToString();

                dirSucursal = (calleNombre + " " + numero).Trim();

                string localidadNombre = sucursal.localidad != null ? sucursal.localidad.nombre : "";
                string paisNombre = sucursal.pais != null ? sucursal.pais.nombre : "";

                locPaisSucursal = (localidadNombre + ", " + paisNombre).Trim().Trim(',');
            }

            html = html.Replace("@DIR_SUCURSAL", dirSucursal);
            html = html.Replace("@LOC_PAIS_SUC", locPaisSucursal);

            // ============================
            // 2) Datos del cliente / factura
            // ============================
            string clienteNombre = $"{cliente.nombre} {cliente.apellido}";
           

            html = html.Replace("@NUMEROFACTURA", factura.num_factura);
            html = html.Replace("@FECHA", factura.fecha.ToString("dd/MM/yyyy"));
            html = html.Replace("@CLIENTE", clienteNombre);
            html = html.Replace("@CONDIVA", cliente.condicion_frente_al_iva);
            html = html.Replace("@FORMAPAGO", factura.forma_de_pago);
            html = html.Replace("@DOCUMENTO", cliente.dni.ToString());

            string domicilio = $"{cliente.calle.nombre} {cliente.numeracion_calle}";
            string localidad = $"{cliente.localidad.nombre}";
            html = html.Replace("@DOMICILIO", domicilio);
            html = html.Replace("@LOCALIDAD", localidad);

            // ===== Fila dinámica de "Forma de Pago" (+ "Cuotas" si es tarjeta) =====
            var forma = factura.forma_de_pago ?? "";
            string cuotasSel = cuotas.ToString();

            bool esTarjeta = forma == "Visa Crédito"
                          || forma == "Mastercard"
                          || forma == "Amex";

            string rowFormaPago;
            if (esTarjeta)
            {
                rowFormaPago = @"
                <tr>
                  <td style='background:#F6DDE6; font-weight:bold;'>Forma de Pago:</td>
                  <td style='width:40%;'>" + System.Net.WebUtility.HtmlEncode(forma) + @"</td>
                  <td style='width:20%; background:#F6DDE6; font-weight:bold;'>Cuotas:</td>
                  <td>" + System.Net.WebUtility.HtmlEncode(cuotasSel) + @"</td>
                </tr>";
                    }
            else
            {
                rowFormaPago = @"
                <tr>
                  <td style='background:#F6DDE6; font-weight:bold;'>Forma de Pago:</td>
                  <td colspan='3'>" + System.Net.WebUtility.HtmlEncode(forma) + @"</td>
                </tr>";
            }
            html = html.Replace("@ROW_FORMA_PAGO", rowFormaPago);

            // En la web: Bonificación SIEMPRE 0
            double bonificacion = 0.0;

            // Recargo por tarjeta (si lo hubiera)
            double recargo = factura.recargo_tarjeta;

            // Importe = total sin recargo (las promos ya están aplicadas en el precio_total)
            double importe = factura.precio_total - recargo;

            // Total = lo que realmente paga el cliente
            double total = factura.precio_total;

            html = html.Replace("@IMPORTE", "$ " + importe.ToString("0.00"));
            html = html.Replace("@DESCUENTO", "$ " + bonificacion.ToString("0.00"));  // Bonificación = 0
            html = html.Replace("@RECARGO", "$ " + recargo.ToString("0.00"));
            html = html.Replace("@TOTAL", "$ " + total.ToString("0.00"));

            // ===== Detalle de filas =====
            var detallesFactura = db.detalle_factura
                .Include(d => d.perfume)
                .Include(d => d.promocion)
                .Include(d => d.promocion1)
                .Where(d => d.factura_id == factura.id)
                .ToList();

            StringBuilder filasHtml = new StringBuilder();

            foreach (var detalle in detallesFactura)
            {
                string descripcion = detalle.perfume.nombre;
                double precioUnitario = detalle.precio_unitario;
                int cantidad = detalle.cantidad;
                double descuento1 = detalle.promocion?.descuento ?? 0;
                double descuento2 = detalle.promocion1?.descuento ?? 0;

                double descuentoTotal = 0;
                int cantRestante = cantidad;

                if (descuento2 > 10 && cantRestante >= 2)
                {
                    int pares = cantRestante / 2;
                    descuentoTotal += pares * (descuento2 / 100.0) * precioUnitario * 2;
                    cantRestante -= pares * 2;
                }

                if (descuento1 == 10 && cantRestante > 0)
                {
                    descuentoTotal += cantRestante * (descuento1 / 100.0) * precioUnitario;
                }

                double subtotal = (precioUnitario * cantidad) - descuentoTotal;

                filasHtml.AppendLine("<tr>");
                filasHtml.AppendLine($"  <td class='cant'>{cantidad}</td>");
                filasHtml.AppendLine($"  <td>{descripcion}</td>");
                filasHtml.AppendLine($"  <td class='money'>${precioUnitario:0.00}</td>");
                filasHtml.AppendLine($"  <td class='money'>${descuentoTotal:0.00}</td>");
                filasHtml.AppendLine($"  <td class='money'>${subtotal:0.00}</td>");
                filasHtml.AppendLine("</tr>");
            }

            //Fila extra de costo de envío (si corresponde)
            if (costoEnvio > 0)
            {
                double costoEnvioDouble = (double)costoEnvio;

                filasHtml.AppendLine("<tr>");
                filasHtml.AppendLine("  <td class='cant'>1</td>");
                filasHtml.AppendLine("  <td>Costo de envío</td>");
                filasHtml.AppendLine($"  <td class='money'>${costoEnvioDouble:0.00}</td>");
                filasHtml.AppendLine("  <td class='money'>$0.00</td>");           // sin descuento
                filasHtml.AppendLine($"  <td class='money'>${costoEnvioDouble:0.00}</td>");
                filasHtml.AppendLine("</tr>");
            }


            html = html.Replace("@FILAS", filasHtml.ToString());

            var request = System.Web.HttpContext.Current.Request;
            var baseUrl = $"{request.Url.Scheme}://{request.Url.Authority}{request.ApplicationPath.TrimEnd('/')}/";
            string urlLogo = baseUrl + "Imagen/Mostrar?nombre=LogoEtereaFactura.png";

            string imgTag = $"<img src=\"{urlLogo}\" style=\"width:60px; height:60px;\" />";
            html = html.Replace("@LOGO", imgTag);

            return html;
        }

        public static string GenerarHtmlFacturaA(etereaEntities7 db, factura factura, cliente cliente, int cuotas, decimal costoEnvio)
        {
            string templatePath = System.Web.HttpContext.Current.Server.MapPath("~/Templates/FacturaA.html");
            string html = File.ReadAllText(templatePath);

            var ci = new CultureInfo("es-AR");
            string Mon(decimal v) => "$ " + v.ToString("N2", ci);

            // ============================
            // 1) Datos de la sucursal
            // ============================
            int sucursalIdParaDireccion = (factura.sucursal_id == 0) ? 1 : factura.sucursal_id;

            var sucursal = db.sucursal
                             .Include("calle")
                             .Include("localidad")
                             .Include("pais")
                             .FirstOrDefault(s => s.id == sucursalIdParaDireccion);

            string dirSucursal = "";
            string locPaisSucursal = "";

            if (sucursal != null)
            {
                string calleNombre = sucursal.calle != null ? sucursal.calle.nombre : "";

                // Si numeracion_calle es int:
                string numero = sucursal.numeracion_calle.ToString();

                // Si el campo se llama distinto, cambiá esta línea:
                // string numero = sucursal.numero.ToString();
                // string numero = sucursal.altura.ToString();

                dirSucursal = (calleNombre + " " + numero).Trim();

                string localidadNombre = sucursal.localidad != null ? sucursal.localidad.nombre : "";
                string paisNombre = sucursal.pais != null ? sucursal.pais.nombre : "";

                locPaisSucursal = (localidadNombre + ", " + paisNombre).Trim().Trim(',');
            }

            html = html.Replace("@DIR_SUCURSAL", dirSucursal);
            html = html.Replace("@LOC_PAIS_SUC", locPaisSucursal);

            // ============================
            // 2) Datos cliente / factura
            // ============================
            string clienteNombre = $"{cliente.nombre} {cliente.apellido}";

            html = html.Replace("@NUMEROFACTURA", factura.num_factura);
            html = html.Replace("@FECHA", factura.fecha.ToString("dd/MM/yyyy"));
            html = html.Replace("@CLIENTE", clienteNombre);
            html = html.Replace("@CONDIVA", cliente.condicion_frente_al_iva);
            html = html.Replace("@DOCUMENTO", cliente.dni.ToString());

            string domicilio = $"{cliente.calle.nombre} {cliente.numeracion_calle}";
            string localidad = $"{cliente.localidad.nombre}";
            html = html.Replace("@DOMICILIO", domicilio);
            html = html.Replace("@LOCALIDAD", localidad);

            // ===== Fila dinámica de "Forma de Pago" (+ "Cuotas" si es tarjeta) =====
            var forma = factura.forma_de_pago ?? "";
            string cuotasSel = cuotas.ToString();

            bool esTarjeta = forma == "Visa Crédito"
                          || forma == "Mastercard"
                          || forma == "Amex";

            string rowFormaPago;
            if (esTarjeta)
            {
                rowFormaPago = @"
                <tr>
                  <td style='background:#F6DDE6; font-weight:bold;'>Forma de Pago:</td>
                  <td style='width:40%;'>" + System.Net.WebUtility.HtmlEncode(forma) + @"</td>
                  <td style='width:20%; background:#F6DDE6; font-weight:bold;'>Cuotas:</td>
                  <td>" + System.Net.WebUtility.HtmlEncode(cuotasSel) + @"</td>
                </tr>";
            }
            else
            {
                rowFormaPago = @"
                <tr>
                  <td style='background:#F6DDE6; font-weight:bold;'>Forma de Pago:</td>
                  <td colspan='3'>" + System.Net.WebUtility.HtmlEncode(forma) + @"</td>
                </tr>";
            }
            html = html.Replace("@ROW_FORMA_PAGO", rowFormaPago);

            // ============================
            // 3) Detalle de la factura
            //    (misma lógica que desktop)
            // ============================
            var detallesFactura = db.detalle_factura
                .Include(d => d.perfume)
                .Include(d => d.promocion)
                .Include(d => d.promocion1)
                .Where(d => d.factura_id == factura.id)
                .ToList();

            StringBuilder filasHtml = new StringBuilder();

            decimal sumaTotalConIva = 0m;   // suma de columna "Total c/IVA"
            decimal sumaTotalDescPromo = 0m;

            foreach (var detalle in detallesFactura)
            {
                string descripcion = detalle.perfume.nombre;
                double precioUnitario = detalle.precio_unitario; // se asume PRECIO CON IVA
                int cantidad = detalle.cantidad;
                double descuento1 = detalle.promocion?.descuento ?? 0;
                double descuento2 = detalle.promocion1?.descuento ?? 0;

                double descuentoTotal = 0;
                int cantRestante = cantidad;

                // Promo por cantidad (descuento2 > 10) → pares
                if (descuento2 > 10 && cantRestante >= 2)
                {
                    int pares = cantRestante / 2;
                    descuentoTotal += pares * (descuento2 / 100.0) * precioUnitario * 2;
                    cantRestante -= pares * 2;
                }

                // Promo 10% (descuento1 == 10) sobre lo que quedó
                if (descuento1 == 10 && cantRestante > 0)
                {
                    descuentoTotal += cantRestante * (descuento1 / 100.0) * precioUnitario;
                }

                double subtotal = precioUnitario * cantidad;           // sin aplicar promo
                double totalConIvaLinea = subtotal - descuentoTotal;    // columna "Total c/IVA"
                double subtotalSinIvaLinea = totalConIvaLinea / 1.21;   // solo para info interna

                sumaTotalConIva += (decimal)totalConIvaLinea;
                sumaTotalDescPromo += (decimal)descuentoTotal;

                filasHtml.AppendLine("<tr>");
                filasHtml.AppendLine($"  <td class='cant'>{cantidad}</td>");
                filasHtml.AppendLine($"  <td>{descripcion}</td>");
                filasHtml.AppendLine($"  <td class='money'>{Mon((decimal)precioUnitario)}</td>");
                filasHtml.AppendLine($"  <td class='money'>{Mon((decimal)subtotal)}</td>");
                filasHtml.AppendLine($"  <td class='money'>{Mon((decimal)descuentoTotal)}</td>");
                filasHtml.AppendLine($"  <td class='money'>{Mon((decimal)totalConIvaLinea)}</td>");
                filasHtml.AppendLine("</tr>");
            }

            //Fila extra de costo de envío (si corresponde)
            if (costoEnvio > 0)
            {
                filasHtml.AppendLine("<tr>");
                filasHtml.AppendLine("  <td class='cant'>1</td>");
                filasHtml.AppendLine("  <td>Costo de envío</td>");
                filasHtml.AppendLine($"  <td class='money'>{Mon(costoEnvio)}</td>"); // Precio unitario
                filasHtml.AppendLine($"  <td class='money'>{Mon(costoEnvio)}</td>"); // Subtotal
                filasHtml.AppendLine("  <td class='money'>$ 0,00</td>");             // Descuento
                filasHtml.AppendLine($"  <td class='money'>{Mon(costoEnvio)}</td>"); // Total c/IVA
                filasHtml.AppendLine("</tr>");

                // También lo sumamos a los totales con IVA
                sumaTotalConIva += costoEnvio;
            }


            html = html.Replace("@FILAS", filasHtml.ToString());

            // ============================
            // 4) Totales (como desktop)
            // ============================

            // Por ahora en la web NO estás aplicando descuento extra por forma de pago,
            // sólo promos y recargos → Bonificación = 0
            decimal bonificacion = 0m;

            // Recargo tarjeta viene de la factura
            decimal recargo = (decimal)factura.recargo_tarjeta;

            // Precio Final c/IVA = suma líneas - bonificación + recargo
            decimal precioFinalConIva = sumaTotalConIva - bonificacion + recargo;
            precioFinalConIva = Math.Round(precioFinalConIva, 2, MidpointRounding.AwayFromZero);

            // Importe Neto Gravado + IVA sobre el PRECIO FINAL (como desktop)
            decimal importeNeto = 0m;
            decimal iva = 0m;
            if (precioFinalConIva != 0)
            {
                importeNeto = Math.Round(precioFinalConIva / 1.21m, 2, MidpointRounding.AwayFromZero);
                iva = precioFinalConIva - importeNeto;
                iva = Math.Round(iva, 2, MidpointRounding.AwayFromZero);
            }

            // Reemplazos de pie de página
            html = html.Replace("@DESCUENTO", Mon(bonificacion));        // "Bonificación:"
            html = html.Replace("@RECARGO", Mon(recargo));               // "Recargo Tarjeta:"
            html = html.Replace("@PRECIO_FINAL", Mon(precioFinalConIva));// "Precio Final c/IVA:"
            html = html.Replace("@IMPORTE", Mon(importeNeto));           // "Importe Neto Gravado:"
            html = html.Replace("@IVA", Mon(iva));                       // "IVA 21%:"
            html = html.Replace("@TOTAL", Mon(precioFinalConIva));       // "Total a pagar:"

            // ============================
            // 5) Logo
            // ============================
            var request = System.Web.HttpContext.Current.Request;
            var baseUrl = $"{request.Url.Scheme}://{request.Url.Authority}{request.ApplicationPath.TrimEnd('/')}/";
            string urlLogo = baseUrl + "Imagen/Mostrar?nombre=LogoEtereaFactura.png";

            string imgTag = $"<img src=\"{urlLogo}\" style=\"width:60px; height:60px;\" />";
            html = html.Replace("@LOGO", imgTag);

            return html;
        }


        public static byte[] GenerarFacturaPdf(string html)
        {
            using (var ms = new MemoryStream())
            {
                using (var document = new Document(PageSize.A4, 50, 50, 60, 60))
                {
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    document.Open();
                    using (var sr = new StringReader(html))
                    {
                        XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, sr);
                    }
                    document.Close();
                }
                return ms.ToArray();
            }
        }
    }
}
