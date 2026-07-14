namespace EmporioGege.Application.DTOs
{
    // FORMATO ASSUMIDO — a Zé Delivery (Seller Public API) não publica o schema completo
    // do payload de webhook sem aprovação como parceiro. Os nomes de campo abaixo são um
    // adaptador provisório; ajuste-os assim que tiver acesso à documentação/sandbox real
    // (endpoint "patchConfigurar webhook" / seção "Validação de autenticidade do evento"
    // em https://seller-public-api.ze.delivery/docs).
    public record ZeDeliveryWebhookPayload(string EventId, string EventType, ZeDeliveryOrder Order);

    public record ZeDeliveryOrder(string OrderId, List<ZeDeliveryItem> Items);

    // Sku aqui é comparado contra produtos.codigo_barras — também uma suposição provisória,
    // ajustar se a Zé Delivery identificar itens por outro campo (SKU interno próprio, etc.).
    public record ZeDeliveryItem(string Sku, int Quantity);
}
