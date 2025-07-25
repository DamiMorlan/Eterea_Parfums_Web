using Eterea_Parfums_Desktop;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Eterea_Parfums_Web.Helpers;

namespace Eterea_Parfums_Web.Controllers
{
    public class ClienteController : Controller
    {

        private etereaEntities7 db = new etereaEntities7();
        // GET: Cliente/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Cliente/Login
        [HttpPost]
        public ActionResult Login(string usuario, string clave)
        {
            var cliente = db.cliente.FirstOrDefault(c => c.usuario == usuario);

            if (cliente != null && PasswordHelper.VerificarPassword(clave, cliente.clave))
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

            return View(new FormularioClienteViewModel());
        }

        [HttpGet]
        public JsonResult ObtenerProvincias(int paisId)
        {
            using (var db = new etereaEntities7())
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
            using (var db = new etereaEntities7())
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
            using (var db = new etereaEntities7())
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
            using (var db = new etereaEntities7())
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
        /*public ActionResult Registrar(cliente nuevoCliente)
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

            using (var db = new etereaEntities7())
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
                nuevoCliente.clave = PasswordHelper.CrearHash(nuevoCliente.clave);
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
        }*/

        public ActionResult Registrar(FormularioClienteViewModel model)
        {

            if (!ModelState.IsValid)
            {
                CargarPaises();
                return View(model);
            }

            using (var db = new etereaEntities7())
            {
                /*if (db.cliente.Any(c => c.usuario == model.Usuario))
                {
                    ModelState.AddModelError("Usuario", "El nombre de usuario ya está en uso.");
                    CargarPaises();
                    return View(model);
                }

                if (db.cliente.Any(c => c.dni == model.Dni))
                {
                    ModelState.AddModelError("Dni", "Ya existe una cuenta con ese DNI.");
                    CargarPaises();
                    return View(model);
                }

                if (db.cliente.Any(c => c.e_mail == model.Email))
                {
                    ModelState.AddModelError("Email", "Ya existe una cuenta con ese correo.");
                    CargarPaises();
                    return View(model);
                }*/

                // Mapeo del ViewModel al Entity
                var nuevoCliente = new cliente
                {
                    id = ObtenerProximoIdDelCliente(),
                    nombre = model.Nombre,
                    apellido = model.Apellido,
                    usuario = model.Usuario,
                    clave = PasswordHelper.CrearHash(model.Clave),
                    dni = (long)model.Dni,
                    fecha_nacimiento = model.FechaNacimiento,
                    celular = model.Celular,
                    e_mail = model.Email,
                    pais_id = model.PaisId,
                    provincia_id = model.ProvinciaId,
                    localidad_id = model.LocalidadId,
                    calle_id = model.CalleId,
                    numeracion_calle = (int)model.NumeracionCalle,
                    piso = model.Piso,
                    departamento = model.Departamento,
                    codigo_postal = model.CodigoPostal,
                    comentarios_domicilio = model.ComentariosDomicilio,
                    condicion_frente_al_iva = "Consumidor final",
                    activo = true,
                    rol = "cliente"
                };

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
            }

            return RedirectToAction("Login", "Cliente");
        }


        [HttpGet]
        public JsonResult ValidarUsuario(string usuario)
        {
            using (var db = new etereaEntities7())
            {
                bool existe = db.cliente.Any(c => c.usuario == usuario);
                return Json(!existe, JsonRequestBehavior.AllowGet); // Devuelve true si es válido (no existe)
            }
        }

        [HttpGet]
        public JsonResult ValidarDni(long dni)
        {
            using (var db = new etereaEntities7())
            {
                bool existe = db.cliente.Any(c => c.dni == dni);
                return Json(!existe, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ValidarEmail(string email)
        {
            using (var db = new etereaEntities7())
            {
                bool existe = db.cliente.Any(c => c.e_mail == email);
                return Json(!existe, JsonRequestBehavior.AllowGet);
            }
        }

        public int ObtenerProximoIdDelCliente()
        {
            using (var db = new etereaEntities7())
                if (db.cliente.Any())
                {
                    return db.cliente.Max(i => i.id) + 1;
                }
                else
                {
                    return 1;
                }
        }

        [HttpGet]
        public ActionResult Perfil()
        {
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];
            var cliente = db.cliente.Find(clienteId);
            if (cliente == null)
                return RedirectToAction("Login", "Cliente");

            var model = new FormularioPerfilViewModel
            {
                Usuario = cliente.usuario,
                Dni = cliente.dni,
                Email = cliente.e_mail,
                Nombre = cliente.nombre,
                Apellido = cliente.apellido,
                FechaNacimiento = cliente.fecha_nacimiento,
                Celular = cliente.celular,
                PaisId = cliente.pais_id,
                ProvinciaId = cliente.provincia_id,
                LocalidadId = cliente.localidad_id,
                CalleId = cliente.calle_id,
                NumeracionCalle = cliente.numeracion_calle,
                Piso = cliente.piso,
                Departamento = cliente.departamento,
                CodigoPostal = cliente.codigo_postal.HasValue ? cliente.codigo_postal.Value : (int?)null,
                ComentariosDomicilio = cliente.comentarios_domicilio
            };

            CargarPaises();
            return View(model);
        }



        public ActionResult SeleccionarDireccion()
        {
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];

            // 1. Obtener el DNI del cliente logueado
            var dni = db.cliente
                .Where(c => c.id == clienteId)
                .Select(c => c.dni)
                .FirstOrDefault();

            // 2. Buscar los domicilios usados por ese cliente (texto completo)
            var textos = db.orden
             .Where(o => o.dni == dni && o.domicilio_de_envio != null)
             .AsEnumerable()
             .Select(o => o.domicilio_de_envio.Trim().Replace("\r\n", "\n").Replace("\n", " ").Replace("  ", " ").Trim())
             .Distinct(StringComparer.InvariantCultureIgnoreCase) // ✅ comparación sin mayúsculas/minúsculas
             .ToList();

            // Esto evita duplicados con saltos de línea distintos o espacios

            // 3. Transformar en ViewModels divididos en dos líneas
            var modelo = textos.Select(t =>
            {
                string linea1 = "";
                string linea2 = "";

                int indiceCP = t.IndexOf("C.P.");

                if (indiceCP > 0)
                {
                    linea1 = t.Substring(0, indiceCP).Trim();
                    linea2 = t.Substring(indiceCP).Trim();
                }
                else
                {
                    // Por si no se encuentra "C.P.:"
                    linea1 = t;
                }


                return new DireccionFormateadaViewModel
                {
                    Linea1 = linea1,
                    Linea2 = linea2
                };
            }).ToList();

            return View(modelo);
        }


        [HttpGet]
        public ActionResult AgregarDireccion()
        {
            return View(new DireccionEntregaViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarDireccion(DireccionEntregaViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Conversión segura de datos al llamar al helper
            string calle = model.Calle?.Trim() ?? "";
            int numeracion = 0;
            int? cp = null;

            if (!string.IsNullOrWhiteSpace(model.Numeracion))
                int.TryParse(model.Numeracion.Trim(), out numeracion);

            if (!string.IsNullOrWhiteSpace(model.CodigoPostal))
                cp = int.TryParse(model.CodigoPostal.Trim(), out int tmp) ? tmp : (int?)null;

            string piso = model.Piso?.Trim() ?? "";
            string depto = model.Departamento?.Trim() ?? "";
            string localidad = model.Localidad?.Trim() ?? "";
            string provincia = model.Provincia?.Trim() ?? "";

            // Usa el helper centralizado para construir el texto formateado
            Session["NuevoDomicilioEntrega"] = DireccionHelper.ConstruirTextoCompleto(
                calle,
                numeracion,
                piso,
                depto,
                cp,
                localidad,
                provincia
            );

            return RedirectToAction("VistaPrevia", "Pedido");
        }



        [HttpPost]
        public ActionResult SeleccionarDireccionConfirmar(string texto)
        {
            Session["DomicilioDeEnvioTexto"] = texto;
            Session["NuevoDomicilioEntrega"] = texto;

            // Redirige a VistaPrevia
            return RedirectToAction("VistaPrevia", "Pedido");
        }


        private string ConstruirDireccionEnvio(etereaEntities7 db, cliente cli)
        {
            // Obtener nombres desde claves foráneas
            var calle = db.calle.Find(cli.calle_id)?.nombre ?? "";
            var localidad = db.localidad.Find(cli.localidad_id)?.nombre ?? "";
            var provincia = db.provincia.Find(cli.provincia_id)?.nombre ?? "";

            // Usar el helper centralizado con tipos correctos
            return DireccionHelper.ConstruirTextoCompleto(
                calle,
                cli.numeracion_calle,
                cli.piso ?? "",
                cli.departamento ?? "",
                cli.codigo_postal,
                localidad,
                provincia
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Perfil(FormularioPerfilViewModel model)
        {
            if (!ModelState.IsValid)
            {
                CargarPaises();
                return View(model);
            }

            int clienteId = (int)Session["clienteId"];
            var clienteExistente = db.cliente.Find(clienteId);
            if (clienteExistente == null)
                return RedirectToAction("Login", "Cliente");

            // Validación manual por si DNI debe aceptar también CUIT
            var dniStr = model.Dni.ToString();
            if (dniStr.Length != 8 && dniStr.Length != 11)
            {
                ModelState.AddModelError("Dni", "El DNI/CUIT debe tener 8 o 11 dígitos.");
                CargarPaises();
                return View(model);
            }

            // Usuario, DNI, email únicos si cambiaron
            if (db.cliente.Any(c => c.usuario == model.Usuario && c.id != clienteId))
            {
                ModelState.AddModelError("Usuario", "El nombre de usuario ya está en uso.");
                CargarPaises();
                return View(model);
            }
            if (db.cliente.Any(c => c.dni == model.Dni && c.id != clienteId))
            {
                ModelState.AddModelError("Dni", "Ya existe una cuenta con ese DNI.");
                CargarPaises();
                return View(model);
            }
            if (db.cliente.Any(c => c.e_mail == model.Email && c.id != clienteId))
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con ese correo.");
                CargarPaises();
                return View(model);
            }

            // Mapear propiedades
            clienteExistente.usuario = model.Usuario;
            clienteExistente.dni = model.Dni;
            clienteExistente.e_mail = model.Email;
            clienteExistente.nombre = model.Nombre;
            clienteExistente.apellido = model.Apellido;
            clienteExistente.fecha_nacimiento = model.FechaNacimiento;
            clienteExistente.celular = model.Celular;
            clienteExistente.pais_id = model.PaisId;
            clienteExistente.provincia_id = model.ProvinciaId;
            clienteExistente.localidad_id = model.LocalidadId;
            clienteExistente.calle_id = model.CalleId;
            clienteExistente.numeracion_calle = model.NumeracionCalle.Value;
            clienteExistente.piso = model.Piso;
            clienteExistente.departamento = model.Departamento;
            clienteExistente.codigo_postal = model.CodigoPostal;
            clienteExistente.comentarios_domicilio = model.ComentariosDomicilio;

            if (!string.IsNullOrWhiteSpace(model.Clave))
            {
                clienteExistente.clave = PasswordHelper.CrearHash(model.Clave);
            }

            db.SaveChanges();
            Session["usuarioLogueado"] = clienteExistente;

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        public ActionResult VerPerfil()
        {
            if (Session["clienteId"] == null)
            {
                return RedirectToAction("Login", "Cliente");
            }

            int clienteId = (int)Session["clienteId"];

            using (var db = new etereaEntities7())
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