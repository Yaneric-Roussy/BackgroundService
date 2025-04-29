using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BackgroundService.Models
{
    public class Player
    {
        public int Id { get; set; }
        [MaxLength(100)]
        public required string UserId { get; set; }
        public required IdentityUser User { get; set; }
        // TODO: Ajouter une propriété NbWins
        public int NbWins { get; set; } = 0;
    }
}
