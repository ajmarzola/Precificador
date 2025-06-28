import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ColecaoService, Colecao } from '../../services/colecao.service';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-colecao-form',
  templateUrl: './colecao-form.component.html'
})
export class ColecaoFormComponent implements OnInit {
  form: FormGroup;
  id?: string;

  constructor(
    private fb: FormBuilder,
    private colecaoService: ColecaoService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    this.form = this.fb.group({
      nome: ['', [Validators.required, Validators.maxLength(100)]],
      ano: ['', [Validators.required]],
      dataLancamento: ['']
    });
  }

  ngOnInit() {
    this.id = this.route.snapshot.paramMap.get('id') || undefined;
    if (this.id) {
      this.colecaoService.getById(this.id).subscribe(colecao => this.form.patchValue(colecao));
    }
  }

  onSubmit() {
    if (this.form.invalid) return;
    const colecao: Colecao = this.form.value;
    if (this.id) {
      this.colecaoService.update(this.id, colecao).subscribe(() => this.router.navigate(['/colecao']));
    } else {
      this.colecaoService.create(colecao).subscribe(() => this.router.navigate(['/colecao']));
    }
  }
}