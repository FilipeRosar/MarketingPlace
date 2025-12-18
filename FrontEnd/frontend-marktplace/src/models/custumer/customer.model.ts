export interface Customer {
  id: string;
  name: string;
  email: string;
  phone: string | null;
  cpf: string | null;
  profileImageUrl: string | null;
  createdAt: string;
  lastOrderDate: string | null;
  totalSpent: number;
}
