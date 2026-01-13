import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { ChatService, ChatMessage, ChatThread, ChatCustomerThread } from '../../services/chat/chat.service';
import { SellerService } from '../../services/seller/seller.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css'
})
export class ChatComponent implements OnInit {
  public authService = inject(AuthService);
  private chatService = inject(ChatService);
  private sellerService = inject(SellerService);
  private route = inject(ActivatedRoute);

  isLoading = false;
  errorMessage = '';
  threads: ChatThread[] = [];
  selectedThread: ChatThread | null = null;
  messages: ChatMessage[] = [];
  messageInput = '';
  customerThreads: ChatCustomerThread[] = [];
  selectedCustomerThread: ChatCustomerThread | null = null;

  sellerId: string | null = null;
  sellerUserId: string | null = null;
  sellerName: string | null = null;

  ngOnInit(): void {
    const currentUser = this.authService.currentUserValue;
    if (!currentUser) return;

    this.chatService.privateMessages$.subscribe(messages => {
      const last = messages[messages.length - 1];
      if (!last) return;

      const currentUserId = this.authService.currentUserValue?.id;
      if (currentUser.role === 'Seller') {
        if (!this.selectedThread) return;
        const matchesThread = last.user === this.selectedThread.customerId || (currentUserId && last.user === currentUserId);
        if (matchesThread) {
          this.messages = [...this.messages, last];
          this.touchThread(last.message);
        }
      } else {
        if (!this.selectedCustomerThread) return;
        const matchesSeller = last.user === this.selectedCustomerThread.sellerUserId || (currentUserId && last.user === currentUserId);
        if (matchesSeller) {
          this.messages = [...this.messages, last];
        }
      }
    });

    if (currentUser.role === 'Seller') {
      this.loadSellerContext(currentUser.id);
    } else {
      this.loadCustomerThreads();
      this.route.queryParamMap.subscribe(params => {
        const nextSellerId = params.get('sellerId');
        const nextSellerUserId = params.get('sellerUserId');
        const nextSellerName = params.get('sellerName');
        if (nextSellerId && nextSellerUserId) {
          this.sellerId = nextSellerId;
          this.sellerUserId = nextSellerUserId;
          this.sellerName = nextSellerName;
          this.selectCustomerThreadBySeller(nextSellerId);
        }
      });
    }
  }

  // ✅ Método para formatar horário corretamente
  getFormattedTime(timestamp: string | Date): string {
    try {
      const date = typeof timestamp === 'string' ? new Date(timestamp) : timestamp;

      // Verifica se a data é válida
      if (isNaN(date.getTime())) {
        return '00:00';
      }

      const hours = date.getHours().toString().padStart(2, '0');
      const minutes = date.getMinutes().toString().padStart(2, '0');
      return `${hours}:${minutes}`;
    } catch (error) {
      console.error('Erro ao formatar timestamp:', error);
      return '00:00';
    }
  }

  selectThread(thread: ChatThread) {
    if (!this.sellerId) return;
    this.selectedThread = thread;
    this.loadThreadMessages(thread);
  }

  sendMessage() {
    const message = this.messageInput.trim();
    if (!message) return;

    if (this.authService.currentUserValue?.role === 'Seller') {
      if (!this.selectedThread) return;
      this.chatService.sendPrivateMessageHttp(this.selectedThread.customerId, message).subscribe({
        next: (sent) => {
          this.messages = [...this.messages, sent];
          this.touchThread(sent.message);
          this.messageInput = '';
        },
        error: () => {
          this.errorMessage = 'Erro ao enviar mensagem.';
        }
      });
    } else {
      if (!this.selectedCustomerThread) return;
      this.chatService.sendPrivateMessageHttp(this.selectedCustomerThread.sellerUserId, message).subscribe({
        next: (sent) => {
          this.messages = [...this.messages, sent];
          this.touchCustomerThread(sent.message);
          this.messageInput = '';
        },
        error: () => {
          this.errorMessage = 'Erro ao enviar mensagem.';
        }
      });
    }
  }

  private loadSellerContext(userId: string) {
    this.isLoading = true;
    this.errorMessage = '';
    this.sellerService.getSellerByUserId(userId).subscribe({
      next: (seller) => {
        this.sellerId = seller.id;
        this.loadThreads();
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Erro ao carregar dados do vendedor.';
      }
    });
  }

  private loadThreads() {
    this.chatService.getThreads().subscribe({
      next: (threads) => {
        this.threads = threads;
        this.isLoading = false;
        if (!this.selectedThread && threads.length) {
          this.selectThread(threads[0]);
        }
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Erro ao carregar conversas.';
      }
    });
  }

  private loadThreadMessages(thread: ChatThread) {
    if (!this.sellerId) return;
    this.isLoading = true;
    this.chatService.getMessages(this.sellerId, thread.customerId).subscribe({
      next: (messages) => {
        this.messages = messages;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Erro ao carregar mensagens.';
      }
    });
  }

  private loadCustomerMessages() {
    const currentUserId = this.authService.currentUserValue?.id;
    if (!this.sellerId || !currentUserId) return;
    this.isLoading = true;
    this.chatService.getMessages(this.sellerId, currentUserId).subscribe({
      next: (messages) => {
        this.messages = messages;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Erro ao carregar mensagens.';
      }
    });
  }

  private loadCustomerThreads() {
    this.isLoading = true;
    this.chatService.getCustomerThreads().subscribe({
      next: (threads) => {
        this.customerThreads = threads;
        this.isLoading = false;
        if (threads.length && !this.selectedCustomerThread) {
          this.selectCustomerThread(threads[0]);
        }
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Erro ao carregar conversas.';
      }
    });
  }

  selectCustomerThread(thread: ChatCustomerThread) {
    this.selectedCustomerThread = thread;
    this.sellerId = thread.sellerId;
    this.sellerUserId = thread.sellerUserId;
    this.sellerName = thread.sellerName;
    this.loadCustomerMessages();
  }

  private selectCustomerThreadBySeller(sellerId: string) {
    const existing = this.customerThreads.find(t => t.sellerId === sellerId);
    if (existing) {
      this.selectCustomerThread(existing);
      return;
    }
    if (this.sellerUserId) {
      this.selectedCustomerThread = {
        sellerId,
        sellerUserId: this.sellerUserId,
        sellerName: this.sellerName || 'Loja',
        sellerImageUrl: undefined,
        lastMessage: '',
        lastMessageAt: new Date().toISOString()
      };
    } else {
      this.selectedCustomerThread = null;
    }
    this.loadCustomerMessages();
  }

  private touchThread(lastMessage: string) {
    if (!this.selectedThread) return;
    const thread = this.threads.find(t => t.customerId === this.selectedThread?.customerId);
    if (thread) {
      thread.lastMessage = lastMessage;
      thread.lastMessageAt = new Date().toISOString();
    }
  }

  private touchCustomerThread(lastMessage: string) {
    if (!this.selectedCustomerThread) return;
    const thread = this.customerThreads.find(t => t.sellerId === this.selectedCustomerThread?.sellerId);
    if (thread) {
      thread.lastMessage = lastMessage;
      thread.lastMessageAt = new Date().toISOString();
    } else {
      this.customerThreads = [
        {
          ...this.selectedCustomerThread,
          lastMessage,
          lastMessageAt: new Date().toISOString()
        },
        ...this.customerThreads
      ];
    }
  }
}
