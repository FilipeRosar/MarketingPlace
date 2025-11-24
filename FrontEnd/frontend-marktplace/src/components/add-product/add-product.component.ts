import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService } from '../../services/product/product.service';

@Component({
  selector: 'app-add-product',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './add-product.component.html',
  styleUrl: './add-product.component.css'
})
export class AddProductComponent {
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private router = inject(Router);

  productForm: FormGroup;
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  isLoading = false;

  categories = [
    { label: 'Decoração', value: 'HomeDecor' },
    { label: 'Joias', value: 'Jewelry' },
    { label: 'Roupas', value: 'Clothing' },
    { label: 'Arte', value: 'Art' },
    { label: 'Brinquedos', value: 'Toys' },
    { label: 'Acessórios', value: 'Accessories' },
    { label: 'Móveis', value: 'Furniture' },
    { label: 'Cozinha', value: 'Kitchenware' },
    { label: 'Papelaria', value: 'Stationery' },
    { label: 'Outros', value: 'Other' }
  ];

  constructor() {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(5)]],
      price: ['', [Validators.required, Validators.min(0.01)]],
      stockQuantity: [1, [Validators.required, Validators.min(1)]],
      category: ['', Validators.required]
    });
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      this.productForm.markAsDirty();

      const reader = new FileReader();
      reader.onload = () => {
        this.imagePreview = reader.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  onSubmit() {
    if (this.productForm.invalid || !this.selectedFile) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    const formData = new FormData();
    formData.append('name', this.productForm.get('name')?.value);
    formData.append('description', this.productForm.get('description')?.value);

    // Tratamento de preço para garantir formato correto
    const priceValue = this.productForm.get('price')?.value;
    formData.append('price', priceValue.toString().replace(',', '.'));

    formData.append('stockQuantity', this.productForm.get('stockQuantity')?.value);
    formData.append('category', this.productForm.get('category')?.value);

    formData.append('images', this.selectedFile);

    console.log('--- Enviando Produto ---');
    formData.forEach((value, key) => {
      console.log(`${key}:`, value);
    });

    this.productService.createProduct(formData).subscribe({
      next: () => {
        this.isLoading = false;
        alert('Produto criado com sucesso!');
        this.router.navigate(['/seller-dashboard']);
      },
      error: (err) => {
        console.error('Erro ao criar produto:', err);
        this.isLoading = false;

        const serverMsg = typeof err.error === 'string' ? err.error : err.error?.title || 'Erro desconhecido';
        alert(`Falha ao cadastrar: ${serverMsg}`);
      }
    });
  }
}
