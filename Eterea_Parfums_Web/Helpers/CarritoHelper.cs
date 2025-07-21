using System;
using System.Linq;
using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;

namespace Eterea_Parfums_Web.Helpers
{
    public static class CarritoHelper
    {
        /// <summary>
        /// Construye el ItemCarritoViewModel aplicando todas las reglas de promos.
        /// </summary>
        public static ItemCarritoViewModel BuildItemViewModel(carrito item, int stockDisponible)
        {
            var perfume = item.perfume;
            int cantidad = item.cantidad;

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

            double precioOriginal = perfume.precio_en_pesos;
            double totalSinDescuento = precioOriginal * cantidad;
            double totalConDescuento = totalSinDescuento;
            double descuentoAplicado = 0;

            // Cálculo de descuentos
            if (promo10 != null && promoPorCantidad == null)
            {
                descuentoAplicado = totalSinDescuento * 0.10;
                totalConDescuento = totalSinDescuento - descuentoAplicado;
            }
            else if (promo10 == null && promoPorCantidad != null)
            {
                int pares = cantidad / 2;
                int resto = cantidad % 2;

                double descuentoPorPar = (2 * precioOriginal) * (promoPorCantidad.descuento / 100.0);

                descuentoAplicado = pares * descuentoPorPar;
                totalConDescuento = totalSinDescuento - descuentoAplicado;
            }
            else if (promo10 != null && promoPorCantidad != null)
            {
                int pares = cantidad / 2;
                int resto = cantidad % 2;

                double descuentoPorPar = (2 * precioOriginal) * (promoPorCantidad.descuento / 100.0);
                double descuentoResto = resto * precioOriginal * 0.10;

                descuentoAplicado = (pares * descuentoPorPar) + descuentoResto;
                totalConDescuento = totalSinDescuento - descuentoAplicado;
            }

            // Usamos el helper unificado para la leyenda
            string leyendaPromo = ObtenerLeyendaPromoSegunCantidad(item, stockDisponible);

            return new ItemCarritoViewModel
            {
                PerfumeId = perfume.id,
                Nombre = perfume.nombre,
                TipoDePerfume = perfume.tipo_de_perfume?.tipo_de_perfume1 ?? "",
                Presentacion = perfume.presentacion_ml,
                Genero = perfume.genero?.genero1 ?? "",
                Imagen = perfume.imagen1,
                PrecioOriginal = precioOriginal,
                PrecioConDescuento = Math.Round(
                    cantidad > 0 ? totalConDescuento / cantidad : precioOriginal, 2),
                Cantidad = cantidad,
                Total = Math.Round(totalConDescuento, 2),
                TienePromo = (promo10 != null || promoPorCantidad != null),
                LeyendaPromo = leyendaPromo,
                StockDisponibleParaVentaWeb = stockDisponible,
                MostrarPrecioTachado = promo10 != null && (promoPorCantidad == null || cantidad < 2),
                TotalSinDescuento = Math.Round(totalSinDescuento, 2),
                DescuentoAplicado = Math.Round(descuentoAplicado, 2)
            };
        }

        public static string ObtenerLeyendaPromoSegunCantidad(carrito item, int stockDisponible)
        {
            var perfume = item.perfume;
            int cantidad = item.cantidad;

            // ✅ Nueva validación al principio
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