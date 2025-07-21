// 1. ✅ Método centralizado para construir formato
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;

public static class DireccionHelper
{
    public static string ConstruirTextoCompleto(string calle, int numeracion, string piso, string departamento, int? codigoPostal, string localidad, string provincia)
    {
        string linea1 = $"{calle?.Trim()} {numeracion}".Trim();

        if (!string.IsNullOrWhiteSpace(piso))
            linea1 += $" Piso {piso}";
        if (!string.IsNullOrWhiteSpace(departamento))
            linea1 += $" Dpto. {departamento}";

        string linea2 = $"C.P. {(codigoPostal?.ToString() ?? "")}, {localidad}, {provincia}";

        return linea1 + "\n" + linea2;
    }

    public static string ConstruirTextoCompleto(DireccionEntregaViewModel model)
    {
        string linea1 = $"{model.Calle} {model.Numeracion}".Trim();
        if (!string.IsNullOrWhiteSpace(model.Piso))
            linea1 += $" Piso {model.Piso}";
        if (!string.IsNullOrWhiteSpace(model.Departamento))
            linea1 += $" Dpto. {model.Departamento}";

        string linea2 = $"C.P. {model.CodigoPostal}, {model.Localidad}, {model.Provincia}";
        return linea1 + "\n" + linea2;
    }
}
