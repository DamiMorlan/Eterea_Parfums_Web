using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class ClienteController : Controller
    {
        // GET: Cliente/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Cliente/Login
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            // Aquí va la lógica real para validar usuario contra la base de datos
            // Por ejemplo, un usuario de prueba:
            if (username == "admin" && password == "1234")
            {
                // Podés usar Session o FormsAuthentication para autenticar
                Session["Usuario"] = username;
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View();
            }
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
    }
}