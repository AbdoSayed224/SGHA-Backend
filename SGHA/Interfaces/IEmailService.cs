using SGHA.DTO;

namespace SGHA.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailRequestDto emailRequest);
    }
}
