using CondoSphereMobile.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static CondoSphereMobile.Models.Enums;

namespace CondoSphereMobile.ViewModels
{
    public class QuotaVM : INotifyPropertyChanged
    {
        public QuotaDto Quota { get; }
        public QuotaVM(QuotaDto q)
        {
            Quota = q;
            // pendente quando o servidor disser Pending ou RequiresAction
            IsPending = q.Payment != null &&
                        (q.Payment.Status == PaymentStatusType.Pending ||
                         q.Payment.Status == PaymentStatusType.RequiresAction);
        }

        // estado local
        bool _isPending;
        public bool IsPending
        {
            get => _isPending;
            set { if (_isPending != value) { _isPending = value; Notify(); Notify(nameof(StatusLabel)); Notify(nameof(CanPay)); Notify(nameof(PayButtonText)); } }
        }

        // “proxies” p/ facilitar binding
        public decimal Amount => Quota.Amount;
        public DateTime DueDate => Quota.DueDate;
        public bool IsPaid => Quota.IsPaid;

        // UI
        public string StatusLabel => IsPending ? "Pendente" : (IsPaid ? "Pago" : "Em aberto");
        public bool CanPay => !IsPaid && !IsPending;
        public string PayButtonText => IsPending ? "Pendente" : (IsPaid ? "Pago" : "Pagar");

        public event PropertyChangedEventHandler? PropertyChanged;
        void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
