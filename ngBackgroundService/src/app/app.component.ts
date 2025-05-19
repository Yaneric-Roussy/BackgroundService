import { AccountService } from './services/account.service';
import { Component } from '@angular/core';

// On doit commencer par ajouter signalr dans les node_modules: npm install @microsoft/signalr
// Ensuite on inclut la librairie
import * as signalR from "@microsoft/signalr"

interface RoundResult{
  winners:string[],
  nbClicks:number
}

interface GameInfo{
  multiplierCost:number,
  nbWins:number;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'ngBackgroundService';

  baseUrl = "https://localhost:7056/";

  // Ajouter une variable nbWins
  nbWins : number = 0;
  private hubConnection?: signalR.HubConnection

  isConnected = false;
  nbClicks = 0;
  // TODO: Ajouter 3 variables: Le multiplier, le multiplierCost, mais également le multiplierIntialCost pour remettre à jour multiplierCost après chaque fin de round (ou sinon on peut passer l'information dans l'appel qui vient du Hub!)
  multiplier : number = 1;
  multiplierCost : number = 10;
  multiplierInitialCost : number = 10;
  constructor(public account:AccountService){
  }

  Increment() {
    this.nbClicks += this.multiplier;
    this.hubConnection!.invoke('Increment')
  }

  BuyMultiplier() {
    if(this.nbClicks >= this.multiplierCost)
    {
      this.hubConnection!.invoke('BuyMultiplier');
      this.nbClicks -= this.multiplierCost;
      this.multiplier *= 2;
      this.multiplierCost *= 2;
    }
  }

  async register(){
    try{
      await this.account.register();
    }
    catch(e){
      alert("Erreur pendant l'enregistrement!!!!!");
      return;
    }
    alert("L'enregistrement a été un succès!");
  }

  async login(){
    await this.account.login();
  }

  async logout(){
    await this.account.logout();

    if(this.hubConnection?.state == signalR.HubConnectionState.Connected)
      this.hubConnection.stop();
    this.isConnected = false;
  }

  isLoggedIn() : Boolean{
    return this.account.isLoggedIn();
  }

  connectToHub() {
    this.hubConnection = new signalR.HubConnectionBuilder()
                              .withUrl(this.baseUrl + 'game', { accessTokenFactory: () => sessionStorage.getItem("token")! })
                              .build();

    if(!this.hubConnection)
    {
      console.log("Impossible de créer un HubConnection???");
      return;
    }

    this.hubConnection.on('GameInfo', (data:GameInfo) => {
      this.isConnected = true;
      this.multiplierInitialCost = data.multiplierCost;
      this.multiplierCost = this.multiplierInitialCost;
      this.nbWins = data.nbWins;
    });

    this.hubConnection.on('EndRound', (data:RoundResult) => {
      this.nbClicks = 0;
      this.multiplierCost = this.multiplierInitialCost;
      this.multiplier = 1;

      if(data.nbClicks > 0){
        let phrase = " a gagné avec ";
        if(data.winners.length > 1)
          phrase = " ont gagnées avec "
        alert(data.winners.join(", ") + phrase + data.nbClicks + " clicks!");
        this.nbWins++;
      }
      else{
        alert("Aucun gagnant...");
      }
    });

    this.hubConnection
      .start()
      .then(() => {
        console.log("Connecté au Hub");
      })
      .catch(err => console.log('Error while starting connection: ' + err))
  }
}
