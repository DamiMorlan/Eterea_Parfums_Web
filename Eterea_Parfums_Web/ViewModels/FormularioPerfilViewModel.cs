namespace Eterea_Parfums_Web.ViewModels
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Eterea_Parfums_Web.Validations;
    using System.Web.Mvc;

    public class FormularioPerfilViewModel
    {
        [Display(Name = "Nombre:")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(16, ErrorMessage = "Máximo 16 caracteres.")]
        public string Nombre { get; set; }

        [Display(Name = "Apellido:")]
        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(16, ErrorMessage = "Máximo 16 caracteres.")]
        public string Apellido { get; set; }

        [Display(Name = "Usuario:")]
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(8, MinimumLength = 4, ErrorMessage = "Debe tener entre 4 y 8 caracteres.")]
        public string Usuario { get; set; }

        [Display(Name = "Clave:")]
        [MinLength(8, ErrorMessage = "Debe tener al menos 8 caracteres.")]
        [StringLength(25, ErrorMessage = "Máximo 25 caracteres.")]
        [DataType(DataType.Password)]
        public string Clave { get; set; }

        [Display(Name = "DNI:")]
        [Range(10000000, 99999999999, ErrorMessage = "Debe ingresar un DNI (8 dígitos) o un CUIT (11 dígitos).")]
        public long Dni { get; set; }

        [Display(Name = "Fecha de Nacimiento:")]
        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [FechaMenorQueActual(EdadMinima = 18, EdadMaxima = 250, ErrorMessage = "La fecha debe ser válida y debes tener 18 años como mínimo.")]
        public DateTime FechaNacimiento { get; set; }

        [Display(Name = "Celular")]
        [Required(ErrorMessage = "El celular es obligatorio.")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres.")]
        [RegularExpression(@"^(\+54\s?9\s?\d{2}\s?\d{4}-?\d{4}|\+54\s?\d{10}|\d{10,11})$",
         ErrorMessage = "Ingrese un número de celular válido, con o sin código de país.")]
        public string Celular { get; set; }

        [Display(Name = "Email:")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido.")]
        [Required(ErrorMessage = "El email es obligatorio.")]
        [StringLength(30, ErrorMessage = "Máximo 30 caracteres.")]
        public string Email { get; set; }

        [Display(Name = "País:")]
        [Required(ErrorMessage = "Seleccione un país.")]
        [Range(2, int.MaxValue, ErrorMessage = "Seleccione un país válido u otra opcion.")]
        public int PaisId { get; set; }

        [Display(Name = "Provincia:")]
        [Required(ErrorMessage = "Seleccione una provincia.")]
        [Range(2, int.MaxValue, ErrorMessage = "Seleccione una provincia válida u otra opcion.")]
        public int ProvinciaId { get; set; }

        [Display(Name = "Localidad:")]
        [Required(ErrorMessage = "Seleccione una localidad.")]
        [Range(2, int.MaxValue, ErrorMessage = "Seleccione una localidad válida u otra opcion.")]
        public int LocalidadId { get; set; }

        [Display(Name = "Calle:")]
        [Required(ErrorMessage = "La calle es obligatoria.")]
        [Range(2, int.MaxValue, ErrorMessage = "Seleccione una calle válida u otra opcion.")]
        public int CalleId { get; set; }


        [Display(Name = "Numeración:")]
        [Required(ErrorMessage = "La numeración es obligatoria")]
        [Range(1, 99999, ErrorMessage = "Ingrese un número que contenga hasta 5 dígitos.")]
        public int? NumeracionCalle { get; set; }

        [Display(Name = "Piso:")]
        //[Required(ErrorMessage = "Debe ingresar un número de piso")]
        [StringLength(5, ErrorMessage = "Máximo 5 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Solo letras y números")]
        public string Piso { get; set; }

        [Display(Name = "Departamento:")]
        //[Required(ErrorMessage = "Debe ingresar un número de departamento")]
        [StringLength(5, ErrorMessage = "Máximo 5 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9]+$", ErrorMessage = "Solo letras y números")]
        public string Departamento { get; set; }

        [Display(Name = "Código Postal:")]
        [Required(ErrorMessage = "El código postal es obligatorio")]
        [Range(1000, 9999, ErrorMessage = "El código postal debe tener 4 dígitos.")]
        public int? CodigoPostal { get; set; }

        [Display(Name = "Comentarios del domicilio:")]
        [MaxLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        public string ComentariosDomicilio { get; set; }
    }

}