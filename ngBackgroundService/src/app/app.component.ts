import { AccountService } from './services/account.service';
import { Component } from '@angular/core';

// On doit commencer par ajouter signalr dans les node_modules: npm install @microsoft/signalr
// Ensuite on inclut la librairie
import * as signalR from '@microsoft/signalr';

interface RoundResult {
  winners: string[];
  nbClicks: number;
}

interface GameInfo {
  multiplierCost: number;
  nbWins: number;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent {
  title = 'ngBackgroundService';

  baseUrl = 'https://localhost:7056/';

  // Ajouter une variable nbWins
  nbWins = 0;

  private hubConnection?: signalR.HubConnection;

  isConnected = false;
  nbClicks = 0;
  // TODO: Ajouter 3 variables: Le multiplier, le multiplierCost, mais également le multiplierIntialCost pour remettre à jour multiplierCost après chaque fin de round (ou sinon on peut passer l'information dans l'appel qui vient du Hub!)
  multiplier = 1;
  multiplierCost = 0;
  multiplierInitialCost = 1;

  constructor(public account: AccountService) {}

  increment() {
    //TODO: Augmenter le nbClicks par la valeur du multiplicateur
    this.nbClicks += 1 * this.multiplier;
    this.hubConnection!.invoke('Increment');
  }

  buyMultiplier() {
    // TODO: Implémenter la méthode qui permet d'acheter un niveau de multiplier (Appel au Hub!)
    this.hubConnection!.invoke('BuyMultiplier');
  }

  async register() {
    try {
      await this.account.register();
    } catch (e) {
      alert("Erreur pendant l'enregistrement!!!!!");
      return;
    }
    alert("L'enregistrement a été un succès!");
  }

  async login() {
    await this.account.login();
  }

  async logout() {
    await this.account.logout();

    if (this.hubConnection?.state == signalR.HubConnectionState.Connected)
      this.hubConnection.stop();
    this.isConnected = false;
  }

  isLoggedIn(): Boolean {
    return this.account.isLoggedIn();
  }

  connectToHub() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.baseUrl + 'game', {
        accessTokenFactory: () => sessionStorage.getItem('token')!,
      })
      .build();

    if (!this.hubConnection) {
      console.log('Impossible de créer un HubConnection???');
      return;
    }

    this.hubConnection.on('GameInfo', (data: GameInfo) => {
      this.isConnected = true;
      // TODO: Mettre à jour les variables pour le coût du multiplier et le nbWins
      this.nbWins = data.nbWins;
      this.multiplierCost = data.multiplierCost;
      this.multiplierInitialCost = data.multiplierCost;
      this.multiplier = 1;
    });

    this.hubConnection.on('MultiplierBought', () => {
      console.log('Multiplier bought');
      this.nbClicks -= this.multiplierCost;
      this.multiplierCost *= 2;
      this.multiplier *= 2;
    });

    this.hubConnection.on('EndRound', (data: RoundResult) => {
      this.nbClicks = 0;
      // TODO: Reset du multiplierCost et le multiplier
      this.multiplierCost = this.multiplierInitialCost;
      this.multiplier = 1;

      // TODO: Si le joueur a gagné, on augmene nbWins
      if (data.winners.includes(this.account.username)) {
        this.nbWins++;
      }

      if (data.nbClicks > 0) {
        let phrase = ' a gagné avec ';
        if (data.winners.length > 1) phrase = ' ont gagnées avec ';
        alert(data.winners.join(', ') + phrase + data.nbClicks + ' clicks!');
      } else {
        alert('Aucun gagnant...');
      }
    });

    this.hubConnection
      .start()
      .then(() => {
        console.log('Connecté au Hub');
      })
      .catch((err) => console.log('Error while starting connection: ' + err));
  }
}
