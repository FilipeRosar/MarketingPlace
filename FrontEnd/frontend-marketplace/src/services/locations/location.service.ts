import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface IbgeState {
  id: number;
  sigla: string;
  nome: string;
}

export interface IbgeCity {
  id: number;
  nome: string;
}

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private http = inject(HttpClient);
  private readonly BASE_URL = 'https://servicodados.ibge.gov.br/api/v1/localidades';

  getStates(): Observable<IbgeState[]> {
    return this.http.get<IbgeState[]>(`${this.BASE_URL}/estados`).pipe(
      map(states => states.sort((a, b) => a.nome.localeCompare(b.nome)))
    );
  }

  getCitiesByState(uf: string): Observable<IbgeCity[]> {
    return this.http.get<IbgeCity[]>(`${this.BASE_URL}/estados/${uf}/municipios`).pipe(
      map(cities => cities.sort((a, b) => a.nome.localeCompare(b.nome)))
    );
  }
}
