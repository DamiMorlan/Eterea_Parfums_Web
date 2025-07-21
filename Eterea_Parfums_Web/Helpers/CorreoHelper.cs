using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Configuration;

namespace Eterea_Parfums_Web.Helpers
{
    public static class CorreoHelper
    {
        public static void EnviarCorreoConfirmacionPedido(string emailDestino, string nombreCliente, string numeroFactura, double total)
        {
            string asunto = "Confirmación de tu compra - Etérea Parfums";
            string mensaje = $"Hola {nombreCliente},\n\n" +
                             $"Gracias por tu compra. Tu factura Nº {numeroFactura} ha sido generada con éxito.\n" +
                             $"Total: ${total:F2}\n\n" +
                             $"¡Gracias por confiar en Etérea Parfums!\n\n" +
                             "Este es un mensaje automático, por favor no respondas.";

            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress("etereaparfumsinfo@gmail.com", "Etérea Parfums");
                mail.To.Add(emailDestino);
                mail.Subject = asunto;
                mail.Body = mensaje;
                mail.IsBodyHtml = false;

                var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("etereaparfumsinfo@gmail.com", "jcfy ubtj cknd bosb"), // Podés mover esto a Web.config si querés mayor seguridad
                    EnableSsl = true
                };

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                // Log error (no uses MessageBox en web)
                System.Diagnostics.Debug.WriteLine("Error al enviar correo: " + ex.Message);
            }
        }

    }
}
