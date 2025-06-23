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
             using (etereaEntities1 db = new etereaEntities1())
             {
                 cliente usuarioLogueado = db.cliente.FirstOrDefault(a => a.usuario == usuario && a.clave == clave);

                 if (usuarioLogueado != null)
                 {
                     // Si el usuario se autentica correctamente
                     // Guardar el objeto Cliente en la sesión
                     Session["usuarioLogueado"] = usuarioLogueado;
                     // Usuario y contraseña válidos, redirigir a la página Index de la carpeta Carrito
                     return RedirectToAction("Index", "Perfume");
                 }
                 else
                 {
                     // Usuario o contraseña inválidos, volver a cargar la página de login con un mensaje de error
                     ViewData["Error"] = "Usuario o contraseña incorrectos";
                     return View();
                 }
             }
            return View();
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