using System.ComponentModel.DataAnnotations;

namespace Probny_Kolos1.DTOs;

public class CreateRentalDTO
{
    [Required]
    public DateTime RentalDate { get; set; }
    
    [Required]
    public List<CreateMovieDTO> Movies { get; set; } = [];
}