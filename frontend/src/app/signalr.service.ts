import { Injectable, NgZone, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';

export interface NewBidEvent {
  amount: number;
  bidderId: string;
}

export interface AuctionClosedEvent {
  winnerBidderId: string | null;
  winningBidAmount: number;
}

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private hubConnection?: signalR.HubConnection;
  private baseUrl = '';

  onNewBid: (data: NewBidEvent) => void = () => {};
  onAuctionClosed: (data: AuctionClosedEvent) => void = () => {};
  onConnected: () => void = () => {};

  constructor(private ngZone: NgZone) {}

  setBaseUrl(url: string): void {
    this.baseUrl = url;
  }

  async start(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/hubs/auction`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('NewBid', (data: NewBidEvent) => {
      this.ngZone.run(() => this.onNewBid(data));
    });

    this.hubConnection.on('AuctionClosed', (data: AuctionClosedEvent) => {
      this.ngZone.run(() => this.onAuctionClosed(data));
    });

    await this.hubConnection.start();
    this.ngZone.run(() => this.onConnected());
  }

  async joinAuction(auctionId: string): Promise<void> {
    if (!this.hubConnection) throw new Error('Not connected');
    await this.hubConnection.invoke('JoinAuction', auctionId);
  }

  async stop(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = undefined;
    }
  }

  ngOnDestroy(): void {
    this.stop();
  }
}