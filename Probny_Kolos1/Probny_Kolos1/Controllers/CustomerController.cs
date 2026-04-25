using Microsoft.AspNetCore.Mvc;
using Probny_Kolos1.DTOs;
using Probny_Kolos1.Exceptions;
using Probny_Kolos1.Services;

namespace Probny_Kolos1.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomerController(ICustomerService service) : ControllerBase
{
    [HttpGet("{id:int}/rentals")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        try
        {
            return Ok(await service.GetCustomersAsync(id));
        }catch(CustomerNotFoundException e)
        {
            return NotFound(new ErrorResponseDTO()
            {
                Message = e.Message
            });
        }
    }
}