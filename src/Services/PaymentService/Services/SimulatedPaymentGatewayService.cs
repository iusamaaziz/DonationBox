using Microsoft.Extensions.Logging;
using PaymentService.DTOs;
using PaymentService.Models;

namespace PaymentService.Services;

public class SimulatedPaymentGatewayService : IPaymentGatewayService
{
    private readonly ILogger<SimulatedPaymentGatewayService> _logger;
    private readonly Random _random = new();

    public SimulatedPaymentGatewayService(ILogger<SimulatedPaymentGatewayService> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentGatewayResponse> ProcessPaymentAsync(ProcessPaymentRequest request, string transactionId)
    {
        _logger.LogInformation("Processing payment for transaction {TransactionId} with amount {Amount}", 
            transactionId, request.Amount);

        // Simulate processing delay
        await Task.Delay(_random.Next(1000, 3000));

        // Simulate different payment outcomes based on amount and card details
        var response = new PaymentGatewayResponse
        {
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = GenerateGatewayTransactionId(request.PaymentMethod),
            PaymentGateway = GetGatewayName(request.PaymentMethod),
            Amount = request.Amount,
            GatewayFee = CalculateProcessingFee(request.Amount, request.PaymentMethod)
        };

        // Simulate payment scenarios
        if (ShouldSimulateFailure(request))
        {
            response.IsSuccess = false;
            response.Status = PaymentStatus.Failed;
            response.ErrorMessage = GetSimulatedErrorMessage(request);
            _logger.LogWarning("Payment failed for transaction {TransactionId}: {Error}", 
                transactionId, response.ErrorMessage);
        }
        else
        {
            response.IsSuccess = true;
            response.Status = PaymentStatus.Completed;
            response.ProcessingFee = CalculateProcessingFee(request.Amount, request.PaymentMethod);
            response.Metadata = new Dictionary<string, object>
            {
                ["gateway"] = GetGatewayName(request.PaymentMethod),
                ["last4"] = GetLast4Digits(request.PaymentDetails.CardNumber),
                ["auth_code"] = GenerateAuthCode()
            };
            
            _logger.LogInformation("Payment completed for transaction {TransactionId} with fee {Fee}", 
                transactionId, response.ProcessingFee);
        }

        return response;
    }

    public async Task<RefundGatewayResponse> RefundPaymentAsync(string gatewayTransactionId, decimal amount, string reason)
    {
        _logger.LogInformation("Processing refund for gateway transaction {GatewayTransactionId} with amount {Amount}", 
            gatewayTransactionId, amount);

        // Simulate processing delay
        await Task.Delay(_random.Next(500, 2000));

        // Simulate refund success (95% success rate)
        var isSuccess = _random.NextDouble() > 0.05;

        var response = new RefundGatewayResponse
        {
            IsSuccess = isSuccess,
            RefundId = "rf_" + Guid.NewGuid().ToString("N")[..24],
            RefundedAmount = amount,
            ProcessedAt = DateTime.UtcNow
        };

        if (isSuccess)
        {
            response.Status = PaymentStatus.Refunded;
            _logger.LogInformation("Refund completed for gateway transaction {GatewayTransactionId}", gatewayTransactionId);
        }
        else
        {
            response.Status = PaymentStatus.Failed;
            response.ErrorMessage = "Refund failed: Original transaction not found or already refunded";
            _logger.LogWarning("Refund failed for gateway transaction {GatewayTransactionId}: {Error}", 
                gatewayTransactionId, response.ErrorMessage);
        }

        return response;
    }

    public async Task<PaymentGatewayStatus> GetPaymentStatusAsync(string gatewayTransactionId)
    {
        _logger.LogInformation("Checking status for gateway transaction {GatewayTransactionId}", gatewayTransactionId);

        // Simulate API call delay
        await Task.Delay(_random.Next(200, 800));

        // For simulation, assume all payments are completed
        return new PaymentGatewayStatus
        {
            Status = PaymentStatus.Completed,
            Amount = 100.00m, // Placeholder amount
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedAt = DateTime.UtcNow.AddMinutes(-8),
            Metadata = new Dictionary<string, object>
            {
                ["gateway"] = "Simulated Gateway",
                ["status_check_time"] = DateTime.UtcNow
            }
        };
    }

    private bool ShouldSimulateFailure(ProcessPaymentRequest request)
    {
        // Simulate failures based on specific conditions
        
        // Fail if amount is exactly $13.00 (unlucky number test)
        if (request.Amount == 13.00m)
            return true;

        // Fail if card number ends with 0000 (test card)
        if (request.PaymentDetails.CardNumber?.EndsWith("0000") == true)
            return true;

        // Random 5% failure rate for other transactions
        return _random.NextDouble() < 0.05;
    }

    private string GetSimulatedErrorMessage(ProcessPaymentRequest request)
    {
        if (request.Amount == 13.00m)
            return "Insufficient funds";

        if (request.PaymentDetails.CardNumber?.EndsWith("0000") == true)
            return "Card declined";

        var errors = new[]
        {
            "Insufficient funds",
            "Card declined",
            "Expired card",
            "Invalid CVV",
            "Transaction limit exceeded"
        };

        return errors[_random.Next(errors.Length)];
    }

    private decimal CalculateProcessingFee(decimal amount, PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.CreditCard => Math.Round(amount * 0.029m + 0.30m, 2),
            PaymentMethod.DebitCard => Math.Round(amount * 0.024m + 0.30m, 2),
            PaymentMethod.PayPal => Math.Round(amount * 0.034m + 0.30m, 2),
            PaymentMethod.BankTransfer => Math.Round(amount * 0.008m, 2),
            PaymentMethod.ApplePay => Math.Round(amount * 0.029m + 0.30m, 2),
            PaymentMethod.GooglePay => Math.Round(amount * 0.029m + 0.30m, 2),
            _ => Math.Round(amount * 0.030m + 0.30m, 2)
        };
    }

    private string GenerateGatewayTransactionId(PaymentMethod paymentMethod)
    {
        var prefix = paymentMethod switch
        {
            PaymentMethod.CreditCard => "pi_",
            PaymentMethod.DebitCard => "pi_",
            PaymentMethod.PayPal => "PAYID-",
            PaymentMethod.BankTransfer => "ACH-",
            PaymentMethod.ApplePay => "ap_",
            PaymentMethod.GooglePay => "gp_",
            _ => "tx_"
        };

        return prefix + Guid.NewGuid().ToString("N")[..24];
    }

    private string GetGatewayName(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.PayPal => "PayPal",
            PaymentMethod.BankTransfer => "ACH Network",
            PaymentMethod.ApplePay => "Apple Pay",
            PaymentMethod.GooglePay => "Google Pay",
            _ => "Stripe"
        };
    }

    private string GetLast4Digits(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
            return "****";

        return cardNumber[^4..];
    }

    private string GenerateAuthCode()
    {
        return _random.Next(100000, 999999).ToString();
    }
}
