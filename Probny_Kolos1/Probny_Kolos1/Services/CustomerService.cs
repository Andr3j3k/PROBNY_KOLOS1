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

    public async Task<int> CreateRentalAsync(int customerId, CreateRentalDTO dto)
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync();

    await using var transaction = await connection.BeginTransactionAsync();

    try
    {
        const string customerSql = """
            SELECT COUNT(1)
            FROM Customer
            WHERE customer_id = @CustomerId;
            """;

        await using (var customerCommand = new SqlCommand(customerSql, connection, (SqlTransaction)transaction))
        {
            customerCommand.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;

            var customerCount = (int)await customerCommand.ExecuteScalarAsync();

            if (customerCount == 0)
                throw new CustomerNotFoundException($"Customer with id {customerId} was not found.");
        }

        var moviesData = new List<(int MovieId, decimal RentalPrice)>();

        foreach (var movie in dto.Movies)
        {
            const string movieSql = """
                SELECT movie_id
                FROM Movie
                WHERE title = @Title;
                """;

            await using var movieCommand = new SqlCommand(movieSql, connection, (SqlTransaction)transaction);
            movieCommand.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = movie.Title;

            var movieIdObj = await movieCommand.ExecuteScalarAsync();
            

            moviesData.Add(((int)movieIdObj, movie.RentalPrice));
        }

        const string statusSql = """
            SELECT status_id
            FROM Status
            WHERE name = 'Rented';
            """;

        int statusId;
        await using (var statusCommand = new SqlCommand(statusSql, connection, (SqlTransaction)transaction))
        {
            var statusObj = await statusCommand.ExecuteScalarAsync();

            statusId = (int)statusObj;
        }

        const string insertRentalSql = """
            INSERT INTO Rental (rental_date, return_date, customer_id, status_id)
            VALUES (@RentalDate, NULL, @CustomerId, @StatusId);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        int rentalId;
        await using (var rentalCommand = new SqlCommand(insertRentalSql, connection, (SqlTransaction)transaction))
        {
            rentalCommand.Parameters.Add("@RentalDate", SqlDbType.DateTime2).Value = dto.RentalDate;
            rentalCommand.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;
            rentalCommand.Parameters.Add("@StatusId", SqlDbType.Int).Value = statusId;

            rentalId = (int)await rentalCommand.ExecuteScalarAsync();
        }

        const string insertRentalItemSql = """
            INSERT INTO Rental_Item (rental_id, movie_id, price_at_rental)
            VALUES (@RentalId, @MovieId, @PriceAtRental);
            """;

        foreach (var movie in moviesData)
        {
            await using var rentalItemCommand = new SqlCommand(insertRentalItemSql, connection, (SqlTransaction)transaction);
            rentalItemCommand.Parameters.Add("@RentalId", SqlDbType.Int).Value = rentalId;
            rentalItemCommand.Parameters.Add("@MovieId", SqlDbType.Int).Value = movie.MovieId;

            var priceParam = rentalItemCommand.Parameters.Add("@PriceAtRental", SqlDbType.Decimal);
            priceParam.Precision = 10;
            priceParam.Scale = 2;
            priceParam.Value = movie.RentalPrice;

            await rentalItemCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
        return rentalId;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
}
