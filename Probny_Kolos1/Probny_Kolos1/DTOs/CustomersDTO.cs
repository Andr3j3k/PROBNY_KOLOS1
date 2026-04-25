namespace Probny_Kolos1.DTOs;

public class CustomersDTO
{
    public string FirstName { get; set; }=string.Empty;
    public string LastName { get; set; }=string.Empty;
    public List<RentalDTO> Rentals { get; set; } = [];
}