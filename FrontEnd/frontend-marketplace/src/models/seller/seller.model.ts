export interface Seller {
  id: string;
  userId?: string;
  name: string;

  instagram?: string;
  facebook?: string;
  tiktok?: string;
  youtube?: string;

  moments?: SellerMoment[];
}

export interface SellerMoment {
  id: string;
  videoUrl: string;
  thumbnailUrl?: string;
  description: string;
  createdAt: string;
}
