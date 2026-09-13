// src/app/core/models/auth.model.ts
export interface LoginRequest { email: string; password: string; }
export interface RegisterRequest { name: string; email: string; password: string; }
export interface AuthResponse { token: string; role: string; name: string; userId: string; }
export interface Product {
  id: string;
  name: string;
  category: string;
  price: number;
  description: string;
  imageUrl: string;
  stock: number;
  createdAt: string;
}