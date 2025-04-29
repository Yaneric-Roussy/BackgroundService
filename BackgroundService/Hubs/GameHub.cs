using BackgroundService.Data;
using BackgroundService.DTOs;
using BackgroundService.Models;
using BackgroundService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackgroundService.Hubs
{
    [Authorize]
    public class GameHub(Game game, BackgroundServiceContext backgroundServiceContext)
        : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            game.AddUser(Context.UserIdentifier!);

            Player player = backgroundServiceContext.Player.Single(p => p.UserId == Context.UserIdentifier!);

            await Clients.Caller.SendAsync("GameInfo", new GameInfoDto
            {
                NbWins = player.NbWins,
                MultiplierCost = Game.MULTIPLIER_BASE_PRICE,
                // TODO: Remplir l'information avec les 2 nouveaux features (nbWins et multiplierCost)
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            game.RemoveUser(Context.UserIdentifier!);
            await base.OnDisconnectedAsync(exception);
        }

        public void Increment()
        {
            game.Increment(Context.UserIdentifier!);
        }

        // Ajouter une méthode pour pouvoir acheter un multiplier
        public async Task BuyMultiplier()
        {
            await game.BuyMultiplier(Context.UserIdentifier!, Context.ConnectionId);
        }
    }
}
