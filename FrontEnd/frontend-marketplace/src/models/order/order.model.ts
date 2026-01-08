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
  quantity: number;
  unitPrice: number;
  productImage?: string;
}

export interface Order {
  id: string;
  createdAt: string;
  totalAmount: number;
  status: OrderStatus;
  trackingCode?: string;
  carrier?: string;
  shippedAt?: string;
  items: OrderItem[];
}
