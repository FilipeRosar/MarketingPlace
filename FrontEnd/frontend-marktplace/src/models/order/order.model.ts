export enum OrderStatus {
  Pending = 0,
  Paid = 1,
  Shipped = 2,
  Delivered = 3,
  Canceled = 4
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
