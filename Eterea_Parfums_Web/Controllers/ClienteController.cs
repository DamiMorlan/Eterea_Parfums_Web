using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Eterea_Parfums_Web.Models;

namespace Eterea_Parfums_Web.Controllers
{
    public class ClienteController : Controller
    {

        private etereaEntities1 db = new etereaEntities1();
        // GET: Cliente/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Cliente/Login

        [HttpPost]
        public ActionResult Login(string usuario, string clave)
        {
            var cliente = db.cliente.FirstOrDefault(c => c.usuario == usuario && c.clave == clave);

            if (cliente != null)
            {
                Session["clienteId"] = cliente.id;               // 👈 ID
                Session["usuarioLogueado"] = cliente.usuario;    // 👈 o guardar cliente directamente si lo usás más
                Session["clienteNombre"] = cliente.nombre;


                return RedirectToAction("Index", "Home"); // o "Perfume" si preferís
            }

            ViewData["Error"] = "Usuario o contraseña incorrectos.";
            return View();
        }


        public ActionResult Logout()
        {
            Session.Clear(); // Elimina todos los datos de sesión (incluye UsuarioId, etc.)

            return RedirectToAction("Index", "Home"); // Redirige a la pantalla principal
        }

        // GET: Cliente/Registrar
        public ActionResult Registrar()
        {
            return View();
        }

        // POST: Cliente/Registrar
        [HttpPost]
        public ActionResult Registrar(string username, string password)
        {
            // Aquí agregás la lógica para crear el usuario en la base de datos

            // Por ahora solo redirigimos a Login
            return RedirectToAction("Login");
        }

        public ActionResult Perfil()
        {
            return View();
        }
    }
}