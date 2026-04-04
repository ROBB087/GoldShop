using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class ClientNoteRepository
{
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
