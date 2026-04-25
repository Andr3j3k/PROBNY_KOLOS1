using System.Data;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Data.SqlClient;
using Probny_Kolos1.DTOs;
using Probny_Kolos1.Exceptions;

namespace Probny_Kolos1.Services;

public class CustomerService(IConfiguration configuration) : ICustomerService
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    public async Task<CustomersDTO> GetCustomersAsync(int customerId)
    {
        const string sql = """
                           SELECT 
                           c.first_name, 
                           c.last_name,
                           r.rental_id,
                           r.rental_date,
                           r.return_date,
                           s.name,
                           m.title,
                           ri.price_at_rental
                           FROM Customer c 
                           INNER JOIN Rental r ON c.customer_id=r.customer_id
                           INNER JOIN Status s ON r.status_id=s.status_id
                           INNER JOIN Rental_Item ri ON r.rental_id=ri.rental_id
                           INNER JOIN Movie m ON ri.movie_id=m.movie_id
                           WHERE c.customer_id=@CustomerId
                           """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        
        command.Parameters.Add("@CustomerId",SqlDbType.Int).Value=customerId;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        
        if(!await reader.ReadAsync())
        {
            throw new CustomerNotFoundException("Nie znaleziono customera od id {customerId}");
        }

        var result = new CustomersDTO()
        {
            FirstName = reader.GetString(reader.GetOrdinal("first_name")),
            LastName = reader.GetString(reader.GetOrdinal("last_name")),
            Rentals = new List<RentalDTO>()
        };

        do
        {
            if(reader.IsDBNull(reader.GetOrdinal("rental_id")))
                continue;
            
            var rentalId=reader.GetInt32(reader.GetOrdinal("rental_id"));
            var rental=result.Rentals.FirstOrDefault(r=>r.Id == rentalId);

            if (rental == null)
            {
                rental = new RentalDTO()
                {
                    Id = rentalId,
                    RentalDate = reader.GetDateTime(reader.GetOrdinal("rental_date")),
                    ReturnDate = reader.IsDBNull(reader.GetOrdinal("return_date"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("return_date")),
                    Status = reader.GetString(reader.GetOrdinal("name")),
                    Movies = new List<MovieDTO>()
                };
                result.Rentals.Add(rental);
            }

            if (!reader.IsDBNull(reader.GetOrdinal("title")))
            {
                rental.Movies.Add(new MovieDTO
                {
                    Title = reader.GetString(reader.GetOrdinal("title")),
                    PriceAtRental = reader.GetDecimal(reader.GetOrdinal("price_at_rental")),
                });
            }
        }while(await reader.ReadAsync());
        return result;
    }
}
