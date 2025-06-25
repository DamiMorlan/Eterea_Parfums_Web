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
                     // Guardar el objeto Cliente en la sesión
                     Session["usuarioLogueado"] = usuarioLogueado;
                     // Usuario y contraseña válidos, redirigir a la página Index de la carpeta Carrito
                     return RedirectToAction("Index", "Home");
                 }
                 else
                 {
                     // Usuario o contraseña inválidos, volver a cargar la página de login con un mensaje de error
                     ViewData["Error"] = "Usuario o contraseña incorrectos";
                     return View();
                 }
             }
        }

        // GET: Cliente/Registrar
        public ActionResult Registrar()
        {
            CargarPaises();

            return View();
        }

        [HttpGet]
        public JsonResult ObtenerProvincias(int paisId)
        {
            using (var db = new etereaEntities1())
            {
                var provincias = db.provincia
                    .Where(p => p.pais_id == paisId)
                    .Select(p => new {
                        id = p.id,
                        nombre = p.nombre
                    }).ToList();

                return Json(provincias, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerLocalidades(int provinciaId)
        {
            using (var db = new etereaEntities1())
            {
                var localidades = db.localidad
                    .Where(l => l.provincia_id == provinciaId)
                    .Select(l => new {
                        id = l.id,
                        nombre = l.nombre
                    }).ToList();

                return Json(localidades, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ObtenerCalles(int localidadId)
        {
            using (var db = new etereaEntities1())
            {
                var calles = db.calle
                    .Where(c => c.localidad_id == localidadId)
                    .Select(c => new {
                        id = c.id,
                        nombre = c.nombre
                    }).ToList();

                return Json(calles, JsonRequestBehavior.AllowGet);
            }
        }

        private void CargarPaises()
        {
            using (var db = new etereaEntities1())
            {
                ViewBag.Paises = db.pais
                    .Select(p => new SelectListItem
                    {
                        Value = p.id.ToString(),
                        Text = p.nombre
                    }).ToList();
            }
        }

        // POST: Cliente/Registrar
        [HttpPost]
        public ActionResult Registrar(cliente nuevoCliente)
        {
            
            if (nuevoCliente.dni.ToString().Length != 8)
            {
                ModelState.AddModelError("dni", "El DNI debe tener 8 números.");
                CargarPaises();
                return View(nuevoCliente);
            }

            if (!ModelState.IsValid)
            {
                CargarPaises();
                return View(nuevoCliente);
            }

            using (var db = new etereaEntities1())
            {
                bool usuarioExiste = db.cliente.Any(c => c.usuario == nuevoCliente.usuario);
                if (usuarioExiste)
                {
                    ModelState.AddModelError("usuario", "El nombre de usuario ya está en uso.");
                    CargarPaises();
                    return View(nuevoCliente); 
                }

                bool dniExiste = db.cliente.Any(d => d.dni == nuevoCliente.dni);
                if (dniExiste)
                {
                    ModelState.AddModelError("dni", "Hay una cuenta existente con ese DNI.");
                    CargarPaises();
                    return View(nuevoCliente); 
                }

                bool emailExiste = db.cliente.Any(e => e.e_mail == nuevoCliente.e_mail);
                if (emailExiste) 
                {
                    ModelState.AddModelError("email", "Hay una cuenta existente con ese email.");
                    CargarPaises();
                    return View(nuevoCliente);
                }

                // Si todo está bien, lo guardás en la base:
                nuevoCliente.activo = true;
                nuevoCliente.rol = "cliente";
                nuevoCliente.id = ObtenerProximoIdDelCliente();
                db.cliente.Add(nuevoCliente);
                db.SaveChanges();

                // Redireccionar a otra vista
                return RedirectToAction("Login", "Cliente");
            }
        }

        public int ObtenerProximoIdDelCliente()
        {
            using (var db = new etereaEntities1())
                if (db.cliente.Any())
                {
                    return db.cliente.Max(i => i.id) + 1;
                }else {
                    return 1;
                }
        }

        public ActionResult Perfil()
        {
            return View();
        }
    }
}