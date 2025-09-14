using CondoSphere.Data.Interfaces;
using CondoSphere.Features.Voting;
using CondoSphere.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CondoSphere.Data.Repositories
{
    public class VotingRepository : IVotingRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly string _jsonPath;
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

        public VotingRepository(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "appdata"));
            _jsonPath = Path.Combine(env.ContentRootPath, "appdata", "votes.json");
            if (!File.Exists(_jsonPath)) File.WriteAllText(_jsonPath, "[]");
        }

        // -------- JSON helpers --------
        private async Task<List<MeetingVoteDto>> ReadAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                using var fs = File.OpenRead(_jsonPath);
                return (await JsonSerializer.DeserializeAsync<List<MeetingVoteDto>>(fs, _json)) ?? new();
            }
            finally { _lock.Release(); }
        }

        private async Task WriteAllAsync(List<MeetingVoteDto> items)
        {
            await _lock.WaitAsync();
            try
            {
                using var fs = File.Create(_jsonPath);
                await JsonSerializer.SerializeAsync(fs, items, _json);
            }
            finally { _lock.Release(); }
        }

        // -------- Public API --------
        public async Task<IReadOnlyList<VoteListItemVM>> ListMeetingsForUserAsync(string userId, string userEmail)
        {
            // condomínios do usuário (residente)
            var myCondoIds = await _db.Units
                .Where(u => u.OwnerId == userId)
                .Select(u => u.CondominiumId)
                .Distinct()
                .ToListAsync();

            // reuniões recentes / futuras desses condomínios
            var limit = DateTime.Now.AddMonths(-1);
            var meetings = await _db.Meetings
                .AsNoTracking()
                .Include(m => m.Condominium)
                .Where(m => myCondoIds.Contains(m.CondominiumId) && m.ScheduledDate >= limit)
                .OrderByDescending(m => m.ScheduledDate)
                .ToListAsync();

            // votos do usuário (para marcar "AlreadyVoted")
            var allVotes = await ReadAllAsync();

            var list = meetings.Select(m => new VoteListItemVM
            {
                MeetingId = m.Id,
                ScheduledDate = m.ScheduledDate,
                Agenda = m.Agenda,
                Condominium = m.Condominium?.Name ?? "",
                AlreadyVoted = !string.IsNullOrWhiteSpace(userEmail) &&
                               allVotes.Any(v => v.MeetingId == m.Id &&
                                   v.VoterEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            }).ToList();

            return list;
        }

        public async Task<IReadOnlyList<SelectListItem>> GetUnitsForMeetingAsync(int meetingId)
        {
            var condoId = await _db.Meetings
                .Where(m => m.Id == meetingId)
                .Select(m => m.CondominiumId)
                .FirstOrDefaultAsync();

            var units = await _db.Units
                .Where(u => u.CondominiumId == condoId)
                .OrderBy(u => u.Number)
                .Select(u => new SelectListItem { Value = u.Number, Text = u.Number })
                .ToListAsync();

            return units;
        }

        public async Task<bool> HasUserVotedAsync(int meetingId, string voterEmail)
        {
            if (string.IsNullOrWhiteSpace(voterEmail)) return false;
            var all = await ReadAllAsync();
            return all.Any(v => v.MeetingId == meetingId &&
                                v.VoterEmail.Equals(voterEmail, StringComparison.OrdinalIgnoreCase));
        }

        public async Task UpsertVoteAsync(MeetingVoteDto vote)
        {
            var all = await ReadAllAsync();

            // 1 voto por Meeting + Email
            all.RemoveAll(v =>
                v.MeetingId == vote.MeetingId &&
                v.VoterEmail.Equals(vote.VoterEmail, StringComparison.OrdinalIgnoreCase));

            all.Add(vote);
            await WriteAllAsync(all);
        }

        public async Task<IReadOnlyList<MeetingVoteDto>> GetVotesByMeetingAsync(int meetingId)
        {
            var all = await ReadAllAsync();
            return all.Where(v => v.MeetingId == meetingId).ToList();
        }

        public async Task<MeetingVoteResultDto> GetResultAsync(int meetingId)
        {
            var list = await GetVotesByMeetingAsync(meetingId);
            return new MeetingVoteResultDto
            {
                MeetingId = meetingId,
                Total = list.Count,
                AFavor = list.Count(x => x.Choice == "A favor"),
                Contra = list.Count(x => x.Choice == "Contra"),
                Abstencao = list.Count(x => x.Choice == "Abstenção"),
            };
        }
    }
}
