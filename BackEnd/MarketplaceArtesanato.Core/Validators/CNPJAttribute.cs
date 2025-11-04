using MarketplaceArtesanato.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Validators
{
    public class SellerDocumentAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var seller = (Seller)validationContext.ObjectInstance;

            if (string.IsNullOrWhiteSpace(seller.CPF) && string.IsNullOrWhiteSpace(seller.CNPJ))
                return new ValidationResult("Pelo menos CPF ou CNPJ deve ser informado.");

            if (!string.IsNullOrWhiteSpace(seller.CPF) && !IsValidCPF(seller.CPF))
                return new ValidationResult("CPF inválido.");

            if (!string.IsNullOrWhiteSpace(seller.CNPJ) && !IsValidCNPJ(seller.CNPJ))
                return new ValidationResult("CNPJ inválido.");

            return ValidationResult.Success;
        }

        private bool IsValidCPF(string cpf)
        {
            cpf = cpf.Replace(".", "").Replace("-", "");
            if (cpf.Length != 11 || cpf.All(c => c == cpf[0])) return false;

            int[] multiplicadores1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicadores2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            var temp = cpf.Substring(0, 9);
            var soma = multiplicadores1.Select((m, i) => m * (temp[i] - '0')).Sum();
            var resto = soma % 11;
            var digito1 = resto < 2 ? 0 : 11 - resto;
            if (cpf[9] - '0' != digito1) return false;

            temp += digito1;
            soma = multiplicadores2.Select((m, i) => m * (temp[i] - '0')).Sum();
            resto = soma % 11;
            var digito2 = resto < 2 ? 0 : 11 - resto;
            return cpf[10] - '0' == digito2;
        }

        private bool IsValidCNPJ(string cnpj)
        {
            cnpj = cnpj.Replace(".", "").Replace("/", "").Replace("-", "");
            if (cnpj.Length != 14 || cnpj.All(c => c == cnpj[0])) return false;

            int[] multiplicadores1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multiplicadores2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            var temp = cnpj.Substring(0, 12);
            var soma = multiplicadores1.Select((m, i) => m * (temp[i] - '0')).Sum();
            var resto = soma % 11;
            var digito1 = resto < 2 ? 0 : 11 - resto;
            if (cnpj[12] - '0' != digito1) return false;

            temp += digito1;
            soma = multiplicadores2.Select((m, i) => m * (temp[i] - '0')).Sum();
            resto = soma % 11;
            var digito2 = resto < 2 ? 0 : 11 - resto;
            return cnpj[13] - '0' == digito2;
        }
    }
}
