using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KernelWebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PaperController : ControllerBase
    {
        private IKernelRepository _repository;

        public PaperController(IKernelRepository r)
        {
            _repository = r;
        }

        [HttpGet]
        public IResult GetResult([FromQuery] bool state)
        {
            var result = _repository.PaperSwitch(state, out var message);
            if(result) return Results.Ok(result);
            return Results.Problem(message);
        }
    }
}
