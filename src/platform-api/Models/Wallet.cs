namespace AgentPort.PlatformApi.Data;

public sealed class WalletAccount : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Balance { get; set; }
    public decimal ReservedBalance { get; set; }
    public string Status { get; set; } = "active";
    public string MetadataJson { get; set; } = "{}";
}

public sealed class WalletTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletAccountId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ExternalReference { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class WalletReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WalletAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "active";
    public DateTime ExpiresAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PaymentProviderConfig : EntityBase
{
    public Guid WorkspaceId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Mode { get; set; } = "test";
    public bool IsEnabled { get; set; }
    public Guid? ApiKeySecretReferenceId { get; set; }
    public Guid? WebhookSecretReferenceId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
