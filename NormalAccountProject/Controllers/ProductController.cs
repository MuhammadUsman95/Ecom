using Microsoft.AspNetCore.Mvc;

namespace NormalAccountProject.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
