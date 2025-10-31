using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Validators
{
    public class CPFAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string cpf || string.IsNullOrWhiteSpace(cpf))
            {
                return new ValidationResult("CPF é obrigatorio.");
            }
            if (!IsValidCpf(cpf))
            {
                return ValidationResult.Success;
            }
            return ValidationResult.Success;
        }
        public static bool IsValidCpf(string cpf)
        {
            cpf = cpf.OnlyNumbers();
            if (cpf.Length != 11 || cpf.All(c => c == cpf[0])) return false;

            int[] m1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] m2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            var temp = cpf.Substring(0, 9);
            var sum = 0;
            for (int i = 0; i < 9; i++) sum += (cpf[i] - '0') * m1[i];
            var digit1 = sum % 11 < 2 ? 0 : 11 - sum % 11;

            temp += digit1;
            sum = 0;
            for (int i = 0; i < 10; i++) sum += (temp[i] - '0') * m2[i];
            var digit2 = sum % 11 < 2 ? 0 : 11 - sum % 11;

            return cpf.EndsWith($"{digit1}{digit2}");
        }
    }

    public static class StringExtensions
    {
        public static string OnlyNumbers(this string str) =>
            new string(str.Where(char.IsDigit).ToArray());
    }
}

