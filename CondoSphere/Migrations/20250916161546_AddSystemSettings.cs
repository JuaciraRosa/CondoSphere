using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CondoSphere.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CompanyDisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SupportEmail = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    DefaultLateFeePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DefaultInterestMonthlyPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    GraceDaysForQuotas = table.Column<int>(type: "int", nullable: false),
                    WelcomeUserEmailSubject = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    WelcomeUserEmailHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetEmailSubject = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    PasswordResetEmailHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "CompanyDisplayName", "DefaultInterestMonthlyPercent", "DefaultLateFeePercent", "GraceDaysForQuotas", "PasswordResetEmailHtml", "PasswordResetEmailSubject", "SupportEmail", "TenantId", "UpdatedAt", "WelcomeUserEmailHtml", "WelcomeUserEmailSubject" },
                values: new object[] { 1, "CondoSphere", 1.00m, 2.00m, 5, "<p>Olá {{User.Email}},</p><p>Clique para redefinir (válido por 4 dias): <a href='{{ResetUrl}}'>Reset</a></p><p>Se não foi você, ignore.</p>", "CondoSphere – Redefinição de palavra-passe", "support@condosphere.app", null, new DateTimeOffset(new DateTime(2025, 9, 16, 16, 15, 44, 953, DateTimeKind.Unspecified).AddTicks(8102), new TimeSpan(0, 0, 0, 0, 0)), "<p>Olá {{User.FullName}},</p><p>A sua conta foi criada.</p><p>Senha provisória: <code>{{TempPassword}}</code></p><p>Por favor altere aqui: <a href='{{ResetUrl}}'>Alterar palavra-passe</a></p><p>Cumprimentos,<br/>{{Company.Name}}</p>", "Bem-vindo(a) ao CondoSphere" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_TenantId",
                table: "SystemSettings",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
