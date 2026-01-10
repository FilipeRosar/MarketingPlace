export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  tags: string[];
  imageUrl: string;
  images: string[];
  weight?: number;
  width?: number;
  height?: number;
  length?: number;

  category: string;
  status?: string;

  averageRating: number;
  totalRatings: number;
  salePrice?: number;
  maxInstallments?: number;
  maxNoInterestInstallments?: number;
  sellerId: string;
  seller: Seller;
  sellerName: string;
  storyEnabled?: boolean;
  storyMaker?: string;
  storyExperience?: string;
  storyInspiration?: string;
  storyMarkdown?: string;
  storyMediaUrls?: string[];
}
export interface Seller {
  id: string;
  name: string;
  email: string;
  phone: string;
}
export interface CreateProductRequest {
  name: string;
  description: string;
  price: number;
  category: string;
  imageFile?: File;
  stockQuantity: number;
}
export class ProductModel  {

}
