using Eterea_Parfums_Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class ContactController : Controller
    {
        [HttpPost]
        public ActionResult EnviarContacto(string nombre, string email, string mensaje)
        {
            try
            {
                string asunto = "Nuevo mensaje de contacto desde Etérea Parfums";
                string cuerpo = $"Nombre: {nombre}\n" +
                                $"Correo: {email}\n\n" +
                                $"Mensaje:\n{mensaje}";

                // Destino: casilla de la empresa
                string destinatario = "etereaparfumsinfo@gmail.com";

                // 1) Mail a la empresa
                CorreoHelper.EnviarCorreoGenerico(destinatario, asunto, cuerpo);

                // 2) 👉 Respuesta automática al cliente
                CorreoHelper.EnviarRespuestaAutoContacto(email, nombre);

                // 3) Mensaje para mostrar en la vista
                TempData["MensajeEnviado"] =
                    "Tu consulta fue recibida correctamente. " +
                    "En los próximos 5 días hábiles estarás recibiendo nuestra respuesta. " +
                    "¡Gracias por comunicarte con Etérea Parfums!";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al enviar mensaje de contacto: " + ex.Message);
                TempData["MensajeError"] = "Hubo un error al enviar tu mensaje. Por favor, intentá más tarde.";
            }

            // Volver a la página de contacto
            return Redirect(Url.Action("Index", "Contact") + "#form-contacto");
        }


        // GET: Contact
        public ActionResult Index()
        {
            return View();
        }

        // GET: Contact/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Contact/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Contact/Create
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

        // GET: Contact/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Contact/Edit/5
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

        // GET: Contact/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Contact/Delete/5
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
    }
}
