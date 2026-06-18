import { Component, OnInit, signal } from '@angular/core';
import { AuthService } from './auth.service';
import { SignalRService, NewBidEvent, AuctionClosedEvent } from './signalr.service';

interface EventLog {
  text: string;
  timestamp: Date;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App implements OnInit {
  baseUrl = 'http://localhost:5187';

  // Auth forms
  showLogin = signal(true);
  email = signal('');
  password = signal('');
  authError = signal('');
  authLoading = signal(false);

  // Auction creation
  title = signal('');
  description = signal('');
  startingPrice = signal(0);
  endTime = signal('');
  createdAuctionId = signal('');
  createError = signal('');
  createLoading = signal(false);

  // Auction join / bid
  auctionId = signal('');
  bidAmount = signal(0);
  joined = signal(false);
  auctionClosed = signal(false);
  currentHighestBid = signal(0);
  bidError = signal('');
  bidLoading = signal(false);
  joinLoading = signal(false);

  // Events
  events = signal<EventLog[]>([]);

  constructor(
    protected auth: AuthService,
    private signalrService: SignalRService
  ) {
    this.signalrService.setBaseUrl(this.baseUrl);
  }

  async ngOnInit(): Promise<void> {
    try {
      await this.signalrService.start();
    } catch {
      // Not logged in yet — that's fine, will connect later
    }

    this.signalrService.onNewBid = (data: NewBidEvent) => {
      this.addEvent(`New bid: ${data.amount} by ${data.bidderId}`);
      this.currentHighestBid.set(data.amount);
    };

    this.signalrService.onAuctionClosed = (data: AuctionClosedEvent) => {
      this.addEvent(
        `Auction closed | Winner: ${data.winnerBidderId ?? 'no bids'} | Final bid: ${data.winningBidAmount}`
      );
      this.auctionClosed.set(true);
      this.currentHighestBid.set(data.winningBidAmount);
    };
  }

  toggleForm(): void {
    this.showLogin.update(v => !v);
    this.authError.set('');
  }

  async register(): Promise<void> {
    this.authLoading.set(true);
    this.authError.set('');
    try {
      const res = await fetch(`${this.baseUrl}/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: this.email(), password: this.password() })
      });
      if (!res.ok) {
        const body = await res.json();
        this.authError.set(body?.title || 'Registration failed');
        return;
      }
      this.addEvent('Registration successful! Please log in.');
      this.showLogin.set(true);
    } catch (e: any) {
      this.authError.set(e.message || 'Network error');
    } finally {
      this.authLoading.set(false);
    }
  }

  async login(): Promise<void> {
    this.authLoading.set(true);
    this.authError.set('');
    try {
      const res = await fetch(`${this.baseUrl}/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: this.email(), password: this.password() })
      });
      if (res.status === 401) {
        this.authError.set('Invalid email or password');
        return;
      }
      if (!res.ok) {
        this.authError.set('Login failed');
        return;
      }
      const body = await res.json();
      this.auth.token = body.token;
      this.addEvent('Logged in successfully!');
      await this.signalrService.start();
    } catch (e: any) {
      this.authError.set(e.message || 'Network error');
    } finally {
      this.authLoading.set(false);
    }
  }

  logout(): void {
    this.auth.logout();
    this.joined.set(false);
    this.auctionClosed.set(false);
    this.createdAuctionId.set('');
    this.signalrService.stop();
  }

  async createAuction(): Promise<void> {
    this.createLoading.set(true);
    this.createError.set('');
    this.createdAuctionId.set('');
    try {
      const res = await fetch(`${this.baseUrl}/auctions`, {
        method: 'POST',
        headers: this.auth.getAuthHeaders(),
        body: JSON.stringify({
          title: this.title(),
          description: this.description(),
          startingPrice: this.startingPrice(),
          endTime: new Date(this.endTime()).toISOString()
        })
      });
      if (res.status === 401) {
        this.createError.set('Unauthorized — please log in again');
        return;
      }
      if (!res.ok) {
        const body = await res.text();
        this.createError.set(body || 'Failed to create auction');
        return;
      }
      const body = await res.json();
      this.createdAuctionId.set(body.id);
      this.addEvent(`Auction created! ID: ${body.id}`);
    } catch (e: any) {
      this.createError.set(e.message || 'Network error');
    } finally {
      this.createLoading.set(false);
    }
  }

  async joinAuction(): Promise<void> {
    const id = this.auctionId().trim();
    if (!id) {
      this.bidError.set('Please enter an Auction ID');
      return;
    }

    this.joinLoading.set(true);
    this.bidError.set('');

    try {
      await this.signalrService.joinAuction(id);

      const res = await fetch(`${this.baseUrl}/auctions/${id}`);
      if (!res.ok) {
        throw new Error(`HTTP error! Status: ${res.status}`);
      }
      const auction = await res.json();

      this.joined.set(true);
      this.auctionClosed.set(auction.isClosed);
      this.currentHighestBid.set(auction.currentHighestBid);
      this.auctionId.set(id);

      if (auction.isClosed) {
        this.addEvent(`Auction already closed. Winner: ${auction.winnerId ?? 'no bids'} | Final bid: ${auction.currentHighestBid}`);
      } else {
        this.addEvent(`Auction active. Current highest bid: ${auction.currentHighestBid}`);
      }
    } catch (e: any) {
      this.bidError.set(e.message || 'Failed to join auction');
    } finally {
      this.joinLoading.set(false);
    }
  }

  async placeBid(): Promise<void> {
    if (!this.auctionId()) return;

    this.bidLoading.set(true);
    this.bidError.set('');

    try {
      const res = await fetch(`${this.baseUrl}/auctions/${this.auctionId()}/bids`, {
        method: 'POST',
        headers: this.auth.getAuthHeaders(),
        body: JSON.stringify({ amount: this.bidAmount() })
      });

      if (res.status === 401) {
        this.bidError.set('Unauthorized — please log in again');
        return;
      }

      if (res.status === 404) {
        this.bidError.set('Auction not found');
        return;
      }

      if (!res.ok) {
        const body = await res.text();
        this.bidError.set(body || 'Failed to place bid');
        return;
      }

      const body = await res.json();
      this.addEvent(`Bid placed: ${body.amount}`);
    } catch (e: any) {
      this.bidError.set(e.message || 'Network error');
    } finally {
      this.bidLoading.set(false);
    }
  }

  toNumber(value: any): number {
    return Number(value);
  }

  private addEvent(text: string): void {
    this.events.update(events => [...events, { text, timestamp: new Date() }]);
  }
}
