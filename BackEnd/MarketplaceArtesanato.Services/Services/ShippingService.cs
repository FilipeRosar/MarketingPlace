using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.DTO; // Certifique-se de ter este namespace para ShippingOptionDto
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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

            var token = _configuration["MelhorEnvio:Token"];
            var baseUrl = _configuration["MelhorEnvio:Url"] ?? "https://sandbox.melhorenvio.com.br";

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TramaMarketplace/1.0 (contato@trama.com.br)");
        }

        public async Task<List<ShippingOptionDto>> CalculateShippingAsync(CalculateShippingRequest request)
        {
            var payload = new
            {
                from = new { postal_code = request.ZipCodeFrom },
                to = new { postal_code = request.ZipCodeTo },
                products = request.Items.Select(i => new
                {
                    id = "x",
                    width = (int)i.Width,
                    height = (int)i.Height,
                    length = (int)i.Length,
                    weight = i.Weight,
                    insurance_value = 10.0,
                    quantity = i.Quantity
                }).ToArray()
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v2/me/shipment/calculate", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                return new List<ShippingOptionDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var meOptions = JsonSerializer.Deserialize<List<MelhorEnvioOption>>(content);

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
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                    .Include(o => o.Buyer)
                        .ThenInclude(b => b.Address)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                    throw new InvalidOperationException("Pedido não encontrado.");

                if (order.Items == null || !order.Items.Any())
                    throw new InvalidOperationException("Pedido nao possui itens.");

                var sellerId = order.Items.First().Product.SellerId;

                var seller = await _context.Sellers
                    .Include(s => s.User)
                    .Include(s => s.Address)
                    .FirstOrDefaultAsync(s => s.Id == sellerId);

                if (seller == null)
                    throw new InvalidOperationException("Vendedor nao encontrado.");

                var missingFields = new List<string>();

                var buyerZip = NormalizeZip(order.Buyer?.Address?.ZipCode);
                var sellerZip = NormalizeZip(seller.Address?.ZipCode);
                var buyerZipMasked = FormatZip(buyerZip);
                var sellerZipMasked = FormatZip(sellerZip);
                // Sandbox do Melhor Envio aceita apenas CEPs de teste.
                var isSandbox = _httpClient.BaseAddress != null &&
                                _httpClient.BaseAddress.Host.Contains("sandbox", StringComparison.OrdinalIgnoreCase);
                if (isSandbox)
                {
                    var sandboxFrom = NormalizeZip(_configuration["MelhorEnvio:SandboxZipFrom"]);
                    var sandboxTo = NormalizeZip(_configuration["MelhorEnvio:SandboxZipTo"]);

                    if (!string.IsNullOrWhiteSpace(sandboxFrom))
                        sellerZip = sandboxFrom;
                    if (!string.IsNullOrWhiteSpace(sandboxTo))
                        buyerZip = sandboxTo;

                    sellerZipMasked = FormatZip(sellerZip);
                    buyerZipMasked = FormatZip(buyerZip);
                }
                if (sellerZip == "00000000")
                    missingFields.Add("CEP do vendedor (valor 00000000)");
                if (buyerZip == "00000000")
                    missingFields.Add("CEP do comprador (valor 00000000)");

                if (order.Buyer == null)
                    missingFields.Add("Comprador");

                if (order.Buyer?.Address == null)
                {
                    missingFields.Add("Endereco do comprador");
                }
                else
                {
                    if (buyerZip.Length != 8)
                        missingFields.Add("CEP do comprador");
                    if (string.IsNullOrWhiteSpace(order.Buyer.Address.Street))
                        missingFields.Add("Rua do comprador");
                    if (string.IsNullOrWhiteSpace(order.Buyer.Address.Number))
                        missingFields.Add("Numero do comprador");
                    if (string.IsNullOrWhiteSpace(order.Buyer.Address.City))
                        missingFields.Add("Cidade do comprador");
                    if (string.IsNullOrWhiteSpace(order.Buyer.Address.State))
                        missingFields.Add("Estado do comprador");
                }

                if (seller.User == null)
                    missingFields.Add("Dados do vendedor");

                if (seller.Address == null)
                {
                    missingFields.Add("Endereco do vendedor");
                }
                else
                {
                    if (sellerZip.Length != 8)
                        missingFields.Add("CEP do vendedor");
                    if (string.IsNullOrWhiteSpace(seller.Address.Street))
                        missingFields.Add("Rua do vendedor");
                    if (string.IsNullOrWhiteSpace(seller.Address.Number))
                        missingFields.Add("Numero do vendedor");
                    if (string.IsNullOrWhiteSpace(seller.Address.City))
                        missingFields.Add("Cidade do vendedor");
                    if (string.IsNullOrWhiteSpace(seller.Address.State))
                        missingFields.Add("Estado do vendedor");
                }

                if (string.IsNullOrWhiteSpace(order.Buyer?.Name))
                    missingFields.Add("Nome do comprador");
                if (string.IsNullOrWhiteSpace(order.Buyer?.Email))
                    missingFields.Add("Email do comprador");
                if (string.IsNullOrWhiteSpace(order.Buyer?.CPF))
                    missingFields.Add("CPF do comprador");

                if (string.IsNullOrWhiteSpace(seller.User?.Name))
                    missingFields.Add("Nome do vendedor");
                if (string.IsNullOrWhiteSpace(seller.User?.Email))
                    missingFields.Add("Email do vendedor");
                if (string.IsNullOrWhiteSpace(seller.User?.CPF))
                    missingFields.Add("CPF do vendedor");

                if (missingFields.Count > 0)
                    throw new InvalidOperationException("Campos obrigatorios faltando: " + string.Join(", ", missingFields));

                var from = new
                {
                    name = seller.User.Name?.Trim() ?? "Vendedor Trama",
                    phone = seller.User.Phone ?? "11999999999",
                    email = seller.User.Email ?? "contato@trama.com.br",
                    document = seller.User.CPF ?? "00000000000",
                    address = seller.Address.Street ?? "Rua Desconhecida",
                    number = seller.Address.Number ?? "S/N",
                    complement = seller.Address.Complement ?? "",
                    district = seller.Address.District ?? "Centro",
                    city = seller.Address.City ?? "Cidade",
                    state_abbr = seller.Address.State ?? "SP",
                    country_id = "BR",
                    postal_code = sellerZip,
                    note = $"Pedido Mitrama #{order.Id.ToString().Substring(0, 8).ToUpper()}"
                };

                var to = new
                {
                    name = order.Buyer.Name?.Trim() ?? "Cliente Trama",
                    phone = order.Buyer.Phone ?? "11999999999",
                    email = order.Buyer.Email ?? "cliente@trama.com.br",
                    document = order.Buyer.CPF ?? "00000000000",
                    address = order.Buyer.Address.Street ?? "Rua Desconhecida",
                    number = order.Buyer.Address.Number ?? "S/N",
                    complement = order.Buyer.Address.Complement ?? "",
                    district = order.Buyer.Address.District ?? "Centro",
                    city = order.Buyer.Address.City ?? "Cidade",
                    state_abbr = order.Buyer.Address.State ?? "SP",
                    country_id = "BR",
                    postal_code = buyerZip,
                    note = "Entregar com cuidado - Produto artesanal"
                };

                var serviceId = request.ServiceId;
                if (string.IsNullOrWhiteSpace(serviceId))
                {
                    var calculatePayload = new
                    {
                        from = new { postal_code = sellerZip },
                        to = new { postal_code = buyerZip },
                        products = order.Items.Select(i => new
                        {
                            id = "x",
                            width = (int)(i.Product.Width > 0 ? i.Product.Width : 11),
                            height = (int)(i.Product.Height > 0 ? i.Product.Height : 2),
                            length = (int)(i.Product.Length > 0 ? i.Product.Length : 16),
                            weight = (double)(i.Product.Weight > 0 ? i.Product.Weight : 0.3m),
                            insurance_value = (double)order.TotalAmount,
                            quantity = i.Quantity
                        }).ToArray()
                    };

                    var calcContent = new StringContent(JsonSerializer.Serialize(calculatePayload), Encoding.UTF8, "application/json");
                    var calcResponse = await _httpClient.PostAsync("/api/v2/me/shipment/calculate", calcContent);
                    var calcBody = await calcResponse.Content.ReadAsStringAsync();

                    if (!calcResponse.IsSuccessStatusCode)
                        throw new InvalidOperationException($"Erro ao calcular frete: {calcResponse.StatusCode} - {calcBody}");

                    var options = JsonSerializer.Deserialize<List<MelhorEnvioOption>>(calcBody);
                    var firstValid = options?.FirstOrDefault(o => string.IsNullOrEmpty(o.Error));
                    serviceId = firstValid?.Id.ToString();
                }

                if (string.IsNullOrWhiteSpace(serviceId))
                    throw new InvalidOperationException("Serviço de frete não definido ou indisponível.");

                var cartPayload = new
                {
                    service = serviceId,
                    agency = string.IsNullOrWhiteSpace(request.AgencyId) ? null : request.AgencyId,
                    from,
                    to,
                    products = order.Items.Select(i => new
                    {
                        name = i.Product.Name.Length > 50 ? i.Product.Name.Substring(0, 47) + "..." : i.Product.Name,
                        quantity = i.Quantity,
                        unitary_value = (double)i.UnitPrice,
                        weight = i.Product.Weight > 0 ? i.Product.Weight : 0.3m,
                        width = i.Product.Width > 0 ? i.Product.Width : 11,
                        height = i.Product.Height > 0 ? i.Product.Height : 2,
                        length = i.Product.Length > 0 ? i.Product.Length : 16
                    }).ToList(),
                    volumes = new[]
                    {
                new
                {
                    height = 10,
                    width = 15,
                    length = 20,
                    weight = order.Items.Sum(i => i.Product.Weight * i.Quantity) > 0
                        ? order.Items.Sum(i => i.Product.Weight * i.Quantity)
                        : 0.5m
                }
            },
                    options = new
                    {
                        insurance_value = (double)order.TotalAmount,
                        receipt = false,
                        own_hand = false,
                        reverse = false,
                        non_commercial = true,
                        platform = "Mitra.ma"
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(cartPayload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    }),
                    Encoding.UTF8,
                    "application/json");

                // 7. Adiciona ao carrinho
                var cartResponse = await _httpClient.PostAsync("/api/v2/me/cart", jsonContent);
                var cartContent = await cartResponse.Content.ReadAsStringAsync();

                if (!cartResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Erro ao adicionar ao carrinho: {cartResponse.StatusCode} - {cartContent} | CEP origem: {sellerZipMasked} | CEP destino: {buyerZipMasked}");

                var cartJson = JsonDocument.Parse(cartContent);
                var orderIdME = cartJson.RootElement.GetProperty("id").GetString()
                    ?? throw new InvalidOperationException("ID do pedido no Melhor Envio não retornado.");

                // 8. Checkout
                var checkoutPayload = new { orders = new[] { orderIdME } };
                var checkoutResponse = await _httpClient.PostAsync("/api/v2/me/shipment/checkout",
                    new StringContent(JsonSerializer.Serialize(checkoutPayload), Encoding.UTF8, "application/json"));

                if (!checkoutResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Erro no checkout: {await checkoutResponse.Content.ReadAsStringAsync()}");

                // 9. Gera etiqueta
                var printPayload = new { mode = "public", orders = new[] { orderIdME } };
                var printResponse = await _httpClient.PostAsync("/api/v2/me/shipment/print",
                    new StringContent(JsonSerializer.Serialize(printPayload), Encoding.UTF8, "application/json"));

                if (!printResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Erro ao gerar etiqueta: {await printResponse.Content.ReadAsStringAsync()}");

                var printContent = await printResponse.Content.ReadAsStringAsync();
                var printJson = JsonDocument.Parse(printContent);
                var url = printJson.RootElement.GetProperty("url").GetString();

                if (string.IsNullOrEmpty(url))
                    throw new InvalidOperationException("URL da etiqueta não foi gerada.");

                return url;
            }
            catch (Exception ex)
            {
                // Log detalhado (use ILogger em produção)
                Console.WriteLine($"[ERRO GERAR ETIQUETA] {ex.Message}\n{ex.StackTrace}");
                throw new InvalidOperationException($"Falha ao gerar etiqueta: {ex.Message}");
            }
        }

        private static string NormalizeZip(string? zip)
        {
            if (string.IsNullOrWhiteSpace(zip)) return string.Empty;
            var digits = new string(zip.Where(char.IsDigit).ToArray());
            return digits;
        }

        private static string FormatZip(string? zip)
        {
            var digits = NormalizeZip(zip);
            if (digits.Length != 8) return digits;
            return $"{digits.Substring(0, 5)}-{digits.Substring(5, 3)}";
        }

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
}

