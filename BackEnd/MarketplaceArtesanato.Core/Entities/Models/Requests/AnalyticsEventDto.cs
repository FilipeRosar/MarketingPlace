using System;
using System.Collections.Generic;

namespace MarketplaceArtesanato.Core.Entities.Models.Requests
{
    /// <summary>
    /// DTO para um evento individual de analytics do frontend
    /// </summary>
    public class AnalyticsEventDto
    {
        /// <summary>
        /// Nome do evento (ex: view_item, add_to_cart, purchase)
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// Categoria do evento (ex: Product, Cart, Checkout, Auth)
        /// </summary>
        public string EventCategory { get; set; }

        /// <summary>
        /// Rótulo descritivo do evento
        /// </summary>
        public string EventLabel { get; set; }

        /// <summary>
        /// Valor numérico associado ao evento
        /// </summary>
        public decimal? EventValue { get; set; }

        /// <summary>
        /// Dados customizados específicos do evento
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; }

        /// <summary>
        /// Timestamp quando o evento ocorreu no cliente
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// User-Agent do navegador
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Endereço IP do cliente
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// ID do usuário (se autenticado)
        /// </summary>
        public string UserId { get; set; }

        public AnalyticsEventDto()
        {
            CustomData = new Dictionary<string, object>();
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// DTO para um lote de eventos de analytics
    /// </summary>
    public class AnalyticsEventBatchDto
    {
        /// <summary>
        /// Lista de eventos a serem registrados
        /// </summary>
        public List<AnalyticsEventDto> Events { get; set; }

        /// <summary>
        /// Timestamp quando o lote foi criado no cliente
        /// </summary>
        public DateTime? BatchTimestamp { get; set; }

        public AnalyticsEventBatchDto()
        {
            Events = new List<AnalyticsEventDto>();
            BatchTimestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Resposta do servidor ao receber eventos
    /// </summary>
    public class AnalyticsEventResponseDto
    {
        /// <summary>
        /// Mensagem de status
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Número de eventos recebidos
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Timestamp de processamento no servidor
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Indicador de sucesso
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Tipos de eventos suportados pelo frontend
    /// </summary>
    public static class AnalyticsEventTypes
    {
        // Product Events
        public const string ViewItem = "view_item";
        public const string ItemListView = "item_list_view";
        public const string AddToCart = "add_to_cart";
        public const string RemoveFromCart = "remove_from_cart";
        public const string AddToWishlist = "add_to_wishlist";
        public const string RemoveFromWishlist = "remove_from_wishlist";
        public const string ViewCart = "view_cart";
        public const string ViewItemDetails = "view_item_details";

        // Checkout Events
        public const string BeginCheckout = "begin_checkout";
        public const string AddShippingInfo = "add_shipping_info";
        public const string AddPaymentInfo = "add_payment_info";
        public const string Purchase = "purchase";
        public const string ApplyCoupon = "apply_coupon";
        public const string RemoveCoupon = "remove_coupon";

        // Search Events
        public const string Search = "search";
        public const string ViewSearchResults = "view_search_results";

        // Auth Events
        public const string Login = "login";
        public const string SignUp = "sign_up";
        public const string Logout = "logout";

        // Navigation Events
        public const string PageView = "page_view";
        public const string ScreenView = "screen_view";

        // Error Events
        public const string Error = "error";
        public const string Exception = "exception";
        public const string ApiError = "api_error";

        // Custom Events
        public const string CustomEvent = "custom_event";
    }

    /// <summary>
    /// Categorias de eventos
    /// </summary>
    public static class AnalyticsEventCategories
    {
        public const string Product = "Product";
        public const string Cart = "Cart";
        public const string Checkout = "Checkout";
        public const string Payment = "Payment";
        public const string Auth = "Auth";
        public const string Navigation = "Navigation";
        public const string Search = "Search";
        public const string Error = "Error";
        public const string Performance = "Performance";
        public const string User = "User";
    }
}
