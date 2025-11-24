import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Seller } from '../../models/product/product.model';

@Injectable({
  providedIn: 'root'
})
export class SellerService {
  private apiUrl = `${environment.apiUrl}/sellers`;

  constructor(private http: HttpClient) { }

  getSellerById(id: string): Observable<Seller> {
    return this.http.get<Seller>(`${this.apiUrl}/${id}`);
  }
}
