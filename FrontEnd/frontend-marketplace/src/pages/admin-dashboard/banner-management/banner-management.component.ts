import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BannerService, Banner, CreateBannerDto, UpdateBannerDto } from '../../../services/banner/banner.service';
import { NotificationService } from '../../../services/notification/notification.service';

@Component({
  selector: 'app-banner-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './banner-management.component.html',
  styleUrl: './banner-management.component.css'
})
export class BannerManagementComponent implements OnInit {
  private bannerService = inject(BannerService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  banners = signal<Banner[]>([]);
  isLoading = signal(false);
  showForm = signal(false);
  editingId = signal<string | null>(null);

  form!: FormGroup;
  selectedFile: File | null = null;
  previewUrl: string | null = null;

  sortedBanners = computed(() => {
    return [...this.banners()].sort((a, b) => a.displayOrder - b.displayOrder);
  });

  ngOnInit() {
    this.initForm();
    this.loadBanners();
  }

  private initForm() {
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      subtitle: [''],
      linkUrl: [''],
      displayOrder: [0, Validators.required],
      isActive: [true],
      backgroundColor: ['#ffffff'],
      fontFamily: ['Arial, sans-serif'],
      fontColor: ['#1f2937'],
      fontSizeTitle: [48],
      fontSizeSubtitle: [18],
      imageWidth: [1200, Validators.required],
      imageHeight: [400, Validators.required],
      imageObjectFit: ['cover']
    });
  }

  private loadBanners() {
    this.isLoading.set(true);
    this.bannerService.getAllBanners().subscribe({
      next: (data) => {
        this.banners.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.notificationService.error('Erro ao carregar banners');
        this.isLoading.set(false);
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      
      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.notificationService.error('Por favor, selecione uma imagem válida');
        return;
      }

      // Validate file size (max 5MB)
      if (file.size > 5 * 1024 * 1024) {
        this.notificationService.error('A imagem deve ter no máximo 5MB');
        return;
      }

      this.selectedFile = file;

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        this.previewUrl = e.target?.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  openForm(banner?: Banner) {
    if (banner) {
      this.editingId.set(banner.id);
      this.form.patchValue({
        title: banner.title,
        subtitle: banner.subtitle || '',
        linkUrl: banner.linkUrl || '',
        displayOrder: banner.displayOrder,
        isActive: banner.isActive,
        backgroundColor: banner.backgroundColor || '#ffffff',
        fontFamily: banner.fontFamily || 'Arial, sans-serif',
        fontColor: banner.fontColor || '#1f2937',
        fontSizeTitle: banner.fontSizeTitle || 48,
        fontSizeSubtitle: banner.fontSizeSubtitle || 18,
        imageWidth: banner.imageWidth || 1200,
        imageHeight: banner.imageHeight || 400,
        imageObjectFit: banner.imageObjectFit || 'cover'
      });
      this.previewUrl = banner.imageUrl;
    } else {
      this.editingId.set(null);
      this.form.reset({ 
        displayOrder: this.banners().length, 
        isActive: true,
        backgroundColor: '#ffffff',
        fontFamily: 'Arial, sans-serif',
        fontColor: '#1f2937',
        fontSizeTitle: 48,
        fontSizeSubtitle: 18,
        imageWidth: 1200,
        imageHeight: 400,
        imageObjectFit: 'cover'
      });
      this.previewUrl = null;
    }
    this.selectedFile = null;
    this.showForm.set(true);
  }

  closeForm() {
    this.showForm.set(false);
    this.form.reset();
    this.selectedFile = null;
    this.previewUrl = null;
    this.editingId.set(null);
  }

  saveBanner() {
    if (!this.form.valid) {
      this.notificationService.error('Por favor, preencha os campos obrigatórios');
      return;
    }

    const editId = this.editingId();
    if (editId) {
      // Update
      if (!this.selectedFile && !this.previewUrl) {
        this.notificationService.error('A imagem é obrigatória');
        return;
      }

      const updateData: UpdateBannerDto = {
        title: this.form.value.title,
        subtitle: this.form.value.subtitle || undefined,
        linkUrl: this.form.value.linkUrl || undefined,
        displayOrder: this.form.value.displayOrder,
        isActive: this.form.value.isActive,
        backgroundColor: this.form.value.backgroundColor,
        fontFamily: this.form.value.fontFamily,
        fontColor: this.form.value.fontColor,
        fontSizeTitle: this.form.value.fontSizeTitle,
        fontSizeSubtitle: this.form.value.fontSizeSubtitle,
        imageWidth: this.form.value.imageWidth,
        imageHeight: this.form.value.imageHeight,
        imageObjectFit: this.form.value.imageObjectFit
      };

      this.bannerService.updateBanner(editId, updateData, this.selectedFile || undefined).subscribe({
        next: () => {
          this.notificationService.success('Banner atualizado com sucesso!');
          this.closeForm();
          this.loadBanners();
        },
        error: (err) => {
          console.error(err);
          this.notificationService.error('Erro ao atualizar banner');
        }
      });
    } else {
      // Create
      if (!this.selectedFile) {
        this.notificationService.error('Por favor, selecione uma imagem');
        return;
      }

      const createData: CreateBannerDto = {
        title: this.form.value.title,
        subtitle: this.form.value.subtitle || undefined,
        linkUrl: this.form.value.linkUrl || undefined,
        displayOrder: this.form.value.displayOrder,
        backgroundColor: this.form.value.backgroundColor,
        fontFamily: this.form.value.fontFamily,
        fontColor: this.form.value.fontColor,
        fontSizeTitle: this.form.value.fontSizeTitle,
        fontSizeSubtitle: this.form.value.fontSizeSubtitle,
        imageWidth: this.form.value.imageWidth,
        imageHeight: this.form.value.imageHeight,
        imageObjectFit: this.form.value.imageObjectFit
      };

      this.bannerService.createBanner(createData, this.selectedFile).subscribe({
        next: () => {
          this.notificationService.success('Banner criado com sucesso!');
          this.closeForm();
          this.loadBanners();
        },
        error: (err) => {
          console.error(err);
          this.notificationService.error('Erro ao criar banner');
        }
      });
    }
  }

  deleteBanner(id: string) {
    if (!confirm('Tem certeza que deseja deletar este banner?')) return;

    this.bannerService.deleteBanner(id).subscribe({
      next: () => {
        this.notificationService.success('Banner deletado com sucesso!');
        this.loadBanners();
      },
      error: (err) => {
        console.error(err);
        this.notificationService.error('Erro ao deletar banner');
      }
    });
  }

  toggleActive(banner: Banner) {
    const updateData: UpdateBannerDto = { isActive: !banner.isActive };
    this.bannerService.updateBanner(banner.id, updateData).subscribe({
      next: () => {
        this.notificationService.success(
          banner.isActive ? 'Banner desativado!' : 'Banner ativado!'
        );
        this.loadBanners();
      },
      error: (err) => {
        console.error(err);
        this.notificationService.error('Erro ao atualizar status');
      }
    });
  }
}
