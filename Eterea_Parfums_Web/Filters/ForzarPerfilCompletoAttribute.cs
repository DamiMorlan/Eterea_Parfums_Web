using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace Eterea_Parfums_Web.Filters
{
    public class ForzarPerfilCompletoAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;

            // 1) Si NO hay cliente logueado -> NO obligamos a nada
            //    Deja pasar al action normal (página pública)
            if (session["clienteId"] == null)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            // 2) Solo si hay cliente logueado, miramos el flag ForzarCompletarPerfil
            bool forzar = false;
            if (session["ForzarCompletarPerfil"] != null)
            {
                bool.TryParse(session["ForzarCompletarPerfil"].ToString(), out forzar);
            }

            // 3) Si hay que forzar, mandar siempre a Perfil (primer login)
            if (forzar)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new
                        {
                            controller = "Cliente",
                            action = "Perfil",
                            primerLogin = true
                        }
                    )
                );
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
