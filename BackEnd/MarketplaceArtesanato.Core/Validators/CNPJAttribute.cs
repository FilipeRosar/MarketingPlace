using MarketplaceArtesanato.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MarketplaceArtesanato.Core.Validators
{
    public class SellerDocumentAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (validationContext.ObjectInstance is not Seller seller)
            {
                return ValidationResult.Success; 
            }

            if (!string.IsNullOrWhiteSpace(seller.CNPJ))
            {
                if (!IsValidCNPJ(seller.CNPJ))
                {
                    return new ValidationResult("O CNPJ informado é inválido.");
                }
            }

            return ValidationResult.Success;
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