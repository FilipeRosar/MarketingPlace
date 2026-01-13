export enum OrderStatus {
  Pending = 0,
  Confirmed = 1,
  Processing = 2,
  Sent = 3,
  Delivered = 4,
  Canceled = 5,
  Refunded = 6
}

export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  productImage?: string;
  unitPrice: number;
  quantity: number;
  sellerName?: string;
}

export interface Order {
  id: string;
  sellerName?: string;
  shippingCost: number;
  createdAt: string;
  totalAmount: number;
  status: OrderStatus;
  trackingCode?: string;
  trackingCodes?: string[];
  carrier?: string;
  shippingAddress?:{
    street: string;
    number?: string;
    complement?: string;
    neighborhood: string;
    city: string;
    state: string;
    zipCode: string;
  }
  shippedAt?: string;
  items: OrderItem[];
}
