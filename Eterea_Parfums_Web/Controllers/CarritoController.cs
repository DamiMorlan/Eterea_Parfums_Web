using Eterea_Parfums_Web.Models;
using Eterea_Parfums_Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Eterea_Parfums_Web.Controllers
{
    public class CarritoController : Controller
    {
        // GET: Carrito
        public ActionResult Index()
        {
            int clienteId = 2; // Simulación de cliente logueado

            using (var db = new etereaEntities1())
            {
                // Obtener perfumes en el carrito con datos relacionados
                var perfumesEnCarrito = db.carrito
                    .Where(c => c.cliente_id == clienteId)
                    .Select(c => new
                    {
                        c.cantidad,
                        Perfume = c.perfume
                    })
                    .ToList();

                var viewModel = perfumesEnCarrito.Select(p =>
                {
                    var perfume = p.Perfume;

                    // Buscar si el perfume tiene alguna promoción activa (usando navegación)
                    var promo = perfume.promocion.FirstOrDefault(pr =>
                        pr.activo &&
                        pr.fecha_inicio <= DateTime.Now &&
                        pr.fecha_fin >= DateTime.Now);

                    double precioOriginal = perfume.precio_en_pesos;
                    double precioFinal = precioOriginal;
                    bool tienePromo = false;

                    if (promo != null)
                    {
                        precioFinal -= precioOriginal * promo.descuento / 100.0;
                        tienePromo = true;
                    }

                    return new ItemCarritoViewModel
                    {
                        PerfumeId = perfume.id,
                        Nombre = perfume.nombre,
                        TipoDePerfume = perfume.tipo_de_perfume.tipo_de_perfume1,
                        Presentacion = perfume.presentacion_ml,
                        Genero = perfume.genero.genero1,
                        Imagen = perfume.imagen1,
                        PrecioOriginal = precioOriginal,
                        PrecioConDescuento = precioFinal,
                        Cantidad = p.cantidad,
                        Total = precioFinal * p.cantidad,
                        TienePromo = tienePromo
                    };
                }).ToList();

                return View(viewModel);
            }
        }


        // GET: Carrito/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Carrito/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Carrito/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Carrito/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Carrito/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Carrito/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Carrito/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
