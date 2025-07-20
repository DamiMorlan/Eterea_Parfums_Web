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
            bool tienePromo = false;
            string leyendaPromo = "";

            if (promo10 != null && promoPorCantidad == null)
            {
                // Solo promo del 10%
                descuentoAplicado = totalSinDescuento * 0.10;
                totalConDescuento = totalSinDescuento - descuentoAplicado;
                tienePromo = true;
                leyendaPromo = "Promoción 10% OFF";
            }
            else if (promo10 == null && promoPorCantidad != null)
            {
                if (cantidad >= 2)
                {
                    int pares = cantidad / 2;
                    int resto = cantidad % 2;

                    double descuentoPorcentajePar = promoPorCantidad.descuento; // Ej: 40 significa 80% OFF en 2da unidad
                    double descuentoPorPar = (2 * precioOriginal) * (descuentoPorcentajePar / 100.0);

                    descuentoAplicado = pares * descuentoPorPar;
                    totalConDescuento = totalSinDescuento - descuentoAplicado;

                    tienePromo = true;
                    leyendaPromo = $"Promoción {descuentoPorcentajePar}% OFF aplicados al total de cada par";
                }
                else
                {
                    totalConDescuento = totalSinDescuento;
                    descuentoAplicado = 0;
                    tienePromo = true;

                    if (stockDisponible >= 2)
                        leyendaPromo = $"Si llevás 2 iguales, la segunda unidad tiene {100 - promoPorCantidad.descuento}% de descuento";
                }
            }
            else if (promo10 != null && promoPorCantidad != null)
            {
                int pares = cantidad / 2;
                int resto = cantidad % 2;

                double descuentoPorcentajePar = promoPorCantidad.descuento;
                double descuentoPorPar = (2 * precioOriginal) * (descuentoPorcentajePar / 100.0);
                double descuentoResto = resto * precioOriginal * 0.10;

                descuentoAplicado = (pares * descuentoPorPar) + descuentoResto;
                totalConDescuento = totalSinDescuento - descuentoAplicado;

                tienePromo = true;
                leyendaPromo = $"Promoción combinada: {descuentoPorcentajePar}% OFF en cada par";

                if (resto == 1)
                {
                    leyendaPromo += " + 10% OFF en la unidad restante";
                }
            }
            else
            {
                // Sin promociones
                totalConDescuento = totalSinDescuento;
                descuentoAplicado = 0;
                tienePromo = false;
                leyendaPromo = "";
            }

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
                TienePromo = tienePromo,
                LeyendaPromo = leyendaPromo,
                StockDisponibleParaVentaWeb = stockDisponible,
                MostrarPrecioTachado = promo10 != null
                    && (promoPorCantidad == null || cantidad < 2),
                TotalSinDescuento = Math.Round(totalSinDescuento, 2),
                DescuentoAplicado = Math.Round(descuentoAplicado, 2)
            };
        }




    }
}