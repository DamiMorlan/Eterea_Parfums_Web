using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Configuration;

namespace Eterea_Parfums_Web.Helpers
{
    public static class CorreoHelper
    {
        public static void EnviarCorreoConfirmacionPedido(string emailDestino, string nombreCliente, string tipoFactura, string numeroFactura, double total, string rutaPdf)
        {
            string asunto = "Confirmación de tu compra - Etérea Parfums";
            string mensaje = $"Hola {nombreCliente},\n\n" +
                           $"Gracias por tu compra. Tu factura {tipoFactura} Nº {numeroFactura} ha sido generada con éxito.\n" +
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

                if (!string.IsNullOrEmpty(rutaPdf) && File.Exists(rutaPdf))
                {
                    //Toma el nombre real del archivo generado (Factura A..., Factura B..., etc.)
                    var adjunto = new Attachment(rutaPdf);
                    adjunto.Name = Path.GetFileName(rutaPdf); 
                    mail.Attachments.Add(adjunto);
                }

                var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("etereaparfumsinfo@gmail.com", "jcfy ubtj cknd bosb"), // Podés mover esto a Web.config si querés mayor seguridad
                    EnableSsl = true
                };

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al enviar correo: " + ex.Message);
            }
        }

        public static void EnviarCorreoGenerico(string emailDestino, string asunto, string cuerpo)
        {
            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress("etereaparfumsinfo@gmail.com", "Etérea Parfums");
                mail.To.Add(emailDestino);
                mail.Subject = asunto;
                mail.Body = cuerpo;
                mail.IsBodyHtml = false;


                var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("etereaparfumsinfo@gmail.com", "jcfy ubtj cknd bosb"),
                    EnableSsl = true
                };

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al enviar correo: " + ex.Message);
            }
        }

        public static void EnviarRespuestaAutoContacto(string emailDestino, string nombre)
        {
            string asunto = "Hemos recibido tu consulta - Etérea Parfums";

            string cuerpo =
                $"Hola {nombre},\n\n" +
                "Gracias por comunicarte con Etérea Parfums.\n\n" +
                "Tu consulta fue recibida correctamente y nuestro equipo ya la está revisando.\n" +
                "En los próximos 5 días hábiles estarás recibiendo nuestra respuesta.\n\n" +
                "¡Muchas gracias por escribirnos!\n\n" +
                "Etérea Parfums.\n\n" +
                "Este es un mensaje automático, por favor no respondas a este correo.";

            EnviarCorreoGenerico(emailDestino, asunto, cuerpo);
        }

        public static void EnviarCorreoBienvenidaRegistro(string emailDestino, string nombreCliente, string usuario)
        {
            string asunto = "¡Bienvenido/a a Etérea Parfums!";

            string cuerpo =
                $"Hola {nombreCliente},\n\n" +
                "¡Te damos la bienvenida a Etérea Parfums!\n\n" +
                "Tu registro se ha completado correctamente y ya podés ingresar a nuestra web con tus datos de acceso.\n\n" +
                $"Nombre de usuario: {usuario}\n\n" +
                "Desde ahora podrás:\n" +
                "- Realizar compras online.\n" +
                "- Ver el historial de tus pedidos.\n" +
                "- Gestionar tus domicilios de envío.\n\n" +
                "¡Gracias por registrarte y por confiar en Etérea Parfums!\n\n" +
                "Etérea Parfums.\n\n" +
                "Este es un mensaje automático, por favor no respondas a este correo.";

            EnviarCorreoGenerico(emailDestino, asunto, cuerpo);
        }


    }
}
