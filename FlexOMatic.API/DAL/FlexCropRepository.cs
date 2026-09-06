using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

public class FlexCropRepository
{
    private readonly string _connectionString;

    public FlexCropRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("FlexDb");
    }

    public IEnumerable<FlexCrop> GetAll()
    {
        var crops = new List<FlexCrop>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("GetAllFlexCrops", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            crops.Add(new FlexCrop
            {
                CropID = (int)reader["CropID"],
                CropIdea = reader["CropIdea"].ToString(),
                CropName = reader["CropName"].ToString(),
                Lore = reader["Lore"].ToString(),
                Rarity = reader["Rarity"].ToString(),
                SubmitterName = reader["SubmitterName"].ToString(),
                ImageUrl = reader["ImageUrl"].ToString(),
                SubmissionDate = (DateTime)reader["SubmissionDate"],
                Likes = (int)reader["Likes"]
            });
        }

        return crops;
    }

    public void InsertMinimal(string cropIdea, string cropName, string imageUrl)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("InsertFlexCrop", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@CropIdea", cropIdea);
        cmd.Parameters.AddWithValue("@CropName", cropName);
        cmd.Parameters.AddWithValue("@Lore", "Generated from server-side brilliance");
        cmd.Parameters.AddWithValue("@Rarity", GetRandomRarity()); // or randomized
        cmd.Parameters.AddWithValue("@SubmitterName", "Anonymous");
        cmd.Parameters.AddWithValue("@ImageUrl", imageUrl);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public int InsertMinimalReturningId(string cropIdea, string cropName, string imageUrl)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("InsertFlexCrop", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        // Prefer explicit types/sizes to avoid AddWithValue surprises
        cmd.Parameters.Add("@CropIdea", SqlDbType.NVarChar, 255).Value = (object)cropIdea ?? DBNull.Value;
        cmd.Parameters.Add("@CropName", SqlDbType.NVarChar, 255).Value = (object)cropName ?? DBNull.Value;
        cmd.Parameters.Add("@Lore", SqlDbType.NVarChar, 255).Value = "Generated from server-side brilliance";
        cmd.Parameters.Add("@Rarity", SqlDbType.NVarChar, 50).Value = GetRandomRarity();
        cmd.Parameters.Add("@SubmitterName", SqlDbType.NVarChar, 100).Value = "Anonymous";
        cmd.Parameters.Add("@ImageUrl", SqlDbType.NVarChar, 500).Value = (object)imageUrl ?? DBNull.Value;

        conn.Open();

        // Because the proc returns a result set with the new ID
        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }


    private string GetRandomRarity()
    {
        var rarities = new List<(string Name, int Weight)>
        {
            ("MYTHICAL", 1),
            ("LEGENDARY", 3),
            ("EPIC", 6),
            ("RARE", 10),
            ("UNCOMMON", 20),
            ("COMMON", 60)
        };

        var totalWeight = rarities.Sum(r => r.Weight);
        var rand = new Random().Next(0, totalWeight);
        var acc = 0;

        foreach (var rarity in rarities)
        {
            acc += rarity.Weight;
            if (rand < acc)
                return rarity.Name;
        }

        return "COMMON";
    }

    public void LogRequest(string ipAddress, string cropIdea, string polishedPrompt, int statusCode, string statusMessage)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("InsertFlexLog", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@IPAddress", ipAddress);
        cmd.Parameters.AddWithValue("@CropIdea", cropIdea ?? string.Empty);
        cmd.Parameters.AddWithValue("@PolishedPrompt", polishedPrompt ?? string.Empty);
        cmd.Parameters.AddWithValue("@StatusCode", statusCode);
        cmd.Parameters.AddWithValue("@StatusMessage", statusMessage ?? string.Empty);

        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void UpdateSubmitterName(int cropId, string submitterName)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("UPDATE FLEX_CROPS SET SubmitterName = @name WHERE CropID = @id", conn);
        cmd.Parameters.AddWithValue("@name", submitterName);
        cmd.Parameters.AddWithValue("@id", cropId);
        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public void IncrementLikes(int cropId)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("UPDATE FLEX_CROPS SET Likes = ISNULL(Likes,0) + 1 WHERE CropID = @id", conn);
        cmd.Parameters.AddWithValue("@id", cropId);
        conn.Open();
        cmd.ExecuteNonQuery();
    }

    public int CountSubmissionsFromIp(string ip, DateTime since)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT COUNT(*) FROM FLEX_LOGS WHERE IPAddress = @ip AND CreatedAt >= @since", conn);
        cmd.Parameters.AddWithValue("@ip", ip);
        cmd.Parameters.AddWithValue("@since", since);
        conn.Open();
        return (int)cmd.ExecuteScalar();
    }



}
