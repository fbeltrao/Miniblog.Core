using Microsoft.AspNetCore.Mvc;
using WebEssentials.AspNetCore.Pwa;

namespace Miniblog.Core.Controllers
{
    public class SharedController : Controller
    {
        private readonly WebManifest _webManifest;

        public SharedController(WebManifest webManifest)
        {
            this._webManifest = webManifest;
        }

        public IActionResult Error()
        {
            return View(Response.StatusCode);
        }

        /// <summary>
        ///  This is for use in wwwroot/serviceworker.js to support offline scenarios
        /// </summary>
        public IActionResult Offline()
        {
            return View();
        }

        [Route("/manifest.json")]
        public IActionResult WebManifest()
        {
            return Json(this._webManifest);
        }
        
    }
}
