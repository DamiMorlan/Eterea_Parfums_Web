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
    private static string MapImagesPath() => HostingEnvironment.MapPath("~/imagenes");

    private static string EnsureImagesPath()
    {
        var p = MapImagesPath();
        Directory.CreateDirectory(p);
        return p;
    }

    // POST /api/imagenes/upload
    [HttpPost, Route("upload")]
    public IHttpActionResult Upload()
    {
        var req = HttpContext.Current?.Request;
        if (req == null || req.Files.Count == 0)
            return BadRequest("Archivo requerido (multipart/form-data con key 'file').");

        var file = req.Files[0];

        // Validaciones
        var ext = (Path.GetExtension(file.FileName) ?? "").ToLowerInvariant();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
            return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");

        // Nombre de archivo deseado (si lo envían) o el original
        var desiredName = req.Form["fileName"];
        string safeBaseName;

        if (!string.IsNullOrWhiteSpace(desiredName))
        {
            // limpiar y asegurar extensión
            var dn = Path.GetFileNameWithoutExtension(desiredName);
            safeBaseName = SanitizeFileName(dn);
            // si vino sin extensión, usamos la del archivo
            if (Path.GetExtension(desiredName).Equals("", StringComparison.Ordinal))
                ext = ext; // ya está
            else
                ext = Path.GetExtension(desiredName).ToLowerInvariant();
        }
        else
        {
            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            safeBaseName = SanitizeFileName(originalName);
        }

        var imagesDir = EnsureImagesPath();

        // Evitar colisión: sufijos -1, -2…
        string finalName = GetNonCollidingName(imagesDir, safeBaseName, ext);
        string fullPath = Path.Combine(imagesDir, finalName);

        file.SaveAs(fullPath);

        // URL pública directa (IIS sirve estáticos)
        var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
        var publicUrl = $"{baseUrl}/imagenes/{finalName}";

        var info = new FileInfo(fullPath);
        return Ok(new
        {
            fileName = finalName,
            url = publicUrl,
            relativePath = $"/imagenes/{finalName}",
            size = info.Length
        });
    }

    // Helpers
    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '-');
        // opcional: bajar a minúsculas y recortar
        return (name ?? "img").Trim().ToLowerInvariant();
    }

    private static string GetNonCollidingName(string dir, string baseName, string ext)
    {
        string candidate = baseName + ext;
        int i = 1;
        while (File.Exists(Path.Combine(dir, candidate)))
        {
            candidate = $"{baseName}-{i}{ext}";
            i++;
        }
        return candidate;
    }

    // (Opcional) GET /api/imagenes/{name} — si preferís servir por API
    [HttpGet, Route("{name}")]
    public HttpResponseMessage Get(string name)
    {
        var fileName = Path.GetFileName(name);
        var fullPath = Path.Combine(MapImagesPath(), fileName);
        if (!File.Exists(fullPath))
            return Request.CreateResponse(HttpStatusCode.NotFound);

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var resp = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
        resp.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(MimeMapping.GetMimeMapping(fileName));
        resp.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromDays(1) };
        return resp;
    }

    // (Opcional) DELETE /api/imagenes/{name}
    [HttpDelete, Route("{name}")]
    public IHttpActionResult Delete(string name)
    {
        var fileName = Path.GetFileName(name);
        var fullPath = Path.Combine(MapImagesPath(), fileName);
        if (!File.Exists(fullPath)) return NotFound();
        File.Delete(fullPath);
        return StatusCode(HttpStatusCode.NoContent);
    }
}