export interface LoginRequest { email: string; password: string; }
export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  phoneNumber?: string;
  birthDate?: Date;
}
export interface AuthResponse { token: string; role: string; name: string; userId: string; phone?: string; }

export interface Category {
  id: string;
  name: string;
  nameEn: string | null;
  parentId: string | null;
  children: Category[];
}

export interface ProductImage {
  id: string;
  imageUrl: string;
  order: number;
}

export interface CategoryRef {
  id: string;
  name: string;
  nameEn: string | null;
}

export interface Product {
  id: string;
  name: string;
  nameEn: string | null;
  slug: string;
  categories: CategoryRef[];
  price: number;
  description: string;
  descriptionEn: string | null;
  images: ProductImage[];
  stock: number;
  createdAt: string;
}

export interface TrashedProduct {
  id: string;
  name: string;
  nameEn: string | null;
  imageUrl: string | null;
  price: number;
  deletedAt: string;
}

export interface TrashedCategory {
  id: string;
  name: string;
  nameEn: string | null;
  deletedAt: string;
}

export interface CreateProductDto {
  name: string;
  nameEn: string | null;
  categoryIds: string[];
  price: number;
  description: string;
  descriptionEn: string | null;
  imageUrls: string[];
  stock: number;
}

export type FulfillmentChoice = 'Split' | 'Wait';

export interface CartItem {
  id: string;
  productId: string;
  productName: string;
  imageUrl: string;
  price: number;
  quantity: number;
  total: number;
  stock: number;
  fulfillmentChoice: FulfillmentChoice | null;
}

export interface FavouriteItem {
  id: string;
  productId: string;
  productName: string;
  imageUrl: string;
  price: number;
}

export interface HeroImage {
  id: string;
  imageUrl: string;
  sortOrder: number;
}

export interface ContactMessage {
  id: string;
  name: string;
  email: string;
  message: string;
  createdAt: string;
  isReplied: boolean;
  replyText: string | null;
  repliedAt: string | null;
}

export type OrderStatus = 'Pending' | 'Shipped' | 'Delivered' | 'Cancelled';

export interface AdminOrderItem {
  id: string;
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  fulfillmentChoice: FulfillmentChoice | null;
  shippedNowQuantity: number | null;
  firstShipmentSentAt: string | null;
  nextShipmentEstimatedAt: string | null;
  finalShipmentSentAt: string | null;
}

export interface AdminOrder {
  id: string;
  customerName: string;
  customerEmail: string;
  deliveryFirstName: string;
  deliveryLastName: string;
  deliveryPhone: string;
  deliveryAddress: string;
  deliveryMethod: string;
  paymentMethod: string;
  status: OrderStatus;
  cancellationNote: string | null;
  isSeen: boolean;
  totalAmount: number;
  createdAt: string;
  items: AdminOrderItem[];
}

export interface UserProfile {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  city?: string;
  address?: string;
  postCode?: string;
}