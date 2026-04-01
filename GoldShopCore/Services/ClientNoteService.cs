using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class ClientNoteService
{
    private readonly ClientNoteRepository _clientNoteRepository;

    public ClientNoteService(ClientNoteRepository clientNoteRepository)
    {
        _clientNoteRepository = clientNoteRepository;
    }

    public List<ClientNote> GetNotes() => _clientNoteRepository.GetAll();

    public int AddNote(string clientName, string content)
    {
        var note = new ClientNote
        {
            ClientName = clientName.Trim(),
            Content = content.Trim(),
            CreatedAt = DateTime.Now
        };

        return _clientNoteRepository.Add(note);
    }

    public void UpdateNote(int id, string clientName, string content, DateTime createdAt)
    {
        var note = new ClientNote
        {
            Id = id,
            ClientName = clientName.Trim(),
            Content = content.Trim(),
            CreatedAt = createdAt
        };

        _clientNoteRepository.Update(note);
    }

    public void DeleteNote(int id)
    {
        _clientNoteRepository.Delete(id);
    }
}
