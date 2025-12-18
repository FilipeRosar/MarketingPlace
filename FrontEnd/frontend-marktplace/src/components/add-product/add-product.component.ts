import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService } from '../../services/product/product.service';
import { NotificationService } from '../../services/notification/notification.service'; // Importar Notification

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
  private notificationService = inject(NotificationService); 
  selectedFiles: File[] = [];
  imagePreviews: string[] = [];
  tags: string[] = [];
  productForm: FormGroup;
  isLoading = false;

  categories = [
    { label: 'Decoração', value: '0' },
    { label: 'Joias', value: '1' },
    { label: 'Roupas', value: '2' },
    { label: 'Arte', value: '3' },
    { label: 'Brinquedos', value: '4' },
    { label: 'Acessórios', value: '5' },
    { label: 'Móveis', value: '6' },
    { label: 'Cozinha', value: '7' },
    { label: 'Papelaria', value: '8' },
    { label: 'Outros', value: '9' }
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
  const files: FileList = event.target.files;

  if (files.length === 0) return;

  if (this.selectedFiles.length + files.length > 10) {
    this.notificationService.warning('Máximo de 10 fotos por produto.');
    return;
  }

  for (let i = 0; i < files.length; i++) {
    const file = files[i];

    if (!file.type.startsWith('image/')) {
      this.notificationService.error(`Arquivo ${file.name} não é uma imagem.`);
      continue;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.notificationService.error(`Imagem ${file.name} muito grande (máx 5MB).`);
      continue;
    }

    this.selectedFiles.push(file);

    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.imagePreviews.push(e.target.result);
    };
    reader.readAsDataURL(file);
  }

  this.productForm.markAsDirty();
}

  addTag(value: string) {
    const tag = value.trim();
    if (tag && !this.tags.includes(tag)) {
      this.tags.push(tag);
    }
  }
  removeImage(index: number) {
  this.selectedFiles.splice(index, 1);
  this.imagePreviews.splice(index, 1);
  this.productForm.markAsDirty();
}
  removeTag(index: number) {
    this.tags.splice(index, 1);
  }

 onSubmit() {
  if (this.productForm.invalid || this.selectedFiles.length === 0) {
    this.productForm.markAllAsTouched();
    this.notificationService.error('Preencha todos os campos e adicione pelo menos uma foto.');
    return;
  }

  this.isLoading = true;
  const formData = new FormData();

  formData.append('Name', this.productForm.get('name')!.value);
  formData.append('Description', this.productForm.get('description')!.value);
  formData.append('Price', this.productForm.get('price')!.value.toString().replace(',', '.'));
  formData.append('StockQuantity', this.productForm.get('stockQuantity')!.value);
  formData.append('Category', this.productForm.get('category')!.value);

  // ENVIA TODAS AS IMAGENS
  this.selectedFiles.forEach(file => {
    formData.append('Images', file, file.name);
  });

  // Tags
  this.tags.forEach((tag, i) => {
    formData.append(`Tags[${i}]`, tag);
  });

  this.productService.createProduct(formData).subscribe({
    next: () => {
      this.notificationService.success('Produto criado com sucesso!');
      this.router.navigate(['/seller-dashboard']);
    },
    error: (err) => {
      this.isLoading = false;
      console.error(err);
      this.notificationService.error('Erro ao publicar. Verifique os dados.');
    },
    complete: () => this.isLoading = false
  });
}
}
