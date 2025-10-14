using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;

[RoutePrefix("api/imagenes")]
public class ImagenApiController : ApiController
{
    // ==== Carpetas configurables (con defaults seguros) ====
    private static string ImagesFolderVirtual =>
        ConfigurationManager.AppSettings["ImagesFolderVirtual"] ?? "~/imagenes"; // carpeta física (virtual) donde guardar

    private static string ImagesFolderPublic =>
        (ConfigurationManager.AppSettings["ImagesFolderPublic"] ?? "/imagenes").TrimEnd('/'); // segmento público para armar URLs

    // ==== Helpers de path ====
    private static string MapImagesPath() => HostingEnvironment.MapPath(ImagesFolderVirtual);

    private static string EnsureImagesPath()
    {
        var p = MapImagesPath();
        Directory.CreateDirectory(p);
        return p;
    }

    private static string SafeFileName(string name) => Path.GetFileName(name ?? string.Empty); // evita traversal

    private static bool IsAllowedExt(string fileNameOrExt)
    {
        var ext = Path.GetExtension(fileNameOrExt)?.ToLowerInvariant();
        return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".webp";
    }

    private static string BuildPublicUrl(HttpRequestMessage req, string fileName)
    {
        var baseUrl = req.RequestUri.GetLeftPart(UriPartial.Authority);
        return $"{baseUrl}{ImagesFolderPublic}/{fileName}";
    }

    private static string BuildApiUrl(HttpRequestMessage req, string fileName)
    {
        var baseUrl = req.RequestUri.GetLeftPart(UriPartial.Authority);
        return $"{baseUrl}/api/imagenes/{fileName}";
    }

    // ==== API Key opcional (si está en web.config) ====
    private bool IsAuthorized(HttpRequestMessage req)
    {
        var headerName = ConfigurationManager.AppSettings["ApiKeyHeaderName"];
        var headerVal = ConfigurationManager.AppSettings["ApiKeyValue"];

        if (string.IsNullOrWhiteSpace(headerName) || string.IsNullOrWhiteSpace(headerVal))
            return true; // modo compatibilidad: sin key configurada, no exige auth

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
    //   newName (opcional) o fileName (opcional) = "nombre-deseado.jpg"
    //   Si no mandás nombre, usa GUID + ext del archivo
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

        // Aceptamos newName o fileName (compatibilidad con cliente)
        var desiredFromForm = req.Form["newName"];
        if (string.IsNullOrWhiteSpace(desiredFromForm))
            desiredFromForm = req.Form["fileName"];

        var desiredName = SafeFileName(desiredFromForm);
        string finalName;

        if (!string.IsNullOrWhiteSpace(desiredName))
        {
            if (!IsAllowedExt(desiredName))
                return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");

            finalName = desiredName;
        }
        else
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (!IsAllowedExt(ext))
                return BadRequest("Extensión no permitida (.jpg/.jpeg/.png/.webp).");

            finalName = Guid.NewGuid().ToString("N") + ext;
        }

        var images = EnsureImagesPath();
        var fullPath = Path.Combine(images, finalName);

        try
        {
            if (File.Exists(fullPath)) File.Delete(fullPath); // sobreescribe si existía
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

        var images = EnsureImagesPath();
        var newFullPath = Path.Combine(images, finalName);

        try
        {
            if (File.Exists(newFullPath)) File.Delete(newFullPath);
            file.SaveAs(newFullPath);

            if (!string.IsNullOrWhiteSpace(oldName))
            {
                var oldFullPath = Path.Combine(images, oldName);
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

        var images = EnsureImagesPath();
        var oldFull = Path.Combine(images, oldName);
        var newFull = Path.Combine(images, newName);

        if (!File.Exists(oldFull))
            return NotFound();

        try
        {
            if (File.Exists(newFull)) File.Delete(newFull); // overwrite si ya existía
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
        var fullPath = Path.Combine(MapImagesPath(), fileName);

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
        var fullPath = Path.Combine(MapImagesPath(), fileName);

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
