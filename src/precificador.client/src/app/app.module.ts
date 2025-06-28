import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ColecaoListComponent } from './colecao/colecao-list/colecao-list.component';
import { ColecaoFormComponent } from './colecao/colecao-form/colecao-form.component';
import { ColecaoDetailComponent } from './colecao/colecao-detail/colecao-detail.component';

@NgModule({
  declarations: [
    AppComponent,
    ColecaoListComponent,
    ColecaoFormComponent,
    ColecaoDetailComponent
  ],
  imports: [
    BrowserModule, HttpClientModule,
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
