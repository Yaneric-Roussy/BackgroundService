using BackgroundService.Data;
using BackgroundService.Hubs;
using BackgroundService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SuperChance.DTOs;

namespace BackgroundService.Services
{
    public class UserData
    {
        public int Score { get; set; }
        // TODO: Ajouter une propriété pour le multiplier
        public int Multiplier { get; set; } = 1;
    }

    public class Game(IHubContext<GameHub> gameHub, IServiceScopeFactory serviceScopeFactory)
        : Microsoft.Extensions.Hosting.BackgroundService
    {
        private const int DELAY = 30 * 1000;
        public const int MULTIPLIER_BASE_PRICE = 10;

        private readonly Dictionary<string, UserData> _users = new();

        public void AddUser(string userId)
        {
            _users[userId] = new UserData();
        }

        public void RemoveUser(string userId)
        {
            _users.Remove(userId);
        }

        public void Increment(string userId)
        {
            UserData userData = _users[userId];
            // TODO: Ajouter la valeur du muliplier au lieu d'ajouter 1
            userData.Score += 1 * userData.Multiplier;
        }

        // TODO: Ajouter une méthode pour acheter un multiplier. Le coût est le prix de base * le multiplier actuel
        // Les prix sont donc de 10, 20, 40, 80, 160 (Si le prix de base est 10)
        // Réduire le score du coût du multiplier
        // Doubler le multiplier du joueur
        public async Task BuyMultiplier(string userId, string connectionId)
        {
            UserData userData = _users[userId];
            int multiplierCost = MULTIPLIER_BASE_PRICE * userData.Multiplier;
            if (userData.Score >= multiplierCost)
            {
                userData.Score -= multiplierCost;
                userData.Multiplier *= 2;

                await gameHub.Clients.Client(connectionId).SendAsync("MultiplierBought");
            }
        }

        private async Task EndRound(CancellationToken stoppingToken)
        {
            List<string> winners = [];
            int biggestValue = 0;
            // Reset des compteurs
            foreach (string key in _users.Keys)
            {
                int value = _users[key].Score;
                if (value > 0 && value >= biggestValue)
                {
                    if (value > biggestValue)
                    {
                        winners.Clear();
                        biggestValue = value;
                    }

                    winners.Add(key);
                }
            }

            // Reset
            foreach (string key in _users.Keys)
            {
                // TODO: On remet le multiplier à 1!
                _users[key].Score = 0;
                _users[key].Multiplier = 1;
            }

            // Aucune participation!
            if (biggestValue == 0)
            {
                RoundResult noResult = new()
                {
                    Winners = null,
                    NbClicks = 0
                };
                await gameHub.Clients.All.SendAsync("EndRound", noResult, stoppingToken);
                return;
            }

            using IServiceScope scope = serviceScopeFactory.CreateScope();
            
            BackgroundServiceContext backgroundServiceContext =
                scope.ServiceProvider.GetRequiredService<BackgroundServiceContext>();

            // TODO: Mettre à jour et sauvegarder le nbWins des joueurs
            List<Player> winningPlayers = await backgroundServiceContext.Player
                .Where(p => winners.Contains(p.UserId))
                .Include(player => player.User)
                .ToListAsync(cancellationToken: stoppingToken);

            foreach (Player winningPlayer in winningPlayers)
            {
                winningPlayer.NbWins += 1;
            }

            await backgroundServiceContext.SaveChangesAsync(stoppingToken);

            RoundResult roundResult = new()
            {
                Winners = winningPlayers.Select(p => p.User.UserName)!,
                NbClicks = biggestValue
            };
            await gameHub.Clients.All.SendAsync("EndRound", roundResult, stoppingToken);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(DELAY, stoppingToken);
                await EndRound(stoppingToken);
            }
        }
    }
}