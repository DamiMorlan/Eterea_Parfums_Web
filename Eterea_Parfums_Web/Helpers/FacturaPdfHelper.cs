using Eterea_Parfums_Web.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using System;
using System.IO;
using System.Text;

namespace Eterea_Parfums_Web.Helpers
{
    public static class FacturaHelper
    {
        public static string GenerarHtmlFactura(factura factura, cliente cliente)
        {
            string templatePath = System.Web.HttpContext.Current.Server.MapPath("~/Templates/FacturaB.html");
            string html = File.ReadAllText(templatePath);

            string clienteNombre = $"{cliente.nombre} {cliente.apellido}";
            double importeSinIva = factura.precio_total / 1.21;
            double iva = factura.precio_total - importeSinIva;

            html = html.Replace("@NUMEROFACTURA", factura.num_factura);
            html = html.Replace("@FECHA", factura.fecha.ToString("dd/MM/yyyy"));
            html = html.Replace("@CLIENTE", clienteNombre);
            html = html.Replace("@CONDIVA", cliente.condicion_frente_al_iva);
            html = html.Replace("@DOCUMENTO", cliente.dni.ToString());
            string domicilio = $"{cliente.calle.nombre} {cliente.numeracion_calle}";
            string localidad = $"{cliente.localidad.nombre}";
            html = html.Replace("@DOMICILIO", domicilio);
            html = html.Replace("@LOCALIDAD", localidad);
            html = html.Replace("@IMPORTE", importeSinIva.ToString("0.00"));
            html = html.Replace("@IVA", iva.ToString("0.00"));
            html = html.Replace("@DESCUENTO", factura.descuento.ToString("0.00"));
            html = html.Replace("@RECARGO", factura.recargo_tarjeta.ToString("0.00"));
            html = html.Replace("@TOTAL", factura.precio_total.ToString("0.00"));

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
