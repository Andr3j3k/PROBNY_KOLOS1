namespace Probny_Kolos1.DTOs;

public class RentalDTO
{
    public int Id { get; set; }
    public DateTime RentalDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; }=String.Empty;
    public List<MovieDTO> Movies { get; set; } = [];
}