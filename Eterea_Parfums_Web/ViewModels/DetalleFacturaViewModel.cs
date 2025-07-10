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
    public string Promocion { get; set; }

    public decimal PrecioTotal => Cantidad * PrecioUnitario;
}

public class DetalleFacturaViewModel
{
    public decimal PrecioTotal { get; set; }
    public string NumeroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public List<DetallePerfumeViewModel> Perfumes { get; set; }
}