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
        public static ItemCarritoViewModel BuildItemViewModel(
            carrito item,
            int stockDisponible)
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
            var promoPorCantidad = promociones.FirstOrDefault(pr => pr.descuento > 10);

            double precioOriginal = perfume.precio_en_pesos;
            double precioConDescuento = precioOriginal;
            double total = 0;
            bool tienePromo = false;
            string leyendaPromo = "";

            /* --------------- (copia aquí TODO el cuerpo que ya tenías
                               para calcular los diferentes casos) -----------
                               … 
                               … 
                               …                                            */

            return new ItemCarritoViewModel
            {
                PerfumeId = perfume.id,
                Nombre = perfume.nombre,
                TipoDePerfume = perfume.tipo_de_perfume.tipo_de_perfume1,
                Presentacion = perfume.presentacion_ml,
                Genero = perfume.genero.genero1,
                Imagen = perfume.imagen1,
                PrecioOriginal = precioOriginal,
                PrecioConDescuento = precioConDescuento,
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
