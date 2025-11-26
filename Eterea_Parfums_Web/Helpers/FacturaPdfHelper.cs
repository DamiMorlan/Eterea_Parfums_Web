using Eterea_Parfums_Web.Models;
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
            string templatePath = System.Web.HttpContext.Current.Server.MapPath("~/Templates/FacturaB.html");
            string html = File.ReadAllText(templatePath);

            string clienteNombre = $"{cliente.nombre} {cliente.apellido}";
            double importe = factura.precio_total + factura.descuento - factura.recargo_tarjeta;
            double total = factura.precio_total;

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

            // Es tarjeta de crédito si es Visa Crédito, Mastercard o Amex
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

            html = html.Replace("@IMPORTE", importe.ToString("0.00"));
            html = html.Replace("@DESCUENTO", factura.descuento.ToString("0.00"));
            html = html.Replace("@RECARGO", factura.recargo_tarjeta.ToString("0.00"));
            html = html.Replace("@TOTAL", factura.precio_total.ToString("0.00"));

            // Generar filas detalle
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

                // Aplicamos promo del 10% a lo que quede
                if (descuento1 == 10 && cantRestante > 0)
                {
                    descuentoTotal += cantRestante * (descuento1 / 100.0) * precioUnitario;
                }

                double subtotal = (precioUnitario * cantidad) - descuentoTotal;

                filasHtml.AppendLine("<tr>");
                filasHtml.AppendLine($"  <td>{cantidad}</td>");                   // Cantidad
                filasHtml.AppendLine($"  <td>{descripcion}</td>");                // Descripción
                filasHtml.AppendLine($"  <td>${precioUnitario:0.00}</td>");       // Precio unitario
                filasHtml.AppendLine($"  <td>${descuentoTotal:0.00}</td>");       // Descuento
                filasHtml.AppendLine($"  <td>${subtotal:0.00}</td>");             // Subtotal
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

        public static string GenerarHtmlFacturaA(etereaEntities7 db, factura factura, cliente cliente, int cuotas)
        {
            string templatePath = System.Web.HttpContext.Current.Server.MapPath("~/Templates/FacturaA.html");
            string html = File.ReadAllText(templatePath);

            string clienteNombre = $"{cliente.nombre} {cliente.apellido}";
            double iva = factura.precio_total / 1.21 * 0.21;
            double importeSinIva = (factura.precio_total - iva + factura.descuento - factura.recargo_tarjeta );
            

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

            // Es tarjeta de crédito si es Visa Crédito, Mastercard o Amex
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

            html = html.Replace("@IMPORTE", importeSinIva.ToString("0.00"));
            html = html.Replace("@IVA", iva.ToString("0.00"));
            html = html.Replace("@DESCUENTO", factura.descuento.ToString("0.00"));
            html = html.Replace("@RECARGO", factura.recargo_tarjeta.ToString("0.00"));
            html = html.Replace("@TOTAL", factura.precio_total.ToString("0.00"));

            // Generar filas detalle
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

                // Aplicamos promo del 10% a lo que quede
                if (descuento1 == 10 && cantRestante > 0)
                {
                    descuentoTotal += cantRestante * (descuento1 / 100.0) * precioUnitario;
                }

                double subtotal = ((precioUnitario * cantidad) - descuentoTotal)/1.21;
                double subtotalConIva = (precioUnitario * cantidad) - descuentoTotal;

                filasHtml.AppendLine("<tr>");
                filasHtml.AppendLine($"  <td>{cantidad}</td>");                    // Cantidad
                filasHtml.AppendLine($"  <td>{descripcion}</td>");                 // Descripción
                filasHtml.AppendLine($"  <td>${precioUnitario:0.00}</td>");        // Precio unitario
                filasHtml.AppendLine($"  <td>${descuentoTotal:0.00}</td>");        // Descuento
                filasHtml.AppendLine($"  <td>${subtotal:0.00}</td>");              // Importe sin IVA
                filasHtml.AppendLine($"  <td>${subtotalConIva:0.00}</td>");        // Importe con IVA
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
