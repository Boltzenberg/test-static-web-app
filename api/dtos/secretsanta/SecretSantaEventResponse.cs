namespace Boltzenberg.Functions.Dtos.SecretSanta
{
    public record SecretSantaEventResponse(
        string EventId,
        string VersionToken,
        string GroupName,
        int Year,
        bool IsRunning,
        List<ParticipantDto> Participants
    );
}
