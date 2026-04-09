using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class ClientNoteRepository
{
    public PagedResult<ClientNote> GetPaged(string? searchText, int pageNumber, int pageSize)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Max(pageSize, 1);
        var offset = (normalizedPageNumber - 1) * normalizedPageSize;
        var query = searchText?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(query);
        var likeValue = hasSearch ? $"%{query}%" : null;

        using var connection = Database.OpenConnection();

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = @"
SELECT COUNT(*)
FROM ClientNotes
WHERE $search IS NULL
   OR ClientName LIKE $search
   OR Content LIKE $search;";
        countCommand.Parameters.AddWithValue("$search", (object?)likeValue ?? DBNull.Value);

        var totalCount = Convert.ToInt32(countCommand.ExecuteScalar());

        using var pageCommand = connection.CreateCommand();
        pageCommand.CommandText = @"
SELECT Id, ClientName, Content, CreatedAt
FROM ClientNotes
WHERE $search IS NULL
   OR ClientName LIKE $search
   OR Content LIKE $search
ORDER BY CreatedAt DESC, Id DESC
LIMIT $limit OFFSET $offset;";
        pageCommand.Parameters.AddWithValue("$search", (object?)likeValue ?? DBNull.Value);
        pageCommand.Parameters.AddWithValue("$limit", normalizedPageSize);
        pageCommand.Parameters.AddWithValue("$offset", offset);

        var notes = new List<ClientNote>();
        using var reader = pageCommand.ExecuteReader();
        while (reader.Read())
        {
            notes.Add(new ClientNote
            {
                Id = reader.GetInt32(0),
                ClientName = reader.GetString(1),
                Content = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3))
            });
        }

        return new PagedResult<ClientNote>
        {
            Items = notes,
            TotalCount = totalCount,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize
        };
    }

    public List<ClientNote> GetAll()
    {
        var notes = new List<ClientNote>();
        using var connection = Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ClientName, Content, CreatedAt
FROM ClientNotes
ORDER BY CreatedAt DESC, Id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            notes.Add(new ClientNote
            {
                Id = reader.GetInt32(0),
                ClientName = reader.GetString(1),
                Content = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3))
            });
        }

        return notes;
    }

    public ClientNote? GetById(int id)
    {
        using var connection = Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, ClientName, Content, CreatedAt
FROM ClientNotes
WHERE Id = $id
LIMIT 1;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new ClientNote
        {
            Id = reader.GetInt32(0),
            ClientName = reader.GetString(1),
            Content = reader.GetString(2),
            CreatedAt = DateTime.Parse(reader.GetString(3))
        };
    }

    public int Add(ClientNote note)
    {
        using var connection = Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO ClientNotes (ClientName, Content, CreatedAt)
VALUES ($clientName, $content, $createdAt);
SELECT last_insert_rowid();";
        BindCommon(command, note);

        var id = (long)command.ExecuteScalar()!;
        return (int)id;
    }

    public void Update(ClientNote note)
    {
        using var connection = Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE ClientNotes
SET ClientName = $clientName,
    Content = $content
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", note.Id);
        BindCommon(command, note);
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ClientNotes WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static void BindCommon(SqliteCommand command, ClientNote note)
    {
        command.Parameters.AddWithValue("$clientName", note.ClientName);
        command.Parameters.AddWithValue("$content", note.Content);
        command.Parameters.AddWithValue("$createdAt", note.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}
