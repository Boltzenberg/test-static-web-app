namespace Boltzenberg.Functions.Dtos.SecretSanta
{
    public record SecretSantaStartEventRequest(
        string EventId,
        string VersionToken
    );
}
