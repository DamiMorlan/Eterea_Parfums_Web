using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;

[RoutePrefix("api/imagenes")]
public class ImagesController : ApiController
{
    private static string MapUploadsPath()
        => HostingEnvironment.MapPath("~/Uploads"); // no depende de HttpContext

    private static string EnsureUploadsPath()
    {
        var p = MapUploadsPath();
        Directory.CreateDirectory(p);
        return p;
    }

    // POST /api/imagenes/upload
    [HttpPost, Route("upload")]
    public IHttpActionResult Upload()
    {
        var req = HttpContext.Current?.Request;
        if (req == null || req.Files.Count == 0)
            return BadRequest("Archivo requerido (multipart/form-data con 'file').");

        var file = req.Files[0];

        // Validaciones básicas
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
            return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");

        // Renombrar para evitar colisiones
        var safeName = Guid.NewGuid().ToString("N") + ext;
        var uploads = EnsureUploadsPath();
        var fullPath = Path.Combine(uploads, safeName);

        file.SaveAs(fullPath);

        // URL pública directa (estático) o vía API:
        var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
        var publicUrl = $"{baseUrl}/Uploads/{safeName}";
        var apiUrl = $"{baseUrl}/api/imagenes/{safeName}";

        return Ok(new { fileName = safeName, url = publicUrl, api = apiUrl });
    }

    // GET /api/imagenes/{nombre}
    [HttpGet, Route("{name}")]
    public HttpResponseMessage Get(string name)
    {
        var fileName = Path.GetFileName(name); // evita traversal
        var fullPath = Path.Combine(MapUploadsPath(), fileName);

        if (!File.Exists(fullPath))
            return Request.CreateResponse(HttpStatusCode.NotFound);

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var resp = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream)
        };
        resp.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(MimeMapping.GetMimeMapping(fileName));
        // Cache básico (1 día)
        resp.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(1)
        };
        return resp;
    }

    // DELETE /api/imagenes/{nombre}
    [HttpDelete, Route("{name}")]
    public IHttpActionResult Delete(string name)
    {
        var fileName = Path.GetFileName(name);
        var fullPath = Path.Combine(MapUploadsPath(), fileName);

        if (!File.Exists(fullPath)) return NotFound();

        File.Delete(fullPath);
        return StatusCode(HttpStatusCode.NoContent);
    }
}
