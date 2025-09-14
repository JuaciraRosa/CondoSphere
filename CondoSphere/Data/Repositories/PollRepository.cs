using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class PollRepository : IPollRepository
    {
        private readonly ApplicationDbContext _ctx;
        public PollRepository(ApplicationDbContext ctx) => _ctx = ctx;

        public Task<Poll?> GetByIdAsync(int id) =>
            _ctx.Polls.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        public Task<Poll?> GetDetailsAsync(int id) =>
            _ctx.Polls.AsNoTracking()
                .Include(p => p.Options)
                .Include(p => p.Votes)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<Poll>> GetOpenByCondoAsync(int condominiumId)
        {
            var now = DateTime.UtcNow;
            return await _ctx.Polls
                .AsNoTracking()
                .Where(p => p.CondominiumId == condominiumId &&
                            p.StartsAtUtc <= now && p.EndsAtUtc > now)
                .OrderByDescending(p => p.StartsAtUtc)
                .Take(200)
                .ToListAsync();
        }

        public async Task<IEnumerable<Poll>> GetByCreatorAsync(string userId, int? condominiumId = null)
        {
            var q = _ctx.Polls
                .AsNoTracking()
                .Include(p => p.Condominium)   // <<< carrega o nome do condomínio
                .Where(p => p.CreatedById == userId);

            if (condominiumId.HasValue)
                q = q.Where(p => p.CondominiumId == condominiumId.Value);

            return await q
                .OrderByDescending(p => p.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task AddAsync(Poll poll)
        {
            _ctx.Polls.Add(poll);
            await _ctx.SaveChangesAsync();
        }

        public async Task CloseAsync(int pollId)
        {
            var poll = await _ctx.Polls.FirstOrDefaultAsync(p => p.Id == pollId);
            if (poll == null) return;

            if (poll.EndsAtUtc > DateTime.UtcNow)
                poll.EndsAtUtc = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();
        }

        public async Task DeleteAsync(int pollId)
        {
            var poll = await _ctx.Polls.FirstOrDefaultAsync(p => p.Id == pollId);
            if (poll == null) return;

            _ctx.Polls.Remove(poll);
            await _ctx.SaveChangesAsync();
        }

        public async Task<PollVote> UpsertVoteAsync(int pollId, int optionId, string userId)
        {
            var now = DateTime.UtcNow;

            // valida: option pertence ao poll
            var option = await _ctx.PollOptions.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == optionId && o.PollId == pollId);
            if (option == null)
                throw new InvalidOperationException("Opção inválida para esta enquete.");

            // valida: poll aberto
            var poll = await _ctx.Polls.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pollId);
            if (poll == null) throw new InvalidOperationException("Enquete não encontrada.");
            if (!(poll.StartsAtUtc <= now && poll.EndsAtUtc > now))
                throw new InvalidOperationException("Enquete encerrada.");

            var existing = await _ctx.PollVotes.FirstOrDefaultAsync(v => v.PollId == pollId && v.UserId == userId);
            if (existing == null)
            {
                var vote = new PollVote { PollId = pollId, OptionId = optionId, UserId = userId };
                _ctx.PollVotes.Add(vote);
                await _ctx.SaveChangesAsync();
                return vote;
            }
            else
            {
                existing.OptionId = optionId;
                await _ctx.SaveChangesAsync();
                return existing;
            }
        }


        public async Task UpdateAsync(Poll poll, IEnumerable<string> newOptionsToAppend)
        {
            var db = await _ctx.Polls
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Id == poll.Id);

            if (db == null) throw new InvalidOperationException("Poll not found.");

            // Atualiza campos básicos
            db.Title = poll.Title;
            db.Description = poll.Description;
            db.CondominiumId = poll.CondominiumId;
            db.StartsAtUtc = poll.StartsAtUtc;
            db.EndsAtUtc = poll.EndsAtUtc;
            db.AllowSingleChoice = poll.AllowSingleChoice;

            // Acrescenta novas opções (não remove as antigas — evita conflito com votos)
            var toAdd = (newOptionsToAppend ?? Enumerable.Empty<string>())
                        .Select(t => (t ?? "").Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

            if (toAdd.Count > 0)
            {
                foreach (var txt in toAdd)
                    db.Options.Add(new PollOption { Text = txt });
            }

            await _ctx.SaveChangesAsync();
        }


        // Lado residente: enquetes abertas que o utilizador pode ver
        public async Task<IEnumerable<Poll>> GetOpenForUserAsync(string userId)
        {
            var condoIds = await _ctx.Units
                .Where(u => u.OwnerId == userId)
                .Select(u => u.CondominiumId)
                .Distinct()
                .ToListAsync();

            var now = DateTime.UtcNow;

            return await _ctx.Polls
                .AsNoTracking()
                .Include(p => p.Condominium)
                .Where(p =>
                    p.StartsAtUtc <= now &&
                    p.EndsAtUtc > now &&
                    (!p.CondominiumId.HasValue || condoIds.Contains(p.CondominiumId.Value)))
                .OrderByDescending(p => p.StartsAtUtc)
                .ToListAsync();
        }

        public Task<bool> HasUserVotedAsync(int pollId, string userId) =>
            _ctx.PollVotes.AsNoTracking()
                .AnyAsync(v => v.PollId == pollId && v.UserId == userId);
    }
}
