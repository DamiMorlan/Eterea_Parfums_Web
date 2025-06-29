using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
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
            var cliente = db.cliente.FirstOrDefault(c => c.usuario == usuario && c.clave == clave);

            if (cliente != null)
            {
                Session["clienteId"] = cliente.id;               // 👈 ID
                Session["usuarioLogueado"] = cliente.usuario;    // 👈 o guardar cliente directamente si lo usás más
                Session["clienteNombre"] = cliente.nombre;


                return RedirectToAction("Index", "Home"); // o "Perfume" si preferís
            }

            ViewData["Error"] = "Usuario o contraseña incorrectos.";
            return View();
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
                nuevoCliente.condicion_frente_al_iva = "Consumidor final";
                try
                {
                    db.cliente.Add(nuevoCliente);
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        Console.WriteLine($"Entidad de tipo {eve.Entry.Entity.GetType().Name} con estado {eve.Entry.State} tiene errores de validación:");
                        foreach (var ve in eve.ValidationErrors)
                        {
                            Console.WriteLine($"- Propiedad: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                        }
                    }
                    throw;
                }

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
                }
                else
                {
                    return 1;
                }
        }

       public ActionResult Perfil()
        {
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }

            int clienteId = (int)Session["clienteId"];

            using (var db = new etereaEntities1())
            {
                var cliente = db.cliente.Find(clienteId);

                if (cliente == null)
                {
                    return RedirectToAction("Login", "Cliente");
                }

                CargarPaises();
                return View(cliente);
            }
        }

        [HttpPost]
        public ActionResult Perfil(cliente clienteEditado)
        {
            string dniStr = clienteEditado.dni.ToString();
            if (dniStr.Length != 8 && dniStr.Length != 11)
            {
                ModelState.AddModelError("dni", "El DNI/CUIT debe tener 8 o 11 digitos.");
                CargarPaises();
                return View(clienteEditado);
            }

            if (!ModelState.IsValid)
            {

                Console.WriteLine("Hay errores de validación.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                CargarPaises();
                return View(clienteEditado);
            }

            using (var db = new etereaEntities1())
            {
                int clienteId = (int)Session["clienteId"];
                var usuarioLogueado = db.cliente.Find(clienteId);
                bool usuarioExiste = db.cliente.Any(c => c.usuario == clienteEditado.usuario && c.id != usuarioLogueado.id);
                if (usuarioExiste)
                {
                    ModelState.AddModelError("usuario", "El nombre de usuario ya está en uso.");
                    CargarPaises();
                    return View(clienteEditado);
                }

                bool dniExiste = db.cliente.Any(d => d.dni == clienteEditado.dni && d.dni != usuarioLogueado.dni);
                if (dniExiste)
                {
                    ModelState.AddModelError("dni", "Hay una cuenta existente con ese DNI.");
                    CargarPaises();
                    return View(clienteEditado);
                }

                bool emailExiste = db.cliente.Any(e => e.e_mail == clienteEditado.e_mail && e.e_mail != usuarioLogueado.e_mail);
                if (emailExiste)
                {
                    ModelState.AddModelError("email", "Hay una cuenta existente con ese email.");
                    CargarPaises();
                    return View(clienteEditado);
                }


                var clienteExistente = db.cliente.Find(usuarioLogueado.id);
                if (clienteExistente == null)
                {
                    return RedirectToAction("Login", "Cliente");
                }
                clienteExistente.nombre = clienteEditado.nombre;
                clienteExistente.apellido = clienteEditado.apellido;
                clienteExistente.usuario = clienteEditado.usuario;
                clienteExistente.clave = clienteEditado.clave;
                clienteExistente.e_mail = clienteEditado.e_mail;
                clienteExistente.dni = clienteEditado.dni;
                clienteExistente.fecha_nacimiento = clienteEditado.fecha_nacimiento;
                clienteExistente.celular = clienteEditado.celular;
                clienteExistente.pais_id = clienteEditado.pais_id;
                clienteExistente.provincia_id = clienteEditado.provincia_id;
                clienteExistente.localidad_id = clienteEditado.localidad_id;
                clienteExistente.calle_id = clienteEditado.calle_id;
                clienteExistente.numeracion_calle = clienteEditado.numeracion_calle;
                clienteExistente.piso = clienteEditado.piso;
                clienteExistente.departamento = clienteEditado.departamento;
                clienteExistente.codigo_postal = clienteEditado.codigo_postal;
                clienteExistente.comentarios_domicilio = clienteEditado.comentarios_domicilio;

                clienteExistente.activo = usuarioLogueado.activo;
                clienteExistente.rol = usuarioLogueado.rol;
                clienteExistente.id = usuarioLogueado.id;
                clienteExistente.condicion_frente_al_iva = usuarioLogueado.condicion_frente_al_iva;
                try
                {
                    Session["usuarioLogueado"] = clienteExistente;
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var eve in ex.EntityValidationErrors)
                    {
                        Console.WriteLine($"Entidad de tipo {eve.Entry.Entity.GetType().Name} con estado {eve.Entry.State} tiene errores de validación:");
                        foreach (var ve in eve.ValidationErrors)
                        {
                            Console.WriteLine($"- Propiedad: {ve.PropertyName}, Error: {ve.ErrorMessage}");
                        }
                    }
                    throw;
                }

                // Redireccionar a otra vista
                return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult VerPerfil()
        {
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }

            int clienteId = (int)Session["clienteId"];

            using (var db = new etereaEntities1())
            {
                var cliente = db.cliente.Find(clienteId);
                ViewBag.NombrePais = db.pais.Find(cliente.pais_id)?.nombre ?? "";
                ViewBag.NombreProvincia = db.provincia.Find(cliente.provincia_id)?.nombre ?? "";
                ViewBag.NombreLocalidad = db.localidad.Find(cliente.localidad_id)?.nombre ?? "";
                ViewBag.NombreCalle = db.calle.Find(cliente.calle_id)?.nombre ?? "";


                if (cliente == null)
                {
                    return RedirectToAction("Login", "Cliente");
                }

                CargarPaises();
                return View(cliente);
            }
        }
        public ActionResult Logout()
        {
            Session.Clear(); // Elimina todos los datos de sesión (incluye UsuarioId, etc.)

            return RedirectToAction("Index", "Home"); // Redirige a la pantalla principal
        }

    }
}