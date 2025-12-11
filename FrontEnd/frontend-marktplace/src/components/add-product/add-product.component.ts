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
  private notificationService = inject(NotificationService); // Injetar

  tags: string[] = [];
  productForm: FormGroup;
  selectedFile: File | null = null;
  imagePreview: string | null = null;
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

  addTag(value: string) {
    const tag = value.trim();
    if (tag && !this.tags.includes(tag)) {
      this.tags.push(tag);
    }
  }

  removeTag(index: number) {
    this.tags.splice(index, 1);
  }

  onSubmit() {
    if (this.productForm.invalid || !this.selectedFile) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    const formData = new FormData();
    formData.append('Name', this.productForm.get('name')?.value);
    formData.append('Description', this.productForm.get('description')?.value);

    // Converte preço para string com ponto decimal (culture invariant)
    const price = this.productForm.get('price')?.value;
    formData.append('Price', price.toString().replace(',', '.'));

    formData.append('StockQuantity', this.productForm.get('stockQuantity')?.value);
    formData.append('Category', this.productForm.get('category')?.value);

    // CORREÇÃO CRÍTICA: Enviar a imagem com o nome exato 'Images'
    // O Backend espera: public List<IFormFile> Images { get; set; }
    formData.append('Images', this.selectedFile, this.selectedFile.name);

    // Envio de Tags (List<string>)
    for (let i = 0; i < this.tags.length; i++) {
        formData.append(`Tags[${i}]`, this.tags[i]);
    }

    this.productService.createProduct(formData).subscribe({
      next: () => {
        this.isLoading = false;
        this.notificationService.success('Produto criado com sucesso!');
        this.router.navigate(['/seller-dashboard']);
      },
      error: (err) => {
        console.error('Erro ao criar produto:', err);
        this.isLoading = false;

        // Extrai mensagem de erro detalhada se vier do ValidationProblemDetails
        let errorMsg = 'Erro desconhecido.';
        if (err.error?.errors) {
            errorMsg = Object.values(err.error.errors).flat().join(', ');
        } else if (err.error?.message) {
            errorMsg = err.error.message;
        } else if (typeof err.error === 'string') {
            errorMsg = err.error;
        }

        this.notificationService.error(`Falha ao cadastrar: ${errorMsg}`);
      }
    });
  }
}
