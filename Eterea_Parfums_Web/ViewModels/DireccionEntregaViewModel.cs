using System.ComponentModel.DataAnnotations;

namespace Eterea_Parfums_Web.ViewModels
{
    public class DireccionEntregaViewModel
    {
        [Required(ErrorMessage = "La calle es obligatoria")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÑáéíóúñ0-9\s]+$", ErrorMessage = "La calle solo puede contener letras y números")]
        public string Calle { get; set; }

        [Required(ErrorMessage = "La numeración es obligatoria")]
        [RegularExpression(@"^\d+[A-Za-z]?$|^s/n$", ErrorMessage = "La numeración debe ser un número o 's/n'")]
        public string Numeracion { get; set; }

        [RegularExpression(@"^[A-Za-z0-9]*$", ErrorMessage = "El piso solo puede contener letras o números")]
        public string Piso { get; set; }

        [RegularExpression(@"^[A-Za-z0-9]*$", ErrorMessage = "El departamento solo puede contener letras o números")]
        public string Departamento { get; set; }

        [Required(ErrorMessage = "El código postal es obligatorio")]
        [RegularExpression(@"^\d{4,10}$", ErrorMessage = "El código postal debe ser solo números (mínimo 4 dígitos)")]
        public string CodigoPostal { get; set; }

        [Required(ErrorMessage = "La localidad es obligatoria")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÑáéíóúñ0-9\s]+$", ErrorMessage = "La localidad solo puede contener letras y números")]
        public string Localidad { get; set; }

        [Required(ErrorMessage = "La provincia es obligatoria")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÑáéíóúñ\s]+$", ErrorMessage = "La provincia solo puede contener letras")]
        public string Provincia { get; set; }

        // ✅ Método para construir el texto completo del domicilio
        public string ConstruirTextoCompleto()
        {
            var linea1 = $"{Calle} {Numeracion}";
            if (!string.IsNullOrWhiteSpace(Piso))
                linea1 += $" Piso {Piso}";
            if (!string.IsNullOrWhiteSpace(Departamento))
                linea1 += $" Dpto. {Departamento}";

            var linea2 = $"C.P. {CodigoPostal}, {Localidad}, {Provincia}";

            return linea1 + "\n" + linea2;
        }
    }

}