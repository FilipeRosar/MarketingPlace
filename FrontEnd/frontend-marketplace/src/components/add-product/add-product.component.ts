import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService } from '../../services/product/product.service';
import { NotificationService } from '../../services/notification/notification.service';

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
  storyFiles: File[] = [];
  storyPreviews: string[] = [];
  maxStoryMedia = 8;
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
  subcategoriesByCategory: Record<string, string[]> = {
    '0': [
      'Quadros e Placas',
      'Velas artesanais',
      'Vasos e Cachepos',
      'Macrame',
      'Objetos de mesa',
      'Esculturas decorativas',
      'Iluminacao artesanal',
      'Almofadas e Texteis',
      'Decoracao infantil',
      'Decoracao religiosa'
    ],
    '1': [
      'Aneis',
      'Colares',
      'Pulseiras',
      'Brincos',
      'Pingentes',
      'Joias em prata',
      'Joias em ouro',
      'Pedras naturais',
      'Joias personalizadas',
      'Joias minimalistas'
    ],
    '2': [
      'Camisetas',
      'Vestidos',
      'Moda feminina',
      'Moda masculina',
      'Moda infantil',
      'Roupas personalizadas',
      'Bordados',
      'Croche e Trico',
      'Moda sustentavel',
      'Fantasias'
    ],
    '3': [
      'Pinturas',
      'Ilustracoes',
      'Gravuras',
      'Arte digital',
      'Esculturas',
      'Arte abstrata',
      'Arte realista',
      'Arte contemporanea',
      'Posters artisticos',
      'Artes autorais'
    ],
    '4': [
      'Brinquedos educativos',
      'Brinquedos de madeira',
      'Bonecas artesanais',
      'Amigurumi',
      'Jogos pedagogicos',
      'Quebra-cabecas',
      'Brinquedos sensoriais',
      'Brinquedos infantis',
      'Brinquedos personalizados'
    ],
    '5': [
      'Bolsas',
      'Carteiras',
      'Mochilas',
      'Cintos',
      'Lencos',
      'Chapeus',
      'Oculos artesanais',
      'Capas (celular, notebook)',
      'Bijuterias',
      'Acessorios personalizados'
    ],
    '6': [
      'Mesas',
      'Cadeiras',
      'Bancos',
      'Estantes',
      'Prateleiras',
      'Criados-mudos',
      'Moveis rusticos',
      'Moveis planejados',
      'Moveis infantis',
      'Moveis sustentaveis'
    ],
    '7': [
      'Utensilios de madeira',
      'Tabuas de corte',
      'Canecas artesanais',
      'Pratos e Loucas',
      'Copos e Tacas',
      'Panos de prato',
      'Organizadores',
      'Kits de cozinha',
      'Itens personalizados',
      'Decoracao de cozinha'
    ],
    '8': [
      'Cadernos artesanais',
      'Agendas & planners',
      'Blocos de notas',
      'Marcadores de pagina',
      'Cartoes personalizados',
      'Convites (casamento, aniversario, eventos)',
      'Papelaria para casamento',
      'Papelaria infantil',
      'Papelaria escolar',
      'Scrapbook',
      'Adesivos & stickers',
      'Selos & carimbos artesanais',
      'Caixas & embalagens personalizadas',
      'Papelaria corporativa artesanal',
      'Papelaria ecologica'
    ],
    '9': [
      'Produtos personalizados',
      'Kits presente',
      'Datas comemorativas (Natal, Pascoa, Dia das Maes, etc.)',
      'Lembrancinhas',
      'Produtos sob encomenda',
      'Artesanato regional',
      'Produtos sustentaveis',
      'Itens religiosos',
      'Itens misticos / esotericos',
      'Decoracao sazonal',
      'Produtos exclusivos',
      'Colecionaveis',
      'Itens experimentais / novos produtos'
    ]
  };

  subcategoryOptions: string[] = [];

  constructor() {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: ['', [Validators.required, Validators.minLength(5)]],
      price: ['', [Validators.required, Validators.min(0.01)]],
      weight: ['', [Validators.required, Validators.min(0.01)]],
      width: ['', [Validators.required, Validators.min(1)]],
      height: ['', [Validators.required, Validators.min(1)]],
      length: ['', [Validators.required, Validators.min(1)]],
      stockQuantity: [1, [Validators.required, Validators.min(1)]],
      category: ['', Validators.required],
      subcategory: [''],
      maxInstallments: [12, [Validators.required, Validators.min(1), Validators.max(12)]],
      maxNoInterestInstallments: [0, [Validators.required, Validators.min(0), Validators.max(12)]],
      storyEnabled: [false],
      storyMaker: [""],
      storyExperience: [""],
      storyInspiration: [""],
      storyMarkdown: [""]
    });

    this.productForm.get('category')?.valueChanges.subscribe((value) => {
      this.subcategoryOptions = this.subcategoriesByCategory[value] || [];
      this.productForm.get('subcategory')?.setValue('');
    });
  }

  onFileSelected(event: any) {
  const files: FileList = event.target.files;

  if (files.length === 0) return;

  if (this.selectedFiles.length + files.length > 10) {
    this.notificationService.warning('MÃ¡ximo de 10 fotos por produto.');
    return;
  }

  for (let i = 0; i < files.length; i++) {
    const file = files[i];

    if (!file.type.startsWith('image/')) {
      this.notificationService.error(`Arquivo ${file.name} nÃ£o Ã© uma imagem.`);
      continue;
    }

    if (file.size > 5 * 1024 * 1024) {
      this.notificationService.error(`Imagem ${file.name} muito grande (mÃ¡x 5MB).`);
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

  onStoryMediaSelected(event: any) {
    const files: FileList = event.target.files;

    if (files.length === 0) return;

    if (this.storyFiles.length + files.length > this.maxStoryMedia) {
      this.notificationService.warning(`Maximo de ${this.maxStoryMedia} fotos do processo.`);
      return;
    }

    for (let i = 0; i < files.length; i++) {
      const file = files[i];

      if (!file.type.startsWith('image/')) {
        this.notificationService.error(`Arquivo ${file.name} nao e uma imagem.`);
        continue;
      }

      if (file.size > 5 * 1024 * 1024) {
        this.notificationService.error(`Imagem ${file.name} muito grande (max 5MB).`);
        continue;
      }

      this.storyFiles.push(file);

      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.storyPreviews.push(e.target.result);
      };
      reader.readAsDataURL(file);
    }

    this.productForm.markAsDirty();
  }

  removeStoryMedia(index: number) {
    this.storyFiles.splice(index, 1);
    this.storyPreviews.splice(index, 1);
    this.productForm.markAsDirty();
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
  formData.append('Weight', this.productForm.get('weight')!.value.toString().replace(',', '.'));
  formData.append('Width', this.productForm.get('width')!.value);
  formData.append('Height', this.productForm.get('height')!.value);
  formData.append('Length', this.productForm.get('length')!.value);
  formData.append('StockQuantity', this.productForm.get('stockQuantity')!.value);
  formData.append('Category', this.productForm.get('category')!.value);
  formData.append('MaxInstallments', this.productForm.get('maxInstallments')!.value);
  formData.append('MaxNoInterestInstallments', this.productForm.get('maxNoInterestInstallments')!.value);
  formData.append('StoryEnabled', this.productForm.get('storyEnabled')!.value ? 'true' : 'false');

  // ENVIA TODAS AS IMAGENS
  this.selectedFiles.forEach(file => {
    formData.append('Images', file, file.name);
  });

  const selectedSubcategory = this.productForm.get('subcategory')?.value;
  if (selectedSubcategory && !this.tags.includes(selectedSubcategory)) {
    this.tags.push(selectedSubcategory);
  }

  if (this.productForm.get('storyEnabled')!.value) {
    const storyMaker = String(this.productForm.get('storyMaker')!.value || '').trim();
    const storyExperience = String(this.productForm.get('storyExperience')!.value || '').trim();
    const storyInspiration = String(this.productForm.get('storyInspiration')!.value || '').trim();
    const storyMarkdown = String(this.productForm.get('storyMarkdown')!.value || '').trim();

    if (storyMaker) formData.append('StoryMaker', storyMaker);
    if (storyExperience) formData.append('StoryExperience', storyExperience);
    if (storyInspiration) formData.append('StoryInspiration', storyInspiration);
    if (storyMarkdown) formData.append('StoryMarkdown', storyMarkdown);

    this.storyFiles.forEach(file => {
      formData.append('StoryMedia', file, file.name);
    });
  }

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

