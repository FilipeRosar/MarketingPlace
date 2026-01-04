import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegisterSeller } from './register-seller';

describe('RegisterSeller', () => {
  let component: RegisterSeller;
  let fixture: ComponentFixture<RegisterSeller>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterSeller]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegisterSeller);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
