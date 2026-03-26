namespace Boltzenberg.Functions.Dtos.SecretSanta
{
    public record ParticipantDto(
        string Name,
        string Email,
        string? SantaForName,
        string? SantaForEmail
    );
}
