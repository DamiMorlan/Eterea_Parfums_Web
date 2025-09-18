using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.Http;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        // (Opcional) CORS
        // config.EnableCors();

        // Ruteo por atributos
        config.MapHttpAttributeRoutes();

        // Ruta por defecto: /api/{controller}/{id}
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
        );

        // (Opcional) JSON por defecto
        // var json = config.Formatters.JsonFormatter;
        // json.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        // config.Formatters.Remove(config.Formatters.XmlFormatter);
    }
}
