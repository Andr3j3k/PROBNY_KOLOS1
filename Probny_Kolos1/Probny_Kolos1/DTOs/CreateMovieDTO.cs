using System.ComponentModel.DataAnnotations;

namespace Probny_Kolos1.DTOs;

public class CreateMovieDTO
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public decimal RentalPrice { get; set; }
}