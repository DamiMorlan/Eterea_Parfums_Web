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

            // --- PROMOCIONES DISPONIBLES ----------------------------------
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
            double precioConDescuento = precioOriginal;
            double total = precioOriginal * cantidad;
            bool tienePromo = false;
            string leyendaPromo = "";

            // --- LÓGICA DE DESCUENTOS --------------------------------------

            if (promo10 != null && promoPorCantidad == null)
            {
                // Solo promo del 10%
                precioConDescuento = precioOriginal * 0.90;
                total = Math.Round(precioConDescuento * cantidad, 2);
                tienePromo = true;
                leyendaPromo = "Promoción 10% OFF";
            }
            else if (promo10 == null && promoPorCantidad != null)
            {
                // Solo promo por cantidad
                if (cantidad >= 2)
                {
                    int pares = cantidad / 2;
                    int resto = cantidad % 2;
                    double descuentoPorcentaje = promoPorCantidad.descuento;

                    double precioPrimero = precioOriginal;
                    double precioSegundo = descuentoPorcentaje == 50 ? 0 : precioOriginal * (1 - descuentoPorcentaje / 100.0);

                    total = Math.Round((pares * (precioPrimero + precioSegundo)) + (resto * precioOriginal), 2);
                    precioConDescuento = total / cantidad;

                    tienePromo = true;
                    leyendaPromo = descuentoPorcentaje == 50
                        ? "Promoción 2 x 1"
                        : $"Promoción {descuentoPorcentaje}% de descuento en la segunda unidad";
                }
                else
                {
                    total = precioOriginal * cantidad;
                    precioConDescuento = precioOriginal;

                    tienePromo = true;
                    leyendaPromo = $"Si llevás 2 iguales, el segundo tiene {promoPorCantidad.descuento}% de descuento";
                }
            }

            if (promo10 == null && promoPorCantidad != null)
            {
                if (cantidad >= 2)
                {
                    int pares = cantidad / 2;
                    int resto = cantidad % 2;
                    double descuentoPorcentaje = promoPorCantidad.descuento;

                    double precioPrimero = precioOriginal;
                    double precioSegundo = descuentoPorcentaje == 50 ? 0 : precioOriginal * (1 - descuentoPorcentaje / 100.0);

                    total = Math.Round((pares * (precioPrimero + precioSegundo)) + (resto * precioOriginal), 2);
                    precioConDescuento = total / cantidad;

                    tienePromo = true;
                    leyendaPromo = descuentoPorcentaje == 50
                        ? "Promoción 2 x 1"
                        : $"Promoción {descuentoPorcentaje}% de descuento en la segunda unidad";
                }
                else
                {
                    total = precioOriginal * cantidad;
                    precioConDescuento = precioOriginal;

                    tienePromo = true;
                    leyendaPromo = $"Si llevás 2 iguales, el segundo tiene {promoPorCantidad.descuento}% de descuento";
                }
            }

            else
            {
                // Sin promoción
                precioConDescuento = precioOriginal;
                total = precioOriginal * cantidad;
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
                PrecioConDescuento = Math.Round(precioConDescuento, 2),
                Cantidad = cantidad,
                Total = total,
                TienePromo = tienePromo,
                LeyendaPromo = leyendaPromo,
                StockDisponibleParaVentaWeb = stockDisponible,
                MostrarPrecioTachado = promo10 != null
                    && (promoPorCantidad == null || cantidad < 2)
                    && precioConDescuento < precioOriginal
            };
        }

    }
}
