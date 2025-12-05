using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class ShippingService : IShippingService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ArtesianDbContext _context;

        public ShippingService(HttpClient httpClient, IConfiguration configuration, ArtesianDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;

            // Configuração do Cliente HTTP para o Melhor Envio
            var token = _configuration["MelhorEnvio:Token"];
            var baseUrl = _configuration["MelhorEnvio:Url"] ?? "https://sandbox.melhorenvio.com.br"; // Sandbox por padrão

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TramaMarketplace/1.0 (contato@trama.com.br)");
        }

        public async Task<List<ShippingOptionDto>> CalculateShippingAsync(CalculateShippingRequest request)
        {
            // Payload exigido pelo Melhor Envio
            var payload = new
            {
                from = new { postal_code = request.ZipCodeFrom },
                to = new { postal_code = request.ZipCodeTo },
                products = request.Items.Select(i => new
                {
                    id = "x", // ID genérico para cotação
                    width = (int)i.Width,
                    height = (int)i.Height,
                    length = (int)i.Length,
                    weight = i.Weight,
                    insurance_value = 10.0, // Valor seguro mínimo ou real
                    quantity = i.Quantity
                }).ToArray()
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // 1. Chamada API Calculate
            var response = await _httpClient.PostAsync("/api/v2/me/shipment/calculate", jsonContent);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var meOptions = JsonSerializer.Deserialize<List<MelhorEnvioOption>>(content);

            // Mapeia para nosso DTO filtrando erros
            var options = meOptions?
                .Where(o => string.IsNullOrEmpty(o.Error))
                .Select(o => new ShippingOptionDto
                {
                    Name = $"{o.Company.Name} ({o.Name})",
                    Price = decimal.TryParse(o.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0,
                    DeliveryTime = o.DeliveryRange.Max,
                    CompanyLogo = o.Company.Picture
                }).ToList();

            return options ?? new List<ShippingOptionDto>();
        }

        public async Task<string> GenerateLabelAsync(GenerateLabelRequest request)
        {
            // 1. Buscar dados reais do pedido, vendedor e comprador no banco
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Buyer).ThenInclude(c => c.Address)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId);

            if (order == null) throw new Exception("Pedido não encontrado.");

            // Assume que o vendedor é o do primeiro item (em marketplace real, agrupar por vendedor)
            var sellerId = order.Items.First().Product.SellerId;
            var seller = await _context.Sellers
                .Include(s => s.Address)
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null) throw new Exception("Vendedor não encontrado.");

            // Payload para Adicionar ao Carrinho do Melhor Envio
            var cartPayload = new
            {
                service = request.ServiceId, // ID do serviço escolhido (ex: 1 para SEDEX)
                from = new
                {
                    name = seller.Name,
                    phone = seller.Phone,
                    email = seller.Email,
                    document = seller.CPF ?? seller.CNPJ, // CPF ou CNPJ
                    address = seller.Address.Street,
                    number = seller.Address.Number,
                    complement = "",
                    district = "Centro", // TODO: Adicionar Bairro na entidade Address
                    city = seller.Address.City,
                    country_id = "BR",
                    postal_code = seller.Address.ZipCode,
                    note = $"Pedido {order.Id}"
                },
                to = new
                {
                    name = order.Buyer.Name,
                    phone = order.Buyer.Phone,
                    email = order.Buyer.Email,
                    document = order.Buyer.CPF,
                    address = order.Buyer.Address.Street,
                    number = order.Buyer.Address.Number,
                    complement = "",
                    district = "Centro", // TODO: Adicionar Bairro
                    city = order.Buyer.Address.City,
                    state_abbr = order.Buyer.Address.State, // Ex: RJ
                    country_id = "BR",
                    postal_code = order.Buyer.Address.ZipCode,
                    note = ""
                },
                products = order.Items.Select(i => new
                {
                    name = i.Product.Name,
                    quantity = i.Quantity,
                    unitary_value = (double)i.UnitPrice,
                    weight = 1.0, // Placeholder: Adicionar Peso no Produto
                    width = 10,   // Placeholder: Adicionar Dimensões no Produto
                    height = 10,
                    length = 10
                }).ToList(),
                volumes = new[]
                {
                    new { height = 10, width = 10, length = 10, weight = 1.0 } // Caixa final
                },
                options = new
                {
                    insurance_value = (double)order.TotalAmount,
                    receipt = false,
                    own_hand = false,
                    reverse = false,
                    non_commercial = true // Declaração de conteúdo
                }
            };

            // 2. Adicionar ao Carrinho
            var cartRes = await _httpClient.PostAsync("/api/v2/me/cart",
                new StringContent(JsonSerializer.Serialize(cartPayload), Encoding.UTF8, "application/json"));
            cartRes.EnsureSuccessStatusCode();

            var cartJson = JsonDocument.Parse(await cartRes.Content.ReadAsStringAsync());
            var orderIdME = cartJson.RootElement.GetProperty("id").GetString();

            // 3. Checkout (Pagar com saldo da carteira)
            var checkoutPayload = new { orders = new[] { orderIdME } };
            var checkoutRes = await _httpClient.PostAsync("/api/v2/me/shipment/checkout",
                new StringContent(JsonSerializer.Serialize(checkoutPayload), Encoding.UTF8, "application/json"));
            checkoutRes.EnsureSuccessStatusCode();

            // 4. Gerar URL de Impressão
            var printPayload = new { mode = "public", orders = new[] { orderIdME } };
            var printRes = await _httpClient.PostAsync("/api/v2/me/shipment/print",
                new StringContent(JsonSerializer.Serialize(printPayload), Encoding.UTF8, "application/json"));
            printRes.EnsureSuccessStatusCode();

            var printJson = JsonDocument.Parse(await printRes.Content.ReadAsStringAsync());
            var url = printJson.RootElement.GetProperty("url").GetString();

            return url ?? throw new Exception("URL da etiqueta não gerada.");
        }
    }

    // --- Classes Auxiliares para Deserialização (Melhor Envio) ---
    public class MelhorEnvioOption
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("price")] public string Price { get; set; } = "0";
        [JsonPropertyName("delivery_range")] public DeliveryRange DeliveryRange { get; set; } = new();
        [JsonPropertyName("company")] public Company Company { get; set; } = new();
        [JsonPropertyName("error")] public string? Error { get; set; }
    }

    public class DeliveryRange
    {
        [JsonPropertyName("min")] public int Min { get; set; }
        [JsonPropertyName("max")] public int Max { get; set; }
    }

    public class Company
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("picture")] public string Picture { get; set; } = string.Empty;
    }
}
