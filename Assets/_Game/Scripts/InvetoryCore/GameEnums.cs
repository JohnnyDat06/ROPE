public enum CheckMode
{
    Quantity,   // Số lượng món đồ
    TotalValue  // Tổng giá trị tiền ($) - Thay thế cho Weight
}

public enum ConditionType
{
    Lower,   // Thấp hơn
    Equal,   // Bằng
    Higher   // Cao hơn
}

// Giữ nguyên ItemType
public enum ItemType
{
    Small,
    Large,
    IronSmall,
    IronLarge,
    Special
}