export interface User {
  id: string;
  name: string;
  email: string;
  role: string; // e.g., 'buyer', 'seller', 'admin'
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
