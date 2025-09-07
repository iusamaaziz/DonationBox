using PaymentService.DTOs;

namespace PaymentService.Services;

public interface IPaymentSagaService
{
    void QueuePaymentForProcessing(ProcessPaymentRequest request, string transactionId);
}
