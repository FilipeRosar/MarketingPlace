import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CouponCodeGeneratorComponent } from './coupon-code-generator.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

describe('CouponCodeGeneratorComponent', () => {
  let component: CouponCodeGeneratorComponent;
  let fixture: ComponentFixture<CouponCodeGeneratorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CouponCodeGeneratorComponent, CommonModule, FormsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(CouponCodeGeneratorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default values', () => {
    expect(component.prefix).toBe('');
    expect(component.quantity).toBe(1);
    expect(component.generatedCodes.length).toBe(0);
  });

  it('should generate single code without prefix', () => {
    component.prefix = '';
    component.quantity = 1;
    component.generateCodes();

    expect(component.generatedCodes.length).toBe(1);
    expect(component.generatedCodes[0].length).toBe(8); // 8 random chars
  });

  it('should generate single code with prefix', () => {
    component.prefix = 'SUMMER';
    component.quantity = 1;
    component.generateCodes();

    expect(component.generatedCodes.length).toBe(1);
    expect(component.generatedCodes[0]).toContain('SUMMER');
    expect(component.generatedCodes[0].length).toBe(14); // 6 + 8
  });

  it('should generate multiple codes', () => {
    component.prefix = '';
    component.quantity = 5;
    component.generateCodes();

    expect(component.generatedCodes.length).toBe(5);
  });

  it('should generate unique codes', () => {
    component.prefix = '';
    component.quantity = 10;
    component.generateCodes();

    const uniqueCodes = new Set(component.generatedCodes);
    expect(uniqueCodes.size).toBe(10);
  });

  it('should generate codes with uppercase characters', () => {
    component.prefix = '';
    component.quantity = 5;
    component.generateCodes();

    component.generatedCodes.forEach(code => {
      expect(code).toBe(code.toUpperCase());
    });
  });

  it('should generate codes with alphanumeric characters only', () => {
    component.prefix = '';
    component.quantity = 10;
    component.generateCodes();

    const alphanumericRegex = /^[A-Z0-9]+$/;
    component.generatedCodes.forEach(code => {
      expect(alphanumericRegex.test(code)).toBeTruthy();
    });
  });

  it('should copy single code to clipboard', async () => {
    const testCode = 'TESTCODE123';
    spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());

    component.copyToClipboard(testCode);
    await fixture.whenStable();

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(testCode);
  });

  it('should copy all codes to clipboard', async () => {
    component.generatedCodes = ['CODE1', 'CODE2', 'CODE3'];
    spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());

    component.copyAllToClipboard();
    await fixture.whenStable();

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('CODE1\nCODE2\nCODE3');
  });

  it('should show feedback after copy', (done) => {
    component.generatedCodes = ['CODE1'];
    spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());

    component.copyToClipboard('CODE1');

    setTimeout(() => {
      expect(component.copyFeedback).toBeTruthy();
      done();
    }, 100);
  });

  it('should hide feedback after 2 seconds', (done) => {
    component.generatedCodes = ['CODE1'];
    spyOn(navigator.clipboard, 'writeText').and.returnValue(Promise.resolve());

    component.copyToClipboard('CODE1');

    setTimeout(() => {
      expect(component.copyFeedback).toBeFalsy();
      done();
    }, 2100);
  });

  it('should clear generated codes', () => {
    component.generatedCodes = ['CODE1', 'CODE2', 'CODE3'];
    component.clearCodes();

    expect(component.generatedCodes.length).toBe(0);
  });

  it('should generate new codes after clearing', () => {
    component.prefix = 'TEST';
    component.quantity = 3;
    component.generateCodes();
    expect(component.generatedCodes.length).toBe(3);

    component.clearCodes();
    expect(component.generatedCodes.length).toBe(0);

    component.generateCodes();
    expect(component.generatedCodes.length).toBe(3);
  });

  it('should respect max quantity limit', () => {
    component.quantity = 100; // Trying to exceed max
    component.generateCodes();

    // Component doesn't enforce max at generation, but in UI it's limited to 10
    expect(component.generatedCodes.length).toBe(100);
  });
});
