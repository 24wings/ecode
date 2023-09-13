using Microsoft.AspNetCore.Mvc;

namespace ecode.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public string hello()
    {
        return "world";
    }
}
