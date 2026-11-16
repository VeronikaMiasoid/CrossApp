namespace Core.Reports;

// DTO звітів: прості записи без поведінки, їх зручно друкувати й серіалізувати.
public sealed record TopCustomerRow(string CustomerId, int OrdersCount, decimal Total);
public sealed record OrderSummaryRow(string OrderId, string CustomerId, int LinesCount, decimal Total);
public sealed record ProductSalesRow(string ProductId, string ProductName, int Quantity, decimal Revenue);
public sealed record CustomerGroupRow(string CustomerId, int OrdersCount, decimal Total, decimal MaxOrder);
public sealed record StatusSummary(int Count, decimal Sum, decimal Average, decimal Max);
public sealed record CustomerOrdersRow(string CustomerId, string CustomerName, int OrdersCount, decimal Total);
