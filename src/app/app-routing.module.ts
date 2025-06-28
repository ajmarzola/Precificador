import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ColecaoListComponent } from './colecao/colecao-list/colecao-list.component';
import { ColecaoFormComponent } from './colecao/colecao-form/colecao-form.component';

const routes: Routes = [
  { path: 'colecao', component: ColecaoListComponent },
  { path: 'colecao/new', component: ColecaoFormComponent },
  { path: 'colecao/edit/:id', component: ColecaoFormComponent },
  // Adicione outras rotas conforme necessário
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }