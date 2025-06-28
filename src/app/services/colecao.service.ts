import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Colecao {
    id: string;
    nome: string;
    ano: number;
    dataLancamento?: string;
}

@Injectable({
    providedIn: 'root'
})

export class ColecaoService {
    private apiUrl = `${environment.apiUrl}/colecao`;

    constructor(private http: HttpClient) { }

    getAll(): Observable<Colecao[]> {
        return this.http.get<Colecao[]>(this.apiUrl);
    }

    getById(id: string): Observable<Colecao> {
        return this.http.get<Colecao>(`${this.apiUrl}/${id}`);
    }

    getByFilter(filter: string): Observable<Colecao[]> {
        return this.http.get<Colecao[]>(`${this.apiUrl}/${filter}`);
    }

    create(colecao: Colecao): Observable<Colecao> {
        return this.http.post<Colecao>(this.apiUrl, colecao);
    }

    update(id: string, colecao: Colecao): Observable<void> {
        return this.http.put<void>(`${this.apiUrl}/${id}`, colecao);
    }

    delete(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}