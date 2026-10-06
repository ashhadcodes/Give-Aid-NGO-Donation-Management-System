namespace Give_Aid_NGO_Donation_Management_System.Configuration
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "noreply@giveaid.org";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "noreply@giveaid.org";
        public string FromName { get; set; } = "Give-AID NGO";
        public bool EnableSsl { get; set; } = true;
        public bool IsSandboxMode { get; set; } = true;
    }

    public class JazzCashSettings
    {
        public bool Enabled { get; set; } = true;
        public bool SandboxMode { get; set; } = true;
        public string MerchantId { get; set; } = "MC_GIVEAID_TEST";
        public string Password { get; set; } = "pass_test_123";
        public string IntegritySalt { get; set; } = "giveaid_salt_test_987";
        public string ReturnUrl { get; set; } = "/Donation/PaymentCallback?provider=JazzCash";
        public string SandboxUrl { get; set; } = "https://sandbox.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/";
        public string ProductionUrl { get; set; } = "https://payments.jazzcash.com.pk/CustomerPortal/transactionmanagement/merchantform/";
        public string Currency { get; set; } = "PKR";
    }

    public class EasypaisaSettings
    {
        public bool Enabled { get; set; } = true;
        public bool SandboxMode { get; set; } = true;
        public string StoreId { get; set; } = "EP_STORE_GIVEAID";
        public string MerchantId { get; set; } = "EP_MERCHANT_GIVEAID";
        public string HashKey { get; set; } = "easypaisa_secret_hash_key_123";
        public string ReturnUrl { get; set; } = "/Donation/PaymentCallback?provider=Easypaisa";
        public string SandboxUrl { get; set; } = "https://easypaystg.easypaisa.com.pk/easypay/Index.jsf";
        public string ProductionUrl { get; set; } = "https://easypay.easypaisa.com.pk/easypay/Index.jsf";
        public string Currency { get; set; } = "PKR";
    }

    public class CardPaymentSettings
    {
        public bool Enabled { get; set; } = true;
        public bool SandboxMode { get; set; } = true;
        public string GatewayName { get; set; } = "Give-AID Secure Card Gateway (Mock/Sandbox)";
        public string SupportedCurrencies { get; set; } = "PKR,USD,GBP,EUR";
        public decimal MinAmount { get; set; } = 100m;
        public decimal MaxAmount { get; set; } = 1000000m;
    }

    public class FileUploadSettings
    {
        public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5MB
        public string[] AllowedExtensions { get; set; } = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        public string[] AllowedMimeTypes { get; set; } = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        public string UploadFolder { get; set; } = "uploads";
    }

    public class AdminSeedSettings
    {
        public string SuperAdminEmail { get; set; } = "superadmin@giveaid.org";
        public string SuperAdminPassword { get; set; } = "SuperAdmin@123!";
        public string SuperAdminFullName { get; set; } = "Global Super Administrator";

        public string AdminEmail { get; set; } = "admin@giveaid.org";
        public string AdminPassword { get; set; } = "Admin@123!";
        public string AdminFullName { get; set; } = "System Administrator";

        public string DemoUserEmail { get; set; } = "donor@giveaid.org";
        public string DemoUserPassword { get; set; } = "Donor@123!";
        public string DemoUserFullName { get; set; } = "Tariq Mahmood";
    }
}
