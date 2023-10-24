using Microsoft.AspNetCore.Mvc;

namespace KernelWebApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CheckButtonController : ControllerBase
    {
        private IKernelRepository _repository;

        public CheckButtonController(IKernelRepository r) 
        {
            _repository = r;
        }

        [HttpGet]
        public IResult Get()
        {          
            var result = _repository.ButtonCheck();
            return Results.Ok(result);
        }

        [HttpPost]
        public IResult Post()
        {
            _repository.Press = true;
            return Results.Ok(true);
        }
    }
}
