import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ShippingService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/shipping`;

  constructor() { }

  /**
   * Chama o backend para gerar a etiqueta de envio no Melhor Envio.
   * @param orderId ID do pedido que será enviado
   * @param serviceId ID do serviço de frete (ex: '1' para SEDEX, '2' para PAC) - Opcional se o backend já tiver lógica padrão
   */
  generateLabel(orderId: string, serviceId: string = '1'): Observable<{ labelUrl: string }> {
    const payload = {
      orderId: orderId,
      serviceId: serviceId
    };

    return this.http.post<{ labelUrl: string }>(`${this.apiUrl}/generate-label`, payload);
  }

  /**
   * (Futuro) Método para calcular frete no carrinho
   * @param zipCodeFrom CEP de origem
   * @param zipCodeTo CEP de destino
   * @param items Lista de itens com peso/dimensões
   */
  calculateShipping(zipCodeFrom: string, zipCodeTo: string, items: any[]): Observable<any[]> {
    const payload = {
      zipCodeFrom,
      zipCodeTo,
      items
    };
    return this.http.post<any[]>(`${this.apiUrl}/calculate`, payload);
  }
}
