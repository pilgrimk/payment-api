using Microsoft.AspNetCore.Mvc;

namespace PaymentApi.Controllers
{
    public class TestController : Controller
    {
        [HttpPost("testing")]
        public String Test()
        {
            return "SUCCESS";
        }
    }
}
