using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class ImagenController : Controller
    {

        /*Adri*/
        //private string rutaBase = @"C:\Users\intersan\Desktop\TESIS_New\Eterea_Parfums_Desktop\Eterea_Parfums_Desktop\Resources";

        /*Dami*/
        //private string rutaBase = @"C:\Users\damim\source\repos\Eterea_Parfums_Desktop\Eterea_Parfums_Desktop\Resources";

        /*Maxi*/
        private string rutaBase = @"C:\Users\Maxi\source\repos\Eterea_Parfums_Desktop\Eterea_Parfums_Desktop\Resources";


        public ActionResult Mostrar(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
                return HttpNotFound();

            string rutaArchivo = Path.Combine(rutaBase, nombre);

            if (!System.IO.File.Exists(rutaArchivo))
                return HttpNotFound();

            string tipoMime = MimeMapping.GetMimeMapping(rutaArchivo);
            byte[] archivoBytes = System.IO.File.ReadAllBytes(rutaArchivo);
            return File(archivoBytes, tipoMime);
        }
        // GET: Imagen
        public ActionResult Index()
        {
            return View();
        }

        // GET: Imagen/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Imagen/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Imagen/Create
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

        // GET: Imagen/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Imagen/Edit/5
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

        // GET: Imagen/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Imagen/Delete/5
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
