export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;

  imageUrl: string;
  images: string[];

  category: string;
  status?: string;

  averageRating: number;
  totalRatings: number;

  sellerId: string;
  seller: Seller;
  sellerName: string;
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
