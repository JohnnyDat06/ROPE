public enum CheckMode
{
    Quantity,
    Weight
}

public enum ConditionType
{
    Lower, Equal, Higher
}

// --- THÊM MỚI ---
public enum ItemType
{
    Small,      // Đồ nhỏ (1-10$)
    Large,      // Đồ lớn (11-36$)
    IronSmall,  // Sắt nhỏ (16-44$)
    IronLarge,  // Sắt lớn (50-100$)
    Special     // Đặc biệt (>500$)
}