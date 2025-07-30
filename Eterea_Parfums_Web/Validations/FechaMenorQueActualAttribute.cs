using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Validations
{
    public class FechaMenorQueActualAttribute : ValidationAttribute, IClientValidatable
    {
        public int EdadMinima { get; set; } = 0;       // Mínimo 0 años por defecto
        public int EdadMaxima { get; set; } = 120;     // Máximo 120 años por defecto

        public override bool IsValid(object value)
        {
            if (value == null)
                return true; //Lo maneja [Required]

            DateTime fechaNacimiento;
            if (DateTime.TryParse(value.ToString(), out fechaNacimiento))
            {
                var hoy = DateTime.Today;
                if (fechaNacimiento >= hoy)
                    return false;

                int edad = hoy.Year - fechaNacimiento.Year;
                if (fechaNacimiento > hoy.AddYears(-edad)) edad--;  //Ajustar si aún no cumplió este año

                return edad >= EdadMinima && edad <= EdadMaxima;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"La fecha debe ser menor a hoy y debes tener {EdadMinima} años como minimo";
        }

        public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context)
        {
            var rule = new ModelClientValidationRule
            {
                ErrorMessage = FormatErrorMessage(metadata.GetDisplayName()),
                ValidationType = "fechavalida" // debe estar en minúscula
            };

            rule.ValidationParameters["edadminima"] = EdadMinima;
            rule.ValidationParameters["edadmaxima"] = EdadMaxima;

            yield return rule;
        }
    }
}