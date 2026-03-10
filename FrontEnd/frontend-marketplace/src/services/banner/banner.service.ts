import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Banner {
  id: string;
  title: string;
  subtitle?: string;
  imageUrl: string;
  linkUrl?: string;
  isActive: boolean;
  displayOrder: number;
  createdAt?: string;
  backgroundColor?: string;
  fontFamily?: string;
  fontColor?: string;
  fontSizeTitle?: number;
  fontSizeSubtitle?: number;
  imageWidth?: number;
  imageHeight?: number;
  imageObjectFit?: string;
}

export interface CreateBannerDto {
  title: string;
  subtitle?: string;
  linkUrl?: string;
  displayOrder: number;
  backgroundColor?: string;
  fontFamily?: string;
  fontColor?: string;
  fontSizeTitle?: number;
  fontSizeSubtitle?: number;
  imageWidth?: number;
  imageHeight?: number;
  imageObjectFit?: string;
}

export interface UpdateBannerDto {
  title?: string;
  subtitle?: string;
  linkUrl?: string;
  isActive?: boolean;
  displayOrder?: number;
  backgroundColor?: string;
  fontFamily?: string;
  fontColor?: string;
  fontSizeTitle?: number;
  fontSizeSubtitle?: number;
  imageWidth?: number;
  imageHeight?: number;
  imageObjectFit?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BannerService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/banners`;

  // Get only active banners (public)
  getActiveBanners(): Observable<Banner[]> {
    return this.http.get<Banner[]>(this.apiUrl);
  }

  // Get all banners including inactive (admin only)
  getAllBanners(): Observable<Banner[]> {
    return this.http.get<Banner[]>(`${this.apiUrl}/admin`);
  }

  // Create banner with image file
  createBanner(data: CreateBannerDto, imageFile: File): Observable<Banner> {
    const formData = new FormData();
    formData.append('title', data.title);
    if (data.subtitle) formData.append('subtitle', data.subtitle);
    if (data.linkUrl) formData.append('linkUrl', data.linkUrl);
    formData.append('displayOrder', data.displayOrder.toString());
    formData.append('image', imageFile);
    if (data.backgroundColor) formData.append('backgroundColor', data.backgroundColor);
    if (data.fontFamily) formData.append('fontFamily', data.fontFamily);
    if (data.fontColor) formData.append('fontColor', data.fontColor);
    if (data.fontSizeTitle) formData.append('fontSizeTitle', data.fontSizeTitle.toString());
    if (data.fontSizeSubtitle) formData.append('fontSizeSubtitle', data.fontSizeSubtitle.toString());
    if (data.imageWidth) formData.append('imageWidth', data.imageWidth.toString());
    if (data.imageHeight) formData.append('imageHeight', data.imageHeight.toString());
    if (data.imageObjectFit) formData.append('imageObjectFit', data.imageObjectFit);

    return this.http.post<Banner>(this.apiUrl, formData);
  }

  // Update banner with optional new image
  updateBanner(id: string, data: UpdateBannerDto, imageFile?: File): Observable<Banner> {
    const formData = new FormData();
    if (data.title) formData.append('title', data.title);
    if (data.subtitle !== undefined) formData.append('subtitle', data.subtitle || '');
    if (data.linkUrl !== undefined) formData.append('linkUrl', data.linkUrl || '');
    if (data.isActive !== undefined) formData.append('isActive', data.isActive.toString());
    if (data.displayOrder !== undefined) formData.append('displayOrder', data.displayOrder.toString());
    if (data.backgroundColor) formData.append('backgroundColor', data.backgroundColor);
    if (data.fontFamily) formData.append('fontFamily', data.fontFamily);
    if (data.fontColor) formData.append('fontColor', data.fontColor);
    if (data.fontSizeTitle) formData.append('fontSizeTitle', data.fontSizeTitle.toString());
    if (data.fontSizeSubtitle) formData.append('fontSizeSubtitle', data.fontSizeSubtitle.toString());
    if (data.imageWidth) formData.append('imageWidth', data.imageWidth.toString());
    if (data.imageHeight) formData.append('imageHeight', data.imageHeight.toString());
    if (data.imageObjectFit) formData.append('imageObjectFit', data.imageObjectFit);
    if (imageFile) formData.append('image', imageFile);

    return this.http.put<Banner>(`${this.apiUrl}/${id}`, formData);
  }

  // Delete banner
  deleteBanner(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
