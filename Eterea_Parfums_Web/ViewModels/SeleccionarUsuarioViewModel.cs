using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class SeleccionarUsuarioViewModel
    {
        public string UsuarioSeleccionado { get; set; }
        public List<string> UsuariosDisponibles { get; set; }
    }
}
