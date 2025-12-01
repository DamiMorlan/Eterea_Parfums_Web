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
        public static string GenerarHtmlFacturaB(etereaEntities7 db, factura factura, cliente cliente, int cuotas)
        {
            var ciAR = CultureInfo.GetCultureInfo("es-AR");

            string templatePath = System.Web.HttpContext.Current.Server.MapPath("~/Templates/FacturaB.html");
            string html = File.ReadAllText(templatePath, Encoding.UTF8);

            string clienteNombre = $"{cliente.nombre} {cliente.apellido}";

            // Importe neto (sin IVA) ? total + descuento - recargo
            double importe = factura.precio_total + factura.descuento - factura.recargo_tarjeta;
            double total = factura.precio_total;

            // ==== Cabecera ====
            html = html.Replace("@NUMEROFACTURA", factura.num_factura);
            html = html.Replace("@FECHA", factura.fecha.ToString("dd/MM/yyyy"));
            html = html.Replace("@CLIENTE", clienteNombre);
            html = html.Replace("@CONDIVA", cliente.condicion_frente_al_iva ?? "");
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

            // ==== Totales (pie) con $ y alineados por .money ====
            html = html.Replace("@IMPORTE", "$ " + importe.ToString("N2", ciAR));
            html = html.Replace("@DESCUENTO", "$ " + factura.descuento.ToString("N2", ciAR));
            html = html.Replace("@RECARGO", "$ " + factura.recargo_tarjeta.ToString("N2", ciAR));
            html = html.Replace("@TOTAL", "$ " + factura.precio_total.ToString("N2", ciAR));

            // ==== Filas detalle ====
            var detallesFactura = db.detalle_factura
                .Include(d => d.perfume)
                .Include(d => d.promocion)
                .Include(d => d.promocion1)
                .Where(d => d.factura_id == factura.id)
                .ToList();

            var filasHtml = new StringBuilder();

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
                filasHtml.AppendLine($"  <td class=\"cant\">{cantidad}</td>"); // centrado
                filasHtml.AppendLine($"  <td>{System.Net.WebUtility.HtmlEncode(descripcion)}</td>");
                filasHtml.AppendLine($"  <td class=\"money\">$ {precioUnitario.ToString("N2", ciAR)}</td>");
                filasHtml.AppendLine($"  <td class=\"money\">$ {descuentoTotal.ToString("N2", ciAR)}</td>");
                filasHtml.AppendLine($"  <td class=\"money\">$ {subtotal.ToString("N2", ciAR)}</td>");
                filasHtml.AppendLine("</tr>");
            }

            html = html.Replace("@FILAS", filasHtml.ToString());

            // ==== Logo ====
            var request = System.Web.HttpContext.Current.Request;
            var baseUrl = $"{request.Url.Scheme}://{request.Url.Authority}{request.ApplicationPath.TrimEnd('/')}/";
            string urlLogo = baseUrl + "Imagen/Mostrar?nombre=LogoEtereaFactura.png";

            string imgTag = $"<img src=\"{urlLogo}\" style=\"width:60px; height:60px;\" />";
            html = html.Replace("@LOGO", imgTag);

            return html;
        }

        public static string GenerarHtmlFacturaA(etereaEntities7 db, factura factura, cliente cliente, int cuotas)
        {
            var ciAR = CultureInfo.GetCultureInfo("es-AR");

            string templatePath = System.Web.HttpContext.Current.Server.MapPath("~/Templates/FacturaA.html");
            string html = File.ReadAllText(templatePath, Encoding.UTF8);

            string clienteNombre = $"{cliente.nombre} {cliente.apellido}";

            // Base con IVA ANTES de bonificación y recargo
            double baseConIva = factura.precio_total + factura.descuento - factura.recargo_tarjeta;

            // Importe neto gravado (sin IVA)
            double importeSinIva = baseConIva / 1.21;

            // IVA 21% sobre esa base
            double iva = baseConIva - importeSinIva;

            // ==== Cabecera ====
            html = html.Replace("@NUMEROFACTURA", factura.num_factura);
            html = html.Replace("@FECHA", factura.fecha.ToString("dd/MM/yyyy"));
            html = html.Replace("@CLIENTE", clienteNombre);
            html = html.Replace("@CONDIVA", cliente.condicion_frente_al_iva ?? "");
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

            // ==== Totales (pie) con $ ====
            html = html.Replace("@IMPORTE", "$ " + importeSinIva.ToString("N2", ciAR));
            html = html.Replace("@IVA", "$ " + iva.ToString("N2", ciAR));
            html = html.Replace("@DESCUENTO", "$ " + factura.descuento.ToString("N2", ciAR));
            html = html.Replace("@RECARGO", "$ " + factura.recargo_tarjeta.ToString("N2", ciAR));
            html = html.Replace("@TOTAL", "$ " + factura.precio_total.ToString("N2", ciAR));

            // ==== Filas detalle ====
            var detallesFactura = db.detalle_factura
                .Include(d => d.perfume)
                .Include(d => d.promocion)
                .Include(d => d.promocion1)
                .Where(d => d.factura_id == factura.id)
                .ToList();

            var filasHtml = new StringBuilder();

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

                double subtotalConIva = (precioUnitario * cantidad) - descuentoTotal;
                double subtotalSinIva = subtotalConIva / 1.21;

                filasHtml.AppendLine("<tr>");
                filasHtml.AppendLine($"  <td class=\"cant\">{cantidad}</td>"); // centrado
                filasHtml.AppendLine($"  <td>{System.Net.WebUtility.HtmlEncode(descripcion)}</td>");
                filasHtml.AppendLine($"  <td class=\"money\">$ {precioUnitario.ToString("N2", ciAR)}</td>");
                filasHtml.AppendLine($"  <td class=\"money\">$ {descuentoTotal.ToString("N2", ciAR)}</td>");
                filasHtml.AppendLine($"  <td class=\"money\">$ {subtotalSinIva.ToString("N2", ciAR)}</td>");   // Subtotal
                filasHtml.AppendLine($"  <td class=\"money\">$ {subtotalConIva.ToString("N2", ciAR)}</td>");   // Subtotal c/IVA
                filasHtml.AppendLine("</tr>");
            }

            html = html.Replace("@FILAS", filasHtml.ToString());

            // ==== Logo ====
            var request = System.Web.HttpContext.Current.Request;
            var baseUrl = $"{request.Url.Scheme}://{request.Url.Authority}{request.ApplicationPath.TrimEnd('/')}/";
            string urlLogo = baseUrl + "Imagen/Mostrar?nombre=LogoEtereaFactura.png";

            string imgTag = $"<img src=\"{urlLogo}\" style=\"width:60px; height:60px;\" />";
            html = html.Replace("@LOGO", imgTag);

            return html;
        }

    }
}
