using CondoSphere.Data.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Globalization;

namespace CondoSphere.Services
{
    public class ReceiptPdfService
    {
        private readonly IPaymentRepository _payments;

        public ReceiptPdfService(IPaymentRepository payments)
        {
            _payments = payments;
        }

        public async Task<byte[]> GenerateAsync(int paymentId)
        {
            var payment = await _payments.GetByIdDetailedAsync(paymentId);
            if (payment == null || payment.Quota == null || payment.Quota.Unit == null || payment.Quota.Unit.Condominium == null)
                throw new Exception("Payment not found or incomplete relationships.");

            var condo = payment.Quota.Unit.Condominium;
            var unit = payment.Quota.Unit;

            var now = DateTime.UtcNow;
            var culture = new CultureInfo("pt-PT");

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Row(row =>
                    {
                        row.ConstantItem(70).Height(50).Placeholder();
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("RECIBO DE PAGAMENTO").SemiBold().FontSize(16);
                            col.Item().Text($"Condomínio: {condo.Name}");
                            col.Item().Text($"Fra\u00E7\u00E3o: {unit.Number}");
                        });
                        row.ConstantItem(120).Column(col =>
                        {
                            col.Item().Text($"Recibo Nº: {payment.Id:D6}").AlignRight();
                            col.Item().Text($"Data: {now.ToString("dd/MM/yyyy HH:mm", culture)}").AlignRight();
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().LineHorizontal(0.8f);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(8);
                            });

                            void Row2(string k, string v)
                            {
                                table.Cell().BorderBottom(0.5f).Padding(5).Text(k).SemiBold();
                                table.Cell().BorderBottom(0.5f).Padding(5).Text(v);
                            }

                            Row2("Pagamento #", payment.Id.ToString());
                            Row2("Quota #", payment.QuotaId.ToString());
                            Row2("Valor", payment.Amount.ToString("C", culture));
                            Row2("Estado", payment.Status.ToString());
                            Row2("M\u00E9todo", payment.Method.ToString());
                            Row2("Fornecedor", payment.Provider ?? "-");
                            Row2("Ref. Provedor", payment.ProviderPaymentId ?? "-");
                            Row2("Data Pagamento", payment.PaidAt?.ToString("dd/MM/yyyy HH:mm", culture) ?? "-");
                            Row2("URL Recibo Gateway", payment.ReceiptUrl ?? "-");
                        });

                        col.Item().PaddingTop(10).Text("Declara\u00E7\u00E3o").SemiBold();
                        col.Item().Text("Este recibo comprova o pagamento das quotas/encargos do cond\u00F3minio conforme identificado acima.");

                        col.Item().PaddingTop(30).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("______________").AlignCenter();
                                c.Item().Text("Administra\u00E7\u00E3o").AlignCenter();
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("______________").AlignCenter();
                                c.Item().Text("Assinatura").AlignCenter();
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("CondoSphere - Recibo gerado automaticamente em ");
                        x.Span(now.ToString("dd/MM/yyyy HH:mm", culture)).SemiBold();
                    });
                });
            });

            return pdf.GeneratePdf();
        }
    }
}
