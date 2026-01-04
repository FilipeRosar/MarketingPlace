import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Product } from '../../models/product/product.model';


export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  pages: number;
}
@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private apiUrl = `${environment.apiUrl}/products`;

  constructor(private http: HttpClient) {
    console.log('API URL sendo usada:', this.apiUrl);
  }

  getAllProducts(
    page: number = 1,
    pageSize: number = 10,
    search: string = '',
    category?: string,
    minPrice?: number,
    maxPrice?: number,
    sellerId?: string
  ): Observable<any> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (category) params = params.set('category', category);
    if (sellerId) params = params.set('sellerId', sellerId);
    if (minPrice) params = params.set('minPrice', minPrice.toString());
    if (maxPrice) params = params.set('maxPrice', maxPrice.toString());
    return this.http.get<any>(this.apiUrl, { params });
  }
   updateProduct(id: string, productData: any): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, productData);
  }

  getProductById(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  createProduct(productData: FormData): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, productData);
  }
  deleteProduct(id: string): Observable<void> {
    console.log('POST para:', this.apiUrl);
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
