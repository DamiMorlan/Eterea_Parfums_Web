using System;
using System.Linq;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;

namespace Eterea_Parfums_Web.Helpers
{
    public static class CarritoHelper
    {
        // ==========================
        // Helpers de precisión
        // ==========================
        private static decimal ToDec(double v) => (decimal)v;

        // Trunca a 2 decimales SIN redondear
        private static decimal Trunc2(decimal v) => decimal.Truncate(v * 100m) / 100m;

        // Versión para exponer como double (tu VM usa double)
        private static double Trunc2D(decimal v) => (double)Trunc2(v);

        /// <summary>
        /// Construye el ItemCarritoViewModel aplicando todas las reglas de promos,
        /// usando decimal para el cálculo y truncando al exponer.
        /// </summary>
        public static ItemCarritoViewModel BuildItemViewModel(carrito item, int stockDisponible)
        {
            var perfume = item.perfume;
            int cantidad = item.cantidad;

            // Traer promos activas y vigentes (excepto id 1)
            var promociones = perfume.promocion
                .Where(pr => pr.id != 1 &&
                             pr.activo &&
                             pr.fecha_inicio <= DateTime.Now &&
                             pr.fecha_fin >= DateTime.Now)
                .ToList();

            var promo10 = promociones.FirstOrDefault(pr => pr.descuento == 10);
            var promoPorCantidad = promociones
                .Where(pr => pr.descuento > 10)
                .OrderByDescending(pr => pr.descuento)
                .FirstOrDefault();

            // Precio unitario como decimal para cálculo exacto
            decimal p = ToDec(perfume.precio_en_pesos);

            // Subtotal crudo
            decimal subtotalRaw = p * cantidad;

            // Descuento
            decimal descuentoRaw = 0m;

            if (promo10 != null && promoPorCantidad == null)
            {
                // Solo 10%
                descuentoRaw = subtotalRaw * 0.10m;
            }
            else if (promo10 == null && promoPorCantidad != null)
            {
                // Solo promo por cantidad (por pares). En DB: 35 => 35% de (2*p)
                int pares = cantidad / 2;
                decimal descuentoPorPar = (2m * p) * (ToDec(promoPorCantidad.descuento) / 100m);
                descuentoRaw = pares * descuentoPorPar;
            }
            else if (promo10 != null && promoPorCantidad != null)
            {
                // Ambas promos: pares con promo de cantidad + 10% al remanente (si queda 1)
                int pares = cantidad / 2;
                int resto = cantidad % 2;

                decimal descuentoPorPar = (2m * p) * (ToDec(promoPorCantidad.descuento) / 100m);
                decimal descuentoResto = (resto == 1) ? (0.10m * p) : 0m;

                descuentoRaw = (pares * descuentoPorPar) + descuentoResto;
            }
            // Si no hay promos, descuentoRaw = 0

            // Total con descuento (sin redondear)
            decimal totalRaw = subtotalRaw - descuentoRaw;

            // Precio por unidad "promedio" si hay cantidad > 0
            decimal precioConDescUnitarioRaw = (cantidad > 0) ? (totalRaw / cantidad) : p;

            // Leyenda (dejo tu lógica original)
            string leyendaPromo = ObtenerLeyendaPromoSegunCantidad(item, stockDisponible);

            // Mostrar precio tachado:
            // - True cuando hay 10% y NO hay promo por cantidad aplicada (o cantidad < 2)
            bool mostrarPrecioTachado =
                (promo10 != null) &&
                (promoPorCantidad == null || cantidad < 2);

            // Construir VM (todo truncado a 2 decimales, sin redondeo)
            return new ItemCarritoViewModel
            {
                PerfumeId = perfume.id,
                Nombre = perfume.nombre,
                TipoDePerfume = perfume.tipo_de_perfume?.tipo_de_perfume1 ?? "",
                Presentacion = perfume.presentacion_ml,
                Genero = perfume.genero?.genero1 ?? "",
                Imagen = perfume.imagen1,

                // Dejo el precio original como viene (double). Si querés, podés truncarlo así:
                // PrecioOriginal = Trunc2D(p),
                PrecioOriginal = perfume.precio_en_pesos,

                PrecioConDescuento = Trunc2D(precioConDescUnitarioRaw),
                Cantidad = cantidad,

                Total = Trunc2D(totalRaw),
                TienePromo = (promo10 != null || promoPorCantidad != null),
                LeyendaPromo = leyendaPromo,
                StockDisponibleParaVentaWeb = stockDisponible,
                MostrarPrecioTachado = mostrarPrecioTachado,

                TotalSinDescuento = Trunc2D(subtotalRaw),
                DescuentoAplicado = Trunc2D(descuentoRaw)
            };
        }

        public static string ObtenerLeyendaPromoSegunCantidad(carrito item, int stockDisponible)
        {
            var perfume = item.perfume;
            int cantidad = item.cantidad;

            if (stockDisponible < 2)
                return "";

            var promociones = perfume.promocion
                .Where(pr => pr.id != 1 &&
                             pr.activo &&
                             pr.fecha_inicio <= DateTime.Now &&
                             pr.fecha_fin >= DateTime.Now)
                .ToList();

            var promo10 = promociones.FirstOrDefault(pr => pr.descuento == 10);
            var promoPorCantidad = promociones
                .Where(pr => pr.descuento > 10)
                .OrderByDescending(pr => pr.descuento)
                .FirstOrDefault();

            if (promo10 != null && promoPorCantidad != null)
            {
                int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;

                if (cantidad == 1)
                {
                    return $"Promoción 10% OFF<br /><strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
                }
                else
                {
                    return (descuentoSegundaUnidad == 100)
                        ? "Promoción 2 x 1"
                        : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad";
                }
            }

            if (promo10 != null)
            {
                return "Promoción 10% OFF";
            }

            if (promoPorCantidad != null)
            {
                int descuentoSegundaUnidad = promoPorCantidad.descuento * 2;

                if (cantidad == 1)
                {
                    return $"<strong>Si llevás 2 iguales, el segundo tiene {descuentoSegundaUnidad}% de descuento</strong>";
                }
                else if (cantidad % 2 == 1 && cantidad > 1 && stockDisponible >= cantidad + 1)
                {
                    return $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad<br /><strong>¡Si agregás uno más lo llevás con el {descuentoSegundaUnidad}% de descuento!</strong>";
                }
                else
                {
                    return (descuentoSegundaUnidad == 100)
                        ? "Promoción 2 x 1"
                        : $"Promoción {descuentoSegundaUnidad}% de descuento en la segunda unidad";
                }
            }

            return "";
        }
    }
}
