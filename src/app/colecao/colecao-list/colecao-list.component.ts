import { Component, OnInit } from '@angular/core';
import { ColecaoService, Colecao } from '../../services/colecao.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-colecao-list',
  templateUrl: './colecao-list.component.html'
})
export class ColecaoListComponent implements OnInit {
  colecoes: Colecao[] = [];

  constructor(private colecaoService: ColecaoService, private router: Router) {}

  ngOnInit() {
    this.loadColecoes();
  }

  loadColecoes() {
    this.colecaoService.getAll().subscribe(data => this.colecoes = data);
  }

  deleteColecao(id: string) {
    this.colecaoService.delete(id).subscribe(() => this.loadColecoes());
  }
}