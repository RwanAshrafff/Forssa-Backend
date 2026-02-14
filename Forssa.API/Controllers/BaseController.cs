using Microsoft.AspNetCore.Mvc;

namespace Forssa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult HandleResult<T>(T? result)
    {
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    protected IActionResult HandleResult<T>(T? result, string message)
    {
        if (result == null)
            return NotFound(new { message });

        return Ok(new { data = result, message });
    }

    protected IActionResult Created<T>(T result, string message = "Resource created successfully")
    {
        return StatusCode(201, new { data = result, message });
    }

    protected IActionResult BadRequestResult(string message)
    {
        return BadRequest(new { message });
    }

    protected IActionResult NotFoundResult(string message)
    {
        return NotFound(new { message });
    }

    protected IActionResult UnauthorizedResult(string message)
    {
        return Unauthorized(new { message });
    }

    protected IActionResult ForbiddenResult(string message)
    {
        return StatusCode(403, new { message });
    }

    protected IActionResult InternalServerError(string message)
    {
        return StatusCode(500, new { message });
    }
}
