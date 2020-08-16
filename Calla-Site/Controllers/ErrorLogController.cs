using Calla.Data;
using Calla.Models;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System.Linq;
using System.Threading.Tasks;

namespace Calla.Controllers
{
    public class ErrorLogController : Controller
    {
        private readonly ILogger<ErrorLogController> logger;
        private readonly IWebHostEnvironment env;
        private readonly CallaContext db;

        public ErrorLogController(ILogger<ErrorLogController> logger, IWebHostEnvironment env, CallaContext db)
        {
            this.logger = logger;
            this.env = env;
            this.db = db;
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (!env.IsDevelopment())
            {
                return StatusCode(StatusCodes.Status401Unauthorized);
            }

            if (Request.ContentType == "application/json")
            {
                return Json(db.Errors
                    .OrderBy(err => err.Id)
                    .ToArray());
            }
            else
            {
                return View(db.Errors.ToArray());
            }
        }

        [HttpPost]
        public async Task<ActionResult> Index([FromBody] TraceKitErrorModel err)
        {
            if (err != null)
            {
                logger.LogError(err.message, err);
                _ = db.Add(new Errors
                {
                    From = Request.HttpContext.Connection.RemoteIpAddress.ToString(),
                    On = Request.Headers["Referer"].FirstOrDefault() ?? "N/A",
                    Message = err.message,
                    ErrorObject = JsonConvert.SerializeObject(err)
                });
                await db.SaveChangesAsync().ConfigureAwait(false);
            }

            return Ok();
        }

        [HttpDelete]
        public async Task<ActionResult> Index([FromBody] int id)
        {
            if (!env.IsDevelopment())
            {
                return StatusCode(StatusCodes.Status401Unauthorized);
            }

            var error = db.Errors
                .Where(err => err.Id == id)
                .FirstOrDefault();
            if (error is null)
            {
                return NotFound();
            }
            else
            {
                db.Errors.Remove(error);
                await db.SaveChangesAsync().ConfigureAwait(false);
                return Ok();
            }
        }
    }
}
