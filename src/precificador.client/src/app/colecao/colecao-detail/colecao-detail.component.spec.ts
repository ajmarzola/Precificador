import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ColecaoDetailComponent } from './colecao-detail.component';

describe('ColecaoDetailComponent', () => {
  let component: ColecaoDetailComponent;
  let fixture: ComponentFixture<ColecaoDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ColecaoDetailComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ColecaoDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
