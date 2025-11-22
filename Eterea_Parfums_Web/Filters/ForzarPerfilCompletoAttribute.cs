using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace Eterea_Parfums_Web.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class ForzarPerfilCompletoAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            var controller = filterContext.Controller as Controller;

            // 1) Si no hay cliente logueado, mandamos a Login directamente
            if (session["clienteId"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Cliente",
                        action = "Login"
                    })
                );
                return;
            }

            // 2) Ver si está marcado que debe completar perfil
            bool forzar = false;
            if (session["ForzarCompletarPerfil"] != null)
            {
                bool.TryParse(session["ForzarCompletarPerfil"].ToString(), out forzar);
            }

            if (forzar)
            {
                // 3) Mensaje de bienvenida / explicación SOLO si no viene ya uno seteado
                if (controller != null && !controller.TempData.ContainsKey("AvisoPasswordInsegura"))
                {
                    controller.TempData["AvisoPasswordInsegura"] =
                        "¡Es tu primera vez en nuestra web, bienvenido! " +
                        "Te pedimos que actualices tu usuario y tu contraseña y completes los datos faltantes " +
                        "que te solicitamos a continuación en este formulario para terminar tu registro.";
                }

                // 4) Redirigir a Perfil con primerLogin = true
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(new
                    {
                        controller = "Cliente",
                        action = "Perfil",
                        primerLogin = true
                    })
                );
            }
        }
    }
}
