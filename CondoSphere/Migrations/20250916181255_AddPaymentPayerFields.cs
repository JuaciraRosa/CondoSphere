using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPayerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PaymentReceiptEmailHtml", "PaymentReceiptEmailSubject", "UpdatedAt" },
                values: new object[] { "<p>Olá {{User.FullName}},</p><p>Recebemos o seu pagamento de <strong>{{Payment.Amount}}</strong> em {{Payment.Date}}.</p><p>Referência: <code>{{Payment.Reference}}</code> · Método: {{Payment.Method}}</p><p>Pode consultar/guardar a fatura aqui: <a href='{{InvoiceUrl}}'>Ver fatura</a></p><p>Cumprimentos,<br/>{{Company.Name}}</p>", "Comprovativo de pagamento", new DateTimeOffset(new DateTime(2025, 9, 16, 18, 12, 54, 556, DateTimeKind.Unspecified).AddTicks(95), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PaymentReceiptEmailHtml", "PaymentReceiptEmailSubject", "UpdatedAt" },
                values: new object[] { null, null, new DateTimeOffset(new DateTime(2025, 9, 16, 17, 12, 12, 200, DateTimeKind.Unspecified).AddTicks(5233), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}
