using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

[RoutePrefix("api/imagenes")]
public class ImagesController : ApiController
{
    private string UploadsPath => HttpContext.Current.Server.MapPath("~/Uploads");

    public ImagesController()
    {
        Directory.CreateDirectory(UploadsPath);
    }

    // POST /api/imagenes/upload
    [HttpPost, Route("upload")]
    public IHttpActionResult Upload()
    {
        var req = HttpContext.Current.Request;

        if (req.Files.Count == 0)
            return BadRequest("Archivo requerido (multipart/form-data con 'file').");

        var file = req.Files[0];

        // Validaciones básicas
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
            return BadRequest("Extensión no permitida.");

        // Renombrar para evitar colisiones
        var safeName = Guid.NewGuid().ToString("N") + ext;
        var fullPath = Path.Combine(UploadsPath, safeName);

        file.SaveAs(fullPath);

        // URL pública directa (estático) o vía API:
        var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
        var publicUrl = $"{baseUrl}/Uploads/{safeName}";

        return Ok(new { fileName = safeName, url = publicUrl });
    }

    // GET /api/imagenes/{nombre}
    [HttpGet, Route("{name}")]
    public HttpResponseMessage Get(string name)
    {
        var fullPath = Path.Combine(UploadsPath, name);
        if (!File.Exists(fullPath))
            return Request.CreateResponse(HttpStatusCode.NotFound);

        var result = new HttpResponseMessage(HttpStatusCode.OK);
        result.Content = new StreamContent(File.OpenRead(fullPath));
        result.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(MimeMapping.GetMimeMapping(name));
        return result;
    }

    // DELETE /api/imagenes/{nombre}
    [HttpDelete, Route("{name}")]
    public IHttpActionResult Delete(string name)
    {
        var fullPath = Path.Combine(UploadsPath, name);
        if (!File.Exists(fullPath)) return NotFound();

        File.Delete(fullPath);
        return StatusCode(HttpStatusCode.NoContent);
    }
}
