using System;
using System.IO;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class ImagenesController : Controller
    {
        [HttpPost]
        public ActionResult Subir(HttpPostedFileBase archivo)
        {
            if (archivo == null || archivo.ContentLength == 0)
                return new HttpStatusCodeResult(400, "No se envió ningún archivo");

            try
            {
                var nombreArchivo = Path.GetFileName(archivo.FileName);
                var rutaDestino = Server.MapPath("~/imagenes/");

                if (!Directory.Exists(rutaDestino))
                    Directory.CreateDirectory(rutaDestino);

                var rutaCompleta = Path.Combine(rutaDestino, nombreArchivo);
                archivo.SaveAs(rutaCompleta);

                return Json(new { mensaje = "Imagen subida correctamente", nombre = nombreArchivo });
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error al guardar la imagen: " + ex.Message);
            }
        }
    }
}
