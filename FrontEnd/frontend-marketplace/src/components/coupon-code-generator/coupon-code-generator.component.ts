import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-coupon-code-generator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="generator-container">
      <h3>🎲 Gerador de Código de Cupom</h3>
      
      <div class="generator-form">
        <div class="form-group">
          <label>Prefixo (opcional)</label>
          <input 
            type="text" 
            [(ngModel)]="prefix" 
            placeholder="Ex: SUMMER2024"
            maxlength="15"
            class="input"
          />
        </div>

        <div class="form-group">
          <label>Quantidade</label>
          <input 
            type="number" 
            [(ngModel)]="quantity" 
            min="1"
            max="10"
            class="input"
          />
        </div>

        <button (click)="generateCodes()" class="btn-generate">
          Gerar {{ quantity }} Código(s)
        </button>
      </div>

      <!-- Generated Codes -->
      <div *ngIf="generatedCodes.length > 0" class="generated-codes">
        <h4>Códigos Gerados:</h4>
        <div class="codes-list">
          <div *ngFor="let code of generatedCodes" class="code-item">
            <code>{{ code }}</code>
            <button (click)="copyToClipboard(code)" class="btn-copy" title="Copiar">
              📋
            </button>
          </div>
        </div>
        <button (click)="copyAllToClipboard()" class="btn-copy-all">
          Copiar Todos
        </button>
        <button (click)="clearCodes()" class="btn-clear">
          Limpar
        </button>
      </div>

      <!-- Copy Feedback -->
      <div *ngIf="copyFeedback" class="feedback" [class.show]="copyFeedback">
        ✓ Copiado para área de transferência!
      </div>
    </div>
  `,
  styles: [`
    .generator-container {
      padding: 20px;
      background: white;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    h3 {
      margin: 0 0 20px 0;
      font-size: 16px;
      color: #333;
    }

    .generator-form {
      display: flex;
      gap: 15px;
      margin-bottom: 20px;
      flex-wrap: wrap;
      align-items: flex-end;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-width: 150px;
    }

    label {
      font-size: 12px;
      color: #666;
      margin-bottom: 5px;
      font-weight: 500;
    }

    .input {
      padding: 8px 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
    }

    .input:focus {
      outline: none;
      border-color: #0066cc;
      box-shadow: 0 0 0 2px rgba(0, 102, 204, 0.1);
    }

    .btn-generate {
      padding: 8px 16px;
      background: #0066cc;
      color: white;
      border: none;
      border-radius: 4px;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s;
    }

    .btn-generate:hover {
      background: #0052a3;
    }

    .generated-codes {
      background: #f8f9fa;
      padding: 15px;
      border-radius: 8px;
      margin-top: 20px;
    }

    h4 {
      margin: 0 0 10px 0;
      font-size: 14px;
      color: #333;
    }

    .codes-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-bottom: 15px;
      max-height: 300px;
      overflow-y: auto;
    }

    .code-item {
      display: flex;
      align-items: center;
      gap: 10px;
      background: white;
      padding: 10px;
      border-radius: 4px;
      border: 1px solid #ddd;
    }

    code {
      font-family: monospace;
      font-weight: 600;
      color: #0066cc;
      flex: 1;
      padding: 5px;
    }

    .btn-copy {
      background: none;
      border: none;
      font-size: 16px;
      cursor: pointer;
      padding: 5px;
      transition: transform 0.2s;
    }

    .btn-copy:hover {
      transform: scale(1.2);
    }

    .btn-copy-all,
    .btn-clear {
      padding: 8px 16px;
      border: none;
      border-radius: 4px;
      font-weight: 500;
      cursor: pointer;
      margin-right: 10px;
      transition: background 0.2s;
    }

    .btn-copy-all {
      background: #28a745;
      color: white;
    }

    .btn-copy-all:hover {
      background: #218838;
    }

    .btn-clear {
      background: #6c757d;
      color: white;
    }

    .btn-clear:hover {
      background: #5a6268;
    }

    .feedback {
      position: fixed;
      bottom: 20px;
      right: 20px;
      background: #28a745;
      color: white;
      padding: 12px 20px;
      border-radius: 4px;
      opacity: 0;
      transition: opacity 0.3s;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
    }

    .feedback.show {
      opacity: 1;
    }
  `]
})
export class CouponCodeGeneratorComponent {
  prefix = '';
  quantity = 1;
  generatedCodes: string[] = [];
  copyFeedback = false;

  generateCodes() {
    this.generatedCodes = [];
    
    for (let i = 0; i < this.quantity; i++) {
      const code = this.generateCode();
      this.generatedCodes.push(code);
    }
  }

  private generateCode(): string {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    let code = this.prefix;
    
    // Gerar 8 caracteres aleatórios
    for (let i = 0; i < 8; i++) {
      code += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    
    return code.toUpperCase();
  }

  copyToClipboard(code: string) {
    navigator.clipboard.writeText(code).then(() => {
      this.showFeedback();
    });
  }

  copyAllToClipboard() {
    const allCodes = this.generatedCodes.join('\n');
    navigator.clipboard.writeText(allCodes).then(() => {
      this.showFeedback();
    });
  }

  private showFeedback() {
    this.copyFeedback = true;
    setTimeout(() => {
      this.copyFeedback = false;
    }, 2000);
  }

  clearCodes() {
    this.generatedCodes = [];
  }
}
