using Microsoft.AspNetCore.Mvc;

namespace KernelWebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PowerController : ControllerBase
    {
        private IKernelRepository _repository;

        public PowerController(IKernelRepository r)
        {
            _repository = r;
        }

        [HttpGet]
        public IResult GetResult()
        {
            var result = _repository.PowerSwitch();
            return Results.Ok(result);
        }
    }
}
