namespace Boltzenberg.Functions.Dtos.SecretSanta
{
    public record SecretSantaEventUpdateRequest(
        string EventId,
        string VersionToken,
        string GroupName,
        int Year,
        List<ParticipantDto> Participants
    );
}
