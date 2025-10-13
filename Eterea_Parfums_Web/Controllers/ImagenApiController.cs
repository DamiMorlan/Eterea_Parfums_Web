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
    // ====== Helpers de path ======
    private static string MapUploadsPath()
        => HostingEnvironment.MapPath("~/Uploads"); // no depende de HttpContext

    private static string EnsureUploadsPath()
    {
        var p = MapUploadsPath();
        Directory.CreateDirectory(p);
        return p;
    }

    private static string SafeFileName(string name)
        => Path.GetFileName(name ?? string.Empty); // evita path traversal

    private static bool IsAllowedExt(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp";
    }

    private static string BuildPublicUrl(HttpRequestMessage req, string fileName)
    {
        var baseUrl = req.RequestUri.GetLeftPart(UriPartial.Authority);
        return $"{baseUrl}/Uploads/{fileName}";
    }

    private static string BuildApiUrl(HttpRequestMessage req, string fileName)
    {
        var baseUrl = req.RequestUri.GetLeftPart(UriPartial.Authority);
        return $"{baseUrl}/api/imagenes/{fileName}";
    }

    // ====== (Opcional) API Key si está configurada ======
    private bool IsAuthorized(HttpRequestMessage req)
    {
        var headerName = System.Configuration.ConfigurationManager.AppSettings["ApiKeyHeaderName"];
        var headerVal = System.Configuration.ConfigurationManager.AppSettings["ApiKeyValue"];

        // Si no está configurado, no forzar auth (modo compatibilidad con tu versión actual)
        if (string.IsNullOrWhiteSpace(headerName) || string.IsNullOrWhiteSpace(headerVal))
            return true;

        if (!req.Headers.TryGetValues(headerName, out var values))
            return false;

        foreach (var v in values)
            if (string.Equals(v, headerVal, StringComparison.Ordinal)) return true;

        return false;
    }

    // ===================================================
    // POST /api/imagenes/upload
    // multipart/form-data:
    //   file = (archivo)
    //   newName (opcional) = nombre final deseado (ej: "perfume-123.jpg")
    //   Si no mandás newName => usa GUID + ext
    // ===================================================
    [HttpPost, Route("upload")]
    public IHttpActionResult Upload()
    {
        if (!IsAuthorized(Request))
            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var req = HttpContext.Current?.Request;
        if (req == null || req.Files.Count == 0)
            return BadRequest("Archivo requerido (multipart/form-data con 'file').");

        var file = req.Files[0];

        // Nombre final deseado (opcional)
        var desiredName = SafeFileName(req.Form["newName"]);
        string finalName;

        if (!string.IsNullOrWhiteSpace(desiredName))
        {
            if (!IsAllowedExt(desiredName)) return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");
            finalName = desiredName;
        }
        else
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (!IsAllowedExt(ext)) return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");
            finalName = Guid.NewGuid().ToString("N") + ext;
        }

        var uploads = EnsureUploadsPath();
        var fullPath = Path.Combine(uploads, finalName);

        try
        {
            // Si ya existe con ese nombre, lo reemplazamos
            if (File.Exists(fullPath)) File.Delete(fullPath);

            file.SaveAs(fullPath);

            return Ok(new
            {
                fileName = finalName,
                url = BuildPublicUrl(Request, finalName),
                api = BuildApiUrl(Request, finalName)
            });
        }
        catch (Exception ex)
        {
            return InternalServerError(ex);
        }
    }

    // ===================================================
    // POST /api/imagenes/replace
    // multipart/form-data:
    //   file = (archivo nuevo)
    //   oldName (opcional) = nombre del archivo viejo a borrar
    //   newName (opcional) = nombre final deseado para el nuevo
    // Lógica: guarda el nuevo y si oldName != newName borra el viejo.
    // ===================================================
    [HttpPost, Route("replace")]
    public IHttpActionResult Replace()
    {
        if (!IsAuthorized(Request))
            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var req = HttpContext.Current?.Request;
        if (req == null || req.Files.Count == 0)
            return BadRequest("Archivo requerido (multipart/form-data con 'file').");

        var file = req.Files[0];
        var oldName = SafeFileName(req.Form["oldName"]);
        var newName = SafeFileName(req.Form["newName"]); // opcional

        // Determinar nombre final del nuevo archivo
        string finalName;
        if (!string.IsNullOrWhiteSpace(newName))
        {
            if (!IsAllowedExt(newName)) return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");
            finalName = newName;
        }
        else
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (!IsAllowedExt(ext)) return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");
            finalName = Guid.NewGuid().ToString("N") + ext;
        }

        var uploads = EnsureUploadsPath();
        var newFullPath = Path.Combine(uploads, finalName);

        try
        {
            // Guardar nuevo (sobrescribe si existiera)
            if (File.Exists(newFullPath)) File.Delete(newFullPath);
            file.SaveAs(newFullPath);

            // Borrar viejo si corresponde y no es el mismo nombre
            if (!string.IsNullOrWhiteSpace(oldName))
            {
                var oldFullPath = Path.Combine(uploads, oldName);
                if (!oldName.Equals(finalName, StringComparison.OrdinalIgnoreCase) && File.Exists(oldFullPath))
                {
                    File.Delete(oldFullPath);
                }
            }

            return Ok(new
            {
                fileName = finalName,
                url = BuildPublicUrl(Request, finalName),
                api = BuildApiUrl(Request, finalName)
            });
        }
        catch (Exception ex)
        {
            return InternalServerError(ex);
        }
    }

    // ===================================================
    // POST /api/imagenes/rename?oldName=...&newName=...
    // Cambia el nombre de un archivo existente (sin subir uno nuevo)
    // Si newName existe, lo sobreescribe.
    // ===================================================
    [HttpPost, Route("rename")]
    public IHttpActionResult Rename([FromUri] string oldName, [FromUri] string newName)
    {
        if (!IsAuthorized(Request))
            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        oldName = SafeFileName(oldName);
        newName = SafeFileName(newName);

        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
            return BadRequest("Parámetros 'oldName' y 'newName' requeridos.");

        if (!IsAllowedExt(oldName) || !IsAllowedExt(newName))
            return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");

        var uploads = EnsureUploadsPath();
        var oldFull = Path.Combine(uploads, oldName);
        var newFull = Path.Combine(uploads, newName);

        if (!File.Exists(oldFull))
            return NotFound();

        try
        {
            if (File.Exists(newFull)) File.Delete(newFull); // overwrite
            File.Move(oldFull, newFull);

            return Ok(new
            {
                oldName,
                newName,
                url = BuildPublicUrl(Request, newName),
                api = BuildApiUrl(Request, newName)
            });
        }
        catch (Exception ex)
        {
            return InternalServerError(ex);
        }
    }

    // ===================================================
    // GET /api/imagenes/{name}
    // Devuelve el archivo
    // ===================================================
    [HttpGet, Route("{name}")]
    public HttpResponseMessage Get(string name)
    {
        var fileName = SafeFileName(name);
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
        resp.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(1)
        };
        return resp;
    }

    // ===================================================
    // DELETE /api/imagenes/{name}
    // Borra el archivo si existe
    // ===================================================
    [HttpDelete, Route("{name}")]
    public IHttpActionResult Delete(string name)
    {
        if (!IsAuthorized(Request))
            return ResponseMessage(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var fileName = SafeFileName(name);
        var fullPath = Path.Combine(MapUploadsPath(), fileName);

        if (!File.Exists(fullPath)) return NotFound();

        try
        {
            File.Delete(fullPath);
            return StatusCode(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            return InternalServerError(ex);
        }
    }
}
