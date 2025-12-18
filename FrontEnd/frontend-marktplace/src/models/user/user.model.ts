export interface User {
  id: string;
  name: string;
  email: string;
  role: string;
  profileImageUrl?: string;
  phone?: string;
  cpf?: string;
  address?: any;
  isApproved: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface AuthResponse {
  token: string;
  expiresIn: number;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}
