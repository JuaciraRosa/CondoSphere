using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Events
{
    public static class AppEvents
    {
        public static event Action<int>? QuotaPaid;

        // Guarda os quotas que ficaram pendentes (até o servidor marcar como Pago)
        private static readonly HashSet<int> _pendingQuotaIds = new();

        public static void RaiseQuotaPaid(int quotaId)
        {
            _pendingQuotaIds.Add(quotaId); // marca como pendente no app
            QuotaPaid?.Invoke(quotaId);    // notifica a tela atual
        }

        public static bool IsPending(int quotaId) => _pendingQuotaIds.Contains(quotaId);

        // Opcional: chamar isto quando perceberes que o servidor já voltou como Pago
        public static void ClearPending(int quotaId) => _pendingQuotaIds.Remove(quotaId);
    }

}
