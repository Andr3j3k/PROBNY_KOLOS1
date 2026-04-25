using Probny_Kolos1.DTOs;

namespace Probny_Kolos1.Services;

public interface ICustomerService
{
    Task<CustomersDTO> GetCustomersAsync(int customerId);
    Task<int> CreateRentalAsync(int customerId, CreateRentalDTO rental);
}