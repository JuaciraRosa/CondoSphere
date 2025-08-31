using CondoSphere.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    [Index(nameof(TaxNumber), IsUnique = true)] // opcional: garante unicidade
    public class Company
    {
        public int Id { get; set; }

        [Required, StringLength(100, ErrorMessage = "Name can't exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(20, ErrorMessage = "Tax Number can't exceed 20 characters.")]
        public string TaxNumber { get; set; } = string.Empty;

        [Required, EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;

        // Evita validação/binding nas navegações do formulário e previne null
        [ValidateNever]
        public ICollection<User> Users { get; set; } = new List<User>();

        [ValidateNever]
        public ICollection<Condominium> Condominiums { get; set; } = new List<Condominium>();
    }

}
