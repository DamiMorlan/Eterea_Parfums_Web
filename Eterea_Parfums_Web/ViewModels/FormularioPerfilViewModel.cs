namespace Eterea_Parfums_Web.ViewModels
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Web.Mvc;

    public class FormularioPerfilViewModel
    {
        public int Id;

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(16, ErrorMessage = "Máximo 16 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(16, ErrorMessage = "Máximo 16 caracteres.")]
        public string Apellido { get; set; }
        [Required, StringLength(8, MinimumLength = 4)]
        public string Usuario { get; set; }

        [MinLength(8, ErrorMessage = "Debe tener al menos 8 caracteres.")]
        [StringLength(25, ErrorMessage = "Máximo 25 caracteres.")]
        [DataType(DataType.Password)]
        public string Clave { get; set; }

        [Required, Range(10000000, 99999999)]
        public long Dni { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        [Required(ErrorMessage = "El celular es obligatorio.")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        public string Celular { get; set; }

        [Required, StringLength(30)]
        public string Email { get; set; }



        [Required(ErrorMessage = "Seleccione un país.")]
        public int PaisId { get; set; }

        [Required(ErrorMessage = "Seleccione una provincia.")]
        public int ProvinciaId { get; set; }

        [Required(ErrorMessage = "Seleccione una localidad.")]
        public int LocalidadId { get; set; }

        [Required(ErrorMessage = "La calle es obligatoria")]
        public int CalleId { get; set; }

        [Required(ErrorMessage = "La numeración es obligatoria")]
        [Range(1, 99999, ErrorMessage = "Ingrese un número que contenga hasta 5 dígitos.")]
        public int? NumeracionCalle { get; set; }

        [Required(ErrorMessage = "Debe ingresar un número de piso")]
        [StringLength(5, ErrorMessage = "Máximo 5 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]*$", ErrorMessage = "Solo letras y números")]
        public string Piso { get; set; }

        [Required(ErrorMessage = "Debe ingresar un número de departamento")]
        [StringLength(5, ErrorMessage = "Máximo 5 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]*$", ErrorMessage = "Solo letras y números")]
        public string Departamento { get; set; }

        [Required(ErrorMessage = "El código postal es obligatorio")]
        [Range(1000, 9999, ErrorMessage = "El código postal debe tener 4 dígitos.")]
        public int? CodigoPostal { get; set; }

        [MaxLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        public string ComentariosDomicilio { get; set; }
    }

}