using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class ClientNoteService
{
    private readonly ClientNoteRepository _clientNoteRepository;
    private readonly AuditService _auditService;

    public ClientNoteService(ClientNoteRepository clientNoteRepository, AuditService auditService)
    {
        _clientNoteRepository = clientNoteRepository;
        _auditService = auditService;
    }

    public PagedResult<ClientNote> GetNotesPage(string? searchText, int pageNumber, int pageSize)
        => _clientNoteRepository.GetPaged(searchText, pageNumber, pageSize);

    public List<ClientNote> GetNotes() => _clientNoteRepository.GetAll();

    public int AddNote(string clientName, string content)
    {
        var note = new ClientNote
        {
            ClientName = clientName.Trim(),
            Content = content.Trim(),
            CreatedAt = DateTime.Now
        };

        var id = _clientNoteRepository.Add(note);
        note.Id = id;
        _auditService.Log("ClientNote", id, "Create", null, note);
        return id;
    }

    public void UpdateNote(int id, string clientName, string content, DateTime createdAt)
    {
        var existing = _clientNoteRepository.GetById(id);
        var note = new ClientNote
        {
            Id = id,
            ClientName = clientName.Trim(),
            Content = content.Trim(),
            CreatedAt = createdAt
        };

        _clientNoteRepository.Update(note);
        _auditService.Log("ClientNote", id, "Update", existing, note);
    }

    public void DeleteNote(int id)
    {
        var existing = _clientNoteRepository.GetById(id);
        _clientNoteRepository.Delete(id);
        if (existing != null)
        {
            _auditService.Log("ClientNote", id, "Delete", existing, null);
        }
    }
}
