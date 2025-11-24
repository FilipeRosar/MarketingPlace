export interface RegisterCustomer {
  name: string;
  email: string;
  password: string;
  cpf: string;
  phone: string;
  role: 'Customer';
  address: {
    street: string;
    number: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  }
}
export interface RegisterSeller {
  name: string;
  email: string;
  password: string;
  phone: string;
  cpf: string;
  cnpj?: string;
  role: 'Seller';
  address: {
    street: string;
    number: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
  }
}
