using Eterea_Parfums_Desktop;
using Eterea_Parfums_Web.Filters;
using Eterea_Parfums_Web.Helpers;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class ClienteController : Controller
    {

        private etereaEntities7 db = new etereaEntities7();
        
        // GET: Cliente/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(string usuario, string clave)
        {
            // 1) Buscar cliente por usuario
            var cliente = db.cliente.FirstOrDefault(c => c.usuario == usuario);

            // 2) Si no existe o la contraseña es incorrecta → error de login
            if (cliente == null || !PasswordHelper.VerificarPassword(clave, cliente.clave))
            {
                ViewData["Error"] = "Usuario o contraseña incorrectos.";
                return View();
            }

            // 3) Login correcto → setear sesión
            Session["clienteId"] = cliente.id;
            Session["usuarioLogueado"] = cliente.usuario;
            Session["clienteNombre"] = cliente.nombre;

            // 4) Validar si la contraseña cumple requisitos de seguridad
            string patronSeguridad = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!¡\""#\$%&/()=?¿]).{8,}$";
            bool cumpleRequisitos = System.Text.RegularExpressions.Regex.IsMatch(clave, patronSeguridad);

            // 5) Ver si hay que forzar completar perfil (domicilio, fecha, etc.)
            bool debeForzarPerfil = DebeForzarCompletarPerfil(cliente, clave);

            // 5.b) Determinar motivo específico
            string dniStr = cliente.dni.ToString();
            bool esPrimerLoginReal = cliente.usuario == dniStr && clave == dniStr;
            bool claveInsegura = !cumpleRequisitos && !esPrimerLoginReal;

            // 5.c) Activar bandera global
            bool forzar = esPrimerLoginReal || claveInsegura || debeForzarPerfil;
            Session["ForzarCompletarPerfil"] = forzar;

            if (forzar)
            {
                if (esPrimerLoginReal)
                {
                    // Caso 1 — primer ingreso real con usuario/clave = DNI
                    TempData["AvisoPasswordInsegura"] =
                        "¡Es tu primera vez en nuestra web, bienvenido/a! " +
                        "Te pedimos que actualices tu usuario y tu contraseña y completes los datos " +
                        "faltantes para finalizar tu registro.";
                }
                else if (claveInsegura)
                {
                    // Caso 2 — usuario habitual con clave vieja no segura
                    TempData["AvisoPasswordInsegura"] =
                        "Por normas de seguridad se han actualizado los requisitos de contraseña. " +
                        "Tu contraseña actual ya no cumple con los nuevos estándares, por lo que " +
                        "deberás generar una nueva contraseña que respete las nuevas reglas.";
                }
                else
                {
                    // Caso 3 — otros datos incompletos (domicilio, fecha, etc.)
                    TempData["AvisoPasswordInsegura"] =
                        "Para continuar es necesario que completes tus datos de perfil.";
                }

                return RedirectToAction("Perfil", "Cliente", new { primerLogin = esPrimerLoginReal });
            }

            // 6) Todo OK → ir al Home
            TempData["LoginOK"] = $"¡Bienvenido/a {cliente.nombre}!";

            // 6) Todo OK → ir al Home
            return RedirectToAction("Index", "Home");
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
                    .Where(p => p.id != 1) // 👈 excluye el id 1
                    .Select(p => new SelectListItem
                    {
                        Value = p.id.ToString(),
                        Text = p.nombre
                    }).ToList();
            }
        }



        // POST: Cliente/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(FormularioClienteViewModel model)
        {

            if (!ModelState.IsValid)
            {
                CargarPaises();
                return View(model);
            }

            using (var db = new etereaEntities7())
            {

                // Mapeo del ViewModel al Entity
                var nuevoCliente = new cliente
                {
                    id = ObtenerProximoIdDelCliente(),
                    nombre = model.Nombre,
                    apellido = model.Apellido,
                    usuario = model.Usuario,
                    clave = PasswordHelper.CrearHash(model.Clave),
                    dni = (long)model.Dni,
                    fecha_nacimiento = (DateTime)model.FechaNacimiento,
                    celular = model.Celular,
                    e_mail = model.Email,
                    pais_id = (int)model.PaisId,
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

                    // 📧 Enviar mail de bienvenida al registrarse
                    CorreoHelper.EnviarCorreoBienvenidaRegistro(
                        nuevoCliente.e_mail,
                        nuevoCliente.nombre,
                        nuevoCliente.usuario
                    );
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
            TempData["RegistroOK"] = "Tu registro se completó correctamente. Te enviamos un correo de bienvenida.";

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
        public ActionResult Perfil(bool? primerLogin)
        {
            // Siempre tomar el cliente desde la sesión
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];

            var cliente = db.cliente.Find(clienteId);
            if (cliente == null)
                return RedirectToAction("Login", "Cliente");

            // Mapear entity → ViewModel
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

            // Bandera para la vista (por si querés mostrar textos especiales)
            bool forzar = false;
            if (Session["ForzarCompletarPerfil"] != null)
                bool.TryParse(Session["ForzarCompletarPerfil"].ToString(), out forzar);

            ViewBag.EsPrimerLogin = (primerLogin ?? false) || forzar;

            // ================================
            //  ❗ Agregar errores por campo
            //  cuando haya que forzar perfil
            // ================================
            if (forzar)
            {
                string docStr = cliente.dni.ToString();

                // 1) Usuario igual al DNI/CUIT
                if (!string.IsNullOrEmpty(cliente.usuario) && cliente.usuario == docStr)
                {
                    ModelState.AddModelError(
                        "Usuario",
                        "Debes actualizar tu nombre de usuario, no puede ser tu DNI o CUIT."
                    );
                }

                // 2) Contraseña igual al DNI/CUIT
                bool passwordEsDoc = PasswordHelper.VerificarPassword(docStr, cliente.clave);
                if (passwordEsDoc)
                {
                    ModelState.AddModelError(
                        "Clave",
                        "Debes actualizar tu contraseña, no puede ser tu DNI o CUIT."
                    );
                }

                // 3) Celular vacío o en "0"
                var cel = (cliente.celular ?? "").Trim();
                if (string.IsNullOrEmpty(cel) || cel == "0")
                {
                    ModelState.AddModelError(
                        "Celular",
                        "Debes ingresar un número de celular."
                    );
                }

                // 4) Fecha de nacimiento (solo si es DNI de 8 dígitos)
                string dniStr = cliente.dni.ToString();
                if (dniStr.Length == 8) // DNI
                {
                    var fecha = cliente.fecha_nacimiento;
                    int edad = CalcularEdad(fecha);

                    if (fecha.Year == 1900)
                    {
                        ModelState.AddModelError(
                            "FechaNacimiento",
                            "Debes actualizar tu fecha de nacimiento. No puede quedar en 1900."
                        );
                    }
                    else if (edad < 18)
                    {
                        ModelState.AddModelError(
                            "FechaNacimiento",
                            "Debes ser mayor de 18 años para registrarte con DNI."
                        );
                    }
                }

                // 5) Domicilio: pais / provincia / localidad / calle = 1 ("SIN DATO")
                if (cliente.pais_id == 1)
                {
                    ModelState.AddModelError(
                        "PaisId",
                        "Debes seleccionar un país."
                    );
                }

                if (cliente.provincia_id == 1)
                {
                    ModelState.AddModelError(
                        "ProvinciaId",
                        "Debes seleccionar una provincia."
                    );
                }

                if (cliente.localidad_id == 1)
                {
                    ModelState.AddModelError(
                        "LocalidadId",
                        "Debes seleccionar una localidad."
                    );
                }

                if (cliente.calle_id == 1)
                {
                    ModelState.AddModelError(
                        "CalleId",
                        "Debes seleccionar una calle."
                    );
                }

                // 6) Numeración
                if (cliente.numeracion_calle == 0)
                {
                    ModelState.AddModelError(
                        "NumeracionCalle",
                        "Debes ingresar la altura del domicilio."
                    );
                }

                // 7) Código postal
                if (!cliente.codigo_postal.HasValue ||
                    cliente.codigo_postal.Value.ToString().Length != 4)
                {
                    ModelState.AddModelError(
                        "CodigoPostal",
                        "Debes ingresar el código postal de 4 dígitos."
                    );
                }
            }

            // Cargar combos 
            CargarPaises();
         

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Perfil(FormularioPerfilViewModel model, bool? primerLogin)
        {
            // 1) Chequear sesión
            if (Session["clienteId"] == null)
                return RedirectToAction("Login", "Cliente");

            int clienteId = (int)Session["clienteId"];

            var clienteExistente = db.cliente.Find(clienteId);
            if (clienteExistente == null)
                return RedirectToAction("Login", "Cliente");

            // ¿Sigue siendo perfil incompleto? (primer login o forzado)
            bool esPrimerLogin = (Session["ForzarCompletarPerfil"] as bool?) ?? false;
            ViewBag.EsPrimerLogin = esPrimerLogin;

            // Documento original y el que viene del formulario
            string docBase = clienteExistente.dni.ToString(); // lo que tiene grabado
            string docForm = model.Dni.ToString();            // lo que viene en el form

            // ---------------------------------------------------
            // 2) REGLAS DE SEGURIDAD EXTRA
            // ---------------------------------------------------

            // 2.1) USUARIO no puede ser DNI/CUIT
            //     – si es primer login usamos el doc original para comparar
            string docParaUsuario = esPrimerLogin ? docBase : docForm;
            if (model.Usuario == docParaUsuario)
            {
                ModelState.AddModelError(
                    "Usuario",
                    "Debes actualizar tu nombre de usuario, no puede ser tu DNI o CUIT."
                );
            }

            // 2.2) CONTRASEÑA no puede ser DNI/CUIT y debe cumplir el patrón
            bool passwordActualEsDoc = PasswordHelper.VerificarPassword(docBase, clienteExistente.clave);

            // patrón de seguridad que pedís
            string patronSeguridad = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!¡""#$%&/()=?¿]).{8,}$";

            // 2.2.a) PRIMER LOGIN → OBLIGATORIO cambiar la contraseña
            if (esPrimerLogin && passwordActualEsDoc)
            {
                if (string.IsNullOrWhiteSpace(model.Clave))
                {
                    ModelState.AddModelError(
                        "Clave",
                        "Debes ingresar una nueva contraseña, no puede seguir siendo tu DNI o CUIT."
                    );
                }
                else
                {
                    if (model.Clave == docBase)
                    {
                        ModelState.AddModelError(
                            "Clave",
                            "La nueva contraseña no puede ser tu DNI o CUIT."
                        );
                    }
                    else if (!System.Text.RegularExpressions.Regex.IsMatch(model.Clave, patronSeguridad))
                    {
                        ModelState.AddModelError(
                            "Clave",
                            "La clave debe tener 8+ caracteres, incluir mayúsculas, letras, números y 1 carácter especial (!, ¡, \", #, $, %, &, /, (, ), =, ?, ¿)."
                        );
                    }
                }
            }
            // 2.2.b) NO es primer login → la contraseña es opcional, pero si la cambia debe ser válida
            else if (!string.IsNullOrWhiteSpace(model.Clave))
            {
                if (model.Clave == docForm)
                {
                    ModelState.AddModelError(
                        "Clave",
                        "La nueva contraseña no puede ser tu DNI o CUIT."
                    );
                }
                else if (!System.Text.RegularExpressions.Regex.IsMatch(model.Clave, patronSeguridad))
                {
                    ModelState.AddModelError(
                        "Clave",
                        "La clave debe tener 8+ caracteres, incluir mayúsculas, letras, números y 1 carácter especial (!, ¡, \", #, $, %, &, /, (, ), =, ?, ¿)."
                    );
                }
            }

            // 2.3) CELULAR obligatorio y distinto de "0"
            if (string.IsNullOrWhiteSpace(model.Celular) || model.Celular.Trim() == "0")
            {
                ModelState.AddModelError(
                    "Celular",
                    "Debes ingresar un número de celular."
                );
            }

            // ---------------------------------------------------
            // 3) Validación de DNI y FECHA de nacimiento
            // ---------------------------------------------------

            // DNI longitud
            if (docForm.Length != 8 && docForm.Length != 11)
            {
                ModelState.AddModelError("Dni",
                    "Ingresá un documento válido: 8 dígitos si es DNI o 11 dígitos si es CUIT para completar tu registro.");
            }

            // Si es DNI (8 dígitos), obligamos fecha válida, >18 y <100 y ≠1900
            if (docForm.Length == 8)
            {
                var fecha = model.FechaNacimiento;

                if (fecha == default(DateTime))
                {
                    ModelState.AddModelError(
                        "FechaNacimiento",
                        "Debes ingresar tu fecha de nacimiento si usas DNI."
                    );
                }
                else
                {
                    int edad = CalcularEdad(fecha);

                    // acá se filtra 01/01/1900, menores de 18 y mayores de 100
                    if (fecha.Year == 1900 || edad < 18 || edad > 100)
                    {
                        ModelState.AddModelError(
                            "FechaNacimiento",
                            "Debes ingresar una fecha de nacimiento válida: mayor de 18 años, no mayor de 100 y distinta de 1900."
                        );
                    }
                }
            }

            // ---------------------------------------------------
            // 4) Unicidad de usuario / DNI / email
            // ---------------------------------------------------
            if (db.cliente.Any(c => c.usuario == model.Usuario && c.id != clienteId))
            {
                ModelState.AddModelError("Usuario",
                    "El nombre de usuario ya está en uso. Elegí otro para poder finalizar tu registro.");
            }

            if (db.cliente.Any(c => c.dni == model.Dni && c.id != clienteId))
            {
                ModelState.AddModelError("Dni", "Ya existe una cuenta con ese DNI/CUIT.");
            }

            if (db.cliente.Any(c => c.e_mail == model.Email && c.id != clienteId))
            {
                ModelState.AddModelError("Email", "Ya existe una cuenta con ese correo.");
            }

            // ---------------------------------------------------
            // 5) Validaciones de domicilio "SIN DATO"
            // ---------------------------------------------------
            if (model.PaisId == 1)
                ModelState.AddModelError("PaisId", "Debe seleccionar un país válido.");
            if (model.ProvinciaId == 1)
                ModelState.AddModelError("ProvinciaId", "Debe seleccionar una provincia válida.");
            if (model.LocalidadId == 1)
                ModelState.AddModelError("LocalidadId", "Debe seleccionar una localidad válida.");
            if (model.CalleId == 1)
                ModelState.AddModelError("CalleId", "Debe seleccionar una calle válida.");
            if (!model.CodigoPostal.HasValue || model.CodigoPostal.Value.ToString().Length != 4)
                ModelState.AddModelError("CodigoPostal", "Debe ingresar el código postal de 4 dígitos.");
            if (!model.NumeracionCalle.HasValue || model.NumeracionCalle.Value <= 0)
                ModelState.AddModelError("NumeracionCalle", "Debes ingresar la altura del domicilio.");

            // ---------------------------------------------------
            // 6) Si hay CUALQUIER error → NO guardamos
            // ---------------------------------------------------
            if (!ModelState.IsValid)
            {
                // sigue siendo perfil incompleto → mantener el flag para que lo sigan forzando
                Session["ForzarCompletarPerfil"] = true;

                CargarPaises();
                return View(model);
            }

            // ---------------------------------------------------
            // 7) Si llegamos acá → todos los datos son válidos
            // ---------------------------------------------------

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

            // Cambio de clave solo si escribió una nueva (ya validada)
            if (!string.IsNullOrWhiteSpace(model.Clave))
            {
                clienteExistente.clave = PasswordHelper.CrearHash(model.Clave);
            }

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al guardar Perfil: " + ex);
                ModelState.AddModelError("", "Ocurrió un error al guardar los datos. Intente nuevamente.");
                CargarPaises();
                return View(model);
            }

            // perfil completo → dejo de forzar
            Session["ForzarCompletarPerfil"] = false;

            // refrescar datos de sesión
            Session["clienteId"] = clienteExistente.id;
            Session["usuarioLogueado"] = clienteExistente.usuario;
            Session["clienteNombre"] = clienteExistente.nombre;

            TempData["Mensaje"] = primerLogin == true
                ? "Tu perfil se actualizó correctamente. ¡Ya podés usar el carrito!"
                : "Perfil fue editado correctamente";

            return RedirectToAction("Index", "Home");
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



        [ForzarPerfilCompleto]
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


        [HttpGet]
        public ActionResult OlvidarPassword() {
            return View();
        }

        [HttpPost]
        public ActionResult OlvidarPassword(OlvidarPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cliente = db.cliente.FirstOrDefault(c => c.e_mail == model.Email && c.activo);
            if (cliente == null)
            {
                // No revelar si el email existe por seguridad
                ViewBag.Mensaje = "Si el correo está registrado, recibirás un enlace.";
                return View();
            }

            // Generar y guardar token con expiración
            string token = Guid.NewGuid().ToString();
            cliente.token_recuperacion = token;
            //Tiempo de vida del link
            cliente.token_expiracion = DateTime.Now.AddHours(1);
            db.SaveChanges();

            // Se construye enlace con token
            string link = Url.Action("ResetearPassword", "Cliente", new { token }, protocol: Request.Url.Scheme);

            // Enviar correo
            CorreoHelper.EnviarCorreoGenerico(
                model.Email,
                "Recuperar contraseña",
                $"Hacé clic en el siguiente enlace para restablecer tu contraseña:\n\n{link}\n\nEste enlace expirará en 1 hora."
            );

            ViewBag.Mensaje = "Te enviamos un enlace para restablecer tu contraseña.";
            return View();
        }


        [HttpGet]
        public ActionResult ResetearPassword(string token)
        {
            var cliente = db.cliente.FirstOrDefault(c => c.token_recuperacion == token && c.token_expiracion > DateTime.Now);
            if (cliente == null)
                return HttpNotFound();

            return View(new ResetearPasswordViewModel { Token = token });
        }

        [HttpPost]
        public ActionResult ResetearPassword(ResetearPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var cliente = db.cliente.FirstOrDefault(c => c.token_recuperacion == model.Token && c.token_expiracion > DateTime.Now);
            if (cliente == null)
                return HttpNotFound();

            cliente.clave = PasswordHelper.CrearHash(model.NuevaPassword);
            cliente.token_recuperacion = null;
            cliente.token_expiracion = null;
            db.SaveChanges();

            return RedirectToAction("Login");
        }


        private int CalcularEdad(DateTime fechaNacimiento)
        {
            var hoy = DateTime.Today;
            int edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento > hoy.AddYears(-edad))
                edad--;
            return edad;
        }

        private bool DebeForzarCompletarPerfil(cliente c, string claveIngresada)
        {
            bool usuarioClaveDocIgual = false;

            // c.dni es long
            if (c.dni > 0)
            {
                string dniStr = c.dni.ToString();

                // Caso inicial: usuario y contraseña = DNI
                if (!string.IsNullOrEmpty(c.usuario) &&
                    !string.IsNullOrEmpty(claveIngresada) &&
                    c.usuario == dniStr &&
                    claveIngresada == dniStr)
                {
                    usuarioClaveDocIgual = true;
                }
            }

            // 🟣 FECHA DE NACIMIENTO INVÁLIDA SOLO PARA DNI (8 dígitos)
            bool fechaInvalida = false;
            string dniActual = c.dni.ToString();

            if (dniActual.Length == 8)   // es DNI, no CUIT
            {
                DateTime fecha = c.fecha_nacimiento; // en tu modelo no es nullable

                int edad = CalcularEdad(fecha);

                if (fecha.Year == 1900 || edad < 18)
                {
                    fechaInvalida = true;
                }
            }



            // Domicilio incompleto con la lógica de SIN DATO
            bool domicilioIncompleto =
                   c.pais_id == 1
                || c.provincia_id == 1
                || c.localidad_id == 1
                || c.calle_id == 1
                || !c.codigo_postal.HasValue
                || c.codigo_postal.Value.ToString().Length != 4
                || c.numeracion_calle == 0;

            // ⬅️ ahora también se tiene en cuenta la fecha
            return usuarioClaveDocIgual || domicilioIncompleto || fechaInvalida;
        }



    }
}