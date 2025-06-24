using System.Collections.Generic;
using System.Web.Mvc;
using Eterea_Parfums_Web.ViewModels;

namespace Eterea_Parfums_Web.Controllers
{
    public class InicioController : Controller
    {
        [HttpGet]
        public ActionResult SeleccionarUsuario()
        {
            var model = new SeleccionarUsuarioViewModel
            {
                UsuariosDisponibles = new List<string> { "Adrian", "Jose" }  // Podés adaptar a tu caso
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult SeleccionarUsuario(SeleccionarUsuarioViewModel model)
        {
            if (!string.IsNullOrEmpty(model.UsuarioSeleccionado))
            {
                // Asignar la conexión correspondiente según el usuario
                switch (model.UsuarioSeleccionado.ToLower())
                {
                    case "adrian":
                        Session["ConexionActiva"] = "eterea_local_adrian";
                        break;
                    case "jose":
                        Session["ConexionActiva"] = "eterea_local_jose";
                        break;
                    default:
                        Session["ConexionActiva"] = "eterea_local_adrian";
                        break;
                }

                // Redirigir a la página principal (o donde desees continuar)
                return RedirectToAction("Index", "Home");
            }

            // Si no seleccionó nada, mostrar el formulario de nuevo
            model.UsuariosDisponibles = new List<string> { "Adrian", "Jose" };
            return View(model);
        }
    }
}


