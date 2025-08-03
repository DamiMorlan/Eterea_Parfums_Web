using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Eterea_Parfums_Web.ViewModels
{
    public class OlvidarPasswordViewModel
    {
        [EmailAddress(ErrorMessage = "Ingrese un email válido.")]
        [Required(ErrorMessage = "El email es obligatorio.")]
        [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
        public string Email { get; set; }
    }
}