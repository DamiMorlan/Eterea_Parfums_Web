using System.Web.Mvc;

namespace Eterea_Parfums_Web.Filters
{
    public class ForzarPerfilCompletoAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            bool forzar = session != null && (session["ForzarCompletarPerfil"] as bool? ?? false);

            string controller = filterContext.RouteData.Values["controller"].ToString();
            string action = filterContext.RouteData.Values["action"].ToString();

            // Si debe completar perfil y NO está en Cliente/Perfil
            if (forzar && !(controller == "Cliente" && action == "Perfil"))
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new { controller = "Cliente", action = "Perfil", primerLogin = true }
                    )
                );
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
