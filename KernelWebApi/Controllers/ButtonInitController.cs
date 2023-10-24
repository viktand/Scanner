using Microsoft.AspNetCore.Mvc;

namespace KernelWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ButtonInitController : ControllerBase
    {
        private IKernelRepository _repositoty;

        public ButtonInitController(IKernelRepository r) 
        {
            _repositoty = r;
        }

        [HttpGet]
        public IResult Index()
        {
            var result = _repositoty.ButtonIni();
            return Results.Ok(result);
        }
    }
}
