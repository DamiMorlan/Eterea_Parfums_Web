using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Eterea_Parfums_Web.Models;

public class DetallePerfumeViewModel
{
    public int Id { get; set; }
    public string Imagen1 { get; set; }
    public string Nombre { get; set; }
    public string Marca { get; set; }
    public int Tamaño { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }

    // Texto que mostrás en la vista (por ej. "10% OFF", "2x1", "Sin promoción", etc.)
    public string Promocion { get; set; }

    //se setea desde el controlador con las promos aplicadas
    public decimal PrecioTotal { get; set; }
}

public class DetalleFacturaViewModel
{
    public decimal PrecioTotal { get; set; }
    public string NumeroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public List<DetallePerfumeViewModel> Perfumes { get; set; }

    public decimal RecargoTarjeta { get; set; }   // dbo.factura.recargo_tarjeta
    public string FormaDePago { get; set; }       // dbo.factura.forma_de_pago

    // NUEVO: cuotas (mapea a dbo.factura.factura_pdf)
    public string Cuotas { get; set; }
}