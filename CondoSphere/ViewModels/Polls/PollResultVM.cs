using CondoSphere.Models;

namespace CondoSphere.ViewModels.Polls
{
    public class PollResultVM
    {
        public Poll Poll { get; set; } = default!;
        public int Total { get; set; }
        public List<PollResultItemVM> Items { get; set; } = new();
    }
}
